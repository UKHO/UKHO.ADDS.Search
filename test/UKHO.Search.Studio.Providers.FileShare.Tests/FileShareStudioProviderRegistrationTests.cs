using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using UKHO.Search.Studio.Providers;
using UKHO.Search.Studio.Providers.FileShare;
using UKHO.Search.Studio.Providers.FileShare.Injection;
using UKHO.Search.Studio.Providers.FileShare.Tests.TestDoubles;
using Xunit;

namespace UKHO.Search.Studio.Providers.FileShare.Tests
{
    public sealed class FileShareStudioProviderRegistrationTests
    {
        [Fact]
        public void AddFileShareStudioProvider_registers_studio_provider_and_validation_succeeds()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<StudioProviderRegistrationValidator>>(NullLogger<StudioProviderRegistrationValidator>.Instance);
            services.AddProviderDescriptor<FileShareProviderMetadataRegistrationMarker>(new ProviderDescriptor("file-share", "File Share"));

            services.AddFileShareStudioProvider();
            services.AddSingleton<IFileShareStudioBatchPayloadStore>(new StubBatchPayloadStore());
            services.AddSingleton<IFileShareStudioQueueWriter>(new StubQueueWriter());

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();
            var validator = provider.GetRequiredService<IStudioProviderRegistrationValidator>();

            catalog.GetProvider("file-share").ProviderName.ShouldBe("file-share");
            catalog.GetProvider("file-share").ShouldBeAssignableTo<IStudioIngestionProvider>();
            Should.NotThrow(() => validator.Validate());
        }

        [Fact]
        public void AddFileShareStudioProvider_is_idempotent()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<StudioProviderRegistrationValidator>>(NullLogger<StudioProviderRegistrationValidator>.Instance);
            services.AddProviderDescriptor<FileShareProviderMetadataRegistrationMarker>(new ProviderDescriptor("file-share", "File Share"));

            services.AddFileShareStudioProvider();
            services.AddFileShareStudioProvider();
            services.AddSingleton<IFileShareStudioBatchPayloadStore>(new StubBatchPayloadStore());
            services.AddSingleton<IFileShareStudioQueueWriter>(new StubQueueWriter());

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();

            catalog.GetAllProviders().Count.ShouldBe(1);
            services.Count(x => x.ServiceType == typeof(IStudioProvider) && x.ImplementationType == typeof(FileShareStudioProvider)).ShouldBe(1);
        }

        private sealed class StubBatchPayloadStore : IFileShareStudioBatchPayloadStore
        {
            public Task<IReadOnlyList<FileShareStudioBusinessUnit>> GetBusinessUnitsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<FileShareStudioBusinessUnit>>([]);
            }

            public Task<IReadOnlyList<Guid>> GetPendingBatchIdsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Guid>>([]);
            }

            public Task<IReadOnlyList<Guid>> GetPendingBatchIdsForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Guid>>([]);
            }

            public Task MarkBatchIndexedAsync(Guid batchId, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<int> ResetAllIndexingStatusAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }

            public Task<int> ResetIndexingStatusForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }

            public Task<FileShareStudioBatchPayloadSource?> TryGetPayloadSourceAsync(Guid batchId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<FileShareStudioBatchPayloadSource?>(null);
            }
        }

        private sealed class StubQueueWriter : IFileShareStudioQueueWriter
        {
            public Task SubmitAsync(string payloadJson, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
