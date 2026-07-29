# Upstream baselines

This document defines the two upstream baselines used by Material3Expressive. They intentionally
move independently: the library can study and implement a newer AndroidX source revision even when
Compose Multiplatform has not yet published an executable desktop artifact from the same lineage.

## Current pins

| Contract | Pin | Purpose |
|---|---|---|
| Implementation source baseline | AndroidX `ff0a0a99c4f0cfe95d75d5df615e51d399575922` (`androidx-main`, 2026-07-26), Compose Material3 `1.5.0-alpha25` | API inventory, token transcription, component behavior, and new implementation work |
| Executable parity reference | Compose Multiplatform Material3 `1.12.0-alpha03`, corresponding to Jetpack Material3 `1.5.0-alpha22` | Executable Compose scenes used by visual-parity tests |
| Parity runtime/toolchain | Compose Multiplatform plugin/runtime `1.12.0-beta02`, Kotlin `2.3.20`, JDK 17 | Builds and runs the desktop reference producer |

The implementation source baseline is available in the local AndroidX checkout at
`D:\source\androidx`. Its Material3 common source root is:

```text
D:\source\androidx\compose\material3\material3\src\commonMain\kotlin\androidx\compose\material3
```

The exact executable dependency graph and checksums are pinned under
[`tests/ComposeReference`](../../tests/ComposeReference).

## Terminology

Use these names consistently:

- **implementation source baseline** means the exact AndroidX commit used to decide what the
  Avalonia implementation should expose and how it should behave;
- **executable parity reference** means the Compose Multiplatform artifact that can actually be
  rendered by the desktop test producer;
- **upstream baseline** without a qualifier is ambiguous and should not be used in new
  documentation or code comments.

The alpha22 executable reference is not the implementation target for the whole library. It is a
deliberately older, executable observation point imposed by the Compose Multiplatform release
schedule.

## Resolving differences between the pins

When alpha22 and alpha25 differ:

1. API shape, tokens, and new implementation work follow the implementation source baseline.
2. Existing parity scenes continue to compare against the alpha22 executable behavior and must
   identify the version gap when it is the reason for a mismatch.
3. A tolerance, ignore region, or passing label must not be introduced merely to hide an upstream
   version difference.
4. Features present in alpha25 but absent from the executable artifact use source review, unit
   tests, structural tests, and deterministic Avalonia visual cases. Their Compose parity status is
   recorded as unavailable for the pinned reference, not passed.
5. A deliberate compatibility choice must name both behaviors and explain why the older behavior
   is retained.

Token tables continue to record their per-file token-database stamps. The AndroidX library version
does not replace those stamps because generated upstream token files can originate from different
token-database revisions.

## Upgrade policy

The two pins are upgraded separately and explicitly.

### Implementation source baseline

An update must record:

- the full AndroidX commit hash and commit date;
- the `COMPOSE_MATERIAL3` version declared by that checkout;
- added, removed, stabilized, and behaviorally changed Material3 APIs;
- affected token tables and components;
- changes to [`component-matrix.md`](../coverage/component-matrix.md).

### Executable parity reference

An update is one atomic dependency-contract change. It must update the Gradle plugin and Material3
pins, dependency lock file, checksum metadata, producer manifest, C# manifest validator, and parity
documentation together. It must not regenerate or approve an Avalonia image as the expected
result.

## Authoritative locations

| Information | Authoritative location |
|---|---|
| AndroidX implementation survey | [`androidx-material3.md`](androidx-material3.md) |
| Executable parity workflow and environment | [`visual-parity/README.md`](../testing/visual-parity/README.md) |
| Compose dependency pins | [`tests/ComposeReference`](../../tests/ComposeReference) |
| Feature-level port status | [`component-matrix.md`](../coverage/component-matrix.md) |
| Forward work and priorities | [`ROADMAP.md`](../../ROADMAP.md) |
