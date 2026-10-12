# Runtime 引擎核心概念

本文面向要直接用 C# 写动画代码的读者。运行时行为参考 `ComposeAnimation.Test/AnimatableTest.cs` 与 `ComposeAnimation.Test/TransitionTest.cs`；交互演示见 `Catalog/Demos/Engine`。

## 两层架构

- **`ComposeAnimation.Runtime`**（命名空间 `ComposeAnimation.Runtime`）：纯动画引擎。只引用 Avalonia 的基础类型与 `Dispatcher`（`Point`/`Size`/`Rect`、帧回调等），不含任何控件，可以脱离 XAML 在任意代码中使用。
- **`ComposeAnimation.Avalonia`**（命名空间 `ComposeAnimation`，XAML 前缀 `ca:`）：控件层。`AnimatedVisibility`、`AnimatedContent`、`AnimatedContentSize` 等控件内部就是创建 `Animatable<T>`、`Transition<TState>` 并在帧时钟上驱动它们；控件层暴露的规格对象（如 `DoubleSpringSpec`）也只是 Runtime 规格的 XAML 友好封装。

因此，理解了 Runtime 就理解了整个库的行为：控件层负责"什么时候播"，引擎负责"怎么播"。

## 帧时钟与 AnimationScope

`IFrameSource` 保留显式时钟的底层入口：`VerifyAccess()` 检查所属线程；`NextFrameAsync(CancellationToken)` 返回下一帧的绝对时间戳并支持取消。独立 Runtime 调用者自行提供时钟。

Avalonia 应用在加载 XAML 或设置 Content 之前调用 `AnimationScope.Initialize(topLevel, clock?)`。默认帧源与每窗口唯一的内部 FrameClock 由内部实现提供，不再公开构造；`SharedAnimationFrameClock` 已删除。一个根环境固定一个帧源，同根控件、直接动画与派生 Popup 自动共享按需请求；整个根最多有一个挂起的源请求。UI 动画 API 必须在 UI 线程调用。

```csharp
// Window 构造函数内，InitializeComponent 之前
var root = AnimationScope.Initialize(this);
InitializeComponent();

// 控件附着之后；重复调用返回同一个 Scope，不需要也不应释放
var motion = AnimationScope.Get(control);
var alpha = motion.CreateAnimatable(0d);
motion.BindAnimatedValue(control, Visual.OpacityProperty, alpha);
_ = motion.Launch(ct => alpha.AnimateToAsync(1, cancellationToken: ct), "Fade in");
```

控件在树内移动时，旧 Scope 会在下一帧等待者恢复前合并重建一次，随后控件的 `ScopeChanged` 事件触发，持有旧 Scope 的运行应在 `AnimationScope.AddScopeChangedHandler` 注册的处理程序中重启。控件脱离树时 Scope 结束，窗口关闭结束根环境（根没有公开的 `Dispose`）。`scope.NextFrameAsync(ct)` 等待窗口的下一帧，并在帧批次中内联恢复。工厂对象终身受其 Scope 约束：结束后可以读最终值、调用清理操作，不能启动或变更新动画。详见 [Scope 使用与迁移](../animation-scope.md)。

测试项目引用 `ComposeAnimation.TestSupport`，使用 `AnimationTestWindow(clock)` 在 Content 初始化前建立环境。`ManualFrameSource.SendFrame(absoluteTime)` 发送一个绝对时间戳；`AdvanceBy(delta)` 只发送一帧，不隐式补起点帧。送帧后仍需推进 Dispatcher；`AnimationTestDispatcher.WaitUntilAsync` 提供有上限的条件等待。

## 异步模型与中断

所有动画方法返回 `Task`（方法名带 `Async` 后缀），例如 `Animatable<T>.AnimateToAsync` 返回 `Task<AnimationResult<T>>`，正常完成时 `AnimationResult<T>` 携带结束状态快照（`EndState`）与结束原因（`EndReason`：`Finished` 或 `BoundReached`）。

**打断是常态，不是异常。**当动画被更新的动画、`Stop()` 或 `SnapToAsync` 打断时，旧 Task 以 `OperationCanceledException` 结束——`await` 动画必须捕获它：

```csharp
// 运行时动画示例
try
{
    await forward;
}
catch (OperationCanceledException)
{
    // 被新的动画打断了
}
```

`Animatable<T>` 通过内部互斥锁保证同一时刻只有一个动画在写值；`MutatePriority` 控制互斥优先级：`Default` < `UserInput` < `PreventUserInput`。同级或更高优先级的动画会打断当前动画；**更低优先级的动画会被直接拒绝**（其 Task 同样以取消结束），见 `ComposeAnimation.Test/AnimatableTest.cs` 的 `AnimateTo_WithLowerPriority_IsRejected`。

注意 `AnimationState<T>` + `SuspendAnimation` 扩展方法是**没有互斥保护**的底层入口，同一状态并发播放会交错写值；需要打断安全时请用 `Animatable<T>`。

### 在组件中共享 MutatorMutex

`MutatorMutex` 是公开的 Runtime 类型，可用于 Tooltip 等组件的长时间状态操作。需要相互排斥的操作共享同一个实例；不同实例互不影响。

```csharp
var mutex = new MutatorMutex();
var operation = mutex.MutateAsync(async ct =>
{
    await Task.Delay(TimeSpan.FromSeconds(1), ct);
    return "completed";
}, priority: MutatePriority.UserInput);

await operation;
```

