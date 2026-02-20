using Azure.Identity;
using FileShareImageBuilder.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Search.Configuration;

namespace FileShareImageBuilder;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddSqlServerClient(StorageNames.FileShareEmulatorDatabase);
        builder.AddAzureBlobServiceClient(ServiceNames.Blobs);

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<FileShareReadOnlyClientFactory>();

        builder.Services.AddSingleton<IAuthenticationTokenProvider>(_ =>
        {
            var scope = ConfigurationReader.GetRemoteServiceScope();

            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

            var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
                ClientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId,
            });

            return new TokenCredentialProvider(credential, new[] { $"{scope}/.default" });
        });

        builder.Services.AddSingleton<IFileShareReadOnlyClient>(sp =>
        {
            var baseAddress = ConfigurationReader.GetRemoteServiceBaseAddress();
            var tokenProvider = sp.GetRequiredService<IAuthenticationTokenProvider>();
            return sp.GetRequiredService<FileShareReadOnlyClientFactory>().CreateClient(baseAddress, tokenProvider);
        });

        builder.Services.AddSingleton<MetadataImporter>();
        builder.Services.AddSingleton<ContentImporter>();
        builder.Services.AddSingleton<ImageBuilder>();

        using var host = builder.Build();

        await host.Services.GetRequiredService<ImageBuilder>()
            .RunAsync()
            .ConfigureAwait(false);
    }
}