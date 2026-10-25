using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ArchotechHarness.Cli;

/// <summary>Gives the launched game window the harness title and icon, and reapplies them if the game resets the title.</summary>
/// <remarks>The icons belong to this process, so it must stay alive while the game runs; the watchdog owns it.</remarks>
public sealed partial class WindowBranding : IDisposable
{
    public const string Title = "Archotech Harness";

    private const uint ImageIcon = 1;
    private const uint LoadFromFile = 0x10;
    private const uint SetIconMessage = 0x80;
    private const int SmallIcon = 0;
    private const int BigIcon = 1;
    private const int SmallIconMetric = 49;
    private const int BigIconMetric = 11;

    private readonly IntPtr _bigIcon;
    private readonly IntPtr _smallIcon;

    public WindowBranding(string iconPath)
    {
        _bigIcon = LoadIcon(iconPath, GetSystemMetrics(BigIconMetric));
        _smallIcon = LoadIcon(iconPath, GetSystemMetrics(SmallIconMetric));
    }

    /// <summary>Brands the process's main window if it shows another title. Returns false while it has no window.</summary>
    public bool Apply(Process game)
    {
        game.Refresh();
        var window = game.MainWindowHandle;
        if (window == IntPtr.Zero)
        {
            return false;
        }

        if (game.MainWindowTitle != Title)
        {
            if (!SetWindowTextW(window, Title))
            {
                return false;
            }

            if (_bigIcon != IntPtr.Zero)
            {
                SendMessageW(window, SetIconMessage, BigIcon, _bigIcon);
            }

            if (_smallIcon != IntPtr.Zero)
            {
                SendMessageW(window, SetIconMessage, SmallIcon, _smallIcon);
            }
        }

        return true;
    }

    /// <summary>Keeps the window branded until the game exits, checking once a second.</summary>
    public void Maintain(Process game)
    {
        while (!game.WaitForExit(TimeSpan.FromSeconds(1)))
        {
            Apply(game);
        }
    }

    public void Dispose()
    {
        DestroyIcon(_bigIcon);
        DestroyIcon(_smallIcon);
    }

    private static IntPtr LoadIcon(string path, int size) =>
        File.Exists(path) ? LoadImageW(IntPtr.Zero, path, ImageIcon, size, size, LoadFromFile) : IntPtr.Zero;

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowTextW(IntPtr window, string text);

    [LibraryImport("user32.dll")]
    private static partial void SendMessageW(IntPtr window, uint message, nint wParam, IntPtr lParam);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr LoadImageW(IntPtr instance, string name, uint type, int width, int height, uint load);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    private static partial void DestroyIcon(IntPtr icon);
}
