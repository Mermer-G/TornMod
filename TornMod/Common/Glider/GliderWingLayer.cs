using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace TornMod.Common.Glider;

// Temporary cloth-and-spar glider, built from vanilla pixels. No wing equipment flight logic.
public class GliderWingLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        var glider = drawInfo.drawPlayer.GetModPlayer<GliderPlayer>();
        return !drawInfo.drawPlayer.dead && !glider.HideWings && glider.WingOpen > 0.01f;
    }
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        var glider = player.GetModPlayer<GliderPlayer>();
        Vector2 root = drawInfo.Position - player.position + player.MountedCenter - Main.screenPosition +
            new Vector2(-player.direction * 5f, -4f * player.gravDir);
        float span = MathHelper.Lerp(5f, 30f, glider.WingOpen) * (1f - 0.25f * glider.DiveBlend);
        float tuck = glider.DiveBlend * 0.5f;
        Color cloth = drawInfo.colorArmorBody * glider.WingOpen;
        cloth = Color.Multiply(cloth, 0.75f);
        for (int side = -1; side <= 1; side += 2)
        {
            float angle = (side * (0.35f + tuck) + (side < 0 ? MathHelper.Pi : 0f)) * player.gravDir;
            var membrane = new DrawData(TextureAssets.MagicPixel.Value, root, new Rectangle(0, 0, 1, 1),
                cloth, angle, new Vector2(0f, 0.5f), new Vector2(span, 9f * glider.WingOpen), SpriteEffects.None, 0);
            drawInfo.DrawDataCache.Add(membrane);
            var spar = new DrawData(TextureAssets.MagicPixel.Value, root, new Rectangle(0, 0, 1, 1),
                drawInfo.colorArmorBody * glider.WingOpen, angle, new Vector2(0f, 0.5f),
                new Vector2(span, 2f), SpriteEffects.None, 0);
            drawInfo.DrawDataCache.Add(spar);
        }
    }
}
