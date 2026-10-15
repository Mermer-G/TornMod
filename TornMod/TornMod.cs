using Terraria.ModLoader;
using System.IO;
using Terraria;
using Terraria.ID;
using TornMod.Common.SaberCombat;
using TornMod.Common.Glider;

namespace TornMod;

public class TornMod : Mod
{
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte kind = reader.ReadByte();
        if (kind == 2)
        {
            int warpId = reader.ReadByte();
            var destination = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
            if (Main.netMode != NetmodeID.Server || warpId != whoAmI || warpId >= Main.maxPlayers ||
                !Common.WeaponConfig.Current.WarpToCursor) return;
            Player player = Main.player[warpId];
            if (!player.active || player.dead || !Common.WarpToCursorPlayer.ValidDestination(player, destination)) return;
            Common.WarpToCursorPlayer.Apply(player, destination);
            return;
        }
        if (kind == 1)
        {
            int gliderId = reader.ReadByte();
            bool active = reader.ReadBoolean(), dive = reader.ReadBoolean();
            var velocity = new System.Numerics.Vector2(reader.ReadSingle(), reader.ReadSingle());
            bool acrobatics = reader.ReadBoolean();
            float aim = reader.ReadSingle();
            bool powered = reader.ReadBoolean();
            bool externalMomentum = reader.ReadBoolean();
            if (gliderId >= Main.maxPlayers || (Main.netMode == NetmodeID.Server && gliderId != whoAmI) ||
                !GliderController.ValidSnapshot(velocity, true, externalMomentum) || !float.IsFinite(aim) || System.MathF.Abs(aim) > System.MathF.PI + 0.001f ||
                (powered && (!active || !acrobatics)) || (externalMomentum && !active)) return;
            // The owning client predicts movement; standard Terraria controls carry position/collisions.
            if (Main.netMode == NetmodeID.MultiplayerClient && gliderId == Main.myPlayer) return;
            var glider = Main.player[gliderId].GetModPlayer<GliderPlayer>();
            glider.ApplyNetworkState(active, dive, velocity, acrobatics, aim, powered, externalMomentum);
            if (Main.netMode == NetmodeID.Server) glider.SendState(-1, whoAmI);
            return;
        }
        if (kind != 0) return;
        int playerId = reader.ReadByte();
        bool fallProtection = reader.ReadBoolean();
        int hitStop = reader.ReadByte();
        if (playerId >= Main.maxPlayers || (Main.netMode == NetmodeID.Server && playerId != whoAmI)) return;
        var combat = Main.player[playerId].GetModPlayer<SaberCombatPlayer>();
        combat.ApplyNetworkState(fallProtection, hitStop);
        if (Main.netMode == NetmodeID.Server) combat.SendState(-1, whoAmI);
    }
}

