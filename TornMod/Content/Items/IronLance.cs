using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common.Lancer;

namespace TornMod.Content.Items;

public class IronLance : LancerWeapon
{
    public override Rectangle SpriteFrame(Texture2D texture) => new(25, 302, 2122, 121);
    public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 10)
        .AddIngredient(ItemID.Wood, 5).AddTile(TileID.Anvils).Register();
}
