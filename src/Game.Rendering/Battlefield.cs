using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerGame.Core;

namespace TowerGame.Rendering;

/// <summary>Illustrated battlefield: baked meadow, forest, hardware and unit sprites with live effects.</summary>
public sealed partial class Battlefield
{
    readonly Painter p;
    readonly IllustratedSprites art;
    public Battlefield(Painter painter) { p = painter; art = new IllustratedSprites(painter); }
    public static Color Accent(TowerKind kind) => kind switch
    {
        TowerKind.Gatling => new(235, 195, 86), TowerKind.Goo => new(112, 231, 128),
        TowerKind.Missile => new(244, 158, 73), TowerKind.Tesla => new(115, 222, 247),
        TowerKind.Flame => new(255, 120, 53), TowerKind.Mortar => new(224, 188, 110),
        TowerKind.Laser => new(255, 116, 151), _ => new(165, 235, 255)
    };
    public static Color Ground(string theme) => theme switch
    {
        "sand" => new(177, 157, 73), "crystal" => new(91, 133, 111), "stone" => new(105, 142, 65),
        "ice" => new(166, 198, 171), "marsh" => new(93, 139, 60), "volcano" or "volcanic" => new(126, 114, 64),
        "river" => new(119, 167, 59), _ => new(135, 177, 51)
    };

    /// <summary>Fit square cells with a narrow decorative verge; the HUD overlays the landscape.</summary>
    public static (float Cell, Vector2 Origin) Layout(MapDefinition map, Rectangle bounds, float zoom, Vector2 pan)
    {
        float cell = Math.Min(bounds.Width / (map.Width + .6f), bounds.Height / (map.Height + .6f)) * zoom;
        return (cell, new(bounds.Center.X - map.Width * cell / 2 + pan.X, bounds.Center.Y - map.Height * cell / 2 + pan.Y));
    }
    public static Cell? CellAt(MapDefinition map, Rectangle bounds, float zoom, Vector2 pan, Vector2 point)
    {
        if (!bounds.Contains(point)) return null;
        var (cell, origin) = Layout(map, bounds, zoom, pan);
        int x = (int)MathF.Floor((point.X - origin.X) / cell), y = (int)MathF.Floor((point.Y - origin.Y) / cell);
        return map.Contains(x, y) ? new Cell(x, y) : null;
    }

