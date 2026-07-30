---
id: M2-APPBAR-001
order: 260
work_item: AppBar scroll behavior
priority: P0
status: Complete
milestone: M2
dependencies: [D002]
decisions: [D002, D003]
parity: motion
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md, docs/testing/visual-parity/animation.md]
source-baseline: [AppBar.kt]
---

# AppBar scroll behavior

## Scope

Define and implement flexible app-bar layouts, shared scroll-state ownership, transitions, and
desktop input behavior.

## Exit criteria

The library-wide owned/external scroll-state convention is accepted and the app-bar behavior has
structural, interaction, accessibility, and parity evidence where supported.

## Current direction

[`D002`](../decisions/D002-shared-owned-external-state.md) option 1A is accepted. App-bar scroll
coordination is complex cross-control state, so the public behavior must expose a per-instance
non-null state object, allow application-owned replacement, define in-flight replacement and
reset semantics, and keep persistence application-owned through a recoverable initial value.

## Current implementation evidence

- AndroidX source review: `AppBar.kt:518-968,1384-1425,1693-2375,3493-3761` at
  `ff0a0a99` / Material3 1.5.0-alpha25, plus `AppBarSmallTokens`,
  `AppBarMediumTokens`, `AppBarLargeTokens`, and flexible token files at v14_0_0.
- `MaterialAppBar` now supports standard and flexible Small/Medium/Large top-app-bar layouts,
  alpha25 expanded/collapsed heights and typography, scrolled container color, navigation/action
  slots, and control- or data-valued titles. A control-valued title is explicitly handed between
  the stacked and inline presenters so it never acquires two visual parents.
- D002 option 1A is implemented through a per-instance non-null `AppBarScrollState`.
  Application-owned replacement transfers observation and immediately snaps/cancels an in-flight
  transition; `ResetState()` and runtime null normalization install a fresh expanded state.
  Applications persist a bar by constructing state with recoverable collapsed-fraction and
  content-offset values.
- `Pinned`, `EnterAlways`, and `ExitUntilCollapsed` behaviors coordinate either an attached
  `ScrollViewer` or logical deltas forwarded through `ApplyScrollDelta`. Precision wheel and
  child-boundary expansion follow desktop scrolling semantics without changing the public state
  contract.
- Collapse and expand commands use the shared deterministic spring clock; live scroll input snaps
  to its delivered offset. Title crossfade, size change, interruption, external replacement, and
  old-owner detachment are deterministic-test covered.
- The app bar exposes Avalonia's `IExpandCollapseProvider`: expanded, intermediate, and collapsed
  fractions map to the three UIA states, and the peer follows state replacement.
- Focused deterministic tests: `MaterialAppBarTests` (15/15 passed), covering standard/flexible
  tokens, all three scroll behaviors, `ScrollViewer` wiring, state/reset/replacement/interruption,
  control-valued title ownership, and UI Automation.
- The alpha22 executable producer supports the standard Medium expanded endpoint. Consecutive
  artifacts `artifacts/visual-parity/xunit/20260730-051351-712`,
  `artifacts/visual-parity/xunit/20260730-051846-243`, and final-source validation
  `artifacts/visual-parity/xunit/20260730-052253-535` pass it as
  `semanticRasterEquivalent` with exact geometry/text contracts and zero rejected pixels.
  Expected/actual raster, layout, comparison, mask, and diff SHA-256 hashes are identical between
  all three runs.
- Flexible Medium/Large app bars are alpha25-only and therefore unavailable in the alpha22
  executable reference. The alpha22 collapsed/scrolled color contract also predates the alpha25
  `SurfaceContainer` token, so it is recorded as unavailable instead of weakened into a false
  pass. The producer has no protocol for driving a live desktop `ScrollViewer`; scroll and motion
  behavior therefore use deterministic interaction/clock tests rather than fake parity rows.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `ff0a0a99` `AppBar.kt:518-968,1384-1425,1693-2375,3493-3761` and v14_0_0 AppBar token files; Avalonia `ScrollViewer` attachment and logical-delta forwarding are recorded desktop adaptations. |
| Tokens | Standard 64/112/152dp and flexible 64/112/120dp heights, 136/152dp subtitle heights, title/subtitle roles, surface/scrolled colors, and icon colors are generated from `AppBar.tokens.json`. |
| API | One XAML-compatible `MaterialAppBar` exposes `Size`, `Variant`, slots, `ScrollBehavior`, optional `ScrollTarget`, `ApplyScrollDelta`, non-null replaceable `State`, and `ResetState()`. |
| Visual | The supported standard Medium expanded endpoint passes the centralized existing `component-with-text` profile twice with exact probes and zero rejected pixels. Alpha25-only and alpha22-token-conflict endpoints are explicitly parity-unavailable. |
| Behavior | Three scroll behaviors, content-boundary handoff, command animation, title crossfade, interruption, replacement, reset, and detached old ownership are deterministic-test covered. |
| Desktop | Precision-wheel/`ScrollViewer` coordination and custom logical deltas are supported. The top bar creates no popup or additional keyboard focus stop; slotted controls retain their own focus behavior. |
| Accessibility | Pane semantics plus `IExpandCollapseProvider` expose and announce expanded, partially expanded, and collapsed states and follow external state replacement. |
| Tests | Focused suite passes 15/15; one supported parity row passes three times reproducibly at the artifact roots above; the Catalog project builds with no errors. |
| Documentation | README usage, Catalog standard/flexible examples, component matrix, roadmap history, and this task record describe the API, ownership, parity support boundary, and remaining family gap. |

The exit criteria are satisfied for top-app-bar layout and scrolling: ownership, transition,
desktop input, accessibility, structural, interaction, and supported parity evidence are present.
`BottomAppBar`/`FlexibleBottomAppBar` remain absent, so the broader AppBar family stays `Partial`
in the component matrix. D003 remains a cross-component policy question, but this task adds no
unrecorded divergence or comparison-profile exception and is not blocked by it.
