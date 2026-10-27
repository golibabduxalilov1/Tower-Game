namespace TowerGame.Core;

/// <summary>
/// Guide-based tower DPS/radii, with eight game coins per guide dollar so the familiar
/// 40-coin Gatling and existing saves retain their currency scale. Enemy HP, timing,
/// resistance and campaign pressure are authored Qalqon values, not claimed FR2 data.
/// </summary>
public static class Balance
{
    public const int CoinsPerGuideDollar = 8;
    public const int Revision = 1;

    public static TowerStats Tower(TowerKind kind, int level = 1)
    {
        level = Math.Clamp(level, 1, 3);
        var stats = kind switch
        {
            TowerKind.Gatling => new TowerStats(5 * CoinsPerGuideDollar, 35, .25, 2.5),
            TowerKind.Goo => new(10 * CoinsPerGuideDollar, 0, .8, 2.5),
            TowerKind.Missile => new(20 * CoinsPerGuideDollar, 75, 1.5, 3.5),
            TowerKind.Tesla => new(70 * CoinsPerGuideDollar, 700, 1, 2.5),
            TowerKind.Flame => new(50 * CoinsPerGuideDollar, 300, .2, 2.5),
            // The existing mortar uses the guide's Cannon progression, retaining a short dead zone.
            TowerKind.Mortar => new(30 * CoinsPerGuideDollar, 100, 2.5, 3.5, 1.25),
            TowerKind.Laser => new(40 * CoinsPerGuideDollar, 125, .8, 2.5),
            _ => new(15 * CoinsPerGuideDollar, 0, 1, 1.5)
        };
        double dps = kind == TowerKind.Laser && level == 3 ? 450 : stats.Damage * level;
        // DPS is per second, not damage per shot. Upgrading never silently expands the radius.
        return stats with { Damage = dps * stats.Interval };
    }

    public static EnemyStats Enemy(EnemyKind kind) => kind switch
    {
        EnemyKind.Infantry => new(120, 1.2, 8, MovementKind.Ground),
        EnemyKind.Runner => new(90, 2.1, 8, MovementKind.Ground),
        EnemyKind.Heavy => new(360, .8, 16, MovementKind.Ground),
        EnemyKind.Motorcycle => new(240, 2.5, 16, MovementKind.Ground),
        EnemyKind.Tank => new(1400, .55, 40, MovementKind.Ground),
        EnemyKind.Helicopter => new(220, 1.9, 16, MovementKind.Air),
        EnemyKind.Plane => new(160, 3.1, 16, MovementKind.Air),
        EnemyKind.RailCart => new(360, 1.4, 16, MovementKind.Rail),
        EnemyKind.HeavyTank => new(4000, .45, 80, MovementKind.Ground, true),
        EnemyKind.AirFortress => new(4000, .7, 80, MovementKind.Air, true),
        _ => new(6000, .6, 80, MovementKind.Rail, true)
    };

    public static int UpgradeCost(Tower tower) => tower.Kind == TowerKind.Tesla
        ? 50 * CoinsPerGuideDollar
        : (int)Math.Floor(Tower(tower.Kind).Cost * (tower.Level == 1 ? .75 : 1.25));

    public static int InvestmentCost(TowerKind kind, int level) => Tower(kind).Cost
        + (level >= 2 ? UpgradeCost(new Tower { Kind = kind, Level = 1 }) : 0)
        + (level >= 3 ? UpgradeCost(new Tower { Kind = kind, Level = 2 }) : 0);

    internal static int LegacyInvestmentCost(TowerKind kind, int level)
    {
        int price = kind switch { TowerKind.Gatling => 40, TowerKind.Goo => 55, TowerKind.Missile => 100,
            TowerKind.Tesla => 150, TowerKind.Flame => 120, TowerKind.Mortar => 140, TowerKind.Laser => 170, _ => 70 };
        return price + (level >= 2 ? (int)Math.Floor(price * .75) : 0) + (level >= 3 ? (int)Math.Floor(price * 1.25) : 0);
    }

    internal static void MigrateLegacy(SessionSnapshot snapshot)
    {
        if (snapshot.BalanceRevision != 0) return;
        foreach (var tower in snapshot.Towers) tower.Spent = InvestmentCost(tower.Kind, tower.Level);
        foreach (var enemy in snapshot.Enemies)
        {
            double previous = enemy.Kind switch { EnemyKind.Infantry => 40, EnemyKind.Runner => 28,
                EnemyKind.Heavy => 110, EnemyKind.Motorcycle => 70, EnemyKind.Tank => 380,
                EnemyKind.Helicopter => 140, EnemyKind.Plane => 95, EnemyKind.RailCart => 220,
                EnemyKind.HeavyTank => 1500, EnemyKind.AirFortress => 1200, _ => 1800 };
            var factor = Enemy(enemy.Kind).Health / previous;
            var fraction = enemy.Health / enemy.MaxHealth;
            enemy.MaxHealth = Math.Min(1e15, enemy.MaxHealth * factor);
            enemy.Health = enemy.MaxHealth * fraction;
        }
        snapshot.BalanceRevision = Revision;
    }

    public static double SlowStrength(TowerKind kind, int level) => (kind, Math.Clamp(level, 1, 3)) switch
    {
        (TowerKind.Goo, 1) => .40, (TowerKind.Goo, 2) => .60, (TowerKind.Goo, _) => .75,
        (TowerKind.Ice, 1) => .45, (TowerKind.Ice, 2) => .60, (TowerKind.Ice, _) => .70,
        _ => 0
    };

    public static double SlowEffectiveness(EnemyKind kind) => kind switch
    {
        EnemyKind.RailColossus => 0,
        EnemyKind.Runner => .5,
        EnemyKind.Motorcycle => .75,
        _ => Enemy(kind).Boss ? .5 : 1
    };

    public static double DamageMultiplier(EnemyKind enemy, TowerKind tower) => (enemy, tower) switch
    {
        (EnemyKind.Heavy, TowerKind.Gatling) => .70,
        (EnemyKind.Tank, TowerKind.Gatling) => .35,
        (EnemyKind.HeavyTank, TowerKind.Gatling) => .25,
        (EnemyKind.RailCart, TowerKind.Gatling) => .70,
        (EnemyKind.RailColossus, TowerKind.Gatling) => .30,
        (EnemyKind.Tank, TowerKind.Flame) => .55,
        (EnemyKind.HeavyTank, TowerKind.Flame) => .45,
        (EnemyKind.RailColossus, TowerKind.Flame) => .65,
        (EnemyKind.Tank or EnemyKind.HeavyTank, TowerKind.Laser) => .85,
        (EnemyKind.Tank or EnemyKind.HeavyTank, TowerKind.Missile) => 1.25,
        _ => 1
    };

    // Four-unit pushes with a short recovery gap keep AoE and slowing tactically relevant.
    public static double SpawnTime(int index, double interval) =>
        index / 4 * (interval * 4 + .45) + index % 4 * interval * .48;

    public static bool CanTarget(TowerKind tower, EnemyKind enemy)
    {
        var movement = Enemy(enemy).Movement;
        return tower switch
        {
            TowerKind.Goo or TowerKind.Ice => movement != MovementKind.Rail,
            TowerKind.Flame or TowerKind.Mortar => movement != MovementKind.Air,
            _ => true
        };
    }
}
