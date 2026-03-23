using Microsoft.Extensions.DependencyInjection;
using UKHO.Search.Studio.Injection;

namespace UKHO.Search.Studio.Providers.FileShare.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddFileShareStudioProvider(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IFileShareStudioBatchPayloadStore, FileShareStudioBatchPayloadStore>();
            services.AddSingleton<IFileShareStudioQueueWriter, FileShareStudioQueueWriter>();
            services.AddStudioProvider<FileShareStudioProviderRegistrationMarker, FileShareStudioProvider>();

            return services;
        }
    }
}
