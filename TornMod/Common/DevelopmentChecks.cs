using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TornMod.Content.Items;
using TornMod.Content.Projectiles;
using TornMod.Common.SaberCombat;

namespace TornMod.Common;

// Opt-in loader-time checks only. Never modify an actual player/world session.
public class DevelopmentChecks : ModSystem
{
    public override void PostAddRecipes()
    {
        if (!Main.dedServ || !Program.LaunchParameters.ContainsKey("-slipchecks")) return;
        bool exitAfterChecks = Program.LaunchParameters.ContainsKey("-slipchecks-exit");
        try
        {
            RunChecks();
            if (exitAfterChecks) Environment.Exit(0);
        }
        catch (Exception exception)
        {
            Console.WriteLine("SLIPCHECKS FAILED: " + exception);
            if (exitAfterChecks) Environment.Exit(1);
            throw;
        }
    }

    private void RunChecks()
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Slipper check failed: " + message);
            checks++;
        }

        int momType = ModContent.ItemType<MomsSlipper>();
        int slipperonType = ModContent.ItemType<Slipperon>();
        Check(global::TornMod.Common.Commands.GiveCommand.Find("Saber").Contains(ModContent.ItemType<global::TornMod.Content.Items.Saber>()), "Give resolves a mod weapon by name");
        Check(global::TornMod.Common.Commands.GiveCommand.Find("TornMod/RepeatingCrossbow").SequenceEqual(new[] { ModContent.ItemType<RepeatingCrossbow>() }), "Give resolves an exact mod/item name");
        Check(global::TornMod.Common.Commands.GiveCommand.Find("MusketBall").SequenceEqual(new int[] { ItemID.MusketBall }), "Give resolves vanilla internal item names");
        Check(global::TornMod.Common.Commands.GiveCommand.Find(ItemID.WoodenArrow.ToString()).SequenceEqual(new int[] { ItemID.WoodenArrow }), "Give resolves numeric item IDs");
        Check(global::TornMod.Common.Commands.GiveCommand.Find("999999").Count == 0 &&
            global::TornMod.Common.Commands.GiveCommand.Find("item-does-not-exist").Count == 0, "Give rejects unknown IDs and names");
        var momItem = new Item();
        int spawnRate = 600, spawnMaximum = 5;
        global::TornMod.Common.Commands.TestSpawnSystem.Apply(0f, ref spawnRate, ref spawnMaximum);
        Check(spawnRate == int.MaxValue && spawnMaximum == 0, "Zero spawn multiplier disables natural NPC spawning");
        spawnRate = 600; spawnMaximum = 5;
        global::TornMod.Common.Commands.TestSpawnSystem.Apply(5f, ref spawnRate, ref spawnMaximum);
        Check(spawnRate == 120 && spawnMaximum == 25, "Fivefold spawn multiplier changes frequency and population");
        spawnRate = 600; spawnMaximum = 5;
        global::TornMod.Common.Commands.TestSpawnSystem.Apply(0.5f, ref spawnRate, ref spawnMaximum);
        Check(spawnRate == 1200 && spawnMaximum == 3, "Fractional spawn multiplier reduces natural spawning");
        spawnRate = 1; spawnMaximum = 10;
        global::TornMod.Common.Commands.TestSpawnSystem.Apply(100f, ref spawnRate, ref spawnMaximum);
        Check(spawnRate == 1 && spawnMaximum == 200, "Spawn controls retain valid rate and population bounds");
        Check(global::TornMod.Common.Commands.BossCommand.Resolve("eye") == NPCID.EyeofCthulhu &&
            global::TornMod.Common.Commands.BossCommand.Resolve("EyeofCthulhu") == NPCID.EyeofCthulhu,
            "Boss command resolves shorthand and vanilla NPC names");
        Check(global::TornMod.Common.Commands.BossCommand.Resolve("not-a-boss") == -1 &&
            global::TornMod.Common.Commands.BossCommand.Resolve("999999") == -1, "Invalid boss inputs are rejected");
        momItem.SetDefaults(momType);
        Check(momItem.ModItem.Tooltip.Value.Contains("Consumes one slipper"), "Mom tooltip resolves to the correct English localization key");
        foreach (int weaponType in new[] { slipperonType, ModContent.ItemType<global::TornMod.Content.Items.Saber>(), ModContent.ItemType<Piercer>() })
        {
            var weapon = new Item(); weapon.SetDefaults(weaponType);
            Check(weapon.ModItem.Tooltip.Value.Length > 40 && weapon.ModItem.Tooltip.Value != weapon.ModItem.Tooltip.Key,
                "Every weapon has a resolved English tooltip");
        }
        Check(typeof(WeaponConfig).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Length == 3,
            "Gameplay config contains Instant Respawn, Invincibility and Warp to Cursor");
        Check(WeaponBalance.SaberDamage == 12 && WeaponBalance.SaberDashTicks == 12 && WeaponBalance.SaberLength == 65f &&
            WeaponBalance.SaberChargedCritMultiplier == 4f && WeaponBalance.SaberHitStopTicks == 10,
            "Saber balance is frozen from the active saved configuration");
        Check(momItem.damage == 12 && momItem.crit == 4 && momItem.consumable && momItem.maxStack >= 1000, "Mom item defaults");
        var spinnerItem = new Item();
        spinnerItem.SetDefaults(slipperonType);
        Check(spinnerItem.damage == 29 && !spinnerItem.consumable && spinnerItem.DamageType == DamageClass.Ranged, "Slipperon item defaults");
        int momRecipes = 0;
        int spinnerRecipes = 0;
        for (int i = 0; i < Recipe.numRecipes; i++)
        {
            Recipe recipe = Main.recipe[i];
            if (recipe.createItem.type == momType)
            {
                momRecipes++;
                Check(recipe.createItem.stack == 25 && recipe.requiredTile.Contains(TileID.Loom), "Mom result and station");
                Check(HasIngredient(recipe, ItemID.Silk, 1) && HasIngredient(recipe, ItemID.Wood, 2), "Mom ingredients");
            }
            if (recipe.createItem.type == slipperonType)
            {
                spinnerRecipes++;
                Check(recipe.createItem.stack == 1 && recipe.requiredTile.Contains(TileID.Anvils), "Slipperon result and station");
                Check(HasIngredient(recipe, momType, 1000) && HasIngredient(recipe, ItemID.FallenStar, 5), "Slipperon ingredients");
                Check(recipe.acceptedGroups.Contains(RecipeGroup.recipeGroupIDs["TornMod:GoldOrPlatinumBar"]) &&
                    HasIngredient(recipe, ItemID.GoldBar, 10), "Gold/platinum substitution");
            }
        }
        Check(momRecipes == 1 && spinnerRecipes == 1, "Exactly one recipe per weapon");

        NPC saved0 = Main.npc[0];
        NPC saved1 = Main.npc[1];
        Player savedPlayer = Main.player[0];
        int savedMyPlayer = Main.myPlayer;
        try
        {
            Main.myPlayer = 0;
            Main.player[0] = new Player { active = true, dead = false };
            Main.player[0].Center = Vector2.Zero;
            var glideItem = new Item(); glideItem.SetDefaults(ModContent.ItemType<GliderWings>());
            Player glidePlayer = Main.player[0];
            var glide = glidePlayer.GetModPlayer<global::TornMod.Common.Glider.GliderPlayer>();
            Check(glideItem.accessory && glideItem.wingSlot <= 0 && glideItem.ModItem.Tooltip.Value.Contains("Hold Jump"),
                "Glider Wings is an accessory with an English tooltip and no normal flight slot");
            Check(!Main.recipe.Take(Recipe.numRecipes).Any(r => r.createItem.type == glideItem.type), "Glider has no crafting recipe yet");
            int wingsBefore = glidePlayer.wingsLogic;
            glideItem.ModItem.UpdateAccessory(glidePlayer, true);
            Check(glide.Equipped && glide.HideWings && glidePlayer.wingsLogic == wingsBefore, "Equipping glider respects visual hiding without enabling wings");
            glide.ResetEffects();
            Check(!glide.Equipped && !glide.HideWings, "Glider equipment flags reset each tick");
            glidePlayer.gravDir = 1;
            glidePlayer.velocity = new Vector2(10, 0.5f);
            glide.ApplyNetworkState(true, false, new System.Numerics.Vector2(10, 0.5f));
            for (int tick = 0; tick < 30; tick++) glide.PostUpdate();
            Check(glide.WingOpen > 0.95f && glide.Lean > 0.4f && glide.Lean < 0.62f, "Glide unfolds and approaches a 20–35 degree right lean");
            var draw = new Terraria.DataStructures.PlayerDrawSet {
                drawPlayer = glidePlayer, Position = glidePlayer.position,
                DrawDataCache = new System.Collections.Generic.List<Terraria.DataStructures.DrawData>()
            };
            Vector2 pivot = glidePlayer.MountedCenter - Main.screenPosition;
            for (int i = 0; i < 3; i++) draw.DrawDataCache.Add(new Terraria.DataStructures.DrawData(null,
                pivot + new Vector2(i * 8, -12), new Rectangle(0, 0, 1, 1), Color.White, 0f, Vector2.Zero, 1f,
                Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0));
            glide.TransformDrawData(ref draw);
            for (int i = 0; i < 3; i++) Check(MathF.Abs(draw.DrawDataCache[i].rotation - glide.Lean) < 0.001f &&
                MathF.Abs(Vector2.Distance(draw.DrawDataCache[i].position, pivot) - new Vector2(i * 8, -12).Length()) < 0.001f,
                "Body, armor and held-item cache entries share the same rigid rotation");
            glidePlayer.velocity = new Vector2(-10, 1);
            Main.myPlayer = Main.maxPlayers; // Exercise the receiving peer, rather than owner input.
            glide.ApplyNetworkState(true, true, new System.Numerics.Vector2(-10, 1));
            for (int tick = 0; tick < 60; tick++) glide.PostUpdate();
            Check(glide.Lean < -1f && glide.DiveBlend > 0.99f, "Remote dive state produces a smooth left-facing dive pose");
            Main.myPlayer = 0;
            glidePlayer.wingsLogic = 1; glidePlayer.wingTime = 30;
            Check(glide.Blocked, "Wings with flight capacity take priority over gliding");
            glidePlayer.wingTime = 0;
            Check(!glide.Blocked, "Exhausted wings allow gliding"); glidePlayer.wingsLogic = wingsBefore;
            var bridge = new global::TornMod.Common.Glider.GliderFlightBridge();
            glidePlayer.controlJump = true; glidePlayer.jump = 0;
            glidePlayer.rocketBoots = 2; glidePlayer.rocketTime = 5; glidePlayer.rocketTimeMax = 5;
            glidePlayer.wingsLogic = 1; glidePlayer.wingTime = 120;
            glidePlayer.rocketRelease = false;
            bridge.Prepare(glidePlayer, true);
            Check(glidePlayer.wingsLogic == 0 && glidePlayer.rocketTime == 5 && glidePlayer.rocketRelease,
                "Held Jump re-arms rockets and temporarily protects boots fuel from wing conversion");
            glidePlayer.wingTime = 0; // Vanilla's no-wings branch.
            bridge.RestoreWings(glidePlayer);
            Check(glidePlayer.wingsLogic == 1 && glidePlayer.wingTime == 120 && glidePlayer.rocketTime == 5,
                "Flight bridge restores the exact wing reserve without adding fuel");
            glidePlayer.wingsLogic = wingsBefore; glidePlayer.wingTime = 0;
            Check(glide.Blocked, "Remaining rocket fuel takes priority over normal gliding");
            glidePlayer.rocketTime = 0; glidePlayer.rocketDelay = 1;
            Check(glide.Blocked, "Last rocket burst completes before glide handoff");
            glidePlayer.rocketDelay = 0;
            Check(!glide.Blocked, "Exhausted rocket boots allow gliding");
            glidePlayer.rocketBoots = 0; glidePlayer.rocketFrame = true;
            Check(!global::TornMod.Common.Glider.GliderFlightBridge.RocketThrust(glidePlayer), "No boots means no redirected rocket thrust");
            glidePlayer.rocketFrame = false;
            ref var cloud = ref glidePlayer.GetJumpState<Terraria.ModLoader.CloudInABottleJump>();
            cloud.Enable(); cloud.Available = true;
            glidePlayer.wingsLogic = 1; glidePlayer.wingTime = 80;
            glidePlayer.releaseJump = false;
            bridge.Prepare(glidePlayer, true);
            Check(glidePlayer.releaseJump && glide.Blocked && glidePlayer.wingsLogic == 0,
                "Held Jump re-arms a double jump and defers wing flight until the jump is used");
            bridge.RestoreWings(glidePlayer);
            Check(glidePlayer.wingTime == 80, "Double-jump priority preserves the complete wing reserve");
            glidePlayer.wingsLogic = wingsBefore; glidePlayer.wingTime = 0;
            cloud.Disable();
            glidePlayer.controlJump = false;
            glidePlayer.releaseJump = false; glidePlayer.rocketRelease = false;
            bridge.Prepare(glidePlayer, false);
            Check(!glidePlayer.releaseJump && !glidePlayer.rocketRelease, "Outside glider chaining, vanilla release latches are untouched");
            Main.myPlayer = Main.maxPlayers;
            glidePlayer.velocity = new Vector2(0, -5);
            glide.ApplyNetworkState(true, false, new System.Numerics.Vector2(0, -5), true, -MathHelper.PiOver2);
            for (int tick = 0; tick < 30; tick++) glide.PostUpdate();
            Check(glide.Gliding && glide.Acrobatics && !glide.Boosting, "Receiving peer accepts an unpowered upward acrobatic climb");
            glide.ApplyNetworkState(true, false, new System.Numerics.Vector2(0, -5), true, MathHelper.PiOver2);
            for (int tick = 0; tick < 60; tick++) glide.PostUpdate();
            Check(MathF.Abs(global::TornMod.Common.Glider.GliderController.WrapAngle(glide.Lean - MathHelper.Pi)) < 0.01f,
                "Mouse-down acrobat pose can rotate completely upside down");
            Main.myPlayer = 0;
            glidePlayer.wet = true; Check(glide.Blocked, "Liquids block the glider"); glidePlayer.wet = false;
            glidePlayer.grapCount = 1; Check(glide.Blocked, "Grappling blocks the glider"); glidePlayer.grapCount = 0;
            glidePlayer.carpet = true; glidePlayer.carpetTime = 1;
            Check(glide.Blocked, "Active carpet flight blocks the glider"); glidePlayer.carpet = false; glidePlayer.carpetTime = 0;
            glide.ApplyNetworkState(false, false, System.Numerics.Vector2.Zero);
            Vector2 releasedVelocity = glidePlayer.velocity;
            glide.PreUpdateMovement();
            Check(glidePlayer.velocity == releasedVelocity, "Inactive glider leaves vanilla velocity untouched");
            for (int tick = 0; tick < 60; tick++) glide.PostUpdate();
            Check(glide.WingOpen < 0.001f && MathF.Abs(glide.Lean) < 0.001f, "Release folds wings and returns to an upright pose");
            glide.UpdateDead(); glidePlayer.velocity = Vector2.Zero;
            var customKeys = new System.Collections.Generic.List<string> { "J" };
            Check(global::TornMod.Common.Glider.GliderKeybinds.ResolvePressed(false, true, null), "An unbound/new profile resolves a default W press");
            Check(global::TornMod.Common.Glider.GliderKeybinds.ResolvePressed(true, false, customKeys), "Explicit custom keybind edge is honored");
            Check(!global::TornMod.Common.Glider.GliderKeybinds.ResolvePressed(false, true, customKeys), "Custom rebind prevents W fallback from taking over");
            Check(global::TornMod.Common.Glider.GliderKeybinds.ResolvePressed(false, true, new System.Collections.Generic.List<string> { "W" }),
                "Default W also resolves through vanilla Up when the two actions share that key");
            Check(!global::TornMod.Common.Glider.GliderKeybinds.ResolvePressed(false, false, null), "Holding W without a new edge does not toggle again");
            glide.ResetEffects();
            glidePlayer.velocity = new Vector2(12, 1);
            glide.QueueToggleRequest(true); // Input occurs before the current equipment update.
            glideItem.ModItem.UpdateAccessory(glidePlayer, false);
            Check(glide.ApplyPendingToggle(false) && glide.Acrobatics && glidePlayer.velocity == new Vector2(12, 1),
                "Queued key uses this tick's equipment and switches mode without resetting momentum");
            Check(!glide.ApplyPendingToggle(false) && glide.Acrobatics, "One queued edge switches only once");
            glide.ApplyNetworkState(false, false, System.Numerics.Vector2.Zero);
            glidePlayer.velocity = Vector2.Zero; glidePlayer.controlUp = true;
            glide.QueueToggleRequest(true); glide.SetControls();
            Check(!glide.ApplyPendingToggle(true) && glidePlayer.controlUp, "Grounded W retains ordinary Up and does not switch modes");
            glidePlayer.velocity = new Vector2(12, 1); glide.QueueToggleRequest(false);
            Check(!glide.ApplyPendingToggle(false), "Queued W without held Jump cannot switch modes");
            glide.ResetEffects(); glidePlayer.velocity = Vector2.Zero;
            glidePlayer.itemAnimation = 0; glidePlayer.direction = 1;
            glidePlayer.velocity = new Vector2(22, 1);
            glide.ApplyNetworkState(true, false, new System.Numerics.Vector2(22, 1));
            glidePlayer.controlLeft = true; glidePlayer.controlRight = false;
            glide.UpdateFacingFromInput();
            Check(glidePlayer.direction == -1 && glidePlayer.velocity.X == 22,
                "Left changes high-speed glide facing without reversing momentum");
            glidePlayer.velocity = new Vector2(-22, 1);
            glidePlayer.controlLeft = false; glidePlayer.controlRight = true;
            glide.UpdateFacingFromInput();
            Check(glidePlayer.direction == 1 && glidePlayer.velocity.X == -22,
                "Right changes high-speed glide facing without reversing momentum");
            glidePlayer.controlLeft = true; glide.UpdateFacingFromInput();
            Check(glidePlayer.direction == 1, "Opposing directional inputs do not flicker facing");
            glidePlayer.controlRight = false; glidePlayer.itemAnimation = 2;
            glide.UpdateFacingFromInput();
            Check(glidePlayer.direction == 1, "Active weapon animation retains its aim-facing priority");
            glidePlayer.controlLeft = false; glidePlayer.itemAnimation = 0;
            glide.ApplyNetworkState(false, false, System.Numerics.Vector2.Zero);
            glidePlayer.velocity = Vector2.Zero;
            var crossbowItem = new Item(); crossbowItem.SetDefaults(ModContent.ItemType<RepeatingCrossbow>());
            var crossbow = (RepeatingCrossbow)crossbowItem.ModItem;
            Player bowPlayer = Main.player[0];
            Check(crossbowItem.damage == 12 && crossbowItem.knockBack == 2 && crossbowItem.crit == 4 &&
                crossbowItem.DamageType == DamageClass.Ranged && crossbowItem.shootSpeed == 12 && crossbowItem.autoReuse,
                "Repeating Crossbow has the requested ranged stats and held-click repeat");
            Check(crossbowItem.useTime == 10 && crossbowItem.useAnimation == 10 && crossbowItem.useAmmo == AmmoID.Arrow,
                "Crossbow uses normal arrows with a ten-tick interval");
            Check(!crossbow.CanUseItem(bowPlayer), "Crossbow cannot fire without arrow ammo");
            bowPlayer.inventory[54] = new Item(ItemID.WoodenArrow, 99);
            Check(crossbow.CanUseItem(bowPlayer), "Crossbow can fire with normal arrows");
            crossbow.RecordShot(bowPlayer);
            Check(crossbow.ShotsRemaining == 2 && crossbow.VisualFrame(bowPlayer) == 2, "First shot leaves two arrows and matching sprite");
            crossbow.RecordShot(bowPlayer);
            Check(crossbow.ShotsRemaining == 1 && crossbow.VisualFrame(bowPlayer) == 3, "Second shot leaves one arrow and matching sprite");
            crossbow.RecordShot(bowPlayer);
            Check(crossbow.ReloadRemaining == 60 && !crossbow.CanUseItem(bowPlayer) && crossbow.VisualFrame(bowPlayer) == 4,
                "Third shot starts automatic one-second reload and blocks firing");
            for (int i = 0; i < 15; i++) crossbow.UpdateInventory(bowPlayer);
            Check(crossbow.ShotsRemaining == 1 && crossbow.VisualFrame(bowPlayer) == 5, "Reload sprite and magazine refill are synchronized");
            for (int i = 0; i < 44; i++) crossbow.UpdateInventory(bowPlayer);
            Check(crossbow.ReloadRemaining == 1 && !crossbow.CanUseItem(bowPlayer), "Reload cannot finish before sixty ticks");
            crossbow.UpdateInventory(bowPlayer);
            Check(crossbow.ShotsRemaining == 3 && crossbow.ReloadRemaining == 0 && crossbow.CanUseItem(bowPlayer), "Reload restores three shots at tick sixty");
            crossbow.RecordShot(bowPlayer);
            var bowTag = new Terraria.ModLoader.IO.TagCompound(); crossbow.SaveData(bowTag);
            var restoredBowItem = new Item(); restoredBowItem.SetDefaults(crossbowItem.type);
            var restoredBow = (RepeatingCrossbow)restoredBowItem.ModItem; restoredBow.LoadData(bowTag);
            Check(restoredBow.ShotsRemaining == 2, "Crossbow partial magazine survives save/load");
            using (var buffer = new System.IO.MemoryStream())
            {
                using (var writer = new System.IO.BinaryWriter(buffer, System.Text.Encoding.UTF8, true)) crossbow.NetSend(writer);
                buffer.Position = 0;
                using var reader = new System.IO.BinaryReader(buffer);
                restoredBow.NetReceive(reader);
                Check(restoredBow.ShotsRemaining == 2, "Crossbow magazine survives network serialization");
            }
            for (int i = 0; i < 8; i++)
            {
                Rectangle sprite = RepeatingCrossbow.SpriteFrame(i);
                Check(sprite.Width > 400 && sprite.Right <= 1774 && sprite.Bottom <= 887, "Crossbow frame crop stays inside the supplied sheet");
            }
            bool bowRecipe = false;
            for (int i = 0; i < Recipe.numRecipes; i++)
                if (Main.recipe[i].createItem.type == crossbowItem.type)
                {
                    Recipe recipe = Main.recipe[i];
                    bowRecipe = HasIngredient(recipe, ItemID.Wood, 15) && HasIngredient(recipe, ItemID.Chain, 2) &&
                        HasIngredient(recipe, ItemID.IronBar, 8) && recipe.acceptedGroups.Contains(RecipeGroupID.IronBar) &&
                        recipe.requiredTile.Contains(TileID.Anvils);
                }
            Check(bowRecipe, "Crossbow recipe is fifteen wood, eight Iron/Lead bars and two chains at an Anvil");
            var gameplay = WeaponConfig.Current;
            bool savedRespawn = gameplay.InstantRespawn, savedInvincibility = gameplay.Invincibility;
            try
            {
                var settingsPlayer = Main.player[0].GetModPlayer<InstantRespawnPlayer>();
                gameplay.InstantRespawn = true;
                Main.player[0].respawnTimer = 180;
                settingsPlayer.UpdateDead();
                Check(Main.player[0].respawnTimer == 1, "Instant Respawn removes the death countdown");
                gameplay.InstantRespawn = false;
                Main.player[0].respawnTimer = 180;
                settingsPlayer.UpdateDead();
                Check(Main.player[0].respawnTimer == 180, "Disabled Instant Respawn preserves normal countdown");
                gameplay.Invincibility = true;
                Check(settingsPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByOther(1), 0, false), "Invincibility blocks player damage");
                gameplay.Invincibility = false;
                Check(!settingsPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByOther(1), 0, false), "Disabled Invincibility permits normal damage");
            }
            finally { gameplay.InstantRespawn = savedRespawn; gameplay.Invincibility = savedInvincibility; }
            var rifleItem = new Item(); rifleItem.SetDefaults(ModContent.ItemType<Piercer>());
            var musket = new Item(); musket.SetDefaults(ItemID.Musket);
            var rifle = (Piercer)rifleItem.ModItem;
            var rifleClass = DamageClass.Ranged;
            Check(rifleItem.damage == (int)MathF.Round(musket.damage * 1.5f) && rifleItem.DamageType == rifleClass &&
                rifleItem.CountsAsClass(DamageClass.Ranged), "Piercer base damage is halved and inherits ranged effects");
            Main.player[0].inventory[0] = rifleItem; Main.player[0].selectedItem = 0;
            Check(rifle.CanUseItem(Main.player[0]), "New Piercer is ready to fire");
            rifle.RecordShot(Main.player[0]);
            Check(!rifle.CanUseItem(Main.player[0]), "Firing locks the rifle until its bolt is cycled");
            Main.player[0].altFunctionUse = 2;
            Check(rifle.CanUseItem(Main.player[0]) && !rifle.NeedsAmmo(Main.player[0]) &&
                !rifle.CanConsumeAmmo(new Item(), Main.player[0]), "Right-click bolt does not require or consume ammo");
            var tag = new Terraria.ModLoader.IO.TagCompound(); rifle.SaveData(tag);
            var savedRifleItem = new Item(); savedRifleItem.SetDefaults(rifleItem.type);
            var savedRifle = (Piercer)savedRifleItem.ModItem; savedRifle.LoadData(tag);
            Check(savedRifle.NeedsBolt, "Uncycled bolt state persists when saved");
            rifle.CompleteBolt(Main.player[0]); Main.player[0].altFunctionUse = 0;
            Check(rifle.CanUseItem(Main.player[0]), "Bolt completion unlocks firing again");
            Main.player[0].GetDamage(DamageClass.Ranged) += 0.25f;
            Check(MathF.Abs(Main.player[0].GetTotalDamage(rifleClass).ApplyTo(100) - 125) < 0.001f,
                "Piercer uses vanilla ranged damage bonuses");
            Main.player[0].GetDamage(DamageClass.Ranged) = StatModifier.Default;
            Projectile piercingBullet = Shot(ModContent.ProjectileType<PiercerBulletProjectile>(), Vector2.UnitX * 18);
            Check(piercingBullet.penetrate == 3 && piercingBullet.localNPCHitCooldown == -1 &&
                piercingBullet.DamageType == rifleClass, "Piercer bullet pierces three distinct enemies");
            Check(global::TornMod.Common.BoltAction.BoltMechanics.ReloadTicks == 120 &&
                global::TornMod.Common.BoltAction.BoltMechanics.CloseStart - global::TornMod.Common.BoltAction.BoltMechanics.OpenTicks == 84,
                "Bolt cycle is two seconds with a long 84-tick open phase");
            int reloadType = ModContent.ProjectileType<PiercerReloadProjectile>();
            Main.player[0].ownedProjectileCounts[reloadType] = 1;
            Main.player[0].maxRunSpeed = 5f; Main.player[0].accRunSpeed = 5f; Main.player[0].runAcceleration = 1f;
            Main.player[0].jumpSpeedBoost = 0;
            var mobility = Main.player[0].GetModPlayer<global::TornMod.Common.BoltAction.BoltActionPlayer>();
            mobility.PostUpdateRunSpeeds(); mobility.PostUpdateEquips();
            Check(MathF.Abs(Main.player[0].maxRunSpeed - 7.5f) < 0.001f &&
                MathF.Abs(Main.player[0].jumpSpeedBoost - Terraria.Player.jumpSpeed * 0.2f) < 0.001f,
                "Reload gives fifty percent movement and twenty percent jump power");
            Main.player[0].ownedProjectileCounts[reloadType] = 0;
            Projectile hotCasing = Shot(ModContent.ProjectileType<HotCasingProjectile>(), new Vector2(2f, 5f));
            hotCasing.velocity.Y = 0;
            Check(!hotCasing.ModProjectile.OnTileCollide(new Vector2(2f, 5f)) && hotCasing.timeLeft == 300 && hotCasing.ai[0] == 1,
                "Hot casing survives ground collision for exactly five seconds");
            Check(hotCasing.ModProjectile.CanDamage() == false, "Hot casing burns through a debuff rather than direct projectile damage");
            hotCasing.ModProjectile.AI();
            Check(!Main.player[0].HasBuff(BuffID.OnFire), "Grounded casing never burns the player");
            Check(piercingBullet.extraUpdates == 5, "Piercer bullet uses six short movement steps per tick for high speed");
            piercingBullet.Center = new Vector2(22 + global::TornMod.Common.Lancer.LancerTuning.ImpactForwardBoost, 0); piercingBullet.velocity = new Vector2(22 + global::TornMod.Common.Lancer.LancerTuning.ImpactForwardBoost, 0);
            Check(piercingBullet.ModProjectile.Colliding(piercingBullet.Hitbox, new Rectangle(8, -4, 4, 8)) == true,
                "Fast bullet swept collision catches enemies between movement samples");
            rifle.RecordShot(Main.player[0]);
            Projectile reload = Shot(reloadType, Vector2.Zero);
            reload.ai[0] = 119; reload.ai[1] = 0;
            reload.ModProjectile.AI();
            Check(!rifle.NeedsBolt, "Final reload tick actually unlocks the same rifle instance");
            rifle.RecordShot(Main.player[0]);
            Projectile cancelledReload = Shot(reloadType, Vector2.Zero);
            cancelledReload.ai[1] = 1;
            cancelledReload.ModProjectile.AI();
            Check(rifle.NeedsBolt, "Switching item slots cancels reload without unlocking the rifle");
            Main.npc[0] = Enemy(new Vector2(100f, 0f));
            Main.npc[1] = Enemy(new Vector2(50f, 0f));
            Check(Homing.FindClosest(Vector2.Zero, 400f) == 1, "Nearest valid NPC");
            Main.npc[1].friendly = true;
            Check(Homing.FindClosest(Vector2.Zero, 400f) == 0, "Friendly NPC excluded");
            Main.npc[1].active = false;
            Main.npc[0].Center = new Vector2(400f, 0f);
            Check(Homing.FindClosest(Vector2.Zero, 400f) == 0, "400px boundary");
            Main.npc[0].Center = new Vector2(401f, 0f);
            Check(Homing.FindClosest(Vector2.Zero, 400f) == -1, "Outside range");
            Main.npc[0].Center = new Vector2(0f, 200f);

            Projectile mom = Shot(ModContent.ProjectileType<MomsSlipperProjectile>(), new Vector2(11f, 0f));
            Check(mom.timeLeft == 100 && mom.tileCollide && mom.penetrate == 1 && mom.ignoreWater, "Mom projectile defaults");
            for (int tick = 0; tick < 10; tick++)
            {
                mom.ModProjectile.AI();
                Check(mom.velocity == new Vector2(11f, 0f) && mom.ai[0] == 0f, "Straight flight");
            }
            mom.ModProjectile.AI();
            Check(mom.ai[0] == 1f, "Target acquired after ten ticks");
            Check(MathF.Abs(mom.velocity.Length() - 11f) < 0.0001f &&
                MathF.Abs(mom.velocity.ToRotation() - MathHelper.ToRadians(6f)) < 0.0001f, "Speed and 6 degree engine turn");
            Check(MathF.Abs(mom.rotation - mom.velocity.ToRotation() - MathHelper.PiOver2 - mom.ai[1] * 0.4f) < 0.0001f, "Visual spin uses age without changing trajectory");
            mom.ModProjectile.OnKill(0); // Dedicated servers must skip cosmetic effects safely.
            Main.npc[1] = Enemy(new Vector2(1f, 0f));
            mom.ModProjectile.AI();
            Check(mom.ai[0] == 1f, "Closer enemy cannot replace lock");
            Main.npc[0].active = false;
            Vector2 lastVelocity = mom.velocity;
            mom.ModProjectile.AI();
            Check(mom.ai[0] == HomingMath.Lost && mom.velocity == lastVelocity, "Death preserves straight trajectory");
            for (int tick = 0; tick < 20; tick++) mom.ModProjectile.AI();
            Check(mom.ai[0] == HomingMath.Lost && mom.velocity == lastVelocity, "No reacquisition after death");
            Check(mom.ModProjectile.OnTileCollide(mom.velocity), "Mom default tile hook requests destruction");

            Projectile spinner = Shot(ModContent.ProjectileType<SlipperonProjectile>(), new Vector2(20f, 0f));
            spinner.Center = new Vector2(300f, 0f);
            spinner.ModProjectile.OnHitNPC(Main.npc[1], default, 29);
            Check(spinner.ai[2] == 1f && spinner.ModProjectile.CanDamage() != false && spinner.friendly, "Hit starts damaging return");
            Check(spinner.penetrate == -1 && spinner.usesLocalNPCImmunity && spinner.localNPCHitCooldown == 10,
                "Unlimited hits with independent per-enemy cooldowns");
            spinner.ModProjectile.AI();
            Check(spinner.velocity.X < 0f && !spinner.tileCollide && spinner.friendly, "Damaging return heads toward owner through tiles");
            // Additional hits must neither consume penetration nor reset return behavior.
            Vector2 returnVelocity = spinner.velocity;
            for (int hit = 0; hit < 12; hit++)
            {
                spinner.ModProjectile.OnHitNPC(Main.npc[1], default, 29);
                Check(spinner.ai[2] == 1f && spinner.penetrate == -1 && spinner.friendly &&
                    spinner.ModProjectile.CanDamage() != false && spinner.velocity == returnVelocity,
                    "Repeated return hits preserve return state and unlimited penetration");
            }
            Projectile wallSpinner = Shot(ModContent.ProjectileType<SlipperonProjectile>(), new Vector2(20f, 0f));
            Check(!wallSpinner.ModProjectile.OnTileCollide(wallSpinner.velocity) && wallSpinner.ai[2] == 1f, "Wall triggers return rather than destruction");
            Projectile missedSpinner = Shot(ModContent.ProjectileType<SlipperonProjectile>(), new Vector2(20f, 0f));
            missedSpinner.Center = new Vector2(300f, 0f);
            missedSpinner.ai[1] = 30f;
            missedSpinner.ModProjectile.AI();
            Check(missedSpinner.ai[2] == 1f, "Missed shot returns after thirty ticks");
            Main.player[0].ownedProjectileCounts[spinnerItem.shoot] = 1;
            Check(!spinnerItem.ModItem.CanUseItem(Main.player[0]), "Second simultaneous Slipperon blocked");
            Main.player[0].ownedProjectileCounts[spinnerItem.shoot] = 0;
            Check(spinnerItem.ModItem.CanUseItem(Main.player[0]), "Slipperon usable when projectile gone");
            int childType = ModContent.ProjectileType<SlipperonChildProjectile>();
            Main.player[0].ownedProjectileCounts[childType] = 1;
            Check(!spinnerItem.ModItem.CanUseItem(Main.player[0]), "Last surviving split child blocks reuse");
            Main.player[0].ownedProjectileCounts[childType] = 0;
            Check(spinnerItem.ModItem.CanUseItem(Main.player[0]), "All split children gone unlocks reuse");
            Main.player[0].altFunctionUse = 2;
            Check(MathF.Abs(spinnerItem.ModItem.UseSpeedMultiplier(Main.player[0]) - 0.5f) < 0.001f, "Right click doubles use duration");
            Main.player[0].altFunctionUse = 0;
            Projectile child = Shot(childType, new Vector2(11f, 0f));
            Main.npc[1].Center = new Vector2(0f, 200f);
            child.ModProjectile.AI();
            Check(child.ai[0] == 2f && MathF.Abs(child.velocity.ToRotation() - MathHelper.ToRadians(10f)) < 0.001f,
                "Split child acquires immediately and turns ten degrees");

            var saber = new Item();
            saber.SetDefaults(ModContent.ItemType<global::TornMod.Content.Items.Saber>());
            var saberClass = DamageClass.Melee;
            Check(saber.damage == 24 && saber.DamageType == saberClass && saber.autoReuse, "Saber vanilla melee and held-click repeat defaults");
            Check(saber.CountsAsClass(DamageClass.Melee), "Saber inherits melee effects and prefixes");
            Player statsPlayer = Main.player[0];
            statsPlayer.GetDamage(DamageClass.Melee) += 0.5f;
            statsPlayer.GetAttackSpeed(DamageClass.Melee) += 0.25f;
            Check(MathF.Abs(statsPlayer.GetTotalDamage(saberClass).ApplyTo(24f) - 36f) < 0.001f &&
                MathF.Abs(statsPlayer.GetTotalAttackSpeed(saberClass) - 1.25f) < 0.001f,
                "Saber inherits melee damage and attack speed bonuses");
            Check(!SaberCombatPlayer.IsSaber(new Item(ItemID.CopperShortsword)),
                "Ordinary melee weapons never acquire Saber charge mechanics");
            statsPlayer.GetDamage(DamageClass.Melee) = StatModifier.Default;
            statsPlayer.GetDamage(saberClass) = StatModifier.Default;
            statsPlayer.GetAttackSpeed(DamageClass.Melee) = 1f;
            Check(WeaponBalance.SaberChargeTicks == 60, "Default charge is one second");
            Main.player[0].inventory[0] = saber;
            Main.player[0].selectedItem = 0;
            Check(saber.ModItem.AltFunctionUse(Main.player[0]), "Saber supports right-click charge");
            Main.player[0].altFunctionUse = 0;
            saber.ModItem.CanUseItem(Main.player[0]);
            Check(!saber.channel, "Left click attacks without channeling");
            Main.player[0].altFunctionUse = 2;
            Main.player[0].velocity.Y = -1f;
            Check(!saber.ModItem.CanUseItem(Main.player[0]) && saber.channel, "Saber right-click charge is blocked while jumping");
            Main.player[0].velocity.Y = 0f;
            Main.player[0].altFunctionUse = 0;
            Projectile slash = Shot(ModContent.ProjectileType<SaberSlashProjectile>(), Vector2.Zero);
            slash.ai[0] = 0f;
            slash.localAI[0] = WeaponBalance.SaberSlashTicks * 0.5f;
            Check(!slash.ModProjectile.ShouldUpdatePosition() && slash.ownerHitCheck && slash.DamageType == saberClass,
                "Slash is attached true melee with owner line-of-sight check");
            Check(slash.ModProjectile.Colliding(slash.Hitbox, new Rectangle(60, -5, 10, 10)) == true,
                "Narrow arc can strike an enemy ahead");
            Check(slash.ModProjectile.Colliding(slash.Hitbox, new Rectangle(300, 100, 10, 10)) == false,
                "Slash cannot hit distant enemies");
            slash.ai[1] = 1f;
            slash.ai[2] = 2f;
            Main.npc[1].whoAmI = 1;
            Main.npc[0].whoAmI = 0;
            Check(slash.ModProjectile.CanHitNPC(Main.npc[0]) == false && slash.ModProjectile.CanHitNPC(Main.npc[1]) != false,
                "Charged contact slash restricts damage to first enemy");
            var normalModifiers = Main.npc[1].GetIncomingStrikeModifiers(DamageClass.Melee, 1);
            var chargedModifiers = Main.npc[1].GetIncomingStrikeModifiers(DamageClass.Melee, 1);
            slash.ModProjectile.ModifyHitNPC(Main.npc[1], ref chargedModifiers);
            NPC.HitInfo normalCrit = normalModifiers.ToHitInfo(24f, true, 4f, false);
            NPC.HitInfo chargedHit = chargedModifiers.ToHitInfo(24f * SaberMechanics.DashDamageScale(1f, 2f), false, 4f, false);
            Check(chargedHit.Crit && chargedHit.Damage == normalCrit.Damage,
                "Full charge deals a standard guaranteed critical after halving old dash damage");
            var combat = Main.player[0].GetModPlayer<SaberCombatPlayer>();
            combat.ProtectDashContact(Main.npc[1]);
            Check(combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(1), 0, false),
                "Dash contact target cannot hurt player during impact slash");
            Check(!combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(0), 0, false),
                "Dash contact protection does not block other enemies");
            combat.ProtectDashContact(Main.npc[0]);
            Check(combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(0), 0, false) &&
                combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(1), 0, false),
                "Passage contact protection covers multiple crossed enemies");
            Check(combat.TryRecordDashHit(Main.npc[0]) && !combat.TryRecordDashHit(Main.npc[0]) &&
                combat.TryRecordDashHit(Main.npc[1]), "Passage hits register once per enemy while allowing multiple enemies");
            var targetDummy = new NPC { active = true, type = NPCID.TargetDummy, immortal = true };
            Check(SaberCombatPlayer.IsDashTarget(targetDummy), "Dash can damage immortal target dummies for DPS testing");
            Projectile trail = Shot(ModContent.ProjectileType<SaberDashTrailProjectile>(), Vector2.Zero);
            Check(trail.timeLeft == 6 && trail.ModProjectile.CanDamage() == false && !trail.ModProjectile.ShouldUpdatePosition(),
                "Sharp dash trail expires in six ticks and cannot damage enemies");
            slash.ai[1] = 2f;
            Check(slash.ModProjectile.CanDamage() == false, "Dash-end visual slash cannot hit passage targets twice");
            slash.ai[1] = 1f;
            for (int tick = 0; tick < WeaponBalance.SaberSlashTicks + WeaponBalance.SaberHitStopTicks + 2; tick++)
                combat.PostUpdate();
            Check(!combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(1), 0, false),
                "Dash contact protection expires after impact animation");
            foreach (float angle in new[] { 0f, MathHelper.Pi })
            {
                Vector2 nativeTip = new(1f, -1f);
                Vector2 nativeCurve = new(1f, 1f);
                if (SaberDrawing.FacesLeft(angle)) { nativeTip.X *= -1; nativeCurve.X *= -1; }
                Vector2 tip = nativeTip.RotatedBy(SaberDrawing.Rotation(angle));
                Vector2 curve = nativeCurve.RotatedBy(SaberDrawing.Rotation(angle));
                Check(Vector2.Dot(tip, angle.ToRotationVector2()) > 1.4f && MathF.Abs(tip.Y) < 0.001f,
                    "Mirrored blade points along aim on both sides");
                Check(curve.Y > 1.4f, "Blade curve stays below on both sides");
            }
            var overPlayers = new System.Collections.Generic.List<int>();
            var unusedDrawList = new System.Collections.Generic.List<int>();
            slash.ModProjectile.DrawBehind(123, unusedDrawList, unusedDrawList, unusedDrawList, overPlayers, unusedDrawList);
            Check(slash.hide && overPlayers.Contains(123), "Slash draws in front of the player");
            combat.RegisterChargedHit();
            Check(combat.FallProtection.Armed && combat.HitStopTicks == WeaponBalance.SaberHitStopTicks, "Charged hit grants fall token and fixed hit stop");
            Check(!combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByOther(1), 0, false) && combat.FallProtection.Armed,
                "Other damage does not consume fall protection");
            Check(combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByOther(0), 0, false) && !combat.FallProtection.Armed,
                "Fall damage consumes one protection token");
            Check(!combat.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByOther(0), 0, false), "Second fall is not protected");
            Check(!WeaponConfig.Current.WarpToCursor, "Warp defaults to disabled in a new test profile");
            Check(WarpToCursorPlayer.InWorld(new Vector2(32, 32), 20, 42, 100, 100) &&
                !WarpToCursorPlayer.InWorld(new Vector2(float.NaN, 32), 20, 42, 100, 100) &&
                !WarpToCursorPlayer.InWorld(new Vector2(-1, 32), 20, 42, 100, 100) &&
                !WarpToCursorPlayer.InWorld(new Vector2(1590, 32), 20, 42, 100, 100),
                "Warp rejects invalid coordinates and the world border");
            var testSaber = saber;
            Check(SaberCombatPlayer.IsSaber(testSaber) && !SaberCombatPlayer.IsSaber(new Item(ItemID.Musket)),
                "Only Saber receives its charge and dash controller");
            Check(new SaberWeaponRules().AltFunctionUse(testSaber, Main.player[0]), "Saber retains right-click controls");
            Main.player[0].ownedProjectileCounts[ModContent.ProjectileType<SaberChargeProjectile>()] = 1;
            Check(new SaberWeaponRules().CanHitNPC(testSaber, Main.player[0], Main.npc[0]) == false,
                "Saber charge suppresses accidental vanilla melee contact from its held item");
            Main.player[0].ownedProjectileCounts[ModContent.ProjectileType<SaberChargeProjectile>()] = 0;
            Main.player[0].inventory[0] = testSaber;
            Main.player[0].velocity = new Vector2(4, -1);
            Main.player[0].sloping = true; Main.player[0].jump = 0;
            Check(SaberCombatPlayer.IsGrounded(Main.player[0]), "Vanilla slope ascent is valid ground despite upward velocity");
            Main.player[0].sloping = false;
            combat.UpdateGroundGrace(true);
            for (int tick = 0; tick < WeaponBalance.SaberCoyoteTicks; tick++)
            {
                Check(combat.CanChargeDash(), "Coyote grace permits charge just after leaving support");
                combat.UpdateGroundGrace(false);
            }
            Check(!combat.CanChargeDash(), "Coyote grace expires instead of enabling indefinite airborne charges");
            Projectile[] savedLaunchProjectiles = (Projectile[])Main.projectile.Clone();
            try
            {
                for (int i = 0; i < Main.maxProjectiles; i++) Main.projectile[i] = new Projectile { whoAmI = i };
                Main.player[0].sloping = true; Main.player[0].controlUseTile = true;
                Main.player[0].ownedProjectileCounts[ModContent.ProjectileType<SaberChargeProjectile>()] = 0;
                Main.player[0].ownedProjectileCounts[ModContent.ProjectileType<SaberSlashProjectile>()] = 0;
                combat.ResetForTeleport(); combat.UpdateGroundGrace(true);
                Check(combat.BeginCharge(), "Shared subclass item starts a charge");
                Projectile chargeShot = Main.projectile.Where(p => p.active).Single(p => p.ModProjectile is SaberChargeProjectile);
                chargeShot.ai[1] = WeaponBalance.SaberChargeTicks - 1;
                chargeShot.ModProjectile.AI();
                Check(combat.Dashing && chargeShot.ai[2] == 1, "Held right click automatically releases at full charge");
                Vector2 launchVelocity = Main.player[0].velocity;
                combat.FinishDash();
                Check(Main.player[0].velocity == launchVelocity && combat.DashCooldown > 0, "Dash finish preserves launch velocity and starts cooldown");
                combat.PostUpdate();
                Check(Main.player[0].velocity == launchVelocity, "Dash-end slash preserves momentum too");
                chargeShot.ModProjectile.AI();
                for (int tick = 0; tick < 40 && !Main.projectile.Any(p => p.active && p.ModProjectile is SaberChargeProjectile); tick++)
                {
                    foreach (Projectile activeShot in Main.ActiveProjectiles)
                        if (activeShot.ModProjectile is SaberSlashProjectile) activeShot.ModProjectile.AI();
                    Main.player[0].ownedProjectileCounts[ModContent.ProjectileType<SaberChargeProjectile>()] =
                        Main.projectile.Count(p => p.active && p.ModProjectile is SaberChargeProjectile);
                    Main.player[0].ownedProjectileCounts[ModContent.ProjectileType<SaberSlashProjectile>()] =
                        Main.projectile.Count(p => p.active && p.ModProjectile is SaberSlashProjectile);
                    combat.PreUpdateMovement();
                }
                Check(combat.DashCooldown == 0 && Main.projectile.Any(p => p.active && p.ModProjectile is SaberChargeProjectile),
                    "Holding right starts the next subclass charge on cooldown completion");
            }
            finally
            {
                Array.Copy(savedLaunchProjectiles, Main.projectile, savedLaunchProjectiles.Length);
                Main.player[0].sloping = false; Main.player[0].controlUseTile = false;
                combat.ResetForTeleport();
            }
            var iron = new Item(); iron.SetDefaults(ModContent.ItemType<IronLance>());
            var lancerClass = DamageClass.Melee;
            Check(iron.damage == 10 && iron.knockBack == 1.5f && iron.DamageType == lancerClass &&
                iron.CountsAsClass(DamageClass.Melee) && !SaberCombatPlayer.IsSaber(iron), "Iron Lance uses vanilla melee without Saber controls");
            Check(iron.ModItem.Tooltip.Value.Contains("Quick Thrust") && iron.ModItem.Tooltip.Value.Contains("10 ticks"), "Iron Lance English tooltip resolves");
            Check(Main.recipe.Take(Recipe.numRecipes).Any(r => r.createItem.type == iron.type &&
                HasIngredient(r, ItemID.Wood, 5) && r.requiredItem.Any(i => i.stack == 10 && i.type == ItemID.IronBar) &&
                r.requiredTile.Contains(TileID.Anvils)), "Iron Lance recipe uses 10 Iron/Lead bars and 5 Wood at an Anvil");
            Main.player[0].GetDamage(DamageClass.Melee) += 0.25f;
            Main.player[0].GetAttackSpeed(DamageClass.Melee) = 1.2f;
            Check(MathF.Abs(Main.player[0].GetTotalDamage(lancerClass).ApplyTo(100) - 125) < 0.001f &&
                MathF.Abs(Main.player[0].GetTotalAttackSpeed(lancerClass) - 1.2f) < 0.001f, "Lancer inherits melee damage and attack speed stats");
            Main.player[0].GetDamage(DamageClass.Melee) = StatModifier.Default;
            Main.player[0].GetAttackSpeed(DamageClass.Melee) = 1;
            Check(iron.DamageType == saber.DamageType && iron.DamageType == DamageClass.Melee,
                "Iron Lance and Saber share the actual vanilla melee class");
            Main.player[0].inventory[0] = iron;
            Main.player[0].controlUseItem = true; Main.player[0].controlUseTile = false;
            Main.player[0].velocity = new Vector2(22, 0);
            var heldLance = Shot(ModContent.ProjectileType<HeldLanceProjectile>(), Vector2.Zero);
            heldLance.ModProjectile.OnSpawn(Main.player[0].GetSource_ItemUse(iron));
            Check(heldLance.usesLocalNPCImmunity && heldLance.localNPCHitCooldown == 10 && heldLance.penetrate == -1 &&
                !heldLance.ModProjectile.ShouldUpdatePosition() && heldLance.ownerHitCheck, "Lance uses independent local immunity and an attached line-of-sight hitbox");
            for (int tick = 0; tick < 14; tick++) heldLance.ModProjectile.AI();
            Check(heldLance.ModProjectile.CanDamage() == false, "Actual held projectile is harmless during 15-tick preparation");
            heldLance.ModProjectile.AI();
            Check(heldLance.ai[0] == 1 && heldLance.friendly, "Actual held lance becomes ready at tick 15");
            Check(Main.player[0].velocity == new Vector2(22, 0), "Preparing/tracking does not write movement velocity");
            var lanceFields = typeof(HeldLanceProjectile);
            var privateFields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            Vector2 renderedTail = (Vector2)lanceFields.GetField("tail", privateFields).GetValue(heldLance.ModProjectile);
            Vector2 renderedTip = (Vector2)lanceFields.GetField("tip", privateFields).GetValue(heldLance.ModProjectile);
            Vector2 renderedGrip = (Vector2)lanceFields.GetField("grip", privateFields).GetValue(heldLance.ModProjectile);
            Check(MathF.Abs(Vector2.Distance(renderedTail, renderedTip) - 240) < 0.001f &&
                Vector2.Distance((renderedTail + renderedTip) / 2, renderedGrip) < 0.001f,
                "Lance renders at 240 pixels with its grip exactly at the midpoint");
            Vector2 renderedAxis = (renderedTip - renderedTail).SafeNormalize(Vector2.UnitX);
            Vector2 tipTarget = renderedTip - renderedAxis * 20;
            Vector2 shaftTarget = renderedTail + renderedAxis * 20;
            Check(heldLance.ModProjectile.Colliding(default, new Rectangle((int)tipTarget.X - 2, (int)tipTarget.Y - 2, 4, 4)) == true &&
                heldLance.ModProjectile.Colliding(default, new Rectangle((int)shaftTarget.X - 2, (int)shaftTarget.Y - 2, 4, 4)) == false,
                "Actual projectile collision agrees with the rendered tip and excludes the shaft");
            var lancerPlayer = Main.player[0].GetModPlayer<global::TornMod.Common.Lancer.LancerPlayer>();
            Check(!lancerPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(0), 0, false),
                "No speculative protection before a successful tip impact");
            var hitModifiers = new NPC.HitModifiers { SourceDamage = StatModifier.Default };
            Main.player[0].velocity = new Vector2(global::TornMod.Common.Lancer.LancerTuning.ProtectionSpeed, 0);
            Main.npc[1].velocity = Vector2.Zero; heldLance.ai[2] = 0;
            heldLance.ModProjectile.ModifyHitNPC(Main.npc[1], ref hitModifiers);
            heldLance.ModProjectile.OnHitNPC(Main.npc[1], default, 80);
            Check(!lancerPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(1), 0, false) &&
                Main.player[0].velocity.X == global::TornMod.Common.Lancer.LancerTuning.ProtectionSpeed,
                "Exactly 45 mph cannot activate protection or boost; threshold is strictly above");
            Main.player[0].velocity = new Vector2(22, 0);
            hitModifiers = new NPC.HitModifiers { SourceDamage = StatModifier.Default };
            Main.npc[0].velocity = Vector2.Zero; heldLance.ai[2] = 0;
            heldLance.ModProjectile.ModifyHitNPC(Main.npc[0], ref hitModifiers);
            Check(hitModifiers.SourceDamage.ApplyTo(10) == 200 && !hitModifiers.ToHitInfo(10, false, 0).Crit,
                "Momentum grants capped twentyfold source damage without forced crit");
            var lanceGlider = Main.player[0].GetModPlayer<global::TornMod.Common.Glider.GliderPlayer>();
            lanceGlider.ApplyNetworkState(true, false, new System.Numerics.Vector2(22, 0));
            heldLance.ModProjectile.OnHitNPC(Main.npc[0], default, 25);
            Check(Main.player[0].velocity == new Vector2(22 + global::TornMod.Common.Lancer.LancerTuning.ImpactForwardBoost, 0), "Successful impact adds a fixed 50 mph forward impulse");
            var cachedFlight = (global::TornMod.Common.Glider.GliderController)typeof(global::TornMod.Common.Glider.GliderPlayer)
                .GetField("controller", privateFields).GetValue(lanceGlider);
            Check(cachedFlight.Velocity == new System.Numerics.Vector2(22 + global::TornMod.Common.Lancer.LancerTuning.ImpactForwardBoost, 0), "Glider cache retains the impact impulse for its next movement step");
            lanceGlider.ApplyNetworkState(false, false, System.Numerics.Vector2.Zero);
            var protectionDeadlines = (ulong[])typeof(global::TornMod.Common.Lancer.LancerPlayer)
                .GetField("protectionUntil", privateFields).GetValue(lancerPlayer);
            ulong earnedDeadline = protectionDeadlines[0];
            Check(earnedDeadline == Main.GameUpdateCount + 60, "Successful impact earns exactly sixty ticks of protection");
            Check(lancerPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(0), 0, false) &&
                !lancerPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(1), 0, false) &&
                !lancerPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByOther(1), 0, false),
                "Earned protection is restricted to the struck NPC");
            Check(heldLance.ModProjectile.CanHitNPC(Main.npc[0]) == false && heldLance.ModProjectile.CanHitNPC(new NPC { whoAmI = 2 }) != false,
                "Contact cooldown is independent per NPC");
            hitModifiers = new NPC.HitModifiers { SourceDamage = StatModifier.Default };
            heldLance.ModProjectile.ModifyHitNPC(Main.npc[0], ref hitModifiers);
            heldLance.ModProjectile.OnHitNPC(Main.npc[0], default, 10);
            Check(hitModifiers.SourceDamage.ApplyTo(10) == 10 && Main.player[0].velocity == new Vector2(22 + global::TornMod.Common.Lancer.LancerTuning.ImpactForwardBoost, 0),
                "Continuous contact does not repeat momentum damage or forward impulse");
            Check(protectionDeadlines[0] == earnedDeadline, "Ordinary contact cannot renew earned protection");
            protectionDeadlines[0] = Main.GameUpdateCount;
            Check(!lancerPlayer.ImmuneTo(Terraria.DataStructures.PlayerDeathReason.ByNPC(0), 0, false),
                "Protection expires at its deadline even when the NPC remains in contact");
            protectionDeadlines[0] = earnedDeadline;
            Vector2 savedImpactPosition = Main.npc[0].position;
            try
            {
                Main.npc[0].position = new Vector2(10000, 10000);
                for (int tick = 0; tick < global::TornMod.Common.Lancer.LancerTuning.ImpactSeparationTicks; tick++)
                    heldLance.ModProjectile.AI();
                Main.npc[0].position = savedImpactPosition; heldLance.ai[2] = 0;
                hitModifiers = new NPC.HitModifiers { SourceDamage = StatModifier.Default };
                heldLance.ModProjectile.ModifyHitNPC(Main.npc[0], ref hitModifiers);
                Check(hitModifiers.SourceDamage.ApplyTo(10) == 200, "A separated pass rearms the same NPC");
            }
            finally { Main.npc[0].position = savedImpactPosition; }
            ((HeldLanceProjectile)heldLance.ModProjectile).BeginQuick(0, 30, 8);
            hitModifiers = new NPC.HitModifiers { SourceDamage = StatModifier.Default };
            heldLance.ModProjectile.ModifyHitNPC(Main.npc[1], ref hitModifiers);
            Check(hitModifiers.SourceDamage.ApplyTo(10) == 10, "Quick Thrust does not use momentum");
            Vector2 savedThrustCenter = Main.npc[1].Center;
            Main.npc[1].Center = Main.player[0].Center - Vector2.UnitX * 30;
            heldLance.ModProjectile.ModifyHitNPC(Main.npc[1], ref hitModifiers);
            Check(hitModifiers.ToHitInfo(10, false, 8).HitDirection == -1, "Quick pushes left targets away");
            Main.npc[1].Center = Main.player[0].Center + Vector2.UnitX * 30;
            heldLance.ModProjectile.ModifyHitNPC(Main.npc[1], ref hitModifiers);
            Check(hitModifiers.ToHitInfo(10, false, 8).HitDirection == 1, "Quick pushes right targets away");
            heldLance.ModProjectile.OnHitNPC(Main.npc[1], default, 30);
            for (int tick = 0; tick < 10; tick++) heldLance.ModProjectile.AI();
            Check(heldLance.ModProjectile.CanHitNPC(Main.npc[1]) == false,
                "Quick cannot hit an NPC twice even after contact cooldown expires");
            Main.npc[1].Center = savedThrustCenter;
            Projectile[] savedLanceProjectiles = (Projectile[])Main.projectile.Clone();
            try
            {
                for (int i = 0; i < Main.maxProjectiles; i++) Main.projectile[i] = new Projectile { whoAmI = i };
                var lanceWeapon = (global::TornMod.Common.Lancer.LancerWeapon)iron.ModItem;
                Check(lancerPlayer.Begin(lanceWeapon, false, 0), "Shared Lancer player starts preparation");
                Projectile normalLance = Main.projectile.Single(p => p.active && p.ModProjectile is HeldLanceProjectile);
                for (int tick = 0; tick < 15; tick++) normalLance.ModProjectile.AI();
                ModProjectile normalController = normalLance.ModProjectile;
                normalLance.ModProjectile.OnHitNPC(Main.npc[0], default, 10);
                Check(lancerPlayer.Begin(lanceWeapon, true, 0) && normalLance.active && normalLance.ModProjectile == normalController,
                    "Quick suspends readiness in the same projectile, preserving cooldown tables");
                Projectile quickLance = normalLance;
                Check(quickLance.damage == 30 && quickLance.knockBack == 8 && quickLance.localNPCHitCooldown == 10 && lancerPlayer.QuickCooldown == 30,
                    "Quick keeps stats and shared contact immunity with a thirty-tick use cooldown");
                Check(quickLance.ModProjectile.CanHitNPC(Main.npc[0]) == false, "Quick cannot bypass a preceding left contact cooldown");
                Check(!lancerPlayer.Begin(lanceWeapon, true, 0), "Quick spam cannot bypass its use cooldown");
                using var stream = new System.IO.MemoryStream();
                using (var writer = new System.IO.BinaryWriter(stream, System.Text.Encoding.UTF8, true)) quickLance.ModProjectile.SendExtraAI(writer);
                stream.Position = 0;
                var receiver = Shot(quickLance.type, Vector2.Zero); receiver.ai[0] = 3;
                using (var reader = new System.IO.BinaryReader(stream, System.Text.Encoding.UTF8, true)) receiver.ModProjectile.ReceiveExtraAI(reader);
                Check(receiver.localNPCHitCooldown == 10, "Shared immunity and suspended state deserialize");
                for (int tick = 0; tick < 12; tick++) quickLance.ModProjectile.AI();
                Check(quickLance.active && quickLance.ai[0] == 1 && quickLance.damage == 10 && quickLance.knockBack == 1.5f,
                    "Held left resumes readiness immediately with contact damage and knockback restored");
                for (int tick = 0; tick < 12; tick++) receiver.ModProjectile.AI();
                Check(receiver.ai[0] == 1, "Deserialized Quick resumes the saved ready phase");
                lancerPlayer.CancelLance();
                Check(!quickLance.active, "Item switch/warp cancellation still removes the lance");
            }
            finally
            {
                lancerPlayer.UpdateDead();
                Array.Copy(savedLanceProjectiles, Main.projectile, savedLanceProjectiles.Length);
                Main.player[0].controlUseItem = false; Main.player[0].velocity = Vector2.Zero;
            }
            Projectile splitter = Shot(ModContent.ProjectileType<SlipperonSplitProjectile>(), new Vector2(20f, 0f));
            splitter.ModProjectile.OnSpawn(new Terraria.DataStructures.EntitySource_Misc("SlipperChecks"));
            Check(splitter.ai[1] == 300f && splitter.ModProjectile.CanDamage() == false, "Right-click splits at half max distance and does no parent damage");
            splitter.ai[0] = 295f;
            splitter.ModProjectile.AI();
            Check(splitter.velocity.Length() == 5f && splitter.ai[0] == 300f, "Last flight step stops exactly at split distance");
            Projectile[] previousProjectiles = (Projectile[])Main.projectile.Clone();
            try
            {
                int before = 0;
                foreach (Projectile shot in Main.projectile) if (shot.active && shot.type == childType) before++;
                splitter.ModProjectile.AI();
                int after = 0;
                foreach (Projectile shot in Main.projectile) if (shot.active && shot.type == childType) after++;
                Check(after - before == 3 && !splitter.active, "Split creates exactly three children and removes parent");
            }
            finally { Array.Copy(previousProjectiles, Main.projectile, previousProjectiles.Length); }
        }
        finally
        {
            Main.npc[0] = saved0;
            Main.npc[1] = saved1;
            Main.player[0] = savedPlayer;
            Main.myPlayer = savedMyPlayer;
        }
        Mod.Logger.Info($"SLIPCHECKS PASS: {checks} engine assertions. No world simulation or multiplayer session was tested.");
        Console.WriteLine($"SLIPCHECKS PASS: {checks} engine assertions. No world simulation or multiplayer session was tested.");
    }

    private static bool HasIngredient(Recipe recipe, int type, int stack)
    {
        foreach (Item ingredient in recipe.requiredItem)
            if (ingredient.type == type && ingredient.stack == stack) return true;
        return false;
    }

    private static NPC Enemy(Vector2 center)
    {
        var npc = new NPC();
        npc.SetDefaults(NPCID.BlueSlime);
        npc.active = true;
        npc.Center = center;
        return npc;
    }

    private static Projectile Shot(int type, Vector2 velocity)
    {
        var projectile = new Projectile();
        projectile.SetDefaults(type);
        projectile.owner = 0;
        projectile.Center = Vector2.Zero;
        projectile.velocity = velocity;
        return projectile;
    }
}



