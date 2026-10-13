# ComposeAnimation.Avalonia

Avalonia host for the ComposeAnimation runtime: `AnimationScope` (per-control scopes, frame
environment, rebuild and resume), `Animated.Properties`, `AnimatedVisibility`, `AnimatedContent`,
`AnimatedContentSize`, the cooperative Lookahead containers, `AnimatedBounds` and SharedTransition.

Start here (Chinese, with diagrams): [docs/usage](../docs/usage/README.md).

- Scope model, rebuild rules, D17 resume, `MoveContent`, known limits: [animation-scope.md](../docs/usage/animation-scope.md)
- Components: [docs/usage/components](../docs/usage/components/)
- Migrating from `VisualAnimationScope`: [migration guide](../docs/usage/migration-from-visual-animation-scope.md)
- Troubleshooting: [advanced/troubleshooting.md](../docs/usage/advanced/troubleshooting.md)

Quick start:

```csharp
public MainWindow()
{
    AnimationScope.Initialize(this);   // before loading content, once per TopLevel
    InitializeComponent();
}

// In a control, at use time (never dispose a Get scope):
_ = _alpha.AnimateToAsync(AnimationScope.Get(this), 1f, spec);
```

Notes on combining with Avalonia `Transitions` on the same property, and other known
interactions, are in the usage docs. Pre-refactor design notes live in `docs/draft` (historical).
