using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common.BoltAction;

namespace TornMod.Content.Projectiles;

public class HotCasingProjectile : ModProjectile
{
    public override string Texture => "TornMod/Content/Items/PiercerReload";
    public override void SetDefaults()
    {
        Projectile.width = 12; Projectile.height = 6;
        Projectile.tileCollide = true; Projectile.timeLeft = 600;
        Projectile.penetrate = -1;
    }
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);
    public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();
    public override void AI()
    {
        if (Projectile.ai[0] == 0)
        {
            Projectile.velocity.Y = System.Math.Min(10f, Projectile.velocity.Y + 0.25f);
            Projectile.rotation += Projectile.velocity.X * 0.1f;
            return;
        }
        Projectile.velocity = Vector2.Zero;
        if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft % 10 == 0)
        {
            Rectangle hot = Projectile.Hitbox; hot.Inflate(4, 4);
            foreach (NPC npc in Main.ActiveNPCs)
                if (!npc.dontTakeDamage && hot.Intersects(npc.Hitbox)) npc.AddBuff(BuffID.OnFire, 120);
        }
        if (!Main.dedServ && Projectile.timeLeft % 8 == 0)
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0, -0.5f, 150, default, 0.6f);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (oldVelocity.Y > 0 && Projectile.velocity.Y != oldVelocity.Y && Projectile.ai[0] == 0)
        {
            Projectile.ai[0] = 1; Projectile.timeLeft = BoltMechanics.GroundCasingTicks;
            Projectile.velocity = Vector2.Zero; Projectile.rotation = 0;
            Projectile.netUpdate = true;
        }
        return false;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Rectangle casing = new(1544, 180, 62, 43);
        Main.EntitySpriteDraw(TextureAssets.Projectile[Type].Value, Projectile.Center - Main.screenPosition,
            casing, Color.Lerp(lightColor, Color.Orange, 0.4f), Projectile.rotation, casing.Size() / 2f,
            16f / casing.Width, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        return false;
    }
}
