using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TowerGame.Core;
using TowerGame.Platform;

namespace TowerGame.Persistence;

public sealed class BestResult
{
    public int Wave { get; set; }
    public long Score { get; set; }
    public int Stars { get; set; }
}

public sealed class PlayerProfile
{
    public HashSet<string> UnlockedMaps { get; set; } = [];
    public HashSet<string> ClassicVictories { get; set; } = [];
    public Dictionary<string, BestResult> Records { get; set; } = [];
    public HashSet<string> EarnedAchievements { get; set; } = [];
}

/// <summary>
/// Atomic, bounded, versioned local storage. Checksums detect corruption, not cheating.
/// No untrusted file path, type name, executable data or credentials are deserialized.
/// </summary>
public sealed class GameStore : IGameServices
{
    const int FormatVersion = 2;
    const int MaxFileBytes = 4 * 1024 * 1024;
    readonly string directory;
    readonly IReadOnlyList<MapDefinition> maps;
    readonly object gate = new();
    static readonly JsonSerializerOptions Json = new() { MaxDepth = 48 };
    public GameSettings Settings { get; private set; } = new();
    public PlayerProfile Profile { get; private set; } = new();
    public string? LastError { get; private set; }
    public bool RecoveredBackup { get; private set; }
    public bool HasSavedBattle => File.Exists(PathFor("battle")) || File.Exists(PathFor("battle") + ".bak");
    public bool IsFirstLaunch { get; }
    public IReadOnlyCollection<string> EarnedAchievements => Profile.EarnedAchievements;

    public bool UnlockAchievement(string achievementId)
    {
        lock (gate)
        {
            if (!Achievements.All.Contains(achievementId)) return false;
            if (!Profile.EarnedAchievements.Add(achievementId)) return true;
            if (Write("profile", Profile, ValidProfile)) return true;
            Profile.EarnedAchievements.Remove(achievementId);
            return false;
        }
    }

    public GameStore(string directory, IReadOnlyList<MapDefinition> maps)
    {
        this.directory = Path.GetFullPath(directory);
        this.maps = maps;
        try { Directory.CreateDirectory(this.directory); }
        catch (Exception e) when (StorageError(e)) { LastError = "save_failed"; }
        IsFirstLaunch = !File.Exists(PathFor("settings")) && !File.Exists(PathFor("settings") + ".bak");
        Settings = Read<GameSettings>("settings", ValidSettings) ?? new();
        Profile = Read<PlayerProfile>("profile", ValidProfile) ?? new();
        if (maps.Count > 0) Profile.UnlockedMaps.Add(maps[0].Id);
    }

    string PathFor(string name) => Path.Combine(directory, name + ".json");
    public bool IsMapUnlocked(int index) => index >= 0 && index < maps.Count &&
        (index == 0 || Profile.UnlockedMaps.Contains(maps[index].Id));
    public bool IsModeUnlocked(string mapId, Mode mode) =>
        mode == Mode.Classic || Profile.ClassicVictories.Contains(mapId);
    static string Key(string id, Mode mode, Difficulty difficulty) => $"{id}/{mode}/{difficulty}";
    public string Record(string mapId, Mode mode, Difficulty difficulty)
    {
        if (!Profile.Records.TryGetValue(Key(mapId, mode, difficulty), out var result)) return "—";
        return $"{result.Wave}  /  {result.Score:N0}  {new string('★', result.Stars)}";
    }

    public (int Wave, long Score, int Stars)? Best(string mapId, Mode mode, Difficulty difficulty) =>
        Profile.Records.TryGetValue(Key(mapId, mode, difficulty), out var r) ? (r.Wave, r.Score, r.Stars) : null;

    public SessionSnapshot? LoadBattle()
    {
        lock (gate) return Read<SessionSnapshot>("battle", ValidBattle);
    }

