using Verse;

namespace ArchotechHarness.Commands;

/// <summary>A count of a thing to put on a storage building.</summary>
public sealed record StoredStack(ThingDef Def, int Count);
