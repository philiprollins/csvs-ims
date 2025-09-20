namespace Library;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the errors if the operation failed.
    /// </summary>
    public Dictionary<string, string> Errors { get; protected set; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Ok() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with the value.</returns>
    public static Result<T> Ok<T>(T value) => new(value);

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="field">The field that caused the error.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Fail(string field, string error) => new() { IsSuccess = false, Errors = { [field] = error } };

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    /// <param name="errors">The errors.</param>
    /// <returns>A failed result.</returns>
    public static Result Fail(Dictionary<string, string> errors) => new() { IsSuccess = false, Errors = new(errors) };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Fail(string error) => new() { IsSuccess = false, Errors = { ["Error"] = error } };

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="errors">The errors.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Fail<T>(Dictionary<string, string> errors) => new(errors);

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="field">The field that caused the error.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Fail<T>(string field, string error) => new(new Dictionary<string, string> { [field] = error });

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Fail<T>(string error) => new(new Dictionary<string, string> { ["Error"] = error });

    /// <summary>
    /// Combines multiple results into a single result.
    /// </summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>A combined result.</returns>
    public static Result Combine(params Result[] results)
    {
        var errors = new Dictionary<string, string>();
        foreach (var result in results.Where(r => r.IsFailure))
        {
            foreach (var (key, value) in result.Errors)
            {
                if (errors.ContainsKey(key))
                {
                    errors[key] += "; " + value;
                }
                else
                {
                    errors[key] = value;
                }
            }
        }
        return errors.Count != 0 ? Fail(errors) : Ok();
    }
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T Value { get; private set; } = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    public Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class with errors.
    /// </summary>
    /// <param name="errors">The errors.</param>
    public Result(Dictionary<string, string> errors)
    {
        IsSuccess = false;
        Errors = new(errors);
    }
}