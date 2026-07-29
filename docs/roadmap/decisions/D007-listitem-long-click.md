# D007 — ListItem long-click contract

Status: Accepted  
Date: 2026-07-30

Use the existing `InteractionMode` API. Interactive rows expose `LongClick`, `LongClickCommand`,
`LongClickCommandParameter`, and `LongClickLabel`; `None` remains gesture-free. Avalonia
`Holding`, including mouse, is the desktop gesture and consumes the later primary click.
`LongClickLabel` supplies fallback automation help text.
