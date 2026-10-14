using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Content.Items;

namespace TornMod.Common;

public static class WeaponRecipes
{
    public static void RegisterMom(ModItem item) => item.CreateRecipe(25)
        .AddIngredient(ItemID.Silk).AddIngredient(ItemID.Wood, 2)
        .AddTile(TileID.Loom).Register();

    public static void RegisterSlipperon(ModItem item) => item.CreateRecipe()
        .AddIngredient(ModContent.ItemType<MomsSlipper>(), 1000)
        .AddIngredient(ItemID.FallenStar, 5)
        .AddRecipeGroup("TornMod:GoldOrPlatinumBar", 10)
        .AddTile(TileID.Anvils).Register();
}
