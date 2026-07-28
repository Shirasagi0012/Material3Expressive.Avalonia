# Compose reference producer

Desktop/JVM command-line tool that renders the parity scenes with the pinned
Compose Multiplatform Material 3 artifact and writes a fresh expected bundle
(PNG + layout JSON + manifest) on every invocation.

The output is not committed; it is regenerated at the beginning of every parity
run so the reference version is always cleanly tied to the pinned Compose
Multiplatform dependency set.

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

Per-case capture result (schema v1) containing:
- Coordinate space definition (2× scale, root-relative physical pixels)
- Viewport dimensions in physical pixels
- Required node bounds as reported by `Modifier.onGloballyPositioned`
- Text layout metrics, baselines, and per-character advance boxes for registered `Text` nodes

The official `TextField` editable implementation exposes a `TextLayoutResult` through semantics,
but not its independent root drawing origin. Until a dedicated adapter supplies that origin, input
text remains covered by the text-field image region; labels and supporting text are fully probed.

## Rendering environment

- Logical scale: `2.0` (1 dp = 2 physical px)
- Density: `Density(2.0f, 1.0f)` — 320 dpi equivalent
- Font scale: `1.0`
- Locale: `en-US`
- Layout direction: `LTR`
- Color space: sRGB
- Background: `MaterialTheme.colorScheme.surface`
- Font: Repository-pinned Roboto variable font
- Execution: Headless, offscreen via `ImageComposeScene`

## Adding a new case

1. Add a scene `@Composable (ProbeRegistry) -> Unit` function in the appropriate
   file under `scenes/`.
2. Register it in `ReferenceCaseRegistry` with a `CaseContract` describing the
   viewport, theme, and required node probes.
3. Rebuild and run.
