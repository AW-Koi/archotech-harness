using System.Collections.Generic;

namespace ArchotechHarness.Layout;

/// <summary>A table and the chairs facing it.</summary>
public sealed record DiningSet(Placement Table, IReadOnlyList<Placement> Chairs);
