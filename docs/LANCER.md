# Lancer and Iron Lance

Updated: 2026-10-14. Implemented prototype; live world/multiplayer verification remains pending.

## Controls and balance

Obtain with `/give Iron Lance`, or craft 10 Iron/Lead Bars + 5 Wood at an Anvil. It is intended for Pre-Eye of Cthulhu.

- Hold left: prepare for 15 ticks and keep the 240-pixel lance extended. Preparing and ready stances smoothly track the mouse, capped at 540 degrees/second (9 degrees/tick). Release retracts over eight ticks and disables damage immediately. Running, jumping and gliding do not cancel the stance.
- Only the last 40 pixels at the tip deal damage: 10 base damage, 1.5 knockback, independent 10-tick NPC immunity. Nominal raw DPS is 60 before defense, ordinary crits and modifiers.
- Momentum uses relative closing velocity projected onto current lance aim. At 30 mph relative forward approach speed, the first impact deals 5x source damage, linearly increasing to 20x at 100 mph (about 19.56 pixels/tick), capped thereafter. Below 30 mph it uses ordinary contact damage; it does not force a crit. Continuous contact cannot repeat the bonus; 20 consecutive ready ticks outside the damaging tip path rearm that NPC. Quick Thrust does not rearm momentum.
- Successful first tip impact above 45 mph of player forward speed adds a fixed 50 mph (about 9.78 pixels/tick) forward impulse and earns 60 ticks of contact protection only against that NPC. Ordinary contact does not renew it. Missed/predicted hits, other NPCs, enemy projectiles, falls and terrain are not protected. Player position is never manually advanced; normal tile collision still manages movement.
- Right temporarily suspends the stance for Quick Thrust: 30 base damage, knockback 8 away from the player, 30-tick use interval. Its locked-aim animation lasts 12 ticks, peaking at tick 4; each NPC is hit once and momentum never applies. Held left resumes a previously ready stance immediately. Incomplete preparation resumes saved progress instead of granting free readiness. Releasing left during Quick ends it without restoring the stance.

## Architecture and rendering

Iron Lance and its held projectile use vanilla DamageClass.Melee; Saber controls remain restricted to the Saber item. No weapon-family or custom damage class is registered. Prototype timing remains fixed despite inherited attack-speed stats. LancerTuning centralizes readiness, turn rate, tip length, impulse/protection and momentum parameters; LancerSettings contains weapon damage, length, knockback and intervals.

Future lances extend LancerWeapon and override settings, sprite crop and recipe. Assigning Melee damage alone does not opt another ModItem into the lance mechanics template. LancerController owns phases, aim, extension and suspended stance; LancerPlayer owns input, use cooldown and earned per-NPC protection. Standard projectile AI carries phase/age/angle; extra AI carries source item/slot and saved stance/progress. Owner aim updates are sent every three ticks and phase transitions immediately. Peers do not use their own mouse or independently decide readiness/resume.

Quick reuses the held projectile. Local immunity stays at 10 ticks; shared per-NPC contact counters survive mode changes, and separate NPC-instance storage enforces one hit per thrust. Switching modes cannot clear a preceding contact cooldown. The player-level 30-tick Quick cooldown and momentum/separation tables also survive the interruption. No per-tick disposable collections are introduced.

Drawing uses a midpoint grip and full 240-pixel shaft. Collision derives the final 40 pixels from those same endpoints and sweeps that segment between ticks, including intermediate targets and aim turns. Large discontinuities reset sweep history; cursor warp cancels lances. Grip position compensates for Glider lean. A narrowly called Glider external-momentum hook captures successful Lancer impulses so cached flight velocity does not overwrite them. Ordinary Glider traversal tuning is unchanged. Explicit external momentum temporarily allows overspeed, decaying with drag instead of clamping away the 50-mph impulse. The existing Glider packet adds an external-momentum flag, with finite speed validation capped at 256 pixels/tick; player controls carry position. No new packet kind was added. Protection activation plays a vanilla metal sound and a gold particle ring; projectile extra AI carries an event counter so remote clients can show it. All peers need matching updated code.

## Art provenance

The provided diagonal PNG is preserved in `References/IronLance-original.png`. The built-in imagegen edit produced the horizontal, transparent variant at `TornMod/Content/Items/IronLance.png`; its visible crop is `(25, 302, 2122, 121)` within a 2172×724 canvas. Alpha outside the sprite was checked. This is a generated orientation edit, not a pixel-identical mathematical rotation.

The exact image-edit prompt and asset hashes are recorded in [art metadata](../References/IronLance-art.json). It requested the same iron point, wooden shaft, binding rings, leather grip and pommel, aligned horizontally with a right-facing point and transparent background.

## Verification and pending checks

Pure checks cover 14/15-tick readiness, bounded aim and angle wrap, tip-only stationary/swept geometry, suspended ready/incomplete stance, release, peer authority, momentum cap and allocation-free sweeps. Loader fixtures cover item/class/recipe/tooltip, inheritance, capped damage without forced crit, fixed impulse, earned NPC-specific protection, shared contact/Quick cooldowns, once-per-thrust hits, immediate ready resumption and extra-AI serialization. Existing regression checks also run.

These checks do not simulate a complete live world or two clients. Pending: actual per-enemy cadence, real high-speed NPC/terrain contacts, sustained button behavior, Glider hand alignment/full rolls, visual readability, and network latency/owner aim/hit consistency. See [Testing](TESTING.md) for the current consolidated results.
