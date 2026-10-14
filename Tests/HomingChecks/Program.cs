using System;
using System.Numerics;
using TornMod.Common;
using TornMod.Common.SaberCombat;
using TornMod.Common.Glider;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var glider = new GliderController();
glider.Capture(Vector2.Zero);
Check(glider.Step(1, 0, false).Y > 0f, "Glider never launches a stationary player upward");
Check(!GliderController.CanDeploy(true, true, true, false, 0), "Grounded Space does not activate glider flight");
Check(!GliderController.CanDeploy(true, false, false, false, 1), "Releasing Space restores normal movement");
Check(!GliderController.CanDeploy(true, true, false, true, 1), "Competing flight and movement systems block gliding");
glider.Capture(new Vector2(12f, 0.35f));
for (int tick = 0; tick < 120; tick++)
{
    Vector2 before = glider.Velocity;
    Vector2 next = glider.Step(1, 0, false);
    Check(next.Y > 0 && float.IsFinite(next.X) && float.IsFinite(next.Y), "Fast glide always descends and stays finite");
    Check(next.Length() <= before.Length() + GliderTuning.Gravity + 0.001f, "Gliding creates no energy beyond downward gravity");
}
glider.Capture(new Vector2(8f, 0.5f));
float initialDiveSpeed = glider.Velocity.Length();
for (int tick = 0; tick < 50; tick++) glider.Step(1, 0, true);
Check(glider.Velocity.Length() > initialDiveSpeed && glider.Velocity.Y > 3f, "Dive gains speed through descent");
float diveHorizontal = glider.Velocity.X;
for (int tick = 0; tick < 50; tick++) glider.Step(1, 0, false);
Check(glider.Velocity.X > diveHorizontal, "Pull-out converts downward momentum into horizontal speed");
var slowGlide = new GliderController(); slowGlide.Capture(new Vector2(1f, 0.5f));
var fastGlide = new GliderController(); fastGlide.Capture(new Vector2(12f, 0.5f));
float slowDrop = 0, fastDrop = 0;
for (int tick = 0; tick < 120; tick++)
{
    slowDrop += slowGlide.Step(1, 0, false).Y;
    fastDrop += fastGlide.Step(1, 0, false).Y;
}
Check(fastDrop < slowDrop, "Horizontal airspeed provides stronger lift and less altitude loss");
Check(!GliderController.CanDeploy(true, true, false, false, -0.01f), "Glide does not cancel an upward jump");
Check(!GliderController.ValidSnapshot(new Vector2(float.NaN, 0)) &&
    !GliderController.ValidSnapshot(new Vector2(0, float.PositiveInfinity)) &&
    !GliderController.ValidSnapshot(new Vector2(23, 0)) && !GliderController.ValidSnapshot(new Vector2(0, -1)),
    "Network snapshots reject nonfinite, excessive and upward velocities");
Check(GliderController.ValidSnapshot(new Vector2(22, 0)), "Maximum legal glide speed survives networking");
float totalDescent = 0;
glider.Capture(new Vector2(10, 0.5f));
for (int tick = 0; tick < 18000; tick++)
{
    bool diving = tick % 180 < 60;
    Vector2 before = glider.Velocity;
    Vector2 after = glider.Step(1, tick % 1200 < 600 ? 1 : -1, diving);
    Check(after.Y > 0 && after.Length() <= GliderTuning.MaxSpeed + 0.001f,
        "Five-minute dive/pull-out/reversal run always descends within the speed cap");
    Check(after.Length() <= before.Length() + (diving ? GliderTuning.DiveGravity : GliderTuning.Gravity) + 0.001f,
        "Alternating dive and steering cannot inject propulsion energy");
    totalDescent += after.Y;
}
Check(totalDescent > 18000 * 0.01f, "Repeated dive/pull-out cycles cannot maintain altitude indefinitely");
var mirroredGlider = new GliderController();
glider.Capture(new Vector2(10, 1)); mirroredGlider.Capture(new Vector2(-10, 1));
for (int tick = 0; tick < 300; tick++)
{
    var right = glider.Step(1, 1, tick < 100);
    var left = mirroredGlider.Step(-1, -1, tick < 100);
    Check(MathF.Abs(right.X + left.X) < 0.002f && MathF.Abs(right.Y - left.Y) < 0.002f,
        "Glide and dive are symmetric to the left and right");
}
for (int i = 0; i < 1000; i++) glider.Step(1, 1, false);
long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < 10000; i++) glider.Step(1, 1, i % 120 < 30);
long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
Check(allocatedAfter == allocatedBefore, "Steady-state glide physics allocates zero managed bytes");

