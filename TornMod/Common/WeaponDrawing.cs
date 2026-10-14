using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace TornMod.Common;

public static class WeaponDrawing
{
    // Keep the supplied PNGs unchanged. Scale them only when rendering.
    public static bool DrawWorldItem(Item item, Color light, float rotation, float scale, float size)
    {
        Texture2D texture = TextureAssets.Item[item.type].Value;
        Main.spriteBatch.Draw(texture, item.Center - Main.screenPosition, null, light,
            rotation, texture.Size() / 2f, size / texture.Height * scale, SpriteEffects.None, 0f);
        return false;
    }

    public static bool DrawProjectile(Projectile projectile, Color light, float size)
    {
        Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
        Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, null, light,
            projectile.rotation, texture.Size() / 2f, size / texture.Height * projectile.scale,
            SpriteEffects.None, 0);
        return false;
    }
}
