using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TornMod.Common;

public class WeaponRecipeGroups : ModSystem
{
    public override void AddRecipeGroups() => RecipeGroup.RegisterGroup("TornMod:GoldOrPlatinumBar",
        new RecipeGroup(() => Language.GetTextValue("Mods.TornMod.RecipeGroups.GoldOrPlatinumBar"),
            ItemID.GoldBar, ItemID.PlatinumBar));
}
