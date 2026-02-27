using Projects;
using UKHO.ADDS.Search.Configuration;

namespace UKHO.ADDS.Search.AppHost
{
    public class AppHost
    {
        public static async Task Main(string[] args)
        {
            var builder = DistributedApplication.CreateBuilder(args);

            var keyCloakUsernameParameter = builder.AddParameter("keycloak-username");
            var keyCloakPasswordParameter = builder.AddParameter("keycloak-password", true);

            var azureStoragePathParameter = builder.AddParameter("azure-storage");

            var emulatorDataImageParameter = builder.AddParameter("emulator-data-image");

            var environmentParameter = builder.AddParameter("environment");

            var emulatorPersistentParameter = builder.AddParameter("emulator-persistent");

            var azureStoragePathValue = await azureStoragePathParameter.Resource.GetValueAsync(CancellationToken.None);

            var keycloak = builder.AddKeycloak(ServiceNames.KeyCloak, 8080, keyCloakUsernameParameter, keyCloakPasswordParameter)
                                  .WithDataVolume()
                                  .WithRealmImport("./Realms")
                                  .WithLifetime(ContainerLifetime.Persistent);

            var storage = builder.AddAzureStorage(ServiceNames.Storage)
                                 .RunAsEmulator(e =>
                                 {
                                     e.WithDataBindMount(azureStoragePathValue);
                                 });

            var storageQueue = storage.AddQueues(ServiceNames.Queues);
            var storageTable = storage.AddTables(ServiceNames.Tables);
            var storageBlob = storage.AddBlobs(ServiceNames.Blobs);

            var sqlServer = builder.AddSqlServer(ServiceNames.SqlServer)
                                   .WithLifetime(ContainerLifetime.Persistent)
                                   .AddDatabase(StorageNames.FileShareEmulatorDatabase);

            var elasticSearch = builder.AddElasticsearch(ServiceNames.ElasticSearch)
                                       .WithLifetime(ContainerLifetime.Persistent)
                                       .WithDataVolume()
                                       .WaitFor(storage);

            var fileShareEmulatorDataImage = await emulatorDataImageParameter.Resource.GetValueAsync(CancellationToken.None) ?? string.Empty;

            var fileShareEmulatorDataVolumeName = $"{ServiceNames.FileShareEmulator}-data";

            // The file share emulator needs a set of seed files available at runtime.
            // Docker/Aspire cannot mount a Docker image filesystem directly as a volume, so we:
            // 1) create/mount a named volume (persistent across runs) and
            // 2) run a one-shot init container from the data image which copies the seed files into that volume.
            //
            // Subsequent runs are fast because the named volume is already populated and the init container becomes a no-op.
            var fileShareEmulatorDataSeeder = builder.AddContainer($"{ServiceNames.FileShareEmulator}-data-seeder", fileShareEmulatorDataImage)
                                                    .WithVolume(fileShareEmulatorDataVolumeName, "/seed")
                                                    .WithEntrypoint("/bin/sh")
                                                    .WithArgs(
                                                        "-c",
                                                        // If the volume is empty, copy the data image's seeded content into it and then write a
                                                        // sentinel file as the final step. The emulator's readiness endpoint depends on this file,
                                                        // which prevents reporting Ready if the copy is only partially complete.
                                                        "if [ -z \"$(ls -A /seed 2>/dev/null)\" ]; then echo '[data-seeder] Seeding volume...'; rm -f /seed/.seed.complete; cp -a /data/. /seed/; echo 'ok' > /seed/.seed.complete; else echo '[data-seeder] Volume already seeded.'; fi");

            // The emulator mounts the populated named volume at `/data` and waits for the init container to finish.
            // This guarantees the emulator sees the seeded files before it starts handling requests.
            var fileShareEmulator = builder.AddContainer(ServiceNames.FileShareEmulator, fileShareEmulatorDataImage)
                .WithDockerfile("../UKHO.ADDS.Search.FileShareEmulator", "Dockerfile")
                .WithBuildArg("BUILD_CONFIGURATION", "Debug")
                .WithEnvironment("environment", environmentParameter)
                .WithExternalHttpEndpoints()
                .WithReference(storageQueue)
                .WithReference(sqlServer)
                .WaitFor(storageQueue)
                .WaitFor(sqlServer)
                .WithVolume(fileShareEmulatorDataVolumeName, "/data");

            var emulatorPersistentValue = await emulatorPersistentParameter.Resource.GetValueAsync(CancellationToken.None);
            var emulatorPersistent = bool.TryParse(emulatorPersistentValue, out var parsed) && parsed;

            if (emulatorPersistent)
            {
                fileShareEmulator.WithLifetime(ContainerLifetime.Persistent);
            }

            builder.AddProject<UKHO_ADDS_Search_Ingestion>(ServiceNames.Ingestion)
                .WithExternalHttpEndpoints()
                .WithReference(storageQueue)
                .WithReference(storageTable)
                .WithReference(storageBlob)
                .WithReference(elasticSearch)
                .WaitFor(storageQueue)
                .WaitFor(storageTable)
                .WaitFor(storageBlob)
                .WaitFor(elasticSearch);

            builder.AddProject<UKHO_ADDS_Search_Query>(ServiceNames.Query)
                   .WithExternalHttpEndpoints()
                   .WithReference(keycloak)
                   .WithReference(storageQueue)
                   .WithReference(storageTable)
                   .WithReference(storageBlob)
                   .WithReference(elasticSearch)
                   .WaitFor(keycloak)
                   .WaitFor(storageQueue)
                   .WaitFor(storageTable)
                   .WaitFor(storageBlob)
                   .WaitFor(elasticSearch);

            builder.AddProject<FileShareImageBuilder>(ServiceNames.FileShareBuilder)
                   .WithEnvironment("environment", environmentParameter)
                   .WithReference(storageBlob)
                   .WithReference(sqlServer)
                   .WaitFor(storageBlob)
                   .WaitFor(sqlServer)
                   .WithExplicitStart();

            builder.Build()
                   .Run();
        }
    }
}