using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Content.Items;
using TornMod.Content.Projectiles;

namespace TornMod.Common.SaberCombat;

public class SaberCombatPlayer : ModPlayer
{
    public static bool IsGrounded(Player player)
    {
        if (player.jump > 0) return false;
        if (player.sloping) return true; // Vanilla slope walking may give an upward Y velocity.
        // PreUpdateMovement sees gravity already applied even while standing on a floor.
        // Reject upward movement, but use actual tile support to distinguish standing from falling.
        if (player.velocity.Y * player.gravDir < -0.01f) return false;
        Vector2 probe = Collision.TileCollision(player.position, new Vector2(0, player.gravDir * 6f),
            player.width, player.height, player.controlDown, false, (int)player.gravDir);
        return probe.Y * player.gravDir < 5.999f;
    }
    public static bool IsSaber(Item item) => !item.IsAir && item.CountsAsClass(ModContent.GetInstance<SaberDamageClass>());
    public int CoyoteTicks { get; private set; }
    public int DashCooldown { get; private set; }
    public bool CanChargeDash() => CoyoteTicks > 0 || IsGrounded(Player);
    internal void UpdateGroundGrace(bool grounded) => CoyoteTicks = grounded ? WeaponBalance.SaberCoyoteTicks : Math.Max(0, CoyoteTicks - 1);
    public bool CanBeginCharge() => IsSaber(Player.HeldItem) && CanChargeDash() && !Dashing && !pendingSlash &&
        DashCooldown == 0 && HitStopTicks == 0 &&
        Player.ownedProjectileCounts[ModContent.ProjectileType<SaberChargeProjectile>()] == 0 &&
        Player.ownedProjectileCounts[ModContent.ProjectileType<SaberSlashProjectile>()] == 0;
    public bool BeginCharge()
    {
        if (Player.whoAmI != Main.myPlayer || !CanBeginCharge()) return false;
        Item item = Player.HeldItem;
        Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero,
            ModContent.ProjectileType<SaberChargeProjectile>(), Player.GetWeaponDamage(item),
            Player.GetWeaponKnockback(item), Player.whoAmI);
        Player.itemTime = Player.itemAnimation = 2;
        return true;
    }
    public FallProtectionState FallProtection { get; private set; } = new();
    public override void Initialize() => FallProtection = new FallProtectionState();
    public bool Dashing { get; private set; }
    public float RemainingDistance { get; private set; }
    public int HitStopTicks { get; private set; }
    public bool HitStopThisTick { get; private set; }
    private Vector2 dashDirection;
    private int dashDamage;
    private float dashKnockback;
    private bool pendingSlash;
    private int dashTicks;
    private float dashSpeed;
    private float dashRetention;
    private readonly HashSet<NPC> protectedContacts = new();
    private readonly HashSet<NPC> dashHits = new();
    private int contactProtectionTicks;
    private int dashDuration;
    private Vector2 trailStart;
    private bool recordTrail;

    internal static bool IsDashTarget(NPC npc) => npc.active && !npc.friendly && !npc.dontTakeDamage &&
        (!npc.immortal || npc.type == NPCID.TargetDummy);
    internal bool TryRecordDashHit(NPC npc) => dashHits.Add(npc);

    internal void ProtectDashContact(NPC target)
    {
        protectedContacts.Add(target);
        contactProtectionTicks = WeaponBalance.SaberSlashTicks + WeaponBalance.SaberHitStopTicks + 2;
    }

    public void StartDash(Vector2 direction, float range, int damage, float knockback)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        dashDirection = direction.SafeNormalize(new Vector2(Player.direction, 0f));
        RemainingDistance = Math.Clamp(range, 1f, 4000f);
        dashRetention = Math.Clamp(WeaponBalance.SaberDashVelocityRetention, 0.6f, 0.95f);
        dashDuration = WeaponBalance.SaberDashTicks;
        dashSpeed = SaberMechanics.InitialDashSpeed(RemainingDistance, dashRetention, dashDuration);
        dashDamage = damage;
        dashKnockback = knockback;
        dashTicks = 0;
        protectedContacts.Clear();
        dashHits.Clear();
        contactProtectionTicks = 0;
        HitStopTicks = 0;
        HitStopThisTick = false;
        Dashing = true;
        Player.mount.Dismount(Player);
        Player.velocity = dashDirection * dashSpeed;
        SyncMovement();
    }

    public void CancelDash() { Dashing = false; pendingSlash = false; RemainingDistance = 0f; recordTrail = false; }
    public void ResetForTeleport()
    {
        CancelDash();
        HitStopTicks = CoyoteTicks = 0;
        HitStopThisTick = false;
        protectedContacts.Clear();
        contactProtectionTicks = 0;
    }

    internal void FinishDash()
    {
        Dashing = false;
        pendingSlash = true;
        DashCooldown = Math.Max(1, (int)MathF.Ceiling(Player.HeldItem.useTime / Player.GetWeaponAttackSpeed(Player.HeldItem)));
        SyncMovement();
    }

    public override void PreUpdateMovement()
    {
        HitStopThisTick = HitStopTicks > 0;
        if (Player.whoAmI != Main.myPlayer) return;
        if (DashCooldown > 0) DashCooldown--;
        if (!Player.dead) UpdateGroundGrace(!Dashing && IsGrounded(Player));
        // Sample release before movement, rather than waiting for the projectile's later AI phase.
        if (!Dashing && !Player.controlUseTile)
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.owner == Player.whoAmI && projectile.ModProjectile is SaberChargeProjectile charge && projectile.ai[2] == 0f)
                { charge.Release(Player); break; }
        HitStopThisTick = HitStopTicks > 0;
        if (HitStopTicks > 0)
        {
            Player.velocity = Vector2.Zero;
            return;
        }
        if (!Dashing)
        {
            if (Player.controlUseTile && IsSaber(Player.HeldItem)) BeginCharge();
            return;
        }
        Player.gravity = 0f;
        Player.maxFallSpeed = 1000f;
        if (Player.dead || !Player.active || !IsSaber(Player.HeldItem))
        {
            CancelDash();
            return;
        }
        // Normalize the velocity impulse to a short, fixed-duration ease-out dash.
        float distance = SaberMechanics.NextDashStep(RemainingDistance,
            dashTicks >= dashDuration - 1 ? RemainingDistance : dashSpeed);
        dashSpeed *= dashRetention;
        Vector2 allowed = Vector2.Zero;
        bool stopped = false;
        trailStart = Player.MountedCenter;
        recordTrail = true;
        // Sweep in <=4px segments; slide along open tile axes instead of stopping on any contact.
        for (float travelled = 0f; travelled < distance;)
        {
            float segment = Math.Min(4f, distance - travelled);
            Vector2 step = dashDirection * segment;
            Vector2 actual = Collision.TileCollision(Player.position + allowed, step,
                Player.width, Player.height, false, false, (int)Player.gravDir);
            bool blocked = Vector2.DistanceSquared(step, actual) > 0.0001f;
            if (blocked)
            {
                if (SaberMechanics.TrySlide(dashDirection.X, dashDirection.Y, step.X, step.Y,
                    actual.X, actual.Y, out float slideX, out float slideY))
                    dashDirection = new Vector2(slideX, slideY);
                else stopped = true;
            }
            Rectangle next = new((int)(Player.position.X + allowed.X + actual.X),
                (int)(Player.position.Y + allowed.Y + actual.Y), Player.width, Player.height);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (IsDashTarget(npc) && next.Intersects(npc.Hitbox))
                {
                    ProtectDashContact(npc);
                    if (TryRecordDashHit(npc))
                    {
                        Player.ApplyDamageToNPC(npc, dashDamage, dashKnockback,
                            dashDirection.X >= 0f ? 1 : -1, true, ModContent.GetInstance<SaberDamageClass>());
                        RegisterChargedHit();
                    }
                }
            }
            allowed += actual;
            travelled += segment;
            if (stopped) break;
        }
        RemainingDistance = Math.Max(0f, RemainingDistance - allowed.Length());
        Player.velocity = allowed;
        if (stopped || RemainingDistance <= 0.001f || dashTicks >= dashDuration - 1)
        {
            // Let vanilla apply only the swept safe displacement this tick.
            FinishDash();
            Player.velocity = allowed;
        }
        if (++dashTicks % 3 == 0) SyncMovement();
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI == Main.myPlayer && recordTrail)
        {
            recordTrail = false;
            if (!Main.dedServ && Vector2.DistanceSquared(trailStart, Player.MountedCenter) > 1f)
                Projectile.NewProjectile(Player.GetSource_Misc("SaberDashTrail"), trailStart, Vector2.Zero,
                    ModContent.ProjectileType<SaberDashTrailProjectile>(), 0, 0f, Player.whoAmI,
                    Player.MountedCenter.X, Player.MountedCenter.Y);
        }
        if (Player.whoAmI == Main.myPlayer && pendingSlash)
        {
            pendingSlash = false;
            float attackAngle = (Main.MouseWorld - Player.MountedCenter).SafeNormalize(dashDirection).ToRotation();
            SpawnSlash(attackAngle, true, dashDamage, dashKnockback, visualOnly: true);
            SyncMovement();
        }
        if (HitStopTicks > 0) HitStopTicks--;
        if (contactProtectionTicks > 0 && --contactProtectionTicks == 0) protectedContacts.Clear();
    }

    public void SpawnSlash(float angle, bool charged, int damage, float knockback, int target = -1, bool visualOnly = false)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.MountedCenter, Vector2.Zero,
            ModContent.ProjectileType<SaberSlashProjectile>(), damage, knockback, Player.whoAmI,
            angle, visualOnly ? 2f : charged ? 1f : 0f, target + 1);
    }

    public void RegisterChargedHit()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        FallProtection.Grant();
        // Passage hits must not interrupt the dash with hit stop.
        HitStopTicks = Dashing ? 0 : WeaponBalance.SaberHitStopTicks;
        HitStopThisTick = HitStopTicks > 0;
        SendState();
    }

    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        int slot = damageSource.SourceNPCIndex;
        if (slot >= 0 && slot < Main.maxNPCs && damageSource.SourceProjectileLocalIndex < 0)
        {
            NPC source = Main.npc[slot];
            if (Dashing && IsDashTarget(source))
            {
                ProtectDashContact(source);
                return true;
            }
            if (contactProtectionTicks > 0 && protectedContacts.Contains(source)) return true;
        }
        if (!FallProtection.TryConsume(damageSource.SourceOtherIndex)) return false;
        if (Player.whoAmI == Main.myPlayer) SendState();
        return true;
    }

    public override void UpdateDead()
    {
        CancelDash();
        CoyoteTicks = DashCooldown = 0;
        HitStopTicks = 0;
        contactProtectionTicks = 0;
        protectedContacts.Clear();
        if (FallProtection.Armed) { FallProtection.Clear(); if (Player.whoAmI == Main.myPlayer) SendState(); }
    }

    private void SyncMovement()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
    }

    public void ApplyNetworkState(bool fallProtection, int hitStopTicks)
    {
        if (fallProtection) FallProtection.Grant(); else FallProtection.Clear();
        HitStopTicks = Math.Clamp(hitStopTicks, 0, 10);
    }

    public void SendState(int toWho = -1, int ignoreWho = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) return;
        var packet = Mod.GetPacket();
        packet.Write((byte)0);
        packet.Write((byte)Player.whoAmI);
        packet.Write(FallProtection.Armed);
        packet.Write((byte)HitStopTicks);
        packet.Send(toWho, ignoreWho);
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendState(toWho, fromWho);
}


