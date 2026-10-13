# 从 VisualAnimationScope 迁移 · Migration from VisualAnimationScope

本文面向 Material3Expressive 的第 6 阶段迁移（约 40 个组件），以及任何基于 `origin/main@23388bc` 的旧 API 编写的代码。对照表可以逐项机械替换；第 9 节列出替换后行为上会注意到的差异。所有新 API 名称以 `grokbot/anim-foundation` 当前代码为准，示例在 `ComposeAnimation.Test/DocSamples.cs` 中编译（`MigrationSamples`）。

> **⚠ 第 6 阶段注意：`IsEnding` 已不再公开。** M3E 中有 72 处、37 个文件使用 `IsEnding`（多为 `_motion is { IsEnding: false }` 守卫）。按 `Get` 模型通常直接删除守卫；确需判断已保存的 Scope 是否结束时改用 `scope.Lifetime.IsCancellationRequested`。详见第 2 节与第 9 节。

## 1. 根环境 · Root

| 旧 · Old | 新 · New |
|---|---|
| `VisualAnimationScope.Initialize(window)` | `AnimationScope.Initialize(window)` |
| `VisualAnimationScope.Initialize(window, clock)` | `AnimationScope.Initialize(window, frameSource)`（`IFrameSource?`） |
| `VisualAnimationScope.Initialize(window, clock, timeProvider)` | `AnimationScope.Initialize(window, frameSource, timeProvider)` |

## 2. 控件 Scope：`Create` / `Dispose` → `Get` · Control scopes

旧模型在附着时创建、脱离时释放；新模型在使用处取得、从不释放。

```csharp
// 旧 · Old
private VisualAnimationScope? _motion;
protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);
    _motion = VisualAnimationScope.Create(this);
    _alpha = _motion.CreateAnimatable(IsSelected ? 1f : 0f);
}
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    _motion?.Dispose();
    _motion = null;
    base.OnDetachedFromVisualTree(e);
}
private void Animate(float target) => _ = _alpha!.AnimateToAsync(target, spec);
```

```csharp
// 新 · New
private readonly Animatable<float> _alpha = new(new FloatConverter(), 0f, VisibilityThresholds.Float);
private void Animate(float target) => _ = _alpha.AnimateToAsync(AnimationScope.Get(this), target, spec);
```

- `AnimationScope.Get(this)` 是 O(1) 查找，重复调用返回同一个 Scope；**不要** `Dispose` 它，也不要缓存到脱离之后。控件未附着时 `Get` 抛出 `InvalidOperationException`。
- 状态对象（`Animatable`/`Transition`/`InfiniteTransition`）不再依赖 Scope，可以在字段初始化时创建，跨附着/脱离保留。
- 需要在重新挂到树上（换父级）后重启工作的组件，订阅 `AnimationScope.AddScopeChangedHandler(this, handler)`：`e.NewScope` 为新 Scope（脱离时为 null），在处理程序里用新 Scope 重新运行。`IAnimationScopeHost` 是库内部接口，M3E 使用该事件。
- `scope.IsEnding`（旧公开属性）已不公开。旧代码 `_motion is { IsEnding: false } motion` 的守卫在 `Get` 模型中一般可以删除（`Get` 不会返回已结束的 Scope）；需要判断某个已保存的 Scope 是否已结束时，用 `scope.Lifetime.IsCancellationRequested`。

## 3. 短生命周期子 Scope · Sub-lifetimes

| 旧 · Old | 新 · New |
|---|---|
| `var child = scope.CreateChild(); … child.Dispose();` | `using var child = scope.CreateChild();`（返回 `AnimationChildScope : IDisposable`） |

`Dispose` 只取消该子 Scope 及其后代的运行；父 Scope 结束或重建时子 Scope 自动结束；重复释放无副作用。

## 4. 状态对象构造 · Constructing state objects

| 旧 · Old | 新 · New |
|---|---|
| `new Animatable<T>(converter, initial, clock, threshold, label)` | `new Animatable<T>(converter, initial, threshold, label)` |
| `scope.CreateAnimatable(1f)`（float/double/int/Point/Vector/Size/Rect/Color… 重载） | `new Animatable<float>(new FloatConverter(), 1f, VisibilityThresholds.Float)`（其他类型同理：`DoubleConverter`+`VisibilityThresholds.Double` 等；旧扩展正是这样构造的） |
| `scope.CreateAnimatable(value, converter, threshold, label)` | `new Animatable<T>(converter, value, threshold, label)` |
| `new Transition<T>(initial, clock)` / `scope.CreateTransition(initial)` | `new Transition<T>(initial)` / `new Transition<T>(transitionState)` |
| `new InfiniteTransition(clock)` / `scope.CreateInfiniteTransition()` | `new InfiniteTransition()` |

