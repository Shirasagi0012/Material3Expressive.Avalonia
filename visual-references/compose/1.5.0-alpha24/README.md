# Compose Material 3 reference set

This directory accepts screenshots only from the capture contract in
`manifest.json`: the released Compose Material 3 `1.5.0-alpha24`
`material3-android` AAR with SHA-256
`2D02D84CE0A73B604B6393738C1076B79B97BF33E3B4A4DB80502D75C2BAA09F`,
API 35, mdpi, `en-US`, and Roboto preset weights. The local capture device is
the `Pixel_9_Pro` API 35 AVD;
the capture setup overrides its native 480 dpi density to 160 dpi and records
that effective density in `capture.json`.

`manifest.json` is the reviewed case plan. `reference-index.json` and
`cases/**/*.png` are produced by the importer after it verifies the capture
metadata, dimensions, and SHA-256 hashes. Hand-copying a PNG into this directory
does not make it a valid reference.
