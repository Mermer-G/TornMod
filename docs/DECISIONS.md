# Maintainer handoff and source map

Updated: 2026-10-14. Read root [AGENTS.md](../AGENTS.md), [README](README.md), the affected mechanic document and [Testing](TESTING.md) before continuing.

## Source map

| Concern | Source |
| --- | --- |
| Frozen weapon tuning | [WeaponBalance](../TornMod/Common/WeaponBalance.cs), [WeaponDefaults](../TornMod/Common/WeaponDefaults.cs) |
| Saber shared controller/rules | [SaberCombatPlayer](../TornMod/Common/Saber/SaberCombatPlayer.cs), [SaberWeaponRules](../TornMod/Common/Saber/SaberWeaponRules.cs) |
| Saber pure helpers | [SaberMechanics](../TornMod/Common/Saber/SaberMechanics.cs) |
| Glider math/vanilla bridge/input | [GliderController](../TornMod/Common/Glider/GliderController.cs), [GliderFlightBridge](../TornMod/Common/Glider/GliderFlightBridge.cs), [GliderPlayer](../TornMod/Common/Glider/GliderPlayer.cs) |
| Bolt rifle | [Piercer](../TornMod/Content/Items/Piercer.cs), [BoltActionPlayer](../TornMod/Common/BoltAction/BoltActionPlayer.cs) |
| Shared Lancer controller/weapon/player | [LancerMechanics](../TornMod/Common/Lancer/LancerMechanics.cs), [LancerWeapon](../TornMod/Common/Lancer/LancerWeapon.cs), [LancerPlayer](../TornMod/Common/Lancer/LancerPlayer.cs), [HeldLanceProjectile](../TornMod/Content/Projectiles/HeldLanceProjectile.cs) |
| Crossbow state and aim | [RepeatingCrossbow](../TornMod/Content/Items/RepeatingCrossbow.cs) |
| Config, warp, packets | [WeaponConfig](../TornMod/Common/WeaponConfig.cs), [WarpToCursor](../TornMod/Common/WarpToCursor.cs), [TornMod](../TornMod/TornMod.cs) |
| English UI | [en-US localization](../TornMod/Localization/en-US_Mods.TornMod.hjson), [tr-TR localization](../TornMod/Localization/tr-TR_Mods.TornMod.hjson) |
| Runtime test harness | [DevelopmentChecks](../TornMod/Common/DevelopmentChecks.cs), [test.ps1](../test.ps1) |

## Decisions to preserve

- Projectile math and drawing size are separate. Preserve supplied PNGs; crop/scale during rendering. `References/FrozenWeaponConfig.json` records the old tuning snapshot, but current constants are authoritative.
- Updated 2026-10-15 at user request: use vanilla Melee/Ranged classes only, with no weapon-family layer. This supersedes custom-class dispatch: Saber controls now explicitly identify the Saber item so other melee weapons do not acquire its charge. Preserve its 60-tick charge, useTime/attack-speed cooldown, exit momentum and enemy passage.
- Keep normal Glider traversal tuning/speed. Do not turn the movement accessory into an ordinary wing slot. The bridge's intentional held-Jump/flight-priority changes belong outside pure physics.
- Glider facing and input edges must be handled independently of high-speed vanilla momentum and early equipment state. Respect active weapon aim and custom keybindings.
- Whole-player lean operates on DrawDataCache; separately drawn projectile effects need their own compatibility work. Never report uninspected graphics as verified.
- Packet kinds: 0 Saber fall protection/hit stop; 1 Glider state; 2 server-validated cursor warp. Ordinary PlayerControls/TeleportEntity and item/projectile serialization carry related state. All clients need matching updated code.
- Localization filenames already provide the `Mods.TornMod` prefix. Root entries are Items, Configs, DamageClasses, Keybinds, etc.; do not wrap the contents in another Mods/TornMod object.
- Preserve `.dev/tModLoader` saves. Fresh isolated build/check outputs are disposable; active file locks require user-controlled reload/closure, not process termination.
- Add newly resolved decisions to these documents. Keep one current test report rather than restarting a collection of contradictory version-specific Markdown files.
- Before completing every user task, assess all maintained documents for necessary updates. Edit only genuinely changed information, preserve decisions with rationale, distinguish implementation from actual validation, and give the user a brief documentation-change summary. The persistent rule is in root AGENTS.md.

- Lancer uses vanilla Melee damage, retaining reusable LancerWeapon mechanic definitions. Keep readiness/smooth aim tracking independent of movement; preserve per-NPC local immunity, first-contact-only momentum and no momentum on Quick Thrust. Swept tip geometry uses value types and shares the rendered grip/tail/tip. Warp cancels held lances to prevent teleport-length sweeps. Lancer synchronizes via standard projectile ai plus extra-AI source item/slot and saved stance/progress, without a new mod packet kind.


Superseded Lancer balance revision (2026-10-14, retained for rationale): user feedback supersedes the original per-deployment +150% momentum cap. Separate passes now rearm after 20 ticks out of contact; momentum guarantees crits scaling to 10x total damage at 100 mph projected relative approach speed. Continuous contact remains unboosted after its first impact. Quick Thrust explicitly knocks targets away from the player.

Current Lancer rework supersedes that forced-crit revision at user request: 15-tick preparation, 120-pixel shaft/40-pixel damaging tip, 540 degrees/second tracking, +150% source-damage momentum cap, earned 12-tick NPC-only protection and fixed forward impulse. Quick preserves stance and shared contact immunity; held left resumes readiness immediately. Keep ordinary Glider tuning unchanged and capture external momentum only when a successful Lancer impact earns it.

Latest Lancer tuning supersedes the initial rework numbers: 240-pixel midpoint grip; damage 5x at 30 mph to 20x at 100 mph relative approach, capped thereafter. Above 45 mph of player forward speed, a successful first impact grants 60-tick NPC-only protection and a 50-mph impulse with sound/particles. The existing Glider state packet now includes external-momentum status so boosted overspeed decays naturally while ordinary flight limits remain unchanged. Matching mod versions are required on all peers.
