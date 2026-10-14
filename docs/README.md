# Torn Mod documentation

Updated: 2026-10-14. Start here when continuing development.

Torn Mod is a Terraria 1.4.4.9 / tModLoader 2026.8.3 prototype with five weapons, Glider Wings, and Saber/Bolt Action damage subclasses. Its identity combines playful everyday objects with precise attack rhythms and momentum-based movement. In-game text is English.

## Contents

- [Weapons](WEAPONS.md): current mechanics, balance, subclass rules and actual recipes.
- [Glider Wings](GLIDER.md): controls, flight priority, physics and visual limitations.
- [Development](DEVELOPMENT.md): setup, profiles, build/reload, commands and config.
- [Testing](TESTING.md): recorded verification and remaining live checks.
- [Roadmap](ROADMAP.md): mood, priorities and progression proposals.
- [Maintainer notes](DECISIONS.md): source map and decisions that must survive future edits.

The latest implemented fixes preserve Saber dash exit momentum, add slope/coyote support and held-right charge repetition, provide optional R cursor warp, and let A/D change Glider facing independently of high velocity. These implementations have automated checks; complete gameplay, visual and multiplayer validation remains pending.

All maintained project Markdown lives in this folder. Previous overlapping/version-specific documents were consolidated; a local-only snapshot is `.dev/documentation-before-2026-10-14.zip` (excluded from Git, along with development saves and build caches). Desktop project notes are separate and were not edited.
