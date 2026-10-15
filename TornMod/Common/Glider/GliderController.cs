using System;
using System.Numerics;

namespace TornMod.Common.Glider;

public static class GliderTuning
{
    public const float Gravity = 0.18f;
    public const float Drag = 0.996f;
    public const float MinSink = 0.35f;
    public const float StallSink = 3.8f;
    public const float LiftSpeed = 7f;
    public const float MaxSpeed = 22f;
    public const float TurnRate = 0.035f;
    public const float DiveGravity = 0.36f;
    public const float DivePitch = 1.15f;
    public const float DiveDrag = 0.999f;
    public const float AcrobaticsTurnRate = 0.055f;
    public const float AcrobaticsDrag = 0.996f;
    public const float AcrobaticsGravity = 0.18f;
    public const float BoostAcceleration = 0.22f;
    public const float PoseSmoothing = 0.12f;
    public const float MaxExternalSpeed = 256f;
}

// Canonical velocity uses positive Y downward, regardless of the player's gravity direction.
// Capture is the boundary between vanilla collisions/impulses and predicted flight.
public sealed class GliderController
{
    public Vector2 Velocity { get; private set; }
    private float externalLimit = GliderTuning.MaxSpeed;
    public bool ExternalMomentumActive => externalLimit > GliderTuning.MaxSpeed;
    public void Capture(Vector2 velocity) => Velocity = velocity;
    public void CaptureExternal(Vector2 velocity)
    {
        Capture(velocity);
        externalLimit = Math.Clamp(velocity.Length(), GliderTuning.MaxSpeed, GliderTuning.MaxExternalSpeed);
    }
    public void ClearExternalMomentum() => externalLimit = GliderTuning.MaxSpeed;
    private float MovementLimit()
    {
        externalLimit = Math.Max(GliderTuning.MaxSpeed, Math.Min(externalLimit, Velocity.Length()) * GliderTuning.Drag);
        return externalLimit;
    }
    public Vector2 Step(int facing, int steering, bool dive)
    {
        Vector2 velocity = Velocity;
        velocity.Y = Math.Max(0f, velocity.Y) + (dive ? GliderTuning.DiveGravity : GliderTuning.Gravity);
        velocity *= dive ? GliderTuning.DiveDrag : GliderTuning.Drag;
        float speed = Math.Min(MovementLimit(), velocity.Length());
        float lift = Math.Clamp(MathF.Abs(velocity.X) / GliderTuning.LiftSpeed, 0f, 1f);
        float sink = GliderTuning.StallSink + (GliderTuning.MinSink - GliderTuning.StallSink) * lift;
        int heading = steering != 0 ? steering : MathF.Abs(velocity.X) > 0.05f ? Math.Sign(velocity.X) : facing;
        float pitch = dive ? GliderTuning.DivePitch : MathF.Asin(Math.Clamp(sink / Math.Max(speed, 0.001f), 0f, 1f));
        float target = heading >= 0 ? pitch : MathF.PI - pitch;
        float angle = MathF.Atan2(velocity.Y, velocity.X);
        float turn = Math.Clamp(target - angle, -GliderTuning.TurnRate, GliderTuning.TurnRate);
        // Rotate existing speed; pull-out changes its direction, never multiplies its magnitude.
        angle = Math.Clamp(angle + turn, 0.001f, MathF.PI - 0.001f);
        Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
        return Velocity;
    }
    public static bool CanDeploy(bool equipped, bool jumpHeld, bool supported, bool blocked, float downwardVelocity) =>
        equipped && jumpHeld && !supported && !blocked && downwardVelocity >= 0f;
    public static bool CanToggleMode(bool equipped, bool jumpHeld, bool airborne, bool blocked) =>
        equipped && jumpHeld && airborne && !blocked;
    public Vector2 StepAcrobatics(float targetAngle, bool powered)
    {
        Vector2 target = new(MathF.Cos(targetAngle), MathF.Sin(targetAngle));
        Vector2 velocity = Velocity * GliderTuning.AcrobaticsDrag;
        if (powered) velocity += target * GliderTuning.BoostAcceleration;
        float limit = MovementLimit();
        float speed = Math.Min(limit, velocity.Length());
        float angle = MathF.Atan2(velocity.Y, velocity.X);
        float authority = Math.Clamp(speed / GliderTuning.LiftSpeed, 0.15f, 1f);
        angle += Math.Clamp(WrapAngle(targetAngle - angle), -GliderTuning.AcrobaticsTurnRate * authority,
            GliderTuning.AcrobaticsTurnRate * authority);
        Vector2 candidate = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed +
            new Vector2(0, GliderTuning.AcrobaticsGravity);
        Vector2 direction = candidate.LengthSquared() > 0.000001f ? Vector2.Normalize(candidate) : Vector2.UnitY;
        // Solve s²/2 - gravity*dy*s = previousSpeed²/2 for the final displacement.
        // Climbing spends kinetic energy; steering loops cannot create free altitude.
        float gravityAlong = GliderTuning.AcrobaticsGravity * direction.Y;
        float finalSpeed = gravityAlong + MathF.Sqrt(speed * speed + gravityAlong * gravityAlong);
        Velocity = direction * Math.Min(limit, finalSpeed);
        return Velocity;
    }
    public Vector2 Coast()
    {
        // When switching back during a climb, retain momentum until falling resumes.
        Velocity = (Velocity + new Vector2(0, GliderTuning.Gravity)) * GliderTuning.Drag;
        float limit = MovementLimit();
        if (Velocity.LengthSquared() > limit * limit)
            Velocity = Vector2.Normalize(Velocity) * limit;
        return Velocity;
    }
    public static float WrapAngle(float angle) => MathF.IEEERemainder(angle, MathF.Tau);
    public static float SmoothAngle(float current, float target) => current + WrapAngle(target - current) * GliderTuning.PoseSmoothing;
    public static bool ValidSnapshot(Vector2 velocity, bool allowUpward = false, bool externalMomentum = false) => float.IsFinite(velocity.X) && float.IsFinite(velocity.Y) &&
        (allowUpward || velocity.Y >= 0f) && velocity.LengthSquared() <=
        MathF.Pow((externalMomentum ? GliderTuning.MaxExternalSpeed : GliderTuning.MaxSpeed) + 0.01f, 2);
}
