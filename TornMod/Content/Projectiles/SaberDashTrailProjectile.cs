using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace TornMod.Content.Projectiles;

// Purely visual, networked motion segment. The melee hit is applied during the dash sweep.
public class SaberDashTrailProjectile : ModProjectile
{
    public override string Texture => "TornMod/Content/Items/Saber";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
        Projectile.timeLeft = 6;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 start = Projectile.Center - Main.screenPosition;
        Vector2 delta = new Vector2(Projectile.ai[0], Projectile.ai[1]) - Projectile.Center;
        float fade = Projectile.timeLeft / 6f;
        fade *= fade;
        float angle = delta.ToRotation();
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        // Straight edges and tapered ends, with a narrow white core instead of a soft smoke trail.
        for (int i = 0; i < 16; i++)
        {
            float t = (i + 0.5f) / 16f;
            float taper = 1f - System.MathF.Abs(2f * t - 1f);
            Vector2 position = start + delta * (i / 16f);
            float segmentLength = delta.Length() / 16f + 0.5f;
            Main.EntitySpriteDraw(pixel, position, new Rectangle(0, 0, 1, 1), new Color(50, 160, 255, 0) * fade,
                angle, new Vector2(0, 0.5f), new Vector2(segmentLength, (2f + taper * 5f) * fade), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(pixel, position, new Rectangle(0, 0, 1, 1), Color.White * fade, angle,
                new Vector2(0, 0.5f), new Vector2(segmentLength, 1.5f * taper * fade), SpriteEffects.None, 0);
        }
        return false;
    }
}
