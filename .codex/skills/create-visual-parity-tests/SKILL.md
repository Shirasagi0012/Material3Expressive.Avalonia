---
name: create-visual-parity-tests
description: Author or review Compose-versus-Avalonia visual-parity cases, probes, motion checkpoints, and narrowly justified comparison-profile changes in the Material3Expressive repository. Use run-visual-parity-tests for execution and diagnosis.
---

# Create visual parity tests

Author a reproducible Compose-versus-Avalonia case. Compose is the pinned executable reference;
AndroidX remains the implementation-source baseline. Keep this Skill focused on authoring.

## Load only the task package

Always read:

1. the active `docs/roadmap/backlog/<task>.md`;
2. `docs/visual-parity-core.md`;
3. the nearest existing Compose scene, Avalonia `VisualCase`, and their relevant tests/source.

The task front matter is the route. Read its `decisions`, `docs`, and `source-baseline` entries.
Then apply these conditional loads:

| Condition | Additional material |
|---|---|
| `parity: motion` or an intermediate frame | `docs/animation-parity.md` |
| `comparison-profile: new` or `change` | `docs/comparison-profiles.md` and `tests/VisualParity/comparison-profiles.json` |
| baseline/version conflict | `docs/upstream-baselines.md` |
| infrastructure, schema, comparer, or registry change | `docs/visual-parity-test-plan.md` |
| producer protocol change | `tests/ComposeReference/README.md` |

Do not load the full ROADMAP, complete decision history, animation, profile, or architecture
documents for an ordinary static case. Load another document only when an observed issue needs it.

## Workflow

1. Read the task package and inspect the AndroidX public API, defaults, layout, child content, and
   relevant tests for the component. Write a small state matrix covering only distinct contracts.
2. Add the Compose scene and `CaseContract` under
   `tests/ComposeReference/capture-app/src/main/kotlin/.../scenes/`. Keep the exact stable ID,
   logical viewport, theme, scale, content, and state. Register named geometry/text/focus/shadow
   probes for facts that must be structural. Do not infer layout from pixels.
3. For motion, author one checkpoint case per declared time. Use the producer-owned replay schedule,
   exact elapsed ticks, scenario ID, trigger, and `usesComposeCoroutineClock` when applicable.
   Never sleep, render manually, or invent a second clock.
4. Add the matching Avalonia `VisualCase` under
   `tests/Material3Expressive.VisualTesting/Cases/`, included once in `VisualCaseCatalog`. Keep
   `Viewport` in logical DIP values. Use `VisualContext.Require<T>`, `Prepare`, and
   `SettleAnimations()` for static setup. Motion cases use matching `VisualMotionSpec`; all
   animated values belong to `MotionGroup`. Do not use native wall-clock transitions in parity.
5. Reuse the nearest comparison profile. A new or changed allowance requires a distinct renderer
   contract, narrow scope, reason, removal/recalibration condition, and positive/negative boundary
   tests. Structural defects cannot be downgraded to raster warnings.
6. Hand execution to `$run-visual-parity-tests`. Record command, fresh artifact root, official kind,
   structural/raster evidence, and remaining gaps in the task evidence. This Skill never changes
   Current focus or closes a roadmap task.

## Non-negotiable guardrails

- Both sides use the same IDs, coordinates, font, locale, direction, state, and background.
- Match physical-pixel rounding deliberately; convert to logical coordinates only afterward.
- Measure actual text layouts and compare content, style, paint, baselines, and caret boxes.
- Audit native Avalonia templates/resources when they can override Material colors or states.
- Expose a real probe for important custom-drawn facts, or document why the fact is unavailable.
- Never add checked-in PNG baselines, broad ignore regions, a third registry, or an allow-list entry.
- A motion result reports timeline-contract status separately from its captured-frame kind.

## Delivery checklist

- task route and relevant source evidence are recorded;
- Compose and Avalonia contracts match and probes are named;
- profile changes, if any, are justified and mutation-tested;
- validation is handed to the run Skill;
- task status remains unchanged unless the roadmap workflow explicitly owns that transition.

For copyable registration details, read `references/case-authoring-template.md` only when needed.
