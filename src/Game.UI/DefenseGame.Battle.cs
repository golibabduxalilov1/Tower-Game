using Microsoft.Xna.Framework;
using TowerGame.Core;
using TowerGame.Platform;
using TowerGame.Rendering;
using static TowerGame.UI.Strings;

namespace TowerGame.UI;

public sealed partial class DefenseGame
{
    GameSession? session;
    bool fast, resultHandled, waveDone;
    double accumulator;
    TowerKind? buildKind;
    Cell? preview; bool previewValid; string previewError = "";
    int selectedTower = -1, lastWave;
    float zoom = 1, shake, flash; Vector2 pan;
    (int Wave, long Score, int Stars)? previousBest;
    Rectangle hud, bar, board;

    // ---------------------------------------------------------------- lifecycle

    void StartBattle(GameSession s)
    {
        session = s; screen = Screen.Battle; dialog = Dialog.None;
        fast = false; accumulator = 0; buildKind = null; preview = null; selectedTower = -1;
        zoom = 1; pan = Vector2.Zero; shake = flash = 0;
        resultHandled = s.State is SessionState.Victory or SessionState.Defeat;
        lastWave = s.Wave; waveDone = false;
        previousBest = store.Best(s.Map.Id, s.Mode, s.Difficulty);
        s.WaveCompleted += () => waveDone = true;
        s.TowerFired += t => audio?.Play(t.Kind.ToString(), combat: true);
        s.Exploded += (_, _, _) => audio?.Play("explosion", combat: true);
        s.EnemyKilled += e =>
        {
            if (!Balance.Enemy(e.Kind).Boss) return;
            Achieve(Achievements.BossDown);
            if (Settings.Flashes) flash = .35f;
        };
        s.EnemyEscaped += e => { if (Settings.ScreenShake) shake = Balance.Enemy(e.Kind).Boss ? .45f : .2f; };
        audio?.Play("click");
    }

    void LeaveBattle()
    {
        if (session is { } s && s.State is not (SessionState.Victory or SessionState.Defeat)) SaveBattle(quiet: true);
        session = null; dialog = Dialog.None; screen = Screen.Menu;
    }

    void OpenPause()
    {
        if (session is not { } s || dialog == Dialog.Result) return;
        if (s.State is SessionState.Preparation or SessionState.Active or SessionState.Intermission) s.SetPaused(true);
        if (s.State == SessionState.Paused) { dialog = Dialog.Pause; buildKind = null; preview = null; }
    }

    void Resume()
    {
        if (session is not { } s) return;
        s.SetPaused(false); dialog = Dialog.None;
        SaveBattle(quiet: true); // The specification saves when leaving pause.
    }

    void CancelSelection() { buildKind = null; preview = null; selectedTower = -1; if (dialog == Dialog.Sell) dialog = Dialog.None; }

    // ---------------------------------------------------------------- update

    void ComputeBattleLayout()
    {
        var hudH = (int)Math.Max(72, hit + 12);
        var barH = (int)Math.Max(112, hit + 28);
        hud = new Rectangle(safe.X, safe.Y, safe.Width, hudH);
        bar = new Rectangle(safe.X, safe.Bottom - barH, safe.Width, barH);
        // The landscape fills the canvas; counters and shop float over it.
        board = new Rectangle(0, 0, (int)MathF.Ceiling(W), (int)MathF.Ceiling(H));
    }