在 `a6c2173` 时没有替代的静态工厂：`AnimationScopeExtensions` 只保留 `SubscribeValue` 与 `BindAnimatedValue`。

## 5. 运行取 Scope 作为第一个参数 · Runs take the scope first

| 旧 · Old | 新 · New |
|---|---|
| `animatable.AnimateToAsync(target, spec, block, priority, ct)` | `animatable.AnimateToAsync(scope, target, spec, block, priority, ct)` |
| `animatable.AnimateToAsync(target, spec, initialVelocity, …)` | `animatable.AnimateToAsync(scope, target, spec, initialVelocity, …)` |
| `animatable.AnimateDecayAsync(initialVelocity, decay, …)` | `animatable.AnimateDecayAsync(scope, initialVelocity, decay, …)` |
| `transition.RunAsync(ct)` | `transition.RunAsync(scope, ct)` |
| `infinite.RunAsync(ct)` | `infinite.RunAsync(scope, ct)` |
| `seekable.AnimateToAsync(target, spec, ct)` | `seekable.AnimateToAsync(scope, target, spec, ct)` |
| `clock.AnimateAsync(…)` / `clock.AnimateDecayAsync(…)`（`SuspendAnimation` 扩展） | `SuspendAnimation.AnimateAsync(scope, …)` / `SuspendAnimation.AnimateDecayAsync(scope, …)` |
| `await scope.Clock.NextFrameAsync(ct)` | `await scope.NextFrameAsync(ct)` |

`scope` 是 `FrameScope`（Runtime 中的公开抽象基类）；Avalonia 代码传 `AnimationScope`。`SnapToAsync`、`Stop`、`UpdateBounds` 不取 Scope。`AnimateToAsync` 各重载的可选参数是否统一仍在评审中（见待批准列表），本次没有改动。

## 6. 帧源与测试 · Frame sources and tests

| 旧 · Old | 新 · New |
|---|---|
| `IAnimationFrameClock` | `IFrameSource`（窗口帧源；窗口内部唯一的 FrameClock 是它的唯一消费者） |
| `ManualAnimationFrameClock` | `ManualFrameSource`（成员不变：`AdvanceBy`、`SendFrame`、`PendingFrameCount`、`Now`） |
| `AnimationTestWindow.TestClock` | `AnimationTestWindow.FrameSource` |
| `scope.Clock` | 已删除：把 Scope 本身传给运行 |
| `SharedAnimationFrameClock` | 已删除（内部 `FrameClock` 取代） |

## 7. 时长缩放 · DurationScale

| 旧 · Old | 新 · New |
|---|---|
| `Animated.DurationScale`（附加属性） | `AnimationScope.DurationScale`（附加属性 `AnimationScope.DurationScaleProperty`，`AnimationScope.SetDurationScale(control, x)`） |
| `scope.DurationScale = x` | 不变（实例属性）；注意 Scope 重建后实例值丢失，需要时在 `ScopeChanged` 处理程序里重新设置 |

附加值沿继承链传递，实例值与祖先值相乘。

## 8. 诊断 · Diagnostics

旧的公开计数与错误字段（`LastError`、`LastDiagnosticError`、`ActiveTaskCount`、`ActiveChildCount`、`SubscriptionCount`、`BindingCount`、`PendingAnimationCount`、`IsEnding`）已从公开面移除。错误统一通过 `AnimationScope.AnimationFailed` 事件报告（向根冒泡）。

## 9. 迁移后会注意到的行为差异 · Behaviour differences

