using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace TowerGame.UI;

/// <summary>
/// Mouse, touch and keyboard folded into logical-coordinate events. A tap is a press and release
/// without dragging and without a second finger; widgets consume it so it reaches one target only.
/// </summary>
public sealed class InputState
{
    KeyboardState keys, previousKeys;
    MouseState mouse, previousMouse;
    Vector2 mouseDown; bool mouseDragged, firstFrame = true;
    int touchId = -1; Vector2 touchStart; bool touchMoved, multiTouch;
    float pinchDistance; Vector2 pinchCenter; bool pinching;
    bool backWasDown;
    float scale = 1; Vector2 offset;

    public Vector2? Hover { get; private set; }
    public Vector2? Tap { get; private set; }
    /// <summary>When set, widgets ignore taps (used while a modal draws underlying layers).</summary>
    public bool Blocked { get; set; }
    public Vector2 PanDelta { get; private set; }
    public float ZoomFactor { get; private set; } = 1;
    public Vector2 ZoomCenter { get; private set; }
    public bool Cancel { get; private set; }
    public bool Back { get; private set; }
    public bool UsedTouch { get; private set; }
    public Keys? PressedKey { get; private set; }

    public void Update(float uiScale, Vector2 uiOffset, float density, bool windowActive, bool touchHost)
    {
        scale = uiScale; offset = uiOffset;
        PanDelta = Vector2.Zero; ZoomFactor = 1; Cancel = false; Back = false; PressedKey = null; injectedKey = null;
        previousKeys = keys; previousMouse = mouse;
        keys = Keyboard.GetState(); mouse = Mouse.GetState();
        if (firstFrame) { previousKeys = keys; previousMouse = mouse; firstFrame = false; }
        foreach (var k in keys.GetPressedKeys()) if (!previousKeys.IsKeyDown(k)) { PressedKey = k; break; }

        var backDown = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
        Back = backDown && !backWasDown; backWasDown = backDown;

        var touching = ReadTouch(density);
        // A hybrid laptop switches back to mouse mode as soon as the cursor moves.
        if (!touchHost && !touching && (mouse.X != previousMouse.X || mouse.Y != previousMouse.Y)) UsedTouch = false;
        if (!touchHost && !UsedTouch && windowActive) ReadMouse();
        else if (!UsedTouch && windowActive) Hover = null;
        ApplyQueued();
    }

    Vector2 Logical(Vector2 p) => (p - offset) / scale;

    void ReadMouse()
    {
        var at = new Vector2(mouse.X, mouse.Y);
        Hover = Logical(at);
        if (mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released) { mouseDown = at; mouseDragged = false; }
        if (mouse.LeftButton == ButtonState.Pressed && Vector2.Distance(at, mouseDown) > 8) mouseDragged = true;
        if (mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed && !mouseDragged) Tap = Logical(at);
        if (mouse.RightButton == ButtonState.Pressed && previousMouse.RightButton == ButtonState.Released) Cancel = true;
        if (mouse.MiddleButton == ButtonState.Pressed && previousMouse.MiddleButton == ButtonState.Pressed)
            PanDelta = new Vector2(mouse.X - previousMouse.X, mouse.Y - previousMouse.Y) / scale;
        var wheel = mouse.ScrollWheelValue - previousMouse.ScrollWheelValue;
        if (wheel != 0) { ZoomFactor = MathF.Pow(1.12f, wheel / 120f); ZoomCenter = Hover.Value; }
    }

    bool ReadTouch(float density)
    {
        TouchCollection touches;
        try { touches = TouchPanel.GetState(); }
        catch (Exception) { return false; } // Touch is optional on desktop.
        if (touches.Count == 0)
        {
            pinching = false;
            if (touchId != -1) touchId = -1;
            return false;
        }
        UsedTouch = true; Hover = null;
        var active = new List<TouchLocation>(4);
        foreach (var t in touches) if (t.State is TouchLocationState.Pressed or TouchLocationState.Moved) active.Add(t);
        if (active.Count >= 2)
        {
            multiTouch = true;
            var a = active[0].Position; var b = active[1].Position;
            var distance = Vector2.Distance(a, b); var center = (a + b) / 2;
            if (pinching && pinchDistance > 1)
            {
                ZoomFactor = distance / pinchDistance; ZoomCenter = Logical(center);
                PanDelta = (center - pinchCenter) / scale;
            }
            pinching = true; pinchDistance = distance; pinchCenter = center;
            return true;
        }
        pinching = false;
        foreach (var t in touches)
        {
            if (t.State == TouchLocationState.Pressed && touchId == -1)
            { touchId = t.Id; touchStart = t.Position; touchMoved = false; multiTouch = false; }
            else if (t.Id == touchId && t.State == TouchLocationState.Moved && Vector2.Distance(t.Position, touchStart) > 10 * density)
                touchMoved = true;
            else if (t.Id == touchId && t.State == TouchLocationState.Released)
            {
                if (!touchMoved && !multiTouch) Tap = Logical(t.Position);
                touchId = -1;
            }
        }
        return true;
    }

    // ---- Scripted input for automated UI tests (--script). Injected events behave like real ones.
    // Non-tap events are queued and applied by the next Update, where keys and gestures are read.
    Keys? injectedKey, queuedKey; bool queuedCancel, queuedBack; (Vector2 Center, float Factor)? queuedZoom;
    public void InjectTap(Vector2 logical) { Tap = logical; Hover = logical; }
    public void InjectKey(Keys key) => queuedKey = key;
    public void InjectCancel() => queuedCancel = true;
    public void InjectBack() => queuedBack = true;
    public void InjectZoom(Vector2 center, float factor) => queuedZoom = (center, factor);
    void ApplyQueued()
    {
        if (queuedKey is { } k) { injectedKey = k; PressedKey = k; queuedKey = null; }
        if (queuedCancel) { Cancel = true; queuedCancel = false; }
        if (queuedBack) { Back = true; queuedBack = false; }
        if (queuedZoom is { } z) { ZoomCenter = z.Center; ZoomFactor = z.Factor; queuedZoom = null; }
    }

    /// <summary>Taps survive extra Update calls until a Draw pass has offered them to widgets.</summary>
    public void EndFrame() => Tap = null;

    /// <summary>Consumes the pending tap if it lies inside r.</summary>
    public bool TryTap(Rectangle r)
    {
        if (Blocked || Tap is not { } t || !r.Contains(t)) return false;
        Tap = null; return true;
    }
    public bool TapOutside(Rectangle r) => !Blocked && Tap is { } t && !r.Contains(t);
    public void ConsumeTap() => Tap = null;
    public bool IsHover(Rectangle r) => !Blocked && Hover is { } h && r.Contains(h);
    public bool Pressed(Keys key) => injectedKey == key || keys.IsKeyDown(key) && !previousKeys.IsKeyDown(key);
    public bool Pressed(string? keyName) => keyName != null && Enum.TryParse<Keys>(keyName, out var k) && Pressed(k);

    public static string KeyLabel(string name) => name switch
    {
        "D0" or "D1" or "D2" or "D3" or "D4" or "D5" or "D6" or "D7" or "D8" or "D9" => name[1..],
        "NumPad0" or "NumPad1" or "NumPad2" or "NumPad3" or "NumPad4" or "NumPad5" or "NumPad6" or "NumPad7" or "NumPad8" or "NumPad9" => "Num " + name[^1],
        "OemPlus" => "+", "OemMinus" => "-", "OemComma" => ",", "OemPeriod" => ".", "Back" => "Backspace",
        _ => name
    };
}
