using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TornMod.Content.Projectiles;

namespace TornMod.Common.Lancer;

public class LancerPlayer : ModPlayer
{
    public int QuickCooldown { get; private set; }
    private HeldLanceProjectile activeLance;
    private Vector2 movementVelocity;
    private ulong snapshotTick;
    private bool hasSnapshot;
    private readonly NPC[] protectedTargets = new NPC[Main.maxNPCs];
    private readonly ulong[] protectionUntil = new ulong[Main.maxNPCs];
    public Vector2 ApproachVelocity => hasSnapshot && snapshotTick == Main.GameUpdateCount ? movementVelocity : Player.velocity;
    internal HeldLanceProjectile FindLance()
    {
        if (activeLance?.Projectile.active == true && activeLance.Projectile.ModProjectile == activeLance) return activeLance;
        activeLance = null;
        foreach (Projectile shot in Main.ActiveProjectiles)
            if (shot.owner == Player.whoAmI && shot.ModProjectile is HeldLanceProjectile lance) { activeLance = lance; break; }
        return activeLance;
    }
    public void CancelLance() { FindLance()?.Projectile.Kill(); activeLance = null; }
    internal void ProtectPassage(NPC target)
    {
        protectedTargets[target.whoAmI] = target;
        protectionUntil[target.whoAmI] = Main.GameUpdateCount + LancerTuning.ContactProtectionTicks;
    }
    internal bool Begin(LancerWeapon weapon, bool quick, float aim)
    {
        if (Player.whoAmI != Main.myPlayer || (quick && QuickCooldown > 0)) return false;
        Item item = Player.HeldItem;
        int damage = Player.GetWeaponDamage(item);
        float knockback = Player.GetWeaponKnockback(item);
        if (quick)
        {
            damage = Math.Max(1, (int)MathF.Round(damage * weapon.Settings.QuickDamageMultiplier));
            knockback = Player.GetWeaponKnockback(item, weapon.Settings.QuickKnockback);
            QuickCooldown = weapon.Settings.QuickUseTicks;
            HeldLanceProjectile existing = FindLance();
            if (existing != null && existing.Phase != LancePhase.QuickThrust)
            {
                existing.BeginQuick(aim, damage, knockback);
                return true;
            }
        }
        CancelLance();
        int slot = Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero,
            ModContent.ProjectileType<HeldLanceProjectile>(), damage, knockback, Player.whoAmI,
            quick ? (float)LancePhase.QuickThrust : (float)LancePhase.Preparing, 0, aim);
        activeLance = Main.projectile[slot].ModProjectile as HeldLanceProjectile;
        Player.itemTime = Player.itemAnimation = 2;
        return true;
    }
    public override void PreUpdateMovement()
    {
        movementVelocity = Player.velocity; snapshotTick = Main.GameUpdateCount; hasSnapshot = true;
        if (Player.whoAmI != Main.myPlayer) return;
        if (QuickCooldown > 0) QuickCooldown--;
        if (Player.dead || Player.HeldItem.ModItem is not LancerWeapon weapon)
        { if (activeLance != null) CancelLance(); return; }
        HeldLanceProjectile lance = FindLance();
        float aim = (Main.MouseWorld - Player.MountedCenter).SafeNormalize(Vector2.UnitX * Player.direction).ToRotation();
        if (Player.controlUseTile)
        {
            if ((lance == null || lance.Phase != LancePhase.QuickThrust) && QuickCooldown == 0) Begin(weapon, true, aim);
            else if (!Player.controlUseItem && lance?.Phase is LancePhase.Preparing or LancePhase.Ready) lance.RequestRetract();
        }
        else if (Player.controlUseItem)
        {
            if (lance == null || lance.Phase == LancePhase.Retracting) Begin(weapon, false, aim);
        }
        else if (lance != null && lance.Phase is LancePhase.Preparing or LancePhase.Ready) lance.RequestRetract();
    }
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        int slot = damageSource.SourceNPCIndex;
        if (slot < 0 || slot >= Main.maxNPCs || damageSource.SourceProjectileLocalIndex >= 0) return false;
        NPC target = Main.npc[slot];
        if (protectedTargets[slot] == target && protectionUntil[slot] > Main.GameUpdateCount) return true;
        return false; // Protection is earned only by a successful high-speed tip hit, never predicted contact.
    }
    public override void UpdateDead()
    {
        CancelLance(); QuickCooldown = 0; hasSnapshot = false;
        Array.Clear(protectedTargets); Array.Clear(protectionUntil);
    }
}
