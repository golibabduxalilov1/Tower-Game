using Microsoft.Xna.Framework;

namespace TowerGame.Rendering;

/// <summary>Small offline raster painter. Every illustrated sprite is made once from code,
/// at high resolution, then reused by SpriteBatch. No external art or runtime image library.</summary>
internal sealed class IllustrationCanvas(int width, int height)
{
    public readonly int Width = width, Height = height;
    public readonly Color[] Pixels = new Color[width * height];
    public static Color C(int r, int g, int b, int a = 255) => new(r, g, b, a);
    public static Color Shade(Color c, float factor) => new((byte)Math.Clamp(c.R * factor, 0, 255), (byte)Math.Clamp(c.G * factor, 0, 255), (byte)Math.Clamp(c.B * factor, 0, 255), c.A);
    public void Pixel(int x, int y, Color c)
    {
        if ((uint)x >= Width || (uint)y >= Height || c.A == 0) return;
        if (c.A == 255) { Pixels[y * Width + x] = c; return; }
        var old = Pixels[y * Width + x]; float a = c.A / 255f, oa = old.A / 255f, aa = a + oa * (1 - a);
        if (aa <= 0) return;
        Pixels[y * Width + x] = new Color((byte)((c.R * a + old.R * oa * (1 - a)) / aa), (byte)((c.G * a + old.G * oa * (1 - a)) / aa), (byte)((c.B * a + old.B * oa * (1 - a)) / aa), (byte)(aa * 255));
    }
    public void Fill(Color c) => Array.Fill(Pixels, c);
    public void Rect(float x, float y, float w, float h, Color c)
    {
        for (int yy = Math.Max(0, (int)y); yy < Math.Min(Height, y + h); yy++)
            for (int xx = Math.Max(0, (int)x); xx < Math.Min(Width, x + w); xx++) Pixel(xx, yy, c);
    }
    public void Ellipse(float x, float y, float rx, float ry, Color c, bool shade = false)
    {
        if (rx <= 0 || ry <= 0) return;
        for (int yy = Math.Max(0, (int)(y - ry - 1)); yy <= Math.Min(Height - 1, y + ry + 1); yy++)
            for (int xx = Math.Max(0, (int)(x - rx - 1)); xx <= Math.Min(Width - 1, x + rx + 1); xx++)
            {
                float nx = (xx + .5f - x) / rx, ny = (yy + .5f - y) / ry, dist = nx * nx + ny * ny;
                if (dist > 1) continue;
                var color = shade ? Shade(c, .62f + .38f * MathF.Sqrt(1 - dist) - nx * .18f - ny * .26f) : c;
                var coverage = Math.Clamp((1 - MathF.Sqrt(dist)) * Math.Min(rx, ry), 0, 1);
                color.A = (byte)(color.A * coverage); Pixel(xx, yy, color);
            }
    }
    public void Ring(float x, float y, float rx, float ry, float thickness, Color c)
    {
        for (int yy = Math.Max(0, (int)(y - ry - 1)); yy <= Math.Min(Height - 1, y + ry + 1); yy++)
            for (int xx = Math.Max(0, (int)(x - rx - 1)); xx <= Math.Min(Width - 1, x + rx + 1); xx++)
            {
                float nx = (xx + .5f - x) / rx, ny = (yy + .5f - y) / ry, d = MathF.Sqrt(nx * nx + ny * ny);
                if (d <= 1 && d >= 1 - thickness / Math.Min(rx, ry)) Pixel(xx, yy, c);
            }
    }
    public void Round(float x, float y, float w, float h, float r, Color top, Color? bottom = null)
    {
        r = Math.Min(r, Math.Min(w, h) / 2);
        for (int yy = Math.Max(0, (int)y); yy < Math.Min(Height, y + h); yy++)
            for (int xx = Math.Max(0, (int)x); xx < Math.Min(Width, x + w); xx++)
            {
                var dx = Math.Max(x + r - xx - .5f, Math.Max(0, xx + .5f - (x + w - r)));
                var dy = Math.Max(y + r - yy - .5f, Math.Max(0, yy + .5f - (y + h - r)));
                if (dx * dx + dy * dy <= r * r)
                    Pixel(xx, yy, bottom is { } b ? Color.Lerp(top, b, Math.Clamp((yy - y) / h, 0, 1)) : top);
            }
    }
    public void Line(float ax, float ay, float bx, float by, float width, Color c)
    {
        float dx = bx - ax, dy = by - ay, len = MathF.Sqrt(dx * dx + dy * dy), r = width / 2;
        if (len == 0) { Ellipse(ax, ay, r, r, c); return; }
        for (int yy = Math.Max(0, (int)(Math.Min(ay, by) - r - 1)); yy <= Math.Min(Height - 1, Math.Max(ay, by) + r + 1); yy++)
            for (int xx = Math.Max(0, (int)(Math.Min(ax, bx) - r - 1)); xx <= Math.Min(Width - 1, Math.Max(ax, bx) + r + 1); xx++)
            {
                float t = Math.Clamp(((xx + .5f - ax) * dx + (yy + .5f - ay) * dy) / (len * len), 0, 1);
                float px = xx + .5f - (ax + dx * t), py = yy + .5f - (ay + dy * t);
                float distance = MathF.Sqrt(px * px + py * py), aa = Math.Clamp(r + .5f - distance, 0, 1);
                if (aa > 0) { var color = c; color.A = (byte)(c.A * aa); Pixel(xx, yy, color); }
            }
    }
    public void Poly(Color c, params Vector2[] points)
    {
        if (points.Length < 3) return;
        var minY = Math.Max(0, (int)points.Min(p => p.Y)); var maxY = Math.Min(Height - 1, (int)Math.Ceiling(points.Max(p => p.Y)));
        var intersections = new List<float>(points.Length);
        for (int y = minY; y <= maxY; y++)
        {
            intersections.Clear();
            for (int i = 0; i < points.Length; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Length];
                if ((a.Y <= y + .5f && b.Y > y + .5f) || (b.Y <= y + .5f && a.Y > y + .5f))
                    intersections.Add(a.X + (y + .5f - a.Y) / (b.Y - a.Y) * (b.X - a.X));
            }
            intersections.Sort();
            for (int i = 0; i + 1 < intersections.Count; i += 2)
                for (int x = Math.Max(0, (int)intersections[i]); x <= Math.Min(Width - 1, intersections[i + 1]); x++) Pixel(x, y, c);
        }
    }
    public void Copy(IllustrationCanvas source, float x, float y, float scale = 1)
    {
        for (int yy = 0; yy < source.Height * scale; yy++)
            for (int xx = 0; xx < source.Width * scale; xx++)
                Pixel((int)x + xx, (int)y + yy, source.Pixels[Math.Clamp((int)(yy / scale), 0, source.Height - 1) * source.Width + Math.Clamp((int)(xx / scale), 0, source.Width - 1)]);
    }
    public Color[] Finish()
    {
        for (int i = 0; i < Pixels.Length; i++) Pixels[i] = Color.FromNonPremultiplied(Pixels[i].R, Pixels[i].G, Pixels[i].B, Pixels[i].A);
        return Pixels;
    }
    public static uint Hash(int x, int y, int seed = 0)
    { uint n = unchecked((uint)(x * 374761393 + y * 668265263 + seed * 2135587861)); n = (n ^ (n >> 13)) * 1274126177; return n ^ (n >> 16); }
}
