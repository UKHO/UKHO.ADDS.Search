namespace FileShareImageBuilder;

public sealed class ImageBuilder
{
    private readonly MetadataImporter _metadataImporter;
    private readonly ContentImporter _contentImporter;

    public ImageBuilder(MetadataImporter metadataImporter, ContentImporter contentImporter)
    {
        _metadataImporter = metadataImporter;
        _contentImporter = contentImporter;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        return RunInternalAsync(cancellationToken);
    }

    private async Task RunInternalAsync(CancellationToken cancellationToken)
    {
        await _metadataImporter.ImportAsync(cancellationToken).ConfigureAwait(false);
        await _contentImporter.ImportAsync(cancellationToken).ConfigureAwait(false);
    }
}
