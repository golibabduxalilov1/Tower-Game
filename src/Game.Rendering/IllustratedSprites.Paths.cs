using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerGame.Rendering;

internal sealed partial class IllustratedSprites
{
    readonly Dictionary<(string Theme, int Variant), Texture2D> approachTextures = [];

    public Texture2D Approach(string theme, int variant)
    {
        var key = (theme, variant % 3);
        if (approachTextures.TryGetValue(key, out var cached)) return cached;
        const int width = 768, height = 192;
        var a = new IllustrationCanvas(width, height);
        var grass = Meadow(theme);
        var soil = theme switch
        {
            "sand" => new Color(169, 151, 98), "ice" => new Color(150, 165, 130),
            "marsh" => new Color(119, 128, 76), "crystal" => new Color(118, 133, 99),
            "volcano" or "volcanic" => new Color(130, 112, 79), _ => new Color(157, 144, 86)
        };
        int seed = 137 + key.Item2 * 317;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            float u = (x + .5f) / width, v = (y + .5f) / height;
            float center = .5f + MathF.Sin(u * 11 + seed) * .019f + MathF.Sin(u * 29 + seed * .3f) * .009f;
            float half = .285f + MathF.Sin(u * 17 + seed) * .018f;
            float rough = RoadNoise(x / 15f, y / 10f, seed) * .019f;
            float distance = MathF.Abs(v - center) + rough;
            float coverage = 1 - RoadSmooth(half - .055f, half + .15f, distance);
            float fade = 1 - RoadSmooth(.73f, 1, u);
            float broad = RoadNoise(x / 70f, y / 36f, seed + 5);
            float fine = RoadNoise(x / 4.5f, y / 3f, seed + 11);
            float grain = RoadNoise(x / 1.4f, y / 1.2f, seed + 19);
            // Subtle uneven compaction, without raised borders, paving or yellow bands.
            float tracks = MathF.Exp(-MathF.Pow((v - center - .085f) / .045f, 2)) +
                MathF.Exp(-MathF.Pow((v - center + .085f) / .045f, 2));
            float light = .99f + broad * .075f + fine * .055f + grain * .025f - tracks * .024f;
            var pixel = Color.Lerp(soil, grass, Math.Clamp((distance - half + .1f) * 1.25f, 0, .34f));
            pixel = new Color((byte)Math.Clamp(pixel.R * light, 0, 255), (byte)Math.Clamp(pixel.G * light, 0, 255),
                (byte)Math.Clamp(pixel.B * light, 0, 255), (byte)Math.Clamp(coverage * fade * .88f * 255, 0, 255));
            a.Pixels[y * width + x] = pixel;
        }
        var rng = new Random(seed);
        for (int i = 0; i < 180; i++)
        {
            float x = rng.Next(10, width - 8), u = x / width;
            float center = height * (.5f + MathF.Sin(u * 11 + seed) * .019f);
            float y = center + (i % 2 == 0 ? -1 : 1) * height * (.27f + rng.NextSingle() * .11f);
            var blade = Color.Lerp(grass, new Color(54, 88, 29), .16f);
            blade.A = (byte)(150 * (1 - RoadSmooth(.73f, 1, u)));
            a.Line(x, y + 2, x - 2 - rng.Next(4), y - 4 - rng.Next(6), 1.2f, blade);
        }
        return approachTextures[key] = painter.CreateTexture(width, height, a.Finish());
    }

    static float RoadSmooth(float low, float high, float value)
    {
        float t = Math.Clamp((value - low) / (high - low), 0, 1);
        return t * t * (3 - 2 * t);
    }
    static float RoadNoise(float x, float y, int seed)
    {
        int ix = (int)MathF.Floor(x), iy = (int)MathF.Floor(y);
        float fx = x - ix, fy = y - iy; fx *= fx * (3 - 2 * fx); fy *= fy * (3 - 2 * fy);
        float Sample(int xx, int yy)
        {
            uint h = unchecked((uint)(xx * 374761393 + yy * 668265263 + seed * 69069));
            h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 65535) / 32767.5f - 1;
        }
        return MathHelper.Lerp(MathHelper.Lerp(Sample(ix, iy), Sample(ix + 1, iy), fx),
            MathHelper.Lerp(Sample(ix, iy + 1), Sample(ix + 1, iy + 1), fx), fy);
    }
}
