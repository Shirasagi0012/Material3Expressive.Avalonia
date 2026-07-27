# Visual parity plan

The visual test system uses the official pinned Compose Material 3 implementation as its only
reference. Avalonia self-regression baselines, candidate recording and approval workflows have
been removed.

## Current flow

```text
Pinned Compose Material 3 + capture contract
                    |
                    v
             Versioned reference PNGs
                    |
                    +---- same case ID, viewport, colors, font and content ----+
                    |                                                          |
                    v                                                          v
            Compose expected image                                  Avalonia actual image
                    |                                                          |
                    +---------------------- parity comparison ----------------+
                                                   |
                                                   v
                                 HTML report, regions and actionable differences
```

The reference is `androidx.compose.material3:material3-android:1.5.0-alpha24`, captured on the
fixed API 35 mdpi environment specified in `visual-references/compose/1.5.0-alpha24/manifest.json`.
Reference provenance includes the AAR, color contract, environment and PNG hashes.

## Scope

- Capture and import Compose reference cases only when their contract matches exactly.
- Render the corresponding Avalonia case under the same viewport, theme, color contract, font,
  locale and direction.
- Report container geometry, edge alignment, perceptual color difference, foreground bounds and
  declared text/icon/container regions.
- Use the Compose image as expected output; never approve an Avalonia render as a baseline.

The next iteration should define a small set of component-region limits that turn an actionable
parity difference into a test failure. Cross-renderer rendering differences that cannot be
eliminated must be named in `docs/parity.md`, not masked anonymously.
