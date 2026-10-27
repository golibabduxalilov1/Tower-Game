using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TowerGame.Core;
using TowerGame.Platform;
using TowerGame.Rendering;
using static TowerGame.UI.Strings;

namespace TowerGame.UI;

public sealed partial class DefenseGame
{
    GameSession? attract; AutoPlayer? attractBot;
    string? rebinding;
    int helpTab;
    static readonly string[] KeyActions = ["Pause", "Speed", "Upgrade", "Sell", "Tower1", "Tower2", "Tower3", "Tower4", "Tower5", "Tower6"];

    // ---------------------------------------------------------------- attract-mode background

    void UpdateAttract()
    {
        if (attract == null || attract.State is SessionState.Victory or SessionState.Defeat || attract.Wave > 30)
        {
            attract = new GameSession(maps[(int)(clock / 60) % maps.Count], Mode.Extended, Difficulty.Easy);
            attractBot = new AutoPlayer(attract);
        }
        if (attract.State is SessionState.Preparation or SessionState.Intermission) { attractBot!.Act(); attract.StartWave(); }
        attract.Step();
        if (attract.Tick % 90 == 0) attractBot!.Act(2);
    }

    void DrawAttractBackground()
    {
        if (attract != null) field.Map(attract.Map, new Rectangle(0, 0, (int)W, (int)H), attract, false, 1.15f, time: clock);
        p.Rect(0, 0, W, H, new Color(10, 28, 28) * (screen is Screen.Menu ? .24f : .74f));
        if (screen == Screen.Menu)
        {
            for (int i = 0; i < 160; i++)
            {
                var width = W * .58f / 160;
                p.Rect(i * width, 0, width + 1, H, new Color(9, 22, 28) * (.92f - i * .00456f));
            }
        }
        p.Rect(0, 0, W, 7, new Color(16, 29, 32));
        p.Rect(0, H - 8, W, 8, new Color(16, 29, 32));
        p.Line(new Vector2(0, H - 9), new Vector2(W, H - 9), Painter.Gold * .7f, 2);
    }

    // ---------------------------------------------------------------- main menu

    void DrawMenu()
    {
        DrawMenuScene();
        bool wideTouch = host.IsTouch && H < 600;
        var left = safe.X + Math.Max(wideTouch ? 24 : 34, safe.Width * (wideTouch ? .035f : .065f));
        float titleY = safe.Y + 86;
        var ice = new Color(232, 242, 255);
        var steel = new Color(135, 165, 203);
        p.Rect(left, titleY - 26, 32, 3, Painter.Gold);
        p.Text(Uzbek ? "CHEGARA MUDOFAASI" : "FRONTIER DEFENSE", left + 44, titleY - 33, 13, steel);
        var titleWidth = Math.Min(wideTouch ? 330 : 380, safe.Width * (wideTouch ? .30f : .37f));
        var titleSize = Math.Min(76, 76 * titleWidth / Math.Max(1, p.MeasureDisplay(T("title"), 76)));
        p.Display(T("title"), left - 3, titleY, titleSize, ice);
        p.Text(T("subtitle"), left + 2, titleY + 92, 19, steel);
        int bw = (int)Math.Min(wideTouch ? 330 : 380, safe.Width * (wideTouch ? .30f : .37f)), bh = (int)Math.Max(hit, 66);
        int y = (int)(titleY + 160);
        if (store.HasSavedBattle)
        {
            if (SymbolButton(new Rectangle((int)left, y, bw, bh), UiSymbol.Play, T("continue"), Style.Primary, caption: T("continue"))) ContinueBattle();
            y += bh + 12;
        }
        if (SymbolButton(new Rectangle((int)left, y, bw, bh), UiSymbol.Play, T("play"), store.HasSavedBattle ? Style.Normal : Style.Primary, caption: T("play"))) screen = Screen.Maps;
        y += bh + 28;
        int utility = (int)Math.Max(hit, 54), gap = Math.Max(8, (bw - utility * 5) / 4);
        (UiSymbol Symbol, string Label)[] controls = [(UiSymbol.Settings, T("settings")), (UiSymbol.Sound, "sound"), (UiSymbol.Music, "music"), (UiSymbol.Trophy, "achievements"), (UiSymbol.Info, T("help"))];
        for (int i = 0; i < controls.Length; i++)
        {
            var (symbol, label) = controls[i];
            var r = wideTouch && symbol == UiSymbol.Info
                ? new Rectangle(safe.Right - utility - 20, safe.Y + 18, utility, utility)
                : new Rectangle((int)left + i * (utility + gap), y, utility, utility);
            bool on = symbol == UiSymbol.Sound && Settings.EffectsVolume > 0 || symbol == UiSymbol.Music && Settings.MusicVolume > 0;
            if (SymbolButton(r, symbol, label, on ? Style.Selected : Style.Normal))
            {
                switch (symbol)
                {
                    case UiSymbol.Settings: OpenSettings(Screen.Menu); break;
                    case UiSymbol.Sound: ToggleSound(); break;
                    case UiSymbol.Music: ToggleMusic(); break;
                    case UiSymbol.Trophy: screen = Screen.Achievements; break;
                    case UiSymbol.Info: returnScreen = Screen.Menu; screen = Screen.Help; break;
                }
            }
            var caption = symbol switch { UiSymbol.Sound => T("effects"), UiSymbol.Music => T("music"), UiSymbol.Trophy => Uzbek ? "Yutuqlar" : "Awards", _ => label };
            p.Center(caption, new Rectangle(r.X - 9, r.Bottom + 9, r.Width + 18, 23), 11, steel);
        }
        if (host.CanQuit && SymbolButton(new Rectangle(safe.Right - 70, safe.Y + 20, 50, 50), UiSymbol.Exit, T("quit"))) Exit();
        if (!wideTouch || !store.HasSavedBattle)
            p.Text(Uzbek ? "8 XARITA   /   8 QUROL   /   100 TO‘LQIN" : "8 MAPS   /   8 TOWERS   /   100 WAVES", left + 2, safe.Bottom - 40, 12, steel);
    }

