using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using TornMod.Content.Items;
using TornMod.Content.Projectiles;

namespace TornMod.Common.BoltAction;

public static class PiercerDrawing
{
    public static Rectangle ClosedFrame => new(30, 552, 1198, 262);
    public static Rectangle ReloadFrame(int frame) => new(new[] { 17, 460, 903, 1346 }[frame % 4], frame < 4 ? 210 : 613, 413, frame < 4 ? 114 : 118);
    public static Vector2 ShoulderOffset(Player player) => new(-player.direction * 5f, -2f * player.gravDir);
    public static Vector2 Muzzle(Player player, float aim) => player.MountedCenter + ShoulderOffset(player) +
        new Vector2(79.5f, MathF.Cos(aim) < 0f ? 9.3f : -9.3f).RotatedBy(aim);
    public static void PoseArms(Player player, float aim)
    {
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
        player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters,
            aim - MathHelper.PiOver2 + player.direction * 0.3f);
    }
    public static DrawData Gun(Texture2D texture, Rectangle frame, Vector2 position, float aim, Color color)
    {
        bool left = MathF.Cos(aim) < 0;
        // The stock, not the trigger, pivots at the player's shoulder.
        Vector2 grip = new(frame.Width * 0.03f, frame.Height * (left ? 0.25f : 0.75f));
        return new DrawData(texture, position, frame, color, aim, grip, 82f / frame.Width,
            left ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
    }
}

public class PiercerHeldLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ArmOverItem);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead &&
        drawInfo.drawPlayer.itemAnimation > 0 &&
        drawInfo.drawPlayer.HeldItem.type == ModContent.ItemType<Piercer>();
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        float aim = player.itemRotation + (player.direction < 0 ? MathHelper.Pi : 0);
        if (player.whoAmI == Main.myPlayer) aim = (Main.MouseWorld - player.MountedCenter).ToRotation();
        Texture2D texture = TextureAssets.Item[ModContent.ItemType<Piercer>()].Value;
        Rectangle frame = PiercerDrawing.ClosedFrame;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.ModProjectile is PiercerReloadProjectile)
            {
                Main.instance.LoadProjectile(projectile.type);
                texture = TextureAssets.Projectile[projectile.type].Value;
                frame = PiercerDrawing.ReloadFrame(BoltMechanics.Frame((int)projectile.ai[0]));
                aim = projectile.ai[2];
                break;
            }
        Vector2 position = drawInfo.Position - player.position + player.MountedCenter - Main.screenPosition + PiercerDrawing.ShoulderOffset(player);
        drawInfo.DrawDataCache.Add(PiercerDrawing.Gun(texture, frame, position, aim, drawInfo.colorArmorBody));
    }
}
