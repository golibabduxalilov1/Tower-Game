using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerGame.Rendering;

public sealed partial class Painter
{
    Texture2D? displayTexture;
    AtlasData? displayAtlas;
    readonly Dictionary<char, int> displayGlyphs = [];

    public void LoadDisplayAtlas(GraphicsDevice device, Stream png, Stream json)
    {
        using var source = Texture2D.FromStream(device, png);
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = Color.FromNonPremultiplied(pixels[i].R, pixels[i].G, pixels[i].B, pixels[i].A);
        displayTexture = CreateTexture(source.Width, source.Height, pixels);
        displayAtlas = JsonSerializer.Deserialize<AtlasData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        displayGlyphs.Clear();
        for (var i = 0; i < displayAtlas.Characters.Length; i++) displayGlyphs.TryAdd(displayAtlas.Characters[i], i);
        foreach (var (alias, target) in new[] { ('ʻ', '‘'), ('ʼ', '’'), ('−', '-'), (' ', ' ') })
            if (!displayGlyphs.ContainsKey(alias) && displayGlyphs.TryGetValue(target, out var index)) displayGlyphs[alias] = index;
    }

    public float MeasureDisplay(string text, float size = 48)
    {
        if (displayAtlas == null) return Measure(text, size);
        float line = 0, width = 0;
        foreach (var c in text)
        {
            if (c == '\n') { width = Math.Max(width, line); line = 0; continue; }
            var id = displayGlyphs.GetValueOrDefault(c, displayGlyphs.GetValueOrDefault('?'));
            line += displayAtlas.Widths[id] * size / displayAtlas.Size;
        }
        return Math.Max(width, line);
    }

    public void Display(string text, float x, float y, float size = 48, Color? color = null, bool outline = true)
    {
        if (displayTexture == null || displayAtlas == null) { Text(text, x, y, size, color); return; }
        var ink = color ?? Gold;
        if (outline)
        {
            var stroke = Math.Max(1.1f, size * .035f);
            DisplayRun(text, x + stroke, y + stroke * 2.2f, size, new Color(14, 24, 21, (int)ink.A));
            DisplayRun(text, x - stroke, y, size, new Color(22, 31, 25, (int)ink.A));
            DisplayRun(text, x + stroke, y, size, new Color(22, 31, 25, (int)ink.A));
            DisplayRun(text, x, y - stroke, size, new Color(22, 31, 25, (int)ink.A));
        }
        DisplayRun(text, x, y, size, ink);
    }

    void DisplayRun(string text, float x, float y, float size, Color color)
    {
        var a = displayAtlas!; var factor = size / a.Size; var initial = x;
        foreach (var c in text)
        {
            if (c == '\n') { x = initial; y += size * 1.2f; continue; }
            var id = displayGlyphs.GetValueOrDefault(c, displayGlyphs.GetValueOrDefault('?'));
            var source = new Rectangle(id % a.Columns * a.CellWidth, id / a.Columns * a.CellHeight, a.CellWidth, a.CellHeight);
            Batch.Draw(displayTexture!, new Vector2(x - a.OffsetX * factor, y - size * .137f - a.OffsetY * factor), source, color, 0, Vector2.Zero, factor, SpriteEffects.None, 0);
            x += a.Widths[id] * factor;
        }
    }

    public void DisplayCenter(string text, Rectangle bounds, float size = 48, Color? color = null) =>
        Display(text, bounds.Center.X - MeasureDisplay(text, size) / 2, bounds.Center.Y - size * .62f, size, color);
}
