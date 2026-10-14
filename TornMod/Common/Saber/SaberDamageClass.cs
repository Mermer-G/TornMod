using Terraria.ModLoader;

namespace TornMod.Common.SaberCombat;

// Accessories can grant Saber-only bonuses through player.GetDamage<SaberDamageClass>()
// and player.GetAttackSpeed<SaberDamageClass>(), without buffing other melee weapons.
public class SaberDamageClass : DamageClass
{
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass) =>
        damageClass == Generic || damageClass == Melee ? StatInheritanceData.Full : StatInheritanceData.None;
    public override bool GetEffectInheritance(DamageClass damageClass) => damageClass == Melee;
}
