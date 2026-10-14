using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TornMod.Common.Commands;

public class TestSpawnSystem : ModSystem
{
    public static float Multiplier { get; set; } = 1f;
    public override void OnWorldLoad() => Multiplier = 1f;
    public override void OnWorldUnload() => Multiplier = 1f;
    public static void Apply(float multiplier, ref int rate, ref int maximum)
    {
        if (multiplier == 1f) return;
        if (multiplier <= 0f) { rate = int.MaxValue; maximum = 0; return; }
        rate = Math.Max(1, (int)Math.Min(int.MaxValue, rate / (double)multiplier));
        maximum = Math.Clamp((int)Math.Ceiling(maximum * (double)multiplier), 0, 200);
    }
}

public class TestSpawnNPC : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) TestSpawnSystem.Apply(TestSpawnSystem.Multiplier, ref spawnRate, ref maxSpawns);
    }
}
