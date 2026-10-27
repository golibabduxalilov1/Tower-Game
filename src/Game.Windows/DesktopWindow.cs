using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

// MonoGame recreates the SDL window during Game.Initialize, so the icon and title bar are styled from a
// component (initialized right after that) rather than on the placeholder window seen before Run().
// Game.Window.Handle is the SDL_Window pointer on DesktopGL; the Win32 HWND comes from SDL_GetWindowWMInfo.
sealed class DesktopWindow(Game game) : GameComponent(game)
{
    readonly List<nint> icons = [];

    public override void Initialize()
    {
        base.Initialize();
        var window = NativeHandle(Game.Window.Handle);
        if (window == 0) return;
        ApplyIcon(window);
        // Title bar in the menu's navy (Windows 11); older systems fall back to the dark caption.
        SetAttribute(window, 20, 1);                    // DWMWA_USE_IMMERSIVE_DARK_MODE
        SetAttribute(window, 35, Rgb(10, 22, 42));      // DWMWA_CAPTION_COLOR
        SetAttribute(window, 34, Rgb(10, 22, 42));      // DWMWA_BORDER_COLOR
        SetAttribute(window, 36, Rgb(232, 242, 255));   // DWMWA_TEXT_COLOR
    }

    void ApplyIcon(nint window)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Content", "app.ico");
        if (!File.Exists(path)) return;
        foreach (var (kind, size) in new[] { (0, GetSystemMetrics(49)), (1, GetSystemMetrics(11)) })
        {
            var icon = LoadImage(0, path, 1, size, size, 0x10);
            if (icon == 0) continue;
            icons.Add(icon);
            SendMessage(window, 0x80, kind, icon);
        }
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var icon in icons) DestroyIcon(icon);
        icons.Clear();
        base.Dispose(disposing);
    }

    static nint NativeHandle(nint sdlWindow)
    {
        // SDL_SysWMinfo: SDL_version (major, minor, patch), subsystem, then the HWND at offset 8.
        var info = new byte[128];
        info[0] = 2;
        try { return sdlWindow != 0 && SDL_GetWindowWMInfo(sdlWindow, info) != 0 ? (nint)BitConverter.ToInt64(info, 8) : 0; }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException) { return 0; }
    }

    static int Rgb(byte r, byte g, byte b) => r | g << 8 | b << 16;

    static void SetAttribute(nint window, int attribute, int value) => DwmSetWindowAttribute(window, attribute, ref value, sizeof(int));

    [DllImport("SDL2.dll")]
    static extern int SDL_GetWindowWMInfo(nint window, byte[] info);
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadImageW")]
    static extern nint LoadImage(nint instance, string name, uint type, int width, int height, uint flags);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DestroyIcon(nint icon);
}
