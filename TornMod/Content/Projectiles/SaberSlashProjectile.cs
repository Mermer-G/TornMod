using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common;
using TornMod.Common.SaberCombat;

namespace TornMod.Content.Projectiles;

// A player-attached melee hitbox and animation, never a flying sword projectile.
public class SaberSlashProjectile : ModProjectile
{
    public override string Texture => "TornMod/Content/Items/Saber";
    private bool Charged => Projectile.ai[1] > 0f;
    public override bool? CanDamage() => Projectile.ai[1] == 2f ? false : null;
    private float AttackSpeed => MathF.Max(0.1f, Main.player[Projectile.owner].GetTotalAttackSpeed(ModContent.GetInstance<SaberDamageClass>()));
    private float AnimationTicks => MathF.Max(1f, WeaponBalance.SaberSlashTicks / AttackSpeed);
    private float Progress => MathHelper.Clamp(Projectile.localAI[0] / AnimationTicks, 0f, 1f);
    private float SwingAngle => Projectile.ai[0] + SaberMechanics.SwingOffset(Projectile.localAI[0] * AttackSpeed) *
        (SaberDrawing.FacesLeft(Projectile.ai[0]) ? -1f : 1f);
    private bool registeredHit;

    public override void SetStaticDefaults() => ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = ModContent.GetInstance<SaberDamageClass>();
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.aiStyle = -1;
        Projectile.timeLeft = 2;
        Projectile.hide = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        Projectile.Center = player.MountedCenter;
        player.heldProj = Projectile.whoAmI;
        if (Charged) player.itemTime = player.itemAnimation = 2;
        player.ChangeDir(MathF.Cos(Projectile.ai[0]) >= 0f ? 1 : -1);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, SwingAngle - MathHelper.PiOver2);
        if (Projectile.localAI[0] == 0f && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.3f, Volume = 0.65f }, Projectile.Center);
        if (player.GetModPlayer<SaberCombatPlayer>().HitStopThisTick) return;
        if (++Projectile.localAI[0] >= AnimationTicks) Projectile.Kill();
    }
    public override bool? CanHitNPC(NPC target) =>
        Charged && Projectile.ai[2] > 0f && target.whoAmI != (int)Projectile.ai[2] - 1 ? false : null;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Player player = Main.player[Projectile.owner];
        if (Charged && Projectile.ai[2] > 0f)
        {
            Rectangle contact = player.Hitbox;
            contact.Inflate(8, 8);
            if (contact.Intersects(targetHitbox)) return true;
        }
        float point = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), player.MountedCenter,
            player.MountedCenter + SwingAngle.ToRotationVector2() * WeaponBalance.SaberLength, 12f, ref point);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.HitDirectionOverride = Main.player[Projectile.owner].Center.X < target.Center.X ? 1 : -1;
        if (!Charged) return;
        modifiers.SetCrit();
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Charged && !registeredHit && Projectile.owner == Main.myPlayer)
        {
            registeredHit = true;
            Main.player[Projectile.owner].GetModPlayer<SaberCombatPlayer>().RegisterChargedHit();
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        // Native sword points up-right; pivot at the grip in the supplied 1254px image.
        Texture2D sword = TextureAssets.Projectile[Type].Value;
        float length = WeaponBalance.SaberLength;
        Vector2 center = Projectile.Center - Main.screenPosition;
        // Rapid narrow afterimages use the same geometry for normal and charged attacks.
        for (int trail = 3; trail >= 0; trail--)
        {
            float angle = SwingAngle - trail * 0.08f * (SaberDrawing.FacesLeft(Projectile.ai[0]) ? -1f : 1f);
            Color color = trail == 0 ? Color.Lerp(lightColor, Color.White, 0.5f) : Color.LightSkyBlue * (0.22f * (4 - trail));
            Main.EntitySpriteDraw(sword, center, null, color, SaberDrawing.Rotation(angle),
                SaberDrawing.Grip(sword, angle), length / (sword.Height * 1.1f), SaberDrawing.Effects(angle), 0);
        }
        Main.instance.LoadProjectile(ProjectileID.Terragrim);
        Texture2D arc = TextureAssets.Projectile[ProjectileID.Terragrim].Value;
        int frames = Math.Max(1, Main.projFrames[ProjectileID.Terragrim]);
        Rectangle frame = arc.Frame(1, frames, 0, (int)Projectile.localAI[0] % frames);
        float effectLength = length * 1.4f;
        Vector2 scale = new(effectLength / frame.Width, 32f / frame.Height);
        // Additive blue glow makes the narrow Terragrim cut readable in dark areas.
        Color glow = new Color(100, 190, 255, 0);
        Main.EntitySpriteDraw(arc, center + Projectile.ai[0].ToRotationVector2() * length * 0.45f,
            frame, glow * (1f - Progress * 0.4f), SaberDrawing.ArcRotation(Projectile.ai[0]), frame.Size() / 2f,
            scale * 1.15f, SaberDrawing.ArcEffects(Projectile.ai[0]), 0);
        Main.EntitySpriteDraw(arc, center + Projectile.ai[0].ToRotationVector2() * length * 0.45f,
            frame, Color.White * (1f - Progress * 0.35f), SaberDrawing.ArcRotation(Projectile.ai[0]), frame.Size() / 2f,
            scale, SaberDrawing.ArcEffects(Projectile.ai[0]), 0);
        return false;
    }
}