    void DrawMenuScene()
    {
        if (p.AssetCover("menu:scene", new Rectangle(0, 0, (int)W, (int)H))) return;
        DrawNavyMenuBackground();
        p.Asset("menu:duel-native-hd", new Rectangle(safe.X + (int)(safe.Width * .42f), safe.Y,
            (int)(safe.Width * .57f), safe.Height));
    }

    void DrawNavyMenuBackground()
    {
        // Opaque contiguous bands avoid the old striped translucent meadow overlay.
        for (int y = 0; y < (int)H; y += 4)
            p.Rect(0, y, W, Math.Min(4, H - y), Color.Lerp(new Color(10, 22, 42), new Color(17, 37, 65), y / H));

    }

    /// <summary>Code-drawn emblem used on the menu (same geometry as the store icon).</summary>
    void DrawShield(Vector2 c, float r)
    {
        var outline = new[] { new Vector2(0, -1), new Vector2(.62f, -.76f), new Vector2(.62f, 0), new Vector2(.42f, .5f), new Vector2(0, .86f), new Vector2(-.42f, .5f), new Vector2(-.62f, 0), new Vector2(-.62f, -.76f) }
            .Select(v => c + v * r).ToArray();
        for (int i = 0; i < 12; i++) p.Polygon(outline.Select(v => c + (v - c) * (1 - i * .012f)).ToArray(), new Color(28, 60, 60), 2);
        p.Polygon(outline, Painter.Mint, Math.Max(3, r * .05f));
        var tower = new Rectangle((int)(c.X - r * .3f), (int)(c.Y - r * .15f), (int)(r * .6f), (int)(r * .5f));
        p.Box(tower, Painter.Mint, 3);
        p.Rect(c.X - r * .15f, c.Y - r * .45f, r * .3f, r * .32f, Painter.Mint);
        p.Rect(c.X - r * .07f, c.Y - r * .62f, r * .14f, r * .2f, Painter.Mint);
        p.Box(new Rectangle((int)(c.X - r * .1f), (int)(c.Y + r * .08f), (int)(r * .2f), (int)(r * .27f)), Painter.Backdrop, r * .08f);
        p.Line(c + new Vector2(-r * .32f, r * .48f), c + new Vector2(r * .32f, r * .48f), Painter.Gold, Math.Max(3, r * .05f));
    }

