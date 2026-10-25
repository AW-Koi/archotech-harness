namespace ArchotechHarness.Cli.Tests;

public class ModLoadOrderTests
{
    private const string EducationId = "ferny.progressioneducation";
    private const string FrameworkId = "oskarpotocki.vanillafactionsexpanded.core";
    private const string ArmourId = "vanillaexpanded.varmoure";
    private const string JanitorId = "terraincognita.janitorscloset";

    [Fact]
    public void Sort_DependencyDeclaredAfterItsDependent_LoadsFirst()
    {
        var education = new ModMetadata(EducationId, "Progression: Education", "Education", [FrameworkId], [], []);
        var framework = new ModMetadata(FrameworkId, "Vanilla Expanded Framework", "Framework", [], [], []);

        var order = ModLoadOrder.Sort([education, framework], []);

        Assert.Equal([FrameworkId, EducationId], order);
    }

    [Fact]
    public void Sort_LoadAfter_PutsTheModAfterTheOneItNames()
    {
        var armour = new ModMetadata(ArmourId, "Vanilla Armour Expanded", "Armour", [], [EducationId], []);
        var education = new ModMetadata(EducationId, "Progression: Education", "Education", [], [], []);

        var order = ModLoadOrder.Sort([armour, education], []);

        Assert.Equal([EducationId, ArmourId], order);
    }

    [Fact]
    public void Sort_LoadBefore_PutsTheModBeforeTheOneItNames()
    {
        var armour = new ModMetadata(ArmourId, "Vanilla Armour Expanded", "Armour", [], [], []);
        var education = new ModMetadata(EducationId, "Progression: Education", "Education", [], [], [ArmourId]);

        var order = ModLoadOrder.Sort([armour, education], []);

        Assert.Equal([EducationId, ArmourId], order);
    }

    [Fact]
    public void Sort_ModUnderTestWithoutConstraints_LoadsAfterRequiredMods()
    {
        var janitor = new ModMetadata(JanitorId, "Janitor's Closet", "JanitorsCloset", [], [], []);
        var education = new ModMetadata(EducationId, "Progression: Education", "Education", [], [], []);

        var order = ModLoadOrder.Sort([education], [janitor]);

        Assert.Equal([EducationId, JanitorId], order);
    }

    [Fact]
    public void Sort_ModUnderTestLoadingBeforeARequiredMod_LoadsFirst()
    {
        var janitor = new ModMetadata(JanitorId, "Janitor's Closet", "JanitorsCloset", [], [], [EducationId]);
        var education = new ModMetadata(EducationId, "Progression: Education", "Education", [], [], []);

        var order = ModLoadOrder.Sort([education], [janitor]);

        Assert.Equal([JanitorId, EducationId], order);
    }

    [Fact]
    public void Sort_ConstraintCycle_KeepsTheDeclaredOrder()
    {
        var armour = new ModMetadata(ArmourId, "Vanilla Armour Expanded", "Armour", [], [EducationId], []);
        var education = new ModMetadata(EducationId, "Progression: Education", "Education", [], [ArmourId], []);

        var order = ModLoadOrder.Sort([armour, education], []);

        Assert.Equal([ArmourId, EducationId], order);
    }

    [Fact]
    public void Sort_ConstraintNamingAModNotLoaded_IsIgnored()
    {
        var armour = new ModMetadata(ArmourId, "Vanilla Armour Expanded", "Armour", [], ["ludeon.rimworld"], ["other.mod"]);
        var education = new ModMetadata(EducationId, "Progression: Education", "Education", [], [], []);

        var order = ModLoadOrder.Sort([armour, education], []);

        Assert.Equal([ArmourId, EducationId], order);
    }
}
