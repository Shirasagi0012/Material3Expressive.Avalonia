# Compose Material3 component coverage

This inventory compares the Avalonia library with the
[implementation source baseline](../reference/upstream-baselines.md). It groups related Kotlin overloads,
`Defaults` objects, and state factories into user-facing feature families; it is not a count of
individual Compose functions.

Last audited: 2026-07-29 against AndroidX
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
| Implemented | 20 | 40% |
| Partial | 18 | 36% |
| Missing | 12 | 24% |
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
| Actions | Horizontal/Vertical FloatingToolbar | None | Missing | Includes toolbar state, scroll behavior, and FAB integration. |
| Selection | Checkbox and TriStateCheckbox | `MaterialCheckbox` | Implemented | Use `IsThreeState="True"` with `IsChecked="{x:Null}"` for Compose `ToggleableState.Indeterminate`; selected and indeterminate alpha22 parity cases pass. |
| Selection | RadioButton | `MaterialRadioButton` | Implemented | Uses Avalonia radio grouping behavior. |
| Selection | Switch | `MaterialSwitch` | Implemented | Desktop control derives from `ToggleButton`; drag-to-toggle is not provided. |
| Selection | Slider, VerticalSlider, RangeSlider | `MaterialSlider`, `MaterialRangeSlider` | Partial | `MaterialSlider.Orientation="Vertical"` adapts Compose top-to-bottom VerticalSlider; `MaterialRangeSlider` provides ordered, bindable `StartValue`/`EndValue`. Alpha22 static vertical/range parity passes; alpha25 state/Saver and keyboard-completion behavior is parity unavailable, and native automation peers for both range thumbs await the custom-control accessibility decision. |
| Progress | LinearProgressIndicator | `MaterialLinearProgressIndicator` | Implemented | Determinate and indeterminate forms exist. |
| Progress | CircularProgressIndicator | `MaterialCircularProgressIndicator` | Implemented | Determinate and indeterminate forms exist. |
| Progress | Linear/CircularWavyProgressIndicator | `MaterialLinearProgressIndicator.IsWavy`; `MaterialCircularProgressIndicator.IsWavy` / `WaveSpeed` | Partial | Circular wavy follows alpha25 `WavyProgressIndicator.kt` and `internal/CircularWavyProgressModifiers.kt`: determinate alpha22 parity emits a documented, expected-edge-only temporary complex-shape warning and default wave motion has a deterministic two-frame test. Dark/indeterminate parity and native progress automation semantics remain incomplete; strict closure is blocked on the custom-drawn accessibility decision. |
| Progress | LoadingIndicator and contained form | `MaterialLoadingIndicator` | Implemented | Compose overloads map to variants/properties. Source review: alpha25 `LoadingIndicator.kt` uses a 650ms `spring(.6, 200, visibilityThreshold=.1)` for each indeterminate morph and a separate 4666ms linear global rotation; deterministic unit tests cover the curve and interval boundary. No executable alpha22 animated-parity claim is made. |
| Inputs | Assist, Filter, Input, and Suggestion chips | `MaterialChip` | Partial | Families are consolidated; some family-specific semantics and defaults are absent. |
| Inputs | TextField | `MaterialTextField` | Implemented | Filled form is selected by `Variant`. |
| Inputs | OutlinedTextField | `MaterialTextField` | Implemented | Outlined form is selected by `Variant`. |
| Inputs | SecureTextField and OutlinedSecureTextField | None | Missing | Password-specific state and reveal behavior are absent. |
| Inputs | ScrollField | None | Missing | Option state, scrolling, and item API are absent. |
| Search | SearchBar family | `MaterialSearchBar` | Partial | Docked/full-screen/top variants and scroll behaviors are incomplete. |
| Menus | DropdownMenu and menu items | Avalonia menu styles | Partial | This is a restyle of Avalonia menus, not a full Compose-style state API. |
| Menus | ExposedDropdownMenu | None | Missing | Anchor box, trailing icon, and exposed-menu behavior are absent. |
| Lists | ListItem and SegmentedListItem | `MaterialListItem`, `MaterialSegmentedList` | Implemented | Standard one-, two-, and three-line rows expose slots plus unified `InteractionMode`; direct and generated string slots receive Compose typography. Interactive modes uniformly support long-click; `None` has deterministic input/automation evidence. The segmented container infers single/first/middle/last positions from visible direct rows, applies alpha25 segmented colours and outer-corner shapes, and physical-pixel-rounds the 2dp gap without owning selection. Thirteen alpha22-compatible cases pass with no warnings, including two segmented light/dark cases; dragged rows additionally require the exact 16dp/8dp shadow contract. Alpha25 line-aware height and modern None executable parity remain unavailable, while long-click action semantics use deterministic tests because a static screenshot cannot observe invocation. |
| Selection | SegmentedButton rows | `MaterialSegmentedButton`, `MaterialSegmentedButtonRow` | Implemented | One XAML row adapts Compose single/multi scope APIs through `SelectionMode`; visible direct children receive equal physical-pixel widths, 1dp outline overlap, inferred outer shapes, and checked-state z-order. `IsChecked`, optional `Icon`, `SelectionIcon` geometry, `ShowSelectionIcon`, padding/size, and shape override remain bindable. Single mode exposes radio/selection automation and RTL-aware arrow/Home/End navigation; multiple mode exposes independent toggle automation. Thirteen deterministic tests pass, and four alpha22-compatible light/dark/disabled/single/multiple cases are `semanticRasterEquivalent` with zero warnings or rejected pixels. |
| Navigation | Tab and TabRow families | `MaterialTab`, `MaterialTabs` | Partial | `IsScrollable` adapts `PrimaryScrollableTabRow`: 52dp edges, 90dp minimum intrinsic tabs, and physical-pixel selected-tab centring. Alpha22 executable parity passes at 240dp and 600dp. Complete indicator/layout variants, icon permutations, and richer accessibility/keyboard coverage remain. |
| Navigation | NavigationBar | `MaterialNavigationBar` | Implemented | Bar and rail share one Avalonia control. |
| Navigation | NavigationRail | `MaterialNavigationBar` | Implemented | Selected through the navigation variant. |
| Navigation | ShortNavigationBar and WideNavigationRail | None | Missing | Adaptive and expanded/collapsed navigation forms are absent. |
| Navigation | Modal, dismissible, and permanent drawers | `MaterialNavigationDrawer` | Partial | Compose state and sheet variants are not fully represented. |
| Navigation | Top/Bottom/Flexible AppBar families | `MaterialAppBar` | Partial | Flexible layouts, app-bar DSL, and scroll behaviors are incomplete. |
| Layout | Scaffold | `MaterialScaffold` | Implemented | Five slots coordinate body, top/bottom bars, external snackbar host, and FAB. Read-only bindable `ContentPadding` preserves Compose's caller-applies-padding contract; null `ContentWindowInsets` observes TopLevel safe area and explicit `Thickness` overrides it. Start/Center/End/EndOverlay, RTL, physical-pixel placement, alpha25 z-order, and neutral Pane automation are deterministic-test covered. Two alpha22-compatible light/dark cases are `exactMatch` with zero warnings/rejected pixels. Snackbar queue/timeouts remain the separate host-state row below. |
| Overlays | AlertDialog and BasicAlertDialog | `MaterialDialog` | Partial | General overlay exists; typed confirm/dismiss and Compose dialog contracts are incomplete. |
| Overlays | BottomSheet and ModalBottomSheet | `MaterialBottomSheet` | Partial | Core open/drag behavior exists; state and modality variants are incomplete. |
| Overlays | BottomSheetScaffold | `MaterialBottomSheetScaffold` | Partial | Standard two-anchor scaffold is available with owned-or-external `BottomSheetState`, vetoable expand/partial/hide commands, 56dp peek, 640dp max width, delayed pointer capture, precision-wheel and nested `ScrollViewer` handoff, interrupted drag settling, exact top/body/snackbar/sheet placement, and `IExpandCollapseProvider`. Six alpha22-compatible static/motion cases have exact geometry, text, shadow, and timeline contracts; partial dark is `semanticRasterEquivalent`, while five light cases truthfully remain raster `mismatch` only for the user-confirmed channel-delta-1 shadow framework parameter issue. Snackbar queue/restoration ownership remains with the separately planned host task. |
| Overlays | Snackbar and SnackbarHost | `MaterialSnackbar`, `MaterialSnackbarHost`, `SnackbarHostState` | Partial | Alpha25 queue/result/cancellation state core and non-null/reset state contract are implemented with six deterministic tests; host content, action/dismiss handling, and Pane/Invoke semantics are available. The alpha22-compatible settled message case is `semanticRasterEquivalent` with zero rejected pixels. Action/motion parity, a Catalog external-Scaffold-slot example, and AccessibilityManager recommended-timeout adaptation remain open; an invalid overlapping Compose action raster is explicitly excluded from evidence. |
| Overlays | Plain/Rich Tooltip and TooltipBox | `MaterialTooltip` | Partial | Position-provider and full state behavior are not exposed. |
| Indicators | Badge and BadgedBox | `MaterialBadge` | Implemented | Composition uses ordinary Avalonia layout rather than `BadgedBox`. |
| Pickers | DatePicker and DatePickerDialog | `MaterialDatePicker` | Partial | Calendar selection exists; dialog/state/formatter coverage is incomplete. |
| Pickers | DateRangePicker | None | Missing | Range state, headline, and calendar range interaction are absent. |
| Pickers | DateInput and DateRangeInput | None | Missing | Typed date alternatives are absent. |
| Pickers | TimePicker, TimeInput, and dialogs | `MaterialTimePicker` | Partial | Clock dial exists; typed input, rich picker, and dialogs are absent. |
| Collections | Carousel families | `MaterialCarousel` | Partial | Compose keylines, snap/fling behavior, state, and multi-aspect API are incomplete. |
| Gestures | SwipeToDismissBox | None | Missing | Anchored dismiss state and direction behavior are absent. |
| Gestures | PullToRefresh | None | Missing | Pull state, threshold, and indicator container are absent. |
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
