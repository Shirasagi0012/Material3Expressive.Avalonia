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
