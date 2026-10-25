using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DevHarness.Cli;

/// <summary>Captures the client area of a process's main window with PrintWindow, which works on the Vulkan swapchain.</summary>
/// <remarks>The window may be covered or unfocused, but not minimised.</remarks>
public static partial class WindowCapture
{
    private const uint PrintClientOnlyFullContent = 0x1 | 0x2;
    private const uint DibRgbColours = 0;

    public static (int Width, int Height) Save(int processId, string path)
    {
        var window = Process.GetProcessById(processId).MainWindowHandle;
        if (window == IntPtr.Zero)
        {
            throw new ScenarioException($"Process {processId} has no main window.");
        }

        if (!GetClientRect(window, out var rect))
        {
            throw new ScenarioException("GetClientRect failed.");
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            throw new ScenarioException("The game window has no visible client area (minimised?).");
        }

        var screen = GetDC(IntPtr.Zero);
        var memory = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, width, height);
        var previous = SelectObject(memory, bitmap);
        try
        {
            if (!PrintWindow(window, memory, PrintClientOnlyFullContent))
            {
                throw new ScenarioException("PrintWindow failed.");
            }

            var header = new BitmapInfoHeader
            {
                Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                Width = width,
                Height = -height,
                Planes = 1,
                BitCount = 32,
            };
            var pixels = new byte[width * height * 4];
            SelectObject(memory, previous);
            if (GetDeviceIndependentBits(memory, bitmap, 0, (uint)height, pixels, ref header, DibRgbColours) == 0)
            {
                throw new ScenarioException("GetDIBits failed.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllBytes(path, PngEncoder.Encode(pixels, width, height));
            return (width, height);
        }
        finally
        {
            DeleteObject(bitmap);
            DeleteDeviceContext(memory);
            _ = ReleaseDC(IntPtr.Zero, screen);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(IntPtr window, out Rect rect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetDC(IntPtr window);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(IntPtr window, IntPtr deviceContext);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleBitmap(IntPtr deviceContext, int width, int height);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr SelectObject(IntPtr deviceContext, IntPtr gdiObject);

    [LibraryImport("gdi32.dll")]
    private static partial void DeleteObject(IntPtr gdiObject);

    [LibraryImport("gdi32.dll", EntryPoint = "DeleteDC")]
    private static partial void DeleteDeviceContext(IntPtr deviceContext);

    [LibraryImport("gdi32.dll", EntryPoint = "GetDIBits")]
    private static partial int GetDeviceIndependentBits(IntPtr deviceContext, IntPtr bitmap, uint start, uint lines, [Out] byte[] bits, ref BitmapInfoHeader header, uint usage);
}
