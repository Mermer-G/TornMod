using System;
using System.Numerics;

namespace TornMod.Common.Lancer;

public static class LancerTuning
{
    public const int ContactInterval = 10;
    public const int PrepareTicks = 15;
    public const float TurnRadiansPerTick = 540f * MathF.PI / 180f / 60f;
    public const float TipDamageLength = 40f;
    public const int ContactProtectionTicks = 60;
    public const float ProtectionSpeed = 45f / MilesPerHourPerPixelTick;
    public const float ImpactForwardBoost = 50f / MilesPerHourPerPixelTick;
    public const int RetractTicks = 8;
    public const int ThrustAnimationTicks = 12;
    public const int ThrustPeakTick = 4;
    public const float MinimumImpactSpeed = 30f / MilesPerHourPerPixelTick;
    // Terraria speedometer units: 1 pixel = 1/8 foot, 60 ticks/second.
    public const float MilesPerHourPerPixelTick = 60f * 3600f / (8f * 5280f);
    public const float FullImpactSpeed = 100f / MilesPerHourPerPixelTick;
    public const float MinimumImpactBonus = 4f;
    public const float MaximumImpactBonus = 19f;
    public const int ImpactSeparationTicks = 20;
    public const float HitRadius = 3f;
    public const float ReadyExtension = 16f;
    public const float RetractedExtension = -22f;
    public const float QuickExtension = 26f;
    public const float MaximumSweepDistance = 256f;
}

public readonly record struct LancerSettings(int ContactDamage, float Length, int PrepareTicks, int ContactInterval,
    float ContactKnockback, float QuickKnockback, int QuickUseTicks)
{
    public static LancerSettings Iron => new(10, 240, LancerTuning.PrepareTicks, LancerTuning.ContactInterval, 1.5f, 8, 30);
    public float QuickDamageMultiplier => 30f / Math.Max(1, ContactInterval);
    public int QuickDamage => Math.Max(1, (int)MathF.Round(ContactDamage * QuickDamageMultiplier));
}

public enum LancePhase { Preparing, Ready, Retracting, QuickThrust, Finished }

public sealed class LancerController
{
    public LancePhase Phase { get; private set; }
    public int Age { get; private set; }
    public float Aim { get; private set; }
    public LancePhase ResumePhase { get; private set; } = LancePhase.Preparing;
    public int ResumeAge { get; private set; }
    public bool Damaging => Phase is LancePhase.Ready or LancePhase.QuickThrust;
    public void Load(LancePhase phase, int age, float aim) { Phase = phase; Age = age; Aim = aim; }
    public void Retract() { Phase = LancePhase.Retracting; Age = 0; }
    public void SetResume(LancePhase phase, int age) { ResumePhase = phase; ResumeAge = age; }
    public void BeginQuick(float aim)
    {
        SetResume(Phase == LancePhase.Ready ? LancePhase.Ready : LancePhase.Preparing,
            Phase == LancePhase.Preparing ? Age : 0);
        Phase = LancePhase.QuickThrust; Age = 0; Aim = aim;
    }
    public void Step(bool held, float mouseAim, int preparationTicks, bool owner)
    {
        if (Phase == LancePhase.Preparing)
        {
            if (owner) Aim = LancerMechanics.TurnTowards(Aim, mouseAim);
            if (owner && !held) { Retract(); return; }
            Age = Math.Min(Age + 1, preparationTicks);
            if (Age >= preparationTicks && owner) { Phase = LancePhase.Ready; Age = 0; }
        }
        else if (Phase == LancePhase.Ready)
        {
            if (owner) Aim = LancerMechanics.TurnTowards(Aim, mouseAim);
            if (owner && !held) Retract();
        }
        else if (Phase == LancePhase.QuickThrust)
        {
            Age = Math.Min(Age + 1, LancerTuning.ThrustAnimationTicks);
            if (Age >= LancerTuning.ThrustAnimationTicks && owner)
            {
                Phase = held ? ResumePhase : LancePhase.Finished;
                Age = held ? ResumeAge : 0;
            }
        }
        else if (Phase == LancePhase.Retracting)
        {
            if (++Age >= LancerTuning.RetractTicks)
                Phase = LancePhase.Finished;
        }
    }
    public float Extension(int preparationTicks) => Phase switch
    {
        LancePhase.Preparing => LancerTuning.RetractedExtension + (LancerTuning.ReadyExtension - LancerTuning.RetractedExtension) * Math.Clamp(Age / (float)Math.Max(1, preparationTicks), 0, 1),
        LancePhase.Ready => LancerTuning.ReadyExtension,
        LancePhase.Retracting => LancerTuning.ReadyExtension - (LancerTuning.ReadyExtension - LancerTuning.RetractedExtension) * Math.Clamp(Age / (float)LancerTuning.RetractTicks, 0, 1),
        LancePhase.QuickThrust => LancerTuning.RetractedExtension + (LancerTuning.QuickExtension - LancerTuning.RetractedExtension) * (Age <= LancerTuning.ThrustPeakTick ?
            Age / (float)LancerTuning.ThrustPeakTick :
            1f - (Age - LancerTuning.ThrustPeakTick) / (float)(LancerTuning.ThrustAnimationTicks - LancerTuning.ThrustPeakTick)),
        _ => LancerTuning.RetractedExtension
    };
}

public static class LancerMechanics
{
    public static float TurnTowards(float current, float target)
    {
        float delta = MathF.IEEERemainder(target - current, MathF.Tau);
        return MathF.IEEERemainder(current + Math.Clamp(delta, -LancerTuning.TurnRadiansPerTick,
            LancerTuning.TurnRadiansPerTick), MathF.Tau);
    }
    public static Vector2 TipStart(Vector2 tail, Vector2 tip)
    {
        Vector2 shaft = tip - tail;
        float length = shaft.Length();
        return length > 0 ? tip - shaft * (Math.Min(length, LancerTuning.TipDamageLength) / length) : tip;
    }
    public static float ApproachSpeed(Vector2 playerVelocity, Vector2 enemyVelocity, float aim) =>
        Math.Max(0, Vector2.Dot(playerVelocity - enemyVelocity, new Vector2(MathF.Cos(aim), MathF.Sin(aim))));
    public static float ImpactBonus(float speed) => speed < LancerTuning.MinimumImpactSpeed ? 0 :
        LancerTuning.MinimumImpactBonus + (LancerTuning.MaximumImpactBonus - LancerTuning.MinimumImpactBonus) *
        Math.Clamp((speed - LancerTuning.MinimumImpactSpeed) /
            (LancerTuning.FullImpactSpeed - LancerTuning.MinimumImpactSpeed), 0, 1);
    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    private static bool Slab(float origin, float direction, float min, float max, ref float start, ref float end)
    {
        if (MathF.Abs(direction) < 0.000001f) return origin >= min && origin <= max;
        float a = (min - origin) / direction, b = (max - origin) / direction;
        start = Math.Max(start, Math.Min(a, b)); end = Math.Min(end, Math.Max(a, b));
        return start <= end;
    }
    private static bool Segment(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        float start = 0, end = 1;
        return Slab(a.X, b.X - a.X, min.X, max.X, ref start, ref end) &&
            Slab(a.Y, b.Y - a.Y, min.Y, max.Y, ref start, ref end);
    }
    private static bool Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        if (MathF.Abs(Cross(b - a, c - a)) < 0.00001f) return false;
        float x = Cross(b - a, p - a), y = Cross(c - b, p - b), z = Cross(a - c, p - c);
        return (x >= 0 && y >= 0 && z >= 0) || (x <= 0 && y <= 0 && z <= 0);
    }
    private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d) =>
        Triangle(p, a, b, c) || Triangle(p, a, c, d);
    // Four edges plus rectangle-corner containment cover a translated spear's swept quadrilateral.
    // Value types only: no sampling arrays, LINQ or temporary allocations on collision queries.
    public static bool SweptHit(Vector2 oldTail, Vector2 oldTip, Vector2 tail, Vector2 tip,
        Vector2 min, Vector2 max, float radius)
    {
        min -= new Vector2(radius); max += new Vector2(radius);
        return Segment(oldTail, oldTip, min, max) || Segment(oldTip, tip, min, max) ||
            Segment(tip, tail, min, max) || Segment(tail, oldTail, min, max) ||
            Inside(min, oldTail, oldTip, tip, tail) || Inside(max, oldTail, oldTip, tip, tail) ||
            Inside(new Vector2(min.X, max.Y), oldTail, oldTip, tip, tail) ||
            Inside(new Vector2(max.X, min.Y), oldTail, oldTip, tip, tail);
    }
}
