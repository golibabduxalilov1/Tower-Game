using TowerGame.Content;
using TowerGame.Persistence;
using TowerGame.Platform;
using TowerGame.UI;

// Windows x64 entry point. Saves live in the per-user private folder; no admin rights needed.
var options = new LaunchOptions();
IPlatformHost host = new DesktopHost();
foreach (var arg in args)
{
    var (key, value) = arg.Split('=', 2) is [var k, var v] ? (k, v) : (arg, "");
    switch (key)
    {
        case "--screenshot": options.Screenshot = value; break;
        case "--scene": options.Scene = value; break;
        case "--frames": options.Frames = int.Parse(value); break;
        case "--map": options.MapIndex = int.Parse(value); break;
        case "--lang": options.Language = value; break;
        case "--size" when value.Split('x') is [var w, var h]: options.Width = int.Parse(w); options.Height = int.Parse(h); break;
        case "--demo": options.Demo = true; break;
        case "--bench": options.Bench = true; break;
        case "--script": options.Script = value; break;
        // Phone layout preview on desktop: --touch=<density>, e.g. --touch=1.4 --size=1200x540.
        case "--touch": host = new PreviewTouchHost(float.Parse(value, System.Globalization.CultureInfo.InvariantCulture)); break;
    }
}
var maps = Catalog.Maps;
// Automation runs never touch the player's real profile.
var directory = options.Screenshot != null || options.Script != null || args.Contains("--temp")
    ? Path.Combine(Path.GetTempPath(), "QalqonAutomation")
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QalqonDefense");
IStoreIntegration storefront = new OfflineStore();
try
{
    using var game = new DefenseGame(new GameStore(directory, maps), maps, options, host, storefront);
    game.Components.Add(new DesktopWindow(game));
    game.Run();
}
catch (Exception e)
{
    // Crash log: exception type and stack only, no user data. Kept next to the private saves.
    try { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "crash.log"), $"{DateTime.UtcNow:O}\n{e}"); } catch (IOException) { }
    throw;
}

sealed class PreviewTouchHost(float density) : IPlatformHost
{
    public bool IsTouch => true;
    public float Density => density;
    public SafeInsets Insets => new((int)(32 * density), 0, 0, 0); // simulated camera cutout
    public bool CanQuit => false;
    public string SystemLanguage => "uz";
}