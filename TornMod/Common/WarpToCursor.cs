using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common.SaberCombat;
using TornMod.Content.Projectiles;

namespace TornMod.Common;

public class WarpKeybinds : ModSystem
{
    internal const string KeyName = "TornMod/WarpToCursor";
    public static ModKeybind Warp { get; private set; }
    public override void Load() => Warp = KeybindLoader.RegisterKeybind(Mod, "WarpToCursor", "R");
    public override void Unload() => Warp = null;
    internal static bool Pressed()
    {
        if (Warp == null) return false;
        if (PlayerInput.Triggers.JustPressed.KeyStatus.TryGetValue(KeyName, out bool edge) && edge) return true;
        var keys = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus;
        keys.TryGetValue(KeyName, out var assigned);
        return (assigned == null || assigned.Count == 0 || assigned.Contains("R")) &&
            PlayerInput.Triggers.JustPressed.QuickMount && keys.TryGetValue("QuickMount", out var mount) && mount.Contains("R");
    }
}

public class WarpToCursorPlayer : ModPlayer
{
    private bool requested;
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (WeaponConfig.Current.WarpToCursor && !Player.dead && WarpKeybinds.Pressed()) requested = true;
    }
    public override void SetControls()
    {
        if (requested && WeaponConfig.Current.WarpToCursor) Player.controlMount = false;
    }
    public override void PreUpdateMovement()
    {
        if (!requested) return;
        requested = false;
        if (!WeaponConfig.Current.WarpToCursor || Player.dead || Player.whoAmI != Main.myPlayer) return;
        Vector2 destination = Main.MouseWorld - Player.Size / 2f;
        if (!ValidDestination(Player, destination))
        {
            Main.NewText("Warp blocked: cursor must be inside the world with room for the player.", Color.OrangeRed);
            return;
        }
        StopCombat(Player);
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Mod.GetPacket();
            packet.Write((byte)2); packet.Write((byte)Player.whoAmI);
            packet.Write(destination.X); packet.Write(destination.Y); packet.Send();
        }
        else Apply(Player, destination);
    }
    internal static bool InWorld(Vector2 target, int width, int height, int worldWidth, int worldHeight) =>
        float.IsFinite(target.X) && float.IsFinite(target.Y) && target.X >= 16 && target.Y >= 16 &&
        target.X + width <= worldWidth * 16 - 16 && target.Y + height <= worldHeight * 16 - 16;
    internal static bool ValidDestination(Player player, Vector2 target) =>
        InWorld(target, player.width, player.height, Main.maxTilesX, Main.maxTilesY) &&
        !Collision.SolidCollision(target, player.width, player.height);
    internal static void StopCombat(Player player)
    {
        player.GetModPlayer<SaberCombatPlayer>().ResetForTeleport();
        player.GetModPlayer<Lancer.LancerPlayer>().CancelLance();
        var glider = player.GetModPlayer<Glider.GliderPlayer>();
        glider.ApplyNetworkState(false, false, System.Numerics.Vector2.Zero, glider.Acrobatics, glider.AimAngle);
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.ModProjectile is SaberChargeProjectile) projectile.Kill();
    }
    internal static void Apply(Player player, Vector2 destination)
    {
        StopCombat(player);
        player.mount.Dismount(player);
        player.RemoveAllGrapplingHooks();
        player.velocity = Vector2.Zero;
        player.Teleport(destination, TeleportationStyleID.RodOfDiscord);
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI,
                destination.X, destination.Y, TeleportationStyleID.RodOfDiscord);
    }
}
