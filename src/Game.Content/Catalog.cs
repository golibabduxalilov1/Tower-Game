using System.Text.Json;
using System.Text.Json.Serialization;
using TowerGame.Core;

namespace TowerGame.Content;

public sealed class ContentPack
{
    public int FormatVersion { get; set; } = 1;
    public string ContentVersion { get; set; } = "1.0.0";
    public List<MapDefinition> Maps { get; set; } = [];
}

/// <summary>Authored, versioned campaign data. Runtime never randomizes campaign waves.</summary>
public static class Catalog
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Lazy<ContentPack> Data = new(Load);
    public static IReadOnlyList<MapDefinition> Maps => Data.Value.Maps;
    public static string Version => Data.Value.ContentVersion;
    public static MapDefinition Get(string id) =>
        Maps.FirstOrDefault(m => m.Id == id) ?? throw new ArgumentException("Unknown map.", nameof(id));
    public static int IndexOf(string id)
    {
        for (int i = 0; i < Maps.Count; i++) if (Maps[i].Id == id) return i;
        return -1;
    }
    public static string Description(string id, bool uz) => uz ? id switch
    {
        "valley" => "Ochiq vodiyda birinchi mudofaa labirintingizni quring.",
        "twin-streams" => "Kesishuvchi ikki yo‘nalishni umumiy hujum hududida to‘xtating.",
        "sand-fortress" => "Ikki darvoza va alohida havo yo‘li orasida mudofaani taqsimlang.",
        "crystal-mine" => "Yer hujumi ustidan kon temiryo‘li o‘tadi.",
        "stone-gates" => "Ochiq maydonda minoralar bilan tor yo‘laklar yarating.",
        "ice-core" => "Markaziy reaktorni to‘rt tomondan kelayotgan hujumdan himoyalang.",
        "marsh-border" => "Ko‘tarilgan maydonchalardan burama yo‘l va temiryo‘lni qamrang.",
        "volcanic-ring" => "Yer, havo va ikki temiryo‘l hujumini birga boshqaring.",
        _ => ""
    } : Get(id).Description;
    public static string TowerDescription(TowerKind kind, bool uz) => (kind, uz) switch
    {
        (TowerKind.Gatling, true) => "Tezkor bitta nishon. Yer, havo va rels. Tank zirhi zararni kamaytiradi.",
        (TowerKind.Goo, true) => "Yer va havoni 2 soniya 40/60/75% sekinlashtiradi.",
        (TowerKind.Missile, true) => "Nishon qatlamida 1 katak radiusli portlash.",
        (TowerKind.Tesla, true) => "1,5 katak ichidagi yana ikki nishonga 50% va 25% zarba.",
        (TowerKind.Flame, true) => "60° konusda yer va relsga zarba. Tank zirhi zararni kamaytiradi.",
        (TowerKind.Mortar, true) => "1,25–3,5 katakdan otadi; belgilangan joyda 1,5 katak portlash.",
        (TowerKind.Laser, true) => "Eni 0,35 katak bo‘lgan chiziqdagi barcha mos nishonlarga zarba.",
        (TowerKind.Ice, true) => "Zararsiz: 2,5 soniya 45/60/70% sekinlashish.",
        (TowerKind.Gatling, false) => "Rapid single-target fire. Ground, air and rail. Tank armor reduces damage.",
        (TowerKind.Goo, false) => "Slows ground and air by 40/60/75% for 2 seconds.",
        (TowerKind.Missile, false) => "Explodes within 1 cell on the target's ground or air layer.",
        (TowerKind.Tesla, false) => "Hits two extra targets within 1.5 cells for 50% and 25% damage.",
        (TowerKind.Flame, false) => "60-degree cone against ground and rail. Tank armor reduces damage.",
        (TowerKind.Mortar, false) => "Fires from 1.25–3.5 cells; explodes at the marked position within 1.5 cells.",
        (TowerKind.Laser, false) => "Pierces every compatible target in a 0.35-cell-wide line.",
        _ => "No damage; 45/60/70% slowing for 2.5 seconds."
    };
    private static ContentPack Load()
    {
        using var stream = typeof(Catalog).Assembly.GetManifestResourceStream("TowerGame.Content.campaign.json")
            ?? throw new InvalidDataException("Campaign content is missing.");
        var pack = JsonSerializer.Deserialize<ContentPack>(stream, JsonOptions)
            ?? throw new InvalidDataException("Campaign content is invalid.");
        var errors = ContentValidator.Validate(pack);
        if (errors.Count != 0) throw new InvalidDataException(string.Join("\n", errors));
        return pack;
    }
}
