using RulesWorkbench.Contracts;

namespace RulesWorkbench.Services
{
    public sealed record BatchPayloadLoadResult(bool Found, EvaluationPayloadDto? Payload, string? Error)
    {
        public static BatchPayloadLoadResult NotFound(string batchId) => new(false, null, $"Batch '{batchId}' not found.");

        public static BatchPayloadLoadResult Failed(string message) => new(false, null, message);

        public static BatchPayloadLoadResult Success(EvaluationPayloadDto payload) => new(true, payload, null);
    }
}
