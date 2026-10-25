using System.Collections.Generic;

namespace ArchotechHarness.Layout;

/// <summary>Where BedroomLayout puts the bed, its end tables and the dresser; null or fewer where nothing was free.</summary>
public sealed record BedroomPlan(Placement Bed, IReadOnlyList<Placement> EndTables, Placement Dresser);
