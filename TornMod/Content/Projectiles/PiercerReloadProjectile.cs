using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Common.BoltAction;
using TornMod.Content.Items;

namespace TornMod.Content.Projectiles;

public class PiercerReloadProjectile : ModProjectile
{
    private Item reloadItem;
    public override string Texture => "TornMod/Content/Items/PiercerReload";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.hide = true; Projectile.timeLeft = 2;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        int slot = (int)Projectile.ai[1];
        if (!player.active || player.dead || slot < 0 || slot >= player.inventory.Length || player.selectedItem != slot ||
            player.HeldItem.ModItem is not Piercer rifle || !rifle.NeedsBolt)
        { Projectile.Kill(); return; }
        reloadItem ??= player.HeldItem;
        if (!ReferenceEquals(reloadItem, player.HeldItem)) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2; Projectile.Center = player.MountedCenter;
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        player.ChangeDir(MathF.Cos(Projectile.ai[2]) >= 0 ? 1 : -1);
        PiercerDrawing.PoseArms(player, Projectile.ai[2]);
        int tick = (int)Projectile.ai[0];
        if (!Main.dedServ && (tick == 0 || tick == BoltMechanics.CloseStart))
            SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.65f, Pitch = tick == 0 ? -0.3f : 0.1f }, player.Center);
        if (tick == BoltMechanics.EjectTick && Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.MountedCenter + new Vector2(player.direction * 12, -6),
                new Vector2(player.direction * 2f, -5.5f), ModContent.ProjectileType<HotCasingProjectile>(), 0, 0f, player.whoAmI);
        if (++Projectile.ai[0] >= BoltMechanics.ReloadTicks)
        {
            if (Projectile.owner == Main.myPlayer) rifle.CompleteBolt(player);
            Projectile.Kill();
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        // The shared held-item layer draws the reload gun under the hands at the shoulder.
        return false;
    }
}