    void UpdateBattle(double dt)
    {
        if (session is not { } s) { screen = Screen.Menu; return; }
        ComputeBattleLayout();
        shake = Math.Max(0, shake - (float)dt); flash = Math.Max(0, flash - (float)dt);

        if (dialog == Dialog.None)
        {
            if (input.ZoomFactor != 1 && board.Contains(input.ZoomCenter)) ZoomAt(input.ZoomCenter, input.ZoomFactor);
            if (input.PanDelta != Vector2.Zero) { pan += input.PanDelta; ClampCamera(); }
        }

        // Fixed 1/60 s simulation; 2x executes twice the ticks. Rendering speed never changes rules.
        if (s.State is SessionState.Active or SessionState.Intermission or SessionState.Preparation)
        {
            accumulator += dt;
            var guard = 0;
            while (accumulator >= GameSession.FixedDelta && guard++ < 10)
            {
                for (int i = 0; i < (fast ? 2 : 1); i++) s.Step();
                accumulator -= GameSession.FixedDelta;
            }
            if (guard >= 10) accumulator = 0; // never spiral after a long hitch
        }
        else accumulator = 0;

        if (waveDone) { waveDone = false; OnWaveCompleted(s); }
        if (s.Wave != lastWave)
        {
            lastWave = s.Wave;
            audio?.Play(s.GetWave(s.Wave).HasBoss ? "boss" : "wave");
        }
        if (!resultHandled && s.State is SessionState.Victory or SessionState.Defeat) HandleResult(s);
        if (selectedTower >= 0 && s.Towers.All(t => t.Id != selectedTower)) selectedTower = -1;

        // Desktop: the hovered cell is the build preview; a click confirms it.
        if (!host.IsTouch && buildKind is { } kind && dialog == Dialog.None)
        {
            var cell = input.Hover is { } hv && !OverHud(hv) ? Battlefield.CellAt(s.Map, board, zoom, pan, hv) : null;
            SetPreview(s, cell, kind);
        }
        else if (host.IsTouch && buildKind is { } k && preview is { } c) SetPreview(s, c, k);
    }

    bool OverHud(Vector2 point) => drawingWidgets.Values.Any(r => r.Contains(point)) || TowerPanelRect().Contains(point) && selectedTower >= 0 || host.IsTouch && buildKind != null && BuildConfirmRect().Contains(point);

    Rectangle BuildConfirmRect()
    {
        var width = (int)Math.Min(safe.Width - 32, Math.Max(390, hit * 5.3f));
        return new Rectangle(safe.Center.X - width / 2, bar.Y - (int)hit - 28, width, (int)hit + 16);
    }

    void SetPreview(GameSession s, Cell? cell, TowerKind kind)
    {
        preview = cell;
        previewValid = cell is { } c && s.CanBuild(c.X, c.Y, kind, out previewError);
        if (cell == null) previewError = "";
    }

    void BattleKeys()
    {
        if (session is not { } s || dialog != Dialog.None) return;
        if (input.Pressed(Settings.Keys.GetValueOrDefault("Pause"))) { OpenPause(); return; }
        if (input.Pressed(Settings.Keys.GetValueOrDefault("Speed"))) fast = !fast;
        for (int i = 0; i < 6; i++)
            if (input.Pressed(Settings.Keys.GetValueOrDefault("Tower" + (i + 1))) && i < s.AvailableTowers.Count)
            { SelectBuild(s.AvailableTowers[i]); }
        if (selectedTower >= 0 && input.Pressed(Settings.Keys.GetValueOrDefault("Upgrade"))) UpgradeSelected(s);
        if (selectedTower >= 0 && input.Pressed(Settings.Keys.GetValueOrDefault("Sell"))) dialog = Dialog.Sell;
    }

    void SelectBuild(TowerKind kind)
    {
        selectedTower = -1;
        buildKind = buildKind == kind ? null : kind;
        preview = null;
    }

    void OnWaveCompleted(GameSession s)
    {
        if (s.State is SessionState.Victory or SessionState.Defeat) return; // the result screen records it
        var snapshot = s.Capture();
        store.RecordProgress(snapshot);
        if (!store.SaveBattle(snapshot)) Toast(T("save_failed"));
        if (s.Wave >= 50) Achieve(Achievements.Wave50);
        if (s.Mode == Mode.Endless && s.Wave >= 150) Achieve(Achievements.Endless150);
        if (store.IsMapUnlocked(maps.Count - 1)) Achieve(Achievements.AllMaps);
    }

    void HandleResult(GameSession s)
    {
        resultHandled = true;
        buildKind = null; preview = null; selectedTower = -1;
        if (!store.SaveBattle(s.Capture())) Toast(T("save_failed")); // records progress and clears the active save
        var won = s.State == SessionState.Victory;
        audio?.Play(won ? "victory" : "defeat");
        if (won && s.Mode == Mode.Classic) Achieve(Achievements.ClassicWin);
        if (won && s.Lives == 20) Achieve(Achievements.Flawless);
        if (s.Wave >= 50) Achieve(Achievements.Wave50);
        if (store.IsMapUnlocked(maps.Count - 1)) Achieve(Achievements.AllMaps);
        dialog = Dialog.Result;
    }

