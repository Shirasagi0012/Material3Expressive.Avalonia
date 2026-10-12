# VisualAnimationScope：动画环境与生命周期

Scope 管理直接动画、任务、值订阅和动画属性贡献。Material 等消费者继续决定目标值、规格和交互规则。

## 建立根环境

在 UI 线程、加载 XAML 或设置 Content **之前**初始化，每个应用 TopLevel 仅一次：

```csharp
public MainWindow()
{
    var root = VisualAnimationScope.Initialize(this);
    root.AnimationFailed += (_, failure) =>
        logger.LogError(failure.Exception, "{Host}: {Operation}",
            failure.Host.GetType().Name, failure.OperationLabel);
    InitializeComponent();
}
```

`Initialize(window, clock)` 可以提供固定帧源；源仍由提供者拥有。默认 UI 帧源和共享分发器由内部实现提供。同根控件和派生 PopupRoot 自动共享按需请求，空闲时不泵帧；Overlay 使用所在根环境。关闭窗口结束根环境。根结束后不能重新初始化同一个普通 TopLevel。

## 控件与模板

控件已附着后调用 `VisualAnimationScope.Create(owner)`，每次获得新 Scope。控件保存自己的 Scope，卸载时自动结束；模板替换时显式结束旧 Scope，再为新模板创建 Scope。逻辑子 Scope 用 `CreateChild(label)`，继承实际 Visual 宿主和根帧环境。

```csharp
var motion = VisualAnimationScope.Create(button);
var surface = currentSurface; // 捕获本次模板，异步退出不重新读取可变字段
var alpha = motion.CreateAnimatable(0d, label: "State layer");
motion.SubscribeValue(alpha, value => surface.SetLayerOpacity(value));

_ = motion.Launch(ct => alpha.AnimateToAsync(.12,
    new TweenSpec<double>(TimeSpan.FromMilliseconds(75)), cancellationToken: ct), "Hover");
```

常见工厂支持 double、float、int、Point、Vector、Size、Rect、Thickness、CornerRadius、Color；自定义值使用 converter 重载。也可以创建 `Transition<TState>` 和 `InfiniteTransition`，它们的节点实现相同的 `IAnimationValue<T>`。

所有工厂对象都受 Scope 生命周期约束，包括创建时尚未运行的对象。结束后新的播放、吸附、目标/规格/边界变更会抛 `ObjectDisposedException`；最终值仍可读取，Stop/Dispose 等清理操作仍允许调用。Runtime 的显式时钟构造保留为独立运行入口；使用 Scope.Clock 构造也会继承其生命周期。

## 任务、取消和诊断

`Launch(Func<CancellationToken, Task>, label?)` 在调用线程同步调用委托，在用户代码之前登记，不使用 Task.Run。它返回委托原任务，保留成功、取消和失败状态，同时完整观察错误。委托的同步异常转成失败任务；错误线程、已结束 Scope 或空委托属于同步调用错误。

新任务不会预先停止已有动画。Animatable 自身的互斥、中断、当前值与速度接续规则不变。一个任务失败不会取消兄弟任务。顺序流程应等待子任务并传递取消令牌；用户在流程内部自行创建并丢弃的任意业务任务不会自动被接管。

根 `AnimationFailed` 提供 `FailureId`、`SourceScope`、实际 `Host`、可选 `OperationLabel` 和原始异常。源 Scope 和根 Scope 保存 `LastError`。取消任务不报告；以 Faulted 状态完成的任务即使携带 OperationCanceledException 也报告。聚合异常逐项处理，未保留来源的新异常是新失败。受管操作链、保留的 await/EDI 异常传播轨迹用于去重，不按消息文本去重；弱异常记录不持有永久任务或 Scope 历史。

诊断处理器抛错不会打断其他处理器或必要清理，也不递归发布；最近处理器错误保存在根的 `LastDiagnosticError`。帧源故障在根报告一次，并停止同一固定环境的后续源请求。规格和局部回调故障不破坏其他控件的帧源。

## 值订阅与动画绑定

```csharp
var progress = motion.CreateAnimatable(0d);
using var binding = motion.BindAnimatedValue(bar, ProgressBar.ValueProperty, progress);
using var subscription = motion.SubscribeValue(progress, value => UpdateDrawing(value));
```

