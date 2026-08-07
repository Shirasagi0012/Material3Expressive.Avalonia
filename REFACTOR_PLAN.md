# Material Runtime Context Refactor Plan

Status: Ready for implementation

Scope: Intentional breaking refactor; source and binary compatibility are not required

Last verified against the repository: 2026-08-07

## 1. Decision summary

Replace the current mixed `MaterialControl` / `MaterialThemeWatcher` / independently-owned
`MotionGroup` / `MaterialInteractionHost` architecture with one private runtime context per actual
`Control`.

The final architecture has these properties:

- Every Material component inherits the most appropriate Avalonia base class.
- A private, non-inherited attached property stores one `MaterialElementContext` on each owner.
- `MaterialThemeScope.ResolvedThemeProperty` remains the sole Material theme transport.
- `MaterialElementContext` owns the owner's resolved theme state and lifecycle orchestration.
- One lazy motion coordinator supplies one frame clock and multiple disposable scopes per owner.
- One lazy interaction controller translates physical input into Material interaction state.
- Components continue to own token resolution, component state, layout, and rendering policy.
- Template presenters resolve their own theme and borrow only their templated owner's interaction.
- The old runtime types and all compatibility shims are deleted at the end.

This is not a public XAML behavior and does not introduce a new public service locator. The attached
property is only an owner-local storage mechanism for internal runtime state.

## 2. Goals and non-goals

### Goals

- Delete `MaterialControl` without losing theme, motion, or interaction behavior.
- Eliminate duplicate theme subscriptions and duplicate frame schedulers on the same owner.
- Make ownership and lifetime explicit for controls, helpers, and template presenters.
- Preserve all component-local token invalidation paths such as Variant, Size, IsError, checked
  state, and effective-enabled changes.
- Replace private-field reflection in tests with intentional internal test seams.
- Make detach, reattach, template replacement, and stale animation-frame behavior deterministic.
- Leave no obsolete aliases, forwarding base classes, dual runtime paths, or legacy test bridges in
  the final tree.

### Non-goals

- Preserving source or binary compatibility for the deleted public runtime types.
- Exposing `MaterialElementContext` to application code.
- Applying Material runtime behavior globally to every Avalonia `Control` through a class handler.
- Moving component runtime policy into `Foundation`.
- Turning component properties or token selection into a generic behavior DSL.

## 3. Verified current baseline

Re-run these commands before implementation because the counts can drift:

```bash
rg -n 'class\s+\w+\s*:\s*MaterialControl\b' src/Material3Expressive
rg -n 'new\s+MaterialThemeWatcher\(' src/Material3Expressive
rg -n 'new\s+MaterialInteractionHost\(' src/Material3Expressive
rg -n 'MaterialInteraction\.GetOrCreate' src/Material3Expressive
rg -n 'new\s+MotionGroup\(' src/Material3Expressive
```

At the verification date, production source contains:

- 17 controls inheriting `MaterialControl`.
- 33 `MaterialThemeWatcher` construction sites, including `MaterialControl` and
  `MaterialInteractionHost`.
- 14 `MaterialInteractionHost` construction sites: 13 direct component sites and one old attached
  cache factory.
- 4 presenter call sites using `MaterialInteraction.GetOrCreate`.
- 13 `MotionGroup` construction sites, including `MaterialControl` and
  `MaterialInteractionHost`.

No production factory, style, collection, or runtime type check depends on `MaterialControl` as a
type. Its production references are inheritance declarations and stale explanatory comments.

The 17 current subclasses to migrate to `TemplatedControl` are:

- `MaterialAppBar`
- `MaterialBadge`
- `MaterialCircularProgressIndicator`
- `MaterialDatePicker`
- `MaterialDivider`
- `MaterialFabMenu`
- `MaterialFloatingToolbar`
- `MaterialLinearProgressIndicator`
- `MaterialLoadingIndicator`
- `MaterialNavigationBar`
- `MaterialSearchBar`
- `MaterialShortNavigationBar`
- `MaterialSnackbar`
- `MaterialTabs`
- `MaterialTextField`
- `MaterialTimePicker`
- `MaterialWideNavigationRail`

## 4. Target architecture

```mermaid
flowchart TB
    Theme[MaterialTheme] --> Scope[MaterialThemeScope]
    Scope -->|inherited ResolvedThemeProperty| Owner[Actual Control]

    Owner -->|private non-inherited attached property| Context[MaterialElementContext]

    Context --> ThemeState[MaterialThemeState]
    Context -. lazy .-> Motion[MaterialMotionCoordinator]
    Context -. lazy .-> Interaction[MaterialInteractionController]

    Motion --> ComponentScope[Component motion scope]
    Motion --> InteractionScope[Interaction motion scope]
    Motion --> PresenterScope[Disposable presenter scopes]

    Context -->|internal capability interfaces| Owner
    Interaction -->|direct owner callback| Owner
    Interaction -->|disposable observer/presentation lease| Presenter[Template presenter]

    Scope -->|theme at the presenter itself| Presenter
    Presenter -->|private context on presenter| PresenterContext[Presenter context]
```

### Ownership boundaries

| Owner | Responsibilities | Must not own |
|---|---|---|
| `MaterialThemeScope` | Publish and inherit immutable resolved theme snapshots | Component callbacks, motion, interaction |
| `MaterialElementContext` | Theme state, deduplication, lifecycle, controller construction, dispatch ordering | Token selection, input details, drawing |
| Concrete component | Token resolution, component state, layout, rendering, precise invalidation | Theme/tree subscriptions, independent frame scheduler |
| `MaterialMotionCoordinator` | One owner clock, animation advancement, scope management, lifecycle settle | Theme or component policy |
| `MaterialMotionScope` | Register and locally settle/remove one subsystem's animations | Independent time source |
| `MaterialInteractionController` | Physical input, interaction state, state layer, ripple, focus indication | Theme watcher, independent motion coordinator |
| Template presenter | Presentation geometry and drawing | Templated owner's theme state |
| `Foundation` | Pure tokens, state, animation, shape, and motion primitives | Control runtime lifecycle |

## 5. Locked design decisions

### 5.1 Private attached context

Use one private, non-inherited attached property:

```csharp
internal sealed class MaterialElementContext
{
    private static readonly AttachedProperty<MaterialElementContext?> ContextProperty =
        AvaloniaProperty.RegisterAttached<Control, MaterialElementContext?>(
            "__MaterialElementContext",
            typeof(MaterialElementContext),
            defaultValue: null,
            inherits: false);

    internal static MaterialElementContext GetOrCreate(Control owner);

    internal static bool TryGet(
        Control owner,
        [NotNullWhen(true)] out MaterialElementContext? context);
}
```

Rules:

- Do not expose the property or public get/set accessors.
- Do not clear it on logical or visual detach.
- Do not inherit it to children or template parts.
- Verify UI-thread access and do not add locking.
- `TryGet` must not create a context.
- Public Material controls acquire their context in their constructor.
- Trusted Material template presenters may acquire the templated owner's context during attach so
  stock Avalonia controls can use Material interaction behavior.
- A non-Control helper receives an existing context or scope; it must not create another runtime for
  the same owner.

Use an attached property rather than a `ConditionalWeakTable`: this is state belonging to a specific
Avalonia object, it remains discoverable for diagnostics/tests, and the owner-property-store-context
cycle remains collectible. Handle the attached property's one-time property notification through
two-phase publication.

### 5.2 Two-phase creation and reentrancy

The context state machine is:

```text
Missing
  -> Created       constructor completed; no subscriptions or callbacks
  -> Publishing    context stored in the private attached property
  -> Active        lifecycle subscriptions installed; initial theme may publish
```

Requirements:

- The context constructor is side-effect free.
- Store the context in the attached property before activation.
- A reentrant `GetOrCreate` during `SetValue` returns the same publishing instance.
- Publishing never calls component theme, motion, or interaction callbacks.
- Activation is idempotent and completes in `finally` after attached-property publication.
- If the owner is already logically attached at first creation, publish its current theme after
  publication completes.
- Normal control construction must not receive runtime callbacks.
- Fields must have valid Default-theme initialization before the first attach callback.

### 5.3 Theme state and first-publication sentinel

`MaterialElementContext` owns:

```csharp
internal readonly record struct MaterialThemeState(
    ResolvedTheme Theme,
    bool IsDark)
{
    internal static MaterialThemeState Default => new(ResolvedTheme.Default, false);
}
```

It must separately track whether it has ever published:

```csharp
private MaterialThemeState _theme = MaterialThemeState.Default;
private bool _hasPublishedTheme;
```

Deduplicate only after the first publication:

```text
hasPublishedTheme
AND ReferenceEquals(next.Theme, current.Theme)
AND next.IsDark == current.IsDark
    => suppress
```

This preserves the required first callback when no theme is declared and the effective snapshot is
already `ResolvedTheme.Default`.

Theme inputs are only:

- `MaterialThemeScope.ResolvedThemeProperty`
- `ThemeVariantScope.ActualThemeVariantProperty`

The context must commit the new state before invoking consumers. A relevant change raised during a
theme callback is queued and drained after the current dispatch instead of recursively dispatching.

### 5.4 Theme changes and component-local token invalidation are separate channels

Resolved component tokens depend on both context state and component-local properties:

```text
Resolved component tokens =
    Resolve(context theme, variant, size, error, enabled, checked, component state, ...)
```

Use an internal theme consumer only for context-originated changes:

```csharp
internal interface IMaterialThemeConsumer
{
    void OnMaterialThemeChanged(MaterialThemeState state);
}
```

Every component must retain its own `OnPropertyChanged` paths for local inputs. Both sources call the
same component-owned token application method:

```csharp
private void ApplyCurrentTokens() => ApplyTokens(_material.Theme);

void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) =>
    ApplyTokens(state);

protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);

    if (change.Property == VariantProperty
        || change.Property == SizeProperty
        || change.Property == IsErrorProperty
        || change.Property == IsEffectivelyEnabledProperty)
    {
        ApplyCurrentTokens();
    }
}
```

Do not force a duplicate theme dispatch to represent a local component property change. The context
must not know which component properties affect tokens.

`ApplyTokens` remains responsible for the component's precise measure/arrange/render invalidation so
theme-originated and local-property-originated recomputation behave identically.

### 5.5 Detach and reparent theme semantics

Do not attempt to retain the last attached theme while off-tree.

Avalonia clears the inheritance parent before its logical-detach event. Inherited theme and variant
fallback notifications can therefore arrive while `Parent` and `IsAttachedToLogicalTree` still look
attached. A flag set in `DetachedFromLogicalTree` is too late to suppress those notifications without
also introducing deferred/heuristic theme dispatch.

The context must instead follow the current effective Avalonia values:

- Publish the inherited fallback, including `ResolvedTheme.Default`, during detach.
- On reattach/reparent, resolve and publish the new scope.
- Retain the same context instance and subscriptions across both operations.

### 5.6 Capability interfaces and dispatch

Use direct internal owner interfaces rather than per-control anonymous runtime subscriptions:

```csharp
internal interface IMaterialThemeConsumer
{
    void OnMaterialThemeChanged(MaterialThemeState state);
}

internal interface IMaterialMotionConsumer
{
    void OnMaterialMotionFrame();
}

internal interface IMaterialInteractionConsumer
{
    void OnMaterialInteractionChanged(InteractionState state);
}
```

Only implement the capabilities a component consumes. Secondary consumers whose lifetime is shorter
than the owner use disposable observer leases instead of owner interfaces.

### 5.7 One motion coordinator and multiple scopes per owner

Replace `MotionGroup` with internal types:

```csharp
internal sealed class MaterialMotionCoordinator
{
    internal MaterialMotionScope CreateScope(string diagnosticName);
    internal void StartIfNeeded();
    internal void SettleAllForLifecycle();
}

internal sealed class MaterialMotionScope : IDisposable
{
    internal T Add<T>(T animation) where T : IAnimation;
    internal SpringAnimation AddSpring(double value, SpringSpec spec);
    internal TweenAnimation AddTween(/* ... */);
    internal void Start();
    internal void Settle();
}
```

Rules:

- Exactly one coordinator may be constructed per context.
- The component, interaction controller, and short-lived presenters use separate scopes.
- All scopes share the same time axis; a scope must not expose `UseManualClock` or `AdvanceBy`.
- Scope `Settle`/`Dispose` affects only that scope.
- Only context lifecycle and test infrastructure may settle or advance the whole coordinator.
- `MotionTestClockScope` registers coordinators and drives every scope atomically.
- Public Foundation animation primitives may remain public; the control-bound scheduler is internal.

The production frame sequence is:

```text
advance animations in all scopes
  -> interaction removes completed transient animations
  -> owner IMaterialMotionConsumer synchronizes outputs
  -> secondary frame observers run
  -> invalidate owner visual
  -> re-check running state after every callback
  -> request another frame if required
```

Preserve the existing post-callback running-state check. A callback such as a snackbar transition
may retarget or start a new animation after the initial animation loop has completed.

The coordinator must use a generation/epoch for scheduled TopLevel callbacks:

- Increment/cancel the generation on visual detach and manual-clock takeover.
- Ignore callbacks captured under an older generation.
- On visual detach, cancel the generation and settle; do not suspend running animation across an
  unrendered gap.
- On visual attach, retry `StartIfNeeded` for work created before a TopLevel existed.

### 5.8 Interaction controller channels

Replace `MaterialInteractionHost` with one internal controller created only by the context:

```csharp
internal sealed class MaterialInteractionController
{
    internal InteractionState State { get; }
    internal double StateLayerOpacity { get; }

    internal void SetInputEnabled(bool enabled);
    internal void SetExternalState(InteractionState state, bool active);
    internal void UpdateIndicationPolicy(MaterialInteractionPolicy policy);

    internal IDisposable Subscribe(IMaterialInteractionObserver observer);
    internal MaterialInteractionPresentationLease AttachPresentation(Control presenter);
}
```

These are distinct channels:

- `SetInputEnabled` is a dynamic physical-input gate. Transitioning to false clears Hovered,
  Focused, Pressed, and press ripples.
- `SetExternalState` represents component-owned states such as Dragged and must not be conflated
  with the input gate.
- `UpdateIndicationPolicy` changes ripple/focus/hover/drag indication behavior without changing the
  underlying interaction state.
- Logical detach clears input-derived state and ripples, then settles lifecycle motion.
- Logical detach does not unconditionally clear externally-owned state.

The controller:

- Reads theme state from its context and receives explicit context theme notifications.
- Uses a dedicated scope from the owner's single motion coordinator.
- Installs owner input handlers exactly once on first creation.
- Uses named handlers rather than unremovable anonymous subscriptions where practical.
- Does not create a watcher, scheduler, or second attached cache.

### 5.9 Presentation leases are live, per-frame geometry providers

Presentation identity is stable for an attach lifetime, but its geometry is mutable:

```csharp
internal sealed class MaterialInteractionPresentationLease : IDisposable
{
    internal MaterialInteractionGeometry Geometry { get; }
    internal void UpdateGeometry(in MaterialInteractionGeometry geometry);
    internal void DrawLayers(DrawingContext context, IBrush? brush, CornerRadius cornerRadius);
}
```

Requirements:

- Controls such as Switch, Checkbox, Slider, RangeSlider, RadioButton, and NavigationItem may call
  `UpdateGeometry` from every Render.
- `MaterialRipplePresenter` may call it from every Render after `TranslatePoint`.
- Updating a struct value must not create a subscription or lease allocation per frame.
- The controller retains the last geometry for pointer and keyboard ripple origin calculations.
- Before the first update, the default interaction area is the owner bounds.
- Disposing a lease removes its geometry and observer relationship immediately.
- A controller has one primary physical presentation geometry at a time. Independent interaction
  targets require independent Control owners/contexts.
- Presenters that only react to state/motion use observer leases and do not compete for geometry.

### 5.10 Theme scope is per actual element

- A presenter that consumes tokens acquires its own `MaterialElementContext` and reads its own
  inherited theme.
- It may borrow the templated owner's interaction controller through a disposable lease.
- Never replace the presenter's theme with the templated owner's theme merely because interaction
  is shared.
- A non-Control helper receives the relevant owner context explicitly.

### 5.11 Old virtual ordering is intentionally retired

The old guarantee that `MaterialControl.OnThemeChanged` runs before a derived
`OnAttachedToLogicalTree` or `OnPropertyChanged` is not retained.

The replacement contract is behavioral:

- Theme application completes synchronously by the end of the effective property notification.
- Theme application completes by the end of logical attachment.
- The resolved theme is applied before the first meaningful measure/render.
- Theme-dependent logic belongs in `IMaterialThemeConsumer`, not in an override watching
  `ResolvedThemeProperty`.
- Component-local property overrides may read the already committed context theme and reapply
  tokens as described in section 5.4.

Do not add a global `AddClassHandler<Control>` to simulate the old ordering. Avalonia class handlers
still run after the virtual property callback, and global dispatch would involve unrelated controls.

## 6. Proposed source layout

```text
src/Material3Expressive/Controls/Runtime/
  MaterialElementContext.cs
  MaterialElementContracts.cs
  MaterialThemeState.cs
  Motion/
    MaterialMotionCoordinator.cs
    MaterialMotionScope.cs
  Interaction/
    MaterialInteractionController.cs
    MaterialInteractionPolicy.cs
    MaterialInteractionGeometry.cs
    MaterialInteractionPresentationLease.cs
    MaterialInteractionObserverLease.cs
    MaterialInteractionDebugSnapshot.cs
```

Keep pure types where they currently belong:

- Theme transport and immutable resolved theme types remain under `Foundation/Theming`.
- Animation, spring, easing, and shape primitives remain under `Foundation`.
- `InteractionState` and pure state-layer token calculations remain under `Foundation/State`.
- Move `MaterialInteractionHost.ComposeOpacity` to a focused pure rendering utility rather than
  retaining a static dependency on the deleted host.
- Move animated-shape creation/registration to a motion scope or focused motion helper.

## 7. Context lifecycle matrix

| Trigger | Context action | Required guarantee |
|---|---|---|
| First `GetOrCreate`, owner detached | Publish AP, activate subscriptions, no component callback | Constructor-safe defaults |
| First `GetOrCreate`, owner already attached | Publish AP, activate, publish current theme once | No missed template-created owner context |
| Resolved theme or actual variant changes | Commit, deduplicate, dispatch owner then interaction | Callback reads new state |
| Component Variant/Size/Error/Enabled changes | Component calls `ApplyTokens(context.Theme)` | No fake theme event |
| Logical attach | Resolve current effective theme | Theme ready before layout/render |
| Inheritance parent removal | Publish effective fallback including Default | No unreliable attach-state heuristic |
| Logical detach | Clear input-derived interaction, settle coordinator | No stale press/hover/focus |
| Visual attach | Retry coordinator start | Pre-TopLevel work starts |
| Visual detach | Cancel epoch, settle coordinator | No animation persists across visual gap |
| Reparent/reattach | Reuse context and resolve new scope | No duplicate subscriptions |
| Template presenter detach | Dispose observer, presentation, and presenter motion leases | No old template retention |

## 8. Tests and intentional test seams

### 8.1 Context tests

- One context per owner under repeated and reentrant `GetOrCreate`.
- Context property is private, non-inherited, and never copied to a child.
- No theme/motion/interaction callback during ordinary control construction.
- First attach publishes exactly once even when the result is `ResolvedTheme.Default`, false.
- Context created on an already attached owner publishes immediately after AP publication.
- Theme reference plus dark-state deduplication.
- Theme changes raised during a theme callback are queued rather than recursively dispatched.
- Detach publishes the effective Default fallback.
- Reparent resolves the new scope while retaining the same context instance.
- Detach/reattach does not add duplicate subscriptions.
- Motion and interaction remain unallocated until requested.
- UI-thread access is enforced.

### 8.2 Component token-input tests

Retain or add representative tests proving that token recomputation happens for:

- Variant changes, including `MaterialLoadingIndicator`.
- Size and Label changes, including FAB-like controls.
- IsError and effective-enabled changes, including text/dropdown inputs.
- Checked/selected changes where token colors or shapes depend on state.
- Theme changes after one of the local properties has already changed.

The migration audit must inspect every existing `ApplyTokens()` call and every direct invocation of
`OnThemeChanged(...)`; do not infer coverage from watcher removal alone.

### 8.3 Motion tests

- One coordinator per owner.
- Component, interaction, and presenter scopes share one clock.
- Scope-local settle/dispose does not affect sibling scopes.
- `MotionTestClockScope` advances coordinators, not individual scopes.
- Exact manual-clock delta and negative-time rejection.
- Animation created or retargeted in a frame consumer schedules the next frame.
- Mutation during tick is safe.
- Visual detach settles and invalidates the old callback generation.
- A stale callback cannot advance a reattached owner.
- Lifecycle settle produces the expected final synchronization callback.

Rename test terminology from GroupCount to CoordinatorCount. If scope cardinality is relevant, expose
an internal diagnostic count separately; do not give scopes independent clocks.

### 8.4 Interaction tests

- One controller per owner, including stock Avalonia controls created through a template presenter.
- `SetInputEnabled(false)` clears only input-derived state and ripples.
- External Dragged state is not accidentally treated as physical input state.
- Logical detach clears stale Pressed/Hovered/Focused state.
- Theme changes recalculate focus/state-layer presentation without a second watcher.
- Owner consumer dispatch precedes secondary observers.
- Presentation geometry can change every frame and is used for the next press.
- Disposed presentation/observer leases receive no further callbacks.
- Template replacement does not retain old geometry, observers, or motion tracks.
- Keyboard activation uses the one context-owned controller; tests must not construct a second host.

### 8.5 Replace reflection with deliberate internal diagnostics

Final tests must not depend on `_interaction`, `_motion`, `_ripples`, or other private field names.

Use friend-assembly access to:

- `MaterialElementContext.TryGet`
- `MotionIfCreated`
- `InteractionIfCreated`
- an immutable `MaterialInteractionDebugSnapshot` when a ripple count/center cannot be asserted
  cleanly through output behavior

Do not retain the controller's old `Motion` property or `_ripples` field solely for tests.

### 8.6 Visual test infrastructure must migrate before the first production motion owner

`VisualMotionSettler` currently discovers private `MotionGroup` and `MaterialInteractionHost` fields
through reflection. Before any production owner switches to the new coordinator:

- Teach the test bridge to discover `MaterialElementContext` and its created coordinator.
- During the temporary mixed migration, continue discovering legacy groups as well.
- Remove the legacy reflection branch immediately after the last legacy group is deleted.
- Keep `MotionTestClockScope` capable of controlling both implementations only for the migration
  window; its final implementation controls coordinators only.

Update reflection-based unit tests in the same commit as their owning component. Replace the old
theme-ordering probes only when `MaterialControl` stops providing that contract.

## 9. Migration rules

The migration unit is an actual owner, not a subsystem or source file.

For each owner, one change must replace all of its old runtime ownership:

```text
MaterialThemeWatcher
+ direct MotionGroup
+ MaterialInteractionHost / MaterialInteraction cache
    -> one cached MaterialElementContext
```

Rules:

- Never leave one owner with both a legacy host/group and a new context controller/coordinator.
- Temporary coexistence is allowed only across different owners during implementation.
- Do not create a compatibility wrapper in which the new runtime delegates to the old runtime or
  vice versa.
- Update deterministic and visual tests for the owner in the same slice.
- Re-run the source inventory after every cohort.
- Keep unrelated user/worktree changes untouched.

## 10. Phased implementation plan

### Phase 0 — Freeze baseline and inventory

- [ ] Re-run the five inventory searches in section 3 and save the owner list in the implementation
      notes/commit description.
- [ ] Confirm all `MaterialControl` references are inheritance, tests, or documentation.
- [ ] Run the current solution build and core tests.
- [ ] Run the current non-parity visual tests.
- [ ] Record visual-parity environment prerequisites separately from visual mismatch results.
- [ ] Record the current public API types to be deleted.

Exit condition: the starting behavior and complete migration surface are known.

### Phase 1 — Add context and theme runtime contracts

- [ ] Add `Controls/Runtime` and the internal capability interfaces.
- [ ] Add the private non-inherited context attached property.
- [ ] Implement two-phase publication and idempotent activation.
- [ ] Implement theme resolution, first-publication sentinel, deduplication, and queued reentrancy.
- [ ] Preserve effective Default fallback during detach.
- [ ] Add context-only probe controls and all section 8.1 tests.
- [ ] Add representative dual-input token tests proving theme and local properties call the same
      component resolver.

Do not migrate a production owner yet, and do not modify `MaterialControl` to wrap the context.

Exit condition: the new theme runtime is independently correct while legacy production behavior is
unchanged.

### Phase 2 — Add motion coordinator and migrate the test clock/settler first

- [ ] Implement one coordinator with disposable scopes.
- [ ] Implement post-consumer running-state recheck.
- [ ] Implement epoch cancellation, visual-detach settle, and visual-attach retry.
- [ ] Register coordinators with `MotionTestClockScope`.
- [ ] Update `VisualMotionSettler` to discover both context coordinators and legacy groups during the
      migration window.
- [ ] Rewrite MotionGroup-centric unit tests around coordinator and scope invariants.
- [ ] Add the snackbar-style callback-starts-new-animation regression test.

Exit condition: the first production coordinator can be introduced without a silent visual-test
clock gap.

### Phase 3 — Add interaction controller and live presentation leases

- [ ] Implement context-only controller creation.
- [ ] Implement `SetInputEnabled`, `SetExternalState`, and indication policy updates.
- [ ] Implement input-derived detach cleanup.
- [ ] Implement owner direct dispatch and disposable secondary observers.
- [ ] Implement presentation leases with per-frame `UpdateGeometry`.
- [ ] Implement default owner-bounds geometry and last-geometry retention.
- [ ] Implement the interaction motion scope on the owner's coordinator.
- [ ] Add intentional internal debug snapshots for tests that require ripple internals.

Exit condition: the new controller is behavior-complete without using the old host, watcher, or
group.

### Phase 4 — Migrate native-base theme-only owners

For each owner that inherits a stock Avalonia base and has no motion/interaction ownership:

- [ ] Replace the watcher field with one cached context.
- [ ] Implement `IMaterialThemeConsumer` explicitly.
- [ ] Preserve every local-property `ApplyTokens` trigger.
- [ ] Keep presenter theme resolution on the presenter itself.
- [ ] Update its tests in the same change.
- [ ] Verify no legacy runtime remains on that owner.

Exit condition: the selected owners have exactly one context and no watcher.

### Phase 5 — Migrate native-base motion and interaction owners atomically

For every stock-base owner with any combination of watcher, group, and host:

- [ ] Replace all of those fields in one owner-level change.
- [ ] Register component animations in the component scope.
- [ ] Route interaction animations through the controller's sibling scope.
- [ ] Convert frame handlers to `IMaterialMotionConsumer` where owner-lifetime dispatch is needed.
- [ ] Convert short-lived observers to disposable leases.
- [ ] Replace private-field reflection tests at the same time.
- [ ] Preserve all local Variant/Size/Error/Enabled/Checked token paths.

Special audits in this phase:

- [ ] `MaterialListItem`: dynamic input gate and externally-owned Dragged state.
- [ ] `MaterialSwitch`: per-frame moving interaction area and stale Pressed cleanup.
- [ ] Checkbox/RadioButton/Slider/RangeSlider/NavigationItem: render-time geometry updates.
- [ ] Snackbar transitions: animation started from a frame consumer.
- [ ] Composite/scaffold controls: helpers receive the owner's context/scope rather than creating
      their own runtime for the same owner.

Exit condition: migrated native-base owners have one context, one coordinator at most, and one
controller at most.

### Phase 6 — Migrate stock-control template presenters

- [ ] Migrate `MaterialRipplePresenter`.
- [ ] Migrate Button, ToggleButton, and IconButton container presenters.
- [ ] Give every theme-consuming presenter its own context.
- [ ] Borrow only the templated owner's interaction through observer/presentation leases.
- [ ] Update geometry on Render/bounds changes and dispose leases on detach/template replacement.
- [ ] Prove a stock Button and all of its Material presenters resolve one owner controller.
- [ ] Rewrite the test that currently constructs a second host on an already templated Button so it
      retrieves that one context-owned controller.
- [ ] Update visual cases that settle motion through presenter-owned or owner-owned animations.

Exit condition: no presenter calls `MaterialInteraction.GetOrCreate` and template replacement leaves
no old observer or geometry.

### Phase 7 — Migrate all 17 `MaterialControl` subclasses

For each listed control in section 3:

- [ ] Change the base class to `TemplatedControl`.
- [ ] Acquire one context.
- [ ] Replace `OnThemeChanged` with explicit `IMaterialThemeConsumer` implementation.
- [ ] Replace protected `ResolvedTheme`, `IsDarkVariant`, and `Spring` reads with context state.
- [ ] Replace protected `Motion` and `OnMotionTick` with the component scope and
      `IMaterialMotionConsumer` as appropriate.
- [ ] Preserve local token triggers, especially `MaterialLoadingIndicator.Variant`.
- [ ] Remove lifecycle overrides whose only purpose was old base behavior.
- [ ] Fully migrate any interaction owned by the same control in the same change.
- [ ] Update focused tests and visual cases.

In the same phase:

- [ ] Replace `ThemeProbe` with a `TemplatedControl + IMaterialThemeConsumer` probe.
- [ ] Delete the two old virtual-ordering assertions.
- [ ] Add/retain tests for theme-before-first-measure/render, live changes, and reparenting.

Exit condition: no production class inherits `MaterialControl`.

### Phase 8 — Close inventory and delete the legacy runtime

- [ ] Re-run all construction/inheritance searches.
- [ ] Migrate any nested or late-created helper missed by the owner cohorts.
- [ ] Move `ComposeOpacity` and other pure helpers out of `MaterialInteractionHost`.
- [ ] Move animated-shape registration to the motion scope/helper.
- [ ] Delete `MaterialControl.cs`.
- [ ] Delete `MaterialThemeWatcher.cs`.
- [ ] Delete `MaterialInteractionHost.cs`.
- [ ] Delete `MaterialInteraction.cs`.
- [ ] Delete `MotionGroup.cs` after all production/test consumers use the coordinator.
- [ ] Remove the transitional legacy branch from `VisualMotionSettler` and
      `MotionTestClockScope`.
- [ ] Remove stale usings, XML docs, comments, and active architecture documentation.
- [ ] Confirm no obsolete alias or forwarding type remains.

Exit condition: only the new runtime architecture exists.

### Phase 9 — Public API, versioning, documentation, and full validation

- [ ] Document deletion of the four public types: `MaterialControl`, `MaterialThemeWatcher`,
      `MaterialInteractionHost`, and `MotionGroup`.
- [ ] Document the new direct Avalonia base type for every affected public control.
- [ ] Add a breaking-change/migration note; do not offer a compatibility shim.
- [ ] Prefer version `0.2.0-alpha` for the incompatible runtime line.
- [ ] Account for the CI rule requiring root-owned packages to share one base version, or explicitly
      revise that versioning policy before changing only the main package version.
- [ ] Update active motion and visual-parity documentation from MotionGroup terminology to
      coordinator/scope terminology.
- [ ] Run all validation in section 12.

Exit condition: implementation, public API, package version, tests, and documentation all describe
the same final architecture.

## 11. Final source and architecture gates

Production gates:

```bash
rg -n 'class\s+\w+\s*:\s*MaterialControl\b' src/Material3Expressive
rg -n 'new\s+MaterialThemeWatcher\(' src/Material3Expressive
rg -n 'new\s+MaterialInteractionHost\(' src/Material3Expressive
rg -n 'MaterialInteraction\.GetOrCreate' src/Material3Expressive
rg -n 'new\s+MotionGroup\(' src/Material3Expressive
```

All must return zero.

Constructor ownership gates:

```bash
rg -n 'new\s+MaterialMotionCoordinator\(' src/Material3Expressive
rg -n 'new\s+MaterialInteractionController\(' src/Material3Expressive
```

Each constructor must appear only in `MaterialElementContext`.

Stale-name cleanup:

```bash
rg -n '\bMaterialControl\b|\bMaterialThemeWatcher\b|\bMaterialInteractionHost\b|\bMotionGroup\b' \
  src/Material3Expressive tests/Material3Expressive.Tests tests/Material3Expressive.VisualTesting
```

Any remaining match must be removed or be an explicitly retained historical archive reference
outside active source/tests.

Reflection-debt gate:

```bash
rg -n 'GetField\("_(interaction|motion|ripples)"|PrivateField\([^,]+,\s*"_(interaction|motion|ripples)"' tests
```

This must return zero.

## 12. Definition of done and validation

The refactor is complete only when all of the following hold:

- [ ] Every production Material owner uses at most one context.
- [ ] No owner uses both legacy and new runtime objects.
- [ ] Theme transport still uses only inherited `ResolvedThemeProperty` plus actual theme variant.
- [ ] First Default-theme resolution publishes exactly once.
- [ ] Theme and every component-local token input produce correct current tokens.
- [ ] Context is never inherited or recreated on ordinary detach/reattach.
- [ ] One coordinator drives all motion scopes for an owner.
- [ ] Frame consumers can start new animations without starving the next frame.
- [ ] Visual detach cancels stale callbacks and settles motion.
- [ ] One controller owns physical interaction for an owner.
- [ ] Input disable/detach clears physical state without corrupting external state.
- [ ] Presentation geometry follows animated controls frame by frame.
- [ ] Template replacement leaves no stale observers, geometry, or motion tracks.
- [ ] No final production or test code depends on private runtime field names.
- [ ] All five legacy files/types are deleted with no shim.
- [ ] Active documentation and package version describe the breaking architecture.

Run at minimum:

```bash
dotnet build Material3Expressive.slnx --no-restore -v:minimal
dotnet test tests/Material3Expressive.TokenGenerator.Tests/Material3Expressive.TokenGenerator.Tests.csproj --no-restore
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj --no-restore
```

Also run the repository's Compose-versus-Avalonia visual-parity workflow when its runner and pinned
font prerequisites are available. Infrastructure failure before a comparison report is not a visual
verdict.

No phase is complete merely because it compiles. Its focused behavior tests, relevant visual cases,
inventory update, and owner-level no-dual-runtime check must pass before moving to the next cohort.
