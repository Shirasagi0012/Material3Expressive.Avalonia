# Visual parity testing

The legacy imported-reference visual-test workflow has been removed. Visual
parity uses a freshly generated Compose Multiplatform expected bundle for each
run; generated images and layout data are run artifacts, never committed
baselines.

The authoritative design and current migration plan are in
[visual-parity-test-plan.md](visual-parity-test-plan.md). The Compose producer
is documented in [tests/ComposeReference/README.md](../tests/ComposeReference/README.md).
