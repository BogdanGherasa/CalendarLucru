namespace TimeBlocker.Web.Services;

public enum ServiceResultStatus
{
    Success,
    NotFound,
    Conflict,
    Invalid
}

public sealed class ServiceResult<T>
{
    private ServiceResult(ServiceResultStatus status, T? value, string? message, Dictionary<string, string[]>? errors)
    {
        Status = status;
        Value = value;
        Message = message;
        Errors = errors;
    }

    public ServiceResultStatus Status { get; }

    public T? Value { get; }

    public string? Message { get; }

    public Dictionary<string, string[]>? Errors { get; }

    public bool IsSuccess => Status == ServiceResultStatus.Success;

    public static ServiceResult<T> Success(T value) => new(ServiceResultStatus.Success, value, null, null);

    public static ServiceResult<T> NotFound() => new(ServiceResultStatus.NotFound, default, null, null);

    public static ServiceResult<T> Conflict(string message) => new(ServiceResultStatus.Conflict, default, message, null);

    public static ServiceResult<T> Invalid(Dictionary<string, string[]> errors) =>
        new(ServiceResultStatus.Invalid, default, null, errors);
}
