# Animatable\<T\>：可打断的值动画持有者

`Animatable<T>` 是 `ComposeAnimation.Runtime` 的主类型，对标 Jetpack Compose 的 `Animatable`：一个同时持有当前值与速度的动画值持有者，通过 `AnimateToAsync` 等异步方法把值驱动到目标。它内部以 `MutatorMutex` 保证互斥——同一时刻最多只有一个动画在写入；新动画启动时会取消在途动画，并从当前值、当前速度无缝接续，因此值的变化始终连续。

`Animatable<T>` 继承 CommunityToolkit.Mvvm 的 `ObservableObject`，`Value`、`Velocity`、`IsRunning` 等属性均实现 `INotifyPropertyChanged`，可直接被 Avalonia 绑定消费。

> 前置概念——帧时钟（`IFrameSource`）、时钟线程模型、Task 异步与 `OperationCanceledException` 中断、`MotionDurationScale`——在 [运行时概览](./overview.md) 有完整说明，本篇不再展开。

## 快速开始

```csharp
using ComposeAnimation;
using ComposeAnimation.Runtime;

// 根环境已在窗口加载内容前初始化；control 已附着
var motion = AnimationScope.Get(control);
var animatable = motion.CreateAnimatable(0d);

// animationSpec 缺省为弹簧；这里显式给一个 64ms 的补间
AnimationResult<double> result = await animatable.AnimateToAsync(
    100, new TweenSpec<double>(duration: TimeSpan.FromMilliseconds(64)));

if (result.EndReason == AnimationEndReason.Finished)
{
    // 动画自然结束：animatable.Value == 100，Velocity 已归零
}
```

例子取自 `ComposeAnimation.Test/AnimatableTest.cs` 的 `AnimateTo_CompletesWithFinishedAndReachesTarget`（测试中改用 `ManualFrameSource` 逐帧步进；应用里时钟由 UI 帧自动驱动，无需手动步进）。生产环境的完整用法——创建、接续动画、捕获取消、监听 `PropertyChanged`——见控件层 `ComposeAnimation.Avalonia/Lookahead/LookaheadPosition.cs`。

## API 要点

### 构造

| 成员 | 类型/签名 | 默认值 | 说明 |
|---|---|---|---|
| `Animatable` | `Animatable(ITwoWayConverter<T> typeConverter, T initialValue, IFrameSource clock, T? visibilityThreshold = default, string? label = null)` | — | `typeConverter` 决定值的向量化维度（见 [类型转换器](./converters-and-easing.md)）；`visibilityThreshold` 只作用于缺省弹簧 spec 的结束判定；`label` 用于在工具中区分动画 |

### 属性

`Value`、`Velocity`、`IsRunning`、`TargetValue`、`LowerBound`、`UpperBound` 的变化都会触发 `PropertyChanged`；`Label` 构造后不变、`State` 每次返回新副本，二者不发通知。

| 成员 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `Value` | `T` | `initialValue` | 当前值，动画每帧更新 |
| `Velocity` | `T` | 0 | 当前速度；动画结束后归零 |
| `IsRunning` | `bool` | `false` | 是否有动画在途 |
| `TargetValue` | `T` | `initialValue` | 在途动画的目标；不被打断时最终会到达该值 |
| `LowerBound` / `UpperBound` | `T?` | 无边界 | 由 `UpdateBounds` 设置；**任一**维度触界动画即停 |
| `Label` | `string` | `"Animatable"` | 可选标识 |
| `State` | `AnimationState<T>` | — | 内部状态的**快照副本**（含全精度 `VelocityVector`、帧时间等），修改副本不影响动画 |

### 方法

全部为异步、可互相打断：被打断一方的任务以 `OperationCanceledException` 完成（调用方传入的 `CancellationToken` 触发取消时亦然）。

| 成员 | 签名 | 说明 |
|---|---|---|
| `AnimateToAsync` | `Task<AnimationResult<T>> AnimateToAsync(T targetValue, IAnimationSpec<T>? animationSpec = null, Action<Animatable<T>>? block = null, MutatePriority priority = MutatePriority.Default, CancellationToken cancellationToken = default)` | 从当前值动画到目标值；`animationSpec` 缺省为弹簧。`block` 每帧回调，参数即 `Animatable<T>` 本身，可读 `Value`/`Velocity` |
| `AnimateToAsync`（带初速） | `Task<AnimationResult<T>> AnimateToAsync(T targetValue, IAnimationSpec<T>? animationSpec, T initialVelocity, Action<Animatable<T>>? block = null, MutatePriority priority = MutatePriority.Default, CancellationToken cancellationToken = default)` | 显式指定初始速度的重载（此重载中 `animationSpec` 不可省略） |
| `AnimateDecayAsync` | `Task<AnimationResult<T>> AnimateDecayAsync(T initialVelocity, IDecayAnimationSpec<T> animationSpec, Action<Animatable<T>>? block = null, MutatePriority priority = MutatePriority.Default, CancellationToken cancellationToken = default)` | 从初速度开始衰减直至静止，常用于 fling 手势之后；规格见 [动画规格](./animation-specs.md) |
| `SnapToAsync` | `Task SnapToAsync(T targetValue, CancellationToken cancellationToken = default)` | 取消在途动画并立即设值；设了边界时先夹取 |
| `Stop` | `void Stop()` | 原地取消在途动画（不跳到目标），立即返回；是取消信号而非变更，**可以在帧回调中调用** |
| `StopAsync` | `Task StopAsync()` | 取消并等待动画完全回卷；**不要在帧回调里调用**（会等待自身） |
| `UpdateBounds` | `void UpdateBounds(AnimatableBounds<T> bounds)` | 更新下/上界。在途动画从下一帧生效，触界时以 `BoundReached` 结束并夹取；空闲时立即夹取当前值。任一维度下界大于上界抛 `ArgumentException` |