    // ---------------------------------------------------------------- camera

    void ZoomAt(Vector2 point, float factor)
    {
        if (session is not { } s) return;
        var (cell, origin) = Battlefield.Layout(s.Map, board, zoom, pan);
        var world = (point - origin) / cell;
        zoom = Math.Clamp(zoom * factor, 1, 3.5f);
        var (cell2, origin2) = Battlefield.Layout(s.Map, board, zoom, Vector2.Zero);
        pan = point - world * cell2 - origin2;
        ClampCamera();
    }

    void ClampCamera()
    {
        if (session is not { } s) return;
        var (cell, _) = Battlefield.Layout(s.Map, board, zoom, Vector2.Zero);
        var ex = Math.Max(0, (cell * s.Map.Width - board.Width) / 2) + 30 * (zoom - 1);
        var ey = Math.Max(0, (cell * s.Map.Height - board.Height) / 2) + 30 * (zoom - 1);
        pan = new Vector2(Math.Clamp(pan.X, -ex, ex), Math.Clamp(pan.Y, -ey, ey));
    }

    // ---------------------------------------------------------------- commands

    void TryBuildAt(GameSession s, Cell c, TowerKind kind)
    {
        if (s.TryBuild(c.X, c.Y, kind, out var error))
        {
            audio?.Play("build"); Achieve(Achievements.FirstBuild);
            preview = null;
            if (s.Money < Balance.Tower(kind).Cost) buildKind = null;
        }
        else { Toast(T(error)); audio?.Play("error"); }
    }

    void UpgradeSelected(GameSession s)
    {
        var t = s.Towers.Find(t => t.Id == selectedTower);
        if (t == null) return;
        if (s.Upgrade(t.Id, out var error)) { audio?.Play("upgrade"); if (t.Level == 3) Achieve(Achievements.MaxLevel); }
        else { Toast(T(error)); audio?.Play("error"); }
    }

    // ---------------------------------------------------------------- drawing

    void DrawBoardOnly()
    {
        if (session is not { } s) return;
        ComputeBattleLayout();
        var shakeOffset = shake > 0 ? new Vector2(MathF.Sin(frame * 1.7f), MathF.Cos(frame * 2.3f)) * shake * 10 : Vector2.Zero;
        Clipped(board, () => field.Map(s.Map, board, s, Settings.ShowPath, zoom, pan + shakeOffset, selectedTower, buildKind != null ? preview : null, buildKind, previewValid, s.Tick * GameSession.FixedDelta));
    }

    void DrawBattle()
    {
        if (session is not { } s) return;
        ComputeBattleLayout();
        DrawBoardOnly();
        if (flash > 0) p.Rect(board.X, board.Y, board.Width, board.Height, Color.White * (flash * .5f));
        var modal = dialog is Dialog.Pause or Dialog.Result or Dialog.Sell or Dialog.Restart;
        input.Blocked = modal;
        DrawHud(s);
        DrawBar(s);
        if (selectedTower >= 0 && s.Towers.Find(t => t.Id == selectedTower) is { } tower) DrawTowerPanel(s, tower);
        DrawBanner(s);
        if (buildKind != null && preview != null && !previewValid && previewError.Length > 0 && host.IsTouch)
        {
            var strip = BuildConfirmRect();
            var errorBand = new Rectangle(strip.X, strip.Y - 36, strip.Width, 30);
            p.Frame(errorBand, new Color(55, 29, 27), new Color(153, 82, 59), 6, 1);
            p.Center(T(previewError), errorBand, 15, new Color(255, 202, 160));
        }

        // A tap that no widget took lands on the battlefield.
        if (!modal && input.Tap is { } tap && board.Contains(tap) && !OverHud(tap))
        {
            input.ConsumeTap();
            var cell = Battlefield.CellAt(s.Map, board, zoom, pan, tap);
            var towerAt = cell is { } c0 ? s.Towers.Find(t => t.X == c0.X && t.Y == c0.Y) : null;
            if (buildKind is { } kind && towerAt == null)
            {
                if (cell is not { } c) CancelSelection();
                else if (!host.IsTouch || preview == c) TryBuildAt(s, c, kind);
                else SetPreview(s, c, kind);
            }
            else
            {
                buildKind = null; preview = null;
                selectedTower = towerAt?.Id ?? -1;
                if (towerAt != null) audio?.Play("click");
            }
        }
        input.Blocked = false;
        switch (dialog)
        {
            case Dialog.Pause: DrawPause(s); break;
            case Dialog.Restart: DrawRestart(s); break;
            case Dialog.Sell: DrawSell(s); break;
            case Dialog.Result: DrawResult(s); break;
        }
    }

