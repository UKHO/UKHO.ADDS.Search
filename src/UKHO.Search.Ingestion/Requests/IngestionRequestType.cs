using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IngestionRequestType
    {
        IndexItem,
        DeleteItem,
        UpdateAcl
    }
}