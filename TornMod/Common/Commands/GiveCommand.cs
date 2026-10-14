using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TornMod.Common.Commands;

public class GiveCommand : LocalTestCommand
{
    public override string Command => "give";
    public override string Usage => "/give <item name, ID or Mod/Item> [amount] | /give list [search]";
    public override string Description => "Give any vanilla or modded item. Use /give list to see this mod's items.";
    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    private static string InternalName(int type) => ModContent.GetModItem(type)?.FullName ?? ItemID.Search.GetName(type);
    internal static List<int> Find(string query)
    {
        if (int.TryParse(query, out int id))
            return id > 0 && id < ItemLoader.ItemCount ? new List<int> { id } : new List<int>();
        string needle = Normalize(query);
        if (needle.Length == 0) return new List<int>();
        var exact = new List<int>();
        var partial = new List<int>();
        for (int type = 1; type < ItemLoader.ItemCount; type++)
        {
            string full = Normalize(InternalName(type));
            string shortName = Normalize(ModContent.GetModItem(type)?.Name ?? ItemID.Search.GetName(type));
            string display = Normalize(Lang.GetItemNameValue(type));
            if (needle == full || needle == shortName || needle == display) exact.Add(type);
            else if (full.Contains(needle) || display.Contains(needle)) partial.Add(type);
        }
        return exact.Count > 0 ? exact : partial;
    }
    private static string Describe(int type) => $"{InternalName(type)} [{type}] — {Lang.GetItemNameValue(type)}";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Ready(caller)) return;
        if (args.Length == 0) { caller.Reply(Usage); return; }
        if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            List<int> items = args.Length > 1 ? Find(string.Join(" ", args.Skip(1))) :
                Mod.GetContent<ModItem>().Select(item => item.Type).OrderBy(type => InternalName(type)).ToList();
            if (items.Count == 0) { caller.Reply("No matching items."); return; }
            caller.Reply(string.Join("\n", items.Take(15).Select(Describe)));
            if (items.Count > 15) caller.Reply($"{items.Count} matches. Narrow the search to see the rest.");
            return;
        }
        int amount = 1, nameLength = args.Length;
        if (args.Length > 1 && int.TryParse(args[^1], out int parsed)) { amount = parsed; nameLength--; }
        if (amount < 1 || amount > 9999) { caller.Reply("Amount must be between 1 and 9999."); return; }
        string name = string.Join(" ", args.Take(nameLength)).Trim('"');
        List<int> matches = Find(name);
        if (matches.Count == 0) { caller.Reply("Item not found. Use /give list or /give list <search>."); return; }
        if (matches.Count != 1)
        {
            caller.Reply("Multiple matches; use the exact name, ID or Mod/Item:\n" + string.Join("\n", matches.Take(10).Select(Describe)));
            return;
        }
        int typeId = matches[0];
        int maxStack = Math.Max(1, ContentSamples.ItemsByType[typeId].maxStack);
        if ((amount + maxStack - 1) / maxStack > 100) { caller.Reply("Use a smaller amount: one command can create up to 100 item stacks."); return; }
        var source = caller.Player.GetSource_Misc("TornMod_GiveCommand");
        for (int remaining = amount; remaining > 0;)
        {
            int stack = Math.Min(remaining, maxStack);
            caller.Player.QuickSpawnItem(source, typeId, stack);
            remaining -= stack;
        }
        caller.Reply($"Gave {amount} x {Lang.GetItemNameValue(typeId)}. If your inventory is full, pick up the dropped items.");
    }
}
