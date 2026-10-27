using Microsoft.Xna.Framework;
using TowerGame.Platform;
using TowerGame.Rendering;
using static TowerGame.UI.Strings;

namespace TowerGame.UI;

public sealed partial class DefenseGame
{
    void DrawAchievements()
    {
        DrawAttractBackground();
        if (BackButton()) { screen = Screen.Menu; return; }
        p.Display(Uzbek ? "YUTUQLAR" : "ACHIEVEMENTS", safe.X + 40 + Math.Max(130, hit * 2.4f), safe.Y + 19, 30, Painter.Gold);
        (string Id, string En, string Uz, string EnDetail, string UzDetail)[] awards =
        [
            (Achievements.FirstBuild,"First defense","Birinchi mudofaa","Build your first tower.","Birinchi minorani quring."),
            (Achievements.MaxLevel,"Fully armed","To‘liq qurollangan","Upgrade a tower to level 3.","Minorani 3-darajaga ko‘taring."),
            (Achievements.BossDown,"Boss hunter","Boss ovchisi","Defeat a boss.","Bossni yo‘q qiling."),
            (Achievements.Wave50,"Hold the line","Chegarani saqlang","Complete wave 50.","50-to‘lqindan o‘ting."),
            (Achievements.ClassicWin,"Classic champion","Klassik chempion","Win a Classic campaign.","Klassik rejimda g‘alaba qozoning."),
            (Achievements.Flawless,"Perfect defense","Mukammal mudofaa","Win with all 20 lives.","20 jonni saqlab g‘alaba qozoning."),
            (Achievements.AllMaps,"Explorer","Tadqiqotchi","Unlock all eight maps.","Sakkiz xaritani ham oching."),
            (Achievements.Endless150,"Unstoppable","Yengilmas","Reach wave 150 in Endless.","Cheksiz rejimda 150-to‘lqinga yeting.")
        ];
        var top = safe.Y + (int)hit + 42; var gap = 14;
        var cw = (safe.Width - 62) / 2; var rh = (safe.Bottom - top - 35 - gap * 3) / 4;
        for (int i = 0; i < awards.Length; i++)
        {
            var a = awards[i]; var earned = store.EarnedAchievements.Contains(a.Id);
            var r = new Rectangle(safe.X + 24 + i / 4 * (cw + gap), top + i % 4 * (rh + gap), cw, rh);
            Panel(r);
            p.ControlIcon(earned ? UiSymbol.Trophy : UiSymbol.Lock, new Vector2(r.X + 43, r.Center.Y), Math.Min(27, rh * .29f), earned ? null : Painter.Muted);
            p.Display(Uzbek ? a.Uz : a.En, r.X + 85, r.Y + 14, rh < 105 ? 17 : 21, earned ? Painter.Gold : Painter.Ink, outline: false);
            p.Paragraph(Uzbek ? a.UzDetail : a.EnDetail, new Rectangle(r.X + 85, r.Y + 43, r.Width - 108, rh - 48), rh < 105 ? 13 : 16, Painter.Muted);
            if (earned) p.ControlIcon(UiSymbol.Check, new Vector2(r.Right - 22, r.Y + 24), 12, Painter.Mint);
        }
        p.Right($"{store.EarnedAchievements.Count} / 8", safe.Right - 25, safe.Y + 27, 22, Painter.Gold);
    }
}
