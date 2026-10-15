using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TornMod.Common;
using TornMod.Common.SaberCombat;
using TornMod.Content.Projectiles;

namespace TornMod.Content.Items;

public class Saber : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 60;
        Item.damage = 24;
        Item.crit = 4;
        Item.knockBack = 4f;
        Item.DamageType = DamageClass.Melee;
        Item.autoReuse = true;
        Item.useTime = Item.useAnimation = 12;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = true;
        Item.noUseGraphic = Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<SaberChargeProjectile>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(silver: 30);
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        Item.channel = player.altFunctionUse == 2;
        return
        (player.altFunctionUse != 2 || player.GetModPlayer<SaberCombatPlayer>().CanBeginCharge()) &&
        player.ownedProjectileCounts[Item.shoot] == 0 &&
        player.ownedProjectileCounts[ModContent.ProjectileType<SaberSlashProjectile>()] == 0 &&
        !player.GetModPlayer<SaberCombatPlayer>().Dashing;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2) return true;
        player.GetModPlayer<SaberCombatPlayer>().SpawnSlash(
            (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction).ToRotation(),
            false, damage, knockback);
        return false;
    }
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage) =>
        damage *= WeaponBalance.SaberDamage / 24f;
    public override bool PreDrawInWorld(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch,
        Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) =>
        WeaponDrawing.DrawWorldItem(Item, lightColor, rotation, scale, WeaponBalance.SaberLength);
    public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 10)
        .AddIngredient(ItemID.Wood, 8).AddTile(TileID.Anvils).Register();
}


