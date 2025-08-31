using Library;
using Xunit;

namespace Library.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_CreatesSuccessfulResult()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void OkT_CreatesSuccessfulResultWithValue()
    {
        var result = Result<int>.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Fail_CreatesFailedResultWithError()
    {
        var result = Result.Fail("Test error");

        Assert.True(result.IsFailure);
        Assert.Equal("Test error", result.Errors["Error"]);
    }

    [Fact]
    public void Fail_WithField_CreatesFailedResult()
    {
        var result = Result.Fail("field", "Field error");

        Assert.True(result.IsFailure);
        Assert.Equal("Field error", result.Errors["field"]);
    }

    [Fact]
    public void FailT_CreatesFailedResultWithValue()
    {
        var result = Result<int>.Fail("Test error");

        Assert.True(result.IsFailure);
        Assert.Equal("Test error", result.Errors["Error"]);
    }

    [Fact]
    public void Combine_ReturnsOk_WhenAllSuccessful()
    {
        var results = new[] { Result.Ok(), Result.Ok() };
        var combined = Result.Combine(results);

        Assert.True(combined.IsSuccess);
    }

    [Fact]
    public void Combine_ReturnsFailure_WhenAnyFails()
    {
        var results = new[] { Result.Ok(), Result.Fail("field1", "Error1"), Result.Fail("field2", "Error2") };
        var combined = Result.Combine(results);

        Assert.True(combined.IsFailure);
        Assert.Equal(2, combined.Errors.Count);
        Assert.Equal("Error1", combined.Errors["field1"]);
        Assert.Equal("Error2", combined.Errors["field2"]);
    }

    [Fact]
    public void Combine_MergesErrors_WhenMultipleFailures()
    {
        var results = new[]
        {
            Result.Fail("field1", "Error1"),
            Result.Fail("field2", "Error2"),
            Result.Fail("field1", "Error3")
        };
        var combined = Result.Combine(results);

        Assert.True(combined.IsFailure);
        Assert.Equal("Error1; Error3", combined.Errors["field1"]);
        Assert.Equal("Error2", combined.Errors["field2"]);
    }
}