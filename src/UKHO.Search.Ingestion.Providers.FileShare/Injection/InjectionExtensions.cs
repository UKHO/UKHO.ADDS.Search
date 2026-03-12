using Microsoft.Extensions.DependencyInjection;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers;

namespace UKHO.Search.Ingestion.Providers.FileShare.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddFileShareProvider(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<IFileShareZipDownloader, FileShareZipDownloader>();
            services.AddScoped<IBatchContentHandler, S57BatchContentHandler>();
            services.AddScoped<IBatchContentHandler, S100BatchContentHandler>();
            services.AddScoped<IBatchContentHandler>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var raw = configuration["ingestion:fileContentExtractionAllowedExtensions"];
                var allowedExtensions = string.IsNullOrWhiteSpace(raw)
                    ? Array.Empty<string>()
                    : raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                return new TextExtractionBatchContentHandler(allowedExtensions, sp.GetRequiredService<ILogger<TextExtractionBatchContentHandler>>());
            });
            services.AddScoped<IIngestionEnricher, BasicEnricher>();
            services.AddScoped<IIngestionEnricher, BatchContentEnricher>();
            services.AddScoped<IIngestionEnricher, ExchangeSetEnricher>();
            services.AddScoped<IIngestionEnricher, GeoLocationEnricher>();

            return services;
        }
    }
}