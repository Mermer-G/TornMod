using Terraria.ModLoader;

namespace TornMod.Common.BoltAction;

public class BoltActionDamageClass : DamageClass
{
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass) =>
        damageClass == Generic || damageClass == Ranged ? StatInheritanceData.Full : StatInheritanceData.None;
    public override bool GetEffectInheritance(DamageClass damageClass) => damageClass == Ranged;
}