    void ContinueBattle()
    {
        var snapshot = store.LoadBattle();
        var map = snapshot == null ? null : maps.FirstOrDefault(m => m.Id == snapshot.MapId);
        if (snapshot == null || map == null) { screen = Screen.RestoreError; return; }
        try
        {
            var game = GameSession.Restore(map, snapshot);
            mapIndex = maps.ToList().IndexOf(map); mode = game.Mode; difficulty = game.Difficulty;
            StartBattle(game);
            if (game.State != SessionState.Paused) OpenPause(); else dialog = Dialog.Pause;
        }
        catch (Exception e) when (e is InvalidDataException or ArgumentException) { screen = Screen.RestoreError; }
    }

    // ---------------------------------------------------------------- map selection

    void DrawMaps()
    {
        DrawWorldMap();
        if (BackButton()) { screen = Screen.Menu; return; }
        p.Display(T("choose_map"), safe.X + 40 + Math.Max(130, hit * 2.4f), safe.Y + 18, 32, Painter.Gold);
        var area = new Rectangle(safe.X + 55, safe.Y + (int)hit + 58, safe.Width - 110, safe.Height - (int)hit - 112);
        Vector2[] positions = [new(.08f,.67f),new(.27f,.71f),new(.43f,.53f),new(.65f,.70f),new(.87f,.53f),new(.75f,.22f),new(.48f,.17f),new(.23f,.27f)];
        Vector2 At(int i) => new(area.X + positions[i].X * area.Width, area.Y + positions[i].Y * area.Height);
        for (int i = 0; i < maps.Count - 1; i++)
        {
            var start = At(i); var end = At(i + 1); var length = Vector2.Distance(start, end);
            for (float d = 0; d < length; d += 17) p.Circle(Vector2.Lerp(start, end, d / length), 3.3f, new Color(255, 229, 148) * .85f);
        }
        var diameter = (int)Math.Max(hit, Math.Min(H < 590 ? 66 : 88, area.Width * .082f));
        for (int i = 0; i < maps.Count; i++)
        {
            var map = maps[i]; var c = At(i); var unlocked = store.IsMapUnlocked(i);
            var r = new Rectangle((int)c.X - diameter / 2, (int)c.Y - diameter / 2, diameter, diameter);
            if (Button(r, "", unlocked ? Style.Primary : Style.Ghost, id: "map:" + i))
            {
                if (unlocked) { mapIndex = i; if (!store.IsModeUnlocked(map.Id, mode)) mode = Mode.Classic; screen = Screen.Setup; }
                else { Toast(T("locked_map")); audio?.Play("error"); }
            }
            if (unlocked) { p.ControlIcon(UiSymbol.Play, c, diameter * .25f); p.Display((i + 1).ToString(), r.X + 5, r.Y - 12, 21, Painter.Gold); }
            else p.ControlIcon(UiSymbol.Lock, c, diameter * .27f, new Color(194, 201, 168));
            var stars = Enum.GetValues<Difficulty>().Max(d => store.Best(map.Id, Mode.Classic, d)?.Stars ?? 0);
            for (int j = 0; j < 3; j++) p.Star(new Vector2(c.X + (j - 1) * 18, r.Bottom + 7), 8, j < stars ? Painter.Gold : new Color(79, 91, 58), j < stars);
            var label = new Rectangle((int)c.X - 110, r.Bottom + 20, 220, 25);
            p.Box(label, new Color(30, 48, 35) * .8f, 12);
            p.DisplayCenter(Uzbek ? map.NameUz : map.Name, label, H < 590 ? 13 : 16, unlocked ? Painter.Ink : new Color(188, 199, 163));
        }
        p.Center(Uzbek ? "Hududni tanlang · Qulfni ochish: oldingi xaritada 50-to‘lqin" : "Choose a region · Reach wave 50 in the previous region to unlock", new Rectangle(safe.X, safe.Bottom - 39, safe.Width, 28), 16, Painter.Ink);
    }

