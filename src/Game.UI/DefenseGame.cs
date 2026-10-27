using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TowerGame.Audio;
using TowerGame.Core;
using TowerGame.Platform;
using TowerGame.Rendering;
using static TowerGame.UI.Strings;

namespace TowerGame.UI;

/// <summary>Command-line switches for automation: screenshots, attract demo and the benchmark.</summary>
public sealed class LaunchOptions
{
    public string? Screenshot { get; set; }
    public string Scene { get; set; } = "battle";
    public int Frames { get; set; } = 40;
    public bool Demo { get; set; }
    public bool Bench { get; set; }
    public int MapIndex { get; set; }
    public string? Language { get; set; }
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public string? Script { get; set; }
}

/// <summary>
/// Shared game shell for Windows and Android. Simulation advances in Update with a fixed 1/60 s step;
/// the interface is immediate-mode and handled during Draw, where layout is known.
/// </summary>
public sealed partial class DefenseGame : Game
{
    enum Screen { Loading, Menu, Maps, Setup, Battle, Settings, Help, RestoreError, Bench, Armory, Achievements }
    enum Dialog { None, Pause, Sell, Overwrite, Result, Restart }
    enum Style { Normal, Primary, Danger, Ghost, Selected }

    readonly GraphicsDeviceManager graphics;
    readonly IGameServices store;
    readonly IReadOnlyList<MapDefinition> maps;
    readonly IPlatformHost host;
    readonly IStoreIntegration storefront;
    readonly LaunchOptions options;
    readonly InputState input = new();
    Painter p = null!;
    Battlefield field = null!;
    SynthAudio? audio;

    Screen screen = Screen.Loading, returnScreen = Screen.Menu;
    Dialog dialog;
    int loadStep, frame;
    double clock;

    // Logical layout: height is fixed (720 desktop, ~48 dp-scaled on touch), width follows the aspect ratio.
    float W = 1280, H = 720, scale = 1, hit = 44;
    Rectangle safe;

    int mapIndex; Mode mode = Mode.Classic; Difficulty difficulty = Difficulty.Normal;
    string toast = ""; double toastTime;
    bool resizing;

    // Performance overlay (F3) and counters.
    readonly System.Diagnostics.Stopwatch updateWatch = new(), drawWatch = new();
    double updateMs, drawMs, fps, fpsTimer; int fpsFrames; bool showStats;

    GameSettings Settings => store.Settings;
    // Developer trace (QALQON_DEBUG=1): input, focus and screen changes, written to stdout only.
    static readonly bool Debug = Environment.GetEnvironmentVariable("QALQON_DEBUG") == "1";
    bool lastActive; Screen lastScreen;
    void Trace()
    {
        if (!Debug) return;
        if (IsActive != lastActive) { lastActive = IsActive; Console.WriteLine($"{clock:0.00} active={IsActive}"); }
        if (screen != lastScreen) { lastScreen = screen; Console.WriteLine($"{clock:0.00} screen={screen}"); }
        if (input.Tap is { } t) Console.WriteLine($"{clock:0.00} tap {t.X:0},{t.Y:0} dialog={dialog} build={buildKind} money={session?.Money}");
    }

    public DefenseGame(IGameServices store, IReadOnlyList<MapDefinition> maps, LaunchOptions? options = null,
        IPlatformHost? host = null, IStoreIntegration? storefront = null)
    {
        this.store = store; this.maps = maps;
        this.options = options ?? new LaunchOptions();
        this.host = host ?? new DesktopHost();
        this.storefront = storefront ?? new OfflineStore();
        graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = this.options.Width,
            PreferredBackBufferHeight = this.options.Height,
            SynchronizeWithVerticalRetrace = true,
            HardwareModeSwitch = false,
            GraphicsProfile = GraphicsProfile.Reach
        };
        if (this.host.IsTouch && (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()))
        {
            graphics.IsFullScreen = true;
            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        Window.Title = "AGS Tower";
        Window.AllowUserResizing = !this.host.IsTouch;
        Window.ClientSizeChanged += OnResize;
        Activated += (_, _) => audio?.Resume();
        Deactivated += (_, _) => OnBackground();
        ApplyFrameRate();
    }

    void ApplyFrameRate() => TargetElapsedTime = TimeSpan.FromSeconds(1.0 / (Settings.FrameRate == 30 ? 30 : 60));

    void OnResize(object? sender, EventArgs e)
    {
        if (resizing || host.IsTouch) return;
        var bounds = Window.ClientBounds;
        if (bounds.Width < 64 || bounds.Height < 64) return; // minimised
        resizing = true;
        graphics.PreferredBackBufferWidth = bounds.Width;
        graphics.PreferredBackBufferHeight = bounds.Height;
        graphics.ApplyChanges();
        resizing = false;
    }

