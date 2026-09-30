# Pinned Compose reference

Selected on 2026-09-30. These pins are the target for subsequent component migration and
the forthcoming visual-parity producer. They do not certify that the existing Avalonia
implementation has already been migrated or visually verified against this target.

Machine-readable pins and source-artifact checksums: [upstream-baseline.json](upstream-baseline.json).
Current implementation status and next work: [START_HERE.md](START_HERE.md).

## Selected commits

| Role | Repository / commit | Published version |
| --- | --- | --- |
| AndroidX implementation source | [androidx/androidx at `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`](https://github.com/androidx/androidx/commit/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea) | Material3 `1.5.0-alpha27`; Compose / animation `1.13.0-alpha02` |
| Executable reference source | [JetBrains/compose-multiplatform-core at `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`](https://github.com/JetBrains/compose-multiplatform-core/commit/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e) | Compose Multiplatform and Material3 `1.13.0-alpha01` |

The AndroidX commit timestamp is 2026-08-20 02:10:24 UTC. The Core commit timestamp is
2026-09-08 08:18:22 UTC; its annotated release tag is `v1.13.0-alpha01`.
The latter is the **Core** repository, not the similarly named `compose-multiplatform`
repository containing the Gradle plugin, release notes and examples.

For the future producer, pin `org.jetbrains.compose` and the Compose runtime, UI,
foundation, animation, Material3 and Material3 ripple dependencies to `1.13.0-alpha01`.
The Gradle plugin uses the same release version but belongs to a different repository.
Material3 must be declared explicitly; do not inherit the previous project's
`material3:1.12.0-alpha03` dependency. The complete resolved dependency graph, compiler,
JDK and native renderer checksums still need to be locked when the producer is built.

## Why this pair

- The [official 1.13.0-alpha01 release](https://github.com/JetBrains/compose-multiplatform/releases/tag/v1.13.0-alpha01)
  maps Material3 to AndroidX `1.5.0-alpha27` and the Compose runtime/UI/foundation to
  `1.13.0-alpha02`. Its Material3, ripple and animation artifacts are available from
  Maven Central; a local build of the full upstream repository is not required.
- The [1.12.1 stable release](https://github.com/JetBrains/compose-multiplatform/releases/tag/v1.12.1)
  still uses Material3 `1.12.0-alpha03`, based on AndroidX `1.5.0-alpha22`.
  Choosing it would retain the old implementation/reference version gap, or require
  moving the implementation target back to alpha22.
- This chooses a published alpha release intentionally. A fixed, recent Expressive
  implementation with corresponding published desktop artifacts is more useful to
  this rewrite than combining a newer AndroidX checkout with older desktop Material3.
- The Core release tag explicitly names `org.jetbrains.compose.material3:material3*:1.13.0-alpha01`.
  Its history includes [Material3 alpha27 integration](https://github.com/JetBrains/compose-multiplatform-core/commit/c3c211e833339b374f2000092ee0d8a6d01b40ad)
  and [Compose alpha02 integration](https://github.com/JetBrains/compose-multiplatform-core/commit/f8cf5a4cb0b68b074b2f0e71961da3cea006f0e0).
  The integration history also records importing AndroidX `f3ed195f...` (build 16125988)
  in [this merge](https://github.com/JetBrains/compose-multiplatform-core/commit/79e91735f71fc684b042633ef5be5ffaa7af0962).

Do not infer the Core release's version from its root `libraryversions.toml`: at the
selected Core commit that file still contains older version values. Use the release
tag, published dependency metadata and verified source contents together.

## Source verification

Verification used Git object contents, not the current checked-out files. Source JARs
were downloaded from Google Maven / Maven Central. Every packaged `.kt` file in the
scope below was mapped from `<sourceSet>/<package>` to `src/<sourceSet>/kotlin/<package>`
and its raw bytes compared with the selected Git blob. No whitespace normalization
or semantic normalization was applied.

| Published source artifact | Compared with | Identical files |
| --- | --- | --- |
| `androidx.compose.material3:material3:1.5.0-alpha27:sources`, commonMain | AndroidX pin | 247 / 247 |
| `org.jetbrains.compose.material3:material3-desktop:1.13.0-alpha01:sources`, all packaged Kotlin source sets | Core pin | 351 / 351 |
| `org.jetbrains.compose.material3:material3-ripple-desktop:1.13.0-alpha01:sources`, all packaged Kotlin source sets | Core pin | 6 / 6 |
| `org.jetbrains.compose.animation:animation-core-desktop:1.13.0-alpha01:sources`, all packaged Kotlin source sets | Core pin | 42 / 42 |
| `org.jetbrains.compose.animation:animation-desktop:1.13.0-alpha01:sources`, all packaged Kotlin source sets | Core pin | 33 / 33 |

### Why not the AndroidX release-notes endpoint?

The [Material3 alpha27 release notes](https://developer.android.com/jetpack/androidx/releases/compose-material3#1.5.0-alpha27)
link to a commit range ending at `e803e681a62cf709ac0eef07a56ac6cc0bba332a`.
That commit's Material3 common source differs from the published Google source JAR in
five files: `Button.kt`, `ComponentProperties.kt`, `ComponentStyles.kt`, `RadioButton.kt`
and `ToggleButton.kt`. The selected `f3ed195f...` commit matches all 247 files.
Consequently, this project pins `f3ed195f...`; the release-notes endpoint and the
Core repository's `androidx/compose-material3/1.5.0-alpha27` tag are not its source pin.

This is a measured source difference. It is not a claim about why the upstream
release notes and published sources differ.

### Known differences between the selected source commits

| Scope | Observed difference | Treatment |
| --- | --- | --- |
| Material3 commonMain | 245 of 247 files identical. `ColorScheme.kt` uses `buildString` instead of concatenation in `toString()`, including separator differences. | Diagnostic string formatting; do not infer a color-rendering difference from it. |
| Material3 commonMain | `internal/BasicTooltip.kt` forces `PopupProperties(focusable = false)` in the Core fork; AndroidX forwards the requested `focusable` value. | Explicit platform behavior difference. A desktop reference pass cannot certify Android focus behavior. Decide and document the Avalonia behavior when implementing Tooltip; do not hide the difference with raster tolerance. |
| Material3 ripple commonMain | All 5 files identical. | Use the common/desktop path for parity. Android's native RippleDrawable remains a separate platform path. |
| animation-core commonMain | All 37 shared Kotlin files identical; Core adds `internal/Arrays.kt`, a FloatArray binary-search helper. | Record the helper as a fork addition, not an animation-trajectory mismatch. |
| animation commonMain | 29 of 30 shared files identical. `SharedTransitionScope.kt` only adds a `kotlin.js.JsName` import and annotation. | JavaScript naming adaptation; no changed animation body in this diff. |

These counts cover the listed modules and source sets, not all of AndroidX or all
platform implementations. The Desktop JAR checks establish source provenance; no
producer was compiled or rendered during this baseline selection, and no visual or
animation parity result is implied.

## Reading the pinned sources locally

The existing local repositories are convenient object stores:

- AndroidX: `D:\source\androidx`
- Compose Core: `D:\source\compose-multiplatform-core`

Both working trees were switched to their respective pinned commits in detached-HEAD
mode on 2026-09-30 and verified clean. Before relying on working-tree files in a later
session, verify HEAD against the pins. Reading the specified commit directly remains reliable:

```powershell
git -C D:\source\androidx show f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea:compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Button.kt
git -C D:\source\compose-multiplatform-core show a9e47e8aa04c95cfe572ab4297c770f3cbfb234e:compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Button.kt
```

For a repository that does not yet contain the objects:

```powershell
git -C D:\source\androidx fetch --depth=1 --no-tags origin f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea
git -C D:\source\compose-multiplatform-core fetch --depth=1 --no-tags origin refs/tags/v1.13.0-alpha01:refs/tags/v1.13.0-alpha01
```

To reproduce the artifact check, download the source URLs in `upstream-baseline.json`,
verify their SHA-256 values, then compare the scoped `.kt` entries with `git show` at
the recorded commit. A source-JAR checksum is not a checksum of the compiled binary.

## Consequences for the current rewrite

The previous source reference was AndroidX `ff0a0a99c4f0cfe95d75d5df615e51d399575922`
(Material3 alpha25). Keep existing validation claims attached to that historical pin
until they are rechecked; selecting a new target does not retroactively upgrade them.

A focused source diff from that revision to the new AndroidX pin found:

- `IconButton.kt`, `internal/AnimatedShape.kt`, `internal/Elevation.kt`, and the
  Material3 ripple common sources are unchanged.
- `Button.kt` removes a hidden, deprecated `contentPaddingFor(buttonHeight)` overload.
- The Expressive spring damping/stiffness values are unchanged; their declarations
  change from `val` to `const val`.
- There are wider Material3 component and token representation changes. Audit the
  affected family when migrating it; do not treat all changed token files as changed values.
- `animation-core/Transition.kt` and `VisibilityThresholds.kt` changed. Reconcile
  them with the separately versioned ComposeAnimation dependency before claiming
  full animation parity against the new baseline. This task did not change that dependency.

## Use and upgrades

- Derive Material3 component rules from the AndroidX pin and inspect the pinned Core
  implementation for desktop rendering and platform adaptations.
- A parity scenario executes the pinned published Core artifacts. Any intentional
  divergence from AndroidX must be named, as with Tooltip above.
- Avoid mixing individual files from current main, alpha25 and this baseline.
- Upgrade commits, artifact versions, source checksums and this difference record
  together. Once the producer exists, also update its dependency lock/checksums and
  regenerate Compose reference artifacts. Never accept an Avalonia screenshot as a
  replacement Compose baseline.
