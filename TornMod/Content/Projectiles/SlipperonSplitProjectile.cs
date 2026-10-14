using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TornMod.Common;
using TornMod.Content.Items;

namespace TornMod.Content.Projectiles;

public class SlipperonSplitProjectile : ModProjectile
{
    public override string Texture => "TornMod/Content/Items/Slipperon";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 180;
        Projectile.penetrate = -1;
        Projectile.aiStyle = -1;
        Projectile.DamageType = DamageClass.Ranged;
    }
    public override bool? CanDamage() => false;
    public override void OnSpawn(IEntitySource source) => Projectile.ai[1] = WeaponBalance.SlipperonMaxDistance * 0.5f;

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Projectile.rotation += 0.4f;
        if (Projectile.ai[0] >= Projectile.ai[1])
        {
            // Only the owner creates children; peers receive the normal projectile packets.
            if (Projectile.owner == Main.myPlayer)
            {
                var item = new Item();
                item.SetDefaults(ModContent.ItemType<MomsSlipper>());
                int damage = owner.GetWeaponDamage(item);
                float knockback = owner.GetWeaponKnockback(item);
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(i * 0.35f) * WeaponBalance.MomSpeed;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                        ModContent.ProjectileType<SlipperonChildProjectile>(), damage, knockback, Projectile.owner);
                }
            }
            Projectile.Kill();
            return;
        }
        float speed = Projectile.velocity.Length();
        float remaining = Projectile.ai[1] - Projectile.ai[0];
        if (speed > remaining && speed > 0f) Projectile.velocity *= remaining / speed;
        Projectile.ai[0] += Projectile.velocity.Length();
    }
    public override bool PreDraw(ref Color lightColor) => WeaponDrawing.DrawProjectile(Projectile, lightColor, 52f);
}