    void DrawWorldMap()
    {
        p.Rect(0, 0, W, H, new Color(31, 108, 119));
        var y = safe.Y + 55; var h = safe.Height - 70;
        var half = safe.Width / 2;
        var left = new Rectangle(safe.X, y, half, h); var right = new Rectangle(safe.X + half, y, safe.Width - half, h);
        void Half(string key, Rectangle destination) { if (p.GetAsset(key) is { } texture) p.Batch.Draw(texture, destination, Color.White); }
        Half("world:background1", left); Half("world:background2", right);
        var hasWorld = p.GetAsset("world:island1") != null; Half("world:island1", left); Half("world:island2", right);
        if (!hasWorld) { field.Map(maps[0], new Rectangle(safe.X + 40, y + 10, safe.Width - 80, h - 20)); p.Rect(0, 0, W, H, new Color(26, 64, 37) * .3f); }
        p.Rect(0, 0, W, 83, new Color(17, 44, 44) * .65f);
    }

    // ---------------------------------------------------------------- mode / difficulty

    void DrawSetup()
    {
        DrawAttractBackground();
        var map = maps[mapIndex];
        var modal = dialog == Dialog.Overwrite;
        input.Blocked = modal;
        if (BackButton()) { screen = Screen.Maps; input.Blocked = false; return; }
        var titleX = safe.X + 40 + Math.Max(130, hit * 2.4f);
        p.Display(Uzbek ? map.NameUz : map.Name, titleX, safe.Y + 18, H < 590 ? 25 : 32, Painter.Gold);
        if (H > 590) p.Right($"{(Uzbek ? "HUDUD" : "REGION")} {mapIndex + 1:00} / 08", safe.Right - 24, safe.Y + 27, 14, Painter.Muted);
        var compact = H < 590;
        var top = (int)(safe.Y + hit + 34);
        var leftW = (int)(safe.Width * (compact ? .47f : .58f));
        var left = new Rectangle(safe.X + 20, top, leftW - 24, safe.Bottom - top - 22);
        Panel(left, 14);
        var preview = new Rectangle(left.X + 9, left.Y + 9, left.Width - 18, (int)Math.Min(left.Height - (compact ? 110 : 130), left.Width * .77f));
        field.Map(map, preview);
        p.Polygon([new(preview.X, preview.Y), new(preview.Right, preview.Y), new(preview.Right, preview.Bottom), new(preview.X, preview.Bottom)], new Color(151, 133, 82), 1.5f);
        var descriptionY = preview.Bottom + 12;
        p.Paragraph(Catalog(map.Id), new Rectangle(left.X + 18, descriptionY, left.Width - 36, compact ? 42 : 54), compact ? 13 : 16, Painter.Ink);
        var stars = Enum.GetValues<Difficulty>().Max(d => store.Best(map.Id, Mode.Classic, d)?.Stars ?? 0);
        var crestY = left.Bottom - 30;
        p.Text(Uzbek ? "KAMPANIYA YULDUZLARI" : "CAMPAIGN STARS", left.X + 18, crestY - 3, 12, Painter.Muted);
        for (int i = 0; i < 3; i++) p.Star(new Vector2(left.Right - 28 - (2 - i) * 27, crestY + 6), 10, i < stars ? Painter.Gold : new Color(84, 96, 92), i < stars);

        var x = safe.X + leftW + 20; var w = safe.Right - 20 - x;
        var y = top;
        var right = new Rectangle(x - 8, top, w + 8, safe.Bottom - top - 22);
        Panel(right, 14); x += 10; w -= 28; y += 16;
        var rowH = (int)Math.Max(hit, compact ? 48 : 54);
        p.Text(T("mode").ToUpperInvariant(), x, y, 13, Painter.Gold); y += 24;
        if (compact)
        {
            var mw = (w - 12) / 3;
            foreach (var m in Enum.GetValues<Mode>())
            {
                var unlocked = store.IsModeUnlocked(map.Id, m);
                var r = new Rectangle(x + (int)m * (mw + 6), y, mw, rowH);
                if (Button(r, T(m.ToString()), m == mode ? Style.Selected : Style.Normal, unlocked, size: 14, id: "mode:" + m)) mode = m;
                if (!unlocked) p.Lock(new Vector2(r.Right - 12, r.Y + 12), 6, Painter.Muted);
            }
            y += rowH + 9;
            p.Text(T(mode + "_desc"), x, y, 12, Painter.Muted); y += 26;
        }
        else
        {
            foreach (var m in Enum.GetValues<Mode>())
            {
                var unlocked = store.IsModeUnlocked(map.Id, m);
                var r = new Rectangle(x, y, w, rowH);
                if (Button(r, "", m == mode ? Style.Selected : Style.Normal, unlocked, id: "mode:" + m)) mode = m;
                p.Display(T(m.ToString()), r.X + 18, r.Y + 7, 18, unlocked ? Painter.Ink : Painter.Muted, outline: false);
                p.Text(unlocked ? T(m + "_desc") : T("mode_locked"), r.X + 18, r.Y + 31, 12, Painter.Muted);
                if (!unlocked) p.Lock(new Vector2(r.Right - 24, r.Center.Y), 10, Painter.Muted);
                y += rowH + 7;
            }
            y += 4;
        }
        p.Text(T("difficulty").ToUpperInvariant(), x, y, 13, Painter.Gold); y += 23;
        var dw = (w - 14) / 3;
        foreach (var d in Enum.GetValues<Difficulty>())
        {
            var r = new Rectangle(x + (int)d * (dw + 7), y, dw, rowH);
            if (Button(r, T(d.ToString()), d == difficulty ? Style.Selected : Style.Normal, size: compact ? 15 : 18)) difficulty = d;
        }
        y += rowH + 8;
        p.Text(T(difficulty + "_desc"), x, y, compact ? 12 : 14, Painter.Muted); y += compact ? 22 : 28;
        var towers = mode == Mode.Classic ? new[] { TowerKind.Gatling, TowerKind.Goo, TowerKind.Missile, TowerKind.Tesla } : new[] { TowerKind.Gatling, TowerKind.Goo, TowerKind.Missile, TowerKind.Tesla }.Concat(map.SpecialTowers).ToArray();
        var start = new Rectangle(x, safe.Bottom - rowH - 36, w, rowH);
        var arsenalRoom = start.Y - y - 12;
        if (arsenalRoom >= 44)
        {
            var icon = Math.Min(compact ? 48 : 64, (w - 12) / (float)towers.Length);
            for (int i = 0; i < towers.Length; i++)
                WeaponIcon(towers[i], new Rectangle((int)(x + i * (icon + 2)), y, (int)icon, (int)icon), 2);
            y += (int)icon + 4;
        }
        if (y + 20 < start.Y - 8)
        {
            var best = store.Best(map.Id, mode, difficulty);
            p.Text(best is { } bestValue ? $"{T("best")}: {T("wave")} {bestValue.Wave} · {Short(bestValue.Score)}" : T("no_record"), x, y, 13, Painter.Muted);
        }
        if (!compact && start.Y - y > 90)
        {
            var planY = start.Y - 79;
            p.Line(new Vector2(x, planY), new Vector2(x + w, planY), new Color(140, 123, 77) * .6f, 1);
            p.Text(Uzbek ? "HUJUM YO‘NALISHLARI" : "ATTACK FRONTS", x + 2, planY + 13, 11, Painter.Gold);
            var fronts = map.Routes.Select(r => r.Movement).Distinct().ToArray();
            for (int i = 0; i < fronts.Length; i++)
            {
                var kind = fronts[i] == MovementKind.Ground ? EnemyKind.Infantry : fronts[i] == MovementKind.Air ? EnemyKind.Helicopter : EnemyKind.RailCart;
                field.EnemyIcon(new Vector2(x + 20 + i * 53, planY + 52), 39, kind, clock);
            }
            p.Display(mode == Mode.Endless ? (Uzbek ? "CHEKSIZ" : "ENDLESS") : map.Waves.Count.ToString(), x + w - 97, planY + 16, mode == Mode.Endless ? 15 : 25, Painter.Gold);
            p.Text(T("wave").ToUpperInvariant(), x + w - 98, planY + 49, 11, Painter.Muted);
        }
        if (Button(start, T("start"), Style.Primary, size: compact ? 22 : 25))
        {
            if (store.HasSavedBattle) dialog = Dialog.Overwrite;
            else StartBattle(new GameSession(map, mode, difficulty));
        }
        input.Blocked = false;
        if (modal) DrawOverwrite(map);
    }

    static string Catalog(string id) => TowerGame.Content.Catalog.Description(id, Uzbek);

    void DrawOverwrite(MapDefinition map)
    {
        Dim();
        var r = new Rectangle((int)(W / 2 - 260), (int)(H / 2 - 110), 520, 220);
        Panel(r);
        p.Paragraph(T("overwrite"), new Rectangle(r.X + 28, r.Y + 28, r.Width - 56, 100), 20);
        var bw = (r.Width - 72) / 2; var bh = (int)Math.Max(hit, 50);
        if (Button(new Rectangle(r.X + 28, r.Bottom - bh - 24, bw, bh), T("no"))) dialog = Dialog.None;
        if (Button(new Rectangle(r.Right - 28 - bw, r.Bottom - bh - 24, bw, bh), T("yes"), Style.Danger))
        {
            store.ClearBattle(); dialog = Dialog.None;
            StartBattle(new GameSession(map, mode, difficulty));
        }
    }

    // ---------------------------------------------------------------- settings

    void OpenSettings(Screen from) { returnScreen = from; screen = Screen.Settings; }

    void CloseSettings()
    {
        rebinding = null;
        if (!store.SaveSettings()) Toast(T("save_failed"));
        ApplyFrameRate();
        screen = returnScreen;
    }

    void DrawSettings()
    {
        if (returnScreen == Screen.Battle && session != null) { DrawBoardOnly(); Dim(); }
        else DrawAttractBackground();
        if (BackButton()) { CloseSettings(); return; }
        p.Display(T("settings"), safe.X + 40 + Math.Max(130, hit * 2.4f), safe.Y + 20, 30, Painter.Gold);
        var columns = host.IsTouch ? 2 : 3;
        var top = (int)(safe.Y + hit + 44); var gap = 20;
        var cw = (int)((safe.Width - 48 - gap * (columns - 1)) / columns);
        var rowH = (int)Math.Max(Math.Ceiling(hit) + 6, 46);
        var x = safe.X + 24; var y = top;

        void Row(string label) { p.Text(label, x, y + rowH / 2f - 13, 18, Painter.Ink); }
        bool Toggle(string label, bool value)
        {
            Row(label);
            var r = new Rectangle(x + cw - 150, y, 150, rowH - 6);
            if (Button(r, T(value ? "on" : "off"), value ? Style.Selected : Style.Normal)) value = !value;
            y += rowH + 4; return value;
        }
        float Volume(string label, float value)
        {
            Row(label);
            var minus = new Rectangle(x + cw - 170, y, rowH - 6, rowH - 6);
            var plus = new Rectangle(x + cw - rowH + 6, y, rowH - 6, rowH - 6);
            if (Button(minus, "−")) value = MathF.Round(Math.Max(0, value - .1f), 2);
            if (Button(plus, "+")) value = MathF.Round(Math.Min(1, value + .1f), 2);
            var bar = new Rectangle(minus.Right + 8, y + rowH / 2 - 8, plus.X - minus.Right - 16, 10);
            p.Box(bar, Painter.PanelLight, 5); p.Box(new Rectangle(bar.X, bar.Y, (int)(bar.Width * value), bar.Height), Painter.Mint, 5);
            p.Center($"{value * 100:0}%", new Rectangle(bar.X, bar.Bottom + 2, bar.Width, 18), 13, Painter.Muted);
            y += rowH + 4; return value;
        }

        // Column 1: language and audio.
        Row(T("language"));
        var half = 72;
        if (Button(new Rectangle(x + cw - half * 2 - 6, y, half, rowH - 6), "O‘zb", Uzbek ? Style.Selected : Style.Normal)) { Settings.Language = "uz"; Uzbek = true; }
        if (Button(new Rectangle(x + cw - half, y, half, rowH - 6), "Eng", !Uzbek ? Style.Selected : Style.Normal)) { Settings.Language = "en"; Uzbek = false; }
        y += rowH + 4;
        Settings.MasterVolume = Volume(T("master"), Settings.MasterVolume);
        Settings.MusicVolume = Volume(T("music"), Settings.MusicVolume);
        Settings.EffectsVolume = Volume(T("effects"), Settings.EffectsVolume);
        Settings.Muted = Toggle(T("mute"), Settings.Muted);

        // Column 2: display.
        x += cw + gap; y = top;
        Settings.ShowPath = Toggle(T("show_path"), Settings.ShowPath);
        Settings.ScreenShake = Toggle(T("shake"), Settings.ScreenShake);
        Settings.Flashes = Toggle(T("flashes"), Settings.Flashes);
        Row(T("fps"));
        if (Button(new Rectangle(x + cw - half * 2 - 6, y, half, rowH - 6), "30", Settings.FrameRate == 30 ? Style.Selected : Style.Normal)) { Settings.FrameRate = 30; ApplyFrameRate(); }
        if (Button(new Rectangle(x + cw - half, y, half, rowH - 6), "60", Settings.FrameRate == 60 ? Style.Selected : Style.Normal)) { Settings.FrameRate = 60; ApplyFrameRate(); }
        y += rowH + 4;
        if (Button(new Rectangle(x, y + 10, cw, rowH - 6), T("bench_run"), Style.Ghost)) { returnScreen = Screen.Settings; StartBench(); return; }

        // Column 3: key bindings (desktop).
        if (!host.IsTouch)
        {
            x += cw + gap; y = top;
            p.Text(T("keys"), x, y, 18, Painter.Muted); y += 30;
            var kh = (int)Math.Min(38, (safe.Bottom - y - 70) / KeyActions.Length - 4);
            foreach (var action in KeyActions)
            {
                p.Text(T(action), x, y + kh / 2f - 11, 16);
                var r = new Rectangle(x + cw - 130, y, 130, kh);
                var label = rebinding == action ? "…" : InputState.KeyLabel(Settings.Keys.GetValueOrDefault(action, "?"));
                if (Button(r, label, rebinding == action ? Style.Selected : Style.Normal, size: 16, id: "bind:" + action)) rebinding = action;
                y += kh + 4;
            }
            if (Button(new Rectangle(x, y + 6, cw, kh), T("reset_keys"), Style.Ghost, size: 16)) { Settings.Keys = new GameSettings().Keys; rebinding = null; }
            if (rebinding != null) p.Text(T("press_key"), x, y + kh + 18, 15, Painter.Gold);
        }
    }

