using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;
using TornMod.Common.Glider;
using TornMod.Common.Lancer;
using TornMod.Content.Items;
using NVector = System.Numerics.Vector2;

namespace TornMod.Content.Projectiles;

public class HeldLanceProjectile : ModProjectile
{
    public override string Texture => "TornMod/Content/Items/IronLance";
    public LancePhase Phase => (LancePhase)(int)Projectile.ai[0];
    private readonly LancerController controller = new();
    private readonly NPC[] touched = new NPC[Main.maxNPCs];
    private readonly int[] separationTicks = new int[Main.maxNPCs];
    private readonly int[] contactCooldown = new int[Main.maxNPCs];
    private readonly NPC[] quickTouched = new NPC[Main.maxNPCs];
    private int syncTicks;
    private int sourceType, sourceSlot;
    private LancerSettings settings = LancerSettings.Iron;
    private Vector2 tail, tip, oldTail, oldTip, grip;
    private bool geometryValid, pendingImpact, pendingProtection;
    private uint impactVisualSerial;
    private LancerWeapon Definition => ItemLoader.GetItem(sourceType) as LancerWeapon;
    private static NVector N(Vector2 v) => new(v.X, v.Y);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true; Projectile.timeLeft = 2;
        Projectile.aiStyle = -1; Projectile.DamageType = DamageClass.Melee;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = LancerTuning.ContactInterval;
    }
    public override void OnSpawn(IEntitySource source)
    {
        sourceType = ModContent.ItemType<IronLance>(); sourceSlot = Main.player[Projectile.owner].selectedItem;
        if (source is EntitySource_ItemUse itemUse && itemUse.Item.ModItem is LancerWeapon weapon)
        { sourceType = itemUse.Item.type; settings = weapon.Settings; }
        Projectile.localNPCHitCooldown = settings.ContactInterval;
        Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(sourceType); writer.Write((byte)sourceSlot);
        writer.Write((byte)controller.ResumePhase); writer.Write(controller.ResumeAge);
        writer.Write(impactVisualSerial);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        sourceType = reader.ReadInt32(); sourceSlot = reader.ReadByte();
        LancePhase resumePhase = (LancePhase)reader.ReadByte(); int resumeAge = reader.ReadInt32();
        uint incomingVisualSerial = reader.ReadUInt32();
        if (sourceType <= 0 || sourceType >= ItemLoader.ItemCount || Definition == null ||
            sourceSlot >= Main.player[Projectile.owner].inventory.Length)
        { Projectile.Kill(); return; }
        settings = Definition.Settings;
        controller.SetResume(resumePhase == LancePhase.Ready ? LancePhase.Ready : LancePhase.Preparing,
            Math.Clamp(resumeAge, 0, settings.PrepareTicks));
        Projectile.localNPCHitCooldown = settings.ContactInterval;
        if (incomingVisualSerial != impactVisualSerial)
        {
            impactVisualSerial = incomingVisualSerial;
            ProtectionEffect();
        }
    }
    public void BeginQuick(float aim, int damage, float knockback)
    {
        controller.Load(Phase, (int)Projectile.ai[1], Projectile.ai[2]);
        controller.BeginQuick(aim);
        Projectile.ai[0] = (float)controller.Phase; Projectile.ai[1] = 0; Projectile.ai[2] = aim;
        Projectile.damage = damage; Projectile.knockBack = knockback;
        Array.Clear(quickTouched); geometryValid = false; Projectile.netUpdate = true;
    }
    public override bool? CanHitNPC(NPC target) => contactCooldown[target.whoAmI] > 0 ||
        (Phase == LancePhase.QuickThrust && quickTouched[target.whoAmI] == target) ? false : null;
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Phase is LancePhase.Ready or LancePhase.QuickThrust ? null : false;
    public void RequestRetract()
    {
        if (Phase is not (LancePhase.Preparing or LancePhase.Ready)) return;
        Projectile.ai[0] = (float)LancePhase.Retracting; Projectile.ai[1] = 0;
        Projectile.friendly = false; Projectile.netUpdate = true;
    }
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.selectedItem != sourceSlot || player.HeldItem.type != sourceType)
        { Projectile.Kill(); return; }
        bool owner = Projectile.owner == Main.myPlayer;
        for (int i = 0; i < contactCooldown.Length; i++)
            if (contactCooldown[i] > 0) contactCooldown[i]--;
        LancePhase before = Phase;
        float angle = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction).ToRotation();
        controller.Load(Phase, (int)Projectile.ai[1], Projectile.ai[2]);
        controller.Step(player.controlUseItem, angle, settings.PrepareTicks, owner);
        Projectile.ai[0] = (float)controller.Phase; Projectile.ai[1] = controller.Age; Projectile.ai[2] = controller.Aim;
        if (controller.Phase == LancePhase.Finished) { Projectile.Kill(); return; }
        if (before == LancePhase.QuickThrust && Phase is LancePhase.Preparing or LancePhase.Ready)
        {
            Projectile.damage = player.GetWeaponDamage(player.HeldItem);
            Projectile.knockBack = player.GetWeaponKnockback(player.HeldItem);
        }
        if (owner && (before != Phase || ++syncTicks >= 3)) { Projectile.netUpdate = true; syncTicks = 0; }
        Projectile.friendly = controller.Damaging;
        Projectile.timeLeft = 2; Projectile.Center = player.MountedCenter;
        player.heldProj = Projectile.whoAmI; player.itemTime = player.itemAnimation = 2;
        player.ChangeDir(MathF.Cos(controller.Aim) >= 0 ? 1 : -1);
        player.itemRotation = controller.Aim - (player.direction < 0 ? MathHelper.Pi : 0);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, controller.Aim - MathHelper.PiOver2);
        oldTail = tail; oldTip = tip;
        Geometry(player, controller.Aim, controller.Extension(settings.PrepareTicks), out grip, out tail, out tip);
        if (!geometryValid || before != Phase || Vector2.DistanceSquared(tail, oldTail) > LancerTuning.MaximumSweepDistance * LancerTuning.MaximumSweepDistance)
        { oldTail = tail; oldTip = tip; }
        geometryValid = true;
        if (Phase == LancePhase.Ready)
            for (int i = 0; i < touched.Length; i++)
            {
                NPC target = touched[i];
                if (target == null) continue;
                if (!target.active || Main.npc[i] != target) { touched[i] = null; continue; }
                if (Hits(target.Hitbox, oldTail, oldTip, tail, tip)) separationTicks[i] = 0;
                else if (++separationTicks[i] >= LancerTuning.ImpactSeparationTicks) touched[i] = null;
            }
    }
    private void Geometry(Player player, float aim, float extension, out Vector2 hand, out Vector2 start, out Vector2 end)
    {
        float lean = player.GetModPlayer<GliderPlayer>().Lean;
        hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2 - lean);
        hand = player.MountedCenter + (hand - player.MountedCenter).RotatedBy(lean) + aim.ToRotationVector2() * extension;
        start = hand - aim.ToRotationVector2() * settings.Length * 0.5f;
        end = start + aim.ToRotationVector2() * settings.Length;
    }
    private bool Hits(Rectangle target, Vector2 previousTail, Vector2 previousTip, Vector2 currentTail, Vector2 currentTip) =>
        LancerMechanics.SweptHit(LancerMechanics.TipStart(N(previousTail), N(previousTip)), N(previousTip),
            LancerMechanics.TipStart(N(currentTail), N(currentTip)), N(currentTip),
            new NVector(target.Left, target.Top), new NVector(target.Right, target.Bottom), LancerTuning.HitRadius);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        geometryValid && Hits(targetHitbox, oldTail, oldTip, tail, tip);
    public float ApproachSpeed(NPC target) => LancerMechanics.ApproachSpeed(
        N(Main.player[Projectile.owner].GetModPlayer<LancerPlayer>().ApproachVelocity), N(target.velocity), Projectile.ai[2]);
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        pendingImpact = false;
        pendingProtection = false;
        if (Phase == LancePhase.QuickThrust)
        {
            Player player = Main.player[Projectile.owner];
            modifiers.HitDirectionOverride = target.Center.X == player.Center.X ?
                (MathF.Cos(Projectile.ai[2]) >= 0 ? 1 : -1) : (target.Center.X > player.Center.X ? 1 : -1);
            return;
        }
        if (Phase != LancePhase.Ready || touched[target.whoAmI] == target) return;
        touched[target.whoAmI] = target;
        separationTicks[target.whoAmI] = 0;
        float speed = ApproachSpeed(target);
        float bonus = LancerMechanics.ImpactBonus(speed);
        modifiers.SourceDamage *= 1f + bonus;
        pendingImpact = bonus > 0;
        pendingProtection = LancerMechanics.ApproachSpeed(
            N(Main.player[Projectile.owner].GetModPlayer<LancerPlayer>().ApproachVelocity), NVector.Zero,
            Projectile.ai[2]) > LancerTuning.ProtectionSpeed;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        contactCooldown[target.whoAmI] = settings.ContactInterval;
        if (Phase == LancePhase.QuickThrust) quickTouched[target.whoAmI] = target;
        if (pendingImpact && pendingProtection)
        {
            Player player = Main.player[Projectile.owner];
            player.GetModPlayer<LancerPlayer>().ProtectPassage(target);
            if (Projectile.owner == Main.myPlayer)
            {
                player.velocity += Projectile.ai[2].ToRotationVector2() * LancerTuning.ImpactForwardBoost;
                player.GetModPlayer<GliderPlayer>().AcceptExternalMomentum();
                impactVisualSerial++; Projectile.netUpdate = true;
                ProtectionEffect();
                if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
                    NetMessage.SendData(Terraria.ID.MessageID.PlayerControls, number: player.whoAmI);
            }
        }
        pendingImpact = false;
        pendingProtection = false;
    }
    private void ProtectionEffect()
    {
        if (Main.dedServ) return;
        Player player = Main.player[Projectile.owner];
        SoundEngine.PlaySound(SoundID.Item37, player.Center);
        Vector2 axis = Projectile.ai[2].ToRotationVector2();
        for (int i = 0; i < 16; i++)
        {
            Vector2 ring = (MathHelper.TwoPi * i / 16f).ToRotationVector2();
            Dust dust = Dust.NewDustPerfect(player.Center + ring * 18f, DustID.GoldFlame,
                ring * 2.5f - axis * 3f, 100, default, 1.2f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!geometryValid) return false;
        Main.instance.LoadItem(sourceType);
        Texture2D texture = TextureAssets.Item[sourceType].Value;
        Rectangle frame = Definition?.SpriteFrame(texture) ?? texture.Bounds;
        Main.EntitySpriteDraw(texture, grip - Main.screenPosition, frame, lightColor, Projectile.ai[2],
            new Vector2(frame.Width * 0.5f, frame.Height * 0.5f), settings.Length / frame.Width,
            MathF.Cos(Projectile.ai[2]) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
        return false;
    }
}