    void DrawHud(GameSession s)
    {
        // Three floating clusters leave the landscape visible between the counters.
        var cy = hud.Y + hud.Height / 2f;
        p.Box(new Rectangle(hud.X + 10, hud.Y + 8, 184, hud.Height - 16), new Color(26, 39, 25) * .83f, 28);
        p.ControlIcon(UiSymbol.Coin, new Vector2(hud.X + 40, cy), 25);
        p.Display(Short(s.Money), hud.X + 73, cy - 22, 34, new Color(255, 226, 86));
        var b = (int)Math.Max(hit, 56);
        if (SymbolButton(new Rectangle(hud.X + 204, hud.Y + (hud.Height - b) / 2, b, b), UiSymbol.Settings, T("settings"))) { OpenPause(); OpenSettings(Screen.Battle); }
        var center = new Rectangle(hud.Center.X - 170, hud.Y + 1, 340, hud.Height - 2);
        p.DisplayCenter(Short(s.Score), new Rectangle(center.X, center.Y - 4, center.Width, 49), 40, new Color(255, 223, 78));
        p.DisplayCenter($"{T("wave")} {s.Wave}" + (s.Mode == Mode.Endless ? "" : $" / {s.TotalWaves}"), new Rectangle(center.X, center.Y + 43, center.Width, 27), 18, Painter.Ink);
        var heart = new Vector2(hud.Right - 47, cy);
        p.ControlIcon(UiSymbol.Heart, heart, 35);
        p.DisplayCenter(s.Lives.ToString(), new Rectangle((int)heart.X - 28, (int)heart.Y - 19, 56, 36), 27, new Color(255, 240, 116));
        var path = new Rectangle(hud.Right - 150, hud.Y + (hud.Height - b) / 2, b, b);
        if (SymbolButton(path, UiSymbol.Path, T("path"), Settings.ShowPath ? Style.Selected : Style.Normal)) { Settings.ShowPath = !Settings.ShowPath; store.SaveSettings(); }
    }

    void DrawBanner(GameSession s)
    {
        string? text = null;
        var finished = s.Mode != Mode.Endless && s.Wave >= s.TotalWaves;
        if (!finished && s.State is SessionState.Preparation or SessionState.Intermission && s.NextWave.HasBoss) text = T("boss_next");
        else if (s.State == SessionState.Active && s.GetWave(Math.Max(1, s.Wave)).HasBoss && s.Enemies.Any(e => !e.Dead && Balance.Enemy(e.Kind).Boss)) text = T("boss_now");
        if (text == null) return;
        var pulse = .75f + .25f * MathF.Sin((float)clock * 5);
        var w = (int)p.Measure(text, 19) + 40;
        var r = new Rectangle(board.Center.X - w / 2, hud.Bottom + 5, w, 38);
        p.Frame(r, new Color(90, 34, 30) * .9f, Painter.Danger * pulse, 10);
        p.Center(text, r, 19, Color.White);
    }

