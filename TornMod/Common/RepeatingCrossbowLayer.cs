using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using TornMod.Content.Items;

namespace TornMod.Common;

public class RepeatingCrossbowLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ArmOverItem);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead &&
        drawInfo.drawPlayer.HeldItem.ModItem is RepeatingCrossbow &&
        drawInfo.drawPlayer.itemAnimation > 0;
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        var bow = (RepeatingCrossbow)player.HeldItem.ModItem;
        float aim = bow.ShotAim;
        bool left = MathF.Cos(aim) < 0f;
        Rectangle frame = RepeatingCrossbow.SpriteFrame(bow.VisualFrame(player));
        Vector2 position = drawInfo.Position - player.position + player.MountedCenter - Main.screenPosition;
        Vector2 grip = new(frame.Width * 0.35f, frame.Height * (left ? 0.38f : 0.62f));
        drawInfo.DrawDataCache.Add(new DrawData(TextureAssets.Item[player.HeldItem.type].Value,
            position, frame, drawInfo.colorArmorBody, aim, grip, 64f / frame.Width,
            left ? SpriteEffects.FlipVertically : SpriteEffects.None, 0));
    }
}
