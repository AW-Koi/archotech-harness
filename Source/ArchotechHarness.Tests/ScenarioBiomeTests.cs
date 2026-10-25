using ArchotechHarness.Scripting;
using RimWorld;
using Verse;

namespace ArchotechHarness.Tests;

[Collection(DefDatabaseCollection.Name)]
public sealed class ScenarioBiomeTests : IDisposable
{
    private const string MeadowDefName = "ArchoTest_Meadow";
    private const string LagoonDefName = "ArchoTest_Lagoon";

    private readonly BiomeDef _meadow = new() { defName = MeadowDefName, canBuildBase = true };

    public ScenarioBiomeTests()
    {
        DefDatabase<BiomeDef>.Add(_meadow);
        DefDatabase<BiomeDef>.Add(new BiomeDef { defName = LagoonDefName, canBuildBase = false });
    }

    public void Dispose() => DefDatabase<BiomeDef>.Clear();

    [Fact]
    public void Resolve_ScenarioWithoutABiome_IsNull()
    {
        Assert.Null(ScenarioBiome.Resolve(typeof(AnywhereScenario)));
    }

    [Fact]
    public void Resolve_ScenarioNamingALoadedBiome_IsThatBiome()
    {
        Assert.Same(_meadow, ScenarioBiome.Resolve(typeof(MeadowScenario)));
    }

    [Fact]
    public void Resolve_ScenarioNamingAnUnknownBiome_Throws()
    {
        Assert.Throws<HarnessException>(() => ScenarioBiome.Resolve(typeof(TypoScenario)));
    }

    [Fact]
    public void Resolve_ScenarioNamingABiomeColoniesCannotSettle_Throws()
    {
        Assert.Throws<HarnessException>(() => ScenarioBiome.Resolve(typeof(LagoonScenario)));
    }

    private sealed class AnywhereScenario : HarnessScenario
    {
        public override Task Run() => Task.CompletedTask;
    }

    [Biome(MeadowDefName)]
    private sealed class MeadowScenario : HarnessScenario
    {
        public override Task Run() => Task.CompletedTask;
    }

    [Biome(LagoonDefName)]
    private sealed class LagoonScenario : HarnessScenario
    {
        public override Task Run() => Task.CompletedTask;
    }

    [Biome("ArchoTest_Medow")]
    private sealed class TypoScenario : HarnessScenario
    {
        public override Task Run() => Task.CompletedTask;
    }
}
