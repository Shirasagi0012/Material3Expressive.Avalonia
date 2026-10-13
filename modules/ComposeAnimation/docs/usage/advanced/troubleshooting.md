# 常见问题与限制

按问题组织：症状 → 原因 → 解决。涉及 Lookahead 家族与 SharedTransition 家族的条目均为**实验性**功能。

## `await` 动画抛出 `OperationCanceledException`

**症状**：调用日志或调试器里出现未处理的 `OperationCanceledException`，调用栈指向 `AnimateToAsync` 等动画方法。

**原因**：动画被打断是常态而非错误。当动画被更新的动画、`Stop()`、`StopAsync()` 或 `SnapToAsync` 打断时，旧动画的 `Task` 以 `OperationCanceledException` 结束。另外，`MutatePriority` 较低的动画在互斥竞争中会被直接拒绝，其 `Task` 同样以取消结束。

**解决**：凡是 `await` 动画 Task 的地方都捕获它。库内部也是这么做的（`ComposeAnimation.Avalonia/Lookahead/LookaheadPosition.cs`）：

```csharp
// 取自 ComposeAnimation.Avalonia/Lookahead/LookaheadPosition.cs
try { await position.AnimateToAsync(target, AnimationSpec); }
catch (OperationCanceledException) { }
```

如果"被谁打断"本身重要，可以在 `catch` 块里读取 `Animatable<T>.TargetValue`/`Value` 判断当前状态。

## 在非 UI 线程调用动画 API 报错

**症状**：调用动画方法时抛出线程访问异常（`AvaloniaFrameSource.VerifyAccess()` 内部即 `Dispatcher.UIThread.VerifyAccess()`）。

**原因**：所有动画 API——`Animatable<T>` 的全部方法、`Transition`、`InfiniteTransition`、帧时钟本身——都必须在时钟所属线程调用；生产时钟的所属线程是 UI 线程。这包括 `PropertyChanged` 回调里的同步操作：通知是在时钟线程上发出的。

**解决**：把动画调用编组回 UI 线程（`Dispatcher.UIThread.Post` / `CheckAccess` 判断）；不要在后台线程持有时钟或 `Animatable<T>` 并直接调用。测试用的手动时钟如果 `VerifyAccess()` 是空实现，则不受此限制。

## 属性不动画、控件不播放，但没有异常

**症状**：设置了 `Animated.Properties` 或内容切换没有动画效果，也看不到异常。

**原因**：未初始化根环境、规格失败或帧源失败。

**解决**：在加载 XAML 或设置 Content 前调用 `AnimationScope.Initialize`，保存根 Scope 并监听 `AnimationFailed`：

```csharp
root.AnimationFailed += (_, failure) => Log(failure.Host, failure.OperationLabel, failure.Exception);
```

规格失败只结束失败运行，其他消费者继续。帧源失败后，同一个环境不会持续重试；修复源后需创建新的 TopLevel 根环境。控件在树内移动时 Scope 会在下一帧等待者恢复前自动重建；卸载后重新挂载由 `AnimationScope.Get` 返回新 Scope，捕获了旧 Scope 的长运行应在 `ScopeChanged` 处理程序中重启，根帧源不会被替换。

## 自定义泵帧时 `AnimatedVisibility` / `AnimatedContent` 不启动

这两个控件自行管理帧循环时，会先通过 `Loaded` 回调等待注册与首轮布局，再以 UI 线程的 `Normal` 优先级启动循环。后续异步等待恢复使用 `Normal`，不会持续依赖 `Loaded` 队列。

自定义宿主或测试泵在初始化阶段仍需处理 `Loaded`，例如完成布局并调用不带优先级门槛的 `Dispatcher.UIThread.RunJobs()`。重新附树或其他导致连接重建的操作，也需要执行这个启动阶段。从初始化开始只调用 `RunJobs(DispatcherPriority.Render)` 会跳过首次启动回调。

循环启动后，可以使用 `RunJobs(DispatcherPriority.Render)` 处理动画续体；但这不表示控件或应用的其他功能都不需要较低优先级任务。推进手动时钟只表示帧已送达，仍需处理 Dispatcher 队列；确定性测试还应等待循环订阅下一帧，避免与异步唤醒竞争。外部父 `Transition` 自行管理的循环，其启动上下文由调用方决定。

## `AnimatedContentSize` 在 Lookahead 域中抛 `NotSupportedException`（实验性）

