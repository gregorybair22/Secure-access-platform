namespace SecureAccess.Application.Common;

/// <summary>Lightweight operation result used by application services.</summary>
public class Result
{
    public bool Succeeded { get; protected set; }
    public string? Error { get; protected set; }

    public static Result Success() => new() { Succeeded = true };
    public static Result Fail(string error) => new() { Succeeded = false, Error = error };
}

/// <summary>Operation result carrying a value on success.</summary>
public class Result<T> : Result
{
    public T? Value { get; private set; }

    public static Result<T> Success(T value) => new() { Succeeded = true, Value = value };
    public new static Result<T> Fail(string error) => new() { Succeeded = false, Error = error };
}