### 相关类型

| 类型 | 定义 | 说明 |
|---|---|---|
| `AnimationResult<T>` | `readonly record struct (AnimationState<T> EndState, AnimationEndReason EndReason)` | 动画正常结束时的结果；`EndState` 捕获末帧的值、速度与帧时间 |
| `AnimationEndReason` | `enum { BoundReached, Finished }` | `BoundReached`：被边界夹停；`Finished`：自然播完 |
| `MutatePriority` | `enum { Default, UserInput, PreventUserInput }` | 变更优先级：同级或更高优先级打断在途动画；更低优先级被直接拒绝（任务以取消完成） |
| `Animatable<T>.AnimatableBounds<T>` | `readonly record struct (Bound<T> Lower, Bound<T> Upper)` | 边界。静态 `Unbounded`；实例方法 `WithLower(T)` / `WithUpper(T)` / `WithoutLower()` / `WithoutUpper()` |
| `Animatable<T>.AnimatableBounds<T>.Bound<T>` | `readonly record struct (T Value)` | 单侧边界；`HasValue`、`Unbounded`；存在从 `T` 的隐式转换，可直接赋裸值 |

## 常见模式/进阶用法

### 打断与 OperationCanceledException

新动画启动即打断旧动画；旧任务抛 `OperationCanceledException`，**必须捕获**，否则会在 `await` 处冒出未处理异常：

```csharp
var tween = new TweenSpec<double>(duration: TimeSpan.FromMilliseconds(100));
var first = animatable.AnimateToAsync(100, tween);

// ……若干帧之后，值已走到 0~100 之间，此时下达新目标
var second = animatable.AnimateToAsync(200, tween);

try
{
    await first;
}
catch (OperationCanceledException)
{
    // first 被取消；second 从当前值、当前速度接续，且第一帧在旧动画的
    // 时间基上同步处理，不打断运动的连续性
}

var result = await second; // EndReason == Finished，Value == 200
```

（取自 `AnimatableTest.cs` 的 `AnimateTo_Interrupted_RestartsFromCurrentValueOnOldTimebase`。）

### 用 PropertyChanged 观察值

`Value`、`Velocity`、`IsRunning`、`TargetValue` 等都会触发 `PropertyChanged`。控件层 `LookaheadPosition` 就是这样驱动布局失效的：

```csharp
animatable.PropertyChanged += (_, e) =>
{
    if (e.PropertyName is nameof(Animatable<double>.Value) or nameof(Animatable<double>.IsRunning))
    {
        // 值变了或动画启停了：刷新依赖该值的内容
    }
};
```

### 边界：UpdateBounds 与 BoundReached

```csharp
animatable.UpdateBounds(new(-20, 90));   // 位置构造：下界 -20、上界 90（Bound<T> 有从 T 的隐式转换）

await animatable.SnapToAsync(500);       // 越界，立即被夹取到 90

var result = await animatable.AnimateToAsync(
    -100, new TweenSpec<double>(duration: TimeSpan.FromMilliseconds(320)));
// 到达下界时动画提前结束：result.EndReason == AnimationEndReason.BoundReached，
// animatable.Value == -20
```

只设单侧可用对象初始化器（`UpdateBounds(new() { Upper = 50.0 })`），或从 `Unbounded` 派生：`UpdateBounds(Animatable<double>.AnimatableBounds<double>.Unbounded.WithUpper(50))`（嵌套类型重声明了 `T`，全名较长，目标类型推断的 `new(...)` 更常用）。运行中调用从下一帧生效；空闲时调用立即夹取当前值。（行为测试见 `AnimatableTest.cs` 的 `UpdateBounds_*`。）

### SnapToAsync / Stop / StopAsync

```csharp
animatable.Stop();          // 原地冻结：Value 停在当前位置，在途任务以取消完成
await animatable.StopAsync(); // 同上，但等动画完全回卷后再返回
await animatable.SnapToAsync(30); // 取消在途动画并立即跳到 30（越界则夹取）
```

三者的共同点：被它们取消的动画任务都抛 `OperationCanceledException`。

### 变更优先级 MutatePriority

```csharp
var running = animatable.AnimateToAsync(100, tween, priority: MutatePriority.PreventUserInput);

// 优先级低于在途动画的变更会被直接拒绝，任务立即以取消完成
try
{
    await animatable.AnimateToAsync(200, tween);
}
catch (OperationCanceledException)
{
    // 被拒绝：running 不受影响，animatable.TargetValue 仍是 100
}
```

