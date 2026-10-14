using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common.BoltAction;

namespace TornMod.Content.Projectiles;

public class PiercerBulletProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bullet;
    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.Bullet);
        AIType = ProjectileID.Bullet;
        Projectile.DamageType = ModContent.GetInstance<BoltActionDamageClass>();
        Projectile.penetrate = 3;
        Projectile.extraUpdates = 5;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override void AI() => Lighting.AddLight(Projectile.Center, new Vector3(0.25f, 0.2f, 0.05f));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, 4f, ref point);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 tail = Projectile.Center - direction * 48f - Main.screenPosition;
        Rectangle pixel = new(0, 0, 1, 1);
        float angle = direction.ToRotation();
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, tail, pixel, new Color(255, 160, 40, 0) * 0.7f,
            angle, new Vector2(0, 0.5f), new Vector2(48f, 5f), SpriteEffects.None, 0);
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, tail, pixel, Color.LightGoldenrodYellow,
            angle, new Vector2(0, 0.5f), new Vector2(48f, 2f), SpriteEffects.None, 0);
        return false;
    }
}