    public bool SaveBattle(SessionSnapshot snapshot)
    {
        lock (gate)
        {
            if (!ValidBattle(snapshot)) { LastError = "invalid_save"; return false; }
            if (snapshot.State is SessionState.Defeat or SessionState.Victory)
            {
                RecordProgress(snapshot);
                if (LastError != null) return false;
                ClearBattle();
                return LastError == null;
            }
            return Write("battle", snapshot, ValidBattle);
        }
    }

    public bool SaveSettings()
    {
        lock (gate) return ValidSettings(Settings) && Write("settings", Settings, ValidSettings);
    }

    public void RecordProgress(SessionSnapshot snapshot)
    {
        lock (gate)
        {
            if (!ValidBattle(snapshot)) { LastError = "invalid_save"; return; }
            var completed = snapshot.State is SessionState.Intermission or SessionState.Victory ||
                snapshot.State == SessionState.Paused && snapshot.ResumeState == SessionState.Intermission
                ? snapshot.Wave : Math.Max(0, snapshot.Wave - 1);
            var index = -1;
            for (var i = 0; i < maps.Count; i++) if (maps[i].Id == snapshot.MapId) index = i;
            if (index >= 0 && completed >= Math.Min(50, maps[index].Waves.Count) && index + 1 < maps.Count)
                Profile.UnlockedMaps.Add(maps[index + 1].Id);
            if (snapshot.State == SessionState.Victory && snapshot.Mode == Mode.Classic)
                Profile.ClassicVictories.Add(snapshot.MapId);
            var key = Key(snapshot.MapId, snapshot.Mode, snapshot.Difficulty);
            if (!Profile.Records.TryGetValue(key, out var best)) Profile.Records[key] = best = new();
            best.Wave = Math.Max(best.Wave, completed);
            best.Score = Math.Max(best.Score, snapshot.Score);
            var stars = snapshot.State == SessionState.Victory
                ? snapshot.Lives == 20 ? 3 : snapshot.Lives >= 10 ? 2 : 1 : 0;
            best.Stars = Math.Max(best.Stars, stars);
            Write("profile", Profile, ValidProfile);
        }
    }

    public bool AppendReport(string text)
    {
        lock (gate)
        {
            try
            {
                var path = Path.Combine(directory, "performance-report.txt");
                if (File.Exists(path) && new FileInfo(path).Length > 512 * 1024) File.Delete(path);
                File.AppendAllText(path, text.Length > 16384 ? text[..16384] : text);
                return true;
            }
            catch (Exception e) when (StorageError(e)) { LastError = "save_failed"; return false; }
        }
    }

    public void ClearBattle()
    {
        lock (gate)
        {
            try
            {
                File.Delete(PathFor("battle"));
                File.Delete(PathFor("battle") + ".bak");
                LastError = null;
            }
            catch (Exception e) when (StorageError(e)) { LastError = "save_failed"; }
        }
    }