建立关系立即发布当前值，响应 Value、null 或空属性名通知，不额外合并相等值。订阅不拥有外部传入的动画对象。

`BindAnimatedValue` 以单向 Animation 优先级贡献采样值，不建立额外过渡。释放只移除自己的贡献，恢复普通绑定、样式或本地值。两类资源可提前释放，也会在 Scope 结束时统一撤销。Ending 后同步重入或排队通知不再通过受管消费端更新。

## 结束与临时效果

`Dispose()` 同步标记整个子树 Ending，先禁用订阅再撤销绑定，随后取消任务和帧等待、停止动画。它不阻塞 UI。`DisposeAsync()` 进一步等待任务、工厂动画和子 Scope 实际退出；忽略取消的用户代码仍需自行合作退出。

```csharp
await using var ripple = motion.CreateChild("Ripple");
var radius = ripple.CreateAnimatable(0d);
ripple.SubscribeValue(radius, value => surface.SetRippleRadius(value));
await ripple.Launch(ct => radius.AnimateToAsync(100, cancellationToken: ct), "Grow");
```

不能在受管任务中等待自身 Scope 或其祖先 DisposeAsync，否则会等待自身，API 会明确拒绝。父任务可以等待不包含该任务的子 Scope 退出。完成任务、已结束子 Scope 和提前释放的消费资源及时移出活动登记。公开活动计数便于压力验收，不能用提前丢弃任务的方式归零。

## 调速与属性吸附

根/子 Scope 的 `DurationScale` 是继承的时长乘数，也可用继承的 `Animated.DurationScale` 调整控件子树。4 表示 0.25× 播放，0 让有限动画在下一帧到达终点。它们改变播放策略，保持原始帧时间戳；有限播放保持已有进度，无限 Transition 沿用改变时长后重新起播的语义，0 时暂停并等待正数策略恢复。

```csharp
Animated.SetDurationScale(demo, 4);
await Animated.SnapToTargetAsync(border, Visual.OpacityProperty);
```

属性吸附立即结束当前贡献并读取实际基础目标，即使目标未变也结束。声明、Spec、普通绑定和样式保持原样，未来目标变化继续按声明播放。

## 测试支持

测试项目通过 NuGet 引用同版本的 `ComposeAnimation.TestSupport` 包：

```csharp
using var clock = new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess);
var window = new AnimationTestWindow(clock) { Content = control };
window.Show();
Dispatcher.UIThread.RunJobs();

var motion = VisualAnimationScope.Create(control);
var value = motion.CreateAnimatable(0d);
var playing = motion.Launch(ct => value.AnimateToAsync(1,
    new TweenSpec<double>(TimeSpan.FromMilliseconds(100)), cancellationToken: ct));
clock.SendFrame(TimeSpan.Zero); // 显式起点帧
Dispatcher.UIThread.RunJobs();
clock.AdvanceBy(TimeSpan.FromMilliseconds(100)); // 只发送这一帧
await AnimationTestDispatcher.WaitUntilAsync(() => playing.IsCompleted);
await playing;
window.Close();
await window.AnimationRoot.DisposeAsync();
```

`SendFrame` 绝对时间单调不减；`AdvanceBy` 不隐式补帧。手动时钟支持多等待者、取消、PendingFrameCount 和 Dispose。送帧不代表布局或渲染完成。条件等待有 Dispatcher 轮数上限，也不声称整个应用已空闲。

## 破坏性迁移

- 删除 Animated.FrameClock/GetFrameClock/SetFrameClock：在 TopLevel 加载内容前初始化固定根环境。
- 删除 Animated.LastError/GetLastError：保存根 Scope，监听 AnimationFailed 并读取 LastError。
- 默认 Avalonia 时钟与 每窗口唯一的内部 FrameClock 均为内部实现；UI 消费者使用 Scope.Clock，测试使用独立 TestSupport。
- 控件模板替换、卸载/重挂结束旧 Scope 并创建新 Scope；不在同一环境热换帧源。
- Catalog 通过播放策略保留速度控制；Material 用子 Scope 拥有 Ripple、Elevation 和 Shape，保留其领域规则。
