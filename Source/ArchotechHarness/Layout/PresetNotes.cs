using System.Collections.Generic;

namespace ArchotechHarness.Layout;

/// <summary>What a best-effort preset left out, as lines for the scenario report.</summary>
public sealed class PresetNotes
{
    private readonly List<string> _lines = new();

    public IReadOnlyList<string> Lines => _lines;

    public void Count(string items, int wanted, int placed, string reason = "no free floor")
    {
        if (placed < wanted)
        {
            _lines.Add($"placed {placed} of {wanted} {items}, {reason}");
        }
    }
}