**症状**：把 `AnimatedContentSize` 放进 lookahead 域后抛出：`Experimental AnimatedContentSize lookahead requires the default template, direct Control content, stretch content alignment, and no ContentTemplate/Padding/BorderThickness.`

**原因**：lookahead 模式（协作式目标测量）目前只允许最简形态：默认模板、直接的 `Control` 内容、`Stretch` 内容对齐，且不能设置 `ContentTemplate`/`Padding`/`BorderThickness`。普通（非 lookahead）布局没有这些限制。

**解决**：把装饰移进内容内部——在 `AnimatedContentSize` 里放一个固定尺寸的叶子控件，把 `Padding`、边框、模板都放在叶子上。

相关复用与完成通知（普通/lookahead 模式均适用）：

- `ResetAnimation()`：取消动画并忘记上一次尺寸，下一次测量直接初始化到目标值、不触发完成通知。回收复用同一实例展示另一条数据时应调用它；它也允许在出错后重试（普通脱离/重新附加会保留当前尺寸）。
- `AnimationCompleted` 事件：动画正常结束（`AnimationEndReason.Finished`）时触发，参数 `AnimatedContentSizeChangedEventArgs` 携带 `InitialSize`/`TargetSize`。

## `AnimatedContent` 报模板错误

**症状**：`InvalidOperationException`：`AnimatedContent requires PART_ContentPanel.` / `AnimatedContent requires PART_ContentPanel with a ContentSlotPanel child.` / `AnimatedContent rejects PART_ContentPresenter; content is presented in slots.`

**原因**：`AnimatedContent` 不用 `ContentPresenter` 呈现内容——每个在播的内容状态放在独立的 slot 里。其模板契约要求：名为 `PART_ContentPanel` 的 `Border`，且其子级为 `ContentSlotPanel`；模板中出现 `PART_ContentPresenter` 会被直接拒绝。由于 `ContentSlotPanel` 是 internal 类型，自定义模板实际无法合法满足该契约。

**解决**：不要替换 `AnimatedContent` 的 `Template`，使用默认模板；外观通过 `Background`/`BorderBrush`/`BorderThickness`/`CornerRadius`/`Padding` 等属性控制（默认模板会把它们绑定到 `PART_ContentPanel`）。

另外：不想要容器尺寸动画时，把 `ContentTransform.SizeTransform` 设为 `null`（例如 C# 里的 `transform.Using(null)`）；省略 `SizeTransform` 则会使用默认的裁剪弹簧尺寸动画，二者含义不同。

## `AnimatedBounds` 抛 `NotSupportedException`（实验性）

**症状**：`NotSupportedException`：`AnimatedBounds requires a registered target and an ILookaheadPlacementHost parent.`；或 `AnimatedSize requires a participating placement parent in the same lookahead domain.`

**原因**：在显式 lookahead 域内，`AnimatedBounds` 的直接父级必须实现 `ILookaheadPlacementHost`。已实现该接口的容器包括 `LookaheadBox`、`LookaheadStackPanel`、`LookaheadScrollViewport`、`LookaheadBoundary`、`LookaheadRoot`、`AnimatedContentSize`（以及 `AnimatedBounds` 自身）等；普通 `Panel`/`StackPanel` 不是。`ParentLayoutMode="AnimatedSize"` 还要求父级在同一 lookahead 域内参与放置。

**解决**：

- 把 `AnimatedBounds` 放进上述 lookahead 容器之一；
- 需要让普通内容成为目标树端点时，用 `LookaheadBoundary` 包裹它（边界内的 `Child` 仍按普通 Avalonia 布局测量/排列，不缩放、不冻结、不复制）；
- 需要丢弃当前运动、下次布局直接在目标处初始化时，调用 `AnimatedBounds.ResetAnimation()`。

## SharedTransition 不配对或不播放（实验性）

**症状**：共享元素没有起飞/落位动画，或准备阶段一直不结束。

**排查**：`SharedTransitionHost` 暴露了一组诊断成员，先看它们：

| 成员 | 含义 |
| --- | --- |
| `Diagnostics`（`IReadOnlyList<string>`） | 最近一次解析的诊断信息（含未配对端点及原因） |
| `LastPreparationError` / `LastSessionError` | 最近一次准备 / 运行期 session 的异常 |
| `IsPreparing` / `IsOverlayActive` | 是否在准备 / 覆盖层是否在绘制 |
| `RegisteredEndpointCount` / `ActiveSessionCount` | 已注册端点数 / 未关闭 session 数 |
| `EndpointStates` | 各端点可见性状态摘要字符串 |
| `LastPreparationParticipants` | 最近一次准备请求的参与者摘要 |