    void FinishRebind(Keys key)
    {
        var action = rebinding!; rebinding = null;
        if (key == Keys.Escape) return;
        var name = key.ToString();
        if (!name.All(char.IsAsciiLetterOrDigit) || name.Length > 32 || key is Keys.F3 or Keys.F11) { audio?.Play("error"); return; }
        // Swap with any action already using this key so bindings stay unique.
        var old = Settings.Keys.GetValueOrDefault(action);
        foreach (var other in KeyActions) if (other != action && Settings.Keys.GetValueOrDefault(other) == name && old != null) Settings.Keys[other] = old;
        Settings.Keys[action] = name;
    }

    // ---------------------------------------------------------------- help

    void DrawHelp()
    {
        if (returnScreen == Screen.Battle && session != null) { DrawBoardOnly(); Dim(); }
        else DrawAttractBackground();
        if (BackButton()) { screen = returnScreen; return; }
        string[] tabs = ["help_towers", "help_enemies", "help_rules"];
        var tx = safe.X + 40 + (int)Math.Max(130, hit * 2.4f);
        var tw = (int)Math.Min(200, (safe.Right - tx - 20) / 3f);
        for (int i = 0; i < tabs.Length; i++)
            if (Button(new Rectangle(tx + i * (tw + 8), safe.Y + 16, tw, (int)hit), T(tabs[i]), i == helpTab ? Style.Selected : Style.Normal)) helpTab = i;
        var area = new Rectangle(safe.X + 20, (int)(safe.Y + hit + 32), safe.Width - 40, (int)(safe.Bottom - hit - 52 - safe.Y));
        Panel(area);
        area.Inflate(-16, -14);
        if (helpTab == 0) HelpTowers(area);
        else if (helpTab == 1) HelpEnemies(area);
        else HelpRules(area);
    }

    void HelpTowers(Rectangle area)
    {
        var kinds = Enum.GetValues<TowerKind>();
        var rows = 4; var cw = (area.Width - 16) / 2; var rh = area.Height / rows;
        for (int i = 0; i < kinds.Length; i++)
        {
            var k = kinds[i]; var s = Balance.Tower(k);
            var x = area.X + i / rows * (cw + 16); var y = area.Y + i % rows * rh;
            // Levels 1-3 side by side: upgrades differ by weapon shape, not only colour.
            var icon = Math.Min(rh * .42f, 40);
            for (int level = 1; level <= 3; level++) field.Tower(new Vector2(x + icon / 2 + 2 + (level - 1) * (icon + 4), y + icon / 2 + 6), icon, k, level);
            var tx = x + icon * 3 + 26;
            p.Text(Name(k), tx, y + 2, 18, Battlefield.Accent(k));
            var targets = string.Join(", ", new[] { EnemyKind.Infantry, EnemyKind.Helicopter, EnemyKind.RailCart }.Where(e => Balance.CanTarget(k, e)).Select(e => T(Balance.Enemy(e).Movement.ToString().ToLowerInvariant())));
            var range = s.MinRange > 0 ? $"{Num(s.MinRange)}–{Num(s.Range)}" : Num(s.Range);
            p.Text($"{T("cost")} {s.Cost} · DPS {Num(s.Damage / s.Interval)} · {Num(s.Interval, 2)} s · {T("range")} {range} · {targets}", tx, y + 26, 13, Painter.Muted);
            p.Paragraph(TowerGame.Content.Catalog.TowerDescription(k, Uzbek), new Rectangle((int)tx, y + 46, (int)(x + cw - tx), rh - 50), 13, Painter.Ink * .85f);
        }
    }

