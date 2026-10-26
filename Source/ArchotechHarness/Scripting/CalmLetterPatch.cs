using HarmonyLib;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Drops letters while the running script asks for no letters.</summary>
[HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter), [typeof(Letter), typeof(string), typeof(int), typeof(bool)])]
public static class CalmLetterPatch
{
    public static bool Prefix() => ScriptSession.Current is not { NoLetters: true };
}
