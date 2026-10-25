using HarmonyLib;
using Verse;

namespace ArchotechHarness.Debugger;

/// <summary>Draws the debug panel after the game's own GUI, inside the same ImGui frame.</summary>
[HarmonyPatch(typeof(Root), nameof(Root.OnGUI))]
public static class DebugPanelPatch
{
    public static void Postfix()
    {
        DebugPanel.OnGUI();
        if (DebugPanel.ScreenRect.width <= 0)
        {
            PanelBlocker.Close();
        }
    }
}
