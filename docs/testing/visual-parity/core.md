# Visual parity core contract

This is the compact contract for routine parity work. Read it with the active backlog task and
the directly relevant source and test files. Load the topic documents only when the task metadata
or the routing table requires them.

## Authority and layers

- The pinned Compose scene is the executable visual reference; AndroidX is the implementation
  source baseline. A parity pass does not prove newer upstream behavior is complete.
- The Compose and Avalonia cases must share the same stable ID, logical viewport, theme, locale,
  state, content, scale, and named structural probes.
- Structural contracts (manifest, geometry, text, focus, shadow, and motion metadata) are checked
  before raster policy. No raster profile can hide a structural defect.
- Use the centralized comparison profile. Reuse `existing` where possible; any new allowance must
  be narrow, justified, tested at its boundary, and remain visible as debt when it is a warning.
- Do not add checked-in screenshots, broad ignore regions, or a third case registry.

## Conditional routing

| Task signal | Load | 
|---|---|
| `parity: motion` or an intermediate frame | `docs/testing/visual-parity/animation.md` |
| `comparison-profile: new` or `change` | `docs/testing/visual-parity/comparison-profiles.md` and the JSON registry |
| baseline conflict or version-gap question | `docs/reference/upstream-baselines.md` |
| infrastructure, schema, or comparer change | `docs/testing/visual-parity/architecture.md` |
| ordinary static case | no additional topic document |

The active task's `decisions`, `docs`, and `source-baseline` fields are the default route. Expand
the route only when an observed failure or an implementation question requires it.

## Evidence

Authoring produces a matching Compose scene and Avalonia `VisualCase`, with probes for every
important stable fact. Execution produces the smallest relevant test result and fresh artifact
path. Report the official `comparison.json.kind`, structural failures, raster evidence, and
remaining gaps back to the task. Only the roadmap workflow changes task status or Current focus.
