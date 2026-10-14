using Terraria.ModLoader;
using TornMod.Content.Items;
using TornMod.Content.Projectiles;

namespace TornMod.Common.BoltAction;

public class BoltActionPlayer : ModPlayer
{
    private bool Reloading => !Player.dead && Player.HeldItem.type == ModContent.ItemType<Piercer>() &&
        Player.ownedProjectileCounts[ModContent.ProjectileType<PiercerReloadProjectile>()] > 0;
    public override void PostUpdateEquips()
    {
        if (Reloading) Player.jumpSpeedBoost += (Terraria.Player.jumpSpeed + Player.jumpSpeedBoost) * 0.2f;
    }
    public override void PostUpdateRunSpeeds()
    {
        if (!Reloading) return;
        Player.maxRunSpeed *= 1.5f;
        Player.accRunSpeed *= 1.5f;
        Player.runAcceleration *= 1.5f;
    }
}
