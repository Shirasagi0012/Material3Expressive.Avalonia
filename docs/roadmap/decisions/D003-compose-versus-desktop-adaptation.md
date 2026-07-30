# D003 — Compose parity versus desktop adaptation

Status: Open
Blocks: cross-component parity and API review

Question: Where is the boundary between executable Compose parity and an intentional Avalonia
desktop adaptation, and how must that boundary be documented?

## Authorized component-specific adaptation — RangeSlider endpoint focus

D004 authorizes two virtual Slider automation children so assistive technology can address the
Start and End thumbs independently. Avalonia virtual peers are not `IInputElement` instances, so
they cannot reproduce Compose's two independent physical focus targets without adding real proxy
controls. The accepted desktop adaptation keeps `MaterialRangeSlider` as one physical focus
element. Forward Tab traverses Start, End, then leaves; Shift+Tab traverses End, Start, then
leaves. This synchronizes the active endpoint with automation focus and draws a visible endpoint
focus indicator without trapping ordinary desktop navigation. Both endpoints must remain
fully keyboard-operable and deterministically tested. This authorization is narrow and does not
resolve the broader D003 question.

## Authorized component-specific adaptation — FloatingToolbar desktop focus and anchoring

Compose places toolbar children in ordinary Row/Column scopes and exposes accessibility custom
actions for expansion. Avalonia keeps each supplied action as a real focusable control, exposes the
container as ToolBar + ExpandCollapse, and adds conventional axis-arrow plus Home/End traversal;
Tab remains ordinary non-trapping desktop traversal. Content that is retracted or translated away
is removed from the Tab sequence, because desktop focus must not land on clipped actions.

`Orientation=Auto` resolves from the constrained host aspect ratio, but the control does not move
itself to a window edge or overwrite alignment/margins. The application owns that placement and can
bind the upstream 16dp `ScreenOffset`. Avalonia has no public equivalent of Compose's
touch-exploration service flag, so the control does not globally force expansion when a screen
reader attaches; its ExpandCollapse provider keeps both states directly recoverable. This
authorization is narrow and does not resolve the broader D003 question.

## Authorized component-specific adaptation — ShortNavigationBar responsiveness and focus

Compose leaves EqualWeight/Centered and Top/Start choices to the caller's window-size logic.
Avalonia preserves both explicit policies and adds `Arrangement=Auto`, resolving below 600dp to
EqualWeight and at/above 600dp to Centered without replacing destination controls or selection
state. Icon position remains explicit per item, so Auto does not silently rewrite application
content. The breakpoint is configurable and `ResolvedArrangement` is observable.

Compose items use Tab semantics and platform focus behavior. Avalonia exposes a Tab/TabItem UIA
selection tree and adds conventional horizontal arrow plus Home/End selection, including RTL
reversal. Tab is left to ordinary desktop traversal. This authorization is narrow and does not
resolve the broader D003 question.

## Authorized component-specific adaptation — WideNavigationRail state and focus

Compose hoists expansion through a suspendable `WideNavigationRailState`; Avalonia maps its single
Boolean target to two-way `IsExpanded` and a read-only animated `ExpansionProgress`, making normal
XAML binding and UI Automation direct. The component deliberately has no internal breakpoint:
adaptive window-size coordination binds this property in M5-NAV-003.

Compose destinations retain Tab semantics and platform focus behavior. Avalonia exposes the rail
as Tab/ISelectionProvider with stable TabItem children, adds ExpandCollapse for the bindable rail
state, and uses conventional vertical Up/Down plus Home/End selection/focus. Tab remains ordinary
non-trapping traversal, and header controls retain their native peer before destinations. This
authorization is narrow and does not resolve the broader D003 question.

## Authorized component-specific adaptation — NavigationSuite window and focus continuity

Compose resolves from WindowAdaptiveInfo, including fold posture, and retains item declarations
through `movableContentOf`. Avalonia has no global fold-posture service, so Auto follows the same
600dp medium-width and 480dp medium-height boundaries while custom posture policies set explicit
`LayoutType`. One collection of real `MaterialNavigationItem` controls moves between the short bar
and wide rail. Selection, enabled state, automation peer identity, and a focused destination are
retained; type changes are atomic, while show/hide and wide expansion keep their upstream spatial
springs. Tab behavior remains owned by the active navigation component. This authorization is
narrow and does not resolve the broader D003 question.