    void DrawBar(GameSession s)
    {
        var puck = (int)Math.Max(hit, Math.Min(86, bar.Height - 18));
        var by = bar.Bottom - puck - 10;
        if (SymbolButton(new Rectangle(bar.X + 14, by, puck, puck), UiSymbol.Pause, "pause")) OpenPause();
        if (SymbolButton(new Rectangle(bar.X + puck + 24, by, puck, puck), UiSymbol.Fast, "speed", fast ? Style.Selected : Style.Normal)) fast = !fast;
        p.DisplayCenter(fast ? "2×" : "1×", new Rectangle(bar.X + puck + 24, by - 22, puck, 23), 15, Painter.Gold);
        var kinds = s.AvailableTowers;
        var available = bar.Width - puck * 2 - 64;
        var step = Math.Min(112, available / kinds.Count);
        var diameter = Math.Min(step - 8, (int)Math.Max(hit, bar.Height - 12));
        var x0 = bar.Right - 12 - step * kinds.Count;
        for (int i = 0; i < kinds.Count; i++)
        {
            var k = kinds[i]; var cost = Balance.Tower(k).Cost;
            var r = new Rectangle(x0 + i * step, bar.Bottom - diameter - 8, diameter, diameter);
            var selected = buildKind == k;
            var supplied = p.GetAsset("icon:" + k) != null;
            if (supplied)
            {
                var id = "tower:" + k;
                if (!input.Blocked) drawingWidgets[id] = r;
                p.Asset("icon:" + k, r, input.IsHover(r) ? new Color(255, 255, 235) : Color.White);
                RegisterFocus(r, id, true);
                if (selected) p.Ring(new Vector2(r.Center.X, r.Center.Y), diameter * .45f, new Color(255, 229, 68), 3, 48);
                if (input.TryTap(r) || ActivateFocus(id)) { SelectBuild(k); audio?.Play("click"); }
            }
            else
            {
                if (Button(r, "", selected ? Style.Selected : Style.Normal, id: "tower:" + k)) SelectBuild(k);
                var art = r; art.Inflate(-7, -7); art.Offset(0, -5); WeaponIcon(k, art);
            }
            var price = supplied
                ? new Rectangle(r.X + (int)(diameter * .53f), r.Y + (int)(diameter * .79f), (int)(diameter * .43f), (int)(diameter * .23f))
                : new Rectangle(r.Center.X - (int)(diameter * .39f), r.Bottom - 19, (int)(diameter * .78f), 27);
            if (!supplied) p.Frame(price, s.Money >= cost ? new Color(242, 199, 45) : new Color(107, 102, 80), new Color(53, 55, 34), 12, 2);
            p.DisplayCenter("$" + cost, price, supplied ? Math.Min(16, diameter * .17f) : 18, s.Money >= cost ? new Color(255, 251, 187) : Painter.Danger);
            if (!host.IsTouch) p.Display(InputState.KeyLabel(Settings.Keys.GetValueOrDefault("Tower" + (i + 1), "")), r.X + 4, r.Y - 14, 13, Painter.Ink);
            if ((selected || input.IsHover(r)) && !(host.IsTouch && buildKind != null)) p.DisplayCenter(Name(k), new Rectangle(r.Center.X - 80, r.Y - 38, 160, 25), 16, Painter.Gold);
        }
        var startW = (int)Math.Max(158, hit * 2.2f);
        var waveRect = new Rectangle(safe.Right - startW - 14, bar.Y - (int)hit - 12, startW, (int)hit);
        if (s.State is SessionState.Preparation or SessionState.Intermission)
        {
            var label = T(s.State == SessionState.Preparation ? "start_wave" : "call_early");
            if (SymbolButton(waveRect, UiSymbol.Play, label, Style.Primary, caption: label)) s.StartWave();
            if (s.State == SessionState.Intermission) p.Right(F("break_in", Math.Ceiling(Math.Max(0, s.BreakTime))), waveRect.Right - 7, waveRect.Y - 24, 15, Painter.Gold);
        }
        else p.Right($"{s.RemainingEnemies} {T("enemies").ToLowerInvariant()}", safe.Right - 18, bar.Y - 25, 15, Painter.Ink);
        if (host.IsTouch && buildKind is { } selectedKind)
        {
            var strip = BuildConfirmRect();
            Panel(strip, 22);
            var gap = 10; var cw = (strip.Width - 30) / 2;
            var confirm = new Rectangle(strip.X + 10, strip.Y + 7, cw, strip.Height - 14);
            var cancel = new Rectangle(confirm.Right + gap, confirm.Y, cw, confirm.Height);
            if (Button(confirm, T("confirm"), Style.Primary, enabled: preview != null && previewValid, size: 18) && preview is { } c) TryBuildAt(s, c, selectedKind);
            if (Button(cancel, T("cancel"), Style.Danger, size: 18)) CancelSelection();
        }
    }

