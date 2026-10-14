using TornMod.Common;

namespace TornMod.Content.Projectiles;

public class SlipperonChildProjectile : MomsSlipperProjectile
{
    protected override int StraightTicks => 0;
    protected override float TargetRange => WeaponBalance.SplitTargetRange;
    protected override float TurnDegrees => WeaponBalance.SplitTurnDegrees;
}

