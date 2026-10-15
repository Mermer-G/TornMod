using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common.SaberCombat;
using NumericsVector = System.Numerics.Vector2;

namespace TornMod.Common.Glider;

public class GliderPlayer : ModPlayer
{
    public bool Equipped;
    public bool HideWings;
    public bool Gliding { get; private set; }
    public bool Acrobatics { get; private set; }
    public bool Boosting { get; private set; }
    public float AimAngle { get; private set; }
    public float WingOpen { get; private set; }
    public float DiveBlend { get; private set; }
    public float Lean { get; private set; }
    private GliderController controller;
    private GliderFlightBridge flight;
    private bool remoteGliding, remoteDive, remotePowered, lastSentGliding, lastSentDive, lastSentMode, lastSentPowered;
    private bool toggleRequested, toggleJumpHeld;
    private int syncTicks;
    private bool Owner => Main.netMode == NetmodeID.SinglePlayer || Player.whoAmI == Main.myPlayer;
    public override void Initialize() { controller = new GliderController(); flight = new GliderFlightBridge(); }
    public override void ResetEffects() { Equipped = false; HideWings = false; }
    internal bool HardBlocked => Player.dead || Player.mount.Active || Player.wet || Player.pulley || Player.tongued ||
        Player.frozen || Player.stoned || Player.webbed || Player.grapCount > 0 ||
        (Player.carpet && Player.carpetTime > 0) || Player.dashDelay < 0 ||
        Player.GetModPlayer<SaberCombatPlayer>().Dashing || Player.GetModPlayer<SaberCombatPlayer>().HitStopTicks > 0;
    internal bool Blocked => HardBlocked || GliderFlightBridge.HasCapacity(Player);
    private bool Supported()
    {
        Vector2 probe = Collision.TileCollision(Player.position, new Vector2(0, Player.gravDir * 2f),
            Player.width, Player.height, Player.controlDown, false, (int)Player.gravDir);
        return probe.Y * Player.gravDir < 1.999f;
    }
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (!GliderKeybinds.Pressed()) return;
        QueueToggleRequest(triggersSet.Jump);
    }
    internal void QueueToggleRequest(bool jumpHeld)
    {
        // Queue the edge; decide after this tick's actual accessories have been applied.
        toggleRequested = true;
        toggleJumpHeld = jumpHeld;
    }
    public override void SetControls()
    {
        if (toggleRequested && (Equipped || Gliding || Boosting) && toggleJumpHeld &&
            (Gliding || Boosting || Player.velocity.Y != 0f) && !HardBlocked && !Supported()) Player.controlUp = false;
    }
    internal bool ApplyPendingToggle(bool supported)
    {
        if (!toggleRequested) return false;
        toggleRequested = false;
        bool eligible = GliderController.CanToggleMode(Equipped, toggleJumpHeld,
            Gliding || Boosting || Player.velocity.Y != 0f, HardBlocked) && !supported;
        Mod.Logger.Info($"Glider toggle: equipped={Equipped}, jump={toggleJumpHeld}, airborne={!supported}, blocked={HardBlocked}, accepted={eligible}");
        if (!eligible) return false;
        Acrobatics = !Acrobatics;
        if (!Main.dedServ) Main.NewText(Acrobatics ? "Glider: Acrobatics" : "Glider: Normal Glide", Color.LightSkyBlue);
        return true;
    }
    public override void PostUpdateRunSpeeds()
    {
        if (toggleRequested) ApplyPendingToggle(!Equipped || HardBlocked || Supported());
        bool eligible = Equipped && !HardBlocked && Player.controlJump;
        flight.Prepare(Player, eligible);
    }
    public override void PreUpdateMovement()
    {
        flight.RestoreWings(Player);
        bool blocked = HardBlocked;
        bool supported = !blocked && (Equipped || remoteGliding) && Supported();
        Boosting = Acrobatics && !blocked && !supported && (Owner ? Equipped && GliderFlightBridge.RocketThrust(Player) : remotePowered && Player.rocketBoots > 0);
        bool capacity = GliderFlightBridge.HasCapacity(Player);
        bool active = Owner ? Equipped && Player.controlJump && !blocked && !supported &&
            (Boosting || (!capacity && (Gliding || Player.velocity.Y * Player.gravDir >= 0f))) : remoteGliding && !blocked && !supported;
        if (!active) { Gliding = Boosting = false; controller.ClearExternalMomentum(); return; }
        if (Owner && Acrobatics)
        {
            Vector2 aim = Main.MouseWorld - Player.MountedCenter;
            if (aim.LengthSquared() > 1f) AimAngle = MathF.Atan2(aim.Y * Player.gravDir, aim.X);
        }
        if (!Gliding)
        {
            // Rocket redirect discards only vanilla's vertical rocket force, not incoming momentum.
            Vector2 initial = Boosting ? flight.BeforeVanilla : Player.velocity;
            controller.Capture(new NumericsVector(initial.X, initial.Y * Player.gravDir));
        }
        Gliding = true;
        int steering = (Player.controlRight ? 1 : 0) - (Player.controlLeft ? 1 : 0);
        NumericsVector velocity;
        if (Acrobatics) velocity = controller.StepAcrobatics(AimAngle, Boosting);
        else if (controller.Velocity.Y < 0f) velocity = controller.Coast();
        else velocity = controller.Step(Player.direction, steering, Owner ? Player.controlDown : remoteDive);
        Player.velocity = new Vector2(velocity.X, velocity.Y * Player.gravDir);
        Player.maxFallSpeed = controller.ExternalMomentumActive ? Math.Max(GliderTuning.MaxSpeed, velocity.Length()) : GliderTuning.MaxSpeed;
    }
    public override void PostUpdate()
    {
        UpdateFacingFromInput();
        if (Gliding)
        {
            var actual = new NumericsVector(Player.velocity.X, Player.velocity.Y * Player.gravDir);
            if (!GliderController.ValidSnapshot(actual, true, controller.ExternalMomentumActive)) Gliding = Boosting = false;
            else controller.Capture(actual);
        }
        bool diving = Gliding && (Acrobatics ? MathF.Sin(AimAngle) > 0.35f : Owner ? Player.controlDown : remoteDive);
        WingOpen = MathHelper.Lerp(WingOpen, Gliding ? 1f : 0f, 0.16f);
        DiveBlend = MathHelper.Lerp(DiveBlend, diving ? 1f : 0f, 0.12f);
        float sign = MathF.Abs(Player.velocity.X) > 0.2f ? Math.Sign(Player.velocity.X) : Player.direction;
        float target = 0f;
        if (Gliding)
            target = Acrobatics ? MathF.Atan2(MathF.Sin(AimAngle) * Player.gravDir, MathF.Cos(AimAngle)) + MathHelper.PiOver2 * Player.gravDir :
                sign * Player.gravDir * MathHelper.Lerp(0.45f, 1.15f, DiveBlend);
        Lean = GliderController.WrapAngle(GliderController.SmoothAngle(Lean, target));
        if (!Gliding && MathF.Abs(Lean) < 0.002f) Lean = 0f;
        if (HardBlocked) Lean = 0f;
        if (Main.netMode == NetmodeID.MultiplayerClient && Owner &&
            (Gliding != lastSentGliding || diving != lastSentDive || Acrobatics != lastSentMode || Boosting != lastSentPowered || (Gliding && ++syncTicks >= 6)))
        {
            syncTicks = 0;
            lastSentGliding = Gliding; lastSentDive = diving; lastSentMode = Acrobatics; lastSentPowered = Boosting;
            NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
            SendState();
        }
    }
    // Called only when another mechanic has actually changed the player's momentum.
    internal void AcceptExternalMomentum()
    {
        if (Gliding) controller.CaptureExternal(new NumericsVector(Player.velocity.X, Player.velocity.Y * Player.gravDir));
        if (Gliding && Owner && Main.netMode == NetmodeID.MultiplayerClient) SendState();
    }
    internal void UpdateFacingFromInput()
    {
        // Vanilla facing can remain locked to horizontal momentum above the normal movement cap.
        // Facing responds to steering independently; active weapon aim keeps its normal priority.
        if (!Gliding || HardBlocked || Player.itemAnimation > 0) return;
        int facing = (Player.controlRight ? 1 : 0) - (Player.controlLeft ? 1 : 0);
        if (facing == 0 || facing == Player.direction) return;
        Player.ChangeDir(facing);
        if (Main.netMode == NetmodeID.MultiplayerClient && Owner)
            NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
    }
    public override void TransformDrawData(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender || MathF.Abs(GliderController.WrapAngle(Lean)) < 0.001f || HardBlocked) return;
        Vector2 pivot = drawInfo.Position - Player.position + Player.MountedCenter - Main.screenPosition;
        for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
        {
            DrawData data = drawInfo.DrawDataCache[i];
            data.position = pivot + (data.position - pivot).RotatedBy(Lean);
            // A firing gun stays pointed at its shot direction while its attachment moves with the torso.
            if (!IsAimedWeapon(data)) data.rotation += Lean;
            drawInfo.DrawDataCache[i] = data;
        }
    }
    private bool IsAimedWeapon(DrawData data)
    {
        Item held = Player.HeldItem;
        if (Main.dedServ || Player.itemAnimation <= 0 || held.useStyle != ItemUseStyleID.Shoot) return false;
        if (TextureAssets.Item[held.type].IsLoaded && data.texture == TextureAssets.Item[held.type].Value) return true;
        if (held.type != ModContent.ItemType<global::TornMod.Content.Items.Piercer>()) return false;
        var reload = TextureAssets.Projectile[ModContent.ProjectileType<global::TornMod.Content.Projectiles.PiercerReloadProjectile>()];
        return reload.IsLoaded && data.texture == reload.Value;
    }
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender || HardBlocked || Player.itemAnimation <= 0 || Player.HeldItem.useStyle != ItemUseStyleID.Shoot) return;
        drawInfo.compositeFrontArmRotation -= Lean;
        drawInfo.compositeBackArmRotation -= Lean;
    }
    public void ApplyNetworkState(bool active, bool dive, NumericsVector velocity, bool acrobatics = false, float aim = 0f, bool powered = false, bool externalMomentum = false)
    {
        remoteGliding = active; remoteDive = dive; remotePowered = powered;
        Gliding = active; Acrobatics = acrobatics; AimAngle = aim; Boosting = powered;
        if (active && externalMomentum) controller.CaptureExternal(velocity);
        else { controller.ClearExternalMomentum(); if (active) controller.Capture(velocity); }
    }
    public void SendState(int toWho = -1, int ignoreWho = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) return;
        var packet = Mod.GetPacket();
        packet.Write((byte)1); packet.Write((byte)Player.whoAmI);
        packet.Write(Gliding);
        packet.Write(Gliding && (Main.netMode == NetmodeID.Server ? remoteDive : Player.controlDown));
        var velocity = Gliding ? controller.Velocity : NumericsVector.Zero;
        packet.Write(velocity.X); packet.Write(velocity.Y);
        packet.Write(Acrobatics); packet.Write(GliderController.WrapAngle(AimAngle)); packet.Write(Boosting);
        packet.Write(controller.ExternalMomentumActive);
        packet.Send(toWho, ignoreWho);
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendState(toWho, fromWho);
    public override void UpdateDead()
    {
        flight.RestoreWings(Player);
        Gliding = remoteGliding = remotePowered = Boosting = false;
        controller.ClearExternalMomentum();
        WingOpen = DiveBlend = Lean = 0f;
        if (Main.netMode == NetmodeID.MultiplayerClient && Owner && lastSentGliding)
        {
            lastSentGliding = lastSentDive = lastSentPowered = false;
            SendState();
        }
    }
}
