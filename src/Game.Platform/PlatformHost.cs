namespace TowerGame.Platform;

/// <summary>Screen-edge insets in physical pixels (display cutouts, system bars).</summary>
public readonly record struct SafeInsets(int Left, int Top, int Right, int Bottom);

/// <summary>Device facts the shared game needs. Implemented by Game.Windows and Game.Android.</summary>
public interface IPlatformHost
{
    /// <summary>Primary input is touch: build requires an explicit confirm step.</summary>
    bool IsTouch { get; }
    /// <summary>Physical pixels per density-independent pixel (Android dp). 1 on desktop.</summary>
    float Density { get; }
    SafeInsets Insets { get; }
    /// <summary>Desktop builds show a Quit button; mobile apps leave through the OS.</summary>
    bool CanQuit { get; }
    /// <summary>Two-letter system language used once on first launch.</summary>
    string SystemLanguage { get; }
}

public sealed class DesktopHost : IPlatformHost
{
    public bool IsTouch => false;
    public float Density => 1;
    public SafeInsets Insets => default;
    public bool CanQuit => true;
    public string SystemLanguage => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
}

/// <summary>
/// Storefront features (achievements, cloud profile, initial language). The core game never
/// depends on a store being reachable: every call must be safe to ignore.
/// </summary>
public interface IStoreIntegration
{
    string Name { get; }
    bool Available { get; }
    /// <summary>Preferred language reported by the store client, or null.</summary>
    string? PreferredLanguage { get; }
    void Unlock(string achievementId);
    /// <summary>Pump store callbacks once per frame.</summary>
    void Update();
}

public sealed class OfflineStore : IStoreIntegration
{
    public string Name => "offline";
    public bool Available => false;
    public string? PreferredLanguage => null;
    public void Unlock(string achievementId) { }
    public void Update() { }
}

/// <summary>Achievement identifiers shared by every store adapter.</summary>
public static class Achievements
{
    public const string FirstBuild = "FIRST_BUILD";
    public const string MaxLevel = "TOWER_LEVEL_3";
    public const string BossDown = "BOSS_DOWN";
    public const string Wave50 = "WAVE_50";
    public const string ClassicWin = "CLASSIC_VICTORY";
    public const string Flawless = "THREE_STARS";
    public const string AllMaps = "ALL_MAPS_OPEN";
    public const string Endless150 = "ENDLESS_150";
    public static readonly string[] All = [FirstBuild, MaxLevel, BossDown, Wave50, ClassicWin, Flawless, AllMaps, Endless150];
}
