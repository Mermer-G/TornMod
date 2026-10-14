using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TornMod.Common.SaberCombat;

// The shared right-click controller applies to every item in the Saber damage subclass.
public class SaberWeaponRules : GlobalItem
{
    public override bool? CanHitNPC(Item item, Player player, NPC target)
    {
        if (!SaberCombatPlayer.IsSaber(item)) return null;
        var combat = player.GetModPlayer<SaberCombatPlayer>();
        return combat.Dashing || player.ownedProjectileCounts[ModContent.ProjectileType<global::TornMod.Content.Projectiles.SaberChargeProjectile>()] > 0 ? false : null;
    }
    public override bool AltFunctionUse(Item item, Player player) => SaberCombatPlayer.IsSaber(item);
    public override bool CanUseItem(Item item, Player player)
    {
        if (!SaberCombatPlayer.IsSaber(item) || player.altFunctionUse != 2) return true;
        return player.GetModPlayer<SaberCombatPlayer>().CanBeginCharge();
    }
    public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (!SaberCombatPlayer.IsSaber(item) || player.altFunctionUse != 2) return true;
        player.GetModPlayer<SaberCombatPlayer>().BeginCharge();
        return false;
    }
}