    void HelpEnemies(Rectangle area)
    {
        var kinds = Enum.GetValues<EnemyKind>();
        var rows = 6; var cw = (area.Width - 16) / 2; var rh = area.Height / rows;
        for (int i = 0; i < kinds.Length; i++)
        {
            var k = kinds[i]; var s = Balance.Enemy(k);
            var x = area.X + i / rows * (cw + 16); var y = area.Y + i % rows * rh;
            var icon = Math.Min(rh * .75f, 46);
            p.Box(new Rectangle(x, y + 2, (int)icon + 8, (int)icon + 4), Painter.PanelLight, 8);
            field.EnemyIcon(new Vector2(x + icon / 2 + 4, y + icon / 2 + 4), icon * (s.Boss ? .9f : 1.3f), k, clock);
            var tx = x + icon + 18;
            p.Text(Name(k) + (s.Boss ? $"  ({T("boss")})" : ""), tx, y + 2, 17, s.Boss ? Painter.Gold : Painter.Ink);
            p.Text($"{T("health")} {s.Health:0} · {T("speed_cells")} {Num(s.Speed, 2)} · {T("reward")} {s.Reward} · {T(s.Movement.ToString().ToLowerInvariant())}", tx, y + 24, 13, Painter.Muted);
            p.Paragraph($"{T("resist")}: {Resistance(k)}", new Rectangle((int)tx, y + 42, (int)(x + cw - tx), rh - 42), 12, Painter.Ink * .85f);
        }
    }

    void HelpRules(Rectangle area)
    {
        var y = area.Y;
        var half = new Rectangle(area.X, y, area.Width, area.Height);
        var big = area.Height > 420 ? 19f : 16f; var small = big - 2;
        y += (int)p.Paragraph(T("rules_text"), half, big, Painter.Ink) + 16;
        y += (int)p.Paragraph(T("controls_touch"), new Rectangle(area.X, y, area.Width, 200), small, Painter.Muted) + 10;
        y += (int)p.Paragraph(T("controls_mouse"), new Rectangle(area.X, y, area.Width, 200), small, Painter.Muted) + 10;
        p.Paragraph(T("controls_keys"), new Rectangle(area.X, y, area.Width, 200), small, Painter.Muted);
    }

    // ---------------------------------------------------------------- restore error

    void DrawRestoreError()
    {
        DrawAttractBackground();
        var r = new Rectangle((int)(W / 2 - 300), (int)(H / 2 - 150), 600, 300);
        Panel(r);
        p.Display(T("restore_title"), r.X + 30, r.Y + 26, 26, Painter.Gold);
        p.Paragraph(T("restore_text"), new Rectangle(r.X + 30, r.Y + 76, r.Width - 60, 140), 18);
        var bh = (int)Math.Max(hit, 50); var bw = (r.Width - 80) / 2;
        if (Button(new Rectangle(r.X + 30, r.Bottom - bh - 24, bw, bh), T("discard"), Style.Danger)) { store.ClearBattle(); screen = Screen.Menu; }
        if (Button(new Rectangle(r.Right - 30 - bw, r.Bottom - bh - 24, bw, bh), T("menu"))) screen = Screen.Menu;
    }
}
