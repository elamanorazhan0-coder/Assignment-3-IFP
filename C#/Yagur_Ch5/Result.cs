namespace Yagur.Ch5;

// Result<T, E> - a value that is EITHER a success (T) OR an error
// Built from the chapter's "Implementing the Result type" section
public class Result<T, E>
{
    private readonly T? _value;
    private readonly E? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T? value, E? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot fetch Value from a failed result.");

    public E Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot fetch Error from a successful result.");

    public static Result<T, E> Ok(T value) => new(value, default, true);
    public static Result<T, E> Fail(E error) => new(default, error, false);

    // Bind: the "railway switch" of Railway-Oriented Programming.
    // Success -> run the next step. Failure -> skip it and carry the error along.
    public Result<TOut, E> Bind<TOut>(Func<T, Result<TOut, E>> next) =>
        IsSuccess ? next(Value) : Result<TOut, E>.Fail(Error);
}

// Factory helpers so callers can write Result.Ok<bool, MyError>(true),
// exactly as in the book's solutions.
public static class Result
{
    public static Result<T, E> Ok<T, E>(T value) => Result<T, E>.Ok(value);
    public static Result<T, E> Success<T, E>(T value) => Result<T, E>.Ok(value); // alias used in the text
    public static Result<T, E> Fail<T, E>(E error) => Result<T, E>.Fail(error);
}
