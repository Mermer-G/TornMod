using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TornMod.Common.Lancer;

// Future lances reuse the player/controller and override their definition, texture crop and recipe.
public abstract class LancerWeapon : ModItem
{
    public virtual LancerSettings Settings => LancerSettings.Iron;
    public virtual Rectangle SpriteFrame(Texture2D texture) => texture.Bounds;
    public override void SetDefaults()
    {
        Item.width = 48; Item.height = 12;
        Item.damage = Settings.ContactDamage; Item.knockBack = Settings.ContactKnockback; Item.crit = 4;
        Item.DamageType = DamageClass.Melee;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = Settings.QuickUseTicks;
        Item.channel = true; Item.noMelee = Item.noUseGraphic = true;
        Item.rare = ItemRarityID.Blue; Item.value = Item.buyPrice(silver: 30);
    }
    public override bool AltFunctionUse(Player player) => true;
    // Shared player input owns prepare/thrust dispatch, independently of vanilla item auto-reuse.
    public override bool CanUseItem(Player player) => false;
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = TextureAssets.Item[Type].Value;
        Rectangle crop = SpriteFrame(texture);
        spriteBatch.Draw(texture, position, crop, drawColor, 0, crop.Size() / 2f,
            scale * texture.Width / crop.Width, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D texture = TextureAssets.Item[Type].Value;
        Rectangle crop = SpriteFrame(texture);
        spriteBatch.Draw(texture, Item.Center - Main.screenPosition, crop, lightColor, rotation,
            crop.Size() / 2f, Settings.Length / crop.Width * scale, SpriteEffects.None, 0);
        return false;
    }
}
