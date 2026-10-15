# Torn Mod — project working rules

These instructions apply to this repository and its subdirectories. Preserve existing project decisions and follow the user's current instructions when continuing work.

## Documentation review at the end of every task

Before sending the final response for every user task, review whether the maintained project documentation needs updating. This review is required even when the task does not change code; a review does not require an edit when no documented information changed.

Check the following documents for relevance and accuracy:

- [docs/README.md](docs/README.md): current project overview and documentation index.
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md): setup, build/reload workflow, commands and config.
- [docs/WEAPONS.md](docs/WEAPONS.md): implemented weapon mechanics, balance, recipes and subclasses.
- [docs/GLIDER.md](docs/GLIDER.md): movement, controls, flight priority, rendering and networking.
- [docs/LANCER.md](docs/LANCER.md): held-lance preparation, contact cadence, thrusts, momentum and sprite provenance.
- [docs/TESTING.md](docs/TESTING.md): actual verification results, limitations and pending checks.
- [docs/ROADMAP.md](docs/ROADMAP.md): intended direction and future proposals.
- [docs/DECISIONS.md](docs/DECISIONS.md): established decisions, source map and maintainer handoff.

Apply these rules:

1. At the end of each task, assess whether any of these files needs an update before completing the response.
2. Update only information that actually changed. Avoid unrelated rewrites, repetitive entries and date-only edits; leave unaffected documents intact.
3. Do not change or delete previously agreed decisions without a reason. When a decision changes, record the new decision and its rationale while preserving relevant context.
4. Never describe unperformed work as complete. Distinguish implemented behavior, automated checks, real in-game/multiplayer testing, and future proposals. Record only tests actually run; retain unresolved limitations.
5. After updating documentation, give the user a short summary of what changed. If review finds no updates necessary, briefly say so.

Keep maintained documentation consolidated in `docs`; this root `AGENTS.md` is the persistent instruction entry point. When editing this file in future tasks, read it first and preserve existing instructions unless the user explicitly changes them.
