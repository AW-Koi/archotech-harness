using HarmonyLib;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Counts each error entering the game's log, repeats included, while the game loads and while a scenario runs.</summary>
[HarmonyPatch(typeof(LogMessageQueue), nameof(LogMessageQueue.Enqueue), [typeof(LogMessage), typeof(bool)], [ArgumentType.Normal, ArgumentType.Out])]
public static class GameErrorCapturePatch
{
    public static void Prefix(LogMessage msg)
    {
        if (msg.type == LogMessageType.Error)
        {
            LoadErrorLog.Current.Add(msg.text);
            ScriptSession.Current?.GameErrors.Add(msg.text);
        }
    }
}
