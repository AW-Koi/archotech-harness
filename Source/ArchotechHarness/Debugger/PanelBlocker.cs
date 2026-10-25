using UnityEngine;
using Verse;

namespace ArchotechHarness.Debugger;

/// <summary>An invisible game window under the debug panel, so clicks on the panel don't also reach the map.</summary>
public sealed class PanelBlocker : Window
{
    private static PanelBlocker _open;

    private PanelBlocker()
    {
        layer = WindowLayer.Super;
        doWindowBackground = false;
        drawShadow = false;
        focusWhenOpened = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        closeOnCancel = false;
        soundAppear = null;
        soundClose = null;
    }

    protected override float Margin => 0;

    /// <summary>Opens the blocker over the area, moves it there, or closes it when the area is empty.</summary>
    public static void Follow(Rect area)
    {
        if (area.width <= 0)
        {
            Close();
            return;
        }

        if (_open == null || !Find.WindowStack.IsOpen(_open))
        {
            _open = new PanelBlocker();
            Find.WindowStack.Add(_open);
        }

        _open.windowRect = area;
    }

    public static void Close()
    {
        if (_open != null)
        {
            Find.WindowStack?.TryRemove(_open, doCloseSound: false);
            _open = null;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
    }

    protected override void SetInitialSizeAndPosition()
    {
    }
}
