# Testing and known limits

Updated: 2026-10-15. Vanilla-class migration checks ran on this date; no live world or multiplayer session ran.

The latest recorded build on 2026-10-15 succeeded with zero compiler warnings/errors. Standalone checks last reported 82,453 numerical assertions; the final loader-time suite reported 241 assertions including the Lancer additions. These are repeated condition evaluations, including per-angle/per-tick loops and previous content. They are not tens of thousands of independent scenarios or play sessions.

## What automated checks establish

The tested vanilla-class migration package was installed into `.dev/tModLoader/Mods/TornMod.tmod` after the game closed. Its SHA-256 matches `dist/TornMod.tmod`: `D0E331B6BCEE690159E1FFA3E9B4109AB8A62CC56138FD835ED5890F9EC7177C`. Installation verification is not a live gameplay test.

- Homing acquisition/turn/target-loss behavior and item/projectile/recipe definitions.
- Saber damage inheritance, charge/cooldown hooks, Saber-only item dispatch, slope-state support, finite coyote grace, exit momentum and held-right repetition.
- Piercer bolt persistence/cancellation, mobility bonuses, bullet penetration/swept collision and casing rules.
- Crossbow ammo gating, magazine/reload/frame boundaries and item serialization.
- Glider descent/energy/speed bounds, dive/pull-out, acrobatic climb/loops, mirrored behavior and allocation-free controller steps.
- Input resolver/queued-toggle rules, remote pose fixtures, cache rotation and high-speed facing without velocity reversal.
- Lancer 14/15-tick readiness, bounded mouse tracking/angle wrap, 240-pixel midpoint grip, 40-pixel tip-only collision and sweeps, allocation-free queries, 5x-to-20x source-damage scaling, 50-mph impact impulse retained by the Glider cache, drag-decaying external overspeed and explicit network snapshot bounds, earned 60-tick NPC-specific protection/deadline/non-renewal, strict 45-mph protection threshold, per-NPC cooldowns surviving Quick interruption, once-per-thrust hits, immediate readiness resumption, vanilla melee bonuses, recipe and saved-state serialization.
- Config defaults, warp coordinate rejection and existing command lookup behavior.

[Tests/HomingChecks](../Tests/HomingChecks/Program.cs) runs pure math. [DevelopmentChecks](../TornMod/Common/DevelopmentChecks.cs) runs opt-in loader fixtures with `-server -slipchecks -slipchecks-exit`. Fresh profiles avoid user config; no world is opened. Hook/flag fixtures cannot prove complete vanilla movement interaction, and direct sprite/collision callback tests are not real rendered or terrain sessions.

## Remaining live checks

| Area | Needed verification |
| --- | --- |
| Time commands | Actual chat invocation of `/day` and `/night`; command code was compiled and existing regression checks passed. |
| Lancer | Actual 10-tick contact cadence across targets, high-speed terrain/NPC passage, Glider hand/weapon alignment, right-click interruption, impact sound/particles and boosted flight feel, damage/knockback feel and two-client mouse tracking, suspended stance and hits. |
| Saber | Actual slopes/platforms/ceilings; high-speed exit momentum; held-right loop and coyote timing; NPC passage damage. |
| Glider | Physical W/rebinding; continuous-Space ability sequence; boots fuel timing; high-speed A/D; walls/slopes; inverted gravity. |
| Drawing | Armor/held weapons through full rolls; muzzle/arm alignment; separate projectile attachment; temporary wing readability. |
| R warp | Real keypress, mount conflict, blocked targets, teleport effects and movement cancellation. |
| Ranged weapons | Live reload/fire rhythm, special ammo interactions, casing landing and magazine visuals. |
| Multiplayer | Two actual clients, latency/loss, join-mid-flight, damage/fuel/position consistency and server-controlled warp. |

The user has played the prototype and reported issues, but there is no recorded comprehensive live validation. Do not infer success merely from a compiled fix or a large assertion count. Future reports should name scenario coverage, failures, actual sessions and limits.


Vanilla-class comparison: pre-migration (2026-10-14) and post-migration (2026-10-15) both passed 82,453 pure evaluations and 241 loader assertions with zero warnings/errors. Obsolete class-isolation fixtures were replaced by vanilla-class identity and Saber item-exclusion checks; existing charge/dash, lance, reload and magazine checks still pass. No family system was added. Actual rendered tooltip labels and multiplayer gameplay still require live verification.
