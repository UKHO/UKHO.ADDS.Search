using System.Collections.Generic;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
	public sealed class RetryingTransformNode<TIn, TOut> : NodeBase<Envelope<TIn>, Envelope<TOut>>
	{
		private readonly Func<TIn, CancellationToken, ValueTask<TOut>> transform;
		private readonly IRetryPolicy retryPolicy;
		private readonly Func<Exception, bool> isTransientException;
		private readonly ChannelWriter<Envelope<TIn>>? errorOutput;
		private readonly bool forwardFailedToMainOutput;
		private readonly string errorCode;

		public RetryingTransformNode(
			string name,
			ChannelReader<Envelope<TIn>> input,
			ChannelWriter<Envelope<TOut>> output,
			Func<TIn, CancellationToken, ValueTask<TOut>> transform,
			IRetryPolicy retryPolicy,
			Func<Exception, bool> isTransientException,
			ChannelWriter<Envelope<TIn>>? errorOutput = null,
			bool forwardFailedToMainOutput = true,
			string errorCode = "TRANSFORM_ERROR",
			Action<string>? log = null,
			IPipelineFatalErrorReporter? fatalErrorReporter = null)
			: base(name, input, output, log, fatalErrorReporter)
		{
			this.transform = transform;
			this.retryPolicy = retryPolicy;
			this.isTransientException = isTransientException;
			this.errorOutput = errorOutput;
			this.forwardFailedToMainOutput = forwardFailedToMainOutput;
			this.errorCode = errorCode;
		}

		protected override async ValueTask HandleItemAsync(Envelope<TIn> item, CancellationToken cancellationToken)
		{
			item.Context.AddBreadcrumb(Name);

			if (item.Status != MessageStatus.Ok)
			{
				await WriteAsync(item.MapPayload(default(TOut)!), cancellationToken).ConfigureAwait(false);
				return;
			}

			while (true)
			{
				try
				{
					var payload = await transform(item.Payload, cancellationToken).ConfigureAwait(false);
					item.MarkOk();
					await WriteAsync(item.MapPayload(payload), cancellationToken).ConfigureAwait(false);
					return;
				}
				catch (Exception ex)
				{
					var isTransient = isTransientException(ex);
					var error = new PipelineError
					{
						Category = PipelineErrorCategory.Transform,
						Code = errorCode,
						Message = "Transform failed.",
						ExceptionType = ex.GetType().FullName,
						ExceptionMessage = ex.Message,
						StackTrace = ex.StackTrace,
						IsTransient = isTransient,
						OccurredAtUtc = DateTimeOffset.UtcNow,
						NodeName = Name,
						Details = new Dictionary<string, string>(),
					};

					if (isTransient && retryPolicy.ShouldRetry(item, error))
					{
						item.MarkRetrying(error);
						item.Attempt++;

						var delay = retryPolicy.GetDelay(item.Attempt);
						if (delay > TimeSpan.Zero)
						{
							await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
						}

						continue;
					}

					item.MarkFailed(error);

					if (errorOutput is not null)
					{
						await errorOutput.WriteAsync(item, cancellationToken).ConfigureAwait(false);
						Metrics.RecordOut(item);
					}

					if (forwardFailedToMainOutput)
					{
						await WriteAsync(item.MapPayload(default(TOut)!), cancellationToken).ConfigureAwait(false);
					}

					return;
				}
			}
		}

		protected override void CompleteOutputs(Exception? error = null)
		{
			base.CompleteOutputs(error);
			errorOutput?.TryComplete(error);
		}
	}
}
