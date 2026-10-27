using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerGame.Core;

namespace TowerGame.Rendering;

public sealed partial class Painter
{
    public bool Soldier(EnemyKind kind, Vector2 foot, float cell, float angle, double gaitCycles)
        => Soldier(kind, foot, cell, angle, gaitCycles, out _);

    public bool Soldier(EnemyKind kind, Vector2 foot, float cell, float angle, double gaitCycles, out float healthY, Color? tint = null)
    {
        healthY = foot.Y - cell;
        var key = kind switch { EnemyKind.Infantry => "soldier:Infantry", EnemyKind.Runner => "soldier:Runner", EnemyKind.Heavy => "soldier:Heavy",
            EnemyKind.Motorcycle => "soldier:Motorcycle", EnemyKind.Tank => "soldier:Tank",
            EnemyKind.Helicopter => "soldier:Helicopter", EnemyKind.Plane => "soldier:Plane",
            EnemyKind.RailCart => "soldier:RailCart", EnemyKind.HeavyTank => "soldier:HeavyTank",
            EnemyKind.AirFortress => "soldier:AirFortress", EnemyKind.RailColossus => "soldier:RailColossus", _ => "" };
        if (key.Length == 0 || !suppliedTextures.TryGetValue(key, out var texture) ||
            !suppliedEntries.TryGetValue(key, out var entry) ||
            entry.ReferenceHeight <= 0 || entry.Cell <= 0 || entry.Columns <= 0 || entry.Directions.Length == 0)
            return false;
        float degrees = ((MathHelper.ToDegrees(angle) + 90) % 360 + 360) % 360;
        bool mirror = degrees > 180;
        if (mirror) degrees = 360 - degrees;
        var direction = entry.Directions[0];
        foreach (var candidate in entry.Directions)
            if (Math.Abs(candidate.Angle - degrees) < Math.Abs(direction.Angle - degrees)) direction = candidate;
        if (direction.Count <= 0) return false;
        var phase = gaitCycles - Math.Floor(gaitCycles);
        int frame = direction.Start + Math.Min(direction.Count - 1, (int)(phase * direction.Count));
        var source = new Rectangle(frame % entry.Columns * entry.Cell, frame / entry.Columns * entry.Cell, entry.Cell, entry.Cell);
        var anchor = new Vector2(mirror ? entry.Cell - entry.AnchorX : entry.AnchorX, entry.AnchorY);
        bool human = kind is EnemyKind.Infantry or EnemyKind.Runner or EnemyKind.Heavy;
        float height = kind switch
        {
            EnemyKind.Heavy => 1.26f, EnemyKind.Runner => 1.1f,
            EnemyKind.Motorcycle => 1.28f, EnemyKind.Tank => 1.35f,
            EnemyKind.Helicopter => 1.65f, EnemyKind.Plane => 1.65f,
            EnemyKind.RailCart => 1.4f, EnemyKind.HeavyTank => 1.9f,
            EnemyKind.AirFortress => 2.15f, EnemyKind.RailColossus => 1.95f, _ => 1.16f
        };
        // Native perspective frames handle turns; suspension stays small enough to keep wheels grounded.
        float phaseAngle = (float)(gaitCycles * MathHelper.TwoPi);
        if (!human)
        {
            float bounce = kind is EnemyKind.Helicopter or EnemyKind.Plane or EnemyKind.AirFortress
                ? .018f : kind == EnemyKind.Motorcycle ? .012f : .006f;
            bool air = kind is EnemyKind.Helicopter or EnemyKind.Plane or EnemyKind.AirFortress;
            foot.Y += MathF.Sin(phaseAngle * (air ? .15f : 2)) * cell * bounce;
        }
        float scale = cell * height / entry.ReferenceHeight;
        if (frame < entry.HealthOffsets.Length) healthY = foot.Y + entry.HealthOffsets[frame] * scale - cell * .07f;
        Batch.Draw(texture, foot, source, tint ?? Color.White, 0, anchor, scale,
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        return true;
    }
}