// Acrobatics scenarios. Counts below are repeated numerical checks, not independent play sessions.
var acrobat = new GliderController();
Check(GliderController.CanToggleMode(true, true, true, false), "Airborne held-Jump glider context allows W mode switching");
Check(!GliderController.CanToggleMode(false, true, true, false) && !GliderController.CanToggleMode(true, false, true, false) &&
    !GliderController.CanToggleMode(true, true, false, false) && !GliderController.CanToggleMode(true, true, true, true),
    "Mode switching leaves unequipped, normal, grounded and blocked W controls alone");
acrobat.Capture(new Vector2(20, 0));
Check(acrobat.StepAcrobatics(MathF.PI, false).X > 19f, "Mouse behind the player cannot reverse momentum instantly");
acrobat.Capture(Vector2.Zero);
Check(acrobat.StepAcrobatics(-MathF.PI / 2, false).Y >= 0, "An unpowered stationary acrobat cannot climb");
acrobat.Capture(Vector2.Zero);
Check(acrobat.StepAcrobatics(-MathF.PI / 2, true).Y < 0, "Authorized rocket thrust can launch upward");
acrobat.Capture(Vector2.Zero);
Check(acrobat.StepAcrobatics(0, true).X > 0, "Rocket thrust can accelerate horizontally");
acrobat.Capture(Vector2.Zero);
Check(acrobat.StepAcrobatics(MathF.PI / 2, true).Y > GliderTuning.AcrobaticsGravity, "Downward thrust adds to a dive");
acrobat.Capture(new Vector2(0, -12));
float acrobatY = 0, highestY = 0;
float startingEnergy = acrobat.Velocity.LengthSquared() / 2;
for (int tick = 0; tick < 900; tick++)
{
    float target = tick < 180 ? -MathF.PI / 2 : tick * 0.04f;
    Vector2 velocity = acrobat.StepAcrobatics(target, false);
    acrobatY += velocity.Y; highestY = Math.Min(highestY, acrobatY);
    float energy = velocity.LengthSquared() / 2 - GliderTuning.AcrobaticsGravity * acrobatY;
    Check(energy <= startingEnergy + 0.03f && GliderController.ValidSnapshot(velocity, true),
        "Climbing and loops cannot create energy or exceed the existing speed cap");
}
Check(highestY < -10 && -highestY <= startingEnergy / GliderTuning.AcrobaticsGravity + 0.2f,
    "Upward pull can spend momentum for finite altitude gain");
acrobat.Capture(new Vector2(12, 0));
for (int tick = 0; tick < 60; tick++) acrobat.StepAcrobatics(MathF.PI / 2, false);
float acrobatDiveX = acrobat.Velocity.X;
Check(acrobat.Velocity.Length() > 12, "Mouse-directed dive gains speed");
for (int tick = 0; tick < 60; tick++) acrobat.StepAcrobatics(0, false);
Check(acrobat.Velocity.X > acrobatDiveX, "Acrobat pull-out converts dive speed to traversal");
float roll = 0, rollTravel = 0;
for (int tick = 0; tick < 900; tick++)
{
    float next = GliderController.SmoothAngle(roll, tick * 0.025f);
    Check(MathF.Abs(next - roll) < 0.21f, "Full-circle poses take the smooth shortest angular path");
    rollTravel += next - roll; roll = next;
}
Check(rollTravel > MathF.Tau * 3, "Pose supports multiple complete 360 degree rolls");
acrobat.Capture(new Vector2(18, -4));
var coast = acrobat.Coast();
Check(coast.X > 17 && coast.Y < 0, "Switching to Normal retains forward and upward momentum");
long acrobatAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
for (int tick = 0; tick < 10000; tick++) acrobat.StepAcrobatics(tick * 0.01f, tick < 20);
Check(GC.GetAllocatedBytesForCurrentThread() == acrobatAllocatedBefore, "Acrobatics controller allocates no per-step managed memory");

float maximum = 3f * MathF.PI / 180f;
for (int age = 0; age < 10; age++)
    Check(!HomingMath.CanAcquire(age, 10, HomingMath.Searching), "First ten ticks fly straight");
Check(HomingMath.CanAcquire(10, 10, HomingMath.Searching), "Acquisition begins on tick eleven");
Check(!HomingMath.CanAcquire(99, 10, 5), "An acquired target cannot be replaced");
Check(!HomingMath.CanAcquire(99, 10, HomingMath.Lost), "Lost lock never reacquires");
Check(HomingMath.IsCloser(400f * 400f, 400f * 400f, false), "Range includes its boundary");
Check(!HomingMath.IsCloser(400.01f * 400.01f, 400f * 400f, false), "Out-of-range enemy excluded");
Check(!HomingMath.IsCloser(160000, 10000, true), "Farther enemy cannot replace closest");
Check(HomingMath.TurnTowards(0f, 0f, maximum) == 0f, "Enemy directly ahead does not bend flight");
Check(MathF.Abs(HomingMath.TurnTowards(0f, MathF.PI, maximum) - maximum) < 0.00001f, "Enemy behind turns by only three degrees");
float seam = HomingMath.TurnTowards(179f * MathF.PI / 180f, -179f * MathF.PI / 180f, maximum);
Check(MathF.Abs(HomingMath.WrapAngle(seam + 179f * MathF.PI / 180f)) < 0.00001f, "Crossing the angle seam follows shortest arc");
// Broad angular sweep checks the shared implementation instead of duplicating it.
for (int i = -180; i <= 180; i += 3)
for (int j = -180; j <= 180; j += 3)
{
    float from = i * MathF.PI / 180f;
    float to = j * MathF.PI / 180f;
    float next = HomingMath.TurnTowards(from, to, maximum);
    Check(MathF.Abs(HomingMath.WrapAngle(next - from)) <= maximum + 0.00001f, "Turn rate limit");
    var velocity = new Vector2(MathF.Cos(next), MathF.Sin(next)) * 11f;
    Check(MathF.Abs(velocity.Length() - 11f) < 0.00001f, "Constant speed");
    Check(MathF.Abs(HomingMath.WrapAngle(to - next)) <= MathF.Abs(HomingMath.WrapAngle(to - from)) + 0.00001f, "No turn away from target");
}
Check(!SaberMechanics.IsCharged(179, 180), "179 ticks is not fully charged");
Check(SaberMechanics.IsCharged(180, 180), "180 ticks reaches full charge");
Check(SaberMechanics.IsCharged(60, 60) && !SaberMechanics.IsCharged(59, 60), "One-second charge threshold");
Check(SaberMechanics.DashDamageScale(0, 2) == 0.5f && SaberMechanics.DashDamageScale(1, 2) == 1f,
    "Dash damage is halved at minimum and full charge");
