using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class SpawnConflictTests
{
    [Theory]
    [InlineData(ThingCategory.Building, true)]
    [InlineData(ThingCategory.Item, true)]
    [InlineData(ThingCategory.Plant, true)]
    [InlineData(ThingCategory.Filth, true)]
    [InlineData(ThingCategory.Pawn, false)]
    public void Replaces_AThingOfThisCategory_OnlyWhenItWouldBeInTheWay(ThingCategory category, bool replaced)
    {
        var existing = new ThingDef { defName = "ArchoTest_Existing", category = category };

        Assert.Equal(replaced, SpawnConflict.Replaces(existing));
    }

    [Fact]
    public void Replaces_APowerConduit_IsFalse()
    {
        var conduit = new ThingDef { defName = "ArchoTest_Conduit", category = ThingCategory.Building, building = new BuildingProperties { isPowerConduit = true } };

        Assert.False(SpawnConflict.Replaces(conduit));
    }
}
