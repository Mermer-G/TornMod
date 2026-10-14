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
using TornMod.Common.BoltAction;
using TornMod.Content.Projectiles;

namespace TornMod.Content.Items;

public class Piercer : ModItem
{
    public bool NeedsBolt { get; private set; }
    public override void SetDefaults()
    {
        var musket = new Item(); musket.SetDefaults(ItemID.Musket);
        Item.damage = System.Math.Max(1, (int)System.MathF.Round(musket.damage * 1.5f));
        Item.DamageType = ModContent.GetInstance<BoltActionDamageClass>();
        Item.width = 82; Item.height = 20;
        Item.useTime = Item.useAnimation = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = true;
        Item.knockBack = 6f;
        Item.crit = 4;
        Item.useAmmo = AmmoID.Bullet;
        Item.shoot = ModContent.ProjectileType<PiercerBulletProjectile>();
        Item.shootSpeed = 24f;
        Item.rare = ItemRarityID.Green;
        Item.value = Item.buyPrice(gold: 1);
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) =>
        player.ownedProjectileCounts[ModContent.ProjectileType<PiercerReloadProjectile>()] == 0 &&
        (player.altFunctionUse == 2 ? NeedsBolt : !NeedsBolt);
    public override bool NeedsAmmo(Player player) => player.altFunctionUse != 2;
    public override bool CanConsumeAmmo(Item ammo, Player player) => player.altFunctionUse != 2;
    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        float aim = player.itemRotation + (player.direction < 0 ? MathHelper.Pi : 0f);
        if (player.whoAmI == Main.myPlayer) aim = (Main.MouseWorld - player.MountedCenter).ToRotation();
        PiercerDrawing.PoseArms(player, aim);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        float aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction).ToRotation();
        if (player.altFunctionUse == 2)
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                ModContent.ProjectileType<PiercerReloadProjectile>(), 0, 0f, player.whoAmI, 0f, player.selectedItem, aim);
        else
        {
            RecordShot(player);
            Vector2 muzzle = PiercerDrawing.Muzzle(player, aim);
            if (!Collision.CanHitLine(player.MountedCenter, 1, 1, muzzle, 1, 1)) muzzle = player.MountedCenter;
            Projectile.NewProjectile(source, muzzle, velocity, ModContent.ProjectileType<PiercerBulletProjectile>(),
                damage, knockback, player.whoAmI);
            SoundEngine.PlaySound(SoundID.Item40 with { Volume = 0.75f }, player.Center);
        }
        return false;
    }
    public void CompleteBolt(Player player) { NeedsBolt = false; SyncItem(player); }
    internal void RecordShot(Player player) { NeedsBolt = true; SyncItem(player); }
    private void SyncItem(Player player)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: player.selectedItem);
    }
    public override void SaveData(TagCompound tag) => tag["NeedsBolt"] = NeedsBolt;
    public override void LoadData(TagCompound tag) => NeedsBolt = tag.GetBool("NeedsBolt");
    public override void NetSend(BinaryWriter writer) => writer.Write(NeedsBolt);
    public override void NetReceive(BinaryReader reader) => NeedsBolt = reader.ReadBoolean();
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = TextureAssets.Item[Type].Value;
        Rectangle crop = PiercerDrawing.ClosedFrame;
        // Inventory scale already compensates for the full 1254px texture; undo its empty margins.
        spriteBatch.Draw(texture, position, crop, drawColor, 0f, crop.Size() / 2f,
            scale * texture.Width / crop.Width, SpriteEffects.None, 0f);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        Rectangle crop = PiercerDrawing.ClosedFrame;
        spriteBatch.Draw(TextureAssets.Item[Type].Value, Item.Center - Main.screenPosition, crop, lightColor,
            rotation, crop.Size() / 2f, 82f / crop.Width * scale, SpriteEffects.None, 0f);
        return false;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Musket)
        .AddRecipeGroup(RecipeGroupID.IronBar, 10).AddIngredient(ItemID.Wood, 10).AddTile(TileID.Anvils).Register();
}
