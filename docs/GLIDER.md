# Glider Wings

Updated: 2026-10-14. Pre-Hardmode momentum accessory; acquire with `/give Glider Wings`. No recipe or ordinary powered wing flight is supplied. Icon and procedural cloth/spar wings are temporary.

## Controls and current behavior

Hold Jump (Space by default) in the air. Press W once to switch Normal Glide / Acrobatics. This is a rebindable key, only eligible when equipped, Jump held, airborne and outside conflicting movement states. Normal Up/rope input remains available elsewhere.

Normal Glide uses A/D steering and Down-to-dive. High horizontal speed gives lower sink; low airspeed stalls. Release Down to convert dive momentum into horizontal travel. Mouse still aims weapons. Maximum flight speed remains 22 pixels/tick; the original normal-mode tuning was preserved.

Acrobatics smoothly turns momentum toward the mouse through full 360° loops. Mouse-down dives, mouse-forward pulls out and mouse-up spends kinetic energy to climb. Unpowered steering does not create free altitude. During the rocket-boots stage, mouse-directed thrust uses vanilla rocket bursts/fuel; no boots means no extra thrust. Switching modes keeps velocity; an upward climb switched to Normal coasts until descent resumes. Release Jump restores vanilla movement.

A/D also changes sprite facing independently of high speed, in both modes, without reversing velocity. Opposing/no input leaves facing stable. Active item animations retain weapon aim priority.

## Flight priority and input fixes

The intended sequence is normal jump → available extra jumps → rocket boots → wings → glide. Gliding does not overwrite remaining normal flight reserves. Exhausted wings/boots remain equipped without permanently blocking glide.

Vanilla continuous Jump does not automatically re-arm every extra jump/rocket latch, and vanilla wings can absorb boots fuel. The isolated GliderFlightBridge re-arms release latches in the eligible context, temporarily suppresses wing logic during earlier jump/boot stages and restores the exact reserve before movement. Vanilla still executes jumps/rocket bursts and spends fuel. Mounts, hooks, liquids, pulleys, carpet flight, incapacitation and Saber/vanilla dashes take priority.

W edges are queued until the current equipment pass is complete. Existing profiles with unassigned/default W accept the shared vanilla Up/W edge; custom rebindings take precedence. Client logs record binding and eligibility. Do not move gating back to an early stale equipment check.

## Physics, rendering and networking

GliderController owns normal/acrobatics math in gravity-relative coordinates; GliderTuning exposes drag, lift, sink, turn, speed and boost constants. GliderPlayer applies velocity before vanilla position movement and captures collision-resolved velocity afterward. Position and hitbox are not directly transformed. Controller stepping creates no per-tick managed allocations.

Wings open/fold and tuck during dives. Normal lean approaches 26°; dive lean 66°. Acrobatics rotates the whole player DrawData cache around a shared pivot. Cached armor/body/legs/held-item attachments stay together; head-only portraits are excluded. Aimed gun textures and composite arms receive compensation. Separately rendered projectiles/effects are outside this cache; their alignment still needs live inspection.

The owning client predicts motion. Mode, aim, active/dive/boost flags and bounded signed velocity snapshots accompany Terraria PlayerControls. Facing changes sync immediately; active snapshots otherwise run every six ticks and at transitions. Server checks sender identity and malformed state; joining clients receive current state. Real latency/packet-loss/two-client behavior is unverified.

Source: [Glider folder](../TornMod/Common/Glider/GliderController.cs). Full test status is maintained only in [Testing](TESTING.md).
