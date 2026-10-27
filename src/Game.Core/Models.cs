namespace TowerGame.Core;

public enum CellType { Ground, Path, Rock, Platform, Entrance, Exit, Rail }
public enum TowerKind { Gatling, Goo, Missile, Tesla, Flame, Mortar, Laser, Ice }
public enum EnemyKind { Infantry, Runner, Heavy, Motorcycle, Tank, Helicopter, Plane, RailCart, HeavyTank, AirFortress, RailColossus }
public enum Mode { Classic, Extended, Endless }
public enum Difficulty { Easy, Normal, Hard }
public enum TargetPriority { ArrivalTime, Nearest, Strongest, Weakest, ExitNearest }
public enum SessionState { Preparation, Active, Intermission, Paused, Victory, Defeat }
public enum MovementKind { Ground, Air, Rail }
public readonly record struct Cell(int X, int Y);
public sealed class RouteDefinition { public string Id { get; set; } = "ground"; public MovementKind Movement { get; set; } public List<Cell> Points { get; set; } = []; }
public sealed class MapDefinition
{
    public string Id { get; set; } = "valley"; public string Name { get; set; } = "Green Valley"; public string NameUz { get; set; } = "Yashil vodiy"; public string Description { get; set; } = ""; public string Theme { get; set; } = "green";
    public int Width { get; set; } = 24; public int Height { get; set; } = 16; public CellType[] Cells { get; set; } = new CellType[384];
    public double HealthScale { get; set; } = 1;
    public List<RouteDefinition> Routes { get; set; } = []; public List<TowerKind> SpecialTowers { get; set; } = [TowerKind.Flame, TowerKind.Ice]; public List<WaveDefinition> Waves { get; set; } = [];
    public CellType At(int x, int y) => Contains(x, y) ? Cells[y * Width + x] : CellType.Rock; public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
}
public sealed class WaveGroup { public EnemyKind Kind { get; set; } public int Count { get; set; } public string RouteId { get; set; } = "ground"; public double Delay { get; set; } public double Interval { get; set; } = .9; public double HealthMultiplier { get; set; } = 1; }
public sealed class WaveDefinition { public int Number { get; set; } public int Reward { get; set; } public List<WaveGroup> Groups { get; set; } = []; public bool HasBoss => Groups.Any(g => g.Kind >= EnemyKind.HeavyTank); }
public sealed class Tower { public int Id { get; set; } public TowerKind Kind { get; set; } public int X { get; set; } public int Y { get; set; } public int Level { get; set; } = 1; public int Spent { get; set; } public double Cooldown { get; set; } public int TargetId { get; set; } public TargetPriority Priority { get; set; } public double AimX { get; set; } public double AimY { get; set; } }
public sealed class SlowEffect { public double Strength { get; set; } public double Remaining { get; set; } }
public sealed class Enemy { public bool Entering { get; set; } public int Id { get; set; } public EnemyKind Kind { get; set; } public string RouteId { get; set; } = "ground"; public double X { get; set; } public double Y { get; set; } public double Health { get; set; } public double MaxHealth { get; set; } public Cell From { get; set; } public Cell To { get; set; } public int Waypoint { get; set; } = 1; public double SegmentProgress { get; set; } public bool Dead { get; set; } public List<SlowEffect> Slows { get; set; } = []; }
public sealed class Projectile { public int Id { get; set; } public TowerKind Kind { get; set; } public int TargetId { get; set; } public double X { get; set; } public double Y { get; set; } public double TargetX { get; set; } public double TargetY { get; set; } public double Damage { get; set; } public double Remaining { get; set; } public bool Air { get; set; } }
public sealed class CombatEffect { public TowerKind Kind { get; set; } public double X { get; set; } public double Y { get; set; } public double EndX { get; set; } public double EndY { get; set; } public double Remaining { get; set; } = .15; }
public sealed class PendingSpawn { public EnemyKind Kind { get; set; } public string RouteId { get; set; } = "ground"; public double Time { get; set; } public double HealthMultiplier { get; set; } = 1; }
public sealed class SessionSnapshot { public int BalanceRevision { get; set; } public string MapId { get; set; } = ""; public int MapWidth { get; set; } public int MapHeight { get; set; } public Mode Mode { get; set; } public Difficulty Difficulty { get; set; } public SessionState State { get; set; } public SessionState ResumeState { get; set; } public int Money { get; set; } public int Lives { get; set; } public int Wave { get; set; } public long Score { get; set; } public int NextId { get; set; } public double WaveTime { get; set; } public double BreakTime { get; set; } public long Tick { get; set; } public uint Seed { get; set; } = 42; public List<Tower> Towers { get; set; } = []; public List<Enemy> Enemies { get; set; } = []; public List<Projectile> Projectiles { get; set; } = []; public List<PendingSpawn> Pending { get; set; } = []; }
public readonly record struct TowerStats(int Cost, double Damage, double Interval, double Range, double MinRange = 0);
public readonly record struct EnemyStats(double Health, double Speed, int Reward, MovementKind Movement, bool Boss = false);
