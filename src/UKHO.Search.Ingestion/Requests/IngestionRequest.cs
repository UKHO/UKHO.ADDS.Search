using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record IngestionRequest
    {
        [JsonConstructor]
        public IngestionRequest(IngestionRequestType requestType, IndexRequest? indexItem, DeleteItemRequest? deleteItem, UpdateAclRequest? updateAcl)
        {
            RequestType = requestType;
            IndexItem = indexItem;
            DeleteItem = deleteItem;
            UpdateAcl = updateAcl;

            ValidateOneOf(RequestType, IndexItem, DeleteItem, UpdateAcl);
        }

        public IngestionRequest()
        {
        }

        [JsonPropertyName("RequestType")]
        public IngestionRequestType RequestType { get; init; }

        [JsonPropertyName("IndexItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IndexRequest? IndexItem { get; init; }

        [JsonPropertyName("DeleteItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DeleteItemRequest? DeleteItem { get; init; }

        [JsonPropertyName("UpdateAcl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UpdateAclRequest? UpdateAcl { get; init; }

        private static void ValidateOneOf(IngestionRequestType requestType, IndexRequest? indexItem, DeleteItemRequest? deleteItem, UpdateAclRequest? updateAcl)
        {
            var setCount = 0;
            if (indexItem is not null)
            {
                setCount++;
            }

            if (deleteItem is not null)
            {
                setCount++;
            }

            if (updateAcl is not null)
            {
                setCount++;
            }

            if (setCount != 1)
            {
                throw new JsonException("IngestionRequest must contain exactly one of IndexItem, DeleteItem, UpdateAcl.");
            }

            var matches = requestType switch
            {
                IngestionRequestType.IndexItem => indexItem is not null,
                IngestionRequestType.DeleteItem => deleteItem is not null,
                IngestionRequestType.UpdateAcl => updateAcl is not null,
                var _ => throw new JsonException($"Unsupported IngestionRequestType '{requestType}'.")
            };

            if (!matches)
            {
                throw new JsonException($"IngestionRequest.RequestType is '{requestType}' but the corresponding payload property is missing.");
            }
        }
    }
}