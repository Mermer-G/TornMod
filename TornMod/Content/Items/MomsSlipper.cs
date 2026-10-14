using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common;
using TornMod.Content.Projectiles;

namespace TornMod.Content.Items;

public class MomsSlipper : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 36;
        Item.maxStack = 9999;
        Item.damage = WeaponDefaults.MomDamage;
        Item.crit = 4;
        Item.DamageType = DamageClass.Ranged;
        Item.useTime = Item.useAnimation = WeaponDefaults.MomUseTicks;
        Item.knockBack = WeaponDefaults.MomKnockback;
        Item.shootSpeed = WeaponDefaults.MomSpeed;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = Item.noUseGraphic = true;
        Item.consumable = true;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<MomsSlipperProjectile>();
        Item.UseSound = SoundID.Item1;
        Item.rare = ItemRarityID.White;
        Item.value = Item.buyPrice(copper: 5);
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage) =>
        damage *= (float)WeaponBalance.MomDamage / WeaponDefaults.MomDamage;
    public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback) =>
        knockback *= WeaponBalance.MomKnockback / WeaponDefaults.MomKnockback;
    public override float UseSpeedMultiplier(Player player) =>
        (float)WeaponDefaults.MomUseTicks / WeaponBalance.MomUseTicks;
    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity,
        ref int type, ref int damage, ref float knockback) =>
        velocity *= WeaponBalance.MomSpeed / WeaponDefaults.MomSpeed;
    public override bool PreDrawInWorld(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch,
        Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) =>
        WeaponDrawing.DrawWorldItem(Item, lightColor, rotation, scale, 40f);
    public override void AddRecipes() => WeaponRecipes.RegisterMom(this);
}

