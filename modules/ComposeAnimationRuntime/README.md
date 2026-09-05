# ComposeAnimationRuntime

`ComposeAnimationRuntime` is a native .NET animation engine for Avalonia-era applications. It follows the capability model of Jetpack Compose animation while using C# value types, explicit clocks, and imperative state objects.

The solution contains two runtime packages. `ComposeAnimationRuntime` is UI-independent and can be sampled from tests, services, or other renderers. `ComposeAnimationRuntime.Avalonia` adds Avalonia property binding, common Avalonia value converters, and a `TopLevel.RequestAnimationFrame` host.

The current alpha includes:

- vectorized values through `IAnimationVectorConverter<T>`;
- retargetable `Animatable<T>` values with value and velocity continuity;
- spring, tween, and snap specifications;
- exponential decay specifications for fling-style motion;
- deterministic manual clocks for coordinated frame stepping;
- shared `IAnimationClock` abstraction for deterministic and Avalonia-driven execution;
- keyframe segments with per-segment easing;
- finite and infinite repeat specifications;
- lightweight state transition coordination;
- Avalonia property binding and compositor frame hosting;
- arbitrary-dimensional vectors and custom stateless trajectory specifications;
- elliptical arc trajectories;
- visibility enter/exit state coordination;
- Avalonia `AnimatedVisibility`, `AnimatedContent`, `AnimatedSize`, and `AnimatedBounds` controls;
- shared transition roots, participant pairing, shared bounds, and shared elements;
- geometry and render-snapshot shared element modes;
- pointer gesture surfaces with bounded drag, fling decay, and spring settling;
- state-driven `AnimatedContent` factories with key-based identity;
- layout snapshots and size-transform effects for enter/exit motion;
- deterministic frame stepping through `Animatable<T>.Tick`;
- an independent test project.

The public surface stays intentionally small while the semantics are being established. Avalonia frame-clock adapters and higher-level content transitions remain separate additions so the core stays usable without a UI dependency.

```csharp
var motion = new Animatable<float>(0, new ScalarVectorConverter());
var spring = new SpringAnimationSpec<float>(new ScalarVectorConverter(), dampingRatio: 0.8f, stiffness: 380f);

motion.AnimateTo(1, spring);
motion.Tick(TimeSpan.FromMilliseconds(16));
```

The project is self-contained under `modules/ComposeAnimationRuntime` and is not referenced by the Material3Expressive solution.

The standalone demo lives under `demo/ComposeAnimationRuntime.Demo` and can be started with:

```powershell
dotnet run --project demo/ComposeAnimationRuntime.Demo
```

Avalonia integration is explicit:

```csharp
using Avalonia.Controls;
using ComposeAnimationRuntime;

var opacity = new Animatable<double>(0, converter);
using var binding = MotionBinding.Bind(control, Visual.OpacityProperty, opacity);
using var host = new AvaloniaMotionHost((TopLevel)control.GetVisualRoot()!);
host.Register(opacity);
opacity.AnimateTo(1, new TweenSpec<double>(converter, TimeSpan.FromMilliseconds(180)));
host.RequestFrame();
```

Gesture-driven motion is available without coupling the core runtime to Avalonia:

```csharp
var gesture = new GestureController();
gesture.SetBounds(Vector2.Zero, new Vector2(320, 640));
gesture.Begin(pointer, timestamp);
gesture.Update(pointer, timestamp);
gesture.End(settleTarget: Vector2.Zero);
```

For deterministic tests or custom renderers, use the same clock contract:

```csharp
IControllableAnimationClock clock = new ManualAnimationClock();
using var track = clock.Register(animation.Tick, () => animation.IsRunning);
clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
```

`AnimatedContent` is state-driven. `TargetState` is mapped to a visual by
`ContentFactory`; `KeySelector` controls identity, so updates with the same key
replace content in place while different keys run the configured transition:

```csharp
var content = new AnimatedContent
{
    ContentFactory = state => new TextBlock { Text = state?.ToString() },
    KeySelector = state => state,
    Transform = new ContentTransform(EnterTransition.FadeIn(), ExitTransition.FadeOut())
};

content.TargetState = "Loading";
content.TargetState = "Ready";
```
