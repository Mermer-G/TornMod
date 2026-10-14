using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common.Glider;

namespace TornMod.Content.Items;

public class GliderWings : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.LeafWings;
    public override void SetDefaults()
    {
        Item.width = 30; Item.height = 26;
        Item.accessory = true;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(silver: 50);
        // No wingSlot or wingTime: this is not a vanilla flight accessory.
    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        var glider = player.GetModPlayer<GliderPlayer>();
        glider.Equipped = true;
        glider.HideWings = hideVisual;
    }
}
