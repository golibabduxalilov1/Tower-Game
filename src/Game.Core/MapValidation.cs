namespace TowerGame.Core;

/// <summary>Checks external content before it can enter the simulation.</summary>
public static class MapValidation
{
    public static void Validate(MapDefinition map)
    {
        static void Need([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition)
        {
            if (!condition) throw new InvalidDataException("Invalid map content");
        }
        Need(map is not null);
        Need(!string.IsNullOrWhiteSpace(map.Id) && map.Id.Length <= 64);
        Need(map.Width is >= 2 and <= 128 && map.Height is >= 2 and <= 128);
        Need(double.IsFinite(map.HealthScale) && map.HealthScale is >= .1 and <= 5);
        Need(map.Cells is not null && map.Cells.Length == map.Width * map.Height);
        Need(map.Cells.All(Enum.IsDefined));
        Need(map.Routes is not null && map.Routes.Count is >= 1 and <= 32);
        Need(map.Routes.Select(r => r?.Id).Distinct().Count() == map.Routes.Count);
        foreach (var route in map.Routes)
        {
            Need(route is not null && !string.IsNullOrWhiteSpace(route.Id) && route.Id.Length <= 64);
            Need(Enum.IsDefined(route.Movement) && route.Points is not null && route.Points.Count is >= 2 and <= 16384);
            Need(route.Points.All(p => map.Contains(p.X, p.Y)));
            Need(route.Points.Zip(route.Points.Skip(1)).All(pair => pair.First != pair.Second));
            if (route.Movement == MovementKind.Ground)
            {
                var distances = Pathfinding.Distances(map, route.Points[^1]);
                Need(distances[route.Points[0].Y * map.Width + route.Points[0].X] >= 0);
            }
            else if (route.Movement == MovementKind.Rail)
            {
                Need(route.Points.Zip(route.Points.Skip(1)).All(pair =>
                    Math.Abs(pair.First.X - pair.Second.X) + Math.Abs(pair.First.Y - pair.Second.Y) == 1));
                Need(route.Points.All(p => map.At(p.X, p.Y) is CellType.Rail or CellType.Entrance or CellType.Exit));
            }
        }
        Need(map.SpecialTowers is not null && map.SpecialTowers.Count <= 4 && map.SpecialTowers.All(Enum.IsDefined));
        Need(map.Waves is not null && map.Waves.Count <= 100);
        for (var i = 0; i < map.Waves.Count; i++)
        {
            var wave = map.Waves[i];
            Need(wave is not null && wave.Number == i + 1 && wave.Reward is >= 0 and <= 100000);
            Need(wave.Groups is not null && wave.Groups.Count is >= 1 and <= 60);
            Need(wave.Groups.Sum(g => (long)(g?.Count ?? 0)) is >= 1 and <= 60);
            foreach (var group in wave.Groups)
            {
                Need(group is not null && Enum.IsDefined(group.Kind) && group.Count is >= 1 and <= 60);
                Need(map.Routes.Any(r => r.Id == group.RouteId && r.Movement == Balance.Enemy(group.Kind).Movement));
                Need(double.IsFinite(group.Delay) && group.Delay is >= 0 and <= 3600);
                Need(double.IsFinite(group.Interval) && group.Interval is >= 0 and <= 60);
                Need(double.IsFinite(group.HealthMultiplier) && group.HealthMultiplier is >= .001 and <= 1e12);
            }
        }
    }
}
