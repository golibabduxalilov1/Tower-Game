using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerGame.Rendering;

public sealed class AtlasData
{
    public string Characters { get; set; } = "";
    public float Size { get; set; } = 32;
    public int CellWidth { get; set; } = 40;
    public int CellHeight { get; set; } = 52;
    public int Columns { get; set; } = 16;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; } = 4;
    public float[] Widths { get; set; } = [];
}

/// <summary>Immediate geometric drawing on top of a few textures generated once at startup.</summary>
public sealed partial class Painter : IDisposable
{
    public readonly SpriteBatch Batch;
    private readonly Texture2D pixel, disc, font;
    private readonly AtlasData atlas;
    private readonly Dictionary<char, int> glyphs = [];
    public static readonly Color Ink = new(234, 243, 255);
    public static readonly Color Muted = new(151, 173, 201);
    public static readonly Color Mint = new(117, 221, 127);
    public static readonly Color Gold = new(247, 195, 87);
    public static readonly Color Danger = new(245, 125, 113);
    public static readonly Color Panel = new(18, 34, 59);
    public static readonly Color PanelLight = new(35, 58, 89);
    public static readonly Color Backdrop = new(9, 19, 36);
    readonly List<Texture2D> illustratedTextures = [];
    public Texture2D CreateTexture(int width, int height, Color[] colors)
    {
        var texture = new Texture2D(Batch.GraphicsDevice, width, height);
        texture.SetData(colors); illustratedTextures.Add(texture); return texture;
    }
    public Painter(GraphicsDevice device, Stream fontPng, Stream fontJson)
    {
        Batch = new SpriteBatch(device);
        pixel = new Texture2D(device, 1, 1);
        pixel.SetData(new[] { Color.White });
        disc = new Texture2D(device, 96, 96);
        var colors = new Color[96 * 96];
        for (var y = 0; y < 96; y++) for (var x = 0; x < 96; x++)
        {
            var distance = MathF.Sqrt((x - 47.5f) * (x - 47.5f) + (y - 47.5f) * (y - 47.5f));
            colors[y * 96 + x] = Color.White * Math.Clamp(48 - distance, 0, 1);
        }
        disc.SetData(colors);
        font = Texture2D.FromStream(device, fontPng);
        // FromStream yields straight alpha; SpriteBatch defaults to premultiplied blending.
        var data = new Color[font.Width * font.Height];
        font.GetData(data);
        for (var i = 0; i < data.Length; i++) data[i] = Color.FromNonPremultiplied(data[i].R, data[i].G, data[i].B, data[i].A);
        font.SetData(data);
        atlas = JsonSerializer.Deserialize<AtlasData>(fontJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        for (var i = 0; i < atlas.Characters.Length; i++) glyphs.TryAdd(atlas.Characters[i], i);
        // Typographic apostrophes used in Uzbek share glyphs when the atlas lacks a variant.
        foreach (var (alias, target) in new[] { ('ʻ', '‘'), ('ʼ', '’'), ('−', '-'), (' ', ' ') })
            if (!glyphs.ContainsKey(alias) && glyphs.TryGetValue(target, out var id)) glyphs[alias] = id;
    }
    public void Rect(float x, float y, float w, float h, Color color) =>
        Batch.Draw(pixel, new Rectangle((int)x, (int)y, Math.Max(1, (int)w), Math.Max(1, (int)h)), color);
    public void Box(Rectangle r, Color color, float radius = 10)
    {
        // Non-overlapping pieces (quarter discs + three rectangles) keep translucent fills even.
        var rr = (int)Math.Min(radius, Math.Min(r.Width, r.Height) / 2f);
        if (rr < 1) { Rect(r.X, r.Y, r.Width, r.Height, color); return; }
        Batch.Draw(pixel, new Rectangle(r.X + rr, r.Y, r.Width - rr * 2, r.Height), color);
        Batch.Draw(pixel, new Rectangle(r.X, r.Y + rr, rr, r.Height - rr * 2), color);
        Batch.Draw(pixel, new Rectangle(r.Right - rr, r.Y + rr, rr, r.Height - rr * 2), color);
        Batch.Draw(disc, new Rectangle(r.X, r.Y, rr, rr), new Rectangle(0, 0, 48, 48), color);
        Batch.Draw(disc, new Rectangle(r.Right - rr, r.Y, rr, rr), new Rectangle(48, 0, 48, 48), color);
        Batch.Draw(disc, new Rectangle(r.X, r.Bottom - rr, rr, rr), new Rectangle(0, 48, 48, 48), color);
        Batch.Draw(disc, new Rectangle(r.Right - rr, r.Bottom - rr, rr, rr), new Rectangle(48, 48, 48, 48), color);
    }
    public void Frame(Rectangle r, Color fill, Color border, float radius = 10, int thickness = 2)
    {
        Box(r, border, radius);
        var inner = r; inner.Inflate(-thickness, -thickness);
        Box(inner, fill, Math.Max(0, radius - thickness));
    }
    public void Circle(Vector2 p, float radius, Color color) =>
        Batch.Draw(disc, p, null, color, 0, new Vector2(48), radius / 48, SpriteEffects.None, 0);
    public void Ring(Vector2 p, float radius, Color color, float thickness = 1.5f, int sides = 64)
    {
        for (var i = 0; i < sides; i++)
        {
            float a = i * MathHelper.TwoPi / sides, b = (i + 1) * MathHelper.TwoPi / sides;
            Line(p + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius, p + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius, color, thickness);
        }
    }
    public void Line(Vector2 a, Vector2 b, Color color, float width = 2)
    {
        var delta = b - a;
        Batch.Draw(pixel, a, null, color, MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
    public void Polygon(Vector2[] points, Color color, float width = 2)
    {
        for (var i = 0; i < points.Length; i++) { Line(points[i], points[(i + 1) % points.Length], color, width); if (width > 2) Circle(points[i], width / 2, color); }
    }

    // ---- Code-drawn icons (the bundled font has no star/check/arrow glyphs). ----
    public void Star(Vector2 c, float r, Color color, bool filled = true)
    {
        if (filled) { ControlIcon(UiSymbol.Star, c, r * 1.28f, Color.White); return; }
        var pts = new Vector2[10];
        for (var i = 0; i < 10; i++)
        {
            var a = -MathHelper.PiOver2 + i * MathHelper.Pi / 5; var rr = i % 2 == 0 ? r : r * .45f;
            pts[i] = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * rr;
        }
        Polygon(pts, color, Math.Max(1.5f, r * .12f));
    }
    public void Heart(Vector2 c, float r, Color color)
    {
        Circle(c + new Vector2(-r * .45f, -r * .2f), r * .5f, color);
        Circle(c + new Vector2(r * .45f, -r * .2f), r * .5f, color);
        Line(c + new Vector2(-r * .85f, -r * .05f), c + new Vector2(0, r * .85f), color, r * .55f);
        Line(c + new Vector2(r * .85f, -r * .05f), c + new Vector2(0, r * .85f), color, r * .55f);
        Circle(c + new Vector2(0, r * .2f), r * .45f, color);
    }
    public void Coin(Vector2 c, float r, Color color)
    {
        Circle(c, r, color); Ring(c, r * .7f, Backdrop * .45f, Math.Max(1, r * .14f), 20);
    }
    public void Check(Vector2 c, float r, Color color) =>
        Polyline(color, r * .28f, c + new Vector2(-r * .8f, 0), c + new Vector2(-r * .2f, r * .6f), c + new Vector2(r * .85f, -r * .6f));
    public void Cross(Vector2 c, float r, Color color)
    {
        Line(c + new Vector2(-r, -r) * .7f, c + new Vector2(r, r) * .7f, color, r * .26f);
        Line(c + new Vector2(-r, r) * .7f, c + new Vector2(r, -r) * .7f, color, r * .26f);
    }
    public void PauseIcon(Vector2 c, float r, Color color)
    {
        Rect(c.X - r * .6f, c.Y - r * .7f, r * .4f, r * 1.4f, color);
        Rect(c.X + r * .2f, c.Y - r * .7f, r * .4f, r * 1.4f, color);
    }
    public void PlayIcon(Vector2 c, float r, Color color, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            var o = c + new Vector2((i - (count - 1) * .5f) * r * .9f, 0);
            // Filled triangle as vertical strips of shrinking height.
            for (var t = 0f; t < 1; t += .08f)
            {
                var h = r * .7f * (1 - t);
                Line(o + new Vector2(-r * .5f + r * 1.1f * t, -h), o + new Vector2(-r * .5f + r * 1.1f * t, h), color, r * .1f);
            }
        }
    }
    public void Lock(Vector2 c, float r, Color color)
    {
        Ring(c + new Vector2(0, -r * .35f), r * .45f, color, r * .16f, 16);
        Box(new Rectangle((int)(c.X - r * .7f), (int)(c.Y - r * .2f), (int)(r * 1.4f), (int)(r * 1.1f)), color, r * .15f);
    }
    public void Polyline(Color color, float width, params Vector2[] points)
    {
        for (var i = 1; i < points.Length; i++) { Line(points[i - 1], points[i], color, width); Circle(points[i], width / 2, color); }
    }

    // ---- Text ----
    public float LineHeight(float size) => size * 1.45f;
    public float Measure(string text, float size = 18)
    {
        float w = 0, best = 0;
        foreach (var ch in text)
        {
            if (ch == '\n') { best = Math.Max(best, w); w = 0; continue; }
            w += GlyphWidth(ch) * size / atlas.Size;
        }
        return Math.Max(best, w);
    }
    float GlyphWidth(char c)
    {
        var id = glyphs.GetValueOrDefault(c, glyphs.GetValueOrDefault('?'));
        return id < atlas.Widths.Length ? atlas.Widths[id] : atlas.CellWidth * .55f;
    }
    public void Text(string text, float x, float y, float size = 18, Color? color = null)
    {
        float scale = size / atlas.Size, startX = x;
        foreach (var ch in text)
        {
            if (ch == '\n') { y += LineHeight(size); x = startX; continue; }
            int id = glyphs.GetValueOrDefault(ch, glyphs.GetValueOrDefault('?'));
            var source = new Rectangle(id % atlas.Columns * atlas.CellWidth, id / atlas.Columns * atlas.CellHeight, atlas.CellWidth, atlas.CellHeight);
            Batch.Draw(font, new Vector2(x - atlas.OffsetX * scale, y - size * .137f - atlas.OffsetY * scale), source, color ?? Ink, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            x += GlyphWidth(ch) * scale;
        }
    }
    public void Center(string text, Rectangle r, float size = 18, Color? color = null) =>
        Text(text, r.Center.X - Measure(text, size) / 2, r.Center.Y - size * .62f, size, color);
    public void Right(string text, float right, float y, float size = 18, Color? color = null) =>
        Text(text, right - Measure(text, size), y, size, color);
    /// <summary>Word-wraps text inside r and returns the height used.</summary>
    public float Paragraph(string text, Rectangle r, float size = 17, Color? color = null, bool draw = true)
    {
        float y = r.Y;
        foreach (var block in text.Split('\n'))
        {
            string line = "";
            foreach (var word in block.Split(' '))
            {
                if (Measure(line + word, size) > r.Width && line.Length > 0)
                { if (draw) Text(line.TrimEnd(), r.X, y, size, color); y += LineHeight(size); line = ""; }
                line += word + " ";
            }
            if (draw) Text(line.TrimEnd(), r.X, y, size, color);
            y += LineHeight(size);
        }
        return y - r.Y;
    }
    public void Dispose() { foreach (var texture in illustratedTextures) texture.Dispose(); illustratedTextures.Clear(); Batch.Dispose(); pixel.Dispose(); disc.Dispose(); font.Dispose(); }
}
