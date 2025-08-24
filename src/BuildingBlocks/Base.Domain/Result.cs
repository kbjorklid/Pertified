namespace Base.Domain;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
public readonly struct Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message cannot be null or empty.", nameof(error));
        }

        return new(false, error);
    }

    public static implicit operator Result(bool success) => success ? Success() : Failure("Operation failed");
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value)
    {
        if (value is null && !typeof(T).IsClass && Nullable.GetUnderlyingType(typeof(T)) == null)
        {
            throw new ArgumentException("Value cannot be null for non-nullable value types.", nameof(value));
        }

        return new(true, value, null);
    }
    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message cannot be null or empty.", nameof(error));
        }

        return new(false, default, error);
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Result result)
    {
        if (result.IsSuccess)
        {
            return new(true, default, null);
        }

        return Failure(result.Error ?? "Unknown error");
    }
}
