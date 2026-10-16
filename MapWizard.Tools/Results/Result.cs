namespace MapWizard.Tools.Results;

/// <summary>
/// Outcome of an operation that can fail in an expected way (unsupported input, missing file, ...).
/// Unexpected failures (bugs) are still exceptions.
/// </summary>
public class Result
{
    private readonly Error? _error;

    protected Result(Error? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The error of a failed result. Accessing it on a successful result throws.
    /// </summary>
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// A <see cref="Result"/> that carries a value when it succeeded.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error) : base(error)
    {
        _value = value;
    }

    /// <summary>
    /// The value of a successful result. Accessing it on a failed result throws.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value: {Error.Message}");

    public static Result<T> Success(T value) => new(value, null);

    public new static Result<T> Failure(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<T>(Error error) => Failure(error);

    public static implicit operator Result<T>(T value) => Success(value);
}
