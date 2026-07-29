# Compose Material3 component coverage

This inventory compares the Avalonia library with the
[implementation source baseline](upstream-baselines.md). It groups related Kotlin overloads,
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
are defined in [`ROADMAP.md`](../ROADMAP.md).

## Summary

| Status | Feature families | Share |
|---|---:|---:|
| Implemented | 19 | 38% |
| Partial | 16 | 32% |
| Missing | 15 | 30% |
| Total | 50 | 100% |

The theming, color, typography, shape, motion, ripple, token-generation, and Material Symbols
foundations are outside this component count. They are shared infrastructure rather than
one-to-one component families.

## Baseline-audit status

The [alpha22→alpha25 difference audit](alpha22-alpha25-difference-audit.md) confirms that all 63
active executable parity cases use alpha22-supported static APIs. They remain alpha22 conformance
evidence only; none is an implicit alpha25 parity pass. Alpha25-specific ToggleButton border
transitions, expressive ListItem text-line-height behavior, and Slider/RangeSlider state and
keyboard-completion behavior are recorded as parity unavailable until the executable reference
can represent them.

## Component matrix

| Area | Compose feature family | Avalonia counterpart | Status | Principal gap or adaptation |
|---|---|---|---|---|
| Buttons | Button variants and sizes | `MaterialButton` | Implemented | Compose composables are represented by `Variant` and `Size`. |
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
| Lists | ListItem and SegmentedListItem | None | Missing | One-, two-, and three-line/list segment layouts are absent. |
| Selection | SegmentedButton rows | None | Missing | Single- and multi-choice row semantics are absent. |
| Navigation | Tab and TabRow families | `MaterialTab`, `MaterialTabs` | Partial | Scrollable rows and the complete indicator/layout API are absent. |
| Navigation | NavigationBar | `MaterialNavigationBar` | Implemented | Bar and rail share one Avalonia control. |
| Navigation | NavigationRail | `MaterialNavigationBar` | Implemented | Selected through the navigation variant. |
| Navigation | ShortNavigationBar and WideNavigationRail | None | Missing | Adaptive and expanded/collapsed navigation forms are absent. |
| Navigation | Modal, dismissible, and permanent drawers | `MaterialNavigationDrawer` | Partial | Compose state and sheet variants are not fully represented. |
| Navigation | Top/Bottom/Flexible AppBar families | `MaterialAppBar` | Partial | Flexible layouts, app-bar DSL, and scroll behaviors are incomplete. |
| Layout | Scaffold | None | Missing | Coordinated app bar, snackbar, FAB, and content insets are absent. |
| Overlays | AlertDialog and BasicAlertDialog | `MaterialDialog` | Partial | General overlay exists; typed confirm/dismiss and Compose dialog contracts are incomplete. |
| Overlays | BottomSheet and ModalBottomSheet | `MaterialBottomSheet` | Partial | Core open/drag behavior exists; state and modality variants are incomplete. |
| Overlays | BottomSheetScaffold | None | Missing | Coordinated sheet/body/snackbar state is absent. |
| Overlays | Snackbar and SnackbarHost | `MaterialSnackbar` | Partial | Host queue, result, and suspend-style state API are absent. |
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
| Primitives | Surface, Divider, and Label | None as public component APIs | Missing | Internal controls draw surfaces and labels, but reusable public primitives are absent. |

## Interpretation notes

- One Avalonia control may intentionally represent several Compose composables when their
  differences are token-driven. This is an API adaptation, not automatically a coverage gap.
- A stock Avalonia control restyle counts as partial when Compose exposes meaningful Material
  state or layout behavior that the style cannot represent.
- Android-only integration is not added to this matrix unless the behavior has a meaningful
  Avalonia desktop equivalent.
- The executable alpha22 parity reference cannot render every alpha25 feature. That limitation is
  tracked separately from implementation status according to
  [`upstream-baselines.md`](upstream-baselines.md).
