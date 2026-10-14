using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common;
using TornMod.Common.SaberCombat;
using TornMod.Content.Items;

namespace TornMod.Content.Projectiles;

public class SaberChargeProjectile : ModProjectile
{
    public void Release(Player player)
    {
        if (Projectile.ai[2] == 1f || Projectile.owner != Main.myPlayer) return;
        if (!player.GetModPlayer<SaberCombatPlayer>().CanChargeDash()) { Projectile.Kill(); return; }
        Projectile.ai[0] = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction).ToRotation();
        float charge = SaberMechanics.ChargeFraction(Projectile.ai[1], WeaponBalance.SaberChargeTicks);
        Projectile.ai[2] = 1f;
        float range = SaberMechanics.ChargedDashRange(charge,
            WeaponBalance.SaberMinDashDistance, WeaponBalance.SaberMaxDashDistance);
        int damage = System.Math.Max(1, (int)System.MathF.Round(Projectile.damage *
            SaberMechanics.DashDamageScale(charge, WeaponBalance.SaberChargedCritMultiplier)));
        player.GetModPlayer<SaberCombatPlayer>().StartDash(Projectile.ai[0].ToRotationVector2(), range, damage, Projectile.knockBack);
        Projectile.netUpdate = true;
    }
    public override string Texture => "TornMod/Content/Items/Saber";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.penetrate = -1;
        Projectile.aiStyle = -1;
        Projectile.DamageType = ModContent.GetInstance<SaberDamageClass>();
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        var combat = player.GetModPlayer<SaberCombatPlayer>();
        if (!player.active || player.dead || !SaberCombatPlayer.IsSaber(player.HeldItem))
        {
            if (Projectile.owner == Main.myPlayer) combat.CancelDash();
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 2;
        Projectile.Center = player.MountedCenter;
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        if (Projectile.ai[2] == 1f)
        {
            if (Projectile.owner == Main.myPlayer && !combat.Dashing) Projectile.Kill();
            return;
        }
        if (Projectile.owner == Main.myPlayer)
        {
            Projectile.ai[0] = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction).ToRotation();
            if (!player.controlUseTile)
            {
                Release(player);
                return;
            }
            if ((int)Projectile.ai[1] % 10 == 0) Projectile.netUpdate = true;
        }
        player.ChangeDir(System.MathF.Cos(Projectile.ai[0]) >= 0f ? 1 : -1);
        Projectile.ai[1] = System.Math.Min(Projectile.ai[1] + 1f, WeaponBalance.SaberChargeTicks);
        if (!Main.dedServ && Projectile.ai[1] == WeaponBalance.SaberChargeTicks && Projectile.localAI[0] == 0f)
        {
            Projectile.localAI[0] = 1f;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.5f }, player.Center);
            for (int i = 0; i < 10; i++)
                Dust.NewDustPerfect(player.MountedCenter, DustID.TintableDustLighted,
                    (i * MathHelper.TwoPi / 10f).ToRotationVector2() * 2f, 100, Color.LightSkyBlue, 0.7f).noGravity = true;
        }
        if (Projectile.owner == Main.myPlayer && Projectile.ai[1] >= WeaponBalance.SaberChargeTicks) Release(player);
    }
}


