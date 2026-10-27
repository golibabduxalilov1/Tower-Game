using Microsoft.Xna.Framework;
using TowerGame.Core;
using TowerGame.Rendering;
using static TowerGame.UI.Strings;

namespace TowerGame.UI;

public sealed partial class DefenseGame
{
    void DrawArmory()
    {
        if (options.Scene == "units") { DrawUnitStudy(); return; }
        if (options.Scene == "soldiers") { DrawSoldierStudy(); return; }
        if (attract != null) field.Map(attract.Map, new Rectangle(0, 0, (int)W, (int)H), attract, false, time: clock);
        p.Rect(0, 0, W, H, new Color(12, 26, 32) * .90f);
        p.Display(Uzbek ? "AGS TOWER ARSENALI" : "AGS TOWER ARMORY", safe.X + 30, safe.Y + 20, 35, Painter.Gold);
        p.Text(Uzbek ? "8 QUROL  /  3 DARAJA  /  YER, HAVO VA RELS" : "8 SYSTEMS  /  3 LEVELS  /  GROUND, AIR & RAIL", safe.X + 32, safe.Y + 67, 13, Painter.Muted);
        var grid = new Rectangle(safe.X + 28, safe.Y + 102, safe.Width - 56, (int)(safe.Height * .59f));
        var width = grid.Width / 8f; var row = (grid.Height - 32) / 3f;
        foreach (var kind in Enum.GetValues<TowerKind>())
        {
            var col = (int)kind;
            var centerX = grid.X + (col + .5f) * width;
            p.DisplayCenter(Name(kind), new Rectangle((int)(centerX - width / 2), grid.Y - 3, (int)width, 28), 14, Battlefield.Accent(kind));
            for (int level = 1; level <= 3; level++)
            {
                var y = grid.Y + 32 + (level - .5f) * row;
                p.Circle(new Vector2(centerX, y + 24), Math.Min(width, row) * .34f, new Color(16, 25, 29));
                field.Tower(new Vector2(centerX, y - 5), Math.Min(width, row) * .97f, kind, level, -.67f);
                for (int star = 0; star < level; star++) p.Star(new Vector2(centerX + (star - (level - 1) * .5f) * 14, y + row * .36f), 5, Painter.Gold);
            }
        }
        var enemyY = grid.Bottom + 18;
        p.Line(new Vector2(safe.X + 30, enemyY), new Vector2(safe.Right - 30, enemyY), new Color(152, 132, 77), 1.5f);
        p.Text(Uzbek ? "DUSHMAN BIRLIKLARI" : "HOSTILE UNITS", safe.X + 30, enemyY + 12, 12, Painter.Gold);
        var enemyWidth = (safe.Width - 60) / 11f;
        var iconSize = Math.Min(enemyWidth * .83f, safe.Bottom - enemyY - 62);
        foreach (var kind in Enum.GetValues<EnemyKind>())
        {
            var x = safe.X + 30 + ((int)kind + .5f) * enemyWidth;
            var y = enemyY + 44 + iconSize * .38f;
            field.EnemyIcon(new Vector2(x, y), iconSize, kind, clock);
            var label = Name(kind); var size = 11f;
            while (p.Measure(label, size) > enemyWidth - 6 && size > 8) size--;
            p.Center(label, new Rectangle((int)(x - enemyWidth / 2), (int)(y + iconSize * .43f), (int)enemyWidth, 22), size, Painter.Ink);
        }
    }

    // QA gallery uses the same directional sprite renderer as live battles and enemy icons.
    void DrawUnitStudy()
    {
        if (attract != null) field.Map(attract.Map, new Rectangle(0, 0, (int)W, (int)H), time: clock);
        p.Rect(0, 0, W, H, new Color(12, 26, 32) * .88f);
        p.Display(Uzbek ? "DUSHMAN BIRLIKLARI" : "HOSTILE UNITS", safe.X + 30, safe.Y + 18, 30, Painter.Gold);
        var kinds = Enum.GetValues<EnemyKind>();
        float width = (safe.Width - 60f) / 4, height = (safe.Height - 95f) / 3;
        for (int i = 0; i < kinds.Length; i++)
        {
            var kind = kinds[i];
            float x = safe.X + 30 + (i % 4 + .5f) * width;
            float y = safe.Y + 85 + i / 4 * height;
            bool human = kind is EnemyKind.Infantry or EnemyKind.Runner or EnemyKind.Heavy;
            float cell = Math.Min(width * .54f, height * .65f) * (Balance.Enemy(kind).Boss ? .55f : .8f);
            var foot = new Vector2(x, y + height * (human ? .71f : .57f));
            float angle = (float)(clock * .65) + i * .52f;
            p.Soldier(kind, foot, cell, angle, clock * (human ? 1.8 : 6) + i * .17);
            p.Center(Name(kind), new Rectangle((int)(x - width / 2), (int)(y + height - 27), (int)width, 24), 15, Painter.Ink);
        }
    }

    void DrawSoldierStudy()
    {
        if (attract != null) field.Map(attract.Map, new Rectangle(0, 0, (int)W, (int)H), time: clock);
        p.Rect(0, 0, W, H, new Color(12, 26, 32) * .88f);
        p.Display(Uzbek ? "ASKARLAR HARAKATI" : "SOLDIERS IN MOTION", safe.X + 30, safe.Y + 20, 32, Painter.Gold);
        p.Text(Uzbek ? "Piyoda, tezkor va og‘ir askar — to‘rt yo‘nalishda" : "Infantry, runner and heavy — four directions", safe.X + 32, safe.Y + 65, 16);
        string[] headings = Uzbek ? ["Yuqoriga", "O‘ngga", "Pastga", "Chapga"] : ["North", "East", "South", "West"];
        float[] angles = [-MathHelper.PiOver2, 0, MathHelper.PiOver2, MathHelper.Pi];
        EnemyKind[] kinds = [EnemyKind.Infantry, EnemyKind.Runner, EnemyKind.Heavy];
        var left = safe.X + 150f; var col = (safe.Width - 180f) / 4;
        for (int c = 0; c < 4; c++) p.DisplayCenter(headings[c], new Rectangle((int)(left + c * col), safe.Y + 99, (int)col, 28), 18, Painter.Ink);
        float rowHeight = (safe.Height - 140f) / 3;
        for (int row = 0; row < kinds.Length; row++)
        {
            var kind = kinds[row]; float y = safe.Y + 136 + row * rowHeight;
            p.Text(Name(kind), safe.X + 30, y + rowHeight * .45f, 15, Painter.Gold);
            for (int c = 0; c < 4; c++)
            {
                var foot = new Vector2(left + (c + .5f) * col, y + rowHeight * .78f);
                p.Box(new Rectangle((int)foot.X - 36, (int)foot.Y - 5, 72, 15), new Color(0, 0, 0) * .22f, 7);
                var rate = kind == EnemyKind.Runner ? 2.3 : kind == EnemyKind.Heavy ? 1.0 : 1.55;
                p.Soldier(kind, foot, Math.Min(118, rowHeight * .78f), angles[c], clock * rate + c * .17);
            }
        }
    }
}
