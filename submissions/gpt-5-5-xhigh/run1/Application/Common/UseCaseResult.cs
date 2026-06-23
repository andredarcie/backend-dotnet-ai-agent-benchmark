namespace CreditCardApi.Application.Common;

public enum UseCaseResultType
{
    Success,
    NotFound,
    ValidationError
}

public sealed record UseCaseResult<T>(
    UseCaseResultType Type,
    T? Value = default,
    IReadOnlyList<string>? Errors = null)
{
    public static UseCaseResult<T> Success(T value) => new(UseCaseResultType.Success, value);
    public static UseCaseResult<T> NotFound() => new(UseCaseResultType.NotFound);
    public static UseCaseResult<T> Invalid(params string[] errors) =>
        new(UseCaseResultType.ValidationError, default, errors);
}
