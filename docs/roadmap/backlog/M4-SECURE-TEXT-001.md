---
id: M4-SECURE-TEXT-001
order: 430
work_item: SecureTextField
priority: P1
status: Complete
milestone: M4
dependencies: [D002]
decisions: [D002, D003]
parity: static
comparison-profile: existing
docs: [docs/testing/visual-parity/core.md]
source-baseline: [SecureTextField.kt]
---

# SecureTextField

## Scope

Implement secure editing, reveal behavior, keyboard/focus semantics, validation, automation, and
Material text-field documentation.

## Exit criteria

Secure input behavior, state restoration, accessibility, tests, and parity/version-gap evidence are
complete.

## Current implementation evidence

- AndroidX source review: `SecureTextField.kt:45-201,214-365` and
  `BasicSecureTextField.kt:124-213,230-333` at `ff0a0a99` / Material3 1.5.0-alpha25.
- `MaterialSecureTextField` reuses the filled/outlined `MaterialTextField` chrome and Avalonia's
  native `TextBox` editing engine for caret, selection, IME, undo, and keyboard focus. It is
  single-line, uses Password/Sensitive text-input hints, disables cut/copy and incoming drops,
  disables capitalization/suggestions, and keeps paste available.
- `SecureTextObfuscationMode` exposes Visible, Hidden, System, and RevealLastTyped requests; the
  bullet is customizable and `IsPasswordVisible`, `RevealPassword()`, and `HidePassword()` provide
  an application-bindable full reveal. The alpha25 RevealLastTyped request safely maps to Hidden
  on Avalonia desktop because its native editor can only reveal the entire password or none of it.
  The implementation never substitutes an unsafe timed full-value reveal.
- D002 option 1A intentionally does not add a state object: text, visibility, obfuscation mode, and
  mask character are ordinary bindable scalar state. Applications restore them directly, and the
  editor's text binding remains two-way.
- Filled/outlined visual validation and supporting/error chrome are inherited from the completed
  text-field implementation. Focus remains on the native editor rather than adding a wrapper focus
  stop.
- `MaterialSecureTextFieldAutomationPeer` exposes an Edit/Value contract named by the label,
  accepts enabled automation value input, returns an empty value so the secret is never disclosed,
  reports disabled read-only state, and publishes supporting/error help and Invalid item status.
  The inner native editor remains in the raw accessibility view to avoid a duplicate normal-view
  element.
- Focused deterministic tests: `MaterialSecureTextFieldTests` (5/5 passed), covering password
  input options, masking and secure RevealLastTyped fallback, cut/copy/drop policy, visible/hidden
  commands, two-way restoration, validation/focus, and non-leaking/error-aware automation.
- Static parity covers filled Hidden and outlined Visible states against the alpha22 executable
  producer. Consecutive artifacts `artifacts/visual-parity/xunit/20260730-053717-117` and
  `artifacts/visual-parity/xunit/20260730-054018-328` pass both cases as
  `semanticRasterEquivalent` with exact component/label geometry and text contracts and zero
  rejected pixels. Expected/actual raster, layout, comparison, mask, and diff SHA-256 hashes are
  identical between runs.
- Authoring the scoped secure-label cases completed the Avalonia custom-label style probe and
  corrected the outlined notch's right endpoint to the active physical-pixel boundary. The five
  settled existing outlined text-field rows remain accepted. The existing focused-empty row's
  four caret-endpoint pixels reproduce with the original notch code and are not caused or hidden
  by this task.

## Completion evidence

| Dimension | Evidence |
|---|---|
| Upstream | AndroidX `ff0a0a99` `SecureTextField.kt:45-201,214-365` and Foundation `BasicSecureTextField.kt:124-213,230-333`; native Avalonia editing, raw-view focus target, and safe RevealLastTyped fallback are recorded desktop adaptations. |
| Tokens | Secure fields reuse generated filled/outlined `TextFieldTokens` v0_210; masking and security behavior introduce no visual token family. |
| API | `MaterialSecureTextField` inherits Text/Label/Placeholder/SupportingText/IsError/Variant and adds `TextObfuscationMode`, `TextObfuscationCharacter`, two-way `IsPasswordVisible`, `RevealPassword()`, and `HidePassword()`. |
| Visual | Filled Hidden and outlined Visible alpha22-compatible states pass the existing centralized `text-field` profile twice with exact named probes and zero rejected pixels. |
| Behavior | Native single-line editing, password/sensitive hints, cut/copy/drop policy, paste, visible/hidden commands, customization, scalar restoration, validation, and focus are deterministic-test covered. |
| Desktop | System mode follows the desktop fully-hidden convention. RevealLastTyped has a documented secure fallback because Avalonia lacks per-codepoint password rendering; no whole-password timed reveal is synthesized. |
| Accessibility | Edit/Value automation is named by Label, accepts input without returning the secret, tracks enabled/read-only state, and raises supporting/error HelpText and Invalid ItemStatus changes. |
| Tests | Focused suite passes 5/5; two static parity rows pass twice reproducibly at the artifact roots above; the Catalog project builds with no errors. |
| Documentation | README usage/security boundary, Catalog hidden/visible/validation examples, component matrix, roadmap history, and this task record match the implementation. |

The exit criteria are satisfied: secure editing, explicit reveal/hide behavior, scalar restoration,
validation, focus, accessibility, tests, and supported parity evidence are complete. D003 remains
open as a cross-component policy decision; the safe desktop masking adaptation and its removal
condition (a native per-codepoint obfuscation API) are explicit, so this task is not blocked by it.
