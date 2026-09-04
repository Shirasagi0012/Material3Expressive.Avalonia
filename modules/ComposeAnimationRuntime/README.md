# ComposeAnimationRuntime

`ComposeAnimationRuntime` is a native .NET animation engine for Avalonia-era applications. It follows the capability model of Jetpack Compose animation while using C# value types, explicit clocks, and imperative state objects.

The solution contains two runtime packages. `ComposeAnimationRuntime` is UI-independent and can be sampled from tests, services, or other renderers. `ComposeAnimationRuntime.Avalonia` adds Avalonia property binding, common Avalonia value converters, and a `TopLevel.RequestAnimationFrame` host.

The current alpha includes:

- vectorized values through `IAnimationVectorConverter<T>`;
- retargetable `Animatable<T>` values with value and velocity continuity;
- spring, tween, and snap specifications;
- exponential decay specifications for fling-style motion;
- deterministic manual clocks for coordinated frame stepping;
- keyframe segments with per-segment easing;
- finite and infinite repeat specifications;
- lightweight state transition coordination;
- Avalonia property binding and compositor frame hosting;
- arbitrary-dimensional vectors and custom stateless trajectory specifications;
- elliptical arc trajectories;
- visibility enter/exit state coordination;
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
