using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.DeadLetter;
using UKHO.Search.Infrastructure.Ingestion.Diagnostics;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Terminal;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    public sealed class FileShareIngestionPipelineAdapter
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly IBulkIndexClient<IndexOperation> bulkIndexClient;
        private readonly IConfiguration configuration;
        private readonly ILoggerFactory loggerFactory;
        private readonly IIngestionProviderService providerService;
        private readonly IQueueClientFactory queueClientFactory;

        public FileShareIngestionPipelineAdapter(IConfiguration configuration, ILoggerFactory loggerFactory, IIngestionProviderService providerService, IQueueClientFactory queueClientFactory, IBulkIndexClient<IndexOperation> bulkIndexClient, BlobServiceClient blobServiceClient)
        {
            this.configuration = configuration;
            this.loggerFactory = loggerFactory;
            this.providerService = providerService;
            this.queueClientFactory = queueClientFactory;
            this.bulkIndexClient = bulkIndexClient;
            this.blobServiceClient = blobServiceClient;
        }

        public FileShareIngestionGraphHandle BuildAzureQueueBacked(CancellationToken cancellationToken)
        {
            var indexRetryMaxAttempts = configuration.GetValue<int>("ingestion:indexRetryMaxAttempts");
            var indexRetryBaseDelayMs = configuration.GetValue<int>("ingestion:indexRetryBaseDelayMilliseconds");
            var indexRetryMaxDelayMs = configuration.GetValue<int>("ingestion:indexRetryMaxDelayMilliseconds");
            var indexRetryJitterMs = configuration.GetValue<int>("ingestion:indexRetryJitterMilliseconds");

            var factories = new FileShareIngestionGraphFactories
            {
                CreateSourceNode = (name, output, supervisor) => new IngestionSourceNode(name, output, configuration, providerService, queueClientFactory, loggerFactory.CreateLogger(name), supervisor),

                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlobDeadLetterSinkNode<IngestionRequest>(name, input, blobServiceClient, configuration, configuration.GetValue("ingestion:deadletterFatalIfCannotPersist", true), logger: loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor),

                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => new BlobDeadLetterSinkNode<IndexOperation>(name, input, blobServiceClient, configuration, configuration.GetValue("ingestion:deadletterFatalIfCannotPersist", true), logger: loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor),

                CreateDiagnosticsSinkNode = (name, input, supervisor) => new DiagnosticsSinkNode<IndexOperation>(name, input, loggerFactory.CreateLogger(name), supervisor),

                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => new InOrderBulkIndexNode(name, input, bulkIndexClient, successOutput, deadLetterOutput, indexRetryMaxAttempts, TimeSpan.FromMilliseconds(indexRetryBaseDelayMs), TimeSpan.FromMilliseconds(indexRetryMaxDelayMs), TimeSpan.FromMilliseconds(indexRetryJitterMs),
                    logger: loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor),

                CreateAckNode = (name, lane, input, supervisor) => new AckSinkNode<IndexOperation>(name, input, loggerFactory.CreateLogger(name), supervisor)
            };

            return FileShareIngestionGraph.BuildAzureQueueBacked(new FileShareIngestionGraphDependencies
            {
                Configuration = configuration,
                LoggerFactory = loggerFactory,
                Factories = factories
            }, cancellationToken);
        }
    }
}