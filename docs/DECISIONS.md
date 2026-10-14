# Maintainer handoff and source map

Updated: 2026-10-14. Read [README](README.md), the affected mechanic document and [Testing](TESTING.md) before continuing.

## Source map

| Concern | Source |
| --- | --- |
| Frozen weapon tuning | [WeaponBalance](../TornMod/Common/WeaponBalance.cs), [WeaponDefaults](../TornMod/Common/WeaponDefaults.cs) |
| Saber shared controller/rules | [SaberCombatPlayer](../TornMod/Common/Saber/SaberCombatPlayer.cs), [SaberWeaponRules](../TornMod/Common/Saber/SaberWeaponRules.cs) |
| Saber pure helpers/classes | [SaberMechanics](../TornMod/Common/Saber/SaberMechanics.cs), [SaberDamageClass](../TornMod/Common/Saber/SaberDamageClass.cs) |
| Glider math/vanilla bridge/input | [GliderController](../TornMod/Common/Glider/GliderController.cs), [GliderFlightBridge](../TornMod/Common/Glider/GliderFlightBridge.cs), [GliderPlayer](../TornMod/Common/Glider/GliderPlayer.cs) |
| Bolt rifle | [Piercer](../TornMod/Content/Items/Piercer.cs), [BoltActionPlayer](../TornMod/Common/BoltAction/BoltActionPlayer.cs) |
| Crossbow state and aim | [RepeatingCrossbow](../TornMod/Content/Items/RepeatingCrossbow.cs) |
| Config, warp, packets | [WeaponConfig](../TornMod/Common/WeaponConfig.cs), [WarpToCursor](../TornMod/Common/WarpToCursor.cs), [TornMod](../TornMod/TornMod.cs) |
| English UI | [en-US localization](../TornMod/Localization/en-US_Mods.TornMod.hjson), [tr-TR localization](../TornMod/Localization/tr-TR_Mods.TornMod.hjson) |
| Runtime test harness | [DevelopmentChecks](../TornMod/Common/DevelopmentChecks.cs), [test.ps1](../test.ps1) |

## Decisions to preserve

- Projectile math and drawing size are separate. Preserve supplied PNGs; crop/scale during rendering. `References/FrozenWeaponConfig.json` records the old tuning snapshot, but current constants are authoritative.
- Classify shared Saber behavior by DamageClass, not original item ID. Right charge remains 60 ticks; cooldown follows useTime/attack speed. Do not reintroduce exit velocity resets, first-enemy stopping or per-item-only charge logic.
- Keep normal Glider traversal tuning/speed. Do not turn the movement accessory into an ordinary wing slot. The bridge's intentional held-Jump/flight-priority changes belong outside pure physics.
- Glider facing and input edges must be handled independently of high-speed vanilla momentum and early equipment state. Respect active weapon aim and custom keybindings.
- Whole-player lean operates on DrawDataCache; separately drawn projectile effects need their own compatibility work. Never report uninspected graphics as verified.
- Packet kinds: 0 Saber fall protection/hit stop; 1 Glider state; 2 server-validated cursor warp. Ordinary PlayerControls/TeleportEntity and item/projectile serialization carry related state. All clients need matching updated code.
- Localization filenames already provide the `Mods.TornMod` prefix. Root entries are Items, Configs, DamageClasses, Keybinds, etc.; do not wrap the contents in another Mods/TornMod object.
- Preserve `.dev/tModLoader` saves. Fresh isolated build/check outputs are disposable; active file locks require user-controlled reload/closure, not process termination.
- Add newly resolved decisions to these documents. Keep one current test report rather than restarting a collection of contradictory version-specific Markdown files.
