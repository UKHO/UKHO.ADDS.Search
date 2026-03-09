using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UKHO.Search.Infrastructure.Ingestion.Bootstrap;
using UKHO.Search.Infrastructure.Ingestion.Statistics;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Providers.FileShare;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddIngestionServices(this IServiceCollection collection)
        {
            collection.AddSingleton<IIngestionDataProviderFactory>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var queueName = configuration["ingestion:filesharequeuename"] ?? "file-share-queue";

                return new FileShareIngestionDataProviderFactory(queueName);
            });

            collection.AddSingleton<IIngestionProviderService, IngestionProviderService>();
            collection.AddSingleton<IBootstrapService, BootstrapService>();
            collection.AddSingleton<IStatisticsService, StatisticsService>();

            return collection;
        }
    }
}