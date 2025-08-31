namespace Library;

public class Maybe<T>
{
    public T Value { get; }

    public bool HasValue { get; }

    public bool IsNone => !HasValue;

    private Maybe(T value)
    {
        Value = value;
        HasValue = true;
    }

    private Maybe()
    {
        Value = default!;
        HasValue = false;
    }

    public static Maybe<T> Some(T value) => new(value);

    public static Maybe<T> None() => new();

    public static Maybe<T> From(T? value)
    {
        if (value is null)
            return None();
        return Some(value);
    }

    public Maybe<TResult> Select<TResult>(Func<T, TResult> selector) => HasValue ? Maybe<TResult>.Some(selector(Value)) : Maybe<TResult>.None();

    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> selector) => HasValue ? selector(Value) : Maybe<TResult>.None();

    public Maybe<T> Where(Func<T, bool> predicate) => HasValue && predicate(Value) ? this : None();
}