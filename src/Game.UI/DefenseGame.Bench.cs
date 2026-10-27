using System.Diagnostics;
using Microsoft.Xna.Framework;
using TowerGame.Core;
using TowerGame.Rendering;
using static TowerGame.UI.Strings;

namespace TowerGame.UI;

public sealed partial class DefenseGame
{
    const double BenchSeconds = 20;
    GameSession? benchSession;
    int benchNextId;
    bool benchDone;
    double benchTime;
    readonly List<double> benchFrames = new(4096);
    readonly Stopwatch benchClock = new();
    string benchReport = "";
    bool iconOnly;

    /// <summary>Application icon drawn by the same code as the menu emblem (exported with --scene=icon).</summary>
    void DrawIcon()
    {
        var size = Math.Min(W, H);
        var r = new Rectangle((int)(W / 2 - size / 2), (int)(H / 2 - size / 2), (int)size, (int)size);
        for (int i = 0; i < 24; i++)
            p.Rect(r.X, r.Y + r.Height * i / 24f, r.Width, r.Height / 24f + 1, Color.Lerp(new Color(25, 62, 64), new Color(8, 25, 31), i / 23f));
        for (int i = 1; i < 6; i++) { p.Rect(r.X + r.Width * i / 6f, r.Y, 1.5f, r.Height, new Color(38, 82, 83)); p.Rect(r.X, r.Y + r.Height * i / 6f, r.Width, 1.5f, new Color(38, 82, 83)); }
        DrawShield(new Vector2(r.Center.X, r.Center.Y + size * .02f), size * .4f);
    }

    /// <summary>Specification load test: 300 enemies, 200 towers, up to 600 shots/effects at 2x, rendered.</summary>
    void StartBench()
    {
        benchSession = StressScenario.Create();
        benchNextId = 10_000_000;
        StressScenario.Refill(benchSession, ref benchNextId);
        benchDone = false; benchTime = 0; benchFrames.Clear(); benchReport = "";
        screen = Screen.Bench;
        benchClock.Restart();
    }

    void UpdateBench()
    {
        if (benchSession is not { } s || benchDone) return;
        var frameMs = benchClock.Elapsed.TotalMilliseconds; benchClock.Restart();
        if (benchTime > .5) benchFrames.Add(frameMs); // skip warm-up
        benchTime += TargetElapsedTime.TotalSeconds;
        s.Step(); s.Step();
        if (s.Tick % 10 == 0) StressScenario.Refill(s, ref benchNextId);
        if (benchTime < BenchSeconds && s.State == SessionState.Active) return;
        benchDone = true;
        var sorted = benchFrames.Order().ToArray();
        double avg = sorted.Length == 0 ? 0 : sorted.Average(), p95 = sorted.Length == 0 ? 0 : sorted[(int)(sorted.Length * .95)], worst = sorted.Length == 0 ? 0 : sorted[^1];
        var target = 1000.0 / (Settings.FrameRate == 30 ? 30 : 60);
        var slow = sorted.Count(f => f > target * 1.5);
        benchReport = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | {Environment.OSVersion.Platform} {(Environment.Is64BitProcess ? "x64/arm64" : "32-bit")} | " +
            $"{GraphicsDevice.PresentationParameters.BackBufferWidth}x{GraphicsDevice.PresentationParameters.BackBufferHeight} | target {Settings.FrameRate} FPS | " +
            $"frames {sorted.Length} | avg {avg:0.00} ms ({1000 / Math.Max(.01, avg):0} FPS) | p95 {p95:0.00} ms | worst {worst:0.00} ms | over 1.5x budget {slow} | " +
            $"enemies {s.Enemies.Count} towers {s.Towers.Count} shots {s.Projectiles.Count} fx {s.Effects.Count} | state {s.State}";
        store.AppendReport(benchReport + Environment.NewLine);
    }

