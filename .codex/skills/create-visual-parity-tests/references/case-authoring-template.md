# Case authoring worksheet

Use this worksheet while adding a case. Replace placeholders with facts from AndroidX and the
repository's existing adapters. Keep the final case small enough that one failure has one likely
explanation.

## State matrix

| ID | Variant/size | Theme | Content | Interaction | Slots | Viewport | Capture time |
|---|---|---|---|---|---|---|---|
| `component/variant/state/theme` | `...` | light/dark | empty/text/long | enabled/focused/etc. | none/icon/label/etc. | logical dp | settled or exact frame |

Record why each row exists. Prefer one row per independent behavior boundary over a large matrix
that repeats the same layout path.

## Compose registration skeleton

```kotlin
private fun componentContract(id: String, params: ComponentParams) = CaseContract(
    id = id,
    viewportWidthDp = 240,
    viewportHeightDp = 96,
    dark = params.dark,
    renderScale = 2.0f,
    requiredNodes = listOf(
        RequiredNodeSpec("component", "component"),
        RequiredNodeSpec("label", "text", parentId = "component", text = params.label),
        // Add icon/indicator/divider/supporting/shadow only when the scene exposes them.
    ),
)

private fun ComponentScene(probes: ProbeRegistry, params: ComponentParams) {
    // Reproduce the reference's outer constraints and theme explicitly.
    Component(
        modifier = Modifier.then(probes.geometry("component")),
        // Apply probes to actual layout children, not guessed screenshot regions.
    )
}
```

Use `probes.text("id")` plus `probes.onTextLayout("id")` for ordinary text. For a custom-drawn
label or indicator, record the equivalent internal layout slot directly and give it a stable node
ID. Do not claim a probe is a text node if it has no text layout data.

## Avalonia registration skeleton

```csharp
new VisualCase(
    Id: "component/variant/state/theme",
    Viewport: new PixelSize(240, 96), // logical dp/DIP, not the 480x192 PNG size
    Theme: VisualTheme.Light,
    CreateContent: context => new MaterialComponent
    {
        Name = "component",
        // Copy content, slots, enabled state, and explicit dimensions from the Compose case.
    },
    Prepare: context =>
    {
        var component = context.Require<MaterialComponent>("component");
        // Set focus/input state after mounting; settle or advance motion deterministically.
        context.SettleAnimations();
    });
```

Register the case in the appropriate `*ParityCases.cs` file and ensure that file is included by
`VisualCaseCatalog.All`. Do not duplicate the ID. If the control has no matching Visual for an
internal drawing, extend the capture adapter with a named probe instead of approximating the node
from the screenshot.

## Probe worksheet

For each visible or behaviorally important element, answer:

| Element | Compose source/probe | Avalonia source/probe | Compare as |
|---|---|---|---|
| outer component | `onGloballyPositioned` | named control bounds | geometry + raster |
| text | `onTextLayout` | `TextLayout`/adapter | text layout + local raster |
| custom indicator/label/notch | explicit geometry probe | internal named bounds probe | geometry + raster |
| focus/caret | focus recorder / visible caret | editor focus / caret adapter | state + local raster |
| elevation | shadow probe | shared shadow adapter | shadow contract + local raster |

If an element cannot be observed structurally, state the limitation in the case or adapter. Do not
silently broaden raster tolerance to compensate.

## Profile decision

Use the decision table, schema reference, calibration procedure, and review checklist in
`docs/comparison-profiles.md`. Record only the case-specific conclusion here:

```text
resolved profile: <existing profile ID>
rule: <existing or proposed case prefix>
structural status: <all matched, or defect/skip to resolve before policy work>
renderer evidence: <owner, delta distribution, component shape, representative cases>
policy action: <none, reuse profile, add prefix, or propose new profile>
mutation coverage: <intended pass and first failing boundary>
```

Do not propose a policy action for a structural mismatch. Keep caret, glyph, fill, border, and
shadow-contract defects active until the implementation or probe is corrected.

## Failure notes

When a case fails, record evidence in this order:

```text
case: component/variant/state/theme
profile: <resolved profile>
failure: Contract | Geometry | TextLayout | Raster
expected/actual: <node or metric from layout.json>
source fact: <AndroidX file and default/layout behavior>
hypothesis: <one variable>
change: <one implementation change>
next run: <smallest case or family>
```

This keeps a screenshot mismatch from being mistaken for a token, font, or renderer problem before
the layout contract has been checked.
