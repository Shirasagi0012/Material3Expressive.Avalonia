---
id: M2-TEXTFIELD-001
order: 270
work_item: TextField desktop caret parity closure
priority: P0
status: Complete
milestone: M2
dependencies: [M1-AUDIT-002]
decisions: [D003]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [TextField.kt, TextFieldCursor.kt]
---

# TextField desktop caret parity closure

## Scope

Close the two supported focused-empty TextField parity failures without weakening raster policy,
while preserving native Avalonia editing, selection, IME, undo, caret movement, and desktop blink
behavior.

## Exit criteria

Filled and outlined focused-empty caret geometry passes the existing comparison contract; focused
tests cover the cursor renderer and the complete non-parity suite remains green.

## Investigation

- AndroidX source pin: `ff0a0a99c4f0cfe95d75d5df615e51d399575922`.
- `foundation/text/TextFieldCursor.kt` draws from `cursorRect.top` through
  `cursorRect.bottom`, using the platform `DefaultCursorThickness`.
- The pinned CMP desktop producer resolves `DefaultCursorThickness` to 1dp.
- Avalonia 12.1 `TextPresenter.GetCaretPoints()` instead moves both endpoints inward by 0.5dp.
  Its 1dp flat-capped line therefore omits the first and last physical rows at 2× capture scale.
- The audit artifact `artifacts/visual-parity/xunit/20260730-095717-485` proves that all geometry,
  text, focus, and semantic raster regions pass except two filled and four outlined caret-endpoint
  pixels. This is implementation work, not a version gap or comparison-profile allowance.

## API and behavior

`MaterialTextField` keeps Avalonia's native `TextBox` and `TextPresenter` as the authority for
editing, selection, IME, undo, scrolling, text layout, and caret position. It makes the native
endpoint-inset caret transparent and draws the CMP Desktop contract from the presenter's current
layout rectangle: a complete 1dp-wide cursor from the line top through its bottom, clipped to the
editor viewport.

The Material cursor is visible only for a focused collapsed selection. A zero
`CaretBlinkInterval` keeps it steadily visible; positive intervals use a control-owned timer that
starts visible, toggles at the editor's interval, resets on caret/selection changes, stops on lost
focus, and stops when detached. This is a visual desktop adaptation only; no public API or
automation contract changes.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `TextFieldCursor.kt`, the `ff0a0a99` source pin, CMP Desktop's 1dp actual, and Avalonia 12.1 endpoint inset are recorded above. |
| Tokens | Cursor color remains the resolved focus-label role; CMP Desktop cursor thickness is the platform 1dp actual, not a new Material token. |
| API | No public API changed; native `TextBox` state and input remain canonical. |
| Visual | All 14 `textfield/` and `secure-textfield/` rows pass; both previously failing focused-empty rows are `semanticRasterEquivalent` with zero rejected pixels. |
| Behavior | The cursor follows native caret position, collapsed selection, editor blink interval, focus, and detach lifetime. |
| Desktop | Native pointer, keyboard, IME, selection, undo, and horizontal scrolling remain owned by Avalonia; only cursor rasterization is adapted. |
| Accessibility | Native TextBox/Edit automation and focus semantics are unchanged. |
| Tests | Three focused tests cover the 1×24dp rectangle, selection visibility, zero/positive blink, and focus stop; the complete unit suite passes 383/383 and non-parity visual/contract suite passes 53/53. |
| Documentation | Existing interactive Catalog examples remain valid and its isolated build succeeds; README, component matrix, AndroidX notes, parity overview, difference audit, roadmap, and history describe the resolved contract. |

## Validation

```powershell
dotnet test tests\Material3Expressive.Tests\Material3Expressive.Tests.csproj --no-restore
dotnet test tests\Material3Expressive.VisualTests\Material3Expressive.VisualTests.csproj `
  --no-restore --filter "Category!=VisualParity"
dotnet test tests\Material3Expressive.VisualTests\Material3Expressive.VisualTests.csproj `
  --no-restore --filter "Category=VisualParity"
```

Results: 383/383 unit tests and 53/53 non-parity visual/contract tests pass. The fresh full parity
artifact is `artifacts/visual-parity/xunit/20260730-101407-403`: 127 accepted rows
(38 `exactMatch`, 85 `semanticRasterEquivalent`, four authorized circular-wavy
`rasterWarning` rows), two explicit display-headline skips, and zero failures. Both
`textfield/{filled,outlined}/focused-empty/light` rows are `semanticRasterEquivalent` with zero
warnings and rejected pixels. No case, profile, raster budget, or reference producer changed.
The Catalog builds with zero errors through isolated output at
`artifacts/build/M2-TEXTFIELD-001/bin`; the only warning is the repository's existing AVA3001
private-API notice.
