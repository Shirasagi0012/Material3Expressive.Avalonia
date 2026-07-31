# Compose Material3 component coverage

This inventory compares the Avalonia library with the
[implementation source baseline](../reference/upstream-baselines.md). It groups related Kotlin overloads,
`Defaults` objects, and state factories into user-facing feature families; it is not a count of
individual Compose functions.

Last audited: 2026-07-31 against AndroidX
`ff0a0a99c4f0cfe95d75d5df615e51d399575922` / Material3 `1.5.0-alpha25`.

## Status model

| Status | Meaning |
|---|---|
| Implemented | A usable Avalonia counterpart covers the primary feature shape. This does not by itself satisfy the roadmap definition of complete. |
| Partial | A counterpart exists, but a meaningful Compose variant, state model, interaction, or container API is absent. |
| Missing | No public counterpart currently exists. |

Strict completion additionally requires token provenance, API review, visual and behavioral
coverage, desktop interaction, accessibility, tests, and documentation. Those delivery dimensions
are defined in [`ROADMAP.md`](../../ROADMAP.md).

## Summary

| Status | Feature families | Share |
|---|---:|---:|
| Implemented | 22 | 44% |
| Partial | 19 | 38% |
| Missing | 9 | 18% |
| Total | 50 | 100% |

The theming, color, typography, shape, motion, ripple, token-generation, and Material Symbols
foundations are outside this component count. They are shared infrastructure rather than
one-to-one component families.

## Baseline-audit status

The [alpha22→alpha25 difference audit](../reference/audits/alpha22-alpha25-difference-audit.md) confirms that every
accepted executable parity case is alpha22 conformance evidence only; none is an implicit alpha25
parity pass. The accepted alpha22-compatible `MaterialListItem` click/selection cases likewise do
not prove alpha25's text-line-height behavior or its modern no-interaction overload. Those paths
remain parity unavailable and use source review plus deterministic Avalonia layout/input tests.
Alpha25-specific ToggleButton border transitions and Slider/RangeSlider state and
keyboard-completion behavior are recorded the same way until the executable reference can
represent them.

## Component matrix

| Area | Compose feature family | Avalonia counterpart | Status | Principal gap or adaptation |
|---|---|---|---|---|
| Buttons | Button variants and sizes | `MaterialButton` | Implemented | Compose composables are represented by `Variant` and `Size`; the alpha25 `textStyleFor` mapping is explicit: ExtraSmall/Small → LabelLarge, Medium → TitleMedium, Large → HeadlineSmall, ExtraLarge → HeadlineLarge. The executable alpha22 large-button scene passes. |
| Buttons | ToggleButton variants | `MaterialToggleButton` | Implemented | Variants are consolidated into one XAML control. |
| Buttons | IconButton variants and sizes | `MaterialIconButton` | Implemented | Width and size are Avalonia properties. |
| Buttons | IconToggleButton variants | `MaterialToggleIconButton` | Implemented | Variants are consolidated into one control. |
| Buttons | ButtonGroup and connected group | `MaterialButtonGroup` | Implemented | Public API follows panel/content conventions. |
| Buttons | SplitButton | `MaterialSplitButton` | Implemented | Leading and trailing segments are hosted controls. |
| Containers | Card, ElevatedCard, OutlinedCard | `MaterialCard` | Implemented | Compose variants map to `CardVariant`. |
| Actions | FloatingActionButton and extended/size variants | `MaterialFab` | Implemented | Label presence selects extended presentation. |
| Actions | FloatingActionButtonMenu | `MaterialFabMenu` | Partial | Compose menu-item and toggle APIs are not fully represented. |
| Actions | Horizontal/Vertical FloatingToolbar | `MaterialFloatingToolbar`, `FloatingToolbarScrollState` | Implemented | One D011 XAML items control adapts both axes and with/without-FAB overloads through Orientation/Auto, leading/trailing/FAB slots, logical FAB position, FastSpatial collapse, 56–80dp FAB sizing, non-null/reset ExitAlways state, ScrollViewer/custom-delta coordination, host-owned 16dp anchoring, desktop arrow/Home/End focus, non-trapping Tab, hidden-focus removal, and ToolBar/ExpandCollapse automation. Two alpha22-compatible horizontal/vertical scenes are exact with zero raster difference. |
| Selection | Checkbox and TriStateCheckbox | `MaterialCheckbox` | Implemented | Use `IsThreeState="True"` with `IsChecked="{x:Null}"` for Compose `ToggleableState.Indeterminate`; selected and indeterminate alpha22 parity cases pass. |
| Selection | RadioButton | `MaterialRadioButton` | Implemented | Uses Avalonia radio grouping behavior. |
| Selection | Switch | `MaterialSwitch` | Implemented | Desktop control derives from `ToggleButton`; drag-to-toggle is not provided. |
| Selection | Slider, VerticalSlider, RangeSlider | `MaterialSlider`, `MaterialRangeSlider` | Implemented | `MaterialSlider.Orientation="Vertical"` adapts Compose top-to-bottom VerticalSlider. `MaterialRangeSlider` provides ordered bindable endpoints, non-trapping Start/End Tab traversal, endpoint focus indication, RTL input, and a Group with two stable Slider/RangeValue automation children. Alpha22 light/dark/disabled static range parity passes; alpha25 state/Saver and keyboard-completion behavior remains accurately parity unavailable and is source/test covered. |
| Progress | LinearProgressIndicator | `MaterialLinearProgressIndicator` | Implemented | Determinate and indeterminate forms use one ProgressBar automation node; determinate exposes read-only RangeValue `0..1`, while indeterminate exposes status without a fabricated value. |
| Progress | CircularProgressIndicator | `MaterialCircularProgressIndicator` | Implemented | Determinate and indeterminate forms use the same D004 ProgressBar automation contract as linear progress. |
| Progress | Linear/CircularWavyProgressIndicator | `MaterialLinearProgressIndicator.IsWavy`; `MaterialCircularProgressIndicator.IsWavy` / `WaveSpeed` | Implemented | Circular wavy follows alpha25 `WavyProgressIndicator.kt` and `internal/CircularWavyProgressModifiers.kt`, including the 6000ms indeterminate sweep/rotation cycle. D004 automation is complete and seven determinate/dark/indeterminate/motion alpha22 rows have exact structure/timelines. DEF-001 explicitly defers their unchanged expected-edge-only complex-shape renderer residual. |
| Progress | LoadingIndicator and contained form | `MaterialLoadingIndicator` | Implemented | Compose overloads map to variants/properties. Source review: alpha25 `LoadingIndicator.kt` uses a 650ms `spring(.6, 200, visibilityThreshold=.1)` for each indeterminate morph and a separate 4666ms linear global rotation; deterministic unit tests cover the curve and interval boundary. No executable alpha22 animated-parity claim is made. |
| Inputs | Assist, Filter, Input, and Suggestion chips | `MaterialChip` | Partial | Families are consolidated; some family-specific semantics and defaults are absent. |
| Inputs | TextField | `MaterialTextField` | Implemented | Filled form is selected by `Variant`; native Avalonia editing, selection, IME, undo, scrolling, and caret position are retained while the visual cursor follows CMP Desktop's complete 1dp line rectangle and native blink interval. Focused-empty parity is accepted with zero warnings or rejected pixels. |
| Inputs | OutlinedTextField | `MaterialTextField` | Implemented | Outlined form is selected by `Variant`; its floating-label notch and the native-positioned complete 1dp desktop cursor pass the supported focused-empty comparison contract with zero warnings or rejected pixels. |
| Inputs | SecureTextField and OutlinedSecureTextField | `MaterialSecureTextField` | Implemented | Filled/outlined secure editing reuses the native single-line editor with password/sensitive input hints, cut/copy and incoming-drop suppression, customizable masking, explicit visible/hidden state, scalar restoration, validation chrome, and non-leaking Edit automation. Five focused tests pass. Alpha25 `RevealLastTyped` safely falls back to desktop-hidden because Avalonia can only reveal the complete value; two alpha22-compatible hidden/visible cases pass twice reproducibly as `semanticRasterEquivalent` with exact component/label contracts and zero rejected pixels. |
| Inputs | ScrollField | None | Missing | Option state, scrolling, and item API are absent. |
| Search | SearchBar family | `MaterialSearchBar` | Partial | Docked/full-screen/top variants and scroll behaviors are incomplete. |
| Menus | DropdownMenu and menu items | Avalonia menu styles | Partial | This is a restyle of Avalonia menus, not a full Compose-style state API. |
| Menus | ExposedDropdownMenu | `MaterialExposedDropdownMenu` | Implemented | Adapts Avalonia `ComboBox` so inherited `IsDropDownOpen`, `SelectedItem`/`SelectedIndex`, editable `Text`, keyboard selection, light-dismiss popup, and ComboBox automation remain canonical. Material adds filled/outlined anchors, a coordinated rotating icon and menu entrance, anchor-width or 112–280dp sizing, the AndroidX 48dp window margin, edge flipping, editable-anchor focus retention, and focus return. Seven deterministic tests cover interaction, geometry, accessibility, and motion; supported alpha22 parity evidence is recorded in the task file. |
| Lists | ListItem and SegmentedListItem | `MaterialListItem`, `MaterialSegmentedList` | Implemented | Standard one-, two-, and three-line rows expose slots plus unified `InteractionMode`; direct and generated string slots receive Compose typography. Interactive modes uniformly support long-click; `None` has deterministic input/automation evidence. The segmented container infers single/first/middle/last positions from visible direct rows, applies alpha25 segmented colours and outer-corner shapes, and physical-pixel-rounds the 2dp gap without owning selection. Thirteen alpha22-compatible cases pass with no warnings, including two segmented light/dark cases; dragged rows additionally require the exact 16dp/8dp shadow contract. Alpha25 line-aware height and modern None executable parity remain unavailable, while long-click action semantics use deterministic tests because a static screenshot cannot observe invocation. |
| Selection | SegmentedButton rows | `MaterialSegmentedButton`, `MaterialSegmentedButtonRow` | Implemented | One XAML row adapts Compose single/multi scope APIs through `SelectionMode`; visible direct children receive equal physical-pixel widths, 1dp outline overlap, inferred outer shapes, and checked-state z-order. `IsChecked`, optional `Icon`, `SelectionIcon` geometry, `ShowSelectionIcon`, padding/size, and shape override remain bindable. Single mode exposes radio/selection automation and RTL-aware arrow/Home/End navigation; multiple mode exposes independent toggle automation. Thirteen deterministic tests pass, and four alpha22-compatible light/dark/disabled/single/multiple cases are `semanticRasterEquivalent` with zero warnings or rejected pixels. |
| Navigation | Tab and TabRow families | `MaterialTab`, `MaterialTabs` | Partial | `IsScrollable` adapts `PrimaryScrollableTabRow`: 52dp edges, 90dp minimum intrinsic tabs, and physical-pixel selected-tab centring. Alpha22 executable parity passes at 240dp and 600dp. Complete indicator/layout variants, icon permutations, and richer accessibility/keyboard coverage remain. |
| Navigation | NavigationBar | `MaterialNavigationBar` | Implemented | Bar and rail share one Avalonia control. |
| Navigation | NavigationRail | `MaterialNavigationBar` | Implemented | Selected through the navigation variant. |
| Navigation | ShortNavigationBar and WideNavigationRail | `MaterialShortNavigationBar`, `MaterialWideNavigationRail`, `MaterialNavigationItem` | Implemented | ShortNavigationBar is complete under D012; its corrected alpha25 layout is parity-unavailable because alpha22 measures each item at full width. WideNavigationRail is complete under D013 with retained items/selection, 96dp collapsed and intrinsic 220–360dp expanded layouts, reversible expansion, Header/Top-Center-Bottom arrangement, vertical desktop navigation, Tab/TabItem selection plus ExpandCollapse automation, and five accepted alpha22 static parity states. Cross-component breakpoint coordination is supplied by `MaterialNavigationSuiteScaffold`. |
| Navigation | Adaptive navigation suite | `MaterialNavigationSuiteScaffold`, `NavigationSuiteLayoutType` | Implemented | D014 keeps one destination collection/selection across 600×480dp Auto resolution to compact/medium short bar or collapsed wide rail, plus explicit expanded/none layouts. Body/primary-action placement, show-hide motion, RTL, focus and automation identity retention are deterministic-test covered. The separate alpha22 adaptive artifact and its broken ShortNavigationBar endpoint are unavailable; two light/dark wide endpoint composition cases pass with zero rejected pixels. |
| Navigation | Modal, dismissible, and permanent drawers | `MaterialNavigationDrawer` | Partial | Compose state and sheet variants are not fully represented. |
| Navigation | Top/Bottom/Flexible AppBar families | `MaterialAppBar` | Partial | Standard and alpha25 flexible Small/Medium/Large top app bars provide non-null/reset external state, Pinned/EnterAlways/ExitUntilCollapsed behavior, `ScrollViewer` and logical-delta coordination, controlled title transitions, and `IExpandCollapseProvider`; 15 focused tests pass, and the supported alpha22 standard-Medium expanded endpoint passes three times reproducibly with exact probes and zero rejected pixels. Flexible variants and alpha25 collapsed color are reference-version unavailable; `BottomAppBar`/`FlexibleBottomAppBar` remain absent. |
| Layout | Scaffold | `MaterialScaffold` | Implemented | Five slots coordinate body, top/bottom bars, external snackbar host, and FAB. Read-only bindable `ContentPadding` preserves Compose's caller-applies-padding contract; null `ContentWindowInsets` observes TopLevel safe area and explicit `Thickness` overrides it. Start/Center/End/EndOverlay, RTL, physical-pixel placement, alpha25 z-order, and neutral Pane automation are deterministic-test covered. Two alpha22-compatible light/dark cases are `exactMatch` with zero warnings/rejected pixels. Snackbar queue/timeouts remain the separate host-state row below. |
| Overlays | AlertDialog and BasicAlertDialog | `MaterialDialog` | Partial | General overlay exists; typed confirm/dismiss and Compose dialog contracts are incomplete. |
| Overlays | BottomSheet and ModalBottomSheet | `MaterialBottomSheet` | Partial | Core open/drag behavior exists; state and modality variants are incomplete. |
| Overlays | BottomSheetScaffold | `MaterialBottomSheetScaffold` | Implemented | Standard persistent scaffold provides a per-instance non-null/reset `BottomSheetState`, application-owned replacement and recoverable initial anchors, vetoable expand/partial/hide commands, 56dp peek, 640dp max width, delayed pointer capture, precision-wheel and nested `ScrollViewer` handoff, interrupted drag settling, exact top/body/snackbar/sheet placement, and `IExpandCollapseProvider`. Six alpha22-compatible static/motion cases have exact geometry, text, shadow, and timeline contracts and pass twice reproducibly as `semanticRasterEquivalent` with zero rejected pixels. Snackbar queue, timeout, cancellation, restoration, and accessibility ownership remain with the separately completed host. |
| Overlays | Snackbar and SnackbarHost | `MaterialSnackbar`, `MaterialSnackbarHost`, `SnackbarHostState` | Implemented | Alpha25 FIFO queue/result/cancellation behavior, non-null/reset state, generated message/action content, Polite Pane/Invoke automation, complete unclipped Level3 shadow damage, and FastEffects/FastSpatial appearance/disappearance motion are implemented with nine deterministic tests. Eight alpha22-compatible settled and short-timeout checkpoints pass reproducibly with transformed geometry, text, shadow, and timeline contracts. Platform accessibility recommended-timeout adaptation is explicitly Deferred by `DEF-002`; applications can select `Indefinite` when content must remain available. |
| Overlays | Plain/Rich Tooltip and TooltipBox | `MaterialTooltip` | Partial | Position-provider and full state behavior are not exposed. |
| Indicators | Badge and BadgedBox | `MaterialBadge` | Implemented | Composition uses ordinary Avalonia layout rather than `BadgedBox`. |
| Pickers | DatePicker and DatePickerDialog | `MaterialDatePicker`, `MaterialDatePickerState` | Partial | Calendar selection and non-null/reset shared state integrate with `MaterialDateInput` and generic `MaterialDialog`; month navigation controls and a specialized dialog wrapper remain absent. |
| Pickers | DateRangePicker | `MaterialDateRangePickerState` | Partial | Ordered, atomically replaceable range state and typed range input exist; graphical range-calendar interaction remains absent. |
| Pickers | DateInput and DateRangeInput | `MaterialDateInput`, `MaterialDateRangeInput` | Implemented | Exact culture/pattern formatting, min/max and ordered-range validation, native focus, Enter/focus-loss commit, Escape cancellation, state restoration/replacement, and dialog dismissal routing are deterministic-test covered. The populated date-input surface passes supported static parity exactly and reproducibly. |
| Pickers | TimePicker, TimeInput, and dialogs | `MaterialTimePicker`, `MaterialTimeInput`, `MaterialTimePickerState` | Partial | Clock dial and 12/24-hour typed input share non-null/reset state; field validation, auto-advance, commit/cancel, restoration, and generic dialog composition are covered. Rich time picker/scroll variants and a specialized dialog wrapper remain absent. |
| Collections | Carousel families | `MaterialCarousel` | Partial | Compose keylines, snap/fling behavior, state, and multi-aspect API are incomplete. |
| Gestures | SwipeToDismissBox | `MaterialSwipeToDismissBox`, `SwipeToDismissBoxState` | Implemented | Three logical anchors, application-restored initial values, non-null/reset external state, direction gating, 56dp position and 125dp/s velocity thresholds, RTL physical reversal, foreground overflow, one-shot dismissal notification, 300ms FastOutSlowIn anchored motion, and mid-flight reset/replacement are implemented. Delete/Shift+Delete/Escape and ListItem/Invoke automation are explicit D003 desktop adaptations. Eleven deterministic tests cover pointer, keyboard, restoration, interruption, layout, and accessibility. Three alpha22-compatible motion checkpoints pass through the existing `component-with-text` profile with exact transformed geometry/timeline contracts and zero warnings. |
| Gestures | PullToRefresh | `MaterialPullToRefreshBox`, `PullToRefreshState` | Implemented | Non-null/reset/restorable progress state, application-owned `IsRefreshing`, strict 80dp threshold over half-distance input, capped nonlinear overshoot, upward pre-scroll retraction, child-at-top downward ownership, fling consumption, interruption, and top-only indicator clipping are implemented. F5/Ctrl+R/Escape and Pane/Invoke/read-only RangeValue are explicit D003 desktop adaptations. Twelve deterministic tests cover thresholds, nested ownership, restoration/replacement, interruption, keyboard, automation, layout, and Level2 default elevation. Three alpha22-compatible controlled retract checkpoints pass through an unchanged existing profile. |
| Desktop | Material scrollbar | None | Missing | No Material non-interactive scrollbar counterpart. |
| Primitives | Surface, Divider, and Label | `MaterialSurface`, `MaterialDivider` | Partial | Public Surface tonal/shadow elevation and horizontal/vertical Divider are available. `MaterialDivider.IsHairline` is the explicit `Dp.Hairline` adaptation: a zero-layout cross axis with a one-physical-pixel retained stroke; `Thickness=0` remains no line. Alpha22 default/custom/horizontal/vertical/hairline parity passes. The separate Material label primitive remains absent, so this grouped family is not complete. |

## Interpretation notes

- One Avalonia control may intentionally represent several Compose composables when their
  differences are token-driven. This is an API adaptation, not automatically a coverage gap.
- A stock Avalonia control restyle counts as partial when Compose exposes meaningful Material
  state or layout behavior that the style cannot represent.
- Android-only integration is not added to this matrix unless the behavior has a meaningful
  Avalonia desktop equivalent.
- The executable alpha22 parity reference cannot render every alpha25 feature. That limitation is
  tracked separately from implementation status according to
  [`upstream-baselines.md`](../reference/upstream-baselines.md).
