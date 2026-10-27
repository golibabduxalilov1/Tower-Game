using System.Text.Json;
using TowerGame.Content;
using TowerGame.Core;

namespace TowerGame.Tools;

public static class CombatAudit
{
    public static void Run()
    {
        var map = Catalog.Maps[0];
        var report = new List<object>();
        foreach (var difficulty in Enum.GetValues<Difficulty>())
        {
            var game = new GameSession(map, Mode.Classic, difficulty);
            foreach (var cell in new[] { new Cell(5, 5), new Cell(7, 7), new Cell(9, 5) })
                if (!game.TryBuild(cell.X, cell.Y, TowerKind.Gatling, out var error)) throw new Exception(error);
            int killed = 0, escaped = 0, shots = 0;
            game.EnemyKilled += _ => killed++;
            game.EnemyEscaped += _ => escaped++;
            game.TowerFired += _ => shots++;
            var waves = new List<object>();
            while (game.Wave < 30 && game.State != SessionState.Defeat)
            {
                game.StartWave();
                for (int i = 0; i < 30000 && game.State == SessionState.Active; i++) game.Step();
                if (game.State == SessionState.Active) throw new Exception("Wave did not terminate");
                waves.Add(new { game.Wave, game.Lives, killed, escaped, shots });
            }
            report.Add(new { strategy = "three fixed level-one Gatlings; no upgrades or spending", difficulty, game.Wave, game.Lives, killed, escaped, shots, waves });
        }
        Console.WriteLine(JsonSerializer.Serialize(report, Catalog.JsonOptions));
    }
}
