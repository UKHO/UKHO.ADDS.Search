namespace UKHO.Search.Ingestion.Providers.FileShare;

public class FileShareIngestionProvider : IIngestionDataProvider
{
    public string Name => "File Share";

    public string QueueName => "file-share-queue";
}