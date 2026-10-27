namespace TowerGame.Core;

/// <summary>
/// Deterministic heuristic player used for the attract/demo mode, soak runs and balance reports.
/// It only uses the public commands a human has (build, upgrade, start wave), so every result it
/// reaches is reachable by a player.
/// </summary>
public sealed class AutoPlayer
{
    readonly GameSession s;
    readonly List<(double X, double Y, int Route)> threat = [];
    double[] need = []; bool uncovered;
    long threatTick = -1; int builds;
    public AutoPlayer(GameSession session) => s = session;

    /// <summary>Spends money: builds while useful positions remain, otherwise upgrades.</summary>
    public void Act(int maxActions = 8)
    {
        if (s.State is SessionState.Victory or SessionState.Defeat or SessionState.Paused) return;
        for (int i = 0; i < maxActions; i++)
        {
            var wantMore = s.Towers.Count < 10 + s.Wave / 2;
            if (wantMore && TryBuild()) continue;
            RefreshThreat();
            if (uncovered) break; // Save for a tower on the exposed route instead of upgrading.
            if (TryUpgrade()) continue;
            if (!wantMore && TryBuild()) continue;
            // Out of space with idle money: replace a slow-only tower with a damage dealer.
            if (s.Money >= 600 && s.Towers.All(t => t.Level == 3 || Balance.UpgradeCost(t) > s.Money)
                && s.Towers.FirstOrDefault(t => t.Kind == TowerKind.Goo) is { } goo && s.Sell(goo.Id))
            { threatTick = -1; if (TryBuild(TowerKind.Tesla)) continue; }
            break;
        }
    }

    /// <summary>Runs one complete wave (starting it if needed) and returns false when the battle ended.</summary>
    public bool PlayWave(int maxSteps = 200000)
    {
        Act();
        if (s.State is SessionState.Preparation or SessionState.Intermission) s.StartWave();
        var wave = s.Wave;
        for (int step = 0; step < maxSteps && s.State == SessionState.Active && s.Wave == wave; step++)
        {
            s.Step();
            if (step % 120 == 0) Act(2);
        }
        return s.State is not (SessionState.Victory or SessionState.Defeat);
    }

    TowerKind NextKind()
    {
        var specials = s.AvailableTowers.Where(t => t >= TowerKind.Flame).ToArray();
        if (builds < 3) return TowerKind.Gatling;
        // Slowing specials (Ice) add no damage; the heuristic only rotates damage dealers in.
        var damage = specials.Where(k => k != TowerKind.Ice).ToArray();
        TowerKind[] cycle = damage.Length == 0
            ? [TowerKind.Missile, TowerKind.Tesla, TowerKind.Gatling, TowerKind.Goo, TowerKind.Tesla, TowerKind.Missile]
            : [TowerKind.Missile, damage[0], TowerKind.Tesla, TowerKind.Gatling, damage[^1], TowerKind.Tesla, TowerKind.Missile, TowerKind.Goo];
        return cycle[builds % cycle.Length];
    }

    void RefreshThreat()
    {
        if (threatTick == s.Tick && threat.Count > 0) return;
        threatTick = s.Tick; threat.Clear();
        var routes = s.Map.Routes;
        for (int r = 0; r < routes.Count; r++)
        {
            var points = s.PreviewPath(routes[r].Id);
            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i - 1]; var b = points[i];
                var length = Math.Max(1, Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y));
                for (int k = 0; k < length; k++)
                {
                    var t = (double)k / length;
                    threat.Add((a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, r));
                }
            }
        }
        // Traffic share of the coming wave per route, against firepower already covering it.
        var wave = s.State == SessionState.Active ? s.GetWave(Math.Max(1, s.Wave)) : s.NextWave;
        var traffic = new double[routes.Count]; var counts = new double[routes.Count];
        var speed = new double[routes.Count]; var total = 0.0;
        foreach (var g in wave.Groups)
        {
            var index = routes.FindIndex(r => r.Id == g.RouteId);
            var stats = Balance.Enemy(g.Kind);
            var hp = stats.Health * g.HealthMultiplier * g.Count;
            traffic[index] += hp; counts[index] += g.Count; speed[index] += stats.Speed * g.Count; total += hp;
        }
        var cover = new double[routes.Count];
        foreach (var t in s.Towers)
        {
            var stats = Balance.Tower(t.Kind, t.Level); var dps = Math.Max(4, stats.Damage) / stats.Interval;
            var per = Coverage(t.X, t.Y, stats, t.Kind);
            for (int r = 0; r < routes.Count; r++) cover[r] += per[r] * dps;
        }
        need = new double[routes.Count]; uncovered = false;
        for (int r = 0; r < routes.Count; r++)
        {
            var pressure = traffic[r] / Math.Max(1, counts[r]) * speed[r] / Math.Max(1, counts[r]) * 1.25;
            need[r] = (.15 + routes.Count * traffic[r] / Math.Max(1, total)) / (1 + cover[r] / Math.Max(100, pressure));
            if (traffic[r] > 0 && cover[r] < pressure) uncovered = true;
        }
    }

    double[] Coverage(int x, int y, TowerStats stats, TowerKind kind)
    {
        var sum = new double[s.Map.Routes.Count];
        foreach (var (px, py, r) in threat)
        {
            var movement = s.Map.Routes[r].Movement;
            var hits = kind switch
            {
                TowerKind.Goo or TowerKind.Ice => movement != MovementKind.Rail,
                TowerKind.Flame or TowerKind.Mortar => movement != MovementKind.Air,
                _ => true
            };
            if (!hits) continue;
            var d = Math.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
            if (d <= stats.Range && d >= stats.MinRange) sum[r] += 1;
        }
        return sum;
    }

    double Value(int x, int y, TowerStats stats, TowerKind kind)
    {
        var per = Coverage(x, y, stats, kind); double v = 0;
        for (int r = 0; r < per.Length; r++) v += per[r] * need[r];
        return v;
    }
    int GroundLength(Cell? extra)
    {
        var total = 0;
        foreach (var r in s.Map.Routes.Where(r => r.Movement == MovementKind.Ground))
        {
            var d = Pathfinding.Distances(s.Map, r.Points[^1], s.Towers, extra);
            total += Math.Max(0, d[r.Points[0].Y * s.Map.Width + r.Points[0].X]);
        }
        return total;
    }

    bool TryBuild(TowerKind? forced = null)
    {
        var kind = forced ?? NextKind();
        if (!s.AvailableTowers.Contains(kind)) kind = TowerKind.Gatling;
        // Early on, unaffordable plans fall back to a cheap gun instead of stalling the defence.
        RefreshThreat();
        if (s.Money < Balance.Tower(kind).Cost)
        {
            // New guide-priced specialist towers need a deliberate affordable counter,
            // rather than either stalling on Tesla or endlessly adding weak machine guns.
            if (s.Money >= Balance.Tower(TowerKind.Missile).Cost) kind = TowerKind.Missile;
            else if (uncovered || s.Towers.Count < 6 + s.Wave / 3) kind = TowerKind.Gatling;
        }
        var stats = Balance.Tower(kind);
        if (s.Money < stats.Cost) return false;
        RefreshThreat();
        var baseLength = GroundLength(null);
        double best = 0; Cell? bestCell = null;
        for (int y = 0; y < s.Map.Height; y++)
            for (int x = 0; x < s.Map.Width; x++)
            {
                if (s.Map.At(x, y) is not (CellType.Ground or CellType.Platform)) continue;
                var near = false;
                foreach (var (px, py, _) in threat) if (Math.Abs(px - x) <= 3 && Math.Abs(py - y) <= 3) { near = true; break; }
                if (!near) continue;
                var per = Coverage(x, y, stats, kind);
                if (per.Sum() < 2) continue;
                double score = 0;
                for (int r = 0; r < per.Length; r++) score += per[r] * need[r];
                if (!s.CanBuild(x, y, kind, out _)) continue;
                // Lengthening the ground route multiplies every tower's time on target.
                score += .8 * (GroundLength(new Cell(x, y)) - baseLength);
                score -= ((x * 7 + y * 13) % 5) * .01; // deterministic tie breaking
                if (score > best) { best = score; bestCell = new Cell(x, y); }
            }
        if (bestCell is not { } c || !s.TryBuild(c.X, c.Y, kind, out _)) return false;
        builds++; threatTick = -1;
        return true;
    }

    bool TryUpgrade()
    {
        RefreshThreat();
        Tower? best = null; double bestValue = 0;
        foreach (var t in s.Towers)
        {
            if (t.Level >= 3) continue;
            var cost = Balance.UpgradeCost(t);
            if (cost > s.Money) continue;
            var current = Balance.Tower(t.Kind, t.Level);
            var upgraded = Balance.Tower(t.Kind, t.Level + 1);
            var gain = t.Kind is TowerKind.Goo or TowerKind.Ice
                ? (Balance.SlowStrength(t.Kind, t.Level + 1) - Balance.SlowStrength(t.Kind, t.Level)) * 100
                : upgraded.Damage / upgraded.Interval - current.Damage / current.Interval;
            var value = Value(t.X, t.Y, current, t.Kind) * gain / cost;
            if (value > bestValue) { bestValue = value; best = t; }
        }
        return best is not null && s.Upgrade(best.Id, out _);
    }
}
