using Verse;

namespace ArchotechHarness.Tests.Layout;

/// <summary>Orders cells by x, then z, so tests can compare sets of cells as lists.</summary>
public sealed class CellOrder : IComparer<IntVec3>
{
    public static CellOrder Instance { get; } = new();

    public int Compare(IntVec3 a, IntVec3 b) => a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z);
}
