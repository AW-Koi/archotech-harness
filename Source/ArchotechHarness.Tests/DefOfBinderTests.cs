using ArchotechHarness.Scripting;
using ArchotechHarness.Tests.DefOfs;
using Verse;

namespace ArchotechHarness.Tests;

[Collection(DefDatabaseCollection.Name)]
public sealed class DefOfBinderTests : IDisposable
{
    private const string MopDefName = "ArchoTest_Mop";
    private const string BucketDefName = "ArchoTest_Bucket";

    private readonly ThingDef _mop = new() { defName = MopDefName };
    private readonly ThingDef _bucket = new() { defName = BucketDefName };

    public DefOfBinderTests()
    {
        DefDatabase<ThingDef>.Add(_mop);
        DefDatabase<ThingDef>.Add(_bucket);
    }

    public void Dispose()
    {
        DefDatabase<ThingDef>.Clear();
        BoundTestDefOf.ArchoTest_Mop = null;
        BoundTestDefOf.Bucket = null;
    }

    [Fact]
    public void DefOfClasses_InAnAssembly_AreTheClassesMarkedDefOf()
    {
        var classes = DefOfBinder.DefOfClasses(typeof(BoundTestDefOf).Assembly);

        Assert.Equal([typeof(BoundTestDefOf), typeof(MissingTestDefOf)], classes.OrderBy(t => t.Name));
    }

    [Fact]
    public void Binding_WhenEveryDefExists_SetsTheFieldsToTheDefs()
    {
        DefOfBinder.Bind([typeof(BoundTestDefOf)]);

        Assert.Same(_mop, BoundTestDefOf.ArchoTest_Mop);
    }

    [Fact]
    public void Binding_FieldWithDefAlias_SetsTheAliasedDef()
    {
        DefOfBinder.Bind([typeof(BoundTestDefOf)]);

        Assert.Same(_bucket, BoundTestDefOf.Bucket);
    }

    [Fact]
    public void Binding_WhenADefIsMissing_ThrowsUnknownDefException()
    {
        var exception = Assert.Throws<UnknownDefException>(() => DefOfBinder.Bind([typeof(MissingTestDefOf)]));

        Assert.Equal(["MissingTestDefOf.ArchoTest_NoSuchFloor"], exception.Fields);
    }
}