    void DrawBench()
    {
        if (benchSession is not { } s) return;
        field.Map(s.Map, new Rectangle(safe.X, safe.Y, safe.Width, safe.Height), s, false, time: clock);
        var r = new Rectangle(safe.X + 16, safe.Y + 16, (int)Math.Min(W - 32, 760), benchDone ? 230 : 70);
        Panel(r);
        p.Text(T("bench_title"), r.X + 20, r.Y + 14, 22, Painter.Mint);
        if (!benchDone)
        {
            p.Right($"{Math.Min(BenchSeconds, benchTime):0}/{BenchSeconds:0} s · FPS {fps:0}", r.Right - 20, r.Y + 18, 18);
            return;
        }
        p.Paragraph(benchReport.Replace(" | ", "\n"), new Rectangle(r.X + 20, r.Y + 48, r.Width - 40, 140), 13, Painter.Ink);
        p.Text(T("bench_saved"), r.X + 20, r.Bottom - 28, 13, Painter.Muted);
        var b = new Rectangle(r.Right - 160, r.Bottom - (int)hit - 12, 140, (int)hit);
        if (Button(b, T("back")))
        {
            benchSession = null;
            if (options.Bench) Exit(); else screen = Screen.Settings;
        }
        if (options.Bench && options.Screenshot == null) { Console.WriteLine(benchReport); Exit(); }
    }

    /// <summary>Deterministic scenes for automated screenshots (--screenshot=file --scene=name).</summary>
    void PrepareScene(string scene)
    {
        mapIndex = Math.Clamp(options.MapIndex, 0, maps.Count - 1);
        var map = maps[mapIndex];
        for (int i = 0; i < 400; i++) UpdateAttract();
        switch (scene)
        {
            case "menu": screen = Screen.Menu; break;
            case "maps": screen = Screen.Maps; break;
            case "achievements": screen = Screen.Achievements; break;
            case "setup": screen = Screen.Setup; mode = Mode.Extended; break;
            case "settings": OpenSettings(Screen.Menu); break;
            case "help": returnScreen = Screen.Menu; screen = Screen.Help; break;
            case "help2": returnScreen = Screen.Menu; screen = Screen.Help; helpTab = 1; break;
            case "help3": returnScreen = Screen.Menu; screen = Screen.Help; helpTab = 2; break;
            case "restore": screen = Screen.RestoreError; break;
            case "icon": iconOnly = true; screen = Screen.Menu; break;
            case "armory": screen = Screen.Armory; break;
            case "soldiers": screen = Screen.Armory; break;
            case "units": screen = Screen.Armory; break;
            default:
                var game = new GameSession(map, Mode.Extended, Difficulty.Normal);
                var bot = new AutoPlayer(game);
                var waves = scene == "prep" ? 0 : scene == "victory" ? 100 : 9;
                for (int w = 0; w < waves; w++) bot.PlayWave();
                bot.Act();
                StartBattle(game);
                if (scene != "prep") { game.StartWave(); for (int i = 0; i < 420; i++) game.Step(); }
                if (scene == "pause") OpenPause();
                if (scene == "tower") selectedTower = game.Towers.OrderByDescending(t => t.Level).First().Id;
                if (scene == "build")
                {
                    buildKind = game.AvailableTowers.FirstOrDefault(k => Balance.Tower(k).Cost <= game.Money);
                    var candidate = Enumerable.Range(0, map.Cells.Length).Select(i => new Cell(i % map.Width, i / map.Width))
                        .OrderBy(c => Math.Abs(c.X - map.Width / 2) + Math.Abs(c.Y - map.Height / 2))
                        .Where(c => game.CanBuild(c.X, c.Y, buildKind.Value, out _)).Select(c => (Cell?)c).FirstOrDefault();
                    SetPreview(game, candidate, buildKind.Value);
                }
                if (scene == "victory" && game.State == SessionState.Victory) HandleResult(game);
                if (scene == "result") { dialog = Dialog.Result; }
                if (scene == "zoom") { ComputeLayout(); ComputeBattleLayout(); ZoomAt(new Vector2(board.Center.X, board.Center.Y), 2f); }
                break;
        }
    }
}
