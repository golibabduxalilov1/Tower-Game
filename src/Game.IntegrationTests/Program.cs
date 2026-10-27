using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using TowerGame.Content;
using TowerGame.Core;
using TowerGame.Persistence;

var failures = new List<string>();
var total = 0;
var testRoot = Path.Combine(Path.GetTempPath(), "QalqonTests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testRoot);
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
void Test(string name, Action action)
{
    total++;
    try { action(); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failures.Add(name + ": " + e.Message); Console.WriteLine("FAIL " + name + ": " + e); }
}
GameStore Store(string name) => new(Path.Combine(testRoot, name), Catalog.Maps);
GameSession Battle() => new(Catalog.Maps[0]);
string Json(object value) => JsonSerializer.Serialize(value);

Test("All authored maps construct every mode and difficulty", () =>
{
    foreach (var map in Catalog.Maps)
        foreach (var mode in Enum.GetValues<Mode>())
            foreach (var difficulty in Enum.GetValues<Difficulty>())
            {
                var game = new GameSession(map, mode, difficulty);
                Check(game.Map.Waves.Count == 100, "Missing waves");
                foreach (var route in map.Routes.Where(r => r.Movement == MovementKind.Ground))
                    Check(game.PreviewPath(route.Id)[^1] == route.Points[^1], "Missing ground route");
            }
});
Test("Fresh profile locks later maps and advanced modes", () =>
{
    var store = Store("fresh");
    Check(store.IsMapUnlocked(0) && !store.IsMapUnlocked(1), "Map lock");
    Check(store.IsModeUnlocked(Catalog.Maps[0].Id, Mode.Classic), "Classic");
    Check(!store.IsModeUnlocked(Catalog.Maps[0].Id, Mode.Endless), "Endless lock");
});
Test("Local achievements persist once and reject unknown identifiers", () =>
{
    var store = Store("achievements");
    Check(store.EarnedAchievements.Count == 0, "Unexpected achievement");
    Check(store.UnlockAchievement(TowerGame.Platform.Achievements.FirstBuild), "Unlock failed");
    Check(store.UnlockAchievement(TowerGame.Platform.Achievements.FirstBuild), "Duplicate unlock should be harmless");
    Check(store.EarnedAchievements.Count == 1, "Achievement duplicated");
    Check(!store.UnlockAchievement("UNRECOGNIZED"), "Unknown achievement accepted");
    Check(Store("achievements").EarnedAchievements.Contains(TowerGame.Platform.Achievements.FirstBuild), "Achievement lost on restart");
});
Test("Settings round trip preserves rebindings and language", () =>
{
    var store = Store("settings");
    store.Settings.Language = "en";
    store.Settings.FrameRate = 30;
    store.Settings.Keys["Pause"] = "P";
    Check(store.SaveSettings(), "Save settings failed");
    var loaded = Store("settings");
    Check(loaded.Settings.Language == "en" && loaded.Settings.FrameRate == 30 &&
          loaded.Settings.Keys["Pause"] == "P", "Settings mismatch");
});
Test("Battle snapshot round trip continues deterministically", () =>
{
    var game = Battle();
    Check(game.TryBuild(3, 4, TowerKind.Gatling, out _), "Build failed");
    game.StartWave();
    for (var i = 0; i < 240; i++) game.Step();
    var store = Store("battle");
    Check(store.SaveBattle(game.Capture()), "Save failed");
    var loaded = Store("battle").LoadBattle();
    Check(loaded != null, "Load failed");
    var restored = GameSession.Restore(game.Map, loaded!);
    for (var i = 0; i < 360; i++) { game.Step(); restored.Step(); }
    Check(Json(game.Capture()) == Json(restored.Capture()), "Simulation drift");
});
Test("Corrupt primary restores last valid backup", () =>
{
    var store = Store("backup");
    var game = Battle();
    Check(store.SaveBattle(game.Capture()), "Initial save");
    game.TryBuild(3, 4, TowerKind.Gatling, out _);
    Check(store.SaveBattle(game.Capture()), "Second save");
    File.WriteAllText(Path.Combine(testRoot, "backup", "battle.json"), "{broken");
    var loaded = Store("backup");
    Check(loaded.LoadBattle()?.Money == 150 && loaded.RecoveredBackup, "Backup not recovered");
    Check(loaded.SaveBattle(game.Capture()), "Recovery save failed");
    Check(Store("backup").LoadBattle()?.Money == 110, "Primary not repaired");
});
Test("Tampered data cannot pass checksum", () =>
{
    var store = Store("tamper");
    Check(store.SaveBattle(Battle().Capture()), "Initial save");
    var file = Path.Combine(testRoot, "tamper", "battle.json");
    var doc = JsonNode.Parse(File.ReadAllText(file))!;
    doc["Data"]!["Money"] = 999;
    File.WriteAllText(file, doc.ToJsonString());
    File.Delete(file + ".bak");
    Check(Store("tamper").LoadBattle() == null, "Tampered data accepted");
});
Test("Legacy v1 envelope migrates on next save", () =>
{
    var store = Store("legacy");
    Check(store.SaveBattle(Battle().Capture()), "Initial save");
    var file = Path.Combine(testRoot, "legacy", "battle.json");
    var doc = JsonNode.Parse(File.ReadAllText(file))!;
    doc["FormatVersion"] = 1;
    File.WriteAllText(file, doc.ToJsonString());
    var loaded = Store("legacy").LoadBattle();
    Check(loaded != null && store.SaveBattle(loaded), "Migration failed");
    Check(JsonNode.Parse(File.ReadAllText(file))!["FormatVersion"]!.GetValue<int>() == 2, "Not rewritten");
});
Test("Unsupported version and oversized file rejected", () =>
{
    var store = Store("future");
    Check(store.SaveBattle(Battle().Capture()), "Initial save");
    var file = Path.Combine(testRoot, "future", "battle.json");
    var doc = JsonNode.Parse(File.ReadAllText(file))!;
    doc["FormatVersion"] = 999;
    File.WriteAllText(file, doc.ToJsonString());
    File.Delete(file + ".bak");
    Check(Store("future").LoadBattle() == null, "Future format accepted");
    File.WriteAllText(file, new string(' ', 4 * 1024 * 1024 + 1));
    Check(Store("future").LoadBattle() == null, "Oversized file accepted");
});
Test("Invalid snapshot never replaces previous valid save", () =>
{
    var store = Store("invalid");
    var snapshot = Battle().Capture();
    Check(store.SaveBattle(snapshot), "Initial save");
    snapshot.Money = -1;
    Check(!store.SaveBattle(snapshot), "Invalid save accepted");
    Check(Store("invalid").LoadBattle()?.Money == 150, "Valid save destroyed");
});
Test("Wave 50 unlock waits until completion", () =>
{
    var store = Store("progress");
    var snapshot = Battle().Capture();
    snapshot.Wave = 50;
    snapshot.State = SessionState.Active;
    store.RecordProgress(snapshot);
    Check(!store.IsMapUnlocked(1), "Unlocked before wave completed");
    snapshot.State = SessionState.Intermission;
    store.RecordProgress(snapshot);
    Check(Store("progress").IsMapUnlocked(1), "Map not unlocked");
});
Test("Classic victory unlocks modes and preserves separate records", () =>
{
    var store = Store("victory");
    var snapshot = Battle().Capture();
    snapshot.Wave = 100;
    snapshot.State = SessionState.Victory;
    snapshot.Score = 123456;
    store.RecordProgress(snapshot);
    var loaded = Store("victory");
    Check(loaded.IsModeUnlocked(snapshot.MapId, Mode.Endless), "Mode locked");
    Check(loaded.Profile.Records.Values.Single().Stars == 3, "Stars");
    Check(loaded.Record(snapshot.MapId, Mode.Classic, Difficulty.Hard) == "—", "Record mixed");
});
Test("Clear removes both active save and backup", () =>
{
    var store = Store("clear");
    Check(store.SaveBattle(Battle().Capture()), "Save failed");
    store.ClearBattle();
    Check(!Store("clear").HasSavedBattle, "Backup resurrects ended battle");
});
Test("Write failure is reported without crashing", () =>
{
    var path = Path.Combine(testRoot, "not-a-directory");
    File.WriteAllText(path, "occupied");
    var store = new GameStore(path, Catalog.Maps);
    Check(!store.SaveBattle(Battle().Capture()) && store.LastError != null, "Failure unreported");
});

Test("Uzbek and English tables have identical keys and every enum is named", () =>
{
    var uz = TowerGame.UI.Strings.TableKeys(true).ToHashSet(); var en = TowerGame.UI.Strings.TableKeys(false).ToHashSet();
    Check(uz.SetEquals(en), "Missing: " + string.Join(", ", uz.Except(en).Concat(en.Except(uz))));
    foreach (var name in Enum.GetNames<TowerKind>().Concat(Enum.GetNames<EnemyKind>()).Concat(Enum.GetNames<Mode>())
                 .Concat(Enum.GetNames<Difficulty>()).Concat(Enum.GetNames<TargetPriority>()))
        Check(uz.Contains(name), "Unnamed: " + name);
    foreach (var error in new[] { "not_enough_money", "path_blocked", "invalid_cell", "enemy_occupies_cell", "tower_unavailable", "battle_over", "save_failed" })
        Check(uz.Contains(error), "Untranslated error " + error);
});
Test("Every interface glyph exists in the font atlas", () =>
{
    var atlas = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "font.json"))).RootElement.GetProperty("characters").GetString()!;
    var aliases = "ʻʼ− \n";
    foreach (var uzbek in new[] { true, false })
        foreach (var key in TowerGame.UI.Strings.TableKeys(uzbek))
        {
            TowerGame.UI.Strings.Uzbek = uzbek;
            var text = TowerGame.UI.Strings.T(key);
            var missing = text.Where(c => !atlas.Contains(c) && !aliases.Contains(c)).Distinct().ToArray();
            Check(missing.Length == 0, $"{key}: missing glyphs {string.Join(" ", missing.Select(c => $"U+{(int)c:X4}"))}");
        }
    TowerGame.UI.Strings.Uzbek = true;
});
Test("First launch, structured records and bounded reports", () =>
{
    var store = Store("first");
    Check(store.IsFirstLaunch, "first launch not detected");
    Check(store.SaveSettings() && !Store("first").IsFirstLaunch, "settings not persisted");
    Check(store.Best(Catalog.Maps[0].Id, Mode.Classic, Difficulty.Normal) == null, "phantom record");
    var snapshot = Battle().Capture(); snapshot.Wave = 12; snapshot.State = SessionState.Intermission; snapshot.Score = 777;
    store.RecordProgress(snapshot);
    Check(store.Best(Catalog.Maps[0].Id, Mode.Classic, Difficulty.Normal) == (12, 777L, 0), "record mismatch");
    Check(store.AppendReport("bench line\n") && File.ReadAllText(Path.Combine(testRoot, "first", "performance-report.txt")) == "bench line\n", "report");
});
Test("Campaign grid is sixteen columns by twelve rows on every map", () =>
{
    Check(Catalog.Version == "3.2.0", "content version " + Catalog.Version);
    Check(Catalog.Maps.All(m => m.Width == 16 && m.Height == 12 && m.Cells.Length == 192), "Grid size mismatch");
    Check(Catalog.Maps.All(m => !m.Cells.Contains(CellType.Rock)), "Campaign still contains blocked rock cells");
    var marsh = Catalog.Get("marsh-border");
    Check(marsh.Cells.Count(c => c == CellType.Platform) >= 20, "marsh rail pads missing");
});
Test("Old board battles are rejected without relocating towers", () =>
{
    var snapshot = Battle().Capture();
    Check(snapshot.MapWidth == 16 && snapshot.MapHeight == 12, "Snapshot grid metadata missing");
    snapshot.MapWidth = 11; snapshot.MapHeight = 8;
    try { GameSession.Restore(Catalog.Maps[0], snapshot); throw new Exception("Legacy grid was accepted"); }
    catch (InvalidDataException) { }
});
Test("Compact Endless retains campaign health pacing after wave one hundred", () =>
{
    foreach (var map in Catalog.Maps)
    {
        var game = new GameSession(map, Mode.Endless);
        var campaign = game.GetWave(100).Groups[0].HealthMultiplier;
        var endless = game.GetWave(101).Groups[0].HealthMultiplier;
        Check(endless > campaign && endless / campaign < 1.04, "Health discontinuity on " + map.Id);
    }
});
Test("Malformed save envelope recovers valid settings backup", () =>
{
    var store = Store("malformed-envelope");
    store.Settings.Language = "en"; Check(store.SaveSettings(), "Initial save failed");
    var file = Path.Combine(testRoot, "malformed-envelope", "settings.json");
    File.WriteAllText(file, "{\"FormatVersion\":2,\"Data\":{},\"Checksum\":null}");
    var loaded = Store("malformed-envelope");
    Check(loaded.Settings.Language == "en" && loaded.RecoveredBackup, "Backup was not restored");
    File.WriteAllText(file, "{\"FormatVersion\":2,\"Checksum\":\"" + new string('0', 64) + "\"}");
    loaded = Store("malformed-envelope");
    Check(loaded.Settings.Language == "en" && loaded.RecoveredBackup, "Missing Data not handled");
});
Test("Guide-paced waves use bursts, scout aircraft and escorted bosses", () =>
{
    foreach(var map in Catalog.Maps)
    {
        Check(map.HealthScale>=.8,"Hidden enemy health discount too large");
        foreach(var wave in map.Waves.Where(w=>w.HasBoss))
        {
            var boss=wave.Groups.Single(g=>Balance.Enemy(g.Kind).Boss);
            Check(wave.Groups.Any(g=>!Balance.Enemy(g.Kind).Boss&&g.Delay>boss.Delay),"Boss has no following escort");
        }
        var wave1=map.Waves[0].Groups.OrderBy(g=>g.Delay).ToArray();
        Check(wave1[1].Delay-wave1[0].Delay < .5,"Opening push is not grouped");
        Check(wave1[4].Delay-wave1[3].Delay > 1,"Missing recovery gap");
    }
    var valley=Catalog.Maps[0];
    Check(valley.Waves[5].Groups.Count(g=>Balance.Enemy(g.Kind).Movement==MovementKind.Air)<=3,"First aircraft arrive as a wall");
});
Console.WriteLine($"\n{total - failures.Count}/{total} integration tests passed.");
Console.WriteLine("Temporary test data: " + testRoot);
Environment.ExitCode = failures.Count == 0 ? 0 : 1;
