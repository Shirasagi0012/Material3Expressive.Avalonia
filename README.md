# Material 3 Expressive for Avalonia

This is the active rewrite of Material 3 Expressive for Avalonia. The solution contains the
rewrite library, its small catalog, and its tests. The previous implementation and its
documentation remain available in Git history and the `refactor/1` checkout.

## Current scope

- Foundation: typed, reference-identity token keys; Reference, System, and Component token
  sources; resource-chain overrides; colour, shape, typography, elevation, state, and motion values.
- Rendering and interaction: `MaterialSurface`, Material Symbols, state layers, ripple, and
  pressed-corner animation.
- Components: Button and IconButton variants and sizes.
- Animation uses ComposeAnimation VisualAnimationScope for fixed root frames, lifetime,
  tasks and value consumption. Material owns elevation, ripple and shape interaction rules.
- No Compose visual parity harness is connected to this rewrite yet.

The implementation details and next work are recorded in [docs/START_HERE.md](docs/START_HERE.md).
That document describes this rewrite; older architecture and roadmap documents were removed from
this branch because they described a different implementation.

## Work locally

The repository uses the MaterialColorUtilities and MaterialSymbols Git
submodules. Initialize them before building:

```powershell
git submodule update --init --recursive
dotnet restore Material3Expressive.slnx
dotnet build Material3Expressive.slnx --no-restore
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore
dotnet run --project src/Material3Expressive.Catalog
```

**Fresh-clone limitation:** the current superproject commit pins at least one submodule commit
that is present in the original local checkout but unavailable from its configured remote.
Publish or replace those pins before relying on a fresh clone or re-enabling CI.