Check(SaberMechanics.SwingOffset(0) < 0f && SaberMechanics.SwingOffset(1) > SaberMechanics.SwingOffset(0), "Unsheathing cut advances immediately");
Check(SaberMechanics.SwingOffset(3) == 0f && SaberMechanics.SwingOffset(30) == 0f, "Fast cut finishes on the aim after three ticks");
Check(SaberMechanics.ChargeFraction(90, 180) == 0.5f, "Half charge at 90 ticks");
Check(SaberMechanics.ChargeFraction(300, 180) == 1f, "Charge cannot exceed full strength");
foreach (float charge in new[] { 0f, 0.5f, 1f })
{
    float range = SaberMechanics.ChargedDashRange(charge, 56f, 336f);
    Check(range >= 56f && range <= 336f, "Charge stays inside dash bounds");
    float budget = range;
    float speed = SaberMechanics.InitialDashSpeed(range, 0.85f);
    float total = 0f;
    int ticks = 0;
    while (budget > 0.001f && ticks++ < 6)
    {
        float step = SaberMechanics.NextDashStep(budget, ticks == 6 ? budget : speed);
        Check(step > 0 && step <= budget, "Physical dash advances without overshoot");
        total += step;
        budget -= step;
        speed *= 0.85f;
    }
    Check(ticks <= 6 && MathF.Abs(total - range) < 0.001f, "Fast dash reaches its bounded distance within six ticks");
}
float remaining = 963f;
float moved = 0f;
while (remaining > 0)
{
    float step = SaberMechanics.NextDashStep(remaining, 40f);
    Check(step <= 40f && step <= remaining, "Dash step never overshoots");
    moved += step;
    remaining -= step;
}
Check(moved == 963f, "Dash completes exactly at its maximum distance");
var fall = new FallProtectionState();
Check(SaberMechanics.CanGroundDash(0, 1, 0), "Standing on floor permits dash");
Check(!SaberMechanics.CanGroundDash(0, 1, 2), "Airborne apex without support blocks dash");
Check(!SaberMechanics.CanGroundDash(-2, 1, 0), "Jumping blocks dash even near floor");
Check(!SaberMechanics.CanGroundDash(2, 1, 2), "Falling without ground support blocks dash");
Check(SaberMechanics.CanGroundDash(0.4f, 1, 0), "Gravity applied before movement does not cancel grounded dash release");
Check(SaberMechanics.CanGroundDash(-0.4f, -1, 0), "Grounded dash tolerates pre-movement gravity with reversed gravity");
Check(!SaberMechanics.CanGroundDash(-0.4f, 1, 0), "Jumping away from support still prevents dash");
Check(SaberMechanics.CanGroundDash(0, -1, 0) && !SaberMechanics.CanGroundDash(0, -1, -2), "Ground check respects reversed gravity");
Check(SaberMechanics.TrySlide(0.99f, 0.1f, 3.96f, 0.4f, 3.96f, 0f, out float slideX, out float slideY) &&
    slideX == 1f && slideY == 0f, "Slight downward aim slides at full speed along the floor");
Check(SaberMechanics.TrySlide(-0.99f, 0.1f, -3.96f, 0.4f, -3.96f, 0f, out slideX, out slideY) &&
    slideX == -1f && slideY == 0f, "Leftward ground dash preserves its direction");
Check(SaberMechanics.TrySlide(0.6f, -0.8f, 2.4f, -3.2f, 0f, -3.2f, out slideX, out slideY) &&
    slideX == 0f && slideY == -1f, "Diagonal wall impact slides upward");
Check(!SaberMechanics.TrySlide(1f, 0f, 4f, 0f, 0f, 0f, out slideX, out slideY), "Head-on wall stops rather than inventing a direction");
Check(!SaberMechanics.TrySlide(0.6f, 0.8f, 2.4f, 3.2f, 0f, 0f, out slideX, out slideY), "Closed corner stops both axes");
Check(!fall.TryConsume(0), "No fall immunity before successful hit");
fall.Grant();
Check(!fall.TryConsume(1) && fall.Armed, "Non-fall hurt preserves token");
Check(fall.TryConsume(0) && !fall.Armed, "First fall consumes protection");
Check(!fall.TryConsume(0), "Protection is single-use");
fall.Grant();
Check(fall.TryConsume(0), "New charged hit rearms protection");
Console.WriteLine($"PASS: {checks} assertions. Engine collision, crafting UI and multiplayer still require in-game checks.");