`MutateAsync<TResult>` 遵循同级或更高优先级抢占、更低优先级拒绝的规则；新操作会等待旧操作及其 `finally` 清理完成。操作必须响应传入的取消令牌，否则抢占只能发出取消请求，新操作仍需等待旧操作退出。操作抛出的其他异常直接传给调用方。

`CancelCurrent()` 请求取消但不等待清理；需要等待时，先保存 `CurrentCompletion`，再取消并等待保存的 Task，按需处理正常取消。`CurrentCompletion` 在没有当前操作时为 null。这套抢占规则适合交互状态协调；需要按顺序等待的消息队列应使用公平排队的机制。

## 可绑定性

`Animatable<T>`（以及 `Transition`、`InfiniteTransition`、它们的动画节点）继承自 CommunityToolkit.Mvvm 的 `ObservableObject`，实现 `INotifyPropertyChanged`。`Value`、`Velocity`、`IsRunning`、`TargetValue`、`LowerBound`、`UpperBound` 等属性在时钟线程上逐帧引发 `PropertyChanged`，可以**直接作为 Avalonia 绑定源**：

```csharp
var animatable = new Animatable<double>(new DoubleConverter(), 0, clock);
// XAML: <ProgressBar Value="{Binding Progress.Value}" />，Progress 是这个 Animatable<double>
```

注意：为了不错过整数/颜色取整等场景下的帧，**赋相同值时通知不会被抑制**——每一帧都会引发 `Value` 通知。

## `MotionDurationScale`

`MotionDurationScale` 是静态的环境（`AsyncLocal`）时长缩放，作用于 `SuspendAnimation`/`Animatable<T>` 驱动的动画：

```csharp
public static class MotionDurationScale
{
    public static double ScaleFactor { get; set; }  // 默认 1，必须有限且 >= 0
    public static void Reset();                     // 恢复当前异步上下文的默认值 1
}
```

`ScaleFactor` 是墙钟时间到播放时间的乘数：`2` 表示半速播放；**`0` 让有限动画在下一帧直接跳到终值**（无限规格保持播放时间为 0）。因为基于 `AsyncLocal`，设置只影响当前异步上下文，`Reset()` 恢复默认。

`Transition` 树保留自己的 `DurationScale`。Scope 环境在初始化时读取 `MotionDurationScale.ScaleFactor` 作为根的初始倍率，之后使用 Scope 链的 `DurationScale` 与继承的附加属性 `AnimationScope.DurationScale` 统一调速（有效倍率为两者相乘）；时钟时间戳保持原值。

## 概念关系与选择指南

各概念分层组合：

1. **`Animation<T>`**：无状态的曲线对象，给定播放时间返回 `GetValue`/`GetVelocity`，如 `TargetBasedAnimation<T>`、`DecayAnimation<T>`。
2. **`IAnimationSpec<T>`**：规格（如 `SpringSpec<T>`、`TweenSpec<T>`、`SnapSpec<T>`、`KeyframesSpec<T>`、`ArcAnimationSpec<T>`、`RepeatableSpec<T>`），经 `Vectorize(converter)` 转成向量规格后驱动 `Animation<T>`。规格与值解耦、可复用。详见 [./animation-specs.md](./animation-specs.md)。
3. **`Animatable<T>` / `AnimationState<T>`**：值持有者，把 `Animation<T>` 跑到帧时钟上，逐帧更新可绑定的 `Value`。`Animatable<T>` 额外提供互斥与打断语义。
4. **`Transition<TState>`**：状态驱动的一组动画——为每个属性注册"按状态取目标值 + 按状态切换段取规格"，`UpdateTarget` 改目标状态即整树协同过渡。详见 [./transitions.md](./transitions.md)。`InfiniteTransition` 是共享时间线的无限循环版本。

选择指南：

| 场景 | 该用什么 |
| --- | --- |
| 用 C# 动画单个值 | `Animatable<T>.AnimateToAsync` |
| 底层逐帧驱动、不需要打断保护 | `AnimationState<T>` + `SuspendAnimation` |
| 状态切换同时驱动多个属性 | `Transition<TState>` |
| 无限循环动画 | `InfiniteTransition` + `InfiniteRepeatableSpec<T>` |
| 惯性/甩动衰减 | `Animatable<T>.AnimateDecayAsync` + `DecayAnimationSpec.ExponentialDecay<T>()` |
| UI 控件进出场 | `AnimatedVisibility`（见 [../components/animated-visibility.md](../components/animated-visibility.md)） |
| styled 属性值变化自动动画 | `Animated.Properties`（见 [../components/animated-properties.md](../components/animated-properties.md)） |

## 一个最小例子

```csharp
// UI 控件附着后
var motion = AnimationScope.Get(control);
var animatable = motion.CreateAnimatable(0d);

var task = animatable.AnimateToAsync(100,
    new TweenSpec<double>(duration: TimeSpan.FromMilliseconds(100), easing: new LinearEasing()));

try
{
    var result = await task;
    // result.EndReason == AnimationEndReason.Finished，animatable.Value == 100
}
catch (OperationCanceledException)
{
    // 被更新的动画 / Stop / SnapToAsync 打断
}
```

更多内容：[../README.md](../README.md) 返回文档入口；遇到问题时见 [../advanced/troubleshooting.md](../advanced/troubleshooting.md)。
