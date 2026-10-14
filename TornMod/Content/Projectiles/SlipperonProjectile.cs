using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common;

namespace TornMod.Content.Projectiles;

public class SlipperonProjectile : ModProjectile
{
    public override string Texture => "TornMod/Content/Items/Slipperon";
    private NPC lockedNpc;
    private bool Returning => Projectile.ai[2] == 1f;

    public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 600;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.aiStyle = -1;
        Projectile.usesLocalNPCImmunity = true;
        // Per-enemy cooldown: any number of different NPCs can be hit on return.
        // The same NPC can be hit again after 10 ticks, without consuming penetration.
        Projectile.localNPCHitCooldown = 10;
    }

    private void BeginReturn()
    {
        if (Returning) return;
        Projectile.ai[2] = 1f;
        Projectile.tileCollide = false;
        Projectile.friendly = true;
        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
    }

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
        {
            Projectile.Kill();
            return;
        }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead)
        {
            Projectile.Kill();
            return;
        }
        
        Projectile.rotation += 0.35f * (Projectile.velocity.X >= 0f ? 1f : -1f);

        if (!Returning && (Projectile.ai[1] >= WeaponBalance.SlipperonOutboundTicks ||
            Vector2.DistanceSquared(Projectile.Center, owner.Center) > WeaponBalance.SlipperonMaxDistance * WeaponBalance.SlipperonMaxDistance))
            BeginReturn();

        if (Returning)
        {
            // Restore collision/damage state on peers receiving a return packet.
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Vector2 offset = owner.Center - Projectile.Center;
            float catchDistance = WeaponBalance.SlipperonReturnSpeed + 16f;
            if (offset.LengthSquared() <= catchDistance * catchDistance)
            {
                Projectile.Kill();
                return;
            }
            Projectile.velocity = offset.SafeNormalize(Vector2.UnitY) * WeaponBalance.SlipperonReturnSpeed;
            return;
        }

        if (Projectile.ai[0] == HomingMath.Searching && Projectile.owner == Main.myPlayer &&
            (int)Projectile.ai[1] % 6 == 0)
        {
            int target = Homing.FindClosest(Projectile.Center, WeaponBalance.SlipperonTargetRange);
            if (target >= 0)
            {
                Projectile.ai[0] = target + 1;
                lockedNpc = Main.npc[target];
                Projectile.netUpdate = true;
            }
        }
        int slot = (int)Projectile.ai[0] - 1;
        if (slot >= 0 && slot < Main.maxNPCs)
        {
            NPC target = Main.npc[slot];
            bool valid = Homing.IsValid(target);
            if (Projectile.owner == Main.myPlayer)
            {
                valid &= target == lockedNpc;
                if (!valid)
                {
                    Projectile.ai[0] = HomingMath.Lost;
                    Projectile.netUpdate = true;
                }
            }
            if (valid)
                Projectile.velocity = Homing.Steer(Projectile.velocity, target.Center - Projectile.Center, WeaponBalance.SlipperonTurnDegrees);
        }
        Projectile.ai[1]++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => BeginReturn();
    public override void OnHitPlayer(Player target, Player.HurtInfo info) => BeginReturn();
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = oldVelocity;
        BeginReturn();
        return false;
    }
    public override bool PreDraw(ref Color lightColor) => WeaponDrawing.DrawProjectile(Projectile, lightColor, 52f);
}

