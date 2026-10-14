using Microsoft.Xna.Framework;
using Terraria;

namespace TornMod.Common.Glider;

// Isolated held-Jump chaining. No fuel is created or consumed here: vanilla owns both counters.
internal sealed class GliderFlightBridge
{
    private int hiddenWings;
    private float savedWingTime;
    public Vector2 BeforeVanilla { get; private set; }
    public static bool ExtraJumpActive(Player player)
    {
        foreach (var jump in player.ExtraJumps) if (jump.Active) return true;
        return false;
    }
    public static bool HasCapacity(Player player) => player.jump > 0 || ExtraJumpActive(player) ||
        player.AnyExtraJumpUsable() || (player.rocketBoots > 0 && (player.rocketTime > 0 || player.rocketDelay > 0)) ||
        (player.wingsLogic > 0 && player.wingTime > 0);
    public void Prepare(Player player, bool eligible)
    {
        BeforeVanilla = player.velocity;
        hiddenWings = 0;
        if (!eligible || !player.controlJump) return;
        bool extras = ExtraJumpActive(player) || player.AnyExtraJumpUsable();
        bool falling = player.velocity.Y * player.gravDir >= 0 && player.jump == 0;
        if (falling && !ExtraJumpActive(player))
        {
            // A held button normally doesn't re-arm these release latches.
            if (extras) player.releaseJump = true;
            else player.rocketRelease = true;
        }
        bool boots = player.rocketBoots > 0 && (player.rocketTime > 0 || player.rocketDelay > 0);
        if ((boots || extras || player.jump > 0) && player.wingsLogic > 0)
        {
            // Keep wings behind jumps/boots and prevent vanilla's boots-to-wing fuel transfer.
            hiddenWings = player.wingsLogic; savedWingTime = player.wingTime;
            player.wingsLogic = 0;
        }
    }
    public void RestoreWings(Player player)
    {
        if (hiddenWings == 0) return;
        player.wingsLogic = hiddenWings; player.wingTime = savedWingTime;
        hiddenWings = 0;
    }
    public static bool RocketThrust(Player player) => player.rocketBoots > 0 && player.rocketFrame &&
        player.controlJump && player.jump == 0 && !ExtraJumpActive(player) && !player.AnyExtraJumpUsable();
}
