using ArchotechHarness.Layout;

namespace ArchotechHarness.Tests.Layout;

public sealed class PresetNotesTests
{
    [Fact]
    public void Lines_WhenEverythingFits_AreEmpty()
    {
        var notes = new PresetNotes();
        notes.Count("plant pots", wanted: 2, placed: 2);

        Assert.Empty(notes.Lines);
    }

    [Fact]
    public void Lines_WhenSomethingDoesNotFit_SayHowManyWerePlaced()
    {
        var notes = new PresetNotes();
        notes.Count("plant pots", wanted: 1, placed: 0);
        notes.Count("lamps", wanted: 3, placed: 3);

        Assert.Equal(["placed 0 of 1 plant pots, no free floor"], notes.Lines);
    }
}
