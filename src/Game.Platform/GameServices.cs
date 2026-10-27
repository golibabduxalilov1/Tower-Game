using TowerGame.Core;
namespace TowerGame.Platform;

public sealed class GameSettings
{
    public string Language { get; set; } = "uz";
    public float MasterVolume { get; set; } = .65f;
    public float MusicVolume { get; set; } = .35f;
    public float EffectsVolume { get; set; } = .65f;
    public bool Muted { get; set; }
    public bool ShowPath { get; set; } = true;
    public bool ScreenShake { get; set; } = true;
    public bool Flashes { get; set; } = true;
    public int FrameRate { get; set; } = 60;
    public Dictionary<string, string> Keys { get; set; } = new()
    {
        ["Pause"] = "Space", ["Speed"] = "F", ["Upgrade"] = "U", ["Sell"] = "Delete",
        ["Tower1"] = "D1", ["Tower2"] = "D2", ["Tower3"] = "D3",
        ["Tower4"] = "D4", ["Tower5"] = "D5", ["Tower6"] = "D6"
    };
}

/// <summary>Persistence and storefront boundary. Rendering never reads user files directly.</summary>
public interface IGameServices
{
    GameSettings Settings { get; }
    bool HasSavedBattle { get; }
    /// <summary>True when no settings file existed at startup (used for initial language choice).</summary>
    bool IsFirstLaunch { get; }
    /// <summary>Short machine-readable error code of the last storage operation, never a path.</summary>
    string? LastError { get; }
    bool RecoveredBackup { get; }
    IReadOnlyCollection<string> EarnedAchievements { get; }
    bool UnlockAchievement(string achievementId);
    /// <summary>Appends plain text (performance reports) to a bounded file in the private save folder.</summary>
    bool AppendReport(string text);
    bool IsMapUnlocked(int index);
    bool IsModeUnlocked(string mapId, Mode mode);
    string Record(string mapId, Mode mode, Difficulty difficulty);
    (int Wave, long Score, int Stars)? Best(string mapId, Mode mode, Difficulty difficulty);
    SessionSnapshot? LoadBattle();
    bool SaveBattle(SessionSnapshot snapshot);
    bool SaveSettings();
    void RecordProgress(SessionSnapshot snapshot);
    void ClearBattle();
}
