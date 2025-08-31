namespace Library;

public class Result
{
    public bool IsSuccess { get; protected set; }

    public bool IsFailure => !IsSuccess;

    public Dictionary<string, string> Errors { get; protected set; } = [];

    public static Result Ok() => new() { IsSuccess = true };

    public static Result<T> Ok<T>(T value) => new(value);

    public static Result Fail(string field, string error) => new() { IsSuccess = false, Errors = { [field] = error } };

    public static Result Fail(Dictionary<string, string> errors) => new() { IsSuccess = false, Errors = new(errors) };

    public static Result Fail(string error) => new() { IsSuccess = false, Errors = { ["Error"] = error } };

    public static Result<T> Fail<T>(Dictionary<string, string> errors) => new(errors);

    public static Result<T> Fail<T>(string field, string error) => new(new Dictionary<string, string> { [field] = error });

    public static Result<T> Fail<T>(string error) => new(new Dictionary<string, string> { ["Error"] = error });

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

public class Result<T> : Result
{
    public T Value { get; private set; } = default!;

    public Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    public Result(Dictionary<string, string> errors)
    {
        IsSuccess = false;
        Errors = new(errors);
    }
}