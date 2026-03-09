using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    public sealed class IngestionPipelineHostedService : IHostedService
    {
        private readonly FileShareIngestionPipelineAdapter adapter;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly ILogger<IngestionPipelineHostedService> logger;
        private FileShareIngestionGraphHandle? graph;
        private Task? runTask;

        public IngestionPipelineHostedService(FileShareIngestionPipelineAdapter adapter, IHostApplicationLifetime hostApplicationLifetime, ILogger<IngestionPipelineHostedService> logger)
        {
            this.adapter = adapter;
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            graph = adapter.BuildAzureQueueBacked(hostApplicationLifetime.ApplicationStopping);
            runTask = RunAsync();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (graph is null)
            {
                return;
            }

            await graph.Supervisor.StopAsync(cancellationToken)
                       .ConfigureAwait(false);

            if (runTask is not null)
            {
                await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken))
                          .ConfigureAwait(false);
            }
        }

        private async Task RunAsync()
        {
            if (graph is null)
            {
                return;
            }

            try
            {
                logger.LogInformation("Ingestion pipeline starting.");

                await graph.Supervisor.StartAsync()
                           .ConfigureAwait(false);

                await graph.Supervisor.Completion.ConfigureAwait(false);

                if (graph.Supervisor.FatalException is not null)
                {
                    logger.LogError(graph.Supervisor.FatalException, "Ingestion pipeline stopped due to fatal node failure. FatalNodeName={FatalNodeName}", graph.Supervisor.FatalNodeName);
                }
                else
                {
                    logger.LogInformation("Ingestion pipeline completed.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ingestion pipeline runner failed.");
                throw;
            }
        }
    }
}