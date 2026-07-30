# D001 — Adaptive navigation package boundary

Status: Open
Blocks: M5 adaptive navigation tasks
Decision owner: User

Question: Should adaptive navigation ship in the core package or a separate package/namespace?

## Current architecture

- All controls, themes, state helpers, and XAML styles currently ship from the single
  `Shirasagi0012.Material3Expressive.Avalonia` package.
- Public controls use `Material3Expressive.Controls` and share one XAML namespace URI.
- Existing navigation bar, rail, drawer, scaffold, theming, shape, motion, and automation
  dependencies are already in the core package. M5 does not introduce a distinct runtime or
  platform dependency.
- The package is still `0.1.0-alpha`; D005 has not established a source-compatibility boundary
  that would justify locking in cross-package versioning.

## Options

### A — Core package and existing Controls namespace

Add FloatingToolbar, ShortNavigationBar, WideNavigationRail, and adaptive coordination to the
current assembly, `Material3Expressive.Controls` namespace, XAML URI, Catalog, and package.

- Preserves one install, theme graph, XML namespace, and version.
- Matches every existing Material component, including navigation and scaffold primitives.
- Leaves open a later package split if a real dependency or release-cadence boundary appears.

### B — Core package with an Adaptive subnamespace

Keep one package but publish these APIs under `Material3Expressive.Controls.Adaptive`, optionally
mapping that CLR namespace to the existing XAML URI.

- Gives a code-level grouping without a second artifact.
- Adds namespace and XML mapping complexity while the controls still depend on core primitives.
- Makes later moves source-breaking unless D005 first defines compatibility policy.

### C — Separate adaptive package

Create a second project/NuGet package depending on the core package.

- Allows independent installation and versioning.
- Requires new package/build/versioning/documentation infrastructure and coordinated theme/XAML
  loading, with no current dependency or size benefit.

## Recommendation

Choose **A: core package and `Material3Expressive.Controls`**. Treat adaptive as a component
capability, not a deployment boundary. Revisit a package split only if a concrete optional
dependency, platform restriction, or independent release cadence appears.

## Confirmation required

The owner must select A, B, or C. Do not infer acceptance from this recommendation.
