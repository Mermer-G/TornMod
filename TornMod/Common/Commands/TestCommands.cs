using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TornMod.Common.Commands;

public abstract class LocalTestCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    protected bool Ready(CommandCaller caller)
    {
        if (Main.netMode != NetmodeID.SinglePlayer) { caller.Reply("Test commands are available in single player only."); return false; }
        if (!caller.Player.active || caller.Player.dead) { caller.Reply("Use this command while your character is alive."); return false; }
        return true;
    }
}

public class SpawnRateCommand : LocalTestCommand
{
    public override string Command => "spawnrate";
    public override string Usage => "/spawnrate <0-100 | reset>";
    public override string Description => "Natural spawn multiplier: 0 disables, 1 normal, 5 fivefold.";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Ready(caller)) return;
        if (args.Length == 0) { caller.Reply($"Spawn multiplier: {TestSpawnSystem.Multiplier:0.##}. {Usage}"); return; }
        if (args.Length != 1) { caller.Reply(Usage); return; }
        if (args[0].Equals("reset", StringComparison.OrdinalIgnoreCase)) TestSpawnSystem.Multiplier = 1f;
        else if (float.TryParse(args[0].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) &&
            float.IsFinite(value) && value >= 0 && value <= 100) TestSpawnSystem.Multiplier = value;
        else { caller.Reply(Usage); return; }
        caller.Reply($"Natural spawn multiplier: {TestSpawnSystem.Multiplier:0.##}. Resets when leaving the world.");
    }
}

public class DummyCommand : LocalTestCommand
{
    public override string Command => "dummy";
    public override string Usage => "/dummy";
    public override string Description => "Gives 5 Target Dummies and 1 DPS Meter.";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Ready(caller)) return;
        if (args.Length != 0) { caller.Reply(Usage); return; }
        var source = caller.Player.GetSource_Misc("TornMod_TestDummies");
        caller.Player.QuickSpawnItem(source, ItemID.TargetDummy, 5);
        caller.Player.QuickSpawnItem(source, ItemID.DPSMeter);
        caller.Reply("Gave 5 Target Dummies and 1 DPS Meter. Place the dummies on the ground; keep the meter in your inventory.");
    }
}

public class ZombiesCommand : LocalTestCommand
{
    public override string Command => "zombies";
    public override string Usage => "/zombies";
    public override string Description => "Spawns 5 zombies on each side; can be repeated.";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Ready(caller)) return;
        if (args.Length != 0) { caller.Reply(Usage); return; }
        Player player = caller.Player;
        int spawned = 0;
        foreach (int side in new[] { -1, 1 })
        for (int i = 0; i < 5; i++)
        {
            float x = Math.Clamp(player.Center.X + side * (160 + i * 48), 32, Main.maxTilesX * 16 - 48);
            for (int offset = 0; offset <= 480; offset += 16)
            {
                Vector2 top = new(x - 16, player.Bottom.Y - 48 + offset);
                if (top.Y < 16 || top.Y + 48 >= Main.maxTilesY * 16 - 16 || Collision.SolidCollision(top, 32, 48)) continue;
                if (!Collision.SolidCollision(top + new Vector2(0, 48), 32, 16) && offset < 480) continue;
                int slot = NPC.NewNPC(player.GetSource_Misc("TornMod_TestZombies"), (int)x, (int)(top.Y + 48), NPCID.Zombie);
                if (slot < Main.maxNPCs) { Main.npc[slot].target = player.whoAmI; spawned++; }
                break;
            }
        }
        caller.Reply($"{spawned}/10 zombies spawned. Use /zombies again for another group.");
    }
}

public class BossCommand : LocalTestCommand
{
    public static readonly Dictionary<string, int> Bosses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["king"] = NPCID.KingSlime, ["eye"] = NPCID.EyeofCthulhu, ["eater"] = NPCID.EaterofWorldsHead,
        ["brain"] = NPCID.BrainofCthulhu, ["bee"] = NPCID.QueenBee, ["skeletron"] = NPCID.SkeletronHead,
        ["deerclops"] = NPCID.Deerclops, ["wall"] = NPCID.WallofFlesh, ["queen"] = NPCID.QueenSlimeBoss,
        ["destroyer"] = NPCID.TheDestroyer, ["prime"] = NPCID.SkeletronPrime, ["plantera"] = NPCID.Plantera,
        ["golem"] = NPCID.Golem, ["fishron"] = NPCID.DukeFishron, ["empress"] = NPCID.HallowBoss,
        ["cultist"] = NPCID.CultistBoss, ["moonlord"] = NPCID.MoonLordCore
    };
    public override string Command => "boss";
    public override string Usage => "/boss <name | NPC ID | list>";
    public override string Description => "Spawns a boss. /boss list shows available names.";
    public static int Resolve(string name)
    {
        if (Bosses.TryGetValue(name, out int type)) return type;
        if (int.TryParse(name, out type)) return type > 0 && type < NPCLoader.NPCCount ? type : -1;
        foreach (FieldInfo field in typeof(NPCID).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.IsLiteral && field.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return Convert.ToInt32(field.GetRawConstantValue());
        return -1;
    }
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Ready(caller)) return;
        if (args.Length != 1 || args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        { caller.Reply(Usage + "\n" + string.Join(", ", Bosses.Keys) + ", twins. Vanilla NPCID names are also accepted."); return; }
        if (args[0].Equals("twins", StringComparison.OrdinalIgnoreCase))
        { NPC.SpawnOnPlayer(caller.Player.whoAmI, NPCID.Retinazer); NPC.SpawnOnPlayer(caller.Player.whoAmI, NPCID.Spazmatism); return; }
        int type = Resolve(args[0]);
        if (type < 1 || type >= NPCLoader.NPCCount) { caller.Reply("Boss not found. /boss list"); return; }
        var probe = new NPC();
        probe.SetDefaults(type);
        if (!probe.boss && !Bosses.ContainsValue(type)) { caller.Reply("This NPC is not a boss. /boss list"); return; }
        NPC.SpawnOnPlayer(caller.Player.whoAmI, type);
        caller.Reply("Boss summoned. Normal day/night and biome behavior still applies.");
    }
}

