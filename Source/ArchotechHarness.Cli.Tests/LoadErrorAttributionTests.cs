using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ArchotechHarness.Cli.Tests;

public sealed class LoadErrorAttributionTests : IDisposable
{
    private const string MopModName = "Mop Mod";

    private const string MopModAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Mop Mod</name>
          <packageId>TerraIncognita.MopMod</packageId>
        </ModMetaData>
        """;

    private const string MopModDefs = """
        <?xml version="1.0" encoding="utf-8"?>
        <Defs>
          <ThingDef Name="MopBase" Abstract="True" />
          <ThingDef ParentName="MopBase">
            <defName>MopMod_Mop</defName>
          </ThingDef>
        </Defs>
        """;

    private const string MopModCode = "namespace MopMod.Comps { public class CompMop { } }";

    private readonly TempDirectory _temp = new();
    private readonly string _installedDirectory;
    private readonly IReadOnlyList<ModFingerprint> _modsUnderTest;

    public LoadErrorAttributionTests()
    {
        var modDirectory = WriteMod("MopMod/mod", MopModAbout, MopModCode);
        _installedDirectory = _temp.Combine("Mods", "archo-terraincognita-mopmod");
        _modsUnderTest = [ModFingerprint.Read(modDirectory, _installedDirectory)];
    }

    public void Dispose() => _temp.Dispose();

    [Theory]
    [InlineData("[Mop Mod] Patch operation Verse.PatchOperationAdd(/Defs/ThingDef[defName=\"Broom\"]/comps) failed")]
    [InlineData("[TerraIncognita.MopMod] Could not read the settings.")]
    public void Attribution_LinePrefixedWithTheModNameOrPackageId_BlamesTheModUnderTest(string text)
    {
        var error = LoadErrorAttribution.Attribute(text, _modsUnderTest);

        Assert.Equal(new LoadError(text, MopModName, FromModUnderTest: true), error);
    }

    [Theory]
    [InlineData("Config error in MopMod_Mop: has null thingClass.")]
    [InlineData("Exception in ConfigErrors() of MopMod_Mop: System.NullReferenceException: Object reference not set to an instance of an object.")]
    [InlineData("Could not resolve cross-reference: No Verse.HediffDef named Soaked found to give to Verse.ThingDef MopMod_Mop")]
    [InlineData("Exception loading def from file Mops.xml: System.ArgumentException: Could not find type named Wet from node <li Class=\"Wet\">")]
    public void Attribution_LineNamingADefOrDefFileOfTheMod_BlamesTheModUnderTest(string text)
    {
        var error = LoadErrorAttribution.Attribute(text, _modsUnderTest);

        Assert.Equal(new LoadError(text, MopModName, FromModUnderTest: true), error);
    }

    [Theory]
    [InlineData("Could not find type named MopMod.Comps.CompProperties_Wring from node <li Class=\"MopMod.Comps.CompProperties_Wring\">")]
    [InlineData("Error in static constructor: System.NullReferenceException\n  at MopMod.Comps.CompMop..cctor ()")]
    public void Attribution_LineNamingATypeInTheModsNamespace_BlamesTheModUnderTest(string text)
    {
        var error = LoadErrorAttribution.Attribute(text, _modsUnderTest);

        Assert.Equal(new LoadError(text, MopModName, FromModUnderTest: true), error);
    }

    [Fact]
    public void Attribution_LineNamingAFileInTheInstalledModFolder_BlamesTheModUnderTest()
    {
        var text = $"XML error: duplicate defName in file {Path.Combine(_installedDirectory, "Defs", "Buckets.xml")}";

        var error = LoadErrorAttribution.Attribute(text, _modsUnderTest);

        Assert.Equal(new LoadError(text, MopModName, FromModUnderTest: true), error);
    }

    [Fact]
    public void Attribution_LinePrefixedWithAnotherModName_NamesThatModAsNotUnderTest()
    {
        const string text = "[Vanilla Expanded Framework] Could not find type named VEF.AnimalBehaviours.CompFloating";

        var error = LoadErrorAttribution.Attribute(text, _modsUnderTest);

        Assert.Equal(new LoadError(text, "Vanilla Expanded Framework", FromModUnderTest: false), error);
    }

    [Theory]
    [InlineData("Getting apparel graphic with undefined body type.")]
    [InlineData("Config error in Broom: has null thingClass.")]
    [InlineData("Exception ticking Pawn: System.NullReferenceException\n  at RimWorld.JobDriver_Clean.MakeNewToils ()")]
    public void Attribution_LineNamingNothingOfTheMod_IsUnattributed(string text)
    {
        var error = LoadErrorAttribution.Attribute(text, _modsUnderTest);

        Assert.Equal(new LoadError(text, null, FromModUnderTest: false), error);
    }

    [Fact]
    public void Attribution_ModDeclaringTypesInAGameNamespace_LeavesGameStackTracesUnattributed()
    {
        var modDirectory = WriteMod("PatchMod", MopModAbout, "namespace RimWorld { public class JobDriver_Wring { } }");
        IReadOnlyList<ModFingerprint> modsUnderTest = [ModFingerprint.Read(modDirectory, installedDirectory: null)];
        const string text = "Exception ticking Pawn: System.NullReferenceException\n  at RimWorld.JobDriver_Clean.MakeNewToils ()";

        var error = LoadErrorAttribution.Attribute(text, modsUnderTest);

        Assert.Equal(new LoadError(text, null, FromModUnderTest: false), error);
    }

    private string WriteMod(string relativeDirectory, string about, string code)
    {
        _temp.WriteFile($"{relativeDirectory}/About/About.xml", about);
        _temp.WriteFile($"{relativeDirectory}/1.7/Defs/Mops.xml", MopModDefs);
        var assembly = _temp.Combine(relativeDirectory, "1.7", "Assemblies", "MopMod.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(assembly)!);
        var compilation = CSharpCompilation.Create("MopMod", [CSharpSyntaxTree.ParseText(code)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.True(compilation.Emit(assembly).Success);
        return _temp.Combine(relativeDirectory);
    }
}
