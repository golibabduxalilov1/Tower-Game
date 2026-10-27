using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerGame.Core;
using static TowerGame.Rendering.IllustrationCanvas;

namespace TowerGame.Rendering;

/// <summary>Original illustrated military hardware, assembled entirely from code and baked once.</summary>
internal sealed partial class IllustratedSprites(Painter painter)
{
    readonly Dictionary<(TowerKind, int, bool), Texture2D> towers = [];
    readonly Dictionary<EnemyKind, Texture2D> enemies = [];
    readonly Dictionary<string, Texture2D> terrain = [];
    readonly Dictionary<string, Texture2D> trees = [];
    readonly Dictionary<string, Texture2D> backdrops = [];
    static readonly Color Outline = C(32, 43, 49), Steel = C(136, 167, 187), SteelDark = C(62, 84, 104), SteelLight = C(211, 229, 225), Brass = C(236, 181, 70), BrassDark = C(143, 89, 39);
    public Texture2D Tower(TowerKind kind, int level, bool turret)
    {
        var key = (kind, level, turret);
        if (towers.TryGetValue(key, out var texture)) return texture;
        var canvas = turret ? MakeTurret(kind, level) : MakeBase(kind, level);
        return towers[key] = painter.CreateTexture(canvas.Width, canvas.Height, canvas.Finish());
    }
    public Texture2D Enemy(EnemyKind kind)
    {
        if (enemies.TryGetValue(kind, out var texture)) return texture;
        var canvas = MakeEnemy(kind);
        return enemies[kind] = painter.CreateTexture(canvas.Width, canvas.Height, canvas.Finish());
    }
    public Texture2D Terrain(MapDefinition map)
    {
        if (terrain.TryGetValue(map.Id, out var texture)) return texture;
        var canvas = MakeTerrain(map);
        return terrain[map.Id] = painter.CreateTexture(canvas.Width, canvas.Height, canvas.Finish());
    }
    public Texture2D Tree(string theme)
    {
        if (trees.TryGetValue(theme, out var texture)) return texture;
        var canvas = MakeTree(theme, 0);
        return trees[theme] = painter.CreateTexture(canvas.Width, canvas.Height, canvas.Finish());
    }
    public Texture2D Backdrop(string theme)
    {
        if (backdrops.TryGetValue(theme, out var texture)) return texture;
        var a = new IllustrationCanvas(512, 512); var color = Meadow(theme);
        for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
            a.Pixels[y * 512 + x] = Shade(color, .8f + (Hash(x, y, 41) % 60) / 1000f);
        var rng = new Random(41);
        // The ground stays quiet; native supplied foliage is placed individually above it.
        for (int i = 0; i < 480; i++)
        {
            var x = rng.Next(512); var y = rng.Next(512);
            a.Line(x, y, x - 2, y - 4, 1, Shade(color, .85f));
            a.Line(x + 1, y, x + 3, y - 6, 1, Shade(color, 1.1f));
        }
        return backdrops[theme] = painter.CreateTexture(512, 512, a.Finish());
    }
    static void Bolt(IllustrationCanvas a, float x, float y, float r = 4)
    {
        a.Ellipse(x, y + 1, r + 1, r + 1, Outline); a.Ellipse(x, y, r, r, SteelLight, true);
        a.Line(x - r * .5f, y + r * .35f, x + r * .5f, y - r * .35f, 1.2f, SteelDark);
    }
    static void Metal(IllustrationCanvas a, float x, float y, float w, float h, float r, Color color)
    {
        a.Round(x - 2, y - 1, w + 4, h + 5, r + 2, Outline);
        a.Round(x, y, w, h, r, Shade(color, 1.18f), Shade(color, .65f));
        a.Line(x + r, y + 2, x + w - r, y + 2, 2, Color.Lerp(color, Color.White, .55f));
    }
    static void Band(IllustrationCanvas a, float x, float y, float w, float h)
    {
        a.Round(x, y, w, h, 3, BrassDark); a.Round(x + 1, y + 1, w - 2, h - 3, 2, C(255, 224, 127), Brass);
    }
    static void Barrel(IllustrationCanvas a, float x, float y, float length, float radius, bool gold = false)
    {
        Metal(a, x, y - radius, length, radius * 2, radius * .3f, gold ? Brass : Steel);
        a.Line(x + 2, y - radius * .6f, x + length - 3, y - radius * .6f, 2, gold ? C(255, 238, 160) : SteelLight);
        Band(a, x + length * .65f, y - radius - 2, 8, radius * 2 + 4);
        a.Ellipse(x + length, y + 2, radius * .5f + 2, radius + 2, Outline);
        a.Ellipse(x + length, y, radius * .5f, radius, gold ? Brass : SteelLight);
        a.Ellipse(x + length + 1, y + .5f, radius * .33f, radius * .67f, C(24, 36, 45));
    }
    static IllustrationCanvas MakeBase(TowerKind kind, int level)
    {
        var a = new IllustrationCanvas(256, 256);
        a.Ellipse(133, 164, 100, 64, C(20, 38, 24, 65)); a.Ellipse(131, 157, 89, 54, C(20, 32, 26, 95));
        // Four articulated stabilisers, rubber feet, hydraulic pistons and exposed bolts.
        for (int i = 0; i < 4; i++)
        {
            float angle = MathF.PI / 4 + i * MathF.PI / 2; float x = 128 + MathF.Cos(angle) * 73, y = 137 + MathF.Sin(angle) * 59;
            a.Line(128 + MathF.Cos(angle) * 48, 137 + MathF.Sin(angle) * 40, x, y, 27, Outline);
            a.Line(128 + MathF.Cos(angle) * 48, 134 + MathF.Sin(angle) * 40, x, y - 4, 17, SteelDark);
            a.Line(128 + MathF.Cos(angle) * 52, 131 + MathF.Sin(angle) * 43, x, y - 6, 6, SteelLight);
            a.Round(x - 14, y - 3, 29, 20, 7, Outline); a.Round(x - 11, y - 5, 23, 18, 6, Steel, SteelDark);
            Bolt(a, x, y + 2, 5);
        }
        a.Ellipse(128, 146, 76, 55, Outline);
        a.Ellipse(128, 140, 74, 52, SteelDark, true);
        a.Ellipse(128, 129, 73, 49, C(196, 166, 98), true);
        a.Ellipse(128, 126, 65, 43, Outline);
        a.Ellipse(128, 122, 61, 40, Steel, true);
        a.Ring(128, 122, 54, 35, 3, SteelLight);
        a.Ring(128, 123, 43, 29, 7, BrassDark); a.Ring(128, 120, 42, 28, 4, Brass);
        for (int i = 0; i < 10; i++)
        {
            float angle = i * MathF.Tau / 10;
            Bolt(a, 128 + MathF.Cos(angle) * 60, 124 + MathF.Sin(angle) * 40, 3.7f);
        }
        if (level >= 2)
        {
            for (int i = -1; i <= 1; i += 2) { Band(a, 128 + i * 45 - 5, 156, 11, 24); a.Round(128 + i * 45 - 3, 159, 7, 9, 2, C(255, 240, 150)); }
        }
        for (int i = 0; i < level; i++) { float x = 128 + (i - (level - 1) / 2f) * 16; a.Ellipse(x, 174, 6, 5, Outline); a.Ellipse(x, 172, 4, 3.5f, Brass, true); }
        return a;
    }
    static IllustrationCanvas MakeTurret(TowerKind kind, int level)
    {
        var a = new IllustrationCanvas(256, 256); var accent = Battlefield.Accent(kind);
        a.Ellipse(130, 138, 55, 44, C(9, 18, 26, 90));
        if (kind is TowerKind.Tesla or TowerKind.Ice)
        {
            a.Ellipse(128, 130, 50, 43, Outline); a.Ellipse(128, 123, 47, 38, Brass, true);
            a.Ellipse(128, 122, 39, 30, SteelDark, true);
        }
        else
        {
            a.Ellipse(125, 127, 51, 44, Outline);
            a.Ellipse(122, 119, 48, 41, kind == TowerKind.Goo ? C(57, 151, 113) : kind == TowerKind.Flame ? C(174, 88, 53) : Steel, true);
            a.Ellipse(112, 107, 28, 20, Color.Lerp(SteelLight, accent, .22f) * .55f);
            a.Ring(123, 119, 45, 37, 2, C(203, 222, 217, 120));
        }
        switch (kind)
        {
            case TowerKind.Gatling:
                Metal(a, 134, 101, 38, 52, 13, SteelDark);
                for (int i = 0; i < level + 2; i++) Barrel(a, 150, 107 + i * (42f / (level + 1)), 76 - (i % 2) * 6, level == 3 ? 6.2f : 7);
                Band(a, 169, 99, 10, 59); Band(a, 199, 99, 9, 59);
                a.Ellipse(123, 113, 20, 15, C(118, 141, 159), true); Bolt(a, 119, 111, 6);
                for (int i = 0; i < 5; i++) Band(a, 76 + i * 8, 149 + MathF.Sin(i * .6f) * 4, 7, 12);
                break;
            case TowerKind.Goo:
                Metal(a, 79, 91, 37, 77, 16, C(39, 73, 66));
                a.Round(84, 98, 27, 59, 12, C(167, 238, 151), C(36, 154, 90));
                a.Round(87, 100, 5, 47, 3, C(230, 255, 206, 190));
                for (int i = 0; i < 5; i++) a.Ellipse(98 + MathF.Sin(i * 2) * 6, 110 + i * 9, 3 + i % 2, 3 + i % 2, C(219, 255, 166, 160));
                Band(a, 78, 96, 38, 8); Band(a, 78, 148, 38, 9);
                a.Line(112, 157, 158, 153, 10, Outline); a.Line(112, 154, 158, 149, 5, C(55, 87, 65));
                Barrel(a, 139, 123, 75 + level * 3, 14, true);
                a.Ellipse(219 + level * 3, 124, 5, 10, C(117, 245, 103), true);
                if (level > 1) { a.Ellipse(129, 93, 14, 10, Outline); a.Ellipse(129, 90, 12, 9, C(117, 245, 103), true); }
                break;
            case TowerKind.Missile:
                var count = level + 1; var spacing = count == 4 ? 22 : 31;
                Metal(a, 118, 82, 68, 93, 12, SteelDark);
                for (int i = 0; i < count; i++)
                {
                    float y = 127 + (i - (count - 1) / 2f) * spacing;
                    Metal(a, 131, y - 10, 73, 21, 7, C(104, 129, 146));
                    a.Round(140, y - 6, 62, 12, 5, C(239, 231, 181), C(160, 150, 109));
                    a.Poly(C(169, 52, 37), new(192, y - 6), new(219, y), new(192, y + 6));
                    a.Line(141, y - 6, 192, y - 6, 2, C(255, 252, 223));
                    Band(a, 151, y - 11, 6, 23);
                }
                Bolt(a, 118, 102, 6); Bolt(a, 118, 153, 6);
                break;
            case TowerKind.Tesla:
                for (int i = 0; i < 3 + (level == 3 ? 1 : 0); i++)
                {
                    float angle = i * MathF.Tau / (3 + (level == 3 ? 1 : 0)); float x = 128 + MathF.Cos(angle) * 57, y = 124 + MathF.Sin(angle) * 46;
                    a.Line(128, 124, x, y, 15, Outline); a.Line(128, 119, x, y - 6, 9, SteelLight);
                    a.Ellipse(x, y - 5, 13, 12, BrassDark); a.Ellipse(x, y - 9, 10, 10, Brass, true); a.Ellipse(x - 2, y - 11, 4, 3, C(255, 244, 163));
                }
                for (int i = 0; i < 5; i++) { a.Ellipse(128, 135 - i * 9, 29 - i * 2, 13, Outline); a.Ellipse(128, 131 - i * 9, 28 - i * 2, 11, Brass, true); }
                a.Ellipse(128, 84, 21 + level * 2, 24 + level * 2, C(35, 76, 101));
                a.Ellipse(125, 78, 18 + level * 2, 22 + level * 2, C(116, 238, 255), true);
                a.Ellipse(119, 71, 8, 10, C(234, 255, 255, 190));
                break;
            case TowerKind.Flame:
                foreach (int sign in new[] { -1, 1 })
                {
                    Metal(a, 89, 121 + sign * 36 - 12, 44, 25, 9, C(165, 65, 37));
                    Band(a, 102, 121 + sign * 36 - 13, 8, 27); a.Round(82, 121 + sign * 36 - 5, 8, 10, 3, BrassDark);
                }
                Barrel(a, 134, 124, 77, 17, true);
                Metal(a, 174, 102, 33, 44, 9, SteelDark);
                for (int i = 0; i < 4; i++) a.Rect(179 + i * 6, 107, 2, 34, C(219, 145, 71));
                a.Ellipse(213, 124, 5, 13, C(255, 126, 30)); a.Ellipse(214, 122, 3, 8, C(255, 235, 118));
                a.Line(111, 156, 171, 145, 8, C(47, 52, 51)); a.Line(111, 153, 171, 142, 3, C(225, 147, 64));
                break;
            case TowerKind.Mortar:
                a.Ellipse(129, 128, 57, 46, Outline); a.Ellipse(128, 122, 54, 43, Brass, true);
                a.Ellipse(137, 117, 43, 39, BrassDark); a.Ellipse(143, 109, 43, 39, C(236, 201, 115), true);
                a.Ellipse(150, 104, 35, 31, Outline); a.Ellipse(151, 104, 27, 24, C(12, 27, 36));
                a.Ellipse(154, 108, 18, 16, C(20, 38, 48)); a.Ring(151, 104, 31, 28, 3, C(247, 217, 138));
                for (int i = 0; i < 3 + level; i++) { float angle = i * MathF.Tau / (3 + level); Bolt(a, 128 + MathF.Cos(angle) * 49, 124 + MathF.Sin(angle) * 38, 4); }
                break;
            case TowerKind.Laser:
                Metal(a, 118, 96, 89, 57, 15, SteelDark);
                Metal(a, 128, 100, 70, 43, 10, C(146, 75, 92));
                for (int i = 0; i < 5; i++) { Metal(a, 140 + i * 11, 94, 5, 56, 2, Steel); a.Rect(141 + i * 11, 106, 2, 32, C(255, 129, 156)); }
                a.Ellipse(209, 122, 11, 24, Outline); a.Ellipse(211, 120, 8, 19, Brass, true);
                a.Ellipse(214, 120, 6, 14, C(254, 94, 127), true); a.Ellipse(214, 115, 3, 7, C(255, 229, 215));
                a.Round(88, 100, 29, 45, 10, C(57, 75, 95));
                a.Line(98, 109, 99, 137, 5, C(238, 106, 139));
                break;
            case TowerKind.Ice:
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * MathF.Tau / 5; float x = 128 + MathF.Cos(angle) * 37, y = 112 + MathF.Sin(angle) * 29;
                    a.Poly(Outline, new(x - 13, y + 10), new(x - 6, y - 18), new(x + 4, y - 29 - level * 3), new(x + 15, y - 7), new(x + 10, y + 14));
                    a.Poly(C(88, 180, 226), new(x - 10, y + 8), new(x - 4, y - 17), new(x + 4, y - 25 - level * 3), new(x + 12, y - 6), new(x + 8, y + 11));
                    a.Poly(C(211, 250, 255), new(x - 4, y - 17), new(x + 4, y - 25 - level * 3), new(x + 3, y + 8), new(x - 10, y + 8));
                }
                a.Ellipse(128, 133, 29, 18, BrassDark); a.Ellipse(128, 128, 27, 16, SteelLight, true);
                for (int i = 0; i < 6; i++) { float aa = i * MathF.PI / 3; a.Line(128, 112, 128 + MathF.Cos(aa) * 22, 112 + MathF.Sin(aa) * 22, 4, C(239, 255, 255)); }
                break;
        }
        // Surface fasteners, identification marks and meaningful upgrade attachments.
        if (kind is not (TowerKind.Ice or TowerKind.Tesla)) { Bolt(a, 103, 139, 4); Bolt(a, 105, 93, 4); }
        if (level >= 2) { Metal(a, 78, 124, 12, 33, 3, SteelDark); for (int j = 0; j < 3; j++) a.Rect(81, 129 + j * 8, 6, 3, Brass); }
        if (level == 3) { Metal(a, 98, 75, 28, 10, 3, Brass); Bolt(a, 105, 80, 3); Bolt(a, 119, 80, 3); }
        return a;
    }
    static IllustrationCanvas MakeEnemy(EnemyKind kind)
    {
        var a = new IllustrationCanvas(256, 256); var boss = Balance.Enemy(kind).Boss;
        var body = boss ? C(153, 75, 52) : C(167, 100, 57); var dark = C(50, 43, 38); var skin = C(240, 185, 124);
        a.Ellipse(135, 156, kind is EnemyKind.Infantry or EnemyKind.Runner or EnemyKind.Heavy ? 48 : 86, 43, C(15, 31, 25, 70));
        switch (kind)
        {
            case EnemyKind.Infantry or EnemyKind.Runner or EnemyKind.Heavy:
                float heavy = kind == EnemyKind.Heavy ? 1.15f : 1; var coat = kind == EnemyKind.Runner ? C(200, 118, 47) : kind == EnemyKind.Heavy ? C(123, 95, 67) : C(165, 83, 54);
                Metal(a, 75, 91, 55, 23, 10, C(52, 48, 43)); Metal(a, 78, 145, 55, 23, 10, C(52, 48, 43));
                a.Round(84, 94, 33, 16, 5, C(95, 83, 62)); a.Round(86, 147, 34, 16, 5, C(95, 83, 62));
                Metal(a, 94, 88, 67 * heavy, 81, 17, coat);
                a.Round(90, 104, 21, 51, 8, C(90, 79, 47), C(57, 57, 34)); a.Line(95, 109, 95, 148, 2, C(170, 149, 85));
                a.Line(133, 91, 133, 161, 6, C(69, 66, 43)); a.Line(113, 104, 150, 150, 6, C(207, 162, 83));
                a.Ellipse(146, 102, 17, 14, coat, true); a.Ellipse(148, 155, 17, 14, coat, true);
                a.Ellipse(165, 103, 12, 11, skin, true); a.Ellipse(167, 153, 12, 11, skin, true);
                Metal(a, 154, 144, 64, 9, 3, C(66, 72, 69)); a.Rect(187, 139, 9, 10, C(83, 67, 46)); a.Line(174, 152, 180, 162, 6, dark);
                a.Ellipse(160, 128, 29, 27, dark); a.Ellipse(165, 132, 24, 21, skin, true);
                a.Ellipse(150, 122, 31, 30, C(48, 69, 53)); a.Ellipse(146, 118, 29, 27, C(104, 124, 79), true);
                a.Line(139, 95, 143, 141, 5, C(151, 158, 104, 160)); a.Line(164, 109, 174, 119, 5, C(54, 65, 45));
                a.Ellipse(172, 133, 2.3f, 2, C(44, 34, 31));
                if (kind == EnemyKind.Heavy) { Metal(a, 192, 98, 22, 66, 6, Steel); Band(a, 194, 104, 7, 52); }
                if (kind == EnemyKind.Runner) { a.Line(88, 81, 112, 86, 5, C(240, 177, 62)); a.Line(73, 177, 104, 174, 3, C(240, 177, 62, 140)); }
                break;
            case EnemyKind.Tank or EnemyKind.HeavyTank:
                for (int i = -1; i <= 1; i += 2)
                {
                    Metal(a, 53, 128 + i * 48 - 17, 145, 35, 13, C(39, 45, 44));
                    for (int x = 62; x < 191; x += 11) { a.Line(x, 128 + i * 48 - 11, x + 3, 128 + i * 48 + 12, 5, C(80, 83, 75)); a.Line(x, 128 + i * 48 - 11, x + 3, 128 + i * 48 - 7, 2, C(155, 147, 112)); }
                }
                a.Poly(Outline, new(61, 91), new(174, 84), new(201, 108), new(201, 150), new(174, 172), new(61, 162));
                a.Poly(body, new(65, 94), new(172, 89), new(195, 110), new(195, 148), new(172, 166), new(65, 158));
                a.Poly(Shade(body, 1.4f), new(66, 94), new(172, 89), new(187, 106), new(82, 111));
                a.Line(87, 115, 87, 150, 3, Shade(body, .55f));
                for (int i = 0; i < 4; i++) a.Line(66 + i * 5, 117, 66 + i * 5, 149, 2, Outline);
                a.Ellipse(132, 128, 43, 35, Outline); a.Ellipse(132, 120, 40, 33, body, true);
                if (boss) { Barrel(a, 146, 110, 90, 8, true); Barrel(a, 146, 135, 90, 8, true); }
                else Barrel(a, 146, 122, 87, 9);
                a.Ellipse(118, 116, 15, 13, dark); a.Ellipse(118, 112, 13, 11, body, true); Bolt(a, 116, 110, 3);
                for (int i = -1; i <= 1; i += 2) { a.Ellipse(185, 128 + i * 28, 5, 5, C(255, 221, 122)); Bolt(a, 80, 128 + i * 28, 3); }
                break;
            case EnemyKind.Motorcycle:
                Metal(a, 44, 111, 49, 34, 15, C(35, 43, 44)); Metal(a, 186, 111, 49, 34, 15, C(35, 43, 44));
                a.Line(67, 119, 210, 119, 8, SteelLight); a.Line(67, 137, 210, 137, 8, SteelDark);
                Metal(a, 81, 111, 102, 35, 10, C(161, 80, 42)); a.Ellipse(167, 128, 28, 24, body, true);
                a.Line(174, 94, 181, 158, 5, SteelLight); a.Ellipse(181, 94, 6, 4, Outline); a.Ellipse(181, 158, 6, 4, Outline);
                Metal(a, 102, 107, 36, 44, 9, C(80, 65, 48)); a.Ellipse(142, 125, 21, 23, C(83, 111, 94), true);
                a.Ellipse(153, 126, 11, 18, C(232, 186, 118), true); a.Round(152, 114, 8, 24, 3, C(92, 174, 192));
                a.Ellipse(209, 128, 7, 9, C(255, 231, 139));
                break;
            case EnemyKind.Helicopter:
                a.Line(46, 128, 149, 128, 17, Outline); a.Line(46, 124, 149, 124, 11, body);
                a.Line(47, 108, 47, 148, 8, SteelLight); a.Line(75, 115, 75, 141, 5, Brass);
                Metal(a, 102, 90, 84, 74, 30, body);
                a.Ellipse(171, 124, 27, 26, Outline); a.Ellipse(172, 121, 22, 23, C(103, 190, 202), true); a.Ellipse(178, 113, 9, 10, C(207, 246, 240, 180));
                a.Line(108, 81, 168, 81, 7, SteelDark); a.Line(108, 175, 168, 175, 7, SteelDark);
                a.Ellipse(135, 116, 15, 12, Outline); a.Ellipse(134, 112, 12, 10, SteelLight, true);
                break;
            case EnemyKind.Plane or EnemyKind.AirFortress:
                var span = boss ? 115 : 90;
                a.Poly(Outline, new(68, 128), new(119, 128 - span), new(146, 128 - span), new(166, 112), new(226, 128), new(166, 145), new(146, 128 + span), new(119, 128 + span));
                a.Poly(body, new(78, 126), new(123, 134 - span), new(140, 134 - span), new(157, 115), new(217, 128), new(157, 140), new(140, 122 + span), new(123, 122 + span));
                a.Line(126, 128 - span + 11, 146, 112, 5, Shade(body, 1.4f));
                a.Line(126, 128 + span - 11, 146, 144, 5, Shade(body, .6f));
                Metal(a, 51, 114, 150, 29, 14, C(182, 141, 90)); a.Ellipse(180, 124, 25, 13, C(90, 182, 209), true);
                a.Poly(SteelDark, new(52, 118), new(38, 82), new(57, 85), new(88, 116)); a.Poly(Steel, new(52, 138), new(38, 174), new(57, 171), new(88, 140));
                for (int i = -1; i <= 1; i += 2) { Metal(a, 135, 128 + i * 52 - 9, 43, 18, 7, SteelDark); Band(a, 148, 128 + i * 52 - 10, 6, 20); }
                if (boss) for (int i = -1; i <= 1; i += 2) { Metal(a, 125, 128 + i * 85 - 8, 44, 17, 7, SteelDark); a.Ellipse(167, 128 + i * 85, 7, 7, Brass, true); }
                break;
            default:
                var colossus = kind == EnemyKind.RailColossus;
                for (int sign = -1; sign <= 1; sign += 2) for (int x = 64; x < 205; x += 27) { a.Ellipse(x, 128 + sign * 42, 12, 11, Outline); a.Ellipse(x, 128 + sign * 42 - 2, 8, 7, Steel, true); Bolt(a, x, 128 + sign * 42 - 3, 3); }
                Metal(a, 46, 89, 167, 78, 10, body); Metal(a, 54, 95, 149, 59, 7, C(131, 98, 68));
                if (colossus)
                {
                    Metal(a, 125, 95, 67, 62, 15, SteelDark); Barrel(a, 153, 120, 84, 13, true);
                    for (int i = 0; i < 3; i++) Band(a, 59 + i * 17, 102, 7, 46);
                }
                else
                {
                    a.Round(69, 106, 104, 39, 6, C(48, 51, 43));
                    for (int i = 0; i < 7; i++) { var x = 79 + (i * 37) % 80; var y = 117 + (i * 17) % 22; a.Poly(C(96 + i * 13, 144 + i * 9, 183 + i * 6), new(x - 11, y), new(x, y - 17), new(x + 12, y - 3), new(x + 3, y + 12)); }
                }
                for (int i = 0; i < 4; i++) Bolt(a, 58 + i * 47, 98, 3);
                break;
        }
        return a;
    }

    static IllustrationCanvas MakeTree(string theme, int variant)
    {
        var a = new IllustrationCanvas(160, 180); var snow = theme == "ice"; var desert = theme == "sand"; var volcanic = theme is "volcano" or "volcanic";
        Color leaves = snow ? C(94, 148, 140) : desert ? C(124, 158, 49) : volcanic ? C(92, 111, 54) : C(75, 146, 33);
        a.Ellipse(85, 148, 64, 27, C(16, 40, 18, 70));
        a.Poly(C(66, 55, 27), new(62, 166), new(69, 113), new(85, 105), new(99, 143), new(112, 165), new(90, 156), new(81, 171));
        a.Poly(C(154, 108, 44), new(68, 161), new(75, 116), new(83, 114), new(89, 146), new(99, 157), new(82, 153), new(78, 164));
        a.Line(75, 138, 80, 157, 3, C(194, 142, 62)); a.Line(86, 127, 93, 151, 2, C(85, 63, 27));
        // Broad, irregular canopy, with scalloped shadows and fine clustered leaves.
        var rng = new Random(variant * 19 + 7);
        (float X, float Y, float R)[] clusters = [(78,70,55),(43,95,34),(112,92,36),(66,110,39),(108,56,35),(51,51,35),(79,37,34)];
        foreach (var c in clusters) a.Ellipse(c.X + 3, c.Y + 13, c.R + 3, c.R * .67f + 3, C(25, 75, 22));
        foreach (var c in clusters)
        {
            a.Ellipse(c.X, c.Y + 5, c.R, c.R * .7f, Shade(leaves, .85f), true);
            a.Ellipse(c.X - 2, c.Y - 3, c.R * .86f, c.R * .62f, Shade(leaves, 1.1f), true);
            for (int i = 0; i < 27; i++)
            {
                float angle = (float)rng.NextDouble() * MathF.Tau, radius = MathF.Sqrt((float)rng.NextDouble()) * c.R * .74f;
                float x = c.X + MathF.Cos(angle) * radius, y = c.Y - 4 + MathF.Sin(angle) * radius * .62f;
                var color = Shade(leaves, .85f + (float)rng.NextDouble() * .7f); a.Ellipse(x + 1, y + 2, 5, 3.5f, Shade(color, .65f)); a.Ellipse(x, y, 4.8f, 3.3f, color, true);
            }
            if (snow) a.Ellipse(c.X - 7, c.Y - c.R * .23f, c.R * .62f, c.R * .31f, C(218, 242, 230), true);
        }
        return a;
    }

    static Color Meadow(string theme) => theme switch
    {
        "sand" => C(177, 157, 73), "crystal" => C(91, 133, 111), "stone" => C(105, 142, 65),
        "ice" => C(166, 198, 171), "marsh" => C(93, 139, 60), "volcano" or "volcanic" => C(126, 114, 64), "river" => C(119, 167, 59), _ => C(135, 177, 51)
    };
    static IllustrationCanvas MakeTerrain(MapDefinition map)
    {
        const int tile = 64, margin = 96; var w = map.Width * tile + margin * 2; var h = map.Height * tile + margin * 2;
        var a = new IllustrationCanvas(w, h); var grass = Meadow(map.Theme); var rng = new Random(431 + map.Width * 31 + map.Id.Sum(c => c));
        // Multiscale colour noise and grass grain, baked into one texture rather than jittering per frame.
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float fine = (Hash(x, y, 11) % 1000) / 1000f;
            float broad = MathF.Sin(x * .021f) * MathF.Sin(y * .027f) + MathF.Sin((x + y) * .047f) * .35f;
            float light = .94f + broad * .035f + fine * .09f;
            float outside = Math.Max(Math.Max(margin - x, x - (w - margin)), Math.Max(margin - y, y - (h - margin)));
            light *= 1 - .18f * Math.Clamp(outside / margin, 0, 1);
            var groundPixel = Shade(grass, light);
            float blend = Math.Clamp(outside / margin, 0, 1);
            groundPixel.A = (byte)MathF.Round(255 * (1 - blend * blend * (3 - 2 * blend)));
            a.Pixels[y * w + x] = groundPixel;
        }
        for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
        {
            float px = margin + x * tile, py = margin + y * tile; var type = map.At(x, y);
            if ((x + y) % 2 == 0) a.Rect(px, py, tile, tile, C(243, 255, 173, 8));
            // Corner crosses establish the build grid without a spreadsheet outline.
            a.Line(px + 3, py, px + 11, py, 1, C(67, 100, 35, 60)); a.Line(px, py + 3, px, py + 11, 1, C(67, 100, 35, 60));
            if (type == CellType.Path)
            {
                a.Round(px - 2, py - 2, tile + 4, tile + 4, 10, C(161, 132, 64, 85)); a.Round(px + 3, py + 3, tile - 6, tile - 6, 8, C(206, 177, 98, 120));
                for (int i = 0; i < 17; i++) { float dx = px + rng.Next(4, 60), dy = py + rng.Next(4, 60); a.Ellipse(dx, dy, 1 + rng.Next(2), .8f + rng.Next(2), C(132, 110, 56, 100)); }
            }
            if (type == CellType.Platform)
            {
                a.Round(px + 3, py + 8, 58, 52, 10, C(42, 60, 37, 100));
                a.Round(px + 3, py + 3, 58, 52, 8, C(117, 126, 89), C(71, 88, 65));
                a.Round(px + 7, py + 6, 50, 45, 6, C(154, 157, 111), C(103, 117, 80));
                a.Round(px + 12, py + 11, 40, 35, 5, C(109, 125, 91)); a.Ring(px + 32, py + 28, 12, 10, 2, C(202, 191, 116, 140));
                foreach (var (xx, yy) in new[] { (12, 12), (52, 12), (12, 45), (52, 45) }) Bolt(a, px + xx, py + yy, 2);
            }
            if (type == CellType.Rock)
            {
                if (map.Theme == "marsh")
                {
                    a.Round(px - 2, py - 2, tile + 4, tile + 4, 7, C(54, 95, 77));
                    a.Line(px + 8, py + 22, px + 34, py + 22, 2, C(136, 172, 123, 80));
                    a.Line(px + 25, py + 47, px + 49, py + 47, 1, C(167, 192, 145, 90));
                    for (int i = 0; i < 3; i++) { float xx = px + 11 + i * 17, yy = py + 34 + (i % 2) * 14; a.Ellipse(xx, yy, 7, 4, C(104, 157, 58), true); a.Line(xx, yy, xx + 8, yy - 3, 1, C(53, 98, 49)); }
                }
                else Rock(a, px + 32, py + 34, 28, map.Theme, x + y * 7);
            }
            if (type is CellType.Ground or CellType.Entrance or CellType.Exit)
            {
                for (int i = 0; i < 7; i++)
                {
                    float xx = px + rng.Next(5, 60), yy = py + rng.Next(8, 60);
                    a.Line(xx - 2, yy, xx - 4, yy - 4, 1, Shade(grass, .8f)); a.Line(xx, yy, xx, yy - 5, 1, Shade(grass, 1.18f));
                }
                if ((x * 11 + y * 7) % 11 == 0) Flower(a, px + 14, py + 46, rng.Next(2) == 0 ? C(232, 225, 170) : C(171, 150, 225), .65f);
            }
        }
        // Split-rail fences outside the buildable area.
        for (int x = margin + 20; x < w - margin - 20; x += 34)
        {
            bool topGate = map.Routes.Any(r => r.Movement == MovementKind.Ground && r.Points.Any(q => q.Y == 0 && Math.Abs(margin + q.X * tile + 32 - x) < 50));
            bool bottomGate = map.Routes.Any(r => r.Movement == MovementKind.Ground && r.Points.Any(q => q.Y == map.Height - 1 && Math.Abs(margin + q.X * tile + 32 - x) < 50));
            if (!topGate) Fence(a, x, margin - 16);
            if (!bottomGate) Fence(a, x, h - margin + 15);
        }
        // Woodland floor: clover, pebbles, flowers and mushrooms between larger silhouettes.
        for (int i = 0; i < (map.Width + map.Height) * 7; i++)
        {
            int x = rng.Next(w), y = rng.Next(h);
            if (x > margin - 14 && x < w - margin + 14 && y > margin - 12 && y < h - margin + 12) continue;
            if (i % 3 == 0) Flower(a, x, y, C(177, 146, 222), .8f);
            else { a.Line(x, y, x - 5, y - 10, 2, C(93, 139, 40)); a.Line(x, y, x + 2, y - 15, 2, C(150, 181, 54)); }
        }
        return a;
    }
    static void Flower(IllustrationCanvas a, float x, float y, Color c, float scale)
    {
        a.Line(x, y + 5 * scale, x - 1, y - 4 * scale, 1.5f * scale, C(62, 111, 38));
        for (int i = 0; i < 5; i++) { float angle = i * MathF.Tau / 5; a.Ellipse(x + MathF.Cos(angle) * 3.7f * scale, y - 4 * scale + MathF.Sin(angle) * 3.7f * scale, 3 * scale, 2.4f * scale, c); }
        a.Ellipse(x, y - 4 * scale, 2.2f * scale, 2 * scale, C(253, 212, 77));
    }
    static void Rock(IllustrationCanvas a, float x, float y, float r, string theme, int variant)
    {
        var stone = theme == "ice" ? C(151, 180, 174) : theme == "crystal" ? C(115, 127, 151) : theme is "volcano" or "volcanic" ? C(105, 94, 82) : C(139, 145, 104);
        a.Ellipse(x + 3, y + r * .65f, r * 1.05f, r * .43f, C(39, 58, 33, 100));
        a.Poly(Shade(stone, .53f), new(x - r, y + r * .4f), new(x - r * .8f, y - r * .45f), new(x - r * .3f, y - r), new(x + r * .5f, y - r * .7f), new(x + r, y + r * .2f), new(x + r * .55f, y + r * .75f), new(x - r * .5f, y + r * .8f));
        a.Poly(stone, new(x - r * .93f, y + r * .1f), new(x - r * .72f, y - r * .5f), new(x - r * .25f, y - r * .88f), new(x + r * .47f, y - r * .61f), new(x + r * .85f, y), new(x + r * .2f, y + r * .32f), new(x - r * .55f, y + r * .4f));
        a.Poly(Shade(stone, 1.22f), new(x - r * .72f, y - r * .5f), new(x - r * .25f, y - r * .88f), new(x + r * .47f, y - r * .61f), new(x + r * .1f, y - r * .2f), new(x - r * .4f, y - r * .1f));
        a.Line(x - r * .36f, y - r * .27f, x + r * .1f, y - r * .2f, Math.Max(1, r * .04f), Shade(stone, .62f));
        if (theme == "crystal")
        {
            a.Poly(C(57, 140, 166), new(x + r * .2f, y + r * .3f), new(x + r * .1f, y - r * 1.3f), new(x + r * .5f, y - r * 1.65f), new(x + r * .8f, y - r), new(x + r * .6f, y + r * .2f));
            a.Poly(C(168, 230, 218), new(x + r * .1f, y - r * 1.3f), new(x + r * .5f, y - r * 1.65f), new(x + r * .45f, y + r * .15f), new(x + r * .2f, y + r * .3f));
        }
        else if (theme == "ice") a.Ellipse(x - r * .15f, y - r * .55f, r * .7f, r * .32f, C(224, 239, 218), true);
        else if (theme is not ("sand" or "volcano" or "volcanic")) { a.Ellipse(x - r * .6f, y + r * .3f, r * .4f, r * .2f, C(88, 128, 45), true); a.Ellipse(x + r * .5f, y + r * .2f, r * .26f, r * .16f, C(108, 143, 50), true); }
    }
    static void Wall(IllustrationCanvas a, float x, float y, string theme)
    {
        a.Round(x - 19, y - 5, 39, 19, 4, C(61, 75, 51, 180));
        a.Round(x - 19, y - 14, 39, 20, 4, C(135, 138, 105), C(98, 111, 83));
        a.Round(x - 17, y - 17, 34, 12, 3, C(188, 180, 137), C(155, 156, 115));
        a.Line(x - 14, y - 16, x + 12, y - 16, 1, C(226, 213, 162)); a.Line(x - 2, y - 12, x + 2, y + 1, 1, C(97, 103, 75));
    }
    static void Fence(IllustrationCanvas a, float x, float y)
    {
        a.Line(x - 18, y, x + 18, y, 7, C(68, 65, 31)); a.Line(x - 18, y - 3, x + 18, y - 3, 4, C(165, 126, 60));
        a.Line(x - 18, y + 10, x + 18, y + 10, 6, C(69, 66, 32)); a.Line(x - 18, y + 8, x + 18, y + 8, 3, C(151, 113, 51));
        a.Line(x, y - 10, x, y + 16, 8, C(83, 72, 31)); a.Line(x - 1, y - 11, x - 1, y + 13, 5, C(177, 139, 63)); a.Ellipse(x - 1, y - 11, 4, 3, C(223, 185, 96));
    }
}
