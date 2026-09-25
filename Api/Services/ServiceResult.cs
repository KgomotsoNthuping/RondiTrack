namespace Api.Services;

public enum ServiceResult
{
    Success,
    BadRequest,
    NotFound,
    Conflict,
    UnprocessableEntity
}

public sealed record ServiceResult<T>(
    ServiceResult Status,
    T? Value = default,
    string? Detail = null)
{
    public bool IsSuccess =>
        Status == ServiceResult.Success;

    public static ServiceResult<T> Success(T value) =>
        new(ServiceResult.Success, value);

    public static ServiceResult<T> BadRequest(string detail) =>
        new(ServiceResult.BadRequest, default, detail);

    public static ServiceResult<T> NotFound(string detail) =>
        new(ServiceResult.NotFound, default, detail);

    public static ServiceResult<T> Conflict(string detail) =>
        new(ServiceResult.Conflict, default, detail);

    public static ServiceResult<T> Unprocessable(string detail) =>
        new(
            ServiceResult.UnprocessableEntity,
            default,
            detail);
}
