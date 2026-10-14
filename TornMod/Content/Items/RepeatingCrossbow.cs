using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TornMod.Content.Items;

public class RepeatingCrossbow : ModItem
{
    public const int Capacity = 3, ShotTicks = 10, ReloadTicks = 60;
    private int shots = Capacity;
    private int reloadRemaining;
    private ulong syncTick;
    public float ShotAim { get; private set; }
    private bool hasShotAim;
    public int ShotsRemaining => reloadRemaining > 0 ? Math.Min(Capacity, (ReloadTicks - reloadRemaining) / 15) : shots;
    public int ReloadRemaining => reloadRemaining;
    public override void SetDefaults()
    {
        Item.width = 64; Item.height = 28;
        Item.damage = 12; Item.knockBack = 2f; Item.crit = 4;
        Item.DamageType = DamageClass.Ranged;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = ShotTicks;
        Item.autoReuse = true;
        Item.noMelee = Item.noUseGraphic = true;
        Item.useAmmo = AmmoID.Arrow;
        Item.shoot = ProjectileID.WoodenArrowFriendly;
        Item.shootSpeed = 12f;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(silver: 25);
    }
    // Keep the requested burst interval fixed; reloading is independent of attack speed.
    public override float UseSpeedMultiplier(Player player) => 1f / Math.Max(0.1f, player.GetTotalAttackSpeed(DamageClass.Ranged));
    public override bool CanUseItem(Player player) => reloadRemaining == 0 && shots > 0 && player.HasAmmo(Item);
    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        float aim = hasShotAim ? ShotAim : player.itemRotation + (player.direction < 0 ? MathHelper.Pi : 0f);
        player.ChangeDir(MathF.Cos(aim) < 0f ? -1 : 1);
        player.itemRotation = MathHelper.WrapAngle(aim - (player.direction < 0 ? MathHelper.Pi : 0f));
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
    }
    internal void RecordShot(Player player)
    {
        if (reloadRemaining > 0 || shots <= 0) return;
        if (--shots == 0) reloadRemaining = ReloadTicks;
        Sync(player);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer || !CanUseItem(player)) return false;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        ShotAim = aim.ToRotation();
        hasShotAim = true;
        UseStyle(player, default);
        Vector2 muzzle = player.MountedCenter + aim * 34f;
        if (!Collision.CanHitLine(player.MountedCenter, 1, 1, muzzle, 1, 1)) muzzle = player.MountedCenter;
        Projectile.NewProjectile(source, muzzle, aim * velocity.Length(), type, damage, knockback, player.whoAmI);
        RecordShot(player);
        SoundEngine.PlaySound(SoundID.Item5, player.Center);
        return false;
    }
    public override void UpdateInventory(Player player)
    {
        if (player.whoAmI != Main.myPlayer || reloadRemaining <= 0) return;
        if (--reloadRemaining == 0) shots = Capacity;
        if (reloadRemaining % 15 == 0) Sync(player);
    }
    private void Sync(Player player)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        int slot = Array.IndexOf(player.inventory, Item);
        if (slot >= 0) NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: slot);
    }
    public int VisualFrame(Player player)
    {
        int remaining = reloadRemaining;
        if (player.whoAmI != Main.myPlayer && remaining > 0)
            remaining = Math.Max(0, remaining - (int)Math.Min(60UL, Main.GameUpdateCount - syncTick));
        if (reloadRemaining > 0)
            return remaining > 45 ? 4 : remaining > 30 ? 5 : remaining > 15 ? 6 : 7;
        return shots >= 3 ? (player.itemAnimation > 0 ? 1 : 0) : shots == 2 ? 2 : shots == 1 ? 3 : 4;
    }
    public override void SaveData(TagCompound tag) { tag["Shots"] = shots; tag["Reload"] = reloadRemaining; }
    public override void LoadData(TagCompound tag)
    {
        shots = tag.ContainsKey("Shots") ? Math.Clamp(tag.GetInt("Shots"), 0, Capacity) : Capacity;
        reloadRemaining = Math.Clamp(tag.GetInt("Reload"), 0, ReloadTicks);
        if (shots == 0 && reloadRemaining == 0) reloadRemaining = ReloadTicks;
        syncTick = Main.GameUpdateCount;
    }
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)shots); writer.Write((byte)reloadRemaining);
        writer.Write(hasShotAim); writer.Write(ShotAim);
    }
    public override void NetReceive(BinaryReader reader)
    {
        shots = Math.Clamp((int)reader.ReadByte(), 0, Capacity);
        reloadRemaining = Math.Clamp((int)reader.ReadByte(), 0, ReloadTicks);
        hasShotAim = reader.ReadBoolean();
        float aim = reader.ReadSingle();
        ShotAim = float.IsFinite(aim) ? aim : 0f;
        syncTick = Main.GameUpdateCount;
    }
    public static Rectangle SpriteFrame(int index)
    {
        // Four columns and two rows; align the stock across both rows.
        int left = (index % 4) switch { 0 => 0, 1 => 466, 2 => 902, _ => 1332 };
        int right = (index % 4) switch { 0 => 466, 1 => 902, 2 => 1332, _ => 1774 };
        return new Rectangle(left, index < 4 ? 160 : 528, right - left, 225);
    }
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = TextureAssets.Item[Type].Value;
        Rectangle crop = SpriteFrame(0);
        spriteBatch.Draw(texture, position, crop, drawColor, 0f, crop.Size() / 2f,
            scale * texture.Width / crop.Width, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        Rectangle crop = SpriteFrame(0);
        spriteBatch.Draw(TextureAssets.Item[Type].Value, Item.Center - Main.screenPosition, crop, lightColor,
            rotation, crop.Size() / 2f, 64f / crop.Width * scale, SpriteEffects.None, 0);
        return false;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Wood, 15)
        .AddRecipeGroup(RecipeGroupID.IronBar, 8).AddIngredient(ItemID.Chain, 2).AddTile(TileID.Anvils).Register();
}
