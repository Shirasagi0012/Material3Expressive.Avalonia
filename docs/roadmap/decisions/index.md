# Roadmap decisions index

Decision records are split so a task can load only the records it names. This index is for
navigation and one-line summaries; the linked files are authoritative.

| ID | Title | Summary |
|---|---|---|
| D001 | [Adaptive navigation package boundary](D001-adaptive-navigation-boundary.md) | Decide whether adaptive navigation belongs in the core package or a separate namespace. |
| D002 | [Shared owned/external state convention](D002-shared-owned-external-state.md) | Decide the common state ownership pattern for controls. |
| D003 | [Compose parity versus desktop adaptation](D003-compose-versus-desktop-adaptation.md) | Define and document the boundary between parity and intentional desktop adaptation. |
| D004 | [Custom-drawn control accessibility abstraction](D004-custom-drawn-accessibility.md) | Define UI Automation representation for independently operable custom-drawn parts. |
| D005 | [Public API source-compatibility guarantees](D005-public-api-compatibility.md) | Decide when public APIs receive source-compatibility guarantees. |
| D006 | [Reporting parity-unavailable alpha25 behavior](D006-alpha25-parity-unavailable.md) | Define catalog and test-output reporting for behavior absent from the pinned reference. |
| D007 | [ListItem long-click contract](D007-listitem-long-click.md) | Use `InteractionMode` and `Holding` for row long-click behavior. |
| D008 | [Segmented list grouping API](D008-segmented-list-grouping.md) | Use `MaterialSegmentedList` to infer grouping from visible direct children. |
| D009 | [MaterialScaffold layout API](D009-materialscaffold-layout-api.md) | Define scaffold slots, content padding, insets, and snackbar ownership. |
| D010 | [SegmentedButton row API](D010-segmented-button-row-api.md) | Adapt single- and multi-choice segmented rows through one explicit-selection container. |
| D011 | [FloatingToolbar XAML API](D011-floating-toolbar-xaml-api.md) | Consolidate horizontal/vertical and with/without-FAB overloads into one adaptive items control. |
| DEF-001 | [Complex-shape raster residual](DEF-001-complex-shape-raster-residual.md) | Keep the authorized complex-shape warning narrow and temporary. |
| DEF-002 | [Snackbar accessibility recommended timeout](DEF-002-snackbar-accessibility-timeout.md) | Defer platform recommended-timeout adaptation until a suitable accessibility preference source exists. |
