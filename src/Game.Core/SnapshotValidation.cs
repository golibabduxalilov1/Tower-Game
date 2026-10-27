namespace TowerGame.Core;

internal static class SnapshotValidation
{
    public static T Clone<T>(T value) => System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(value))!;
    public static void Validate(MapDefinition map, SessionSnapshot s)
    {
        static void Need(bool condition) { if (!condition) throw new InvalidDataException("Invalid saved battle"); }
        static bool Finite(double value, double min, double max) => double.IsFinite(value) && value >= min && value <= max;
        Need(s != null && s.MapId == map.Id && Enum.IsDefined(s.Mode) && Enum.IsDefined(s.Difficulty) && Enum.IsDefined(s.State) && Enum.IsDefined(s.ResumeState));
        Need(s!.BalanceRevision is 0 or Balance.Revision);
        // The compact-board redesign changes route coordinates. Never silently relocate a
        // legacy 24-column battle onto the new 11-column board; the UI offers recovery.
        Need(s!.MapWidth == map.Width && s.MapHeight == map.Height);
        Need(s!.Money >= 0 && s.Money <= 1000000000 && s.Lives >= 0 && s.Lives <= 20 && s.Wave >= 0 && s.Wave <= 1000000 && s.Score >= 0 && s.Score <= long.MaxValue - 1000000 && s.Tick >= 0 && s.Tick < long.MaxValue - 1000000 && s.NextId > 0 && s.NextId < int.MaxValue - 1000000);
        Need(Finite(s.WaveTime, 0, 1e9) && Finite(s.BreakTime, -1, 10) && s.Towers != null && s.Enemies != null && s.Projectiles != null && s.Pending != null);
        Need(s.Towers!.Count <= map.Cells.Length && s.Enemies!.Count <= 300 && s.Projectiles!.Count <= 600 && s.Pending!.Count <= 1000);
        var ids = new HashSet<int>(); var cells = new HashSet<Cell>();
        foreach (var t in s.Towers) { Need(t != null); Need(t!.Id > 0 && t.Id < s.NextId && ids.Add(t.Id) && map.Contains(t.X, t.Y) && map.At(t.X, t.Y) is CellType.Ground or CellType.Platform && cells.Add(new(t.X, t.Y)) && Enum.IsDefined(t.Kind) && Enum.IsDefined(t.Priority) && t.Level >= 1 && t.Level <= 3 && t.Spent >= 0 && t.Spent <= 10000 && Finite(t.Cooldown, -1, 100) && Finite(t.AimX, -EntryApproach.Cells, map.Width - 1 + EntryApproach.Cells) && Finite(t.AimY, -EntryApproach.Cells, map.Height - 1 + EntryApproach.Cells)); }
        foreach (var e in s.Enemies!)
        {
            Need(e != null);
            var route = map.Routes.Find(r => r.Id == e!.RouteId);
            Need(route != null && Enum.IsDefined(e!.Kind));
            Need(route!.Movement == Balance.Enemy(e!.Kind).Movement && e.Id > 0 && e.Id < s.NextId
                && ids.Add(e.Id) && map.Contains(e.From.X, e.From.Y) && map.Contains(e.To.X, e.To.Y)
                && EntryApproach.ValidPosition(map, route, e) && Finite(e.Health, 0, 1e15)
                && Finite(e.MaxHealth, .001, 1e15) && e.Health <= e.MaxHealth
                && Finite(e.SegmentProgress, 0, 1000) && e.Slows != null && e.Slows.Count <= 32);
            if (!e.Entering)
            {
                if (route.Movement != MovementKind.Ground)
                    Need(e.Waypoint >= 1 && e.Waypoint < route.Points.Count && e.To == route.Points[e.Waypoint]);
                else Need(Math.Abs(e.To.X - e.From.X) + Math.Abs(e.To.Y - e.From.Y) <= 1);
                var cross = (e.X - e.From.X) * (e.To.Y - e.From.Y) - (e.Y - e.From.Y) * (e.To.X - e.From.X);
                Need(Math.Abs(cross) < .001 && e.X >= Math.Min(e.From.X, e.To.X) - .001
                    && e.X <= Math.Max(e.From.X, e.To.X) + .001 && e.Y >= Math.Min(e.From.Y, e.To.Y) - .001
                    && e.Y <= Math.Max(e.From.Y, e.To.Y) + .001);
            }
            foreach (var slow in e.Slows!)
                Need(slow != null && Finite(slow.Strength, 0, .99) && Finite(slow.Remaining, 0, 60));
        }
        foreach (var p in s.Projectiles!) { Need(p != null); Need(p!.Id > 0 && p.Id < s.NextId && ids.Add(p.Id) && p.Kind is TowerKind.Missile or TowerKind.Mortar && Finite(p.X, -EntryApproach.Cells, map.Width - 1 + EntryApproach.Cells) && Finite(p.Y, -EntryApproach.Cells, map.Height - 1 + EntryApproach.Cells) && Finite(p.TargetX, -EntryApproach.Cells, map.Width - 1 + EntryApproach.Cells) && Finite(p.TargetY, -EntryApproach.Cells, map.Height - 1 + EntryApproach.Cells) && Finite(p.Damage, 0, 1e6) && Finite(p.Remaining, 0, 10)); }
        foreach (var p in s.Pending!) { Need(p != null); Need(Enum.IsDefined(p!.Kind) && map.Routes.Any(r => r.Id == p.RouteId && r.Movement == Balance.Enemy(p.Kind).Movement) && Finite(p.Time, 0, 1e6) && Finite(p.HealthMultiplier, .001, 1e12)); }
        Need(s.Pending.Select(p => p.Time).SequenceEqual(s.Pending.Select(p => p.Time).Order()));
        Need(s.Lives > 0 || s.State == SessionState.Defeat); Need(s.State != SessionState.Victory || (s.Wave >= map.Waves.Count && s.Lives > 0 && s.Mode != Mode.Endless));
        Need(s.State != SessionState.Defeat || s.Lives == 0);
        Need(s.State != SessionState.Paused || s.ResumeState is SessionState.Preparation or SessionState.Active or SessionState.Intermission);
        var effective = s.State == SessionState.Paused ? s.ResumeState : s.State;
        Need(effective != SessionState.Preparation || (s.Wave == 0 && s.Enemies.Count == 0 && s.Pending.Count == 0));
        Need(effective is not (SessionState.Active or SessionState.Intermission) || s.Wave > 0);
        Need(effective is not (SessionState.Intermission or SessionState.Victory) || (s.Enemies.Count == 0 && s.Pending.Count == 0));
        var allowed = s.Mode == Mode.Classic
            ? new[] { TowerKind.Gatling, TowerKind.Goo, TowerKind.Missile, TowerKind.Tesla }
            : new[] { TowerKind.Gatling, TowerKind.Goo, TowerKind.Missile, TowerKind.Tesla }.Concat(map.SpecialTowers).ToArray();
        foreach (var tower in s.Towers)
        {
            var expected = s.BalanceRevision == 0 ? Balance.LegacyInvestmentCost(tower.Kind, tower.Level)
                : Balance.InvestmentCost(tower.Kind, tower.Level);
            Need(allowed.Contains(tower.Kind) && tower.Spent == expected && tower.TargetId >= 0);
        }
    }
}
