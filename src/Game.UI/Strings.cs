using TowerGame.Core;

namespace TowerGame.UI;

/// <summary>Uzbek (Latin, with U+2018 for o‘/g‘) and English interface text.</summary>
public static class Strings
{
    public static bool Uzbek { get; set; } = true;

    public static string T(string key) =>
        (Uzbek ? Uz : En).TryGetValue(key, out var text) ? text : En.GetValueOrDefault(key, key);
    public static string F(string key, params object[] args) => string.Format(T(key), args);

    static readonly Dictionary<string, string> Uz = new()
    {
        ["title"] = "AGS TOWER", ["subtitle"] = "Yo‘lni qur. Chiziqni ushla.",
        ["loading"] = "Yuklanmoqda…",
        ["continue"] = "Davom ettirish", ["play"] = "O‘ynash", ["settings"] = "Sozlamalar", ["help"] = "Yordam", ["quit"] = "Chiqish",
        ["back"] = "Orqaga", ["start"] = "Boshlash", ["cancel"] = "Bekor qilish", ["confirm"] = "Tasdiqlash", ["yes"] = "Ha", ["no"] = "Yo‘q",
        ["choose_map"] = "Xaritani tanlang", ["locked_map"] = "Oldingi xaritada 50-to‘lqinni yakunlang",
        ["best"] = "Rekord", ["no_record"] = "Rekord yo‘q",
        ["mode"] = "Rejim", ["difficulty"] = "Qiyinlik", ["towers"] = "Minoralar",
        ["Classic"] = "Classic", ["Extended"] = "Extended", ["Endless"] = "Endless",
        ["Classic_desc"] = "Xarita to‘lqinlari, asosiy to‘rtta minora.", ["Extended_desc"] = "Xarita to‘lqinlari, yana ikkita maxsus minora.", ["Endless_desc"] = "Cheksiz to‘lqinlar, Extended qurollari.",
        ["mode_locked"] = "Bu xaritada Classic’ni yengsangiz ochiladi",
        ["Easy"] = "Oson", ["Normal"] = "O‘rtacha", ["Hard"] = "Qiyin",
        ["Easy_desc"] = "Sog‘liq ×0,8 · 180 pul", ["Normal_desc"] = "Sog‘liq ×1 · 150 pul", ["Hard_desc"] = "Sog‘liq ×1,35 · 130 pul",
        ["overwrite"] = "Saqlangan jang o‘chiriladi. Yangi jang boshlansinmi?",
        ["wave"] = "To‘lqin", ["enemies"] = "Dushman", ["next"] = "Keyingi", ["boss_next"] = "Keyingi to‘lqinda BOSS!", ["boss_now"] = "BOSS hujumi!",
        ["start_wave"] = "Boshlash", ["call_early"] = "Hozir chaqirish", ["break_in"] = "Keyingi: {0} s",
        ["speed"] = "Tezlik", ["path"] = "Yo‘l",
        ["paused"] = "Pauza", ["resume"] = "Davom etish", ["save_exit"] = "Saqlash va chiqish", ["exit_nosave"] = "Menyuga chiqish",
        ["victory"] = "G‘alaba!", ["defeat"] = "Mag‘lubiyat", ["score"] = "Ball", ["reached"] = "Yetib borilgan to‘lqin", ["lives_left"] = "Qolgan hayot",
        ["retry"] = "Qayta urinish", ["to_maps"] = "Xaritalar", ["new_record"] = "Yangi rekord!",
        ["level"] = "Daraja", ["damage"] = "Zarar", ["rate"] = "Otish tezligi", ["per_sec"] = "{0}/s", ["range"] = "Radius", ["special"] = "Maxsus ta’sir",
        ["upgrade"] = "Kuchaytirish", ["sell"] = "Sotish", ["max_level"] = "Maksimal daraja", ["sell_value"] = "Sotish qiymati", ["upgrade_cost"] = "Keyingi daraja",
        ["priority"] = "Nishon", ["sell_confirm"] = "{0} minorani {1} pulga sotasizmi?",
        ["ArrivalTime"] = "Birinchi yetadigan", ["Nearest"] = "Eng yaqin", ["Strongest"] = "Eng kuchli", ["Weakest"] = "Eng zaif", ["ExitNearest"] = "Chiqishga eng yaqin",
        ["not_enough_money"] = "Pul yetarli emas", ["path_blocked"] = "Yo‘l yopilib qoladi", ["invalid_cell"] = "Bu yerga qurib bo‘lmaydi",
        ["enemy_occupies_cell"] = "Dushman bu katakda", ["tower_unavailable"] = "Bu minora mavjud emas", ["battle_over"] = "Jang tugagan",
        ["save_failed"] = "Saqlab bo‘lmadi. Oldingi saqlash o‘zgarmadi.", ["saved"] = "Saqlandi",
        ["restore_title"] = "Saqlashni tiklab bo‘lmadi", ["restore_text"] = "Bu jang eski xarita tuzilishiga tegishli yoki fayli buzilgan. Profil, rekordlar va ochilgan hududlar saqlanib qoldi. Yangi xaritada jang boshlashingiz mumkin.",
        ["restore_backup"] = "Asosiy fayl buzilgan edi, zaxira nusxadan tiklandi.",
        ["discard"] = "Faylni o‘chirish", ["menu"] = "Menyu",
        ["language"] = "Til", ["master"] = "Umumiy ovoz", ["music"] = "Musiqa", ["effects"] = "Effektlar", ["mute"] = "Ovozni o‘chirish",
        ["show_path"] = "Yo‘lni ko‘rsatish", ["shake"] = "Kamera silkinishi", ["flashes"] = "Kuchli chaqnashlar", ["fps"] = "Kadr tezligi",
        ["on"] = "Yoqilgan", ["off"] = "O‘chirilgan", ["keys"] = "Klavishlar", ["press_key"] = "Klavishni bosing… (Esc — bekor)", ["reset_keys"] = "Standart klavishlar",
        ["Pause"] = "Pauza", ["Speed"] = "Tezlik 1×/2×", ["Upgrade"] = "Kuchaytirish", ["Sell"] = "Sotish",
        ["Tower1"] = "Minora 1", ["Tower2"] = "Minora 2", ["Tower3"] = "Minora 3", ["Tower4"] = "Minora 4", ["Tower5"] = "Minora 5", ["Tower6"] = "Minora 6",
        ["help_towers"] = "Minoralar", ["help_enemies"] = "Dushmanlar", ["help_controls"] = "Boshqaruv", ["help_rules"] = "Qoidalar",
        ["cost"] = "Narx", ["targets"] = "Nishon", ["ground"] = "yer", ["air"] = "havo", ["rail"] = "rels",
        ["health"] = "Sog‘liq", ["speed_cells"] = "Tezlik", ["reward"] = "Mukofot", ["resist"] = "Qarshilik", ["none"] = "yo‘q",
        ["boss"] = "Boss",
        ["controls_touch"] = "Sensor: pastki paneldan minorani tanlang, katakka teging va «Tasdiqlash» bilan quring. Bo‘sh joyga yoki «Bekor qilish»ga tegib chiqing. Minoraga tegib kuchaytiring yoki soting. Ikki barmoq bilan kattalashtiring va suring. «Orqaga» avval oynani yopadi, keyin pauza.",
        ["controls_mouse"] = "Sichqoncha: chap tugma — tanlash va qurish; o‘ng tugma yoki Esc — bekor qilish; g‘ildirak — kattalashtirish; o‘rta tugma bilan surish — kamera.",
        ["controls_keys"] = "Klavishlar: Space — pauza, 1–6 — minora, U — kuchaytirish, Delete — sotish, F — 1×/2×. Sozlamalarda o‘zgartirish mumkin.",
        ["rules_text"] = "Dushmanlar chiqishga yetmasin. Minoralar yer dushmanlarining yo‘lini o‘zgartiradi, lekin yo‘lni butunlay yopib bo‘lmaydi. Oddiy dushman o‘tsa −1, boss o‘tsa −3 hayot. Har 10-to‘lqinda boss keladi. Pul dushman o‘ldirishdan beriladi; umumiy to‘lqin bonusi yo‘q. Ochko puldan alohida. Birinchi to‘lqingacha sotish 100%, keyin 70% qaytaradi. G‘alabada qolgan hayot yulduzlarni belgilaydi: 20 — 3, 10–19 — 2, 1–9 — 1.",
        ["Infantry"] = "Piyoda", ["Runner"] = "Yuguruvchi", ["Heavy"] = "Og‘ir askar", ["Motorcycle"] = "Mototsikl", ["Tank"] = "Tank",
        ["Helicopter"] = "Vertolyot", ["Plane"] = "Samolyot", ["RailCart"] = "Rels aravasi", ["HeavyTank"] = "Og‘ir tank", ["AirFortress"] = "Havo qal’asi", ["RailColossus"] = "Rels kolossi",
        ["Gatling"] = "Gatling", ["Goo"] = "Yelim", ["Missile"] = "Raketa", ["Tesla"] = "Tesla", ["Flame"] = "Olov", ["Mortar"] = "Minomyot", ["Laser"] = "Lazer", ["Ice"] = "Muz",
        ["resist_tank"] = "Gatling va olov −20%", ["resist_heavytank"] = "Gatling va olov −25%; sekinlash yarim", ["resist_boss"] = "Sekinlash yarim", ["resist_colossus"] = "Sekinlashmaydi",
        ["stars"] = "Yulduzlar",
        ["bench_run"] = "Tezlik sinovini boshlash", ["bench_title"] = "Tezlik sinovi: 300 dushman, 200 minora, 2×", ["bench_saved"] = "Natija ilova papkasidagi hisobotga yozildi.",
    };

    static readonly Dictionary<string, string> En = new()
    {
        ["title"] = "AGS TOWER", ["subtitle"] = "Build the route. Hold the line.",
        ["loading"] = "Loading…",
        ["continue"] = "Continue", ["play"] = "Play", ["settings"] = "Settings", ["help"] = "Help", ["quit"] = "Quit",
        ["back"] = "Back", ["start"] = "Start", ["cancel"] = "Cancel", ["confirm"] = "Confirm", ["yes"] = "Yes", ["no"] = "No",
        ["choose_map"] = "Choose a map", ["locked_map"] = "Complete wave 50 on the previous map",
        ["best"] = "Best", ["no_record"] = "No record",
        ["mode"] = "Mode", ["difficulty"] = "Difficulty", ["towers"] = "Towers",
        ["Classic"] = "Classic", ["Extended"] = "Extended", ["Endless"] = "Endless",
        ["Classic_desc"] = "Map rounds with the four core towers.", ["Extended_desc"] = "Map rounds, plus two special towers.", ["Endless_desc"] = "Endless waves with the Extended arsenal.",
        ["mode_locked"] = "Win Classic on this map to unlock",
        ["Easy"] = "Easy", ["Normal"] = "Normal", ["Hard"] = "Hard",
        ["Easy_desc"] = "Health ×0.8 · 180 money", ["Normal_desc"] = "Health ×1 · 150 money", ["Hard_desc"] = "Health ×1.35 · 130 money",
        ["overwrite"] = "Your saved battle will be deleted. Start a new battle?",
        ["wave"] = "Wave", ["enemies"] = "Enemies", ["next"] = "Next", ["boss_next"] = "BOSS in the next wave!", ["boss_now"] = "BOSS attack!",
        ["start_wave"] = "Start", ["call_early"] = "Call now", ["break_in"] = "Next in {0} s",
        ["speed"] = "Speed", ["path"] = "Path",
        ["paused"] = "Paused", ["resume"] = "Resume", ["save_exit"] = "Save and exit", ["exit_nosave"] = "Exit to menu",
        ["victory"] = "Victory!", ["defeat"] = "Defeat", ["score"] = "Score", ["reached"] = "Wave reached", ["lives_left"] = "Lives left",
        ["retry"] = "Retry", ["to_maps"] = "Maps", ["new_record"] = "New record!",
        ["level"] = "Level", ["damage"] = "Damage", ["rate"] = "Fire rate", ["per_sec"] = "{0}/s", ["range"] = "Range", ["special"] = "Special",
        ["upgrade"] = "Upgrade", ["sell"] = "Sell", ["max_level"] = "Maximum level", ["sell_value"] = "Sell value", ["upgrade_cost"] = "Next level",
        ["priority"] = "Target", ["sell_confirm"] = "Sell {0} for {1}?",
        ["ArrivalTime"] = "First to arrive", ["Nearest"] = "Nearest", ["Strongest"] = "Strongest", ["Weakest"] = "Weakest", ["ExitNearest"] = "Closest to exit",
        ["not_enough_money"] = "Not enough money", ["path_blocked"] = "That would block the path", ["invalid_cell"] = "Can't build here",
        ["enemy_occupies_cell"] = "An enemy is on this cell", ["tower_unavailable"] = "Tower not available", ["battle_over"] = "The battle is over",
        ["save_failed"] = "Couldn't save. Your previous save is unchanged.", ["saved"] = "Saved",
        ["restore_title"] = "Couldn't restore the save", ["restore_text"] = "This battle belongs to an older map layout, or its save is damaged. Your profile, records and unlocked regions are safe. You can start a battle on the new map.",
        ["restore_backup"] = "The main save was damaged and was restored from the backup.",
        ["discard"] = "Delete save", ["menu"] = "Menu",
        ["language"] = "Language", ["master"] = "Master volume", ["music"] = "Music", ["effects"] = "Effects", ["mute"] = "Mute all",
        ["show_path"] = "Show path", ["shake"] = "Camera shake", ["flashes"] = "Strong flashes", ["fps"] = "Frame rate",
        ["on"] = "On", ["off"] = "Off", ["keys"] = "Keys", ["press_key"] = "Press a key… (Esc cancels)", ["reset_keys"] = "Default keys",
        ["Pause"] = "Pause", ["Speed"] = "Speed 1×/2×", ["Upgrade"] = "Upgrade", ["Sell"] = "Sell",
        ["Tower1"] = "Tower 1", ["Tower2"] = "Tower 2", ["Tower3"] = "Tower 3", ["Tower4"] = "Tower 4", ["Tower5"] = "Tower 5", ["Tower6"] = "Tower 6",
        ["help_towers"] = "Towers", ["help_enemies"] = "Enemies", ["help_controls"] = "Controls", ["help_rules"] = "Rules",
        ["cost"] = "Cost", ["targets"] = "Targets", ["ground"] = "ground", ["air"] = "air", ["rail"] = "rail",
        ["health"] = "Health", ["speed_cells"] = "Speed", ["reward"] = "Reward", ["resist"] = "Resistance", ["none"] = "none",
        ["boss"] = "Boss",
        ["controls_touch"] = "Touch: pick a tower in the bottom bar, tap a cell and press Confirm to build. Tap empty space or Cancel to leave. Tap a tower to upgrade or sell it. Pinch to zoom and drag with two fingers. Back closes the open panel first, then pauses.",
        ["controls_mouse"] = "Mouse: left button selects and builds; right button or Esc cancels; wheel zooms; drag with the middle button to move the camera.",
        ["controls_keys"] = "Keys: Space pause, 1–6 towers, U upgrade, Delete sell, F 1×/2×. Rebind them in Settings.",
        ["rules_text"] = "Stop enemies before they reach the exit. Towers reroute ground enemies, but the path can never be closed completely. A normal enemy that escapes costs 1 life, a boss costs 3. A boss arrives every 10th wave. Kills earn battle money; there is no universal wave bonus. Score is separate from money. Selling refunds 100% before the first wave, 70% afterwards. Lives left on victory decide the stars: 20 — 3, 10–19 — 2, 1–9 — 1.",
        ["Infantry"] = "Infantry", ["Runner"] = "Runner", ["Heavy"] = "Heavy soldier", ["Motorcycle"] = "Motorcycle", ["Tank"] = "Tank",
        ["Helicopter"] = "Helicopter", ["Plane"] = "Plane", ["RailCart"] = "Rail cart", ["HeavyTank"] = "Heavy tank", ["AirFortress"] = "Air fortress", ["RailColossus"] = "Rail colossus",
        ["Gatling"] = "Gatling", ["Goo"] = "Goo", ["Missile"] = "Missile", ["Tesla"] = "Tesla", ["Flame"] = "Flame", ["Mortar"] = "Mortar", ["Laser"] = "Laser", ["Ice"] = "Ice",
        ["resist_tank"] = "Gatling and Flame −20%", ["resist_heavytank"] = "Gatling and Flame −25%; half slow", ["resist_boss"] = "Half slow", ["resist_colossus"] = "Cannot be slowed",
        ["stars"] = "Stars",
        ["bench_run"] = "Run performance test", ["bench_title"] = "Performance test: 300 enemies, 200 towers, 2×", ["bench_saved"] = "The result was appended to the report in the app folder.",
    };

    /// <summary>Keys of one language table (used by the localization parity test).</summary>
    public static IReadOnlyCollection<string> TableKeys(bool uzbek) => (uzbek ? Uz : En).Keys;

    public static string Name(TowerKind kind) => T(kind.ToString());
    public static string Name(EnemyKind kind) => T(kind.ToString());

    public static string Resistance(EnemyKind kind)
    {
        var parts = new List<string>();
        foreach (var tower in new[] { TowerKind.Gatling, TowerKind.Flame, TowerKind.Missile })
        {
            var delta = (Balance.DamageMultiplier(kind, tower) - 1) * 100;
            if (Math.Abs(delta) > .01) parts.Add($"{Name(tower)} {delta:+0;-0}%");
        }
        var slow = Balance.SlowEffectiveness(kind);
        if (slow == 0) parts.Add(Uzbek ? "Slow immunitet" : "Slow immune");
        else if (slow < 1) parts.Add($"Slow -{(1 - slow) * 100:0}%");
        return parts.Count == 0 ? T("none") : string.Join(" · ", parts);
    }

    /// <summary>Decimal comma in Uzbek, point in English; integers and halves only need one digit.</summary>
    public static string Num(double value, int digits = 1)
    {
        var text = Math.Round(value, digits).ToString("0." + new string('#', digits), System.Globalization.CultureInfo.InvariantCulture);
        return Uzbek ? text.Replace('.', ',') : text;
    }

    /// <summary>Compact display of large Endless numbers: 12 345 678 → 12,3M.</summary>
    public static string Short(long value)
    {
        if (value < 100000) return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', Uzbek ? ' ' : ',');
        string[] units = ["K", "M", "B", "T", "Q"]; double v = value; var i = -1;
        while (v >= 1000 && i < units.Length - 1) { v /= 1000; i++; }
        return Num(v, v < 100 ? 1 : 0) + units[i];
    }
}
