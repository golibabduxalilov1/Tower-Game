namespace TowerGame.Core;

/// <summary>
/// Performance load from the specification: 300 active enemies, 200 towers and up to 600
/// projectiles/effects. Uses a 40x20 serpentine map so units stay on the field for minutes.
/// Not part of the campaign and never saved.
/// </summary>
public static class StressScenario
{
    public const int Towers = 200, Enemies = 300;

    public static MapDefinition Map()
    {
        const int w = 40, h = 20;
        var map = new MapDefinition { Id = "stress", Name = "Stress", NameUz = "Stress", Theme = "stone", Width = w, Height = h, Cells = new CellType[w * h], SpecialTowers = [TowerKind.Flame, TowerKind.Mortar, TowerKind.Laser, TowerKind.Ice] };
        // Platform walls on rows 1,4,7,... with alternating gaps force a long serpentine route.
        for (int y = 1, wall = 0; y < h - 1; y += 3, wall++)
            for (int x = 0; x < w; x++)
                if (wall % 2 == 0 ? x < w - 2 : x > 1) map.Cells[y * w + x] = CellType.Platform;
        map.Cells[0] = CellType.Entrance;
        map.Cells[(h - 1) * w + w - 1] = CellType.Exit;
        var air = new List<Cell>();
        for (int i = 0; i < 12; i++) { air.Add(new(i % 2 == 0 ? 0 : w - 1, i * (h - 1) / 11)); }
        map.Routes = [new() { Id = "ground", Points = [new(0, 0), new(w - 1, h - 1)] }, new() { Id = "air", Movement = MovementKind.Air, Points = air }];
        return map;
    }

    public static GameSession Create()
    {
        var map = Map();
        var snapshot = new GameSession(map, Mode.Extended).Capture();
        snapshot.Money = 1000000000;
        var game = GameSession.Restore(map, snapshot);
        TowerKind[] kinds = [TowerKind.Gatling, TowerKind.Missile, TowerKind.Tesla, TowerKind.Laser, TowerKind.Mortar, TowerKind.Flame, TowerKind.Ice, TowerKind.Goo];
        var placed = 0;
        for (int y = 0; y < map.Height && placed < Towers; y++)
            for (int x = 0; x < map.Width && placed < Towers; x++)
                if (map.At(x, y) == CellType.Platform && (x + y) % 2 == 0 && game.TryBuild(x, y, kinds[placed % kinds.Length], out _))
                {
                    placed++;
                    var tower = game.Towers[^1];
                    game.Upgrade(tower.Id, out _); game.Upgrade(tower.Id, out _);
                }
        for (int y = 0; y < map.Height && placed < Towers; y++)
            for (int x = 0; x < map.Width && placed < Towers; x++)
                if (map.At(x, y) == CellType.Platform && game.TryBuild(x, y, kinds[placed % kinds.Length], out _)) placed++;
        game.StartWave();
        return game;
    }

    /// <summary>
    /// Tops the field back up to 300 durable units spread along the whole route, and keeps the
    /// combined projectile + effect count at the specified 600 so every system is under load.
    /// </summary>
    public static void Refill(GameSession game, ref int nextId)
    {
        var ground = game.PreviewPath("ground");
        var air = game.Map.Routes[1].Points;
        var alive = 0;
        foreach (var e in game.Enemies)
        {
            if (e.Dead) continue;
            alive++;
            // Recycle units about to escape so lives never run out during a measurement.
            if (game.RemainingDistance(e) < 6)
            {
                var start = e.RouteId == "air" ? air[0] : ground[0];
                var next = e.RouteId == "air" ? air[1] : ground[1];
                e.X = start.X; e.Y = start.Y; e.From = start; e.To = next; e.Waypoint = 1; e.SegmentProgress = 0;
            }
        }
        for (int i = alive; i < Enemies; i++)
        {
            var isAir = i % 3 == 0;
            Enemy enemy;
            if (isAir)
            {
                var segment = (int)((long)nextId * 7 % (air.Count - 3));
                enemy = new Enemy { RouteId = "air", Kind = i % 2 == 0 ? EnemyKind.Helicopter : EnemyKind.Plane, From = air[segment], To = air[segment + 1], Waypoint = segment + 1, X = air[segment].X, Y = air[segment].Y };
            }
            else
            {
                var k = (int)((long)nextId * 13 % Math.Max(1, ground.Count - 12));
                enemy = new Enemy { RouteId = "ground", Kind = (EnemyKind)(i % 5), From = ground[k], To = ground[k + 1], X = ground[k].X, Y = ground[k].Y };
            }
            enemy.Id = nextId++; enemy.Health = enemy.MaxHealth = 1e7;
            game.Enemies.Add(enemy);
        }
        var targets = game.Enemies.Where(e => !e.Dead).ToList();
        for (int i = 0; game.Projectiles.Count + game.Effects.Count < 600 && i < 600; i++)
        {
            var t = targets[(nextId + i * 31) % targets.Count];
            if (i % 2 == 0)
                game.Projectiles.Add(new Projectile { Id = nextId++, Kind = i % 4 == 0 ? TowerKind.Mortar : TowerKind.Missile, TargetId = t.Id, X = t.X - 3, Y = t.Y, TargetX = t.X, TargetY = t.Y, Damage = 1, Remaining = .6, Air = Balance.Enemy(t.Kind).Movement == MovementKind.Air });
            else
                game.Effects.Add(new CombatEffect { Kind = (TowerKind)(i % 8), X = t.X - 2, Y = t.Y - 1, EndX = t.X, EndY = t.Y, Remaining = .5 });
        }
    }
}