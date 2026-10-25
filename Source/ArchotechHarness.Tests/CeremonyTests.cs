using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class CeremonyTests
{
    [Fact]
    public void Wedding_OfAPawnToItself_Throws()
    {
        var pawn = new Pawn();

        Assert.Throws<HarnessException>(() => Ceremony.Wedding(pawn, pawn));
    }
}
