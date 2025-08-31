using Library;
using Xunit;

namespace Library.Tests;

public class MaybeTests
{
    [Fact]
    public void Some_CreatesMaybeWithValue()
    {
        var maybe = Maybe<int>.Some(42);

        Assert.True(maybe.HasValue);
        Assert.False(maybe.IsNone);
        Assert.Equal(42, maybe.Value);
    }

    [Fact]
    public void None_CreatesMaybeWithoutValue()
    {
        var maybe = Maybe<int>.None();

        Assert.False(maybe.HasValue);
        Assert.True(maybe.IsNone);
    }

    [Fact]
    public void From_ReturnsSome_WhenValueNotNull()
    {
        var maybe = Maybe<string>.From("test");

        Assert.True(maybe.HasValue);
        Assert.Equal("test", maybe.Value);
    }

    [Fact]
    public void From_ReturnsNone_WhenValueNull()
    {
        var maybe = Maybe<string>.From(null);

        Assert.True(maybe.IsNone);
    }

    [Fact]
    public void Select_TransformsValue_WhenHasValue()
    {
        var maybe = Maybe<int>.Some(42);
        var result = maybe.Select(x => x.ToString());

        Assert.True(result.HasValue);
        Assert.Equal("42", result.Value);
    }

    [Fact]
    public void Select_ReturnsNone_WhenNone()
    {
        var maybe = Maybe<int>.None();
        var result = maybe.Select(x => x.ToString());

        Assert.True(result.IsNone);
    }

    [Fact]
    public void Bind_AppliesFunction_WhenHasValue()
    {
        var maybe = Maybe<int>.Some(42);
        var result = maybe.Bind(x => Maybe<string>.Some(x.ToString()));

        Assert.True(result.HasValue);
        Assert.Equal("42", result.Value);
    }

    [Fact]
    public void Bind_ReturnsNone_WhenNone()
    {
        var maybe = Maybe<int>.None();
        var result = maybe.Bind(x => Maybe<string>.Some(x.ToString()));

        Assert.True(result.IsNone);
    }

    [Fact]
    public void Where_ReturnsValue_WhenPredicateTrue()
    {
        var maybe = Maybe<int>.Some(42);
        var result = maybe.Where(x => x > 40);

        Assert.True(result.HasValue);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Where_ReturnsNone_WhenPredicateFalse()
    {
        var maybe = Maybe<int>.Some(42);
        var result = maybe.Where(x => x < 40);

        Assert.True(result.IsNone);
    }

    [Fact]
    public void Where_ReturnsNone_WhenNone()
    {
        var maybe = Maybe<int>.None();
        var result = maybe.Where(x => true);

        Assert.True(result.IsNone);
    }
}