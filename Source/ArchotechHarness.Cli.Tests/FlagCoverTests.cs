using ArchotechHarness.Shared;

namespace ArchotechHarness.Cli.Tests;

public class FlagCoverTests
{
    private const long Violent = 0b0001;
    private const long Animals = 0b0010;
    private const long Cleaning = 0b0100;

    private sealed record Childhood(string Name, long Disables);

    private static readonly Childhood[] Childhoods =
    [
        new("ViolentOnly", Violent),
        new("ViolentAndCleaning", Violent | Animals | Cleaning),
        new("ViolentAndAnimals", Violent | Animals),
    ];

    [Fact]
    public void FirstCovering_SeveralRequiredTags_PicksOneCandidateCoveringAllOfThem()
    {
        var chosen = FlagCover.FirstCovering(Childhoods, c => c.Disables, required: Violent | Animals, forbidden: Cleaning);

        Assert.Equal("ViolentAndAnimals", chosen!.Name);
    }

    [Fact]
    public void FirstCovering_NoCandidateAvoidsTheForbiddenTags_ReturnsNull()
    {
        var chosen = FlagCover.FirstCovering(Childhoods, c => c.Disables, required: Cleaning, forbidden: Violent);

        Assert.Null(chosen);
    }
}
