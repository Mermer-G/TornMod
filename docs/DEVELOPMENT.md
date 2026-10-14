# Development and test tools

Updated: 2026-10-14. Commands below run from the repository root, not `docs`.

## Environment and profiles

Use [TornMod.sln](../TornMod.sln), .NET 8, Steam Terraria and tModLoader. [global.json](../global.json) selects the 8.0.400 feature band with latest-patch roll-forward; SDK 8.0.425 and Visual Studio Community 2026 were confirmed during setup. The installed tModLoader baseline is 2026.8.3.0 / Terraria 1.4.4.9.

Open the solution with [open-project.ps1](../open-project.ps1). F5 uses the Terraria launch profile; Terraria - Quick Test adds `-skipselect`. Saves/config/mod packages live in `.dev/tModLoader`. This is the user's real development character/world data: preserve it. Steam saves are separate.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\test.ps1
```

`test.ps1` uses a fresh `.dev/checks/<id>` profile and automatically exits. Build parameters can supply fresh intermediate/output folders and `DevelopmentSavePath` when ordinary output files are locked. A tested package can be copied to `dist/TornMod.tmod` and `.dev/tModLoader/Mods/TornMod.tmod` once unlocked. An external build does not update an already loaded assembly.

For normal reload: save and leave the world, then Workshop → Develop Mods → Build + Reload. Supported method-body changes may use Visual Studio Hot Reload under the debugger. New structure/assets require reload or restart; source/PDB mismatches prevent Hot Reload. Never terminate the user's game to release a package lock. Logs are in the installed `tModLoader-Logs` folder.

## Singleplayer chat commands

| Command | Behavior |
| --- | --- |
| `/give <name, ID or Mod/Item> [amount]` | Dynamic item lookup; supports spaces and unique partial matches. Ambiguity lists choices. |
| `/give list [search]` | Lists this mod's items, or searches all items. New items need no command edits. |
| `/sliptest` | Five weapons, 1000 slippers, 500 bullets and 500 arrows. Use `/give Glider Wings` separately. |
| `/spawnrate <0–100 or reset>` | Natural spawn multiplier; resets on world exit. Does not remove existing enemies. |
| `/boss list` / `/boss eye` | Boss aliases, vanilla boss names or IDs; normal boss behavior still applies. |
| `/dummy` | Five placeable Target Dummies and one DPS Meter. |
| `/zombies` | Up to five zombies on each side; reports placement/slot failures. |

Give amounts are 1–9999, bounded to 100 resulting stacks. Overflow items drop beside the player. These commands are singleplayer-only.

## Gameplay Settings

[WeaponConfig](../TornMod/Common/WeaponConfig.cs) exposes three server-side booleans, all false by default: Instant Respawn, Invincibility and Warp to Cursor. Weapon/glider tuning is in code, not config.

With Warp to Cursor enabled, R places the player's center at the cursor if its upright hitbox fits inside world bounds without solid tiles. It cancels combat/flight, hooks and mount, and resets velocity. Disabled: ordinary R Quick Mount. The key is rebindable; shared/default R has an input fallback. Multiplayer requests are validated by the server before normal teleport synchronization.

Installed API XML/DLL and the official [Visual Studio](https://github.com/tModLoader/tModLoader/wiki/Developing-with-Visual-Studio), [debugging](https://github.com/tModLoader/tModLoader/wiki/Learn-How-To-Debug) and [command-line](https://github.com/tModLoader/tModLoader/wiki/Command-Line) guides are reference entry points, not evidence of live testing.
