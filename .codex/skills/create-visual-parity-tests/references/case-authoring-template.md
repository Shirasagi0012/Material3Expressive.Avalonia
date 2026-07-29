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
        // Add icon/indicator/divider/supporting nodes only when the scene exposes them.
        // For renderer-level elevation, declare its owner as kind = "shadow" instead of a
        // generic component/container node; see the shadow contract below.
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
| elevation | `RequiredNodeSpec(..., kind = "shadow")` + shadow probe | matching named control + shared shadow adapter | complete shadow contract + local raster |

If an element cannot be observed structurally, state the limitation in the case or adapter. Do not
silently broaden raster tolerance to compensate.

### Shadow/elevation contract

When the case intentionally compares renderer-level elevation, declare the owner as a shadow node
on the Compose side:

```kotlin
requiredNodes = listOf(
    RequiredNodeSpec(id = "component", kind = "shadow"),
)
```

Use the same `component` ID for the matching named Avalonia control. Both capture adapters must
emit a complete `ParityShadow` contract: shape and occluder bounds, corner radii, transform,
elevation, ambient/spot colors and alpha, opacity/flags, light, clip,
`occluderDrawn`, and `transparentOccluder`.

Do not add pixel counts or delta ceilings to either scene. `shadowSupport` and `shadowEdge` are
created automatically only after this contract and the node geometry match. Without a matched
shadow contract, the case receives no shadow-specific raster allocation; generic `shapeEdge` is
not a replacement for declaring elevation.

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
expected comparison kind: <exactMatch | semanticRasterEquivalent | rasterWarning | mismatch>
```

Do not propose a policy action for a structural mismatch. Keep caret, glyph, fill, border, and
shadow-contract defects active until the implementation or probe is corrected.

`semanticRasterEquivalent` means the normal `rasterPolicy` accepted expected-derived, calibrated
renderer noise after every structural check passed. It is not a per-case override.

`rasterWarning` is exceptional. It requires schema-v7 `warningRasterPolicy` and a non-empty
`warningReason` in the centralized comparison profile, never in `CaseContract` or `VisualCase`.
Record this additional block when proposing one:

```text
strict raster result: <why the normal policy rejects>
warning owner and caps: <expected-derived owner, delta/count/energy/component limits>
warning reason: <Deferred issue and evidence>
scope: <narrow case-prefix rule>
removal condition: <implementation fix or recalibration evidence>
```

A warning policy is considered only after all structural checks pass. It must not hide geometry,
text-layout, focus, content, or shadow-contract defects.

## Failure notes

When a case fails, record evidence in this order:

```text
case: component/variant/state/theme
profile: <resolved profile>
comparison kind: <exactMatch | semanticRasterEquivalent | rasterWarning | mismatch>
failure: Contract | Geometry | TextLayout | Raster
expected/actual: <node or metric from layout.json>
source fact: <AndroidX file and default/layout behavior>
hypothesis: <one variable>
change: <one implementation change>
next run: <smallest case or family>
```

For a `rasterWarning`, also record the normal raster result, warning-policy result, and the
removal condition. For `semanticRasterEquivalent`, record the normal semantic owner/bin evidence;
do not describe it as a warning.

This keeps a screenshot mismatch from being mistaken for a token, font, or renderer problem before
the layout contract has been checked.
