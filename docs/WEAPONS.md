# Current weapons and subclasses

Updated: 2026-10-14. Code is authoritative; all values below are prototype balance.

## Mom's Slipper and Slipperon

Mom's Slipper is consumable ranged: 12 damage, 20-tick use, speed 11, knockback 2.5, lifetime 100 ticks. It spins visually, flies straight for 10 ticks, then seeks a suitable enemy within 400 pixels and turns up to 6°/tick. Once locked, it never switches/reacquires a lost target. No initial target means periodic searches continue. Terrain collision ends flight; destruction adds wooden particles and a vanilla impact sound, without area damage.

Slipperon is reusable ranged: 29 damage, 38-tick use, speed 20, knockback 9. A hit, wall, 30 outbound ticks or 600-pixel distance triggers return at speed 24. Return passes through walls and keeps damaging enemies; penetration is unlimited with a 10-tick per-NPC hit interval.

Right click takes twice the normal use time. A harmless carrier travels up to 300 pixels, then splits into three homing slippers: 12 base damage, speed 11, 100-tick lifetime, 600-pixel search and 10°/tick turning. Another Slipperon cannot be used until the returning parent or all children disappear.

## Saber

Saber is attached true melee with fast narrow slashes and a passage dash. Effective base damage is 12 (the item's 24 is scaled by WeaponBalance); blade length 65, normal use 12 ticks and slash animation 6 ticks. Melee/Saber attack speed affects use and animation.

Hold left click to repeat cursor-directed slashes. Hold right click on support to charge: full charge automatically releases after 60 ticks, while early release produces a partial dash. Vanilla slope walking counts as support; a six-pixel probe and eight-tick coyote window tolerate brief support loss. Persistent airborne charging is not permitted.

Charge selects a 56–336-pixel controlled dash, lasting up to 12 ticks with 0.85 speed retention. Swept tile checks use ≤4-pixel segments and slide on open axes. Each crossed enemy is hit once with guaranteed crit. Current pre-crit damage scaling is `base × 0.5 × (1 + 3 × chargeFraction)`; the configured full multiplier is 4, so do not describe full charge as merely one standard crit of unmodified base damage.

Dash completion and its visual-only slash retain collision-resolved velocity. Successful passage hits grant one-use fall protection and temporary protection against the contacted enemies; passage does not pause for hit stop. Holding right starts another charge when the cooldown (item useTime / attack speed), ending slash and support/coyote conditions permit.

All Saber-class items use the shared right-click rules, subclass validation and held-item contact suppression. The ready sword is belt-anchored; supplied art, mirrored curvature, Terragrim animation and short trails are preserved.

## Piercer

Bolt Action ranged subclass. Base damage is rounded Musket damage ×1.5. Selected bullet ammo contributes damage/consumption normally; its projectile effects are replaced by a fast, thin custom bullet piercing three distinct enemies. Six movement updates per tick and swept collision reduce missed targets.

After a shot, right click cycles the bolt without ammo: 120 ticks total, 18 opening / 84 open / 18 closing. Reload gives 50% movement/acceleration and 20% jump-launch boosts. Ejected casings fall, remain grounded for 300 ticks and refresh On Fire on overlapping NPCs, never players. Switching slots/death cancels the cycle without rearming. Bolt state persists per item.

The 82-pixel gun appears only when firing/reloading and anchors its stock at the shoulder. Supplied eight-cell/casing art is cropped at draw time.

## Repeating Crossbow

Ranged: 12 damage, 2 knockback, 4% crit, speed 12. Three shots at fixed 10-tick intervals, then fixed 60-tick automatic reload. Each shot uses normal selected arrow ammo/projectile, including vanilla conservation. Magazine slots are not pre-consumed inventory arrows.

Reload advances while in inventory; its visible fill steps are at 15/30/45 ticks, but firing waits for tick 60. Each shot captures a fresh aim; the weapon appears during that shot and retains its direction throughout the animation. Per-item magazine/reload/aim are saved and network-serialized.

## Actual recipes

| Item | Current recipe |
| --- | --- |
| 25 Mom's Slippers | Loom: 1 Silk + 2 Wood |
| Slipperon | Anvil: 1000 Mom's Slippers + 5 Fallen Stars + 10 Gold/Platinum Bars |
| Saber | Anvil: 10 Iron/Lead Bars + 8 Wood |
| Piercer | Anvil: Musket + 10 Iron/Lead Bars + 10 Wood |
| Repeating Crossbow | Anvil: 15 Wood + 8 Iron/Lead Bars + 2 Chain |
| Glider Wings | No recipe yet |

Progression proposals in [Roadmap](ROADMAP.md) have not replaced these recipes. No custom drops/chest loot are implemented for these items.

## Subclass extension

SaberDamageClass fully inherits Generic/Melee stats and Melee effects; BoltActionDamageClass inherits Generic/Ranged stats and Ranged effects. Class-only bonuses do not buff unrelated weapons.

```csharp
Item.DamageType = ModContent.GetInstance<SaberDamageClass>();
player.GetDamage<SaberDamageClass>() += 0.10f;
player.GetAttackSpeed<SaberDamageClass>() += 0.10f;
```

Use the matching class for new hitboxes/direct hits. The shared Saber right-click controller already handles other Saber-class item IDs. Weapon-specific left attacks, visuals and additional mechanics still need explicit design.
