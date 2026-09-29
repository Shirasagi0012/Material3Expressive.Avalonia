# Rewrite starting point

This page is the current status and short work plan for `src/Material3ExpressiveRefactor`.
It describes code in this branch, not the earlier `src/Material3Expressive` implementation.

## What exists

| Area | Current code | Boundary |
| --- | --- | --- |
| Tokens | `Tokens/`, `Baseline/`, `Foundation/Tokens/` | Hand-authored typed keys and baseline sources. `{m3:Token}` resolves aliases and simple derived values from the consuming resource host. No token generator or JSONC catalog is connected. |
| Visual foundation | `Foundation/Rendering/` | Surface, shadow, symbol, and interaction rendering. |
| Interaction | `Foundation/Interaction/` | State layers, ripples, and press-shape behaviour. |
| Components | `Controls/`, `Styles/` | Button and IconButton families. The catalog demonstrates these and Surface. |
| Motion | `Foundation/Motion/` | Current scalar spring/tween engine and frame scheduler. It will be migrated to the separate Compose animation implementation. |
| Tests | `tests/Material3ExpressiveRefactor.Tests/` | Unit and headless tests for the current rewrite. No Compose visual parity harness is connected. |

The old Raw-token → Material Value design documents described another implementation. This
rewrite currently uses `MaterialTokenKey<T>`, `MaterialResource`, and `TokenExtension`.
Changes to that architecture should be deliberate; the removed documents are reference
material in the old checkout, not a requirement for this branch.

## Next slices

1. **Animation integration.** Map resolved motion token values to the external runtime's specs.
   Migrate the clock and lifecycle first, then state-layer/ripple timing and pressed-corner motion.
   Keep deterministic intermediate-frame tests while replacing the current scalar engine.
2. **Rewrite visual parity.** Add a Compose reference producer and an Avalonia capture harness
   targeting this project when needed. Start with Surface, Button, and IconButton,
   and record the exact upstream source and executable-reference versions for each comparison.
3. **Component growth.** Port one family at a time with its token definitions, style, behaviour,
   accessibility, and focused tests. Add visual cases as each family lands.
4. **Repository portability.** Make every pinned submodule commit fetchable from its remote,
   then restore a focused build-and-test CI workflow. The old publishing workflow was removed
   because it targeted the previous implementation.

## Working rules

- Compare against a pinned Compose source revision and record intentional desktop adaptations.
- Keep Material design values separate from animation execution and control state.
- Report unit/headless, visual parity, and interactive desktop checks separately.
- Update this page when a slice becomes active or complete; avoid parallel roadmaps.
