using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using TowerGame.Core;

namespace TowerGame.Rendering;

public sealed partial class Battlefield
{
    sealed class SoldierMotion
    {
        public double X, Y, Cycles;
        public float Angle;
        public SoldierMotion(Enemy enemy)
        {
            X = enemy.X; Y = enemy.Y; Cycles = enemy.Id * .381966;
            Angle = enemy.Entering ? MathF.Atan2((float)(enemy.From.Y - enemy.Y), (float)(enemy.From.X - enemy.X)) : MathF.Atan2(enemy.To.Y - enemy.From.Y, enemy.To.X - enemy.From.X);
        }
    }
    readonly ConditionalWeakTable<Enemy, SoldierMotion> soldierMotion = new();

    bool DrawRunningSoldier(Enemy enemy, Vector2 at, float cell, float targetAngle, double time, out float healthY)
    {
        var motion = soldierMotion.GetValue(enemy, static e => new SoldierMotion(e));
        double dx = enemy.X - motion.X, dy = enemy.Y - motion.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance > .000001)
        {
            // Stride follows distance, so slow effects,2x speed and pauses stay in sync.
            motion.Cycles += distance / (enemy.Kind == EnemyKind.Runner ? .9 :
                enemy.Kind is EnemyKind.Infantry or EnemyKind.Heavy ? .78 : 1.3);
            float turn = MathHelper.WrapAngle(targetAngle - motion.Angle);
            float maxTurn = (float)distance * 8;
            motion.Angle = MathHelper.WrapAngle(motion.Angle + Math.Clamp(turn, -maxTurn, maxTurn));
        }
        motion.X = enemy.X; motion.Y = enemy.Y;
        bool airborne = Balance.Enemy(enemy.Kind).Movement == MovementKind.Air;
        // Simulation time keeps propellers turning while hovering and freezes them on pause.
        var cycles = airborne ? time * 6 + enemy.Id * .381966 : motion.Cycles;
        return p.Soldier(enemy.Kind, at + Vector2.UnitY * cell * .32f, cell, motion.Angle, cycles, out healthY);
    }
}