    void ToggleFullScreen()
    {
        if (host.IsTouch) return;
        if (!graphics.IsFullScreen)
        {
            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            graphics.PreferredBackBufferWidth = mode.Width; graphics.PreferredBackBufferHeight = mode.Height;
        }
        else { graphics.PreferredBackBufferWidth = 1280; graphics.PreferredBackBufferHeight = 720; }
        graphics.IsFullScreen = !graphics.IsFullScreen;
        graphics.ApplyChanges();
    }

    /// <summary>Called when the window loses focus or Android sends the app to the background.</summary>
    public void OnBackground() => OnBackground(force: false);

    void OnBackground(bool force)
    {
        if (!force && (options.Screenshot != null || options.Bench || options.Script != null)) return;
        audio?.Suspend();
        if (screen == Screen.Battle && session is { } s && s.State is SessionState.Preparation or SessionState.Active or SessionState.Intermission)
        {
            OpenPause();
            SaveBattle(quiet: true);
        }
    }

    protected override void LoadContent()
    {
        using var png = TitleContainer.OpenStream("Content/font.png");
        using var json = TitleContainer.OpenStream("Content/font.json");
        p = new Painter(GraphicsDevice, png, json);
        using var displayPng = TitleContainer.OpenStream("Content/display.png");
        using var displayJson = TitleContainer.OpenStream("Content/display.json");
        p.LoadDisplayAtlas(GraphicsDevice, displayPng, displayJson);
        p.LoadSuppliedArt(GraphicsDevice);
        field = new Battlefield(p);
    }

    protected override void UnloadContent()
    {
        audio?.Dispose();
        p?.Dispose();
    }

    // ---------------------------------------------------------------- startup

    void StepLoading()
    {
        switch (loadStep++)
        {
            case 0: break; // first frame shows the loading screen
            case 1:
                if (store.IsFirstLaunch)
                {
                    var language = storefront.PreferredLanguage ?? host.SystemLanguage;
                    Settings.Language = language.StartsWith("uz", StringComparison.OrdinalIgnoreCase) ? "uz" : "en";
                    store.SaveSettings();
                }
                if (options.Language is "uz" or "en") Settings.Language = options.Language;
                Uzbek = Settings.Language == "uz";
                break;
            case 2:
                if (options.Screenshot == null) audio = new SynthAudio();
                break;
            default:
                screen = Screen.Menu;
                if (options.Bench) { StartBench(); return; }
                if (options.Screenshot != null) { PrepareScene(options.Scene); return; }
                if (store.RecoveredBackup) Toast(T("restore_backup"));
                if (store.HasSavedBattle && store.LoadBattle() == null) screen = Screen.RestoreError;
                break;
        }
    }

    // ---------------------------------------------------------------- frame loop

    protected override void Update(GameTime gameTime)
    {
        updateWatch.Restart();
        var dt = Math.Min(gameTime.ElapsedGameTime.TotalSeconds, .25);
        clock += dt;
        ComputeLayout();
        // Scripted verification uses its injected taps; desktop clicks must not alter the run.
        input.Update(scale, Vector2.Zero, host.Density, IsActive && options.Script == null && options.Screenshot == null, host.IsTouch);
        UpdateFocus();
        Trace();
        storefront.Update();
        if (audio != null)
        {
            audio.Master = Settings.MasterVolume; audio.Music = Settings.MusicVolume; audio.Effects = Settings.EffectsVolume;
            audio.Muted = Settings.Muted;
            audio.Update(dt);
        }
        switch (screen)
        {
            case Screen.Loading: StepLoading(); break;
            case Screen.Menu: case Screen.Maps: case Screen.Setup: UpdateAttract(); break;
            case Screen.Battle: UpdateBattle(dt); break;
            case Screen.Bench: UpdateBench(); break;
        }
        HandleKeys();
        toastTime -= dt;
        updateMs = updateMs * .9 + updateWatch.Elapsed.TotalMilliseconds * .1;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        drawWatch.Restart();
        frame++;
        fpsFrames++; fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (fpsTimer >= 1) { fps = fpsFrames / fpsTimer; fpsFrames = 0; fpsTimer = 0; }
        ComputeLayout();
        var capture = options.Screenshot != null && (screen != Screen.Loading || options.Scene == "loading") && frame >= options.Frames;
        var shotPath = capture ? options.Screenshot : pendingShot;
        pendingShot = null;
        RenderTarget2D? target = null;
        if (shotPath != null)
        {
            var pp = GraphicsDevice.PresentationParameters;
            target = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight);
            GraphicsDevice.SetRenderTarget(target);
        }
        RunScript(); // scripted UI tests advance per drawn frame, where taps are resolved
        GraphicsDevice.Clear(Painter.Backdrop);
        p.Batch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: Matrix.CreateScale(scale));
        switch (screen)
        {
            case Screen.Loading: DrawLoading(); break;
            case Screen.Menu: if (iconOnly) DrawIcon(); else DrawMenu(); break;
            case Screen.Maps: DrawMaps(); break;
            case Screen.Setup: DrawSetup(); break;
            case Screen.Battle: DrawBattle(); break;
            case Screen.Settings: DrawSettings(); break;
            case Screen.Help: DrawHelp(); break;
            case Screen.RestoreError: DrawRestoreError(); break;
            case Screen.Bench: DrawBench(); break;
            case Screen.Armory: DrawArmory(); break;
            case Screen.Achievements: DrawAchievements(); break;
        }
        if (!iconOnly) DrawToast();
        if (showStats) DrawStats();
        p.Batch.End();
        widgets.Clear(); foreach (var (k, v) in drawingWidgets) widgets[k] = v; drawingWidgets.Clear();
        focusWidgets.Clear(); foreach (var (k, v) in drawingFocusWidgets) focusWidgets[k] = v; drawingFocusWidgets.Clear();
        input.EndFrame();
        focusActivate = false;
        drawMs = drawMs * .9 + drawWatch.Elapsed.TotalMilliseconds * .1;
        if (target != null)
        {
            GraphicsDevice.SetRenderTarget(null);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(shotPath!))!);
            using (var file = File.Create(shotPath!)) target.SaveAsPng(file, target.Width, target.Height);
            target.Dispose();
            if (capture) Exit();
        }
        base.Draw(gameTime);
    }

    void ComputeLayout()
    {
        var pp = GraphicsDevice.PresentationParameters;
        float wPx = Math.Max(1, pp.BackBufferWidth), hPx = Math.Max(1, pp.BackBufferHeight);
        H = host.IsTouch ? Math.Clamp(hPx / host.Density * 1.35f, 480, 720) : 720;
        scale = hPx / H; W = wPx / scale;
        if (W < 860) { W = 860; scale = wPx / W; H = hPx / scale; } // tall desktop windows
        var ins = host.Insets;
        safe = new Rectangle((int)MathF.Ceiling(ins.Left / scale), (int)MathF.Ceiling(ins.Top / scale),
            (int)(W - (ins.Left + ins.Right) / scale), (int)(H - (ins.Top + ins.Bottom) / scale));
        // 48 dp minimum touch target, converted to logical units.
        hit = MathF.Ceiling(Math.Max(host.IsTouch ? 48 * host.Density / scale : 0, 44));
    }

    // ---------------------------------------------------------------- global keys / back

    void HandleKeys()
    {
        if (input.Pressed(Keys.F11) || (input.Pressed(Keys.Enter) && (Keyboard.GetState().IsKeyDown(Keys.LeftAlt) || Keyboard.GetState().IsKeyDown(Keys.RightAlt))))
            ToggleFullScreen();
        if (input.Pressed(Keys.F3)) showStats = !showStats;
        if (rebinding != null)
        {
            if (input.PressedKey is { } key) FinishRebind(key);
            return;
        }
        if (input.Pressed(Keys.Escape) || input.Back) Back();
        else if (input.Cancel && screen == Screen.Battle) CancelSelection();
        if (screen == Screen.Battle) BattleKeys();
    }

    /// <summary>Esc / Android Back: close the top-most thing first, then pause, then step back a screen.</summary>
    void Back()
    {
        switch (screen)
        {
            case Screen.Battle:
                if (dialog is Dialog.Sell or Dialog.Restart) { dialog = Dialog.None; return; }
                if (dialog == Dialog.Pause) { Resume(); return; }
                if (dialog == Dialog.Result) { LeaveBattle(); return; }
                if (buildKind != null || selectedTower >= 0) { CancelSelection(); return; }
                OpenPause();
                return;
            case Screen.Settings: CloseSettings(); return;
            case Screen.Help: screen = returnScreen; return;
            case Screen.Setup: if (dialog == Dialog.Overwrite) dialog = Dialog.None; else screen = Screen.Maps; return;
            case Screen.Maps: screen = Screen.Menu; return;
            case Screen.Bench: if (benchDone) screen = Screen.Settings; return;
            case Screen.RestoreError: screen = Screen.Menu; return;
            case Screen.Armory: case Screen.Achievements: screen = Screen.Menu; return;
        }
    }

    // ---------------------------------------------------------------- widgets

    void Toast(string text) { toast = text; toastTime = 2.6; }

    void DrawToast()
    {
        if (toastTime <= 0 || toast.Length == 0) return;
        var size = 19f; var w = (int)Math.Min(W - 40, p.Measure(toast, size) + 48);
        var y = screen == Screen.Battle ? (int)(safe.Bottom - Math.Max(88, hit + 30) - 64) : (int)(safe.Bottom - 80);
        var r = new Rectangle((int)(W / 2 - w / 2), y, w, 46);
        var alpha = (float)Math.Clamp(toastTime / .4, 0, 1);
        p.Frame(r, new Color(28, 36, 36) * alpha, Painter.Gold * alpha, 12);
        p.Center(toast, r, size, Painter.Ink * alpha);
    }

    void DrawStats()
    {
        var lines = $"FPS {fps:0}  update {updateMs:0.00} ms  draw {drawMs:0.00} ms  GC {GC.CollectionCount(0)}";
        if (session is { } s) lines += $"\nenemies {s.Enemies.Count}  towers {s.Towers.Count}  shots {s.Projectiles.Count}  fx {s.Effects.Count}";
        if (benchSession is { } b) lines += $"\nenemies {b.Enemies.Count}  towers {b.Towers.Count}  shots {b.Projectiles.Count}  fx {b.Effects.Count}";
        var r = new Rectangle(safe.X + 8, (int)(safe.Bottom - 60), (int)Math.Min(W - 16, 620), 54);
        p.Box(r, Color.Black * .65f, 6);
        p.Text(lines, r.X + 8, r.Y + 6, 15, Painter.Mint);
    }

    /// <summary>Widget rectangles of the last drawn frame, by id or label (used by UI test scripts).</summary>
    readonly Dictionary<string, Rectangle> widgets = [], drawingWidgets = [];

    bool Button(Rectangle r, string label, Style style = Style.Normal, bool enabled = true, float size = 0, string? id = null)
    {
        var widgetId = id ?? label;
        if (!input.Blocked) drawingWidgets[widgetId] = r;
        var hover = enabled && input.IsHover(r);
        Color fill = style switch
        {
            Style.Primary => new Color(34, 82, 140), Style.Danger => new Color(193, 72, 44),
            Style.Selected => new Color(42, 95, 151), Style.Ghost => new Color(29, 43, 64), _ => new Color(30, 51, 80)
        };
        if (!enabled) fill = new Color(37, 47, 63);
        if (hover) fill = Color.Lerp(fill, Color.White, .18f);
        var radius = Math.Min(r.Height * .23f, r.Width * .23f);
        var shadow = r; shadow.Offset(0, 5);
        p.Box(shadow, new Color(4, 10, 22) * .85f, radius);
        p.Frame(r, new Color(15, 30, 52), enabled ? (style == Style.Selected ? new Color(116, 189, 245) : new Color(94, 130, 174)) : new Color(66, 79, 98), radius, 3);
        var face = r; face.Inflate(-5, -5);
        p.Box(face, Color.Lerp(fill, Color.Black, .4f), Math.Max(2, radius - 5));
        face.Height = Math.Max(1, face.Height - 4);
        p.Box(face, fill, Math.Max(2, radius - 5));
        var shine = face; shine.Inflate(-5, -2); shine.Height = Math.Max(2, shine.Height / 3);
        p.Box(shine, Color.White * (enabled ? .14f : .04f), Math.Max(2, radius - 8));
        if (size <= 0) size = Math.Clamp(r.Height * .4f, 14, 22);
        while (size > 12 && p.MeasureDisplay(label, size) > r.Width - 20) size -= 1;
        if (label.Length > 0)
        {
            var textR = r; textR.Offset(0, 2);
            p.Center(label, textR, size, new Color(12, 20, 23) * .8f);
            p.DisplayCenter(label, r, size, enabled ? Painter.Ink : Painter.Muted);
        }
        RegisterFocus(r, widgetId, enabled);
        if (input.TryTap(r) || enabled && ActivateFocus(widgetId))
        {
            if (!enabled) return false;
            audio?.Play("click");
            return true;
        }
        return false;
    }

    /// <summary>A square button whose face is drawn by the caller (icons).</summary>
    bool IconButton(Rectangle r, Action<Vector2, float> icon, Style style = Style.Normal, bool enabled = true, string? id = null)
    {
        var tapped = Button(r, "", style, enabled, id: id);
        icon(new Vector2(r.Center.X, r.Center.Y), Math.Min(r.Width, r.Height) * .32f);
        return tapped;
    }

    void Panel(Rectangle r, float radius = 22)
    {
        var shadow = r; shadow.Offset(0, 7);
        p.Box(shadow, Color.Black * .4f, radius);
        p.Frame(r, new Color(17, 31, 53), new Color(86, 118, 159), radius, 3);
        var inner = r; inner.Inflate(-6, -6);
        p.Frame(inner, new Color(24, 43, 69), new Color(48, 74, 108), Math.Max(4, radius - 5), 1);
    }

    bool SymbolButton(Rectangle r, UiSymbol symbol, string id, Style style = Style.Normal, bool enabled = true, string? caption = null)
    {
        var result = Button(r, "", style, enabled, id: id);
        if (caption == null) p.ControlIcon(symbol, new Vector2(r.Center.X, r.Center.Y - 1), Math.Min(r.Width, r.Height) * .31f, enabled ? null : Painter.Muted);
        else { p.ControlIcon(symbol, new Vector2(r.X + r.Height * .48f, r.Center.Y), r.Height * .25f); var text = new Rectangle(r.X + (int)(r.Height * .9f), r.Y, r.Width - (int)(r.Height * .95f), r.Height); float ts = 20; while (ts > 12 && p.MeasureDisplay(caption, ts) > text.Width - 12) ts--; p.DisplayCenter(caption, text, ts, enabled ? Painter.Ink : Painter.Muted); }
        return result;
    }

    void WeaponIcon(TowerKind kind, Rectangle bounds, int level = 1)
    {
        field.Tower(new Vector2(bounds.Center.X, bounds.Center.Y), Math.Min(bounds.Width, bounds.Height), kind, level);
    }

    void ToggleSound() { Settings.EffectsVolume = Settings.EffectsVolume > 0 ? 0 : .8f; if (!store.SaveSettings()) Toast(T("save_failed")); }
    void ToggleMusic() { Settings.MusicVolume = Settings.MusicVolume > 0 ? 0 : .65f; if (!store.SaveSettings()) Toast(T("save_failed")); }

    void Dim() => p.Rect(0, 0, W, H, Color.Black * .55f);

    void Title(string text, float y, float size = 34) => p.Text(text, safe.X + 32, y, size, Painter.Ink);

    bool BackButton()
    {
        var r = new Rectangle(safe.X + 20, safe.Y + 16, (int)Math.Max(130, hit * 2.4f), (int)hit);
        return SymbolButton(r, UiSymbol.Back, "back", caption: T("back"));
    }

    void DrawLoading()
    {
        DrawNavyMenuBackground();
        p.DisplayCenter(T("title"), new Rectangle(0, 0, (int)W, (int)(H * .8f)), 72, new Color(232, 242, 255));
        var bar = new Rectangle((int)(W / 2 - 190), (int)(H * .58f), 380, 18);
        p.Frame(bar, new Color(17, 28, 33), new Color(139, 115, 62), 7, 2);
        p.Box(new Rectangle(bar.X + 4, bar.Y + 4, (int)((bar.Width - 8) * Math.Min(1, (loadStep + 1) / 4f)), bar.Height - 8), Painter.Gold, 4);
        p.Center(loadStep < 1 ? "Yuklanmoqda… / Loading…" : T("loading"), new Rectangle(0, bar.Bottom + 16, (int)W, 40), 18, Painter.Ink);
    }

    // ---------------------------------------------------------------- persistence

    bool SaveBattle(bool quiet = false)
    {
        if (session is not { } s || s.State is SessionState.Victory or SessionState.Defeat) return true;
        var ok = store.SaveBattle(s.Capture());
        if (!ok) Toast(T("save_failed"));
        else if (!quiet) Toast(T("saved"));
        return ok;
    }

    void Achieve(string id)
    {
        if (!store.UnlockAchievement(id)) Toast(T("save_failed"));
        storefront.Unlock(id);
    }

    static readonly RasterizerState ScissorState = new() { ScissorTestEnable = true, CullMode = CullMode.None };

    /// <summary>Draws into a logical rectangle with everything outside it clipped (zoomed battlefield).</summary>
    void Clipped(Rectangle area, Action draw)
    {
        p.Batch.End();
        var pp = GraphicsDevice.PresentationParameters;
        var pixels = new Rectangle((int)(area.X * scale), (int)(area.Y * scale), (int)(area.Width * scale), (int)(area.Height * scale));
        GraphicsDevice.ScissorRectangle = Rectangle.Intersect(pixels, new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight));
        p.Batch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: ScissorState, transformMatrix: Matrix.CreateScale(scale));
        draw();
        p.Batch.End();
        p.Batch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: Matrix.CreateScale(scale));
    }
}
