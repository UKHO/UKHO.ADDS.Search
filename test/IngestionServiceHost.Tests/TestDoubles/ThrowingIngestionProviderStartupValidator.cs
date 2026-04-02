using UKHO.Search.Services.Ingestion.Providers;

namespace IngestionServiceHost.Tests.TestDoubles
{
    internal sealed class ThrowingIngestionProviderStartupValidator : IIngestionProviderStartupValidator
    {
        private readonly Exception _exception;

        public ThrowingIngestionProviderStartupValidator(Exception exception)
        {
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public void Validate()
        {
            CallCount++;
            throw _exception;
        }
    }
}
