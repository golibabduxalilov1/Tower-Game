using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerGame.Core;

namespace TowerGame.Rendering;

public sealed partial class Battlefield
{
    static Vector2 ApproachEnd(MapDefinition map, Cell gate, Vector2 origin, float cell)
    {
        var outward = gate.X == 0 ? -Vector2.UnitX : gate.X == map.Width - 1 ? Vector2.UnitX :
            gate.Y == 0 ? -Vector2.UnitY : gate.Y == map.Height - 1 ? Vector2.UnitY : Vector2.Zero;
        return origin + (new Vector2(gate.X + .5f, gate.Y + .5f) + outward * .35f) * cell;
    }

    static IEnumerable<Cell> GroundGates(MapDefinition map) => map.Routes
        .Where(r => r.Movement == MovementKind.Ground)
        .SelectMany(r => new[] { r.Points[0], r.Points[^1] }).Distinct();

    void DrawApproaches(MapDefinition map, Rectangle bounds, Vector2 origin, float cell)
    {
        foreach (var gate in GroundGates(map))
        {
            var end = ApproachEnd(map, gate, origin, cell);
            var start = gate.X == 0 ? new Vector2(bounds.Left, end.Y) :
                gate.X == map.Width - 1 ? new Vector2(bounds.Right, end.Y) :
                gate.Y == 0 ? new Vector2(end.X, bounds.Top) :
                gate.Y == map.Height - 1 ? new Vector2(end.X, bounds.Bottom) : end;
            if (start == end) continue;
            var delta = end - start;
            var texture = art.Approach(map.Theme, gate.X + gate.Y);
            p.Batch.Draw(texture, start, null, Color.White, MathF.Atan2(delta.Y, delta.X),
                new Vector2(0, texture.Height / 2f), new Vector2(delta.Length() / texture.Width, cell * 1.2f / texture.Height),
                SpriteEffects.None, 0);
        }
    }

    void NatureScenery(MapDefinition map, Rectangle bounds, Vector2 origin, float cell)
    {
        var rng = new Random(971 + map.Id.Sum(c => c * 17));
        bool desert = map.Theme == "sand";
        bool hd = p.GetAsset("nature:hd-oak-b") != null;
        var left = Rectangle.Intersect(bounds, new Rectangle(bounds.X, bounds.Y,
            Math.Max(0, (int)origin.X - bounds.X), bounds.Height));
        int rightX = (int)MathF.Ceiling(origin.X + map.Width * cell);
        var right = Rectangle.Intersect(bounds, new Rectangle(rightX, bounds.Y,
            Math.Max(0, bounds.Right - rightX), bounds.Height));
        string[] plants = desert
            ? ["nature:palm-tall", "nature:cactus3", "nature:cactus4", "nature:cactus5", "nature:cactus6", "nature:dry-tree", "nature:cactus7"]
            : hd ? ["nature:hd-oak-a", "nature:hd-oak-b", "nature:hd-berry", "nature:hd-fern"]
            : ["nature:tree-round", "nature:berry-bush"];
        var gates = GroundGates(map).ToArray();

        void Side(Rectangle strip)
        {
            if (strip.Width < 12 || strip.Height < 2) return;
            var placed = new List<(Texture2D Texture, Vector2 At, float Scale)>();
            for (int layer = 0; layer < 2; layer++)
            {
                float y = strip.Y - cell + layer * cell * 1.1f;
                while (y < strip.Bottom)
                {
                    var key = plants[rng.Next(plants.Length)];
                    if (p.GetAsset(key) is not { } texture) break;
                    bool tree = key.Contains("oak", StringComparison.Ordinal) || key.Contains("tree-round", StringComparison.Ordinal);
                    float size = desert ? 1.25f : tree ? 2.2f : 1.2f;
                    float width = Math.Min(strip.Width * .84f, cell * size * (.85f + rng.NextSingle() * .35f));
                    float scale = Math.Min(width / texture.Width, 1.1f);
                    width = texture.Width * scale;
                    float x = strip.X + (strip.Width - width) * (layer == 0 ? rng.NextSingle() * .35f : .65f + rng.NextSingle() * .35f);
                    float height = texture.Height * scale;
                    bool crossesGate = gates.Where(c => strip.Right <= origin.X + 1 ? c.X == 0 : c.X == map.Width - 1)
                        .Any(c => y < origin.Y + (c.Y + 1.15f) * cell && y + height > origin.Y + (c.Y - .15f) * cell);
                    if (!crossesGate) placed.Add((texture, new Vector2(x, y), scale));
                    y += Math.Max(cell * .8f, height * (.76f + rng.NextSingle() * .32f));
                }
            }
            foreach (var item in placed.OrderBy(v => v.At.Y + v.Texture.Height * v.Scale))
            {
                float width = item.Texture.Width * item.Scale, height = item.Texture.Height * item.Scale;
                var shadow = new Rectangle((int)(item.At.X + width * .18f), (int)(item.At.Y + height * .87f),
                    Math.Max(2, (int)(width * .65f)), Math.Max(2, (int)(height * .12f)));
                shadow = Rectangle.Intersect(shadow, strip);
                if (shadow.Width > 0 && shadow.Height > 0) p.Box(shadow, new Color(28, 55, 24) * .15f, shadow.Height / 2f);
                TextureInside(item.Texture, item.At, item.Scale, strip);
            }
        }
        Side(left); Side(right);
        if (desert || !hd) return;

        // Sparse high-resolution clumps replace the small repeated archive garden strip.
        for (float x = bounds.X - cell; x < bounds.Right; x += cell * (2.2f + rng.NextSingle() * .9f))
        {
            var key = rng.Next(3) == 0 ? "nature:hd-berry" : "nature:hd-fern";
            if (p.GetAsset(key) is not { } foliage) continue;
            float scale = cell * 1.3f / foliage.Width;
            float width = foliage.Width * scale, height = foliage.Height * scale;
            bool GateAtEdge(bool top) => gates.Where(c => top ? c.Y == 0 : c.Y == map.Height - 1)
                .Any(c => x < origin.X + (c.X + 1.2f) * cell && x + width > origin.X + (c.X - .2f) * cell);
            if (!GateAtEdge(true)) TextureInside(foliage, new Vector2(x, bounds.Y - height * .68f), scale, bounds);
            if (!GateAtEdge(false)) TextureInside(foliage, new Vector2(x + cell * .4f, bounds.Bottom - height * .35f), scale, bounds);
        }
    }
}
