using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerGame.Rendering;

public enum UiSymbol
{
    Play, Pause, Fast, Settings, Sound, Music, Trophy, Back, Star, Lock,
    Upgrade, Sell, Info, Restart, Exit, Heart, Coin, Map, Check, Close, Next, Path
}

/// <summary>Game control silhouettes, baked once with outlines, warm metal highlights and shadows.</summary>
public sealed partial class Painter
{
    readonly Dictionary<UiSymbol, Texture2D> controlSymbols = [];

    public void ControlIcon(UiSymbol symbol, Vector2 center, float radius, Color? color = null)
    {
        if (!controlSymbols.TryGetValue(symbol, out var texture))
            controlSymbols[symbol] = texture = BakeControl(symbol);
        Batch.Draw(texture, center, null, color ?? Color.White, 0, new Vector2(64), radius / 64, SpriteEffects.None, 0);
    }

    Texture2D BakeControl(UiSymbol symbol)
    {
        var a = new IllustrationCanvas(128, 128);
        var edge = new Color(44, 49, 40);
        var ink = new Color(255, 222, 132);
        var highlight = new Color(255, 248, 198);
        void Poly(params Vector2[] points)
        {
            a.Poly(edge, points);
            for (var i = 0; i < points.Length; i++)
                a.Line(points[i].X, points[i].Y, points[(i + 1) % points.Length].X, points[(i + 1) % points.Length].Y, 7, edge);
            a.Poly(ink, points);
            for (var i = 0; i < points.Length - 1; i++)
                if (points[i + 1].X > points[i].X)
                    a.Line(points[i].X + 1, points[i].Y - 1, points[i + 1].X - 1, points[i + 1].Y - 1, 2, highlight);
        }
        void Stroke(float x, float y, float xx, float yy, float width = 10)
        {
            a.Line(x, y + 1, xx, yy + 1, width + 5, edge);
            a.Line(x, y, xx, yy, width, ink);
            a.Line(x - 1, y - 2, xx - 1, yy - 2, Math.Max(2, width * .19f), highlight);
        }
        void Disc(float x, float y, float r)
        {
            a.Ellipse(x, y + 2, r + 3, r + 3, edge);
            a.Ellipse(x, y, r, r, ink, true);
        }
        void Arc(float x, float y, float radius, float start, float end, float width = 8)
        {
            for (var i = 0; i < 20; i++)
            {
                var aa = start + (end - start) * i / 20;
                var bb = start + (end - start) * (i + 1) / 20;
                Stroke(x + MathF.Cos(aa) * radius, y + MathF.Sin(aa) * radius,
                    x + MathF.Cos(bb) * radius, y + MathF.Sin(bb) * radius, width);
            }
        }
        switch (symbol)
        {
            case UiSymbol.Play:
                Poly(new(41, 27), new(98, 64), new(41, 101)); break;
            case UiSymbol.Pause:
                Poly(new(30, 28), new(49, 28), new(49, 100), new(30, 100));
                Poly(new(78, 28), new(97, 28), new(97, 100), new(78, 100)); break;
            case UiSymbol.Fast:
                Poly(new(19, 30), new(62, 64), new(19, 98));
                Poly(new(68, 30), new(111, 64), new(68, 98)); break;
            case UiSymbol.Settings:
                var gear = new Vector2[48];
                for (var i = 0; i < gear.Length; i++)
                {
                    var ang = -MathF.PI / 2 + i * MathF.Tau / gear.Length;
                    var radius = i % 6 is 1 or 2 or 3 ? 48 : 37;
                    gear[i] = new(64 + MathF.Cos(ang) * radius, 64 + MathF.Sin(ang) * radius);
                }
                Poly(gear); a.Ellipse(64, 64, 23, 23, edge); a.Ellipse(64, 62, 16, 16, new Color(146, 165, 167), true); break;
            case UiSymbol.Sound:
                Poly(new(24, 49), new(43, 49), new(67, 28), new(67, 100), new(43, 79), new(24, 79));
                Arc(63, 64, 25, -.85f, .85f, 6); Arc(64, 64, 43, -.85f, .85f, 6); break;
            case UiSymbol.Music:
                Stroke(47, 84, 47, 33, 10); Stroke(92, 75, 92, 24, 10);
                Poly(new(44, 29), new(97, 19), new(97, 39), new(44, 48));
                a.Ellipse(34, 90, 19, 14, edge); a.Ellipse(33, 87, 16, 11, ink, true);
                a.Ellipse(79, 81, 19, 14, edge); a.Ellipse(78, 78, 16, 11, ink, true); break;
            case UiSymbol.Trophy:
                Arc(39, 49, 20, 1.4f, 4.9f, 9); Arc(88, 49, 20, -1.7f, 1.6f, 9);
                Poly(new(36, 25), new(92, 25), new(88, 66), new(76, 79), new(52, 79), new(40, 66));
                Stroke(64, 79, 64, 96, 12); Poly(new(40, 98), new(89, 98), new(96, 109), new(32, 109)); break;
            case UiSymbol.Back:
                Poly(new(61, 23), new(21, 64), new(61, 105), new(61, 80), new(107, 80), new(107, 48), new(61, 48)); break;
            case UiSymbol.Next:
                Poly(new(67, 23), new(107, 64), new(67, 105), new(67, 80), new(21, 80), new(21, 48), new(67, 48)); break;
            case UiSymbol.Star:
                var star = new Vector2[10];
                for (var i = 0; i < 10; i++)
                {
                    var ang = -MathF.PI / 2 + i * MathF.PI / 5; var r = i % 2 == 0 ? 46 : 22;
                    star[i] = new(64 + MathF.Cos(ang) * r, 66 + MathF.Sin(ang) * r);
                }
                Poly(star); break;
            case UiSymbol.Lock:
                Arc(64, 48, 25, MathF.PI, MathF.Tau, 12);
                Poly(new(29, 49), new(99, 49), new(99, 105), new(29, 105));
                a.Ellipse(64, 72, 8, 8, edge); a.Rect(59, 75, 10, 17, edge); break;
            case UiSymbol.Upgrade:
                Poly(new(64, 18), new(106, 54), new(82, 54), new(82, 93), new(46, 93), new(46, 54), new(22, 54));
                Stroke(37, 107, 91, 107, 8); break;
            case UiSymbol.Coin: case UiSymbol.Sell:
                Disc(64, 63, 45); a.Ring(64, 62, 35, 35, 3, new Color(173, 114, 32));
                a.Line(64, 29, 64, 97, 5, edge);
                a.Line(78, 41, 51, 41, 7, edge); a.Line(51, 41, 45, 59, 7, edge);
                a.Line(45, 59, 80, 70, 7, edge); a.Line(80, 70, 74, 86, 7, edge); a.Line(74, 86, 45, 86, 7, edge); break;
            case UiSymbol.Info:
                Disc(64, 64, 44); a.Ellipse(64, 40, 6, 6, edge);
                a.Round(58, 55, 12, 35, 4, edge); break;
            case UiSymbol.Restart:
                Arc(64, 66, 35, -.6f, 4.7f, 12);
                Poly(new(88, 20), new(82, 58), new(110, 40)); break;
            case UiSymbol.Exit:
                Stroke(33, 24, 33, 105, 9); Stroke(33, 24, 71, 24, 9); Stroke(33, 105, 71, 105, 9);
                Poly(new(80, 44), new(107, 65), new(80, 87), new(80, 73), new(48, 73), new(48, 57), new(80, 57)); break;
            case UiSymbol.Heart:
                ink = new Color(242, 88, 67); Disc(45, 49, 25); Disc(83, 49, 25);
                Poly(new(20, 53), new(108, 53), new(64, 108));
                a.Ellipse(37, 40, 10, 7, new Color(255, 188, 148)); break;
            case UiSymbol.Map:
                Poly(new(17, 33), new(48, 23), new(81, 35), new(111, 23), new(111, 97), new(81, 108), new(48, 96), new(17, 107));
                a.Line(48, 27, 48, 92, 3, new Color(140, 126, 75)); a.Line(81, 39, 81, 102, 3, new Color(140, 126, 75));
                a.Line(29, 78, 48, 57, 4, new Color(73, 127, 67)); a.Line(48, 57, 70, 76, 4, new Color(73, 127, 67));
                a.Ellipse(92, 58, 9, 9, new Color(196, 63, 46)); break;
            case UiSymbol.Check:
                Stroke(26, 65, 51, 91, 14); Stroke(51, 91, 104, 32, 14); break;
            case UiSymbol.Close:
                Stroke(34, 34, 95, 95, 14); Stroke(95, 34, 34, 95, 14); break;
            case UiSymbol.Path:
                Stroke(22, 98, 22, 51, 11); Stroke(22, 51, 81, 51, 11); Stroke(81, 51, 81, 28, 11);
                Poly(new(63, 33), new(81, 13), new(103, 33)); break;
        }
        var output = new IllustrationCanvas(128, 128);
        for (var y = 0; y < 124; y++) for (var x = 0; x < 125; x++)
            if (a.Pixels[y * 128 + x].A > 0)
                output.Pixel(x + 3, y + 4, new Color(18, 26, 28, (int)(a.Pixels[y * 128 + x].A * .7f)));
        output.Copy(a, 0, 0);
        return CreateTexture(128, 128, output.Finish());
    }
}
