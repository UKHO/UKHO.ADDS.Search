namespace FileShareImageBuilder;

public sealed class ImageBuilder
{
    private readonly MetadataImporter _metadataImporter;
    private readonly ContentImporter _contentImporter;
    private readonly DataCleaner _dataCleaner;
    private readonly MetadataExporter _metadataExporter;
    private readonly ImageExporter _imageExporter;

    public ImageBuilder(
        MetadataImporter metadataImporter,
        ContentImporter contentImporter,
        DataCleaner dataCleaner,
        MetadataExporter metadataExporter,
        ImageExporter imageExporter)
    {
        _metadataImporter = metadataImporter;
        _contentImporter = contentImporter;
        _dataCleaner = dataCleaner;
        _metadataExporter = metadataExporter;
        _imageExporter = imageExporter;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        return RunInternalAsync(cancellationToken);
    }

    private async Task RunInternalAsync(CancellationToken cancellationToken)
    {
        await _metadataImporter.ImportAsync(cancellationToken).ConfigureAwait(false);
        await _contentImporter.ImportAsync(cancellationToken).ConfigureAwait(false);
        await _dataCleaner.DeleteInvalidBatchesAsync(cancellationToken).ConfigureAwait(false);
        await _metadataExporter.ExportAsync(cancellationToken).ConfigureAwait(false);
        await _imageExporter.ExportAsync(cancellationToken).ConfigureAwait(false);
    }
}