**常见原因与解决**：

- **不在同一坐标空间**：覆盖层绘制在 `SharedTransitionHost` 内，不同 host 里的端点永远不会配对——确认两个端点在同一个 `SharedTransitionHost` 下。
- **缺少共同的父 `Transition`**：共享内容需要共同的父 `Transition<TState>`；先设置 host 的 `Transition` 再调用 `BeginUpdate()`（顺序反了会抛 `InvalidOperationException`）。`BeginUpdate()` 返回的 `SharedTransitionUpdate` 用于把多处内容变更批处理成一次过渡。
- **准备超时**：准备阶段由 `PreparationTimeout` 兜底，默认 1200ms，超时后自动放弃该次准备；设为负值可禁用超时：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="Demo.MainWindow">
    <!-- 默认 1200ms；负值禁用超时兜底 -->
    <ca:SharedTransitionHost x:Name="Host" PreparationTimeout="0:0:2">
        <!-- ... -->
    </ca:SharedTransitionHost>
</Window>
```

## 动画瞬间跳到终值

**症状**：所有动画"啪"地一下直接到终点，看不到过程。

**原因**：时长缩放被设为 0。`MotionDurationScale.ScaleFactor = 0` 会让有限动画在下一帧直接吸附到终值（无限规格保持播放时间为 0）；`Transition.DurationScale = 0` 对 `Transition` 树同理。

**解决**：把 `MotionDurationScale.ScaleFactor` 恢复为 `1`（或调用 `MotionDurationScale.Reset()` 恢复当前异步上下文的默认值），`Transition.DurationScale` 恢复为 `1`。注意 `MotionDurationScale` 基于 `AsyncLocal`——在某个异步上下文里设置后没有恢复，会顺着 `await` 流影响同上下文的其他动画。

## `MoveContent failed: {reason}. Tree edits are not rolled back.`

`Lookahead.MoveContent` 是同步事务。原因文本是以下之一：

- `layout ran inside the move callback; …`：回调内运行了布局或分发器；
- `the Lookahead domain was detached during the move`：移动期间 Lookahead 域脱离了树；
- `the Lookahead domain moved to another window during the move`：域被移到另一个窗口；
- `the content ended outside this Lookahead domain`：内容最终不在该 Lookahead 域内。

抛出时已经执行的树编辑**不会回滚**，调用方需要自行恢复或重建该子树。不要在事务回调里 `await`、调用 `UpdateLayout` 或推进帧。

## 在 Scope 上启动运行抛 `ObjectDisposedException`

Scope 已结束（控件脱离、窗口关闭、父 Scope 结束或子 Scope 已 `Dispose`）。不要缓存 `Get` 的结果跨越脱离：使用时再调用 `AnimationScope.Get(control)`，或在 `ScopeChanged` 处理程序里拿 `e.NewScope`。状态对象（`Animatable`、`Transition`）不受影响，可以在新 Scope 上从停下的值继续。

## `DelayAsync` 抛 `ArgumentOutOfRangeException`

`scope.DelayAsync` 不接受负值（`Timeout.InfiniteTimeSpan` 除外），与 `Task.Delay` 一样同步抛出。它也不请求帧、不受 `DurationScale` 影响；需要按动画时间等待时用 `NextFrameAsync` 循环或带延迟的规格。

## 换父后运行重新开始而不是继续

按顺序检查：

1. 是否调用了 `ResetAnimation`，或进入了另一个 Lookahead 根（真正的上下文变化会重置）。
2. `AnimatedBounds`：坐标参考祖先或运动帧是否变了（Q-H，基准改变时释放）。
3. 控件是否在本次排空前挂回；先移除、过一帧再添加会在排空时释放。
4. 自建运行：是否在 `ScopeChanged` 处理程序里重新启动，并让状态对象从当前值继续，而不是新建对象。
5. 处理程序是否反复让宿主过期：每帧至多重建一次（D18），数值会保持一帧。

## 测试中的帧与等待

测试用 `ManualFrameSource` 驱动帧（`SendFrame` 绝对时间、`AdvanceBy` 只送一帧，不补帧），每步后 `Dispatcher.UIThread.RunJobs()`。本仓库测试项目的条件等待默认 15 s，可用环境变量 `COMPOSE_ANIMATION_TEST_WAIT_MS` 调整（只影响本仓库的测试辅助）。