    Rectangle TowerPanelRect()
    {
        var w = (int)Math.Min(340, W * .34f);
        var top = hud.Bottom + 8;
        var h = (int)Math.Min(bar.Y - top - 8, 460 + (hit - 44) * 2);
        return new Rectangle(safe.Right - w - 8, top, w, h);
    }

    void DrawTowerPanel(GameSession s, Tower t)
    {
        var r = TowerPanelRect();
        Panel(r);
        var stats = Balance.Tower(t.Kind, t.Level);
        var x = r.X + 18; var header = (int)Math.Max(44, hit);
        var close = new Rectangle(r.Right - header - 4, r.Y + 4, header, header);
        var name = Name(t.Kind); float nameSize = 19; while (nameSize > 12 && p.MeasureDisplay(name, nameSize) > r.Width - header * 2 - 34) nameSize--; p.Display(name, x, r.Y + header / 2f - 12, nameSize, Battlefield.Accent(t.Kind));
        var info = new Rectangle(r.Right - header * 2 - 8, r.Y + 4, header, header);
        if (SymbolButton(info, UiSymbol.Info, "tower_info", Style.Ghost)) { OpenPause(); SaveBattle(quiet: true); returnScreen = Screen.Battle; screen = Screen.Help; helpTab = 0; }
        var nameEnd = x + 5;
        for (int i = 0; i < 3; i++) p.Star(new Vector2(nameEnd + i * 20, r.Y + header + 1), 6, i < t.Level ? Painter.Gold : new Color(70, 84, 80), i < t.Level);

        var bh = (int)Math.Max(hit, 40);
        var bottom = r.Bottom - 14;
        var upgrade = new Rectangle(x, bottom - bh, (r.Width - 46) / 2, bh);
        var sell = new Rectangle(upgrade.Right + 10, bottom - bh, (r.Width - 46) / 2, bh);
        var priority = new Rectangle(x, upgrade.Y - bh - 8, r.Width - 36, bh);

        (string Label, string Value)[] rows =
        [
            (T("level"), $"{t.Level}/3"), (T("damage"), Num(stats.Damage)),
            (T("rate"), F("per_sec", Num(1 / stats.Interval, 2))),
            (T("range"), stats.MinRange > 0 ? $"{Num(stats.MinRange)}–{Num(stats.Range, 2)}" : Num(stats.Range, 2)),
            (T("sell_value"), s.SellValue(t).ToString()),
            (T("upgrade_cost"), t.Level >= 3 ? T("max_level") : Balance.UpgradeCost(t).ToString()),
        ];
        var y = r.Y + header + 8f;
        if (r.Height > 420)
        {
            field.Tower(new Vector2(r.Center.X, y + 52), 125, t.Kind, t.Level, -.65f);
            y += 118;
        }
        var room = priority.Y - 8 - y;
        var line = 23f; var size = 16f;
        if (room >= rows.Length * line)
        {
            foreach (var (label, value) in rows) { p.Text(label, x, y, size, Painter.Muted); p.Right(value, r.Right - 18, y, size); y += line; }
            room = priority.Y - 8 - y;
            if (room > 36) p.Paragraph(TowerGame.Content.Catalog.TowerDescription(t.Kind, Uzbek), new Rectangle(x, (int)y + 2, r.Width - 36, (int)room), 13, Painter.Ink * .8f);
        }
        else
        {
            // Compact two-column table for short landscape phones; the description lives in Help.
            size = 14; line = Math.Min(21, room / 3);
            var colW = (r.Width - 36) / 2f;
            for (int i = 0; i < rows.Length; i++)
            {
                var cx = x + i / 3 * (colW + 8); var cy = y + i % 3 * line;
                var (label, value) = i == 5 ? (T("next"), t.Level >= 3 ? "—" : rows[i].Value) : rows[i];
                p.Text(label, cx, cy, size - 1, Painter.Muted);
                p.Right(value, cx + colW - 4, cy, size);
            }
        }
        if (Button(priority, $"{T("priority")}: {T(t.Priority.ToString())}", size: 15, id: "priority"))
            t.Priority = (TargetPriority)(((int)t.Priority + 1) % Enum.GetValues<TargetPriority>().Length);
        var canUpgrade = t.Level < 3 && s.State is not (SessionState.Victory or SessionState.Defeat);
        var upLabel = t.Level < 3 ? $"{T("upgrade")} {Balance.UpgradeCost(t)}" : T("max_level");
        if (SymbolButton(upgrade, UiSymbol.Upgrade, "upgrade", s.Money >= Balance.UpgradeCost(t) && canUpgrade ? Style.Primary : Style.Normal, canUpgrade, caption: upLabel)) UpgradeSelected(s);
        if (SymbolButton(sell, UiSymbol.Sell, "sell", Style.Danger, caption: $"{T("sell")} {s.SellValue(t)}")) dialog = Dialog.Sell;
        if (SymbolButton(close, UiSymbol.Close, "close", Style.Ghost)) selectedTower = -1;
        // Swallow taps on the panel body so they do not reach the map underneath.
        input.TryTap(r);
    }
    // ---------------------------------------------------------------- dialogs