    bool ValidBattle(SessionSnapshot snapshot)
    {
        try
        {
            var map = maps.FirstOrDefault(m => m.Id == snapshot.MapId);
            if (map == null) return false;
            _ = GameSession.Restore(map, snapshot);
            return true;
        }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or NullReferenceException or IndexOutOfRangeException)
        { return false; }
    }

    bool ValidProfile(PlayerProfile p) => p != null && p.UnlockedMaps != null &&
        p.ClassicVictories != null && p.Records != null && p.EarnedAchievements != null &&
        p.EarnedAchievements.Count <= Achievements.All.Length && p.EarnedAchievements.All(Achievements.All.Contains) &&
        p.UnlockedMaps.Count <= maps.Count &&
        p.ClassicVictories.Count <= maps.Count && p.Records.Count <= maps.Count * 9 &&
        p.UnlockedMaps.Concat(p.ClassicVictories).All(id => maps.Any(m => m.Id == id)) &&
        p.Records.All(pair => maps.Any(m => Enum.GetValues<Mode>().Any(mode =>
            Enum.GetValues<Difficulty>().Any(d => Key(m.Id, mode, d) == pair.Key))) &&
            pair.Value != null && pair.Value.Wave is >= 0 and <= 1000000 &&
            pair.Value.Score >= 0 && pair.Value.Stars is >= 0 and <= 3);

    static bool ValidSettings(GameSettings s)
    {
        static bool Volume(float v) => float.IsFinite(v) && v >= 0 && v <= 1;
        return s != null && s.Language is "uz" or "en" &&
            Volume(s.MasterVolume) && Volume(s.MusicVolume) && Volume(s.EffectsVolume) &&
            s.FrameRate is 30 or 60 && s.Keys != null && s.Keys.Count <= 32 &&
            s.Keys.All(p => p.Key.Length <= 32 && p.Value != null && p.Value.Length <= 32 &&
                p.Value.All(c => char.IsAsciiLetterOrDigit(c)));
    }

    sealed class Envelope
    {
        public int FormatVersion { get; set; }
        public string AppVersion { get; set; } = "0.3.0";
        public DateTimeOffset SavedAt { get; set; }
        public JsonElement Data { get; set; }
        public string Checksum { get; set; } = "";
    }

    static bool StorageError(Exception e) => e is IOException or UnauthorizedAccessException or
        JsonException or NotSupportedException or CryptographicException;
    static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    T? Read<T>(string name, Func<T, bool> validate) where T : class
    {
        foreach (var suffix in new[] { "", ".bak" })
        {
            var path = PathFor(name) + suffix;
            if (!File.Exists(path)) continue;
            try
            {
                var value = Decode<T>(path, validate);
                if (suffix == ".bak") RecoveredBackup = true;
                LastError = null;
                return value;
            }
            catch (Exception e) when (StorageError(e) || e is InvalidDataException or ArgumentException)
            { LastError = "restore_failed"; }
        }
        return null;
    }

    T Decode<T>(string path, Func<T, bool> validate) where T : class
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > MaxFileBytes) throw new InvalidDataException("Save too large");
        var envelope = JsonSerializer.Deserialize<Envelope>(file, Json) ??
            throw new InvalidDataException("Missing envelope");
        // Version 1 used the same typed payload/checksum; version 2 formalizes metadata
        // and bounds. Rewriting on the next successful save completes its migration.
        if (envelope.FormatVersion is not (1 or FormatVersion) ||
            envelope.Checksum is not { Length: 64 } ||
            envelope.Data.ValueKind != JsonValueKind.Object ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(envelope.Checksum),
                Encoding.ASCII.GetBytes(Hash(envelope.Data.GetRawText()))))
            throw new InvalidDataException("Invalid checksum or format");
        var value = envelope.Data.Deserialize<T>(Json);
        if (value == null || !validate(value)) throw new InvalidDataException("Invalid payload");
        return value;
    }

    bool Write<T>(string name, T value, Func<T, bool> validate) where T : class
    {
        var path = PathFor(name);
        var temporary = path + ".tmp";
        try
        {
            var data = JsonSerializer.SerializeToElement(value, Json);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope
            {
                FormatVersion = FormatVersion, SavedAt = DateTimeOffset.UtcNow,
                Data = data, Checksum = Hash(data.GetRawText())
            }, Json);
            if (bytes.Length > MaxFileBytes) throw new InvalidDataException("Save too large");
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }
            _ = Decode(temporary, validate);
            if (File.Exists(path))
            {
                // Never replace a valid backup with an already-corrupted primary.
                var primaryValid = false;
                try { _ = Decode(path, validate); primaryValid = true; }
                catch (Exception e) when (StorageError(e) || e is InvalidDataException) { }
                if (primaryValid)
                    File.Replace(temporary, path, path + ".bak");
                else
                {
                    File.Move(temporary, path, true);
                    if (!File.Exists(path + ".bak")) File.Copy(path, path + ".bak");
                }
            }
            else
            {
                File.Move(temporary, path);
                if (!File.Exists(path + ".bak")) File.Copy(path, path + ".bak");
            }
            LastError = null;
            return true;
        }
        catch (Exception e) when (StorageError(e) || e is InvalidDataException)
        {
            LastError = "save_failed";
            return false;
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception e) when (StorageError(e)) { }
        }
    }
}
