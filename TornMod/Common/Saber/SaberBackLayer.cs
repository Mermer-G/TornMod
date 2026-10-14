using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using TornMod.Content.Items;
using TornMod.Content.Projectiles;

namespace TornMod.Common.SaberCombat;

public class SaberBackLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => SaberCombatPlayer.IsSaber(drawInfo.drawPlayer.HeldItem) &&
        drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<SaberSlashProjectile>()] == 0 && !drawInfo.drawPlayer.dead;
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        Texture2D texture = TextureAssets.Item[player.HeldItem.type].Value;
        // Anchor the grip at the belt rather than letting the resting blade orbit the mouse.
        Vector2 position = drawInfo.Position + player.Size / 2f - Main.screenPosition + new Vector2(player.direction * 6f, 8f);
        float bladeDirection = player.direction == 1 ? MathHelper.Pi - 0.12f : 0.12f;
        drawInfo.DrawDataCache.Add(new DrawData(texture, position, null, drawInfo.colorArmorBody,
            SaberDrawing.Rotation(bladeDirection), SaberDrawing.Grip(texture, bladeDirection),
            WeaponBalance.SaberLength / (texture.Height * 1.1f), SaberDrawing.Effects(bladeDirection), 0));
    }
}


