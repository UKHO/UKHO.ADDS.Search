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
                                   .AddDatabase("fss");

            var elasticSearch = builder.AddElasticsearch(ServiceNames.ElasticSearch)
                                       .WithLifetime(ContainerLifetime.Persistent)
                                       .WithDataVolume()
                                       .WaitFor(storage);

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

            builder.AddProject<UKHO_ADDS_Search_FileShareEmulator>(ServiceNames.FileShareEmulator)
                   .WithExternalHttpEndpoints()
                   .WithReference(storageQueue)
                   .WithReference(sqlServer)
                   .WaitFor(storageQueue)
                   .WaitFor(sqlServer);

            builder.AddExecutable(ServiceNames.FileShareBuilder, "cmd", "..", "/c", "start", "dotnet", "run", "--project", "FileShareImageBuilder/FileShareImageBuilder.csproj")
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