using UKHO.Search.ProviderModel;

namespace UKHO.Search.Ingestion.Providers.FileShare
{
    public static class FileShareProviderMetadata
    {
        public static ProviderDescriptor Descriptor { get; } = new(
            FileShareIngestionDataProviderFactory.ProviderName,
            "File Share",
            "Ingests content sourced from File Share.");
    }
}
