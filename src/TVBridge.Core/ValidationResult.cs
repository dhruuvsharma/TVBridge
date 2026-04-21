namespace TVBridge.Core;

public sealed record ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(params string[] errors) => new()
    {
        Errors = [.. errors]
    };
}
