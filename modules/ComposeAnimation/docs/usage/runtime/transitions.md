# 状态过渡：`Transition<TState>` 与 `InfiniteTransition`

`Transition<TState>` 是 ComposeAnimation 运行时的状态驱动过渡系统，对标 Jetpack Compose 的 `Transition`：它把"目标状态"的变化翻译成一组共享同一时间线的动画。一棵过渡由叶子动画（`TransitionAnimationState<TValue>`）和子过渡（`Transition<TChild>`）组成，整棵树随根过渡的帧循环推进，直到最长的动画结束。

`InfiniteTransition` 是与之并列的无限循环动画驱动器，对标 Compose 的 `InfiniteTransition`：子动画加入同一条父时间线（保持相位），永不自行结束，适合加载指示器、呼吸灯这类常驻效果。

两者都位于 `ComposeAnimation.Runtime` 程序集的 `ComposeAnimation.Runtime` 命名空间。选择建议：

- 一组属性随同一状态切换而联动 → `Transition<TState>`
- 永不停止的循环效果 → `InfiniteTransition`
- 只动画单个值、不需要状态机 → 参见 [Animatable](./animatable.md)

帧时钟、线程模型、`Task` 异步与 `OperationCanceledException` 中断、`MotionDurationScale` 等前置概念见 [引擎总览](./overview.md)，本文不再重复。

## 快速开始

在 Avalonia 窗口中，以 `TopLevel` 的动画帧作为时钟：

```csharp
using Avalonia;
using Avalonia.Animation.Easings;
using ComposeAnimation;
using ComposeAnimation.Runtime;

// this 为 Window（TopLevel）。构造函数必须在时钟所属线程（UI 线程）调用
var state = new MutableTransitionState<bool>(false);
using var motion = VisualAnimationScope.Create(this); // 根环境已在加载内容前初始化
var transition = motion.CreateTransition(state, label: "Demo");

var alpha = transition.AnimateDouble(
    s => s ? 1.0 : 0.0,
    _ => new TweenSpec<double>(TimeSpan.FromMilliseconds(300), easing: new CubicEaseInOut()),
    label: "Alpha");

var position = transition.AnimatePoint(
    s => s ? new Point(120, 60) : default,
    label: "Position");   // 省略 transitionSpec 时默认使用弹簧

// 帧循环只在根过渡上运行一次；子过渡随父级时间线运行，不要对子过渡调用 RunAsync
_ = motion.Launch(ct => transition.RunAsync(ct));

// 改变目标状态即触发过渡。动画值逐帧更新并触发 PropertyChanged，可直接用于绑定
state.TargetState = true;
```

读取与观察动画值：

```csharp
alpha.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(alpha.Value))
        Console.WriteLine(alpha.Value);
};
```

销毁时 `Dispose` 过渡，帧循环随之取消：

```csharp
transition.Dispose();   // RunAsync 返回的任务以 OperationCanceledException 结束
```

也可以在创建时直接传初始状态，由过渡内部创建状态对象：

```csharp
using var transition = new Transition<bool>(false, clock);   // 等价于内部 new MutableTransitionState<bool>(false)
transition.UpdateTarget(true);   // 没有外部状态时，用 UpdateTarget 改目标
```

## 核心概念

### 过渡树与共享时间线

