namespace Library;

/// <summary>
/// Represents a value that may or may not be present.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Maybe<T>
{
    /// <summary>
    /// Gets the value if present.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets a value indicating whether the value is present.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets a value indicating whether the value is absent.
    /// </summary>
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

    /// <summary>
    /// Creates a Maybe with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A Maybe containing the value.</returns>
    public static Maybe<T> Some(T value) => new(value);

    /// <summary>
    /// Creates a Maybe with no value.
    /// </summary>
    /// <returns>A Maybe with no value.</returns>
    public static Maybe<T> None() => new();

    /// <summary>
    /// Creates a Maybe from a nullable value.
    /// </summary>
    /// <param name="value">The nullable value.</param>
    /// <returns>A Maybe containing the value if not null, otherwise None.</returns>
    public static Maybe<T> From(T? value)
    {
        if (value is null)
            return None();
        return Some(value);
    }

    /// <summary>
    /// Projects the value into a new form if present.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>A Maybe containing the projected value if present, otherwise None.</returns>
    public Maybe<TResult> Select<TResult>(Func<T, TResult> selector) => HasValue ? Maybe<TResult>.Some(selector(Value)) : Maybe<TResult>.None();

    /// <summary>
    /// Binds the value to a new Maybe if present.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The result of the selector if present, otherwise None.</returns>
    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> selector) => HasValue ? selector(Value) : Maybe<TResult>.None();

    /// <summary>
    /// Filters the value based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns>This Maybe if the value satisfies the predicate and is present, otherwise None.</returns>
    public Maybe<T> Where(Func<T, bool> predicate) => HasValue && predicate(Value) ? this : None();
}