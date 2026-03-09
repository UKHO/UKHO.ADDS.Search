using System.Text.Json;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Requests.Serialization;

namespace UKHO.Search.Ingestion.Providers.FileShare
{
    public sealed class FileShareIngestionDataProvider : IIngestionDataProvider
    {
        private static readonly JsonSerializerOptions SerializerOptions = IngestionJsonSerializerOptions.Create();

        public string Name => "file-share";

        public ValueTask<IngestionRequest> DeserializeIngestionRequestAsync(string messageText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                throw new JsonException("Queue message body is required.");
            }

            var request = JsonSerializer.Deserialize<IngestionRequest>(messageText, SerializerOptions);
            if (request is null)
            {
                throw new JsonException("Queue message could not be deserialized to IngestionRequest.");
            }

            return ValueTask.FromResult(request);
        }
    }
}