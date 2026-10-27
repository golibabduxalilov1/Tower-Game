using System.Text.Json;
using TowerGame.Core;

var tests = new (string Name, Action Run)[]
{
    ("guide DPS and radius contract, including support towers", GuideStats),
    ("armored counters and slow-resistant runners", CounterRoles),
    ("legacy battle reprices once and preserves health fraction", LegacyBalance),
    ("ground ingress is continuous on all four borders", EntryDirections),
    ("towers engage approaching enemies and projectiles restore", EntryCombat),
    ("entry corridor validation and paused restore", EntryValidation),
    ("map round limits and explicit economy bonuses", RoundEconomy),
    ("turrets track during reload and clear dead targets", ReloadTracking),
    ("last enemy leaves no frozen mortar in wave break", WaveShellCleanup),
    ("first wave waits for player", Preparation),
    ("reverse BFS and deterministic tie breaking", Bfs),
    ("last path rejection is atomic", LastPath),
    ("occupied movement segment cannot be built on", OccupiedSegment),
    ("unaffordable build has no side effects", Insufficient),
    ("upgrade formula and timer preservation", Upgrade),
    ("preparation and active sale refunds", Refund),
    ("armor resistance and exactly one death reward", ArmorReward),
    ("strongest slow and independent expiry", Slows),
    ("boss slow reduction and rail immunity", BossSlow),
    ("retains valid target and breaks ties by enemy id", TargetRetention),
    ("Tesla hits only two extra enemies", Tesla),
    ("Flame cone excludes rear and air targets", Flame),
    ("Laser line respects width and maximum range", Laser),
    ("Missile splash respects air layer", Missile),
    ("Mortar lands at original position after target dies", Mortar),
    ("escaped targets never yield late projectile rewards", Escaped),
    ("wave does not finish before delayed spawns", Pending),
    ("defeat wins over wave-100 completion", Defeat),
    ("save restore gives identical subsequent state", Restore),
    ("snapshot is independent of live mutable state", SnapshotCopy),
    ("malformed snapshots are rejected", InvalidSnapshots),
    ("pause freezes simulation", Pause),
    ("1x and 2x have equivalent logical results", DoubleSpeed),
    ("100 consecutive waves award victory once", HundredWaves),
    ("long Endless run stays bounded and does not win", Endless),
    ("content validation rejects broken routes", InvalidMaps),
    ("Endless waves are a pure function of seed and number", EndlessSeed),
    ("AutoPlayer is deterministic and uses only legal commands", AutoPlayerDeterminism),
    ("stress scenario reaches the specified load and stays bounded", Stress),
    ("presentation events fire for shots, kills and escapes", Events),
    ("path preview cache refreshes when walkability changes", PathCache),
    ("save restore preserves the Endless generator", EndlessRestore),
};
int failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL {test.Name}: {ex.Message}"); }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} tests passed");
return failed == 0 ? 0 : 1;

static void Check(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
static void Near(double actual, double expected, double tolerance = 1e-8) => Check(Math.Abs(actual - expected) < tolerance, $"{actual} != {expected}");
static void Throws(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected invalid data rejection"); }
static MapDefinition Map(int width = 24, int height = 16)
{
    var map = new MapDefinition { Width = width, Height = height, Cells = new CellType[width * height], SpecialTowers = [TowerKind.Flame, TowerKind.Mortar, TowerKind.Laser, TowerKind.Ice] };
    map.Routes = [new() { Id = "ground", Points = [new(0, height / 2), new(width - 1, height / 2)] },
        new() { Id = "air", Movement = MovementKind.Air, Points = [new(0, 0), new(width - 1, height - 1)] }];
    map.Cells[(height / 2) * width] = CellType.Entrance;
    map.Cells[(height / 2) * width + width - 1] = CellType.Exit;
    map.Waves = Enumerable.Range(1, 100).Select(w => new WaveDefinition { Number = w, Reward = 10 + w / 5, Groups = [new() { Kind = EnemyKind.Infantry, Count = 1, RouteId = "ground", HealthMultiplier = 1 }] }).ToList();
    return map;
}
static GameSession Rich(MapDefinition? map = null, Mode mode = Mode.Extended)
{
    map ??= Map();
    var snapshot = new GameSession(map, mode).Capture();
    snapshot.Money = 100000;
    return GameSession.Restore(map, snapshot);
}
static Enemy Enemy(int id, double x, double y, EnemyKind kind = EnemyKind.Infantry)
{
    return new() { Id = id, Kind = kind, X = x, Y = y, From = new((int)x, (int)y), To = new((int)x + 1, (int)y), RouteId = Balance.Enemy(kind).Movement == MovementKind.Air ? "air" : "ground", Health = 10000, MaxHealth = 10000 };
}
static GameSession Combat(TowerKind kind, out Tower tower)
{
    var game = Rich();
    Check(game.TryBuild(8, 8, kind, out _));
    tower = game.Towers.Single();
    game.StartWave();
    return game;
}
static void Steps(GameSession game, int count) { for (var i = 0; i < count; i++) game.Step(); }
static void Preparation() { var game = new GameSession(Map()); Steps(game, 1000); Check(game.Wave == 0 && game.Enemies.Count == 0 && game.Money == 150); }
static void Bfs()
{
    var map = Map(); var distances = Pathfinding.Distances(map, new(23, 8));
    Check(distances[8 * 24] == 23); Check(Pathfinding.Next(map, distances, new(5, 5), new(4, 5)) == new Cell(6, 5));
    Check(Pathfinding.Next(map, distances, new(5, 5), new(5, 4)) == new Cell(5, 6));
}
static void LastPath()
{
    var map = Map(8, 3);
    for (var x = 0; x < 8; x++) { map.Cells[x] = CellType.Rock; map.Cells[16 + x] = CellType.Rock; }
    var game = Rich(map); var before = JsonSerializer.Serialize(game.Capture());
    Check(!game.TryBuild(4, 1, TowerKind.Gatling, out var error) && error == "path_blocked");
    Check(before == JsonSerializer.Serialize(game.Capture()));
}
static void OccupiedSegment()
{
    var game = Rich(); game.StartWave(); game.Step(); var enemy = game.Enemies.Single();
    for (var i = 0; i < 300 && enemy.Entering; i++) game.Step();
    game.Step();
    Check(!game.TryBuild(enemy.To.X, enemy.To.Y, TowerKind.Gatling, out var error) && error == "enemy_occupies_cell");
    Check(enemy.X > enemy.From.X && enemy.X < enemy.To.X);
}
static void Insufficient() { var game = new GameSession(Map(), Mode.Extended, Difficulty.Hard); var before = JsonSerializer.Serialize(game.Capture()); Check(!game.TryBuild(2, 3, TowerKind.Laser, out var error) && error == "not_enough_money"); Check(before == JsonSerializer.Serialize(game.Capture())); }
static void Upgrade()
{
    var game = Rich(); game.TryBuild(8, 5, TowerKind.Gatling, out _); var t = game.Towers.Single(); t.Cooldown = .12;
    Check(game.Upgrade(t.Id, out _) && t.Spent == 70); Near(t.Cooldown, .12); Near(Balance.Tower(t.Kind, t.Level).Damage, 17.5);
    Check(game.Upgrade(t.Id, out _) && t.Spent == 120); Near(Balance.Tower(t.Kind, 3).Range, 2.5); Check(!game.Upgrade(t.Id, out _));
}
static void Refund()
{
    var game = Rich(); game.TryBuild(8, 5, TowerKind.Gatling, out _); var t = game.Towers.Single(); Check(game.SellValue(t) == 40);
    game.StartWave(); Check(game.SellValue(t) == 28); var money = game.Money; Check(game.Sell(t.Id) && game.Money == money + 28);
}
static void ArmorReward()
{
    var game = Rich(); var enemy = Enemy(99, 4, 8, EnemyKind.Tank); game.Damage(enemy, 100, TowerKind.Gatling); Near(enemy.Health, 9965);
    game.Damage(enemy, 100, TowerKind.Flame); Near(enemy.Health, 9910); var money = game.Money; var score = game.Score;
    game.Damage(enemy, 100000, TowerKind.Laser); game.Damage(enemy, 100000, TowerKind.Laser);
    Check(game.Money == money + 40 && game.Score == score + 400);
}
static void Slows()
{
    var game = Rich(); game.StartWave(); game.Step(); var enemy = game.Enemies.Single();
    game.ApplySlow(enemy, .25, 2.5); game.ApplySlow(enemy, .55, .5); Near(game.CurrentSpeed(enemy), 1.2 * .45);
    Steps(game, 31); Near(game.CurrentSpeed(enemy), 1.2 * .75); Steps(game, 121); Near(game.CurrentSpeed(enemy), 1.2);
}
static void BossSlow()
{
    var game = Rich(); var boss = Enemy(3, 4, 4, EnemyKind.HeavyTank); game.ApplySlow(boss, .5, 2); Near(game.CurrentSpeed(boss), .45 * .75);
    var rail = Enemy(4, 4, 4, EnemyKind.RailColossus); game.ApplySlow(rail, .9, 2); Check(rail.Slows.Count == 0);
}
static void TargetRetention()
{
    var game = Combat(TowerKind.Gatling, out var t); var a = Enemy(200, 9, 8); var b = Enemy(100, 9, 8); game.Enemies.AddRange([a,b]);
    game.Step(); Check(t.TargetId == 100); t.Priority = TargetPriority.Strongest; a.Health = 999999; Steps(game, 20); Check(t.TargetId == 100);
}
static void Tesla()
{
    var game = Combat(TowerKind.Tesla, out _); var enemies = Enumerable.Range(100, 4).Select(i => Enemy(i, 9, 8)).ToArray(); game.Enemies.AddRange(enemies); game.Step();
    Near(enemies[0].Health, 9300); Near(enemies[1].Health, 9650); Near(enemies[2].Health, 9825); Near(enemies[3].Health, 10000);
}
static void Flame()
{
    var game = Combat(TowerKind.Flame, out var tower); tower.TargetId = 100; var front = Enemy(100, 9, 8); var rear = Enemy(101, 7, 8); var air = Enemy(102, 9, 8, EnemyKind.Helicopter); game.Enemies.AddRange([front,rear,air]); game.Step();
    Near(front.Health, 9940); Near(rear.Health, 10000); Near(air.Health, 10000);
}
static void Laser()
{
    var game = Combat(TowerKind.Laser, out _); var target = Enemy(100, 9, 8); var line = Enemy(101, 10, 8); var side = Enemy(102, 10, 8.4); var far = Enemy(103, 14, 8); game.Enemies.AddRange([target,line,side,far]); game.Step();
    Near(target.Health, 9900); Near(line.Health, 9900); Near(side.Health, 10000); Near(far.Health, 10000);
}
static void Missile()
{
    var game = Combat(TowerKind.Missile, out var tower); tower.TargetId = 100; var target = Enemy(100, 9, 8); var ground = Enemy(101, 9, 8.5); var air = Enemy(102, 9, 8, EnemyKind.Helicopter);
    game.Enemies.AddRange([target,ground,air]); Steps(game, 20); Near(target.Health, 9887.5); Near(ground.Health, 9887.5); Near(air.Health, 10000);
}
static void Mortar()
{
    var game = Combat(TowerKind.Mortar, out _); var target = Enemy(100, 11, 8); var near = Enemy(101, 11, 8.5); game.Enemies.AddRange([target,near]); game.Step();
    Check(game.Projectiles.Count == 1); var x = game.Projectiles[0].TargetX; game.Damage(target, 100000, TowerKind.Laser); Steps(game, 40); Near(near.Health, 9750); Check(game.Projectiles.Count == 0); Check(x < 11.1);
}
static void Escaped()
{
    var game = Rich(); var enemy = Enemy(100, 22.99, 8); enemy.From = new(22, 8); enemy.To = new(23, 8); game.StartWave(); game.Enemies.Add(enemy);
    game.Projectiles.Add(new() { Id = 200, Kind = TowerKind.Missile, TargetId = 100, X = 20, Y = 8, TargetX = 23, TargetY = 8, Damage = 1e6, Remaining = .1 });
    var money = game.Money; Steps(game, 10); Check(game.Lives == 19 && game.Money == money);
}
static void Pending()
{
    var map = Map(); map.Waves[0].Groups.Add(new() { Kind = EnemyKind.Infantry, Count = 1, RouteId = "ground", Delay = 10 });
    var game = Rich(map); game.StartWave(); game.Step(); foreach (var e in game.Enemies) game.Damage(e, 1e9, TowerKind.Laser); game.Step(); Check(game.State == SessionState.Active);
}
static void Defeat()
{
    var map = Map(3, 3); var s = new GameSession(map).Capture(); s.Wave = 99; s.State = SessionState.Intermission; s.Lives = 1;
    var game = GameSession.Restore(map, s); game.StartWave(); Steps(game, 400); Check(game.State == SessionState.Defeat && game.Lives == 0 && game.Score == 0);
}
static void Restore()
{
    var game = Rich(); game.TryBuild(5, 7, TowerKind.Missile, out _); game.StartWave(); Steps(game, 240);
    var snapshot = JsonSerializer.Deserialize<SessionSnapshot>(JsonSerializer.Serialize(game.Capture()))!; var copy = GameSession.Restore(game.Map, snapshot);
    Steps(game, 800); Steps(copy, 800); Check(JsonSerializer.Serialize(game.Capture()) == JsonSerializer.Serialize(copy.Capture()));
}
static void SnapshotCopy()
{
    var game = Rich(); game.TryBuild(4, 4, TowerKind.Gatling, out _); var snapshot = game.Capture(); snapshot.Towers[0].Level = 3; Check(game.Towers[0].Level == 1);
}
static void InvalidSnapshots()
{
    var map = Map(); var s = Rich(map).Capture(); s.Money = -1; Throws(() => GameSession.Restore(map, s));
    s = Rich(map).Capture(); s.State = (SessionState)99; Throws(() => GameSession.Restore(map, s));
    s = Rich(map).Capture(); s.Pending.Add(new() { RouteId = "missing" }); Throws(() => GameSession.Restore(map, s));
    var game = Rich(map); game.TryBuild(4, 4, TowerKind.Gatling, out _); s = game.Capture(); s.Towers[0].Cooldown = double.NaN; Throws(() => GameSession.Restore(map, s));
    game.StartWave(); game.Step(); s = game.Capture(); s.Enemies[0].X = 999; Throws(() => GameSession.Restore(map, s));
}
static void Pause() { var game = Rich(); game.StartWave(); game.Step(); game.SetPaused(true); var before = JsonSerializer.Serialize(game.Capture()); Steps(game, 500); Check(before == JsonSerializer.Serialize(game.Capture())); game.SetPaused(false); Check(game.State == SessionState.Active); }
static void DoubleSpeed()
{
    var a = Rich(); a.TryBuild(6, 7, TowerKind.Gatling, out _); a.StartWave(); var b = GameSession.Restore(a.Map, a.Capture());
    for (int frame = 0; frame < 1000; frame++) a.Step();
    for (int frame = 0; frame < 500; frame++) { b.Step(); b.Step(); }
    Check(JsonSerializer.Serialize(a.Capture()) == JsonSerializer.Serialize(b.Capture()));
}
static void ClearWave(GameSession game)
{
    Check(game.StartWave()); var ticks = 0;
    while (game.State == SessionState.Active && ticks++ < 30000) { game.Step(); foreach (var e in game.Enemies) game.Damage(e, 1e16, TowerKind.Laser); }
    Check(game.State != SessionState.Active, "Wave failed to complete");
}
static void HundredWaves()
{
    var game = Rich(); for (int i = 0; i < 100; i++) ClearWave(game);
    Check(game.State == SessionState.Victory && game.Wave == 100 && game.Lives == 20);
    Check(game.Score == 28000); var score = game.Score; Steps(game, 100); Check(game.Score == score && !game.StartWave());
}
static void Endless()
{
    var game = Rich(mode: Mode.Endless); for (int i = 0; i < 1000; i++) ClearWave(game);
    Check(game.State == SessionState.Intermission && game.Wave == 1000 && game.Enemies.Count == 0);
    Check(game.NextWave.Groups.Sum(g => g.Count) <= 60 && game.NextWave.Groups.All(g => double.IsFinite(g.HealthMultiplier) && g.HealthMultiplier <= 1e12));
}
static void InvalidMaps()
{
    var map = Map(); MapValidation.Validate(map); map.Routes[0].Points[0] = new(-1, 8); Throws(() => MapValidation.Validate(map));
    map = Map(); map.Waves[0].Groups[0].Count = int.MaxValue; Throws(() => MapValidation.Validate(map));
    map = Map(); map.Cells = []; Throws(() => MapValidation.Validate(map));
}
static void EndlessSeed()
{
    var map = Map();
    var a = new GameSession(map, Mode.Endless, seed: 7); var b = new GameSession(map, Mode.Endless, seed: 7); var c = new GameSession(map, Mode.Endless, seed: 8);
    string Wave(GameSession g, int n) => JsonSerializer.Serialize(g.GetWave(n));
    Check(Wave(a, 150) == Wave(b, 150), "same seed differs");
    Check(Wave(a, 150) != Wave(c, 150), "seed has no effect");
    Check(a.GetWave(150).Groups.Sum(g => g.Count) == 60 && a.GetWave(160).HasBoss && !a.GetWave(161).HasBoss, "endless shape");
    Check(Wave(a, 50) == Wave(c, 50), "authored waves must not depend on seed");
}
static void AutoPlayerDeterminism()
{
    string Run() { var g = Rich(); var bot = new AutoPlayer(g); for (int i = 0; i < 6; i++) bot.PlayWave(); return JsonSerializer.Serialize(g.Capture()); }
    var first = Run(); Check(first == Run(), "bot run not reproducible");
    var snap = JsonSerializer.Deserialize<SessionSnapshot>(first)!;
    Check(snap.Wave == 6 && snap.Towers.Count > 0, "bot did not play");
    _ = GameSession.Restore(Map(), snap); // every bot action left a valid, restorable state
}
static void Stress()
{
    var game = StressScenario.Create(); var id = 10_000_000;
    StressScenario.Refill(game, ref id);
    Check(game.Towers.Count == StressScenario.Towers, $"towers {game.Towers.Count}");
    Check(game.Enemies.Count(e => !e.Dead) == StressScenario.Enemies, "enemies");
    var watch = System.Diagnostics.Stopwatch.StartNew();
    var peak = 0;
    for (int i = 0; i < 600; i++) { game.Step(); game.Step(); if (i % 10 == 0) StressScenario.Refill(game, ref id); peak = Math.Max(peak, game.Projectiles.Count + game.Effects.Count); }
    Check(peak >= 550, $"load too low: {peak} projectiles+effects");
    Check(game.State == SessionState.Active, "stress run ended early");
    Check(game.Projectiles.Count <= 600 && game.Effects.Count <= 600, "caps exceeded");
    Console.WriteLine($"     stress: 1200 ticks in {watch.ElapsedMilliseconds} ms ({watch.Elapsed.TotalMilliseconds / 1200:0.000} ms/tick), shots {game.Projectiles.Count}, fx {game.Effects.Count}");
}
static void Events()
{
    var game = Combat(TowerKind.Gatling, out _); int fired = 0, killed = 0, escaped = 0;
    game.TowerFired += _ => fired++; game.EnemyKilled += _ => killed++; game.EnemyEscaped += _ => escaped++;
    var target = Enemy(100, 9, 8); target.Health = target.MaxHealth = 1; game.Enemies.Add(target);
    var runner = Enemy(101, 22.99, 8); runner.From = new(22, 8); runner.To = new(23, 8); game.Enemies.Add(runner);
    Steps(game, 5);
    Check(fired >= 1 && killed == 1 && escaped == 1, $"fired {fired} killed {killed} escaped {escaped}");
}
static void PathCache()
{
    var game = Rich(); var before = game.PreviewPath("ground");
    Check(ReferenceEquals(before, game.PreviewPath("ground")), "not cached");
    Check(game.TryBuild(5, 8, TowerKind.Gatling, out _));
    var after = game.PreviewPath("ground");
    Check(!ReferenceEquals(before, after) && after.Count > before.Count && !after.Contains(new Cell(5, 8)), "stale path after build");
}
static void EndlessRestore()
{
    var map = Map(); var snapshot = new GameSession(map, Mode.Endless, seed: 99).Capture();
    var restored = GameSession.Restore(map, JsonSerializer.Deserialize<SessionSnapshot>(JsonSerializer.Serialize(snapshot))!);
    Check(JsonSerializer.Serialize(restored.GetWave(222)) == JsonSerializer.Serialize(new GameSession(map, Mode.Endless, seed: 99).GetWave(222)), "seed lost");
}
static void GuideStats()
{
    (TowerKind Kind, double[] Dps, double Range, int Dollars)[] guide =
    [
        (TowerKind.Gatling, [35,70,105], 2.5,5),
        (TowerKind.Goo,[0,0,0],2.5,10),
        (TowerKind.Missile,[75,150,225],3.5,20),
        (TowerKind.Tesla,[700,1400,2100],2.5,70),
        (TowerKind.Flame,[300,600,900],2.5,50),
        (TowerKind.Mortar,[100,200,300],3.5,30),
        (TowerKind.Laser,[125,250,450],2.5,40),
        (TowerKind.Ice,[0,0,0],1.5,15)
    ];
    foreach(var row in guide)
        for(int level=1;level<=3;level++)
        {
            var stats=Balance.Tower(row.Kind,level);
            Near(stats.Damage/stats.Interval,row.Dps[level-1]);
            Near(stats.Range,row.Range);
            Check(stats.Cost==row.Dollars*8);
        }
    Near(Balance.SlowStrength(TowerKind.Goo,3),.75);
    Near(Balance.SlowStrength(TowerKind.Ice,1),.45);
    Check(Balance.InvestmentCost(TowerKind.Tesla,3)==170*8);
}
static void CounterRoles()
{
    var game=Rich(); var tank=Enemy(99,4,8,EnemyKind.Tank);
    game.Damage(tank,100,TowerKind.Gatling); Near(tank.Health,9965);
    game.Damage(tank,100,TowerKind.Missile); Near(tank.Health,9840);
    var runner=Enemy(98,4,8,EnemyKind.Runner);
    game.ApplySlow(runner,.75,2); Near(game.CurrentSpeed(runner),2.1*.625);
    game.ApplySlow(runner,.40,3); Near(game.CurrentSpeed(runner),2.1*.625);
}
static void LegacyBalance()
{
    var game=Rich(); game.TryBuild(8,5,TowerKind.Tesla,out _); game.StartWave();
    var old=game.Capture(); old.BalanceRevision=0; old.NextId=1000; old.Towers.Single().Spent=150;
    var enemy=Enemy(99,4,8); enemy.Health=20;enemy.MaxHealth=40;old.Enemies.Add(enemy);
    var restored=GameSession.Restore(game.Map,old);
    Near(restored.Enemies.Single().MaxHealth,120);Near(restored.Enemies.Single().Health,60);
    Check(restored.Towers.Single().Spent==560 && old.Towers.Single().Spent==150);
    var current=restored.Capture(); Check(current.BalanceRevision==Balance.Revision);
    var again=GameSession.Restore(game.Map,current);Near(again.Enemies.Single().Health,60);
    Check(again.Upgrade(again.Towers.Single().Id,out _));
    _=GameSession.Restore(game.Map,again.Capture());
}

static void EntryDirections()
{
    foreach (var entry in new Cell[] { new(0,3), new(7,3), new(4,0), new(4,5) })
    {
        var map = Map(8,6); Array.Fill(map.Cells, CellType.Ground);
        var exit = entry.X == 0 ? new Cell(7,3) : entry.X == 7 ? new Cell(0,3)
            : entry.Y == 0 ? new Cell(4,5) : new Cell(4,0);
        map.Routes[0].Points = [entry, exit];
        map.Cells[entry.Y*8+entry.X] = CellType.Entrance; map.Cells[exit.Y*8+exit.X] = CellType.Exit;
        var game = new GameSession(map); game.StartWave(); game.Step(); var e = game.Enemies.Single();
        var outward = EntryApproach.Outward(map, entry);
        Check(e.Entering); Near((e.X-entry.X)*outward.X+(e.Y-entry.Y)*outward.Y, EntryApproach.Cells-Balance.Enemy(e.Kind).Speed*GameSession.FixedDelta);
        var priorX = e.X; var priorY = e.Y;
        for (var i=0;i<180;i++)
        {
            game.Step();
            Near(Math.Sqrt(Math.Pow(e.X-priorX,2)+Math.Pow(e.Y-priorY,2)), Balance.Enemy(e.Kind).Speed*GameSession.FixedDelta, 1e-7);
            priorX=e.X;priorY=e.Y;
        }
        Check(!e.Entering && map.Contains((int)Math.Round(e.X),(int)Math.Round(e.Y)));
    }
}
static void EntryCombat()
{
    foreach(var kind in new[] { TowerKind.Gatling, TowerKind.Missile })
    {
        var map=Map(8,6);var game=Rich(map);Check(game.TryBuild(1,2,kind,out _));game.StartWave();
        for(var i=0;i<110 && game.Towers[0].TargetId==0;i++) game.Step();
        var e=game.Enemies.Single();Check(e.Entering && e.X<0 && game.Towers[0].AimX<0);
        if(kind==TowerKind.Missile) Check(game.Projectiles.Any(p=>p.TargetX<0));
        var restored=GameSession.Restore(map,game.Capture());
        Steps(game,20);Steps(restored,20);
        Check(JsonSerializer.Serialize(game.Capture())==JsonSerializer.Serialize(restored.Capture()));
        Check(e.Entering && e.Health<e.MaxHealth);
    }
}
static void EntryValidation()
{
    var map=Map(8,6);var game=new GameSession(map);game.StartWave();Steps(game,20);game.SetPaused(true);
    var before=JsonSerializer.Serialize(game.Capture());Steps(game,60);Check(before==JsonSerializer.Serialize(game.Capture()));
    var restored=GameSession.Restore(map,game.Capture());Check(before==JsonSerializer.Serialize(restored.Capture()));
    foreach(var mutate in new Action<Enemy>[] { e=>e.X=-4, e=>e.Y+=.1, e=>e.Entering=false, e=>e.X=double.NaN, e=>e.To=new(1,3), e=>e.Waypoint=2 })
    {
        var snapshot=game.Capture();mutate(snapshot.Enemies.Single());Throws(()=>GameSession.Restore(map,snapshot));
    }
}



static void RoundEconomy()
{
    foreach(var limit in new[] {20,50,60,80})
    {
        var map=Map();map.Waves=map.Waves.Take(limit).ToList();
        foreach(var wave in map.Waves)wave.Reward=0;
        var game=Rich(map);var money=game.Money;
        for(var i=0;i<limit;i++)ClearWave(game);
        Check(game.State==SessionState.Victory && game.Wave==limit && !game.StartWave());
        Check(game.Money==money+limit*Balance.Enemy(EnemyKind.Infantry).Reward);
        Check(GameSession.Restore(map,game.Capture()).State==SessionState.Victory);
        var endless=Rich(map,Mode.Endless);for(var i=0;i<limit;i++)ClearWave(endless);
        Check(endless.StartWave() && endless.Wave==limit+1 && endless.GetWave(limit+1).Reward==0);
    }
    foreach(var bonus in new[]{0,30})
    {
        var map=Map();map.Waves=map.Waves.Take(1).ToList();map.Waves[0].Reward=bonus;
        var game=new GameSession(map);ClearWave(game);
        Check(game.Money==150+Balance.Enemy(EnemyKind.Infantry).Reward+bonus);
        Check(game.Score>game.Money);
    }
}

static void ReloadTracking()
{
    var game=Combat(TowerKind.Gatling,out var tower);var enemy=Enemy(100,9,8);
    game.Enemies.Add(enemy);tower.TargetId=100;tower.Cooldown=2;
    game.Step();Near(tower.AimX,enemy.X);Near(tower.AimY,enemy.Y);Check(tower.Cooldown>1);
    game.Damage(enemy,1e9,TowerKind.Gatling);game.Step();
    Check(tower.TargetId==0);Near(tower.AimX,tower.X+1);Near(tower.AimY,tower.Y);
}
static void WaveShellCleanup()
{
    var game=Rich(Map(8,6));Check(game.TryBuild(1,2,TowerKind.Mortar,out _));game.StartWave();
    for(var i=0;i<110 && game.Projectiles.Count==0;i++)game.Step();
    Check(game.Projectiles.Count==1 && game.Projectiles[0].Remaining>0);
    game.Damage(game.Enemies.Single(),1e9,TowerKind.Laser);game.Step();
    Check(game.State==SessionState.Intermission && game.Projectiles.Count==0 && game.Towers[0].TargetId==0);
    var copy=GameSession.Restore(game.Map,game.Capture());Steps(copy,30);Check(copy.Projectiles.Count==0);
    Check(copy.StartWave());Check(copy.Projectiles.Count==0);
}