- `AnimateValue`（或 `AnimateDouble` 等类型化扩展）在当前过渡上注册叶子动画；`CreateChildTransition` 派生子过渡，二者共同组成一棵树。
- 整棵树共享一条播放时间线：根过渡的 `PlayTime` 从 0 推进到 `TotalDuration`（全树最长动画的时长），子过渡的 `PlayTime` 与父级相同。
- 不同时长的动画并行播放，过渡在最长的动画结束后才完成：完成时 `CurrentState` 变为 `TargetState`，根 `PlayTime` 归零，`IsRunning` 变回 `false`。
- 过渡运行中晚注册的动画从父级当前 `PlayTime` 起算（内部以起始延迟包装），与早注册的动画保持同一时间基准。
- `IsPreparing` 表示自身或某个后代持有准备租约（见 [准备租约](#准备租约beginpreparation)），此时过渡不会判定完成。

### 状态、目标与段

- `CurrentState`：过渡当前到达的状态；`TargetState`：正在前往的目标状态。
- `Segment` 是一个 `TransitionSegment<TState>`（含 `InitialState` 与 `TargetState`），描述当前这一"段"的端点；`Segment.IsTransitioningTo(a, b)` 用于按段匹配。
- 运行途中改目标时，新段从**旧目标**开始（`CurrentState` 立即变为旧目标），与 Compose 的行为一致：

```csharp
transition.UpdateTarget(1);
// …动画进行中…
transition.UpdateTarget(2);
// transition.Segment == new TransitionSegment<int>(1, 2)
// transition.CurrentState == 1
```

- `PendingTargetState` / `HasPendingTargetState` 只在状态对象为 `DeferredTransitionState<TState>` 时有值（见 [三种状态类](#三种状态类mutable--deferred--seekable)）。

### 中断与速度连续性

运行中的动画被重定向时不会跳变：新动画保留当前值与速度向量继续。若新段选出的规格不是弹簧（`SpringSpec<TValue>`），运行时会改用一支中断弹簧（可见性阈值取自 `VisibilityThresholds.For(converter)`）来完成这次接力；若新规格本身是弹簧则直接使用它。这也是 tween 被打断时不会突兀折返的原因。

速度可从动画状态上读取：`Velocity`（转换为 `TValue`）与 `VelocityVector`（`ReadOnlySpan<double>`，保留 `int`、`Color` 等转换器的未舍入精度）。

### 目标值与规格的求值时机

`targetValueByState` 与 `transitionSpec` 是普通 C# 闭包，在动画（重）配置时求值，而不是被快照系统观察。推论：

- 改成相同目标不会重启动画（复用同一个 `Animation` 实例）。
- 闭包捕获的外部数据变化后，需要显式调用 `RefreshAnimations()` 让整棵树重新求值：

```csharp
var distance = 100.0;
var value = transition.AnimateDouble(s => s ? distance : 0, _ => new TweenSpec<double>(TimeSpan.FromMilliseconds(100)));
transition.UpdateTarget(true);

distance = 200;                    // 闭包捕获的外部输入变了
transition.RefreshAnimations();    // 重新求值，动画重定向到 200
```

也可以用动画状态的 `Update(...)` 只替换某一个动画的映射/规格，见 [API 要点](#transitionanimationstatetvalue)。

### 帧循环与生命周期

- `RunAsync(CancellationToken)` 是根过渡的帧循环：只能在**根**上调用（子过渡调用抛 `InvalidOperationException`），且同一时刻只能有一个循环。
- 循环空闲时不请求帧；目标变更、注册动画等变化会自动唤醒它。
- 取消令牌或 `Dispose()` 都会结束循环，任务以 `OperationCanceledException` 完成；取消只是停止推进，不会把状态/值吸附到终点。
- 所有公共成员都必须在时钟所属线程调用（UI Scope 即 UI 线程），详见 [引擎总览](./overview.md)。

## API 要点

### `Transition`（抽象基类）

`abstract class Transition : ObservableObject, IDisposable`，全部属性变更通过 `PropertyChanged` 通知。

| 成员 | 类型/签名 | 说明 |
| --- | --- | --- |
| `ParentTransition` | `Transition?` | 父过渡；根为 `null` |
| `Label` | `string?` | 诊断用标签 |
| `IsRunning` | `bool` | 本节点（含子树）是否有进行中的工作 |
| `IsSeeking` | `bool` | 是否处于手动播放时间（seek）模式 |
| `IsPreparing` | `bool` | 自身或后代是否持有准备租约 |
| `PlayTime` | `TimeSpan` | 根的播放时间（共享时间线） |
| `Animations` | `IReadOnlyList<ITransitionAnimation>` | 本节点直接注册的动画；`ITransitionAnimation` 暴露 `Label`/`Duration`/`IsFinished` |
| `Transitions` | `IReadOnlyList<Transition>` | 子过渡列表 |
| `TotalDuration` | `TimeSpan` | 全树最长动画时长（含子过渡） |
| `DurationScale` | `double`，可读写 | 树范围时长倍率（读写都作用于根）；必须有限且 ≥ 0，否则抛 `ArgumentOutOfRangeException`；`0` 表示子动画下一帧直达终值 |
| `BeginPreparation()` | `TransitionPreparationLease` | 获取准备租约，阻止过渡完成；seeking 中调用抛 `InvalidOperationException` |
| `RunAsync(CancellationToken cancellationToken = default)` | `Task` | 根帧循环；子过渡或重复调用抛 `InvalidOperationException` |
| `RefreshAnimations()` | `void` | 重新求值全树的目标值/规格闭包 |
| `Dispose()` | `void` | 幂等；取消帧循环、撤销租约、分离子动画与子过渡；之后访问成员抛 `ObjectDisposedException` |

### `Transition<TState>`

`partial class Transition<TState> : Transition`。

| 成员 | 类型/签名 | 说明 |
| --- | --- | --- |
| 构造函数 | `Transition(TState initialState, IAnimationFrameClock clock, string? label = null)` | 内部创建 `MutableTransitionState<TState>` |
| 构造函数 | `Transition(TransitionState<TState> transitionState, IAnimationFrameClock clock, string? label = null)` | 使用外部持有的状态对象 |
| `CurrentState` / `TargetState` | `TState` | 当前状态 / 目标状态 |
| `Segment` | `TransitionSegment<TState>` | 当前段；`readonly record struct`，含 `InitialState`、`TargetState` 与 `IsTransitioningTo(TState, TState)` |
| `PendingTargetState` / `HasPendingTargetState` | `TState?` / `bool` | 延迟状态对象的挂起目标（见 `DeferredTransitionState<T>`） |
| `UpdateTarget(TState targetState)` | `void` | 请求新目标（等价于写状态对象的 `TargetState`） |
| `AnimateValue<TValue>(ITwoWayConverter<TValue> typeConverter, Func<TState, TValue> targetValueByState, Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>>? transitionSpec = null, string label = "ValueAnimation")` | `TransitionAnimationState<TValue>` | 注册叶子动画；`transitionSpec` 缺省为弹簧 |
| `CreateChildTransition<TChild>(Func<TState, TChild> transformToChildState, string label = "ChildTransition")` | `Transition<TChild>` | 派生子过渡，共享父时间线；实际标签为 `"{父Label} > {label}"` |
| `CreateDeferredAnimation<TValue>(ITwoWayConverter<TValue> typeConverter, string label = "DeferredAnimation")` | `DeferredAnimation<TValue>` | 创建延迟注册的动画占位 |
| `SetPlaytimeAfterInitialAndTargetStateEstablished(TState initialState, TState targetState, TimeSpan playTime)` | `void` | 把整棵树手动采样到指定播放时间；只能用于根，否则抛 `InvalidOperationException` |

### `TransitionAnimationState<TValue>`

`Transition<TState>` 的嵌套类：`sealed class TransitionAnimationState<TValue> : ObservableObject, IDisposable`。

| 成员 | 类型/签名 | 说明 |
| --- | --- | --- |
| `Value` | `TValue` | 当前值（逐帧更新） |
| `TargetValue` | `TValue` | 当前帧的目标值 |
| `Velocity` | `TValue` | 当前速度（经转换器还原） |
| `VelocityVector` | `ReadOnlySpan<double>` | 未舍入的速度向量 |
| `AnimationSpec` | `IFiniteAnimationSpec<TValue>` | 段选择器选出的规格 |
| `Animation` | `TargetBasedAnimation<TValue>` | 实际运行的动画（中断时内部可能是中断弹簧/起始延迟包装） |
| `Duration` | `TimeSpan` | `Animation.Duration` |
| `IsFinished` | `bool` | 是否已到达终点 |
| `Label` / `TypeConverter` | `string` / `ITwoWayConverter<TValue>` | 注册时给定 |
| `Update(Func<TState, TValue> targetValueByState, Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>>? transitionSpec = null)` | `void` | 替换映射闭包（可选一并替换规格选择器）并重新配置；规格为 `null` 时保留原选择器 |
| `Dispose()` | `void` | 从所属过渡中移除该动画 |

### `DeferredAnimation<TValue>`

`Transition<TState>` 的嵌套类，`IDisposable`。由 `CreateDeferredAnimation` 创建；首次 `Animate` 才真正把动画注册进过渡，之后再调用 `Animate` 复用同一个 `TransitionAnimationState<TValue>` 实例。

| 成员 | 签名 | 说明 |
| --- | --- | --- |
| `Animate` | `Animate(Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>> transitionSpec, Func<TState, TValue> targetValueByState)` | 注册/更新动画 |
| `Animate`（强制起始值重载） | `Animate(Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>> transitionSpec, TValue forcedInitialValue, AnimationVector? forcedInitialVelocity, Func<TState, TValue> targetValueByState)` | 以手工给定的起始值/起始速度开始（手势接力等）；`forcedInitialVelocity` 可为 `null` |
| `TypeConverter` / `Label` | 属性 | 创建时给定 |
| `Dispose()` | `void` | 释放占位；已注册的动画一并移除 |

### 类型化扩展

`TransitionExtensions` 为常见类型提供快捷方式，参数与 `AnimateValue` 相同（省去转换器），返回 `Transition<TState>.TransitionAnimationState<TValue>`：

| 方法 | `TValue` | 转换器 | 默认 `label` |
| --- | --- | --- | --- |
| `AnimateDouble` / `AnimateFloat` / `AnimateInt` | `double` / `float` / `int` | `DoubleConverter` / `FloatConverter` / `Int32Converter` | `"DoubleAnimation"` 等 |
| `AnimatePoint` / `AnimateVector` | `Avalonia.Point` / `Avalonia.Vector` | `PointConverter` / `VectorConverter` | `"PointAnimation"` 等 |
| `AnimateSize` / `AnimateRect` | `Avalonia.Size` / `Avalonia.Rect` | `SizeConverter` / `RectConverter` | `"SizeAnimation"` 等 |
| `AnimateThickness` / `AnimateCornerRadius` | `Avalonia.Thickness` / `Avalonia.CornerRadius` | `ThicknessConverter` / `CornerRadiusConverter` | `"ThicknessAnimation"` 等 |
| `AnimateColor` | `Avalonia.Media.Color` | `ColorConverter` | `"ColorAnimation"` |

### 状态类

`abstract class TransitionState<T> : ObservableObject`：`CurrentState` / `TargetState` 的 setter 均为内部，由过渡系统写回；具体能力在三个子类中：

| 类 | 构造 | 目标切换 | 进度控制 | 典型场景 |
| --- | --- | --- | --- | --- |
| `MutableTransitionState<T>` | `(T initialState)` | 直接写 `TargetState`；`IsIdle` 表示无目标差异且未运行 | 无（自动播放） | 常规状态切换动画 |
| `DeferredTransitionState<T>` | `(T initialState)` | `Defer(target)` 暂存目标（不动画），`AnimateTo(target)` 提交 | 无 | 需等待测量/资源就绪再提交的过渡；配合 `DeferredTransition<TState>` 与 `CreateDeferredAnimation` |
| `SeekableTransitionState<T>` | `(T initialState)` | `SeekToAsync` / `SnapToAsync` / `AnimateToAsync` | `Fraction`（0..1） | 手势驱动的进度（抽屉拖动）、设计器/预览 |

`DeferredTransition<TState>` 是 `sealed class DeferredTransition<TState>(DeferredTransitionState<TState> transitionState, IAnimationFrameClock clock, string? label = null) : Transition<TState>`，与后两者中的 `DeferredTransitionState` 搭配使用。

### `InfiniteTransition`

`sealed class InfiniteTransition : ObservableObject, IDisposable`。构造函数：`InfiniteTransition(IAnimationFrameClock clock, string? label = "InfiniteTransition")`。

| 成员 | 类型/签名 | 说明 |
| --- | --- | --- |
| `Label` / `IsRunning` / `PlayTime` | 属性 | 同 `Transition` 的语义；`PlayTime` 从 0 单调推进 |
| `Animations` | `IReadOnlyList<IInfiniteTransitionAnimation>` | 已注册动画；接口暴露 `Label`/`IsFinished`（无限动画没有 `Duration`） |
| `DurationScale` | `double`，可读写 | 必须有限且 ≥ 0。**改动会重启所有子动画**（与 `Transition` 不同）；`0` = 全部吸附到目标值并挂起帧循环，恢复正值后从头重启 |
| `AnimateValue<TValue>(TValue initialValue, TValue targetValue, ITwoWayConverter<TValue> typeConverter, InfiniteRepeatableSpec<TValue> animationSpec, string label = "ValueAnimation")` | `TransitionAnimationState<TValue>` | 注册无限动画 |
| `RunAsync(CancellationToken cancellationToken = default)` | `Task` | 帧循环；重复调用抛 `InvalidOperationException` |
| `Dispose()` | `void` | 幂等；取消循环、分离子动画；值不吸附 |

其子动画 `InfiniteTransition.TransitionAnimationState<TValue>`（`ObservableObject, IDisposable`）暴露 `Value`/`InitialValue`/`TargetValue`/`AnimationSpec`/`Animation`/`IsFinished`/`Label`/`TypeConverter`，并提供运行时重定向方法：

```csharp
void Update(TValue initialValue, TValue targetValue, InfiniteRepeatableSpec<TValue>? animationSpec = null)
```

`Update` 在下一帧从新初始值重启（无连续性）；初始值、目标值与规格都相同则是 no-op。

`InfiniteTransitionExtensions` 提供与 `Transition` 同名的一组类型化扩展（`AnimateDouble` … `AnimateColor`），签名为 `(this InfiniteTransition, TValue initialValue, TValue targetValue, InfiniteRepeatableSpec<TValue> animationSpec, string label)`。

## 常见模式与进阶用法

### 按 Segment 选择规格

`transitionSpec` 在每次（重）配置时以当前 `Segment` 求值，可以按"从哪来到哪去"选择不同规格：

```csharp
var alpha = transition.AnimateDouble(
    s => s ? 1.0 : 0.0,
    transitionSpec: segment => segment.IsTransitioningTo(false, true)
        ? (IFiniteAnimationSpec<double>)new TweenSpec<double>(TimeSpan.FromMilliseconds(200), easing: new CubicEaseInOut())
        : new SpringSpec<double>(Spring.DampingRatioLowBouncy, Spring.StiffnessLow));
```

### 子过渡

子过渡把父状态映射为子状态，共享父时间线；父级改目标时若映射结果不变，子动画不会重启：

```csharp
using var transition = new Transition<int>(0, clock, "Parent");
transition.AnimateDouble(s => s, _ => new TweenSpec<double>(TimeSpan.FromMilliseconds(50)));

var child = transition.CreateChildTransition(s => s > 0, "Visible");
var value = child.AnimateDouble(s => s ? 100 : 0, _ => new TweenSpec<double>(TimeSpan.FromMilliseconds(200)));

transition.UpdateTarget(1);
// child.PlayTime == transition.PlayTime；transition.TotalDuration 为 200ms（含子树）
// child.ParentTransition == transition；不要对 child 调用 RunAsync
```

### 三种状态类：Mutable / Deferred / Seekable

**`MutableTransitionState<T>`** —— 最常见的直接切换：

```csharp
var state = new MutableTransitionState<bool>(false);
state.TargetState = true;     // 写目标即触发
bool done = state.IsIdle;     // CurrentState == TargetState 且未在运行
```

**`DeferredTransitionState<T>`** —— 把"请求目标"与"提交目标"拆开：

```csharp
var state = new DeferredTransitionState<bool>(false);
using var transition = new DeferredTransition<bool>(state, clock);

state.Defer(true);
// transition.HasPendingTargetState == true，PendingTargetState == true
// 但 TargetState 不变、没有任何动画开始

state.AnimateTo(true);        // 提交：先清除挂起，再写 TargetState，观察者看到一致快照
```

`Defer` 的规则：再次 `Defer` 覆盖挂起目标；`Defer` 一个等于当前 `TargetState` 的值会清除挂起；`null` 可以是合法目标（`HasPendingTargetState` 用独立标志区分"无挂起"与 `default(T)`）。

**`SeekableTransitionState<T>`** —— 进度由调用方显式控制：

```csharp
var state = new SeekableTransitionState<bool>(false);
using var transition = new Transition<bool>(state, clock);
var value = transition.AnimateDouble(s => s ? 100 : 0,
    _ => new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()));

await state.SeekToAsync(true, 0.25);   // 整棵树采样到 25%，停留在手动模式
// state.Fraction == 0.25；value.Value == 25；transition.IsSeeking == true

await state.SeekToAsync(0.75);         // 省略目标 = 当前目标
await state.AnimateToAsync(true);      // 按时钟把进度动画到 100%，完成后退出手动模式
await state.SnapToAsync(false);        // 立即跳到指定状态并退出手动模式
```

注意：seek 与动画操作经内部互斥锁串行化——新操作会取消正在等待帧的旧操作（旧任务收到 `OperationCanceledException`）；`AnimateToAsync(target, animationSpec)` 可传入 `IFiniteAnimationSpec<double>` 控制进度本身的节奏（缺省时沿过渡时间线线性推进）；一个 `SeekableTransitionState<T>` 实例只能配置给一个 `Transition`，否则抛 `InvalidOperationException`；seekable 过渡自己驱动帧，不要再对它调用 `RunAsync`。

### 延迟注册动画：`CreateDeferredAnimation`

注册时初始值尚未确定（例如要等布局测量）时，先占位、后提交：

```csharp
using var transition = new Transition<bool>(false, clock);
using var deferred = transition.CreateDeferredAnimation(new DoubleConverter(), label: "Progress");
// transition.Animations 此时仍为空

var value = deferred.Animate(
    _ => new TweenSpec<double>(TimeSpan.FromMilliseconds(500)),
    s => s ? 100 : 0);

// 手势接力等场景可强制起始值与起始速度：
var velocity = AnimationVector.From(new double[] { 25 });
deferred.Animate(_ => new SpringSpec<double>(), 40, velocity, s => s ? 100 : 0);
// 起始值 40、起始速度 25；重复调用 Animate 返回同一个 TransitionAnimationState
```

### 无限循环：`InfiniteTransition`

```csharp
using var transition = new InfiniteTransition(clock);

var pulse = transition.AnimateDouble(
    0, 100,
    new InfiniteRepeatableSpec<double>(
        new TweenSpec<double>(TimeSpan.FromMilliseconds(1000), easing: new LinearEasing()),
        RepeatMode.Reverse,                                  // 往返；Restart 为每次从头
        new StartOffset(250, StartOffsetType.FastForward))); // 首轮立即从 250ms 处开始

var run = transition.RunAsync();
```

- `StartOffset(offsetMillis)` 默认为 `StartOffsetType.Delay`（推迟首轮开始）；`FastForward` 为首轮快进到指定播放时间立即开始。偏移只在首轮前应用一次，不随重复生效。被重复的动画时长与延迟不能同时为 0（构造时抛 `ArgumentException`）。
- 所有子动画加入同一条父时间线，相位一致；运行中晚注册的动画同样立即加入当前相位。
- 运行时重定向：`pulse.Update(0, 200)` 或换规格 `pulse.Update(0, 100, slowerSpec)`，下一帧从新初始值重启。
- `DurationScale = 2` 半速播放；改为其他值（含改回 1）会**重启所有子动画**；`DurationScale = 0` 全部吸附到目标值并挂起帧循环，恢复正值后从头开始。

### 准备租约：`BeginPreparation`

在改变目标之前先取租约，等真正的动画注册完再释放——过渡在租约释放前不会判定完成：

```csharp
using var reservation = transition.BeginPreparation();
transition.UpdateTarget(true);
// …测量/布局，并注册依赖测量结果的动画…
// using 结束（或异常离开作用域）时释放租约
```

行为要点：

- 租约期间 `IsPreparing` 为 `true` 并沿树向上传播；已在进行的动画照常推进；租约不增加 `TotalDuration`，也不受 `DurationScale` 影响（包括 `0`）。
- 多个租约相互独立；`Dispose` 释放且幂等；释放后不会同步完成——还需要一帧来评估新注册的工作。
- 手动 seek、`SnapToAsync`、过渡 `Dispose` 都会撤销未释放的租约；seeking 期间不允许获取（抛 `InvalidOperationException`）。
- 取消 `RunAsync` **不会**替你释放租约——务必用 `using`/`finally`，否则过渡会一直停留在 `IsPreparing`。
- 控件层的可见性/共享元素过渡正是用它在内容测量前保留呈现，见 [AnimatedVisibility](../components/animated-visibility.md) 与 [SharedTransition](../components/shared-transition.md)。

### 调试整棵树：`DurationScale`

```csharp
transition.DurationScale = 2;   // 全树半速（慢动作调试）
transition.DurationScale = 0;   // 下一帧全部子动画直达终值，过渡随即完成
```

`DurationScale` 在树范围内生效：对任一节点读写都作用于根。它独立于 `MotionDurationScale.ScaleFactor`（后者是 `AsyncLocal` 环境值，作用于 `Animatable`/`SuspendAnimation` 驱动的动画）——需要全局缩放时两者分别设置，详见 [引擎总览](./overview.md)。

### 手动定位：`SetPlaytimeAfterInitialAndTargetStateEstablished`

把整棵树采样到任意播放时间，用于设计器、预览与测试（`SeekableTransitionState` 即构建在此机制上）：

```csharp
transition.SetPlaytimeAfterInitialAndTargetStateEstablished(false, true, TimeSpan.FromMilliseconds(250));
// 整棵树采样在 250ms；IsSeeking == true；帧循环不再推进它，可反复调用双向拖动
transition.SetPlaytimeAfterInitialAndTargetStateEstablished(false, true, TimeSpan.FromMilliseconds(1500));
// 超过 TotalDuration 的采样被钳到终点
```

只能在根过渡上调用（子过渡抛 `InvalidOperationException`）；`playTime` 不能为负（抛 `ArgumentOutOfRangeException`）。

## 注意事项

- **线程**：所有公共成员都必须在时钟所属线程调用（UI Scope 即 UI 线程），否则 `VerifyAccess` 抛异常；详见 [引擎总览](./overview.md)。
- **`RunAsync` 的使用纪律**：只在根调用、同时只能有一个循环；取消或 `Dispose` 使任务以 `OperationCanceledException` 结束，且取消不会吸附状态/值到终点（"取消不等于完成"）。
- **观察者异常**：`PropertyChanged` 处理器抛出的异常会使帧循环出错（从 `RunAsync` 重新抛出）并清除运行标记；通知统一在当前变更批次之后派发，处理器里可以安全地再次改目标或 `Dispose` 动画。
- **`Dispose` 语义**：幂等；撤销租约、分离全部子动画与子过渡；之后的成员访问抛 `ObjectDisposedException`；当前值保持不动（不吸附）。
- **闭包不求值快照**：外部数据变化后需要 `RefreshAnimations()` 或动画的 `Update(...)`；`Transition` 与 `InfiniteTransition` 的 `Update` 在"同值同规格"时是 no-op。
- **`SeekableTransitionState`**：一个实例只能配置给一个 `Transition`；它自己驱动帧，不要再 `RunAsync`。
- **控件层优先**：`AnimatedVisibility`、`AnimatedContent`、`SharedTransition` 都建立在 `Transition` 之上。应用代码优先使用控件层；只有需要自定义协调（共享父时间线、准备租约、手动 seek）时才直接操作 `Transition`。

## 延伸阅读

- [引擎总览：帧时钟、线程与异步模型](./overview.md)
- [`Animatable`：单值动画](./animatable.md)
- [内置动画规格](./animation-specs.md)
- [转换器与缓动](./converters-and-easing.md)
- [自定义动画规格（扩展作者）](../advanced/custom-specs.md)
- [AnimatedVisibility 控件](../components/animated-visibility.md)
- [SharedTransition 共享元素过渡](../components/shared-transition.md)
- [返回文档入口](../README.md)