同级或更高优先级则打断在途动画。典型用法：用户拖拽用 `UserInput` 启动动画，程序化动画保持 `Default`，拖拽期间程序化变更自然被拒绝。（取自 `AnimatableTest.cs` 的 `AnimateTo_WithLowerPriority_IsRejected`。）

### 衰减动画 AnimateDecayAsync

```csharp
// 从 500/秒 的初速度指数衰减到静止，常用于 fling 手势松手之后
var result = await animatable.AnimateDecayAsync(
    500, DecayAnimationSpec.ExponentialDecay<double>());
// result.EndReason == Finished；终值由物理决定，不在调用时指定
```

衰减规格（`ExponentialDecay`、`SplineBasedDecay`、落点预测 `CalculateTargetValue`）见 [动画规格](./animation-specs.md#衰减规格decayanimationspec)。

### 一次性动画与低层 AnimationState

不需要互斥、也不需要长期持有状态时，可以直接在时钟上跑一次性动画，值通过每帧回调消费：

```csharp
// double 便捷重载；泛型重载 clock.AnimateAsync<T>(converter, from, to, ...) 适用于任意类型
await clock.AnimateAsync(0, 100,
    animationSpec: new TweenSpec<double>(duration: TimeSpan.FromMilliseconds(64),
        easing: new LinearEasing()),
    block: (value, velocity) => { /* 每帧回调 */ });
```

再低一层是 `AnimationState<T>`（可变状态持有者）+ `SuspendAnimation` 静态扩展：

```csharp
var state = new AnimationState<double>(new DoubleConverter(), 0);
await state.AnimateToAsync(100, clock,
    new TweenSpec<double>(duration: TimeSpan.FromMilliseconds(64)));
// state.Value == 100，state.IsFinished == true

// 接续上一个动画的动量：以 state.LastFrameTime 为起点，第一帧同步处理
await state.AnimateToAsync(200, clock, spec, sequentialAnimation: true);
```

**注意**：`SuspendAnimation` 系列（`AnimationState<T>.AnimateToAsync` / `AnimateDecayAsync`、`clock.AnimateAsync`）**没有任何互斥保护**——对同一个 `AnimationState<T>` 并发启动两个动画会交错写入帧值。需要打断安全时请用 `Animatable<T>`。每帧回调拿到的是 `AnimationRun<T>`（`Value`/`Velocity`/`TargetValue`/`CancelAnimation()` 等）。（取自 `SuspendAnimationTest.cs`。）

## 注意事项

- **线程**：`Animatable<T>` 的所有公共成员必须在帧时钟所属线程调用（实现内部 `VerifyAccess`），详见 [运行时概览](./overview.md)。`PropertyChanged` 也在该线程触发，可直接喂给 Avalonia 绑定。
- **OCE 不是错误**：打断是常态语义，所有 `await` 动画任务的地方都要准备好捕获取消；`catch (OperationCanceledException)` 后不要吞掉新动画的任务。
- **`visibilityThreshold` 构造参数**只进入缺省弹簧 spec：弹簧在「各分量离目标的距离与速度都低于阈值」时判定结束。位移类值（如 `Point`）建议传 `VisibilityThresholds.Point` 之类的类型化阈值，否则会用到非常紧的 0.01 默认值、动画尾巴拖长，见 [类型转换器与缓动](./converters-and-easing.md)。
- **`block` 每帧触发**，拿到 `Animatable<T>` 本身；在其中读取 `Value`/`Velocity` 是安全的。在 `block` 里想提前结束可用 `Stop()`（不要 `await StopAsync()`，会等自身）。
- **等值不抑制通知**：延迟、整数取整、颜色舍入期间 `Value` 可能逐帧相等，但底层速度向量仍在变化，`PropertyChanged` 依旧每帧触发——不要假设「通知即值变」。
- **速度归零**：动画（含衰减）正常结束后 `Velocity` 重置为 0；需要末速度请从 `AnimationResult<T>.EndState` 取。
- **`MotionDurationScale`** 可整体缩放播放速度（0 表示有限动画下一帧直接到终点），见 [运行时概览](./overview.md)。
- **不要跨实例共享时钟期待互斥**：互斥是 per-`Animatable<T>` 的；多个 `Animatable<T>` 共享一个时钟时，注意时钟实现是否允许多个挂起帧请求（UI 消费者使用同根 Scope 的自动共享；独立 Runtime 时钟由调用者保证多消费者能力）。

## 延伸阅读

- [运行时概览](./overview.md)：帧时钟、线程模型、Task 异步与中断、`MotionDurationScale`
- [动画规格](./animation-specs.md)：`SpringSpec`、`TweenSpec`、`KeyframesSpec`、衰减等全部规格
- [类型转换器与缓动](./converters-and-easing.md)：`ITwoWayConverter<T>`、`VisibilityThresholds`、`ComposeEasings`
- [Transition](./transitions.md)：面向「状态 ↔ 值」编排的高层 API
- [返回使用文档入口](../README.md)
