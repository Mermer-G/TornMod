using System;

namespace TornMod.Common;

// Engine-independent math is shared with the regression checks.
public static class HomingMath
{
    public const float Searching = 0f;
    public const float Lost = -1f;

    public static bool CanAcquire(float age, int delay, float targetState) =>
        age >= delay && targetState == Searching;

    public static float WrapAngle(float angle)
    {
        float wrapped = angle % MathF.Tau;
        if (wrapped > MathF.PI) wrapped -= MathF.Tau;
        if (wrapped < -MathF.PI) wrapped += MathF.Tau;
        return wrapped;
    }

    public static float TurnTowards(float current, float desired, float maximumRadians) =>
        WrapAngle(current + Math.Clamp(WrapAngle(desired - current), -maximumRadians, maximumRadians));

    public static bool IsCloser(float distanceSquared, float bestSquared, bool hasTarget) =>
        distanceSquared >= 0f && (hasTarget ? distanceSquared < bestSquared : distanceSquared <= bestSquared);
}
