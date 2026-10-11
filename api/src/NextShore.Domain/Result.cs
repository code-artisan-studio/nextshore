namespace NextShore.Domain;

public enum ErrorKind
{
    Validation,
    NotFound,

    /// <summary>The input was valid but could not be processed (422).</summary>
    Unprocessable,

    /// <summary>A dependency such as the AI provider failed (502).</summary>
    Upstream,
}

/// <param name="Details">Validation errors by field path, e.g. <c>english.scores.listening</c>.</param>
public sealed record Error(ErrorKind Kind, string Message, IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);

    public static Error Unprocessable(string message) => new(ErrorKind.Unprocessable, message);

    public static Error Upstream(string message) => new(ErrorKind.Upstream, message);

    public static Error Validation(IReadOnlyDictionary<string, string[]> details) =>
        new(ErrorKind.Validation, "One or more fields are invalid.", details);
}

/// <summary>Outcome of an operation that can fail in an expected way (CLAUDE.md conventions).</summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, Error? error)
    {
        _value = value;
        Error = error;
    }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Result failed: {Error!.Message}");

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
