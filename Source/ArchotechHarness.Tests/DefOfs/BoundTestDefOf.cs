using RimWorld;
using Verse;

namespace ArchotechHarness.Tests.DefOfs;

// ReSharper disable InconsistentNaming
[DefOf]
public static class BoundTestDefOf
{
    public static ThingDef ArchoTest_Mop;

    [DefAlias("ArchoTest_Bucket")]
    public static ThingDef Bucket;
}
