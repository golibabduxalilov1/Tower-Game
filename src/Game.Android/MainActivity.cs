using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using TowerGame.Content;
using TowerGame.Persistence;
using TowerGame.Platform;
using TowerGame.UI;

namespace TowerGame.Android;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Icon = "@mipmap/icon",
    RoundIcon = "@mipmap/icon",
    Theme = "@style/QalqonTheme",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden |
        ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.UiMode | ConfigChanges.SmallestScreenSize)]
public sealed class MainActivity : AndroidGameActivity
{
    DefenseGame? game;
    readonly AndroidHost host = new();

    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);
        // Draw under display cutouts; the game keeps controls inside the reported safe insets.
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window?.Attributes is { } attributes)
        {
            attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            Window.Attributes = attributes;
        }
        host.Density = Resources?.DisplayMetrics?.Density ?? 1;
        // App-private storage: no storage permission is requested or needed.
        var directory = System.IO.Path.Combine(FilesDir!.AbsolutePath, "saves");
        game = new DefenseGame(new GameStore(directory, Catalog.Maps), Catalog.Maps, new LaunchOptions(), host, new OfflineStore());
        var view = (View)game.Services.GetService(typeof(View))!;
        view.ViewAttachedToWindow += (_, _) => ReadInsets(view);
        view.LayoutChange += (_, _) => ReadInsets(view);
        SetContentView(view);
        game.Run();
    }

    void ReadInsets(View view)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(28)) return;
        var cutout = view.RootWindowInsets?.DisplayCutout;
        host.Insets = cutout == null ? default : new SafeInsets(cutout.SafeInsetLeft, cutout.SafeInsetTop, cutout.SafeInsetRight, cutout.SafeInsetBottom);
    }

    protected override void OnPause()
    {
        // Lock screen, home button or another app: pause, save and silence before Android may kill us.
        game?.OnBackground();
        base.OnPause();
    }
}

sealed class AndroidHost : IPlatformHost
{
    public bool IsTouch => true;
    public float Density { get; set; } = 1;
    public SafeInsets Insets { get; set; }
    public bool CanQuit => false;
    public string SystemLanguage => Java.Util.Locale.Default.Language ?? "en";
}
