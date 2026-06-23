namespace CreditCardApi.Application;

/// <summary>
/// Outcome of a use-case execution. Controllers translate this into HTTP status codes
/// without knowing anything about the underlying persistence or validation rules.
/// </summary>
public enum ResultStatus
{
    Success,
    NotFound,
    Invalid
}

public class Result
{
    public ResultStatus Status { get; private init; }
    public string? Error { get; private init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Success() => new() { Status = ResultStatus.Success };
    public static Result NotFound(string? error = null) => new() { Status = ResultStatus.NotFound, Error = error };
    public static Result Invalid(string error) => new() { Status = ResultStatus.Invalid, Error = error };
}

public class Result<T>
{
    public ResultStatus Status { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result<T> Success(T value) => new() { Status = ResultStatus.Success, Value = value };
    public static Result<T> NotFound(string? error = null) => new() { Status = ResultStatus.NotFound, Error = error };
    public static Result<T> Invalid(string error) => new() { Status = ResultStatus.Invalid, Error = error };
}
