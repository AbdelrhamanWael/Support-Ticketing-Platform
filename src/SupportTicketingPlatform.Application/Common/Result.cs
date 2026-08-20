namespace SupportTicketingPlatform.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }
        public ErrorType Type { get; }

        private Result(bool isSuccess, T? value, string? error, ErrorType type)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            Type = type;
        }

        public static Result<T> Success(T value) => new(true, value, null, ErrorType.None);
        public static Result<T> Failure(string error, ErrorType type) => new(false, default, error, type);
    }
}
