using System;

namespace TornMod.Common.SaberCombat;

public static class SaberMechanics
{
    public static bool CanGroundDash(float verticalVelocity, float gravityDirection, float allowedVerticalStep) =>
        verticalVelocity * gravityDirection >= -0.01f && allowedVerticalStep * gravityDirection < 1.999f;
    public static bool IsCharged(float heldTicks, int requiredTicks) => heldTicks >= requiredTicks;
    public static float DashRange(float screenWidth, float zoom, float fraction) =>
        Math.Clamp(screenWidth / Math.Max(zoom, 0.1f) * fraction, 1f, 4000f);
    public static float NextDashStep(float remaining, float speed) => Math.Max(0f, Math.Min(remaining, speed));
    public static float ChargeFraction(float ticks, int fullTicks) => Math.Clamp(ticks / Math.Max(1, fullTicks), 0f, 1f);
    public static float DashDamageScale(float charge, float fullMultiplier) =>
        0.5f * (1f + Math.Clamp(charge, 0f, 1f) * (fullMultiplier - 1f));
    public static float ChargedDashRange(float charge, float minimum, float maximum) =>
        minimum + (Math.Max(minimum, maximum) - minimum) * Math.Clamp(charge, 0f, 1f);
    public static float InitialDashSpeed(float range, float retention, int ticks = 6) =>
        range * (1f - retention) / (1f - MathF.Pow(retention, Math.Max(1, ticks)));
    // Remove only the axis rejected by tile collision; preserve speed along the open surface.
    public static bool TrySlide(float directionX, float directionY, float requestedX, float requestedY,
        float actualX, float actualY, out float slideX, out float slideY)
    {
        slideX = MathF.Abs(requestedX - actualX) > 0.001f ? 0f : directionX;
        slideY = MathF.Abs(requestedY - actualY) > 0.001f ? 0f : directionY;
        float length = MathF.Sqrt(slideX * slideX + slideY * slideY);
        if (length < 0.001f) return false;
        slideX /= length;
        slideY /= length;
        return true;
    }
    // The draw happens immediately: finish the cut in three ticks, then hold its endpoint.
    public static float SwingOffset(float ticks)
    {
        float remaining = 1f - Math.Clamp(ticks / 3f, 0f, 1f);
        return -0.6f * remaining * remaining * remaining;
    }
}

public class FallProtectionState
{
    public bool Armed { get; private set; }
    public void Grant() => Armed = true;
    public void Clear() => Armed = false;
    public bool TryConsume(int damageSourceOtherIndex)
    {
        if (!Armed || damageSourceOtherIndex != 0) return false;
        Armed = false;
        return true;
    }
}

