namespace TowerGame.Core;

/// <summary>Ground units walk along the outside approach before joining the board's flow field.</summary>
public static class EntryApproach
{
    public const double Cells = 3;

    public static Cell Outward(MapDefinition map, Cell entry) => entry.X == 0 ? new(-1, 0)
        : entry.X == map.Width - 1 ? new(1, 0)
        : entry.Y == 0 ? new(0, -1)
        : entry.Y == map.Height - 1 ? new(0, 1) : new(0, 0);

    internal static bool ValidPosition(MapDefinition map, RouteDefinition route, Enemy enemy)
    {
        if (!enemy.Entering) return double.IsFinite(enemy.X) && double.IsFinite(enemy.Y)
            && enemy.X >= 0 && enemy.X <= map.Width - 1 && enemy.Y >= 0 && enemy.Y <= map.Height - 1;
        var entry = route.Points[0]; var outward = Outward(map, entry);
        if (route.Movement != MovementKind.Ground || outward == new Cell(0, 0)
            || enemy.From != entry || enemy.To != entry || enemy.SegmentProgress != 0 || enemy.Waypoint != 1) return false;
        var dx = enemy.X - entry.X; var dy = enemy.Y - entry.Y;
        var distance = dx * outward.X + dy * outward.Y;
        var cross = dx * outward.Y - dy * outward.X;
        return double.IsFinite(distance) && double.IsFinite(cross)
            && distance > 0 && distance <= Cells + .000001 && Math.Abs(cross) < .000001;
    }
}
