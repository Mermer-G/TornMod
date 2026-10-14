using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common;

namespace TornMod.Content.Projectiles;

public class MomsSlipperProjectile : ModProjectile
{
    public override string Texture => "TornMod/Content/Items/MomsSlipper";
    private NPC lockedNpc;
    protected virtual int StraightTicks => WeaponBalance.MomStraightTicks;
    protected virtual float TargetRange => WeaponBalance.MomTargetRange;
    protected virtual float TurnDegrees => WeaponBalance.MomTurnDegrees;

    public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = WeaponBalance.MomLifetime;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.aiStyle = -1;
    }

    public override void AI()
    {
        
        // ai[0]: searching=0, locked=NPC slot+1, permanently lost=-1.
        // ai[1]: age. Only the owner selects/invalidates a lock and sends it to peers.
        if (HomingMath.CanAcquire(Projectile.ai[1], StraightTicks, Projectile.ai[0]) &&
            Projectile.owner == Main.myPlayer && ((int)Projectile.ai[1] - StraightTicks) % 6 == 0)
        {
            int target = Homing.FindClosest(Projectile.Center, TargetRange);
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
                // A recycled NPC slot must not count as the original locked enemy.
                valid &= lockedNpc == target;
                if (!valid)
                {
                    Projectile.ai[0] = HomingMath.Lost;
                    Projectile.netUpdate = true;
                }
            }
            if (valid)
                Projectile.velocity = Homing.Steer(Projectile.velocity, target.Center - Projectile.Center, TurnDegrees);
        }
        Projectile.ai[1]++;
        // Visual spin is independent of the velocity used for homing and collision.
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + Projectile.ai[1] * 0.4f;
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.65f, Pitch = -0.25f }, Projectile.Center);
        for (int i = 0; i < 14; i++)
        {
            Vector2 direction = (MathHelper.TwoPi * i / 14f).ToRotationVector2();
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.WoodFurniture,
                direction * Main.rand.NextFloat(1.5f, 3.5f), 80, default, Main.rand.NextFloat(0.7f, 1.1f));
            dust.noGravity = true;
        }
    }

    public override bool PreDraw(ref Color lightColor) => WeaponDrawing.DrawProjectile(Projectile, lightColor, 40f);
}


