# Compose reference producer

Desktop/JVM command-line tool that renders the parity scenes with the pinned
Compose Multiplatform Material 3 artifact and writes a fresh expected bundle
(PNG + layout JSON + manifest) on every invocation.

The output is not committed; it is regenerated at the beginning of every parity
run so the reference version is always cleanly tied to the pinned Compose
Multiplatform dependency set.

For the end-to-end Avalonia comparison command and artifact interpretation, see
[`docs/visual-parity.md`](../../docs/visual-parity.md).

`capture-app/gradle.lockfile` pins the resolved transitive dependency graph.
`gradle/verification-metadata.xml` records dependency checksums. Refresh either
only as part of an explicit Compose reference-version update.

## Pinned versions

| Dependency | Version |
|---|---|
| Compose Multiplatform plugin/runtime | `1.12.0-beta02` |
| `org.jetbrains.compose.material3` | `1.12.0-alpha03` |
| Corresponding Jetpack Material 3 | `1.5.0-alpha22` |
| Kotlin | `2.3.20` |
| JDK | 17+ |
| Skiko | `0.150.1` |

## Prerequisites

- JDK 17+
- The Windows Roboto variable font at the default location

## Usage

From `tests/ComposeReference`:

```powershell
.\gradlew.bat :capture-app:run --args="--output artifacts/visual-parity/my-run"
```

Filter to specific cases:

```powershell
.\gradlew.bat :capture-app:run --args="--output artifacts/visual-parity/my-run --case button/filled/medium/enabled/light"
```

Use wildcard prefix matching:

```powershell
.\gradlew.bat :capture-app:run --args="--output artifacts/visual-parity/my-run --case button/*"
```

## Output structure

```
<output-dir>/
  expected/
    manifest.json
    cases/
      <case-id>/
        image.png
        layout.json
```

### manifest.json

Records the exact build environment, dependency versions, font hash, theme hash,
and per-case file hashes. A missing or incompatible manifest is a contract failure
on the Avalonia comparison side.

### layout.json

Per-case capture result (schema v4) containing:
- Coordinate space definition (case-specific scale, root-relative physical pixels)
- Viewport dimensions in physical pixels
- Required node bounds as reported by `Modifier.onGloballyPositioned`; the custom TextField label
  is a snapped framework layout slot, rather than a floating glyph/line measurement
- Text draw origin, local and root-space metrics/baselines, and per-character advance boxes for
  registered `Text` nodes
- A diagnostic text-style signature (requested/resolved family, pinned font hash, weight, size,
  line height, and letter spacing) wherever both implementations have a direct style probe
- The observed focus state for nodes that explicitly contract focus
- For renderer-level elevation nodes, a reconstructible Skia m150 shadow contract: occluder shape,
  local-to-device transform, elevation, colors/alphas, opacity/flags, point light, and parent clip

The official `TextField` editable implementation exposes a `TextLayoutResult` through semantics,
but not its independent root drawing origin. Until a dedicated adapter supplies that origin, input
text remains covered by the text-field image region; labels and supporting text are fully probed.

## Stable capture protocol

The producer never waits on wall-clock time. `ImageComposeScene.render()` defaults to frame time
zero, so capture starts at `0ns` and advances in deterministic `16.667ms` steps. For a contracted
focus state it pumps at most 16 dispatch frames until the real interaction-source state is observed,
then runs two transition-registration frames, a terminal frame exactly 5 seconds later, two commit
frames, and a final capture frame. Focus is requested only after the official TextField has been
composed, so its non-replaying interaction collector cannot miss the focus event. Generation fails
if the requested semantic state does not arrive within the bounded dispatch phase.

The text cursor remains part of focused screenshots. Blink is disabled only for the capture
environment (`LocalCursorBlinkEnabled=false`), which means an always-visible caret rather than a
hidden one; the Avalonia scene uses the equivalent always-visible caret setting. This prevents a
wall-clock blink phase from being mistaken for a component difference.

## Rendering environment

- Canonical logical scale: `2.0` (1 dp = 2 physical px); scale-specific cases may override it
- Density: `Density(case.renderScale, 1.0f)`; the canonical suite uses 320 dpi equivalent
- Font scale: `1.0`
- Locale: `en-US`
- Layout direction: `LTR`
- Color space: sRGB
- Background: `MaterialTheme.colorScheme.surface`
- Font: Repository-pinned Roboto variable font, with explicit `wght` instances for
  Material's 400 (Normal), 500 (Medium), and 700 (Bold) tokens. The explicit axis is
  required because Compose Desktop's `Font(File, FontWeight)` overload does not apply
  the requested variable-font weight by itself.
- Execution: Headless, offscreen via `ImageComposeScene`

## Adding a new case

1. Add a scene `@Composable (ProbeRegistry) -> Unit` function in the appropriate
   file under `scenes/`.
2. Register it in `ReferenceCaseRegistry` with a `CaseContract` describing the
   viewport, theme, and required node probes.
3. Rebuild and run.
