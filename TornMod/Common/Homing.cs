using Microsoft.Xna.Framework;
using Terraria;

namespace TornMod.Common;

public static class Homing
{
    public static bool IsValid(NPC npc) => npc.active && !npc.friendly &&
        !npc.dontTakeDamage && !npc.immortal && npc.CanBeChasedBy();

    public static int FindClosest(Vector2 center, float range)
    {
        int result = -1;
        float bestSquared = range * range;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!IsValid(npc)) continue;
            float distanceSquared = Vector2.DistanceSquared(center, npc.Center);
            if (!HomingMath.IsCloser(distanceSquared, bestSquared, result >= 0)) continue;
            result = i;
            bestSquared = distanceSquared;
        }
        return result;
    }

    public static Vector2 Steer(Vector2 velocity, Vector2 offset, float turnDegrees)
    {
        if (velocity.LengthSquared() < 0.0001f || offset.LengthSquared() < 0.0001f) return velocity;
        float angle = HomingMath.TurnTowards(velocity.ToRotation(), offset.ToRotation(), MathHelper.ToRadians(turnDegrees));
        return angle.ToRotationVector2() * velocity.Length();
    }
}
