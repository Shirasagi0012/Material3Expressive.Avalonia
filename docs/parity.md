# Parity with compose-material3

Referent: **compose-material3 `1.5.0-alpha25`** (androidx @ `ff0a0a99`). Every token file in the
repository records the upstream file it was transcribed from and that file's token-DB stamp, so a
transcription can be re-checked against the source at any time.

This page lists what deliberately differs. Anything not listed here is intended to match.

## Deviations

| Area | Upstream | Here | Why |
|---|---|---|---|
| **Elevation** | Two shadows composited from a light source — a tight key shadow and a wide ambient one — through Android's shadow renderer. | One box shadow per level, tuned to read the same at the six Material levels. | Avalonia has a single offset-blur-spread shadow per layer. The two cannot be derived from the same optics, so they are matched by eye at the levels that actually ship. |
| **Focus indicator** | The Material 3 spec describes an inset ring drawn outside the container fill. | The focus state layer at `FocusStateLayerOpacity`. | compose-material3 `1.5.0-alpha25` has no separate focus ring for buttons — focus *is* the state layer. The pinned referent wins over the spec page. |
| **Small button vertical padding** | 8dp for precision pointers, 10dp for coarse ones (`ButtonDefaults.smallVerticalPadding`). | 8dp. | Avalonia exposes no equivalent of Android's pointer-precision signal. The desktop value is the default; an app that needs the coarse one can retune the token. |
| **Emphasized typefaces** | The emphasized type scale slots reuse `TypefaceTokens.Brand` and `.Plain` with a heavier weight. | Separate `EmphasizedBrand` and `EmphasizedPlain` slots, defaulting to the same faces. | Out of the box the result is identical. The extra slots exist so an app can point the emphasized styles at a distinct face — a Roboto Flex instance, say — which is what the Expressive spec assumes and what a variable-font pipeline will want. |
| **State layer stacking** | Interactions are an ordered stream; the most recent one shows. | A fixed precedence: dragged over pressed over focused over hovered. | Avalonia reports interaction as independent boolean properties, not as an ordered stream. The fixed order matches the only sequence the interactions can arrive in. |
| **Spring settling** | `getDurationNanos` from the spring estimator. | The same estimator, same port. | No deviation — noted because the obvious shortcut (stop when the value is close to the target) is *not* what is implemented: an overshoot passing through the target would be mistaken for arrival. |
| **Switch drag** | The knob can be dragged between states. | Click and keyboard only. | `MaterialSwitch` paints itself, so it derives from `ToggleButton` rather than `ToggleSwitch`, whose required knob template parts it could not satisfy meaningfully. Drag-to-toggle went with them and is not yet reimplemented. |
| **Menu** | `DropdownMenu`, built from Compose's own primitives. | A restyle of Avalonia's `MenuFlyoutPresenter` and `MenuItem`. | Avalonia's menu already handles keyboard navigation, submenus, access keys and popup placement, none of which Material changes. What Material specifies — the surface, the shape, the type scale, the state layer — is exactly what a restyle can express. |
| **Carousel metrics** | No generated token file exists upstream. | Item widths and spacing taken from the Material 3 spec page. | Nothing to transcribe. The table records `"version": "spec"` rather than a token-DB stamp, so this is visible wherever the values are read. |

## Known upstream inconsistencies, transcribed faithfully

These are transcribed as upstream has them, with a comment at the value:

- `ElevatedButtonTokens.UnselectedIconColor` is `OnPrimary`, while every other unselected icon
  token in that file is `Primary`. Kept rather than silently corrected.
- `TextButtonTokens.LabelColor` is `OnSurfaceVariant`, but `ButtonDefaults.textButtonColors`
  overrides it with `Primary` and marks the token as awaiting correction. The component table uses
  `Primary`, which is what ships.
- `ButtonXSmallTokens`' leading and trailing spaces are 16dp, but
  `ButtonDefaults.ExtraSmallContentPadding` uses 12dp and marks the token as awaiting correction.
  The 12dp is used.
- `FabMediumTokens.ContainerShape` is commented out in the generated file. `CornerLargeIncreased`
  is used, which is what the spec and `FloatingActionButtonDefaults` call for.
- `LargeIconButtonTokens` names its middle width `Uniform` where every other size says `Default`.
  Transcribed under the common name so the component table can treat the sizes uniformly.

## Not built

- **Floating toolbars**, listed alongside the app bars in the roadmap's fourth wave.
- **Adaptive/flexible navigation**, which reshapes the bar and rail by window size. Both exist as
  fixed forms.
- **Date and time input** (the typed alternatives to the pickers' dials and grids).

## Not yet ported

State-layer and container colours for the per-interaction variants (`Focused*`, `Hovered*`,
`Pressed*`) are not transcribed: in every button and icon button variant upstream they repeat the
resting colour verbatim, because Expressive expresses interaction as a state layer drawn over the
container rather than as a different container colour. The elevations, which genuinely do vary by
state, are transcribed.
