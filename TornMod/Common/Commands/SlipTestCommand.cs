using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Content.Items;

namespace TornMod.Common.Commands;

public class SlipTestCommand : ModCommand
{
    public override string Command => "sliptest";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/sliptest";
    public override string Description => "Gives Mom's Slipper, Slipperon, Saber, Piercer and ammo (single player).";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
        {
            caller.Reply("/sliptest is available in single-player test worlds only.");
            return;
        }
        if (args.Length != 0)
        {
            caller.Reply("Usage: /sliptest");
            return;
        }
        Player player = caller.Player;
        if (!player.active || player.dead)
        {
            caller.Reply("Use this command while your character is alive.");
            return;
        }
        var source = player.GetSource_Misc("TornMod_SlipTestCommand");
        player.QuickSpawnItem(source, ModContent.ItemType<MomsSlipper>(), 1000);
        player.QuickSpawnItem(source, ModContent.ItemType<Slipperon>(), 1);
        player.QuickSpawnItem(source, ModContent.ItemType<Saber>(), 1);
        player.QuickSpawnItem(source, ModContent.ItemType<Piercer>(), 1);
        player.QuickSpawnItem(source, ModContent.ItemType<RepeatingCrossbow>(), 1);
        player.QuickSpawnItem(source, ItemID.WoodenArrow, 500);
        player.QuickSpawnItem(source, ItemID.MusketBall, 500);
        caller.Reply("Gave all five weapons, 1000 Mom's Slippers, 500 Musket Balls and 500 Wooden Arrows. If your inventory is full, pick up the dropped items.");
    }
}