    public void Map(MapDefinition map, Rectangle bounds, GameSession? session = null, bool path = false,
        float zoom = 1, Vector2 pan = default, int selected = -1, Cell? preview = null, TowerKind? kind = null, bool valid = false, double time = 0)
    {
        if (bounds.Width < 2 || bounds.Height < 2) return;
        var (cell, origin) = Layout(map, bounds, zoom, pan);
        Vector2 Point(double x, double y) => origin + new Vector2((float)x + .5f, (float)y + .5f) * cell;
        var ground = Ground(map.Theme);
        p.Box(bounds, new Color(ground.ToVector3() * .67f), 16);
        var grove = art.Backdrop(map.Theme);
        var groveScale = Math.Max(cell * 5.3f / 512, .1f);
        for (float gy = bounds.Y; gy < bounds.Bottom; gy += 512 * groveScale)
            for (float gx = bounds.X; gx < bounds.Right; gx += 512 * groveScale)
                TextureInside(grove, new Vector2(gx, gy), groveScale, bounds);
        var terrain = art.Terrain(map);
        TextureInside(terrain, origin - Vector2.One * cell * 1.5f, cell / 64, bounds);
        DrawApproaches(map, bounds, origin, cell);
        NatureScenery(map, bounds, origin, cell);

        // Rails are physical hardware. Ground route hints are small, warm wayfinding chevrons.
        foreach (var route in map.Routes)
        {
            if (route.Movement == MovementKind.Rail)
            {
                for (int i = 1; i < route.Points.Count; i++)
                {
                    Vector2 a = Point(route.Points[i - 1].X, route.Points[i - 1].Y), b = Point(route.Points[i].X, route.Points[i].Y);
                    var dir = b - a; if (dir == Vector2.Zero) continue; dir.Normalize(); var side = new Vector2(-dir.Y, dir.X);
                    p.Line(a, b, new Color(72, 71, 49), cell * .48f);
                    for (float t = 0; t < Vector2.Distance(a, b); t += cell * .22f)
                    {
                        p.Line(a + dir * t - side * cell * .24f + Vector2.UnitY * 2, a + dir * t + side * cell * .24f + Vector2.UnitY * 2, new Color(64, 56, 37), Math.Max(2, cell * .095f));
                        p.Line(a + dir * t - side * cell * .22f, a + dir * t + side * cell * .22f, new Color(157, 119, 65), Math.Max(2, cell * .065f));
                    }
                    foreach (int sign in new[] { -1, 1 })
                    {
                        p.Line(a + side * cell * .15f * sign, b + side * cell * .15f * sign, new Color(37, 48, 46), Math.Max(2, cell * .075f));
                        p.Line(a + side * cell * .15f * sign - Vector2.UnitY, b + side * cell * .15f * sign - Vector2.UnitY, new Color(191, 206, 194), Math.Max(1, cell * .035f));
                    }
                }
            }
            else if (path && route.Movement == MovementKind.Ground)
            {
                IReadOnlyList<Cell> points = session?.PreviewPath(route.Id) ?? route.Points;
                for (int i = 1; i < points.Count; i++)
                {
                    var a = Point(points[i - 1].X, points[i - 1].Y); var b = Point(points[i].X, points[i].Y);
                    var delta = b - a; float length = delta.Length(); if (length < 1) continue; var dir = delta / length; var side = new Vector2(-dir.Y, dir.X);
                    var count = Math.Max(1, (int)(length / (cell * .4f)));
                    for (int j = 0; j < count; j++)
                    {
                        var at = a + delta * ((j + .4f + (float)(time * .6 % .3)) / count);
                        var color = new Color(255, 226, 142) * .72f;
                        p.Line(at - dir * cell * .055f + side * cell * .055f, at + dir * cell * .035f, new Color(51, 82, 35) * .55f, Math.Max(1.4f, cell * .035f));
                        p.Line(at - dir * cell * .055f - side * cell * .055f, at + dir * cell * .035f, color, Math.Max(1.4f, cell * .025f));
                    }
                }
            }
            if (route.Movement == MovementKind.Ground)
            {
                Gate(ApproachEnd(map, route.Points[0], origin, cell), cell, false, time);
                Gate(ApproachEnd(map, route.Points[^1], origin, cell), cell, true, time);
            }
        }
        if (session != null)
        {
            var selectedUnit = session.Towers.FirstOrDefault(t => t.Id == selected);
            if (selectedUnit != null) Radius(Point(selectedUnit.X, selectedUnit.Y), cell, Balance.Tower(selectedUnit.Kind, selectedUnit.Level), Accent(selectedUnit.Kind));
            foreach (var tower in session.Towers)
            {
                float angle = MathF.Atan2((float)tower.AimY - tower.Y, (float)tower.AimX - tower.X);
                var shotEffect = session.Effects.LastOrDefault(e => e.Kind == tower.Kind
                    && e.X == tower.X && e.Y == tower.Y && (e.EndX != e.X || e.EndY != e.Y));
                var recoil = shotEffect == null ? 0 : Math.Clamp((float)(shotEffect.Remaining / .18), 0, 1);
                var barrel = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Tower(Point(tower.X, tower.Y) - barrel * cell * .055f * recoil, cell, tower.Kind, tower.Level, angle, tower.Id == selected);
            }
            foreach (var enemy in session.Enemies.Where(e => !e.Dead).OrderBy(e => e.Y).ThenBy(e => e.Id)) DrawEnemy(Point(enemy.X, enemy.Y), cell, enemy, time);
            // Draw weapon flashes and impacts above units, where they remain readable.
            foreach (var effect in session.Effects) Effect(effect, cell, Point, time);
            foreach (var shot in session.Projectiles)
            {
                var at = Point(shot.X, shot.Y); var target = Point(shot.TargetX, shot.TargetY);
                if (shot.Kind == TowerKind.Mortar)
                {
                    p.Ring(target, cell * 1.5f, Painter.Danger * .3f, 1.5f, 32);
                    p.Circle(at + Vector2.UnitY * 5, cell * .13f, Color.Black * .25f);
                    p.Circle(at, cell * .12f, new Color(49, 57, 50)); p.Circle(at - Vector2.One * cell * .02f, cell * .07f, new Color(207, 177, 94));
                }
                else
                {
                    var dir = target - at; if (dir == Vector2.Zero) dir = Vector2.UnitX; else dir.Normalize();
                    for (int i = 0; i < 4; i++) p.Circle(at - dir * cell * (.1f + i * .09f), cell * (.045f + i * .018f), new Color(236, 220, 181) * (.35f - i * .065f));
                    p.Line(at - dir * cell * .13f, at + dir * cell * .12f, new Color(42, 54, 57), cell * .14f);
                    p.Line(at - dir * cell * .13f - Vector2.UnitY, at + dir * cell * .08f - Vector2.UnitY, new Color(245, 234, 190), cell * .085f);
                    p.Circle(at + dir * cell * .12f, cell * .034f, new Color(224, 101, 49));
                    p.Circle(at - dir * cell * .17f, cell * .1f, new Color(255, 184, 53));
                }
            }
        }
        if (preview is { } previewCell && kind is { } buildKind)
        {
            var at = Point(previewCell.X, previewCell.Y); var color = valid ? new Color(200, 249, 112) : Painter.Danger;
            p.Frame(new Rectangle((int)(at.X - cell * .47f), (int)(at.Y - cell * .47f), (int)(cell * .94f), (int)(cell * .94f)), color * .15f, color * .8f, cell * .12f, 2);
            Radius(at, cell, Balance.Tower(buildKind), color); Tower(at, cell, buildKind, 1, -.6f, true);
            if (!valid) p.Cross(at, cell * .27f, Painter.Danger);
        }
    }
    void TextureInside(Texture2D texture, Vector2 origin, float scale, Rectangle bounds)
    {
        // Thumbnails and setup previews do not use a scissor pass. Crop their cached scenery
        // here so the forest never paints over a neighbouring card or its caption.
        var destination = new Rectangle((int)origin.X, (int)origin.Y, Math.Max(1, (int)(texture.Width * scale)), Math.Max(1, (int)(texture.Height * scale)));
        var clipped = Rectangle.Intersect(destination, bounds);
        if (clipped.Width <= 0 || clipped.Height <= 0) return;
        var source = new Rectangle(Math.Clamp((int)((clipped.X - destination.X) / scale), 0, texture.Width - 1), Math.Clamp((int)((clipped.Y - destination.Y) / scale), 0, texture.Height - 1), Math.Max(1, (int)Math.Ceiling(clipped.Width / scale)), Math.Max(1, (int)Math.Ceiling(clipped.Height / scale)));
        source.Width = Math.Min(source.Width, texture.Width - source.X); source.Height = Math.Min(source.Height, texture.Height - source.Y);
        p.Batch.Draw(texture, clipped, source, Color.White);
    }

