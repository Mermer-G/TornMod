# Damage classification decision

Updated: 2026-10-15. Implemented: vanilla damage classes only. The earlier family-layer proposal was explicitly rejected by the user before implementation.

| Weapon | Damage class |
| --- | --- |
| Saber | Melee |
| Iron Lance | Melee |
| Piercer | Ranged |
| Repeating Crossbow | Ranged |

The three custom DamageClass definitions and their localization were removed. Damaging projectiles and Saber direct hits now use vanilla classes, with standard damage labels and vanilla bonuses. No family tags, family stats, registry or accessory layer was introduced.

Saber previously used its custom class to dispatch charge/dash and query slash speed. Dispatch now identifies the Saber item, and speed/direct hits use Melee. Ordinary melee weapons must not receive Saber mechanics. Lancer keeps its existing LancerWeapon mechanics template, while Piercer/Crossbow retain their own reload/magazine state. These are mechanics, not new damage types.

Pre/post migration both passed test.ps1 with zero compiler warnings/errors, 82,453 pure evaluations and 241 loader assertions. Class-specific fixtures were adapted to vanilla identities and unrelated-item exclusion; no live world, rendered-tooltip or multiplayer session was performed. See [Testing](TESTING.md).