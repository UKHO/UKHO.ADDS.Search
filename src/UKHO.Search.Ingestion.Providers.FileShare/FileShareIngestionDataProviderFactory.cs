using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;

namespace UKHO.Search.Ingestion.Providers.FileShare
{
    public sealed class FileShareIngestionDataProviderFactory : IIngestionDataProviderFactory
    {
        private readonly int _ingressCapacity;
        private readonly ILoggerFactory _loggerFactory;
        private readonly FileShareIngestionProcessingGraphDependencies? _processingGraphDependencies;

        public FileShareIngestionDataProviderFactory(string queueName) : this(queueName, NullLoggerFactory.Instance)
        {
        }

        public FileShareIngestionDataProviderFactory(string queueName, ILoggerFactory loggerFactory, int ingressCapacity = 256)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

            if (ingressCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ingressCapacity), "Ingress channel capacity must be > 0.");
            }

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _ingressCapacity = ingressCapacity;
            _processingGraphDependencies = null;

            QueueName = queueName;
        }

        public FileShareIngestionDataProviderFactory(string queueName, FileShareIngestionProcessingGraphDependencies? processingGraphDependencies, ILoggerFactory loggerFactory, int ingressCapacity = 256)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

            if (ingressCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ingressCapacity), "Ingress channel capacity must be > 0.");
            }

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _ingressCapacity = ingressCapacity;
            _processingGraphDependencies = processingGraphDependencies;

            QueueName = queueName;
        }

        public string Name => "file-share";

        public string QueueName { get; }

        public IIngestionDataProvider CreateProvider()
        {
            var logger = _loggerFactory.CreateLogger<FileShareIngestionDataProvider>();

            return _processingGraphDependencies is null ? new FileShareIngestionDataProvider(_ingressCapacity, logger) : new FileShareIngestionDataProvider(_processingGraphDependencies, _ingressCapacity, logger);
        }
    }
}