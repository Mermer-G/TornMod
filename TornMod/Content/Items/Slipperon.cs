using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common;
using TornMod.Content.Projectiles;

namespace TornMod.Content.Items;

public class Slipperon : ModItem
{
    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.damage = WeaponDefaults.SlipperonDamage;
        Item.crit = 4;
        Item.DamageType = DamageClass.Ranged;
        Item.useTime = Item.useAnimation = WeaponDefaults.SlipperonUseTicks;
        Item.knockBack = WeaponDefaults.SlipperonKnockback;
        Item.shootSpeed = WeaponDefaults.SlipperonSpeed;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.consumable = false;
        Item.maxStack = 1;
        Item.shoot = ModContent.ProjectileType<SlipperonProjectile>();
        Item.UseSound = SoundID.Item1;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(silver: 20);
    }

    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0 &&
        player.ownedProjectileCounts[ModContent.ProjectileType<SlipperonSplitProjectile>()] == 0 &&
        player.ownedProjectileCounts[ModContent.ProjectileType<SlipperonChildProjectile>()] == 0;
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage) =>
        damage *= (float)WeaponBalance.SlipperonDamage / WeaponDefaults.SlipperonDamage;
    public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback) =>
        knockback *= WeaponBalance.SlipperonKnockback / WeaponDefaults.SlipperonKnockback;
    public override float UseSpeedMultiplier(Player player) =>
        (float)WeaponDefaults.SlipperonUseTicks / (WeaponBalance.SlipperonUseTicks * (player.altFunctionUse == 2 ? 2f : 1f));
    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity,
        ref int type, ref int damage, ref float knockback)
    {
        velocity *= WeaponBalance.SlipperonSpeed / WeaponDefaults.SlipperonSpeed;
        if (player.altFunctionUse == 2) type = ModContent.ProjectileType<SlipperonSplitProjectile>();
    }
    public override bool PreDrawInWorld(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch,
        Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) =>
        WeaponDrawing.DrawWorldItem(Item, lightColor, rotation, scale, 52f);
    public override void AddRecipes() => WeaponRecipes.RegisterSlipperon(this);
}