    void DrawPause(GameSession s)
    {
        Dim();
        var bh = (int)Math.Max(hit, 52); var gap = 12;
        var height = bh * 4 + 164;
        var r = new Rectangle((int)(W / 2 - 264), (int)(H / 2 - height / 2), 528, height);
        Panel(r);
        p.DisplayCenter(T("paused"), new Rectangle(r.X, r.Y + 14, r.Width, 50), 34, Painter.Gold);
        var y = r.Y + 79; var bw = (r.Width - 72) / 2;
        if (SymbolButton(new Rectangle(r.X + 30, y, r.Width - 60, bh), UiSymbol.Play, T("resume"), Style.Primary, caption: T("resume"))) Resume();
        y += bh + gap;
        if (SymbolButton(new Rectangle(r.X + 30, y, bw, bh), UiSymbol.Restart, "restart", caption: Uzbek ? "Qayta boshlash" : "Restart")) dialog = Dialog.Restart;
        if (SymbolButton(new Rectangle(r.X + 42 + bw, y, bw, bh), UiSymbol.Settings, T("settings"), caption: T("settings"))) OpenSettings(Screen.Battle);
        y += bh + gap;
        if (SymbolButton(new Rectangle(r.X + 30, y, bw, bh), UiSymbol.Sound, "sound", Settings.EffectsVolume > 0 ? Style.Selected : Style.Normal, caption: T("effects"))) ToggleSound();
        if (SymbolButton(new Rectangle(r.X + 42 + bw, y, bw, bh), UiSymbol.Music, "music", Settings.MusicVolume > 0 ? Style.Selected : Style.Normal, caption: T("music"))) ToggleMusic();
        y += bh + gap;
        if (SymbolButton(new Rectangle(r.X + 30, y, bw, bh), UiSymbol.Info, T("help"), caption: T("help"))) { returnScreen = Screen.Battle; screen = Screen.Help; }
        if (SymbolButton(new Rectangle(r.X + 42 + bw, y, bw, bh), UiSymbol.Exit, T("save_exit"), Style.Danger, caption: T("save_exit")))
        {
            if (SaveBattle(quiet: true)) { session = null; dialog = Dialog.None; screen = Screen.Menu; }
        }
        p.Center($"{(Uzbek ? s.Map.NameUz : s.Map.Name)} · {T(s.Mode.ToString())} · {T(s.Difficulty.ToString())}", new Rectangle(r.X, r.Bottom - 34, r.Width, 24), 14, Painter.Muted);
    }

