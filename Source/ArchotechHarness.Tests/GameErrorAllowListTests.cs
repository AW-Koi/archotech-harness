using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class GameErrorAllowListTests
{
    private const string DrawingJanitor = "Exception drawing Janitor: System.NullReferenceException";

    [Fact]
    public void Allows_WithoutAttributes_RejectsAnError()
    {
        Assert.False(GameErrorAllowList.From([]).Allows(DrawingJanitor));
    }

    [Fact]
    public void Allows_WithABareAttribute_AcceptsAnyError()
    {
        var allowList = GameErrorAllowList.From([new AllowGameErrorsAttribute()]);

        Assert.True(allowList.Allows(DrawingJanitor));
    }

    [Theory]
    [InlineData("drawing Janitor", true)]
    [InlineData("NullReferenceException", true)]
    [InlineData("ticking Janitor", false)]
    [InlineData("drawing janitor", false)]
    public void Allows_WithASubstring_AcceptsOnlyErrorsContainingIt(string containing, bool allowed)
    {
        var allowList = GameErrorAllowList.From([new AllowGameErrorsAttribute(containing)]);

        Assert.Equal(allowed, allowList.Allows(DrawingJanitor));
    }

    [Fact]
    public void Allows_WithSeveralSubstrings_AcceptsAnErrorMatchingAnyOfThem()
    {
        var allowList = GameErrorAllowList.From([new AllowGameErrorsAttribute("ticking Mop"), new AllowGameErrorsAttribute("drawing Janitor")]);

        Assert.True(allowList.Allows(DrawingJanitor));
    }
}
