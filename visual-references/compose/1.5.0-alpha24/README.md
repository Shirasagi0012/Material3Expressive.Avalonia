# Compose Material 3 reference set

This directory accepts screenshots only from the capture contract in
`manifest.json`: the released Compose Material 3 `1.5.0-alpha24`
`material3-android` AAR with SHA-256
`2D02D84CE0A73B604B6393738C1076B79B97BF33E3B4A4DB80502D75C2BAA09F`,
API 35, mdpi, `en-US`, and Roboto preset weights. The local capture device is
the `Pixel_9_Pro` API 35 AVD;
the capture setup overrides its native 480 dpi density to 160 dpi and records
that effective density in `capture.json`.

The current reviewed plan contains 40 cases: the original 15 foundation and
component probes plus 25 first-wave typography, text-field and navigation cases.
All 40 have been captured twice independently with identical PNG hashes.

`manifest.json` is the reviewed case plan. `reference-index.json` and
`cases/**/*.png` are produced by the importer after it verifies the capture
metadata, dimensions, SHA-256 hashes, the exact manifest hash and an exact match
between the captured and planned case-ID sets. The Android test reads the same
staged manifest and refuses an incomplete or extra scene registry. Hand-copying
a PNG into this directory does not make it a valid reference.
