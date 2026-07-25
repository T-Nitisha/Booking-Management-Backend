namespace BookingManagement.Application.Common;

/// <summary>
/// Lightweight result wrapper so expected failure paths (slot already booked,
/// concurrency conflict, not found) don't rely on exceptions for control flow.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public BookingErrorCode? ErrorCode { get; }

    private Result(bool isSuccess, T? value, string? error, BookingErrorCode? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string error, BookingErrorCode errorCode) => new(false, default, error, errorCode);
}

public enum BookingErrorCode
{
    NotFound,
    SlotAlreadyBooked,
    ConcurrencyConflict,
    LockTimeout,
    Forbidden
}
