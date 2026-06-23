namespace CreditCardApi.UseCases
{
    public class UseCaseResult<T>
    {
        public bool Success { get; private set; }
        public bool NotFound { get; private set; }
        public T? Value { get; private set; }
        public string? ErrorMessage { get; private set; }

        public static UseCaseResult<T> Ok(T value) => new() { Success = true, Value = value };
        public static UseCaseResult<T> Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
        public static UseCaseResult<T> FailNotFound() => new() { Success = false, NotFound = true };
    }

    public class UseCaseResult
    {
        public bool Success { get; private set; }
        public bool NotFound { get; private set; }
        public string? ErrorMessage { get; private set; }

        public static UseCaseResult Ok() => new() { Success = true };
        public static UseCaseResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
        public static UseCaseResult FailNotFound() => new() { Success = false, NotFound = true };
    }
}