    void DrawRestart(GameSession s)
    {
        Dim();
        var r = new Rectangle((int)(W / 2 - 250), (int)(H / 2 - 115), 500, 230);
        Panel(r);
        p.Paragraph(Uzbek ? "Bosqichni boshidan boshlaysizmi? Hozirgi jang almashtiriladi." : "Restart this level? Your current battle will be replaced.", new Rectangle(r.X + 28, r.Y + 28, r.Width - 56, 90), 22);
        var bh = (int)Math.Max(hit, 52);
        if (Button(new Rectangle(r.X + 28, r.Bottom - bh - 25, 210, bh), T("no"))) dialog = Dialog.Pause;
        if (Button(new Rectangle(r.Right - 238, r.Bottom - bh - 25, 210, bh), T("yes"), Style.Danger)) StartBattle(new GameSession(s.Map, s.Mode, s.Difficulty));
    }

    void DrawSell(GameSession s)
    {
        Dim();
        var t = s.Towers.Find(t => t.Id == selectedTower);
        if (t == null) { dialog = Dialog.None; return; }
        var r = new Rectangle((int)(W / 2 - 240), (int)(H / 2 - 100), 480, 200);
        Panel(r);
        p.Paragraph(F("sell_confirm", Name(t.Kind), s.SellValue(t)), new Rectangle(r.X + 28, r.Y + 30, r.Width - 56, 80), 21);
        var bh = (int)Math.Max(hit, 50); var bw = (r.Width - 72) / 2;
        if (Button(new Rectangle(r.X + 28, r.Bottom - bh - 22, bw, bh), T("no"))) dialog = Dialog.None;
        if (Button(new Rectangle(r.Right - 28 - bw, r.Bottom - bh - 22, bw, bh), T("sell"), Style.Danger))
        {
            if (s.Sell(t.Id)) { audio?.Play("sell"); selectedTower = -1; }
            dialog = Dialog.None;
        }
    }

    void DrawResult(GameSession s)
    {
        Dim();
        var won = s.State == SessionState.Victory;
        var resultHeight = Math.Min(safe.Height - 24, (int)Math.Max(480, hit * 2 + 318));
        var r = new Rectangle((int)(W / 2 - 260), safe.Center.Y - resultHeight / 2, 520, resultHeight);
        Panel(r);
        p.DisplayCenter(T(won ? "victory" : "defeat"), new Rectangle(r.X, r.Y + 16, r.Width, 56), 38, won ? Painter.Gold : Painter.Danger);
        var stars = won ? s.Lives == 20 ? 3 : s.Lives >= 10 ? 2 : 1 : 0;
        for (int i = 0; i < 3; i++) p.Star(new Vector2(r.Center.X + (i - 1) * 56, r.Y + 102), 22, i < stars ? Painter.Gold : new Color(70, 84, 80), i < stars);
        var y = r.Y + 136f;
        void Line(string label, string value) { p.Text(label, r.X + 50, y, 19, Painter.Muted); p.Right(value, r.Right - 50, y, 19); y += 26; }
        Line(T("score"), Short(s.Score));
        Line(T("reached"), Short(s.Wave));
        if (won) Line(T("lives_left"), s.Lives.ToString());
        if (previousBest is not { } pb || s.Score > pb.Score) p.Center(T("new_record"), new Rectangle(r.X, (int)y, r.Width, 30), 19, Painter.Gold);
        var bh = (int)Math.Max(hit, 52); var bw = (r.Width - 76) / 2;
        var next = maps.ToList().IndexOf(s.Map) + 1;
        if (won && next < maps.Count && store.IsMapUnlocked(next))
        {
            if (SymbolButton(new Rectangle(r.X + 30, r.Bottom - bh * 2 - 40, r.Width - 60, bh), UiSymbol.Next, "next_level", Style.Primary, caption: Uzbek ? "Keyingi bosqich" : "Next level"))
            { mapIndex = next; session = null; dialog = Dialog.None; screen = Screen.Setup; }
        }
        if (SymbolButton(new Rectangle(r.X + 30, r.Bottom - bh - 26, bw, bh), UiSymbol.Map, T("to_maps"), caption: T("to_maps"))) { session = null; dialog = Dialog.None; screen = Screen.Maps; }
        if (SymbolButton(new Rectangle(r.Right - 30 - bw, r.Bottom - bh - 26, bw, bh), UiSymbol.Restart, T("retry"), Style.Primary, caption: T("retry")))
            StartBattle(new GameSession(s.Map, s.Mode, s.Difficulty));
    }
}