- **D17 恢复而非重启：** 控件换父级导致 Scope 重建时，内置宿主（`AnimatedVisibility`、`AnimatedContent`、`AnimatedContentSize`、`AnimatedBounds`、`LookaheadSize`、`LookaheadPosition`、`Animated.Properties`）带着当前值与速度在新 Scope 中继续。自己写的运行（第 2 节）在 `ScopeChanged` 中以原状态对象重新运行时同样连续：`Animatable` 保留值与速度。
- **`RunAsync` 第二次调用：** 在已有运行的状态上再次 `RunAsync` 会取消前一个运行，不再抛出“already has a frame loop”。
- **脱离时在排空时释放：** 控件脱离树时 Scope 不同步结束，在下一次排空（下一帧前或一次合并的分发器回调）结束；在此之前其运行仍在继续。
- **`DelayAsync` 负值：** 负的延迟（`Timeout.InfiniteTimeSpan` 除外）在调用处同步抛出 `ArgumentOutOfRangeException`。
- **`Animated.Properties` 跟随祖先：** 运行在宿主的 `Get` Scope 中，因此跟随祖先 `DurationScale`、随祖先 Scope 结束，并在换父级时恢复。
- **已结束 Scope：** 在已结束的 Scope 上启动运行抛出 `ObjectDisposedException`；状态对象本身仍可使用（用新 Scope 再运行）。
- **帧等待只能消费一次：** `NextFrameAsync` 返回的 `ValueTask` 只能 await 一次（`ValueTask` 的约定；等待者已池化）。

## 10. 检查清单 · Checklist

1. 每个 TopLevel：`VisualAnimationScope.Initialize` → `AnimationScope.Initialize`。
2. 删除 `VisualAnimationScope? _motion` 字段及附着/脱离中的 `Create`/`Dispose`；在使用处 `AnimationScope.Get(this)`。
3. `CreateAnimatable/CreateTransition/CreateInfiniteTransition` → 构造函数（第 4 节），状态对象改为字段初始化。
4. 每个运行调用加 Scope 第一参数（第 5 节）；`motion.Clock.NextFrameAsync` → `scope.NextFrameAsync`。
5. `{ IsEnding: false }` 守卫：删除或改为 `Lifetime.IsCancellationRequested`。
6. 需要换父级后重启的组件：`AddScopeChangedHandler`。
7. `Animated.DurationScale` → `AnimationScope.DurationScale`。
8. 测试：`ManualAnimationFrameClock` → `ManualFrameSource`，`TestClock` → `FrameSource`。
9. 构建、跑 M3E 测试；对照第 9 节检查快照/时序断言。

## 附录：M3E 调用点统计 · Appendix: call sites in `src/Material3Expressive`

只读 grep，`grokbot/anim-foundation@f4b16f1`，`*.cs` 与 `*.axaml`。命令：

```bash
cd src/Material3Expressive
rg -c '<pattern>' -g '*.cs' -g '*.axaml' . | awk -F: '{s+=$2} END{print s}'   # 次数
rg -l '<pattern>' -g '*.cs' -g '*.axaml' . | wc -l                           # 文件数
```

| 模式 · Pattern | 次数 | 文件 | 节 |
|---|---|---|---|
| `VisualAnimationScope\b`（全部） | 109 | 46 | 2 |
| `VisualAnimationScope\.Create\(` | 41 | 38 | 2 |
| `VisualAnimationScope\.Initialize` | 0（库内；Catalog 见下） | 0 | 1 |
| `CreateAnimatable\|CreateTransition\|CreateInfiniteTransition` | 82 | 38 | 4 |
| `new Transition<` | 1 | 1 | 4 |
| `new Animatable<` / `new InfiniteTransition(` | 0 / 0 | 0 | 4 |
| `\.AnimateToAsync\(` | 59 | 37 | 5 |
| `\.AnimateDecayAsync\(` | 3 | 3 | 5 |
| `\.RunAsync\(` | 11 | 10 | 5 |
| `\.AnimateAsync\(`（SuspendAnimation） | 2 | 2 | 5 |
| `\.Clock\b` | 55 | 41 | 5, 6 |
| `NextFrameAsync` | 47 | 38 | 5 |
| `IsEnding` | 72 | 37 | 2 |
| `CreateChild\(` | 18 | 12 | 3 |
| `IAnimationFrameClock` | 4 | 2 | 6 |
| `ManualAnimationFrameClock` / `TestClock` | 0 / 0 | 0 | 6 |
| `Animated\.DurationScale\|DurationScaleProperty` | 1 | 1 | 7 |
| `LastError\|ActiveTaskCount\|PendingAnimationCount…` | 0 | 0 | 8 |
| `AnimationFailed` | 0 | 0 | 8 |
| `\.Dispose\(\)`（含非动画用途，需人工筛选） | 155 | 57 | 2 |

Material3Expressive.Catalog 中另有 22 个文件使用 `VisualAnimationScope.Initialize`。
