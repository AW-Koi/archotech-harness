using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>What a Furnish or Decorate preset placed in a room.</summary>
public sealed class FurnishedRoom(HarnessRoom room, IReadOnlyList<Thing> things)
{
    public HarnessRoom Room { get; } = room;

    public IReadOnlyList<Thing> Things { get; } = things;

    /// <summary>The bed a bedroom preset placed; null for other presets.</summary>
    public Building_Bed Bed => Things.OfType<Building_Bed>().FirstOrDefault();

    public IReadOnlyList<Thing> Of(ThingDef def) => Things.Where(t => t.def == def).ToList();
}
