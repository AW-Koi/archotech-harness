namespace ArchotechHarness.Scripting;

/// <summary>Where a raid that didn't fire failed.</summary>
public enum RaidStep
{
    /// <summary>The faction isn't hostile to the colony.</summary>
    Faction,

    /// <summary>The arrival mode found nowhere for the raiders to enter the map.</summary>
    EntryPoint,

    /// <summary>The game generated no raiders for the faction and points.</summary>
    Raiders,
}
