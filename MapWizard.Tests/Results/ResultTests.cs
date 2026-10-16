using MapWizard.Tools.Results;

namespace MapWizard.Tests.Results;

public class ResultTests
{
    private static readonly Error TestError = new("test.error", "Something went wrong.");

    [Fact]
    public void Success_HasNoErrorAndExposesTheValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_HasTheErrorAndNoValue()
    {
        Result<int> result = TestError;

        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericFailure_CanBeReturnedAsNonGenericResult()
    {
        Result<string> typed = TestError;
        Result untyped = typed;

        Assert.True(untyped.IsFailure);
        Assert.Equal("test.error", untyped.Error.Code);
    }

    [Fact]
    public void NonGenericSuccess_IsSuccessful()
    {
        Assert.True(Result.Success().IsSuccess);
    }
}