    void Gate(Vector2 at, float cell, bool exit, double time)
    {
        var pole = at + new Vector2(-cell * .31f, -cell * .05f);
        p.Circle(pole + Vector2.UnitY * cell * .23f, cell * .16f, new Color(57, 70, 35) * .3f);
        p.Line(pole + Vector2.UnitY * cell * .26f, pole - Vector2.UnitY * cell * .39f, new Color(59, 65, 39), cell * .055f);
        p.Line(pole + Vector2.UnitY * cell * .23f - Vector2.UnitX, pole - Vector2.UnitY * cell * .39f - Vector2.UnitX, new Color(226, 208, 129), cell * .025f);
        p.Circle(pole - Vector2.UnitY * cell * .4f, cell * .045f, new Color(250, 218, 120));
        var color = exit ? new Color(69, 144, 169) : new Color(199, 81, 47);
        var flutter = (float)Math.Sin(time * 3 + at.Y) * cell * .025f;
        for (int i = 0; i < 12; i++)
        {
            var x = i * cell * .027f; var shift = MathF.Sin(i * .25f + (float)time * 3) * flutter;
            p.Line(pole + new Vector2(x + 2, -cell * .35f + shift), pole + new Vector2(x + 2, -cell * .11f + shift - (i > 8 ? (i - 8) * cell * .012f : 0)), color * (1 + i * .018f), cell * .029f);
        }
        p.Circle(pole + new Vector2(cell * .14f, -cell * .23f), cell * .035f, new Color(249, 225, 155));
    }
    void Radius(Vector2 at, float cell, TowerStats stats, Color color)
    {
        p.Circle(at, cell * (float)stats.Range, color * .045f);
        p.Ring(at, cell * (float)stats.Range, Color.Black * .15f, 3, 72);
        p.Ring(at, cell * (float)stats.Range, color * .65f, 1.5f, 72);
        if (stats.MinRange > 0) p.Ring(at, cell * (float)stats.MinRange, Painter.Danger * .55f, 1.5f, 48);
    }
    void Effect(CombatEffect effect, float cell, Func<double, double, Vector2> point, double time)
    {
        Vector2 a = point(effect.X, effect.Y), b = point(effect.EndX, effect.EndY);
        var fade = (float)Math.Clamp(effect.Remaining / .2, .15, 1); var accent = Accent(effect.Kind);
        if (a == b)
        {
            float radius = effect.Kind == TowerKind.Mortar ? 1.5f : 1;
            p.Circle(a + Vector2.UnitY * cell * .07f, cell * radius * (1 - fade * .15f), new Color(63, 71, 48) * .12f * fade);
            p.Ring(a, cell * radius * (1.13f - fade * .5f), new Color(255, 200, 83) * .7f * fade, 3, 40);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * MathF.Tau / 7 + .35f; var v = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                p.Circle(a + v * cell * radius * (.8f - fade * .4f), cell * .12f * fade, new Color(244, 143, 45) * fade * .6f);
                p.Line(a + v * cell * .15f, a + v * cell * radius * (.8f - fade * .25f), new Color(255, 238, 172) * fade, 1.3f);
            }
            return;
        }
        var dir = b - a; if (dir != Vector2.Zero) dir.Normalize();
        var muzzle = a + dir * cell * .49f;
        if (effect.Kind is TowerKind.Gatling or TowerKind.Missile or TowerKind.Mortar)
        {
            var sideFlash = new Vector2(-dir.Y, dir.X);
            p.Circle(muzzle, cell * .18f, new Color(255, 141, 33) * .5f * fade);
            p.Line(muzzle - dir * cell * .07f, muzzle + dir * cell * .24f, new Color(255, 198, 65) * fade, cell * .1f);
            p.Line(muzzle - sideFlash * cell * .11f, muzzle + sideFlash * cell * .11f, new Color(255, 238, 163) * fade, cell * .045f);
            p.Circle(muzzle, cell * .065f, Color.White * fade);
        }
        switch (effect.Kind)
        {
            case TowerKind.Gatling:
                p.Line(muzzle, b, new Color(255, 185, 51) * .28f * fade, cell * .055f);
                var tracer = Vector2.Lerp(muzzle, b, Math.Clamp(1 - (float)effect.Remaining / .18f, 0, 1));
                p.Line(tracer - dir * cell * .18f, tracer + dir * cell * .12f, new Color(255, 246, 183) * fade, Math.Max(2, cell * .04f));
                p.Circle(b, cell * .13f, new Color(255, 171, 48) * .65f * fade);
                p.Line(b - dir * cell * .1f, b + dir * cell * .1f, Color.White * fade, 2);
                break;
            case TowerKind.Tesla:
                var side = new Vector2(-dir.Y, dir.X); var mid = (a + b) / 2 + side * cell * .16f;
                p.Polyline(new Color(72, 174, 246) * .25f * fade, cell * .12f, a, Vector2.Lerp(a, mid, .7f) - side * cell * .17f, mid, Vector2.Lerp(mid, b, .5f) + side * cell * .08f, b);
                p.Polyline(new Color(201, 250, 255) * fade, Math.Max(1.4f, cell * .035f), a, Vector2.Lerp(a, mid, .7f) - side * cell * .17f, mid, Vector2.Lerp(mid, b, .5f) + side * cell * .08f, b);
                p.Circle(b, cell * .12f, new Color(170, 239, 255) * .5f * fade); break;
            case TowerKind.Flame:
                for (int i = 0; i < 10; i++)
                {
                    float t = i / 9f; var at = a + dir * cell * (.35f + t * 1.8f) + new Vector2(-dir.Y, dir.X) * MathF.Sin(i * 2.4f + (float)time * 9) * cell * t * .18f;
                    p.Circle(at, cell * (.07f + t * .14f), new Color(247, 106, 31) * .42f * fade);
                    p.Circle(at - Vector2.UnitY * cell * .025f, cell * (.035f + t * .07f), new Color(255, 222, 106) * .65f * fade);
                }
                break;
            case TowerKind.Laser:
                var end = a + dir * (float)Balance.Tower(TowerKind.Laser).Range * cell;
                p.Line(a, end, accent * .2f * fade, cell * .14f); p.Line(a, end, accent * .8f * fade, cell * .04f); p.Line(a, end, new Color(255, 233, 220) * fade, 1.3f); break;
            case TowerKind.Missile or TowerKind.Mortar: break;
            default:
                p.Line(a + dir * cell * .3f, b, accent * .8f * fade, effect.Kind == TowerKind.Gatling ? 1.3f : 3);
                p.Circle(b, cell * .07f, new Color(255, 240, 158) * fade); break;
        }
    }
    public void Tower(Vector2 at, float cell, TowerKind kind, int level, float angle = -.65f, bool selected = false)
    {
        level = Math.Clamp(level, 1, 3);
        if (selected) { p.Circle(at + Vector2.UnitY * cell * .15f, cell * .48f, Accent(kind) * .12f); p.Ring(at + Vector2.UnitY * cell * .15f, cell * .46f, Accent(kind) * .9f, 2, 36); }
        if (p.SuppliedTower(kind, level, at, cell * 1.22f, angle)) return;
        var scale = cell * 1.25f / 256;
        p.Batch.Draw(art.Tower(kind, level, false), at, null, Color.White, 0, new Vector2(128), scale, SpriteEffects.None, 0);
        p.Batch.Draw(art.Tower(kind, level, true), at - Vector2.UnitY * cell * .065f, null, Color.White, angle, new Vector2(128), scale, SpriteEffects.None, 0);
    }
    public void EnemyIcon(Vector2 at, float cell, EnemyKind kind, double time = 0)
    {
        bool human = kind is EnemyKind.Infantry or EnemyKind.Runner or EnemyKind.Heavy;
        float iconCell = cell * (Balance.Enemy(kind).Boss ? .49f : human ? .85f : .64f);
        if (p.Soldier(kind, at + Vector2.UnitY * cell * (human ? .32f : .03f), iconCell, .4f, time * 2.8)) return;
        var scale = cell * (Balance.Enemy(kind).Boss ? 1.05f : 1.2f) / 256;
        p.Batch.Draw(art.Enemy(kind), at, null, Color.White, -.65f, new Vector2(128), scale, SpriteEffects.None, 0);
    }
    void DrawEnemy(Vector2 at, float cell, Enemy enemy, double time)
    {
        var stats = Balance.Enemy(enemy.Kind); var dir = enemy.Entering ? new Vector2((float)(enemy.From.X - enemy.X), (float)(enemy.From.Y - enemy.Y)) : new Vector2(enemy.To.X - enemy.From.X, enemy.To.Y - enemy.From.Y);
        if (dir == Vector2.Zero) dir = Vector2.UnitX; else dir.Normalize(); var angle = MathF.Atan2(dir.Y, dir.X);
        float size = stats.Boss ? 1.13f : stats.Movement == MovementKind.Air ? .9f : enemy.Kind is EnemyKind.Infantry or EnemyKind.Runner ? .7f : enemy.Kind == EnemyKind.Heavy ? .8f : .9f;
        if (stats.Movement == MovementKind.Ground && !stats.Boss)
            at += new Vector2(((enemy.Id * 37) % 7 - 3) * .023f, ((enemy.Id * 53) % 7 - 3) * .023f) * cell;
        bool human = enemy.Kind is EnemyKind.Infantry or EnemyKind.Runner or EnemyKind.Heavy;
        float humanHealthY = at.Y - cell * .5f;
        size *= 1.5f;
        bool suppliedUnit = DrawRunningSoldier(enemy, at, cell, angle, time, out humanHealthY);
        if (!suppliedUnit)
            p.Batch.Draw(art.Enemy(enemy.Kind), at, null, Color.White, angle, new Vector2(128), cell * size / 256, SpriteEffects.None, 0);
        if (!suppliedUnit && enemy.Kind == EnemyKind.Helicopter)
        {
            var hub = at + dir * cell * .025f; var rotor = new Vector2(MathF.Cos((float)time * 25), MathF.Sin((float)time * 25)) * cell * .4f;
            p.Line(hub - rotor, hub + rotor, new Color(37, 55, 57) * .7f, cell * .04f); var cross = new Vector2(-rotor.Y, rotor.X); p.Line(hub - cross, hub + cross, new Color(213, 227, 211) * .55f, cell * .025f); p.Circle(hub, cell * .045f, Painter.Gold);
        }
        if (enemy.Slows.Count > 0) p.Ring(at, cell * (stats.Boss ? .5f : .32f), new Color(150, 242, 250) * .8f, 2, 24);
        if (enemy.Health < enemy.MaxHealth || stats.Boss)
        {
            float width = cell * (stats.Boss ? .9f : human ? .64f : .53f), y = suppliedUnit ? humanHealthY : at.Y - cell * (stats.Boss ? .53f : .38f), h = Math.Max(3, cell * .065f);
            p.Box(new Rectangle((int)(at.X - width / 2 - 1), (int)y - 1, (int)width + 2, (int)h + 2), new Color(27, 42, 36), 3);
            float fraction = (float)Math.Clamp(enemy.Health / enemy.MaxHealth, 0, 1); var color = fraction > .5f ? new Color(169, 230, 91) : fraction > .25f ? Painter.Gold : Painter.Danger;
            p.Rect(at.X - width / 2, y, width * fraction, h, color); p.Rect(at.X - width / 2, y, width * fraction, 1, Color.Lerp(color, Color.White, .4f));
        }
    }
}
