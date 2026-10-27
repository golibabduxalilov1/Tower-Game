using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerGame.Core;

namespace TowerGame.Rendering;

public sealed partial class Painter
{
    private sealed class SuppliedManifest
    {
        public Dictionary<string, SuppliedEntry> Assets { get; set; } = [];
    }
    private sealed class SuppliedEntry
    {
        public string File { get; set; } = "";
        public int Cell { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int AnchorX { get; set; }
        public int AnchorY { get; set; }
        public int DrawSize { get; set; }
        public int ReferenceHeight { get; set; }
        public SuppliedDirection[] Directions { get; set; } = [];
        public float[] HealthOffsets { get; set; } = [];
    }
    private sealed class SuppliedDirection
    {
        public int Angle { get; set; }
        public int Start { get; set; }
        public int Count { get; set; }
    }
    private readonly Dictionary<string, Texture2D> suppliedTextures = [];
    private readonly Dictionary<string, SuppliedEntry> suppliedEntries = [];
    private bool suppliedLoaded;

    /// <summary>Load selected user-supplied PNG artwork; absent files retain illustrated fallbacks.</summary>
    public void LoadSuppliedArt(GraphicsDevice device)
    {
        if (suppliedLoaded) return;
        suppliedLoaded = true;
        try
        {
            using var stream = TitleContainer.OpenStream("Content/imported/manifest.json");
            var manifest = JsonSerializer.Deserialize<SuppliedManifest>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifest == null) return;
            foreach (var (key, entry) in manifest.Assets)
            {
                if (entry.File != Path.GetFileName(entry.File) || !entry.File.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    using var png = TitleContainer.OpenStream("Content/imported/" + entry.File);
                    using var decoded = Texture2D.FromStream(device, png);
                    var colors = new Color[decoded.Width * decoded.Height];
                    decoded.GetData(colors);
                    if (key.StartsWith("tower:", StringComparison.Ordinal) || key.StartsWith("icon:", StringComparison.Ordinal))
                    {
                        for (int i = 0; i < colors.Length; i++) colors[i] = NavyMetal(colors[i]);
                        if (key.StartsWith("icon:", StringComparison.Ordinal)) SharpenIcon(colors, decoded.Width, decoded.Height);
                    }
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = Color.FromNonPremultiplied(colors[i].R, colors[i].G, colors[i].B, colors[i].A);
                    suppliedTextures[key] = CreateTexture(decoded.Width, decoded.Height, colors);
                    suppliedEntries[key] = entry;
                }
                catch (IOException) { /* Optional art: continue loading the other assets. */ }
            }
        }
        catch (IOException) { }
        catch (JsonException) { }
    }

    public Texture2D? GetAsset(string key) => suppliedTextures.GetValueOrDefault(key);

    public bool Asset(string key, Rectangle bounds, Color? tint = null)
    {
        if (!suppliedTextures.TryGetValue(key, out var texture)) return false;
        float scale = Math.Min(bounds.Width / (float)texture.Width, bounds.Height / (float)texture.Height);
        Batch.Draw(texture, new Vector2(bounds.Center.X, bounds.Center.Y), null, tint ?? Color.White,
            0, new Vector2(texture.Width, texture.Height) / 2, scale, SpriteEffects.None, 0);
        return true;
    }

    /// <summary>Fill a page without letterboxing or stretching the artwork.</summary>
    public bool AssetCover(string key, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || !suppliedTextures.TryGetValue(key, out var texture)) return false;
        float scale = Math.Max(bounds.Width / (float)texture.Width, bounds.Height / (float)texture.Height);
        int width = Math.Min(texture.Width, Math.Max(1, (int)MathF.Round(bounds.Width / scale)));
        int height = Math.Min(texture.Height, Math.Max(1, (int)MathF.Round(bounds.Height / scale)));
        var source = new Rectangle((texture.Width - width) / 2, (texture.Height - height) / 2, width, height);
        Batch.Draw(texture, bounds, source, Color.White);
        return true;
    }

    public bool SuppliedTower(TowerKind kind, int level, Vector2 at, float cell, float angle, Color? tint = null)
    {
        var key = "tower:" + kind;
        if (!suppliedTextures.TryGetValue(key, out var texture) || !suppliedEntries.TryGetValue(key, out var entry) || entry.Cell <= 0) return false;
        float degrees = ((MathHelper.ToDegrees(angle) + 90) % 360 + 360) % 360;
        bool mirror = degrees > 180;
        if (mirror) degrees = 360 - degrees;
        int column = Math.Clamp((int)MathF.Round(degrees / 10), 0, entry.Columns - 1);
        int row = Math.Clamp(level - 1, 0, entry.Rows - 1);
        var source = new Rectangle(column * entry.Cell, row * entry.Cell, entry.Cell, entry.Cell);
        var anchor = new Vector2(mirror ? entry.Cell - entry.AnchorX : entry.AnchorX, entry.AnchorY);
        Batch.Draw(texture, at, source, tint ?? Color.White, 0, anchor, cell * 1.9f / (entry.DrawSize > 0 ? entry.DrawSize : entry.Cell),
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        return true;
    }

    // Grade only blue metal; preserve gold hardware, green shop faces and alpha edges.
    static Color NavyMetal(Color pixel)
    {
        if (pixel.A == 0 || pixel.B <= pixel.R || pixel.B <= pixel.G) return pixel;
        float amount = Math.Clamp((pixel.B - pixel.R) / 55f, 0, 1) * Math.Clamp((pixel.B - pixel.G) / 25f, 0, 1);
        if (pixel.R + pixel.G + pixel.B > 600) amount *= .45f;
        return new Color((byte)(pixel.R * (1 - .48f * amount)),
            (byte)(pixel.G * (1 - .37f * amount)), (byte)(pixel.B * (1 - .14f * amount)), pixel.A);
    }

    static void SharpenIcon(Color[] pixels, int width, int height)
    {
        var source = (Color[])pixels.Clone();
        for (int y = 1; y < height - 1; y++) for (int x = 1; x < width - 1; x++)
        {
            int i = y * width + x;
            var c = source[i]; var n = source[i - width]; var s = source[i + width];
            var e = source[i + 1]; var w = source[i - 1];
            if (c.A < 240 || n.A < 240 || s.A < 240 || e.A < 240 || w.A < 240) continue;
            byte Channel(int center, int sum) => (byte)Math.Clamp((int)MathF.Round(center * 1.4f - sum * .1f), 0, 255);
            pixels[i] = new Color(Channel(c.R, n.R + s.R + e.R + w.R),
                Channel(c.G, n.G + s.G + e.G + w.G), Channel(c.B, n.B + s.B + e.B + w.B), c.A);
        }
    }
}
