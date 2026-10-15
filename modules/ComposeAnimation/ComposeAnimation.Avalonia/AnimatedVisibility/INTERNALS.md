# AnimatedVisibility / enter-exit

## Reference and responsibilities

Reference: `docs/upstream-baseline.json`, AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`,
`compose/animation/animation/src/commonMain/kotlin/androidx/compose/animation/AnimatedVisibility.kt`
and `EnterExitTransition.kt`. The selected Compose Multiplatform Core has the same two files.

- `AnimatedVisibility` owns the source connection and one presentation. Its layout policy alone
  removes the outer target footprint. `Content`/`ContentTemplate`/`Source` replacement retains
  the existing reset contract; changing only the control template preserves the presentation.
- `AnimatedVisibilityScope` owns the shared three-state transition, preparation and resources.
- Each effect owner (`AnimatedVisibility`, `AnimateEnterExit`, `ContentSlot`) owns its own
  `EnterExitMotion`, registered through `Scope.Own`. A layout only connects to a motion.
- `EnterExitMotion` retains active effect definitions and deferred animation channels across
  reversal and template replacement. `trackActiveEnter`/`trackActiveExit`, segment spec selection,
  `currentAlignment` and interruption offset follow the reference. Crossfade explicitly overrides
  the alpha spec for all segments.
- `EnterExitLayout` measures full content and applies current values to `EnterExitSurface`.
  Opacity and transform matrix samples use Animation-priority contributions. The user content's
  bindings, logical parent, opacity and transform are not replaced.
- `EnterExitContentPresenter` keeps native content/template creation and rendering, and supplies
  independent decoration geometry for the cooperative target/actual policies.

## Layout and preparation

All visibility nodes join an ancestor domain using `RegisterInherited`; they never create a local
domain themselves. Outside a domain they keep native visibility animation. Cooperative content
can be used directly; ordinary content still requires an explicit `LookaheadBoundary` in a target tree.

`ILookaheadPreparationParticipant` realizes templates before observing/building the target tree.
Target callbacks neither realize templates nor create/sample animation channels. Presented content
and its data template are withheld while absent, even if a containing domain applies the host template.
Only the first actual measure releases the scope's initial layout preparation.

Target measurement retains full content geometry. Approach measures with current incoming
constraints and uses the committed target content size as the animation reference; native layout
uses its actual full measurement. Frame samples invalidate only actual measure/arrange/visuals.
Ordinary expand/shrink clips/aligns full-sized content; it does not measure children at the animated width.

`Transition.AnimationsReconfiguring` is an internal synchronization point after state/segment updates
and before channel endpoint selection. It makes active definitions coherent for every channel without
depending on a side effect in the alpha selector. The existing preparation lease prevents completion
while layout-dependent channels are being configured; seek previews do not acquire this lease.

## Accepted adaptations (2026-10-16)

The maintainer approved the implementation structure in the AnimatedVisibility design conversation.

- A hidden target has no outer footprint, including its own Margin/minimum/fixed size preferences,
  while retained descendants remain measurable/placeable. `LayoutWithoutFootprint` and the internal
  `HasLayoutFootprint` result flag express this in both target and actual bridges. Parent-owned space
  is unaffected. This preserves the local absent-content contract; it is not Compose Modifier-order parity.
- Layout/effects use DIP and double precision rather than integer pixel channels.
- Effect alignment uses the reference's physical LTR formulas, including in RTL. Ordinary content
  layout still follows Avalonia FlowDirection. This replaces the previous effect-anchor RTL flip.
- Native `IsVisible=false` immediately ends presentation. Source and content/template replacement
  reset the presentation; control-template replacement keeps its scope and motion. Existing environment
  rebuild/drain recovery continues through `ScopeResumeBinding`.
- Deferred pending visibility prepares content and participates in state mapping. MutableTransform
  manual handoff, veil effects and the upstream initial-value-animation retention facility remain absent.
  SharedTransition capture/rendering support is unchanged.

## Checks

`AnimatedVisibilityLookaheadTest` covers stable final header geometry during animated collapse,
zero footprint with retained descendants, hidden templates, reentry, template replacement, pending
target commit/cancellation, preparation retention at zero duration, and active effect replacement.
Existing visibility/content/Crossfade/shared-transition/scope-resume tests cover the shared callers.
Headless results do not certify desktop GPU rendering or Compose visual parity.
