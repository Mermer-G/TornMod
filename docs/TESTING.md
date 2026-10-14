# Testing and known limits

Updated: 2026-10-14. Documentation cleanup did not run new gameplay or code tests.

The latest recorded build on 2026-10-13 succeeded with zero compiler warnings/errors. Standalone checks last reported 82,383 numerical assertions; the final loader-time suite reported 205 assertions after the high-speed facing fix. These are repeated condition evaluations, including per-angle/per-tick loops and previous content. They are not tens of thousands of independent scenarios or play sessions.

## What automated checks establish

- Homing acquisition/turn/target-loss behavior and item/projectile/recipe definitions.
- Saber damage inheritance, charge/cooldown hooks, subclass dispatch, slope-state support, finite coyote grace, exit momentum and held-right repetition.
- Piercer bolt persistence/cancellation, mobility bonuses, bullet penetration/swept collision and casing rules.
- Crossbow ammo gating, magazine/reload/frame boundaries and item serialization.
- Glider descent/energy/speed bounds, dive/pull-out, acrobatic climb/loops, mirrored behavior and allocation-free controller steps.
- Input resolver/queued-toggle rules, remote pose fixtures, cache rotation and high-speed facing without velocity reversal.
- Config defaults, warp coordinate rejection and existing command lookup behavior.

[Tests/HomingChecks](../Tests/HomingChecks/Program.cs) runs pure math. [DevelopmentChecks](../TornMod/Common/DevelopmentChecks.cs) runs opt-in loader fixtures with `-server -slipchecks -slipchecks-exit`. Fresh profiles avoid user config; no world is opened. Hook/flag fixtures cannot prove complete vanilla movement interaction, and direct sprite/collision callback tests are not real rendered or terrain sessions.

## Remaining live checks

| Area | Needed verification |
| --- | --- |
| Saber | Actual slopes/platforms/ceilings; high-speed exit momentum; held-right loop and coyote timing; NPC passage damage. |
| Glider | Physical W/rebinding; continuous-Space ability sequence; boots fuel timing; high-speed A/D; walls/slopes; inverted gravity. |
| Drawing | Armor/held weapons through full rolls; muzzle/arm alignment; separate projectile attachment; temporary wing readability. |
| R warp | Real keypress, mount conflict, blocked targets, teleport effects and movement cancellation. |
| Ranged weapons | Live reload/fire rhythm, special ammo interactions, casing landing and magazine visuals. |
| Multiplayer | Two actual clients, latency/loss, join-mid-flight, damage/fuel/position consistency and server-controlled warp. |

The user has played the prototype and reported issues, but there is no recorded comprehensive live validation. Do not infer success merely from a compiled fix or a large assertion count. Future reports should name scenario coverage, failures, actual sessions and limits.
