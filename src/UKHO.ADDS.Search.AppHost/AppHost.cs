using Projects;
using UKHO.ADDS.Search.Configuration;

namespace UKHO.ADDS.Search.AppHost
{
    public class AppHost
    {
        public static async Task Main(string[] args)
        {
            var builder = DistributedApplication.CreateBuilder(args);

            var keyCloakUsername = builder.AddParameter("keycloak-username");
            var keyCloakPassword = builder.AddParameter("keycloak-password", true);

            var azureStoragePath = builder.AddParameter("azure-storage");

            var emulatorDataImage = builder.AddParameter("emulator-data-image");

            var azureStoragePathValue = await azureStoragePath.Resource.GetValueAsync(CancellationToken.None);

            var keycloak = builder.AddKeycloak(ServiceNames.KeyCloak, 8080, keyCloakUsername, keyCloakPassword)
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

            var fileShareEmulatorDataImage = await emulatorDataImage.Resource.GetValueAsync(CancellationToken.None) ?? string.Empty;

            var fileShareEmulatorDataVolumeName = $"{ServiceNames.FileShareEmulator}-data";

            // The file share emulator needs a set of seed files available at runtime.
            // Docker/Aspire cannot mount a Docker image filesystem directly as a volume, so we:
            // 1) create/mount a named volume (persistent across runs) and
            // 2) run a one-shot init container from the data image which copies the seed files into that volume.
            //
            // Subsequent runs are fast because the named volume is already populated and the init container becomes a no-op.
            var fileShareEmulatorDataInit = builder.AddContainer($"{ServiceNames.FileShareEmulator}-data-init", fileShareEmulatorDataImage)
                                                  .WithVolume(fileShareEmulatorDataVolumeName, "/seed")
                                                  .WithEntrypoint("/bin/sh")
                                                  .WithArgs(
                                                      "-c",
                                                      // If the volume is empty, copy the data image's seeded content into it.
                                                      // - `ls -A /seed` checks for any existing files in the mounted named volume.
                                                      // - `cp -a /data/. /seed/` copies everything from the image's `/data` folder into the volume.
                                                      //   The `-a` flag preserves timestamps/permissions where possible.
                                                      "if [ -z \"$(ls -A /seed 2>/dev/null)\" ]; then echo '[data-init] Seeding volume...'; cp -a /data/. /seed/; else echo '[data-init] Volume already seeded.'; fi");

            // The emulator mounts the populated named volume at `/data` and waits for the init container to finish.
            // This guarantees the emulator sees the seeded files before it starts handling requests.
            var fileShareEmulator = builder.AddContainer(ServiceNames.FileShareEmulator, fileShareEmulatorDataImage)
                .WithDockerfile("../UKHO.ADDS.Search.FileShareEmulator", "Dockerfile")
                .WithExternalHttpEndpoints()
                .WithReference(storageQueue)
                .WithReference(sqlServer)
                .WaitFor(storageQueue)
                .WaitFor(sqlServer)
                .WaitFor(fileShareEmulatorDataInit)
                                          .WithVolume(fileShareEmulatorDataVolumeName, "/data");

            builder.AddProject<UKHO_ADDS_Search_Ingestion>(ServiceNames.Ingestion)
                   .WithExternalHttpEndpoints()
                   .WithReference(storageQueue)
                   .WithReference(storageTable)
                   .WithReference(storageBlob)
                   .WithReference(elasticSearch)
                   .WaitFor(storageQueue)
                   .WaitFor(storageTable)
                   .WaitFor(storageBlob)
                   .WaitFor(elasticSearch)
                   .WaitFor(fileShareEmulator);

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