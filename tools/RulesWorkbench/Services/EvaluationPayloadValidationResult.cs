namespace RulesWorkbench.Services
{
    public sealed record EvaluationPayloadValidationResult(bool IsValid, IReadOnlyList<string> Errors)
    {
        public static EvaluationPayloadValidationResult Success() => new(true, Array.Empty<string>());

        public static EvaluationPayloadValidationResult Failed(params string[] errors) => new(false, errors);
    }
}
