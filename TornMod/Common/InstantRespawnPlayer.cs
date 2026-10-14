using Terraria.ModLoader;
using Terraria.DataStructures;

namespace TornMod.Common;

public class InstantRespawnPlayer : ModPlayer
{
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) =>
        WeaponConfig.Current.Invincibility;
    public override void UpdateDead()
    {
        if (WeaponConfig.Current.InstantRespawn && Player.difficulty != 2 && Player.respawnTimer > 1)
            Player.respawnTimer = 1;
    }
}
