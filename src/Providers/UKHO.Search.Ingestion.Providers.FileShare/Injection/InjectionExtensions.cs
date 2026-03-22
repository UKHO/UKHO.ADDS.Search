using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.ProviderModel.Injection;

namespace UKHO.Search.Ingestion.Providers.FileShare.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddFileShareProviderMetadata(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddProviderDescriptor<FileShareProviderMetadataRegistrationMarker>(FileShareProviderMetadata.Descriptor);

            return services;
        }

        public static IServiceCollection AddFileShareProviderRuntime(this IServiceCollection services, Func<IServiceProvider, IIngestionDataProviderFactory>? providerFactoryFactory = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddFileShareProviderMetadata();
            services.AddFileShareProviderServicesCore();

            if (services.Any(x => x.ServiceType == typeof(FileShareProviderFactoryRegistrationMarker)))
            {
                return services;
            }

            services.AddSingleton<FileShareProviderFactoryRegistrationMarker>();
            services.AddSingleton<IIngestionDataProviderFactory>(sp =>
            {
                if (providerFactoryFactory is not null)
                {
                    return providerFactoryFactory(sp);
                }

                var configuration = sp.GetService<IConfiguration>();
                var loggerFactory = sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
                var queueName = configuration?["ingestion:filesharequeuename"] ?? "file-share-queue";
                var ingressCapacity = configuration?.GetValue("ingestion:providerIngressCapacity", 256) ?? 256;

                return new FileShareIngestionDataProviderFactory(queueName, loggerFactory, ingressCapacity);
            });

            return services;
        }

        public static IServiceCollection AddFileShareProvider(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddFileShareProviderMetadata();
            services.AddFileShareProviderServicesCore();

            return services;
        }

        private static IServiceCollection AddFileShareProviderServicesCore(this IServiceCollection services)
        {
            if (services.Any(x => x.ServiceType == typeof(FileShareProviderServicesRegistrationMarker)))
            {
                return services;
            }

            services.AddSingleton<FileShareProviderServicesRegistrationMarker>();
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