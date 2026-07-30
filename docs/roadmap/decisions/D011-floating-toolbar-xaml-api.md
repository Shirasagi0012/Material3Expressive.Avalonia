# D011 — FloatingToolbar XAML API

Status: Accepted
Date: 2026-07-31
Applies to: M5-TOOLBAR-001

AndroidX exposes separate horizontal/vertical composables and separate overloads with and without
an adjacent FAB. Avalonia represents those four entry points with one
`MaterialFloatingToolbar` in the existing `Material3Expressive.Controls` namespace:

- direct XAML children populate the main `Items` action sequence;
- `LeadingContent` and `TrailingContent` are optional single XAML slots used by the no-FAB form;
- `FloatingActionButton` selects the FAB form and `FabPosition=Start|End` is
  logical: horizontal Start/End follows flow direction, while vertical Start/End means top/bottom;
- `Orientation=Horizontal|Vertical|Auto` consolidates the two axes. `Auto` uses the constrained
  host aspect ratio, choosing horizontal for wide/equal regions and vertical for tall regions;
- the application remains responsible for screen-edge anchoring. The bindable read-only
  `ScreenOffset` output exposes the upstream 16dp recommendation without overwriting application
  alignment or margin values.

`IsExpanded` is a simple two-way scalar under D002. The complex exit-always offset is a replaceable
non-null `FloatingToolbarScrollState`; `ResetState()` creates a fresh default. `ScrollTarget`
adapts an Avalonia `ScrollViewer`, while custom scrolling containers forward positive hide and
negative reveal deltas through `ApplyScrollDelta`. `ExitDirection` selects the logical edge.

The toolbar constrains a supplied FAB to the upstream 56–80dp animated size range but does not
recolour arbitrary custom FAB content. Applications choose a standard Primary or vibrant Tertiary
`MaterialFab` just as Compose callers choose the matching Defaults helper.

The automation node is a ToolBar with `IExpandCollapseProvider`; action controls retain their own
peers. Tab follows ordinary desktop traversal. Axis arrows wrap between visible actions and
Home/End select the first/last action. Collapsed, clipped, or off-screen actions leave the Tab
sequence immediately.
