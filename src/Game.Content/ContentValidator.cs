using TowerGame.Core;

namespace TowerGame.Content;

public static class ContentValidator
{
    public static List<string> Validate(ContentPack pack)
    {
        List<string> errors = [];
        if (pack.FormatVersion != 1) errors.Add("Unsupported content format.");
        if (pack.Maps.Count != 8) errors.Add("Exactly eight maps are required.");
        if (pack.Maps.Select(m => m.Id).Distinct().Count() != pack.Maps.Count) errors.Add("Map IDs must be unique.");
        foreach (var map in pack.Maps) errors.AddRange(ValidateMap(map));
        var specials = pack.Maps.SelectMany(m => m.SpecialTowers).ToHashSet();
        foreach (var kind in new[] { TowerKind.Flame, TowerKind.Mortar, TowerKind.Laser, TowerKind.Ice })
            if (!specials.Contains(kind)) errors.Add($"Special tower {kind} is never available.");
        var enemies = pack.Maps.SelectMany(m => m.Waves).SelectMany(w => w.Groups).Select(g => g.Kind).ToHashSet();
        foreach (var kind in Enum.GetValues<EnemyKind>())
            if (!enemies.Contains(kind)) errors.Add($"Enemy {kind} is absent from campaign.");
        return errors;
    }

    public static List<string> ValidateMap(MapDefinition map)
    {
        List<string> errors = [];
        void Error(string message) => errors.Add($"{map.Id}: {message}");
        if (map.Width is < 4 or > 64 || map.Height is < 4 or > 64 || map.Cells.Length != map.Width * map.Height)
        { Error("Invalid dimensions or cell count."); return errors; }
        if (map.Width != 16 || map.Height != 12) Error("Campaign battlefields must be exactly 16 columns by 12 rows.");
        if (!double.IsFinite(map.HealthScale) || map.HealthScale is < .1 or > 5) Error("Invalid campaign/Endless health scale.");
        if (map.Cells.Any(c => !Enum.IsDefined(c))) Error("Unknown cell type.");
        if (map.SpecialTowers.Count != 2 || map.SpecialTowers.Distinct().Count() != 2 ||
            map.SpecialTowers.Any(t => t < TowerKind.Flame || !Enum.IsDefined(t))) Error("Exactly two distinct special towers required.");
        if (!map.Routes.Any(r => r.Movement == MovementKind.Ground)) Error("No ground route.");
        if (map.Routes.Select(r => r.Id).Distinct().Count() != map.Routes.Count) Error("Duplicate route IDs.");
        foreach (var route in map.Routes)
        {
            if (route.Points.Count < 2 || route.Points.Any(p => !map.Contains(p.X, p.Y)))
            { Error($"Invalid route {route.Id}."); continue; }
            if (route.Points[0] == route.Points[^1]) Error($"Zero-length route {route.Id}.");
            if (route.Movement == MovementKind.Ground)
            {
                var start = route.Points[0];
                var end = route.Points[^1];
                if (map.At(start.X, start.Y) != CellType.Entrance || map.At(end.X, end.Y) != CellType.Exit)
                    Error($"Ground endpoint cells invalid: {route.Id}.");
                if (Pathfinding.Distances(map, end)[start.Y * map.Width + start.X] < 0)
                    Error($"Ground route blocked: {route.Id}.");
            }
            else if (route.Movement == MovementKind.Rail)
            {
                if (route.Points.Any(p => map.At(p.X, p.Y) != CellType.Rail))
                    Error($"Rail cells missing: {route.Id}.");
                for (var i = 1; i < route.Points.Count; i++)
                    if (Math.Abs(route.Points[i].X - route.Points[i - 1].X) +
                        Math.Abs(route.Points[i].Y - route.Points[i - 1].Y) != 1)
                        Error($"Disconnected rail: {route.Id}.");
            }
            else if (route.Movement == MovementKind.Air)
            {
                for (var i = 1; i < route.Points.Count; i++)
                    if (route.Points[i] == route.Points[i - 1]) Error($"Duplicate air waypoint: {route.Id}.");
            }
            else Error("Unknown movement kind.");
        }
        if (map.Waves.Count is < 1 or > 100) Error("Campaign must contain between 1 and 100 waves.");
        for (int index = 0; index < map.Waves.Count; index++)
        {
            var wave = map.Waves[index];
            if (wave.Number != index + 1) Error("Nonconsecutive wave number.");
            var expected = Math.Min(60, 8 + (int)Math.Floor(.6 * wave.Number));
            if (wave.Groups.Sum(g => g.Count) != expected) Error($"Wave {wave.Number} count is not {expected}.");
            if (wave.Reward is < 0 or > 100000) Error($"Wave {wave.Number} reward invalid.");
            if (wave.HasBoss != (wave.Number % 10 == 0)) Error($"Wave {wave.Number} boss schedule invalid.");
            if (wave.Groups.Where(g => g.Kind >= EnemyKind.HeavyTank).Sum(g => g.Count) > 1)
                Error($"Wave {wave.Number} contains multiple bosses.");
            foreach (var group in wave.Groups)
            {
                var route = map.Routes.FirstOrDefault(r => r.Id == group.RouteId);
                if (!Enum.IsDefined(group.Kind) || route == null || route.Movement != Balance.Enemy(group.Kind).Movement)
                    Error($"Wave {wave.Number} has incompatible enemy and route.");
                if (group.Count is < 1 or > 60 || !double.IsFinite(group.Delay) || group.Delay < 0 ||
                    !double.IsFinite(group.Interval) || group.Interval < .22 ||
                    !double.IsFinite(group.HealthMultiplier) || group.HealthMultiplier <= 0)
                    Error($"Wave {wave.Number} has invalid values.");
                var available = new[] { TowerKind.Gatling, TowerKind.Goo, TowerKind.Missile, TowerKind.Tesla }.Concat(map.SpecialTowers);
                if (!available.Any(t => Balance.CanTarget(t, group.Kind))) Error($"No counter to {group.Kind}.");
            }
        }
        return errors;
    }
}
