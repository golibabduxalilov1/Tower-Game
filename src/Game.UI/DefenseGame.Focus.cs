using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TowerGame.Rendering;

namespace TowerGame.UI;

public sealed partial class DefenseGame
{
    readonly Dictionary<string, Rectangle> focusWidgets = [], drawingFocusWidgets = [];
    string? focusedWidget;
    bool focusActivate;
    Screen focusScreen;
    Dialog focusDialog;

    void UpdateFocus()
    {
        if (focusScreen != screen || focusDialog != dialog)
        {
            focusedWidget = null; focusActivate = false; focusScreen = screen; focusDialog = dialog;
            focusWidgets.Clear();
        }
        if (input.Tap != null) focusedWidget = null;
        if (input.Pressed(Keys.Tab) && focusWidgets.Count > 0)
        {
            var ids = focusWidgets.Keys.ToArray();
            var current = Array.IndexOf(ids, focusedWidget);
            var backwards = Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift);
            var next = backwards ? (current < 0 ? ids.Length - 1 : (current - 1 + ids.Length) % ids.Length) : (current + 1) % ids.Length;
            focusedWidget = ids[next];
        }
        if (focusedWidget != null && input.Pressed(Keys.Enter) && !Keyboard.GetState().IsKeyDown(Keys.LeftAlt) && !Keyboard.GetState().IsKeyDown(Keys.RightAlt))
            focusActivate = true;
    }

    void RegisterFocus(Rectangle bounds, string id, bool enabled)
    {
        if (input.Blocked || !enabled) return;
        drawingFocusWidgets[id] = bounds;
        if (focusedWidget != id) return;
        p.Polygon([new(bounds.X - 3, bounds.Y - 3), new(bounds.Right + 3, bounds.Y - 3), new(bounds.Right + 3, bounds.Bottom + 3), new(bounds.X - 3, bounds.Bottom + 3)], Painter.Gold, 2);
    }

    bool ActivateFocus(string id)
    {
        if (input.Blocked || focusedWidget != id || !focusActivate) return false;
        focusActivate = false;
        return true;
    }
}
