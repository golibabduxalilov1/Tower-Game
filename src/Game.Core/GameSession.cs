namespace TowerGame.Core;

public static class Pathfinding
{
    static readonly Cell[] Directions = [new(1, 0), new(0, 1), new(-1, 0), new(0, -1)];
    public static bool Walkable(MapDefinition map, int x, int y, IReadOnlyList<Tower>? towers = null, Cell? blocked = null) => map.Contains(x, y) && map.At(x, y) is CellType.Ground or CellType.Path or CellType.Entrance or CellType.Exit && blocked != new Cell(x, y) && !(towers?.Any(t => t.X == x && t.Y == y) ?? false);
    public static int[] Distances(MapDefinition map, Cell exit, IReadOnlyList<Tower>? towers = null, Cell? blocked = null) { var d = Enumerable.Repeat(-1, map.Width * map.Height).ToArray(); if (!Walkable(map, exit.X, exit.Y, towers, blocked)) return d; var q = new Queue<Cell>(); q.Enqueue(exit); d[exit.Y * map.Width + exit.X] = 0; while (q.TryDequeue(out var p)) foreach (var dir in Directions) { var n = new Cell(p.X + dir.X, p.Y + dir.Y); if (Walkable(map, n.X, n.Y, towers, blocked) && d[n.Y * map.Width + n.X] < 0) { d[n.Y * map.Width + n.X] = d[p.Y * map.Width + p.X] + 1; q.Enqueue(n); } } return d; }
    public static Cell Next(MapDefinition map, int[] distances, Cell p, Cell previous) { var best = p; var distance = distances[p.Y * map.Width + p.X]; var forward = new Cell(p.X + (p.X - previous.X), p.Y + (p.Y - previous.Y)); if (map.Contains(forward.X, forward.Y) && distances[forward.Y * map.Width + forward.X] >= 0 && distances[forward.Y * map.Width + forward.X] < distance) return forward; foreach (var dir in Directions) { var n = new Cell(p.X + dir.X, p.Y + dir.Y); if (map.Contains(n.X, n.Y) && distances[n.Y * map.Width + n.X] >= 0 && distances[n.Y * map.Width + n.X] < distance) { best = n; distance = distances[n.Y * map.Width + n.X]; } } return best; }
}
public sealed class GameSession
{
    public const double FixedDelta = 1.0 / 60;
    public MapDefinition Map { get; }
    public Mode Mode { get; }
    public Difficulty Difficulty { get; }
    public SessionState State { get; private set; } = SessionState.Preparation; public int Money { get; private set; }
    public int Lives { get; private set; } = 20; public int Wave { get; private set; }
    public long Score { get; private set; }
    public long Tick { get; private set; }
    public uint Seed { get; private set; }
    public double BreakTime { get; private set; } = 10; public List<Tower> Towers { get; } = []; public List<Enemy> Enemies { get; } = []; public List<Projectile> Projectiles { get; } = []; public List<CombatEffect> Effects { get; } = [];
    public IReadOnlyList<TowerKind> AvailableTowers { get; }
    public event Action? WaveCompleted;
    /// <summary>Presentation hooks; raised inside Step, never required by the simulation.</summary>
    public event Action<Tower>? TowerFired; public event Action<Enemy>? EnemyKilled; public event Action<Enemy>? EnemyEscaped; public event Action<TowerKind, double, double>? Exploded;
    public int TotalWaves => Math.Max(1, Map.Waves.Count);
    public int PendingCount => pending.Count;
    public int RemainingEnemies => pending.Count + Enemies.Count(e => !e.Dead);
    readonly List<PendingSpawn> pending = []; readonly Dictionary<string, int[]> distances = []; int nextId = 1; double waveTime; SessionState resumeState; readonly List<Enemy> candidates = new(300); readonly Stack<Enemy> enemyPool = new(); readonly Stack<Projectile> projectilePool = new(); readonly List<Enemy>[] spatial;
    public GameSession(MapDefinition map, Mode mode = Mode.Classic, Difficulty difficulty = Difficulty.Normal, uint seed = 42) { MapValidation.Validate(map); if (!Enum.IsDefined(mode) || !Enum.IsDefined(difficulty)) throw new ArgumentOutOfRangeException(nameof(mode)); Map = map; spatial = Enumerable.Range(0, map.Cells.Length).Select(_ => new List<Enemy>(4)).ToArray(); Mode = mode; Difficulty = difficulty; Seed = seed; Money = difficulty == Difficulty.Easy ? 180 : difficulty == Difficulty.Hard ? 130 : 150; AvailableTowers = mode == Mode.Classic ? [TowerKind.Gatling, TowerKind.Goo, TowerKind.Missile, TowerKind.Tesla] : new[] { TowerKind.Gatling, TowerKind.Goo, TowerKind.Missile, TowerKind.Tesla }.Concat(map.SpecialTowers).Distinct().ToArray(); Repath(); }
    readonly Dictionary<string, IReadOnlyList<Cell>> pathCache = [];
    void Repath() { pathCache.Clear(); distances.Clear(); var shared = new Dictionary<Cell, int[]>(); foreach (var r in Map.Routes.Where(r => r.Movement == MovementKind.Ground)) { if (!shared.TryGetValue(r.Points[^1], out var field)) { field = Pathfinding.Distances(Map, r.Points[^1], Towers); shared.Add(r.Points[^1], field); } distances[r.Id] = field; } }
    void IndexEnemies() { foreach (var bucket in spatial) bucket.Clear(); foreach (var e in Enemies) if (!e.Dead) { var x = Math.Clamp((int)e.X, 0, Map.Width - 1); var y = Math.Clamp((int)e.Y, 0, Map.Height - 1); spatial[y * Map.Width + x].Add(e); } }
    void Nearby(double x, double y, double radius) { candidates.Clear(); for (var yy = Math.Max(0, (int)Math.Floor(y - radius)); yy <= Math.Min(Map.Height - 1, (int)Math.Floor(y + radius)); yy++) for (var xx = Math.Max(0, (int)Math.Floor(x - radius)); xx <= Math.Min(Map.Width - 1, (int)Math.Floor(x + radius)); xx++) foreach (var e in spatial[yy * Map.Width + xx]) if (!e.Dead) candidates.Add(e); }
    void RemoveProjectile(int index) { projectilePool.Push(Projectiles[index]); Projectiles.RemoveAt(index); }
    void AddScore(long amount) { Score = Math.Min(long.MaxValue - 1000000, Score + Math.Min(amount, 1000000)); }

    /// <summary>Current route of a newly spawned unit; cached until walkability changes.</summary>
    public IReadOnlyList<Cell> PreviewPath(string routeId) { if (pathCache.TryGetValue(routeId, out var cached)) return cached; var computed = ComputePath(routeId); pathCache[routeId] = computed; return computed; }
    IReadOnlyList<Cell> ComputePath(string routeId) { var route = Map.Routes.First(r => r.Id == routeId); if (route.Movement != MovementKind.Ground) return route.Points; var path = new List<Cell> { route.Points[0] }; var p = path[0]; var prev = p; for (int i = 0; i < Map.Cells.Length && p != route.Points[^1]; i++) { var n = Pathfinding.Next(Map, distances[routeId], p, prev); if (n == p) break; prev = p; p = n; path.Add(p); } return path; }
    public bool CanBuild(int x, int y, TowerKind kind, out string error) { error = ""; if (State is SessionState.Victory or SessionState.Defeat) { error = "battle_over"; return false; } if (!AvailableTowers.Contains(kind)) { error = "tower_unavailable"; return false; } if (Map.At(x, y) is not (CellType.Ground or CellType.Platform) || Towers.Any(t => t.X == x && t.Y == y)) { error = "invalid_cell"; return false; } if (Money < Balance.Tower(kind).Cost) { error = "not_enough_money"; return false; } var cell = new Cell(x, y); if (Enemies.Any(e => !e.Dead && Balance.Enemy(e.Kind).Movement == MovementKind.Ground && (e.From == cell || e.To == cell))) { error = "enemy_occupies_cell"; return false; } foreach (var r in Map.Routes.Where(r => r.Movement == MovementKind.Ground)) { var d = Pathfinding.Distances(Map, r.Points[^1], Towers, cell); if (d[r.Points[0].Y * Map.Width + r.Points[0].X] < 0 || Enemies.Any(e => !e.Dead && e.RouteId == r.Id && d[e.To.Y * Map.Width + e.To.X] < 0)) { error = "path_blocked"; return false; } } return true; }
    public bool TryBuild(int x, int y, TowerKind kind, out string error) { if (!CanBuild(x, y, kind, out error)) return false; var cost = Balance.Tower(kind).Cost; Towers.Add(new Tower { Id = nextId++, Kind = kind, X = x, Y = y, Spent = cost, AimX = x + 1, AimY = y }); Money -= cost; Repath(); return true; }
    public bool Upgrade(int id, out string error) { error = ""; var t = Towers.Find(t => t.Id == id); if (t == null || t.Level >= 3 || State is SessionState.Victory or SessionState.Defeat) { error = "max_level"; return false; } var cost = Balance.UpgradeCost(t); if (Money < cost) { error = "not_enough_money"; return false; } Money -= cost; t.Spent += cost; t.Level++; return true; }
    public int SellValue(Tower t) => Wave == 0 ? t.Spent : (int)Math.Floor(t.Spent * .7);
    public bool Sell(int id) { var t = Towers.Find(t => t.Id == id); if (t == null || State is SessionState.Victory or SessionState.Defeat) return false; Money += SellValue(t); Towers.Remove(t); Repath(); return true; }
    public void SetPaused(bool pause) { if (pause && State is SessionState.Preparation or SessionState.Active or SessionState.Intermission) { resumeState = State; State = SessionState.Paused; } else if (!pause && State == SessionState.Paused) State = resumeState; }
    public WaveDefinition NextWave => GetWave(Wave + 1);
    WaveDefinition? cachedWave;
    /// <summary>Campaign waves come from content. Endless waves past the authored set are a pure function of (Seed, number),
    /// so the saved seed fully restores the generator state.</summary>
    public WaveDefinition GetWave(int number)
    {
        if (number <= Map.Waves.Count) return Map.Waves[number - 1];
        if (cachedWave?.Number == number) return cachedWave;
        var n = Math.Min(number, 100000); var wave = new WaveDefinition { Number = number, Reward = 0 };
        var count = Math.Min(60, 8 + (int)(.6 * n)); var interval = Math.Max(.25, .85 - .004 * n);
        var health = Math.Min(1e12, Map.HealthScale * (1 + .075 * (n - 1)) * Math.Pow(1.012, Math.Min(n - 1, 2000)));
        var routes = Map.Routes;
        for (int i = 0; i < count; i++)
        {
            var h = Mix(Seed, (uint)n, (uint)i); var boss = number % 10 == 0 && i == count / 2;
            var r = boss ? routes[(n / 10 - 1) % routes.Count] : routes[(int)(h % (uint)routes.Count)];
            var kind = r.Movement switch
            {
                MovementKind.Air => boss ? EnemyKind.AirFortress : (h >> 8) % 2 == 0 ? EnemyKind.Helicopter : EnemyKind.Plane,
                MovementKind.Rail => boss ? EnemyKind.RailColossus : EnemyKind.RailCart,
                _ => boss ? EnemyKind.HeavyTank : (EnemyKind)((h >> 8) % 5)
            };
            wave.Groups.Add(new WaveGroup { Kind = kind, Count = 1, RouteId = r.Id, Delay = Math.Round(Balance.SpawnTime(i, interval), 6), Interval = interval, HealthMultiplier = health });
        }
        return cachedWave = wave;
    }
    static uint Mix(uint a, uint b, uint c) { var x = a * 0x9E3779B1u ^ b * 0x85EBCA77u ^ c * 0xC2B2AE3Du; x ^= x >> 15; x *= 0x2C1B3C6Du; x ^= x >> 12; x *= 0x297A2D39u; x ^= x >> 15; return x; }
    public bool StartWave() { if (State is not (SessionState.Preparation or SessionState.Intermission) || (Mode != Mode.Endless && Wave >= TotalWaves)) return false; Wave++; waveTime = 0; State = SessionState.Active; foreach (var g in GetWave(Wave).Groups) for (int i = 0; i < g.Count; i++) pending.Add(new PendingSpawn { Kind = g.Kind, RouteId = g.RouteId, Time = g.Delay + i * g.Interval, HealthMultiplier = g.HealthMultiplier }); pending.Sort((a, b) => a.Time.CompareTo(b.Time)); return true; }
    public void Step() { if (State is SessionState.Paused or SessionState.Defeat or SessionState.Victory) return; Tick++; for (int i = Effects.Count - 1; i >= 0; i--) if ((Effects[i].Remaining -= FixedDelta) <= 0) Effects.RemoveAt(i); if (State == SessionState.Intermission) { BreakTime -= FixedDelta; if (BreakTime <= 0) StartWave(); return; } if (State != SessionState.Active) return; waveTime += FixedDelta; while (pending.Count > 0 && pending[0].Time <= waveTime) { Spawn(pending[0]); pending.RemoveAt(0); } foreach (var e in Enemies) { if (e.Dead) continue; for (int i = e.Slows.Count - 1; i >= 0; i--) if ((e.Slows[i].Remaining -= FixedDelta) <= 0) e.Slows.RemoveAt(i); Move(e); if (Lives <= 0) { State = SessionState.Defeat; return; } } IndexEnemies(); foreach (var t in Towers) Fire(t); for (int i = Projectiles.Count - 1; i >= 0; i--) { var p = Projectiles[i]; p.Remaining -= FixedDelta; var target = Enemies.Find(e => e.Id == p.TargetId && !e.Dead); if (p.Kind != TowerKind.Mortar && target == null) { RemoveProjectile(i); continue; } if (target != null && p.Kind != TowerKind.Mortar) { p.TargetX = target.X; p.TargetY = target.Y; } var f = Math.Min(1, FixedDelta / Math.Max(FixedDelta, p.Remaining)); p.X += (p.TargetX - p.X) * f; p.Y += (p.TargetY - p.Y) * f; if (p.Remaining <= 0) { foreach (var e in Enemies) if (!e.Dead && (Balance.Enemy(e.Kind).Movement == MovementKind.Air) == p.Air && Dist(e.X, e.Y, p.TargetX, p.TargetY) <= (p.Kind == TowerKind.Mortar ? 1.5 : 1)) Damage(e, p.Damage, p.Kind); AddEffect(p.Kind, p.TargetX, p.TargetY, p.TargetX, p.TargetY, .35); Exploded?.Invoke(p.Kind, p.TargetX, p.TargetY); RemoveProjectile(i); } } for (int i = Enemies.Count - 1; i >= 0; i--) if (Enemies[i].Dead) { enemyPool.Push(Enemies[i]); Enemies.RemoveAt(i); } if (pending.Count == 0 && Enemies.Count == 0) {
            // A finished wave must not leave shells frozen throughout the intermission/victory.
            while (Projectiles.Count > 0) RemoveProjectile(Projectiles.Count - 1);
            foreach (var tower in Towers) { tower.TargetId = 0; tower.AimX = tower.X + 1; tower.AimY = tower.Y; }
            Money = Math.Min(1000000000, Money + GetWave(Wave).Reward); AddScore(100); if (Wave >= TotalWaves && Mode != Mode.Endless) { AddScore(Lives * 500); State = SessionState.Victory; } else { State = SessionState.Intermission; BreakTime = 10; } WaveCompleted?.Invoke(); } }
    void Spawn(PendingSpawn spawn)
    {
        var route = Map.Routes.First(r => r.Id == spawn.RouteId);
        var entry = route.Points[0]; var stats = Balance.Enemy(spawn.Kind);
        var hp = Math.Min(1e15, stats.Health * spawn.HealthMultiplier * (Difficulty == Difficulty.Easy ? .8 : Difficulty == Difficulty.Hard ? 1.35 : 1));
        var outward = route.Movement == MovementKind.Ground ? EntryApproach.Outward(Map, entry) : new Cell(0, 0);
        var enemy = enemyPool.Count > 0 ? enemyPool.Pop() : new Enemy();
        enemy.Id = nextId++; enemy.Kind = spawn.Kind; enemy.RouteId = route.Id;
        enemy.Entering = outward != new Cell(0, 0);
        enemy.X = entry.X + outward.X * EntryApproach.Cells;
        enemy.Y = entry.Y + outward.Y * EntryApproach.Cells;
        enemy.From = entry;
        enemy.To = enemy.Entering ? entry : route.Movement == MovementKind.Ground
            ? Pathfinding.Next(Map, distances[route.Id], entry, entry) : route.Points[1];
        enemy.Health = hp; enemy.MaxHealth = hp; enemy.Waypoint = 1; enemy.SegmentProgress = 0;
        enemy.Dead = false; enemy.Slows.Clear(); Enemies.Add(enemy);
    }
    public double CurrentSpeed(Enemy e) => Balance.Enemy(e.Kind).Speed * (1 - (e.Slows.Count == 0 ? 0 : e.Slows.Max(s => s.Strength)));
    public double RemainingDistance(Enemy e) { var r = Map.Routes.First(r => r.Id == e.RouteId); var distance = Dist(e.X, e.Y, e.To.X, e.To.Y); if (r.Movement == MovementKind.Ground) return distance + Math.Max(0, distances[r.Id][e.To.Y * Map.Width + e.To.X]); for (int i = e.Waypoint; i < r.Points.Count - 1; i++) distance += Dist(r.Points[i].X, r.Points[i].Y, r.Points[i + 1].X, r.Points[i + 1].Y); return distance; }
    void Move(Enemy e) { double travel = CurrentSpeed(e) * FixedDelta; var r = Map.Routes.First(r => r.Id == e.RouteId);
        if (e.Entering)
        {
            var entry = r.Points[0]; var remaining = Dist(e.X, e.Y, entry.X, entry.Y);
            if (remaining > travel)
            {
                e.X += (entry.X - e.X) * travel / remaining;
                e.Y += (entry.Y - e.Y) * travel / remaining;
                return;
            }
            e.X = entry.X; e.Y = entry.Y; travel -= remaining; e.Entering = false;
            e.From = entry; e.To = Pathfinding.Next(Map, distances[r.Id], entry, entry); e.SegmentProgress = 0;
        }
        for (int guard = 0; guard < 8 && travel > 0; guard++) { var remaining = Dist(e.X, e.Y, e.To.X, e.To.Y); if (remaining > travel) { e.X += (e.To.X - e.X) * travel / remaining; e.Y += (e.To.Y - e.Y) * travel / remaining; e.SegmentProgress += travel; return; } e.X = e.To.X; e.Y = e.To.Y; travel -= remaining; if (e.To == r.Points[^1] && (r.Movement == MovementKind.Ground || e.Waypoint == r.Points.Count - 1)) { e.Dead = true; Lives = Math.Max(0, Lives - (Balance.Enemy(e.Kind).Boss ? 3 : 1)); EnemyEscaped?.Invoke(e); return; } var previous = e.From; e.From = e.To; e.SegmentProgress = 0; if (r.Movement == MovementKind.Ground) e.To = Pathfinding.Next(Map, distances[r.Id], e.From, previous); else e.To = r.Points[++e.Waypoint]; } }
    static double Dist(double x, double y, double xx, double yy) => Math.Sqrt((x - xx) * (x - xx) + (y - yy) * (y - yy));
    bool Valid(Tower t, Enemy e, TowerStats s) { var d = Dist(t.X, t.Y, e.X, e.Y); return !e.Dead && Balance.CanTarget(t.Kind, e.Kind) && d <= s.Range && d >= s.MinRange; }
    double PriorityValue(Tower t, Enemy e) => t.Priority switch { TargetPriority.Nearest => Dist(t.X, t.Y, e.X, e.Y), TargetPriority.Strongest => -e.Health, TargetPriority.Weakest => e.Health, TargetPriority.ExitNearest => RemainingDistance(e), _ => RemainingDistance(e) / CurrentSpeed(e) };
    void Fire(Tower t)
    {
        var stats = Balance.Tower(t.Kind, t.Level);
        var target = Enemies.Find(e => e.Id == t.TargetId && Valid(t, e, stats));
        // Track a live target between shots; do not leave a dead target locked until reload.
        if (target != null) { t.AimX = target.X; t.AimY = target.Y; }
        else { t.TargetId = 0; t.AimX = t.X + 1; t.AimY = t.Y; }
        t.Cooldown -= FixedDelta;
        if (t.Cooldown > 0) return;
        if (target == null)
        {
            double best = double.MaxValue; Nearby(t.X, t.Y, stats.Range);
            foreach (var e in candidates) if (Valid(t, e, stats))
            {
                var priority = PriorityValue(t, e);
                if (priority < best || priority == best && (target == null || e.Id < target.Id))
                { target = e; best = priority; }
            }
        }
        if (target == null) { t.Cooldown = 0; return; }
        t.TargetId = target.Id; t.AimX = target.X; t.AimY = target.Y; t.Cooldown += stats.Interval; TowerFired?.Invoke(t); AddEffect(t.Kind, t.X, t.Y, target.X, target.Y, .18);
        if (t.Kind is TowerKind.Missile or TowerKind.Mortar) { if (Projectiles.Count < 600) { var p = projectilePool.Count > 0 ? projectilePool.Pop() : new Projectile(); p.Id = nextId++; p.Kind = t.Kind; p.TargetId = target.Id; p.X = t.X; p.Y = t.Y; p.TargetX = target.X; p.TargetY = target.Y; p.Damage = stats.Damage; p.Remaining = t.Kind == TowerKind.Mortar ? .65 : .3; p.Air = Balance.Enemy(target.Kind).Movement == MovementKind.Air; Projectiles.Add(p); } return; }
        if (t.Kind is TowerKind.Goo or TowerKind.Ice) { ApplySlow(target, Balance.SlowStrength(t.Kind, t.Level), t.Kind == TowerKind.Goo ? 2 : 2.5); Damage(target, stats.Damage, t.Kind); return; }
        if (t.Kind == TowerKind.Tesla) { Damage(target, stats.Damage, t.Kind); candidates.Clear(); foreach (var e in Enemies) if (e != target && !e.Dead && Balance.CanTarget(t.Kind, e.Kind) && Dist(target.X, target.Y, e.X, e.Y) <= 1.5) candidates.Add(e); candidates.Sort((a, b) => a.Id.CompareTo(b.Id)); for (int i = 0; i < Math.Min(2, candidates.Count); i++) { Damage(candidates[i], stats.Damage * (i == 0 ? .5 : .25), t.Kind); AddEffect(t.Kind, target.X, target.Y, candidates[i].X, candidates[i].Y, .18); } return; }
        if (t.Kind is TowerKind.Flame or TowerKind.Laser) { var dx = target.X - t.X; var dy = target.Y - t.Y; var len = Math.Sqrt(dx * dx + dy * dy); if (len == 0) len = 1; dx /= len; dy /= len; foreach (var e in Enemies) { if (e.Dead || !Balance.CanTarget(t.Kind, e.Kind)) continue; var ex = e.X - t.X; var ey = e.Y - t.Y; var forward = ex * dx + ey * dy; var d = Dist(t.X, t.Y, e.X, e.Y); if (t.Kind == TowerKind.Flame ? d <= stats.Range && (d == 0 || forward / d >= Math.Cos(Math.PI / 6)) : forward >= 0 && forward <= stats.Range && Math.Abs(ex * dy - ey * dx) <= .175) Damage(e, stats.Damage, t.Kind); } return; }
        Damage(target, stats.Damage, t.Kind);
    }
    public void ApplySlow(Enemy e, double strength, double duration) { strength *= Balance.SlowEffectiveness(e.Kind); if (strength <= 0) return; var existing = e.Slows.Find(s => Math.Abs(s.Strength - strength) < .001); if (existing != null) existing.Remaining = Math.Max(existing.Remaining, duration); else e.Slows.Add(new SlowEffect { Strength = strength, Remaining = duration }); }
    public void Damage(Enemy e, double damage, TowerKind kind) { if (e.Dead) return; damage *= Balance.DamageMultiplier(e.Kind, kind); e.Health = Math.Max(0, e.Health - damage); if (e.Health <= 0) { e.Dead = true; Money = Math.Min(1000000000, Money + Balance.Enemy(e.Kind).Reward); AddScore(Balance.Enemy(e.Kind).Reward * 10); EnemyKilled?.Invoke(e); } }
    void AddEffect(TowerKind kind, double x, double y, double xx, double yy, double time) { if (Effects.Count < 600) Effects.Add(new CombatEffect { Kind = kind, X = x, Y = y, EndX = xx, EndY = yy, Remaining = time }); }
    public SessionSnapshot Capture() => SnapshotValidation.Clone(new SessionSnapshot() { BalanceRevision = Balance.Revision, MapId = Map.Id, MapWidth = Map.Width, MapHeight = Map.Height, Mode = Mode, Difficulty = Difficulty, State = State, ResumeState = resumeState, Money = Money, Lives = Lives, Wave = Wave, Score = Score, NextId = nextId, WaveTime = waveTime, BreakTime = BreakTime, Tick = Tick, Seed = Seed, Towers = Towers, Enemies = Enemies, Projectiles = Projectiles, Pending = pending });
    public static GameSession Restore(MapDefinition map, SessionSnapshot s) { SnapshotValidation.Validate(map, s); s = SnapshotValidation.Clone(s); Balance.MigrateLegacy(s); if (s.MapId != map.Id || s.Money < 0 || s.Money > 1000000000 || s.Lives < 0 || s.Lives > 20 || s.Wave < 0 || s.Wave > 1000000 || s.Towers.Count > map.Cells.Length || s.Enemies.Count > 300 || s.Projectiles.Count > 600 || s.Pending.Count > 1000) throw new InvalidDataException("Invalid session"); var game = new GameSession(map, s.Mode, s.Difficulty, s.Seed) { State = s.State, resumeState = s.ResumeState, Money = s.Money, Lives = s.Lives, Wave = s.Wave, Score = s.Score, nextId = s.NextId, waveTime = s.WaveTime, BreakTime = s.BreakTime, Tick = s.Tick }; foreach (var t in s.Towers) { if (!map.Contains(t.X, t.Y) || t.Level < 1 || t.Level > 3 || !Enum.IsDefined(t.Kind) || !double.IsFinite(t.Cooldown) || game.Towers.Any(a => a.X == t.X && a.Y == t.Y)) throw new InvalidDataException("Invalid tower"); game.Towers.Add(t); } foreach (var e in s.Enemies) { if (!double.IsFinite(e.Health) || e.Health < 0 || !double.IsFinite(e.X) || !double.IsFinite(e.Y) || !map.Contains(e.From.X, e.From.Y) || !map.Contains(e.To.X, e.To.Y) || !map.Routes.Any(r => r.Id == e.RouteId) || !Enum.IsDefined(e.Kind) || e.Slows.Any(s => !double.IsFinite(s.Strength) || s.Strength < 0 || s.Strength >= 1 || !double.IsFinite(s.Remaining))) throw new InvalidDataException("Invalid enemy"); game.Enemies.Add(e); } game.Projectiles.AddRange(s.Projectiles); game.pending.AddRange(s.Pending); game.Repath(); foreach (var r in map.Routes.Where(r => r.Movement == MovementKind.Ground)) if (game.distances[r.Id][r.Points[0].Y * map.Width + r.Points[0].X] < 0) throw new InvalidDataException("Invalid path"); foreach (var e in game.Enemies.Where(e => Balance.Enemy(e.Kind).Movement == MovementKind.Ground)) if (game.distances[e.RouteId][e.To.Y * map.Width + e.To.X] < 0 || !Pathfinding.Walkable(map, e.From.X, e.From.Y, game.Towers)) throw new InvalidDataException("Trapped enemy"); return game; }
}

