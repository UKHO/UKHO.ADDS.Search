using System.Text.Json;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
	public sealed class DeadLetterSchemaTests
	{
		[Fact]
		public async Task Dead_letter_jsonl_includes_envelope_and_error_fields()
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			var filePath = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid().ToString("N"), "deadletter.jsonl");

			var supervisor = new PipelineSupervisor(cts.Token);
			var input = BoundedChannelFactory.Create<Envelope<int>>(capacity: 4, singleReader: true, singleWriter: true);

			var node = new DeadLetterSinkNode<int>(
				"dead-letter",
				input.Reader,
				filePath,
				fatalIfCannotPersist: true,
				fatalErrorReporter: supervisor);

			supervisor.AddNode(node);
			await supervisor.StartAsync();

			var env = new Envelope<int>("key-0", 123);
			env.MarkDropped("dropped", "test");
			await input.Writer.WriteAsync(env, cts.Token);
			input.Writer.TryComplete();

			await supervisor.Completion.WaitAsync(cts.Token);

			var line = (await File.ReadAllLinesAsync(filePath, cts.Token)).Single();
			using var json = JsonDocument.Parse(line);

			json.RootElement.TryGetProperty("Envelope", out var envelope).ShouldBeTrue();
			envelope.TryGetProperty("Key", out _).ShouldBeTrue();
			envelope.TryGetProperty("MessageId", out _).ShouldBeTrue();
			envelope.TryGetProperty("Attempt", out _).ShouldBeTrue();
			envelope.TryGetProperty("Status", out _).ShouldBeTrue();
			envelope.TryGetProperty("Error", out var error).ShouldBeTrue();
			error.TryGetProperty("Code", out _).ShouldBeTrue();

			json.RootElement.TryGetProperty("DeadLetteredAtUtc", out _).ShouldBeTrue();
			json.RootElement.TryGetProperty("NodeName", out var nodeName).ShouldBeTrue();
			nodeName.GetString().ShouldBe("dead-letter");
		}
	}
}
