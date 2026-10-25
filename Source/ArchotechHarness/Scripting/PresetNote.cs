using ArchotechHarness.Layout;

namespace ArchotechHarness.Scripting;

/// <summary>Adds what a best-effort builder left out to the scenario report as a note.</summary>
internal static class PresetNote
{
    public static void Record(string label, PresetNotes notes)
    {
        if (notes.Lines.Count > 0)
        {
            ScriptSession.Current?.Note(label, string.Join("; ", notes.Lines));
        }
    }

    public static void Record(string label, string items, int wanted, int placed, string reason)
    {
        var notes = new PresetNotes();
        notes.Count(items, wanted, placed, reason);
        Record(label, notes);
    }
}
