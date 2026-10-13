# AnimatedState：显式目标与长期属性连接

`AnimatedState<T>` 在 `ComposeAnimation.Avalonia` 中，命名空间为 `ComposeAnimation`。它持有一条目标驱动动画，合并显式目标更新，并管理 Visual owner 的 Scope 恢复。属性连接只消费同一采样流，不建立额外动画或子 Scope。

## 创建、更新、读取

```csharp
using ComposeAnimation;
using ComposeAnimation.Runtime;

_width = AnimatedState.Create(
    owner: this,
    initialValue: GetTargetWidth(),
    label: "Rail width");

_width.ValueChanged += _ => InvalidateMeasure();

// 属性变化，或测量中得出目标时：
_width.UpdateTarget(GetTargetWidth(), GetWidthSpec());

// 布局消费已有采样：
var currentWidth = _width.Value;
```

GetTargetWidth/GetWidthSpec 代表组件自己的目标与规格选择。创建后 Value/TargetValue 都等于 initialValue，不播放初始化动画。

常用工厂覆盖 double、float、int、Point、Vector、Size、Rect、Thickness、CornerRadius、Color。自定义值类型使用泛型构造函数，提供现有 ITwoWayConverter<T>。

`TargetValue` 是最新请求，立即可读；`Value` 是已发布采样；`IsRunning` 表示已开始的受管运行，不包含单纯待提交请求。

## 在 Measure 中更新

UpdateTarget 不求值 spec/converter、不启动运行、不修改 Value、不执行用户通知。可以在同步 MeasureOverride 中反复调用，采样截止点采用最后的目标与 spec 组合。

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var target = CalculateTarget(availableSize);
    _width.UpdateTarget(target, GetWidthSpec());
    return MeasureChildren(_width.Value);
}
```

帧前提交的新运行加入到达帧的采样批次，不先额外等待一次 NextFrameAsync。截止点之后收到的请求属于下一批；空闲时按需唤醒，稳定后停止请求帧。

这不是最终布局分支选择协议。组件仍负责识别哪些测量结果可成为目标；目标来自动画约束下的输出时要检查反馈关系，需要独立目标布局时使用 Lookahead。

## 属性输出与模板替换

动画采样写入 Avalonia 属性必须使用 Animation 优先级。用状态的 Bind 建立长期连接：

```csharp
private IDisposable? _opacityConnection;

protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    base.OnApplyTemplate(e);
    _opacityConnection?.Dispose();
    _opacityConnection = null;

    if (e.NameScope.Find<Border>("PART_Content") is { } target)
        _opacityConnection = _opacity.Bind(target, Visual.OpacityProperty);
}
```

_opacity 在稳定 owner 上创建一次。换模板只释放、替换连接，新部件立即取得当前值，不重建状态或从初值重新开始。

- 连接支持 Visual 与同类型 StyledProperty<T>，包括继承属性；不支持 DirectProperty 或隐式数值转换。
- Bind 只输出采样，不读取底层属性作为目标，不自动跟随其 PropertyChanged。目标由 UpdateTarget 提交。
- 目标尚未附着时先保存逻辑连接；附着且源 Scope 有效时建立贡献。
- 目标脱离时撤销物理贡献，源动画继续；挂回后立即贡献最新值。
- 动画完成后保持终值贡献；只有暂停或释放时才恢复底层绑定、样式和本地值。
- Connection.Dispose 永久解除该关系，不停止源动画。State.Dispose 释放全部连接并停止自己的运行。

状态持有连接，连接持有目标 Visual。模板替换应显式 Dispose 旧连接，不能仅依赖旧部件脱离。

内部 AnimatedStateConnection<T> 不公开构造函数，不拥有 Animatable 或调度帧。多个属性目标共享一个状态：

```csharp
var progress = AnimatedState.Create(this, 0d);
using var firstConnection = progress.Bind(firstBar, ProgressBar.ValueProperty);
using var secondConnection = progress.Bind(secondBar, ProgressBar.ValueProperty);
progress.UpdateTarget(100d, new TweenSpec<double>(TimeSpan.FromMilliseconds(400)));
```

现有 `scope.BindAnimatedValue(target, property, state)` 仍可消费 IAnimationValue<T>，但它仍是随当前 Scope 结束即撤销的连接；不具有 State.Bind 的自动重连生命周期。

## 规格、阈值和通知

省略 UpdateTarget 的 spec 表示使用创建时的默认规格，不沿用上次调用的显式 spec。没有指定默认规格时采用现有类型阈值的默认 spring。

目标相同只更新配置，不重新启动当前运行；每次 Measure 创建新的 spec 实例也不会因此产生帧需求。Spec 在运行启动时向量化，当前运行继续使用启动参数。

```csharp
var width = AnimatedState.Create(this, 80d,
    defaultAnimationSpec: new SpringSpec<double>(dampingRatio: .8, stiffness: 400),
    visibilityThreshold: .1d,
    label: "Width");
```

未指定 visibilityThreshold 时尊重显式 SpringSpec 的阈值。显式 override 仅作用于 Spring，其他规格不受影响。参数采用已有 Optional<T> 区分未指定与显式 default(T)，调用可直接传 .1d。显式零阈值不会被当作缺省；Runtime 要求 Spring 阈值为正，零值在提交阶段报告错误。

Value 实现 IAnimationValue<T>/INotifyPropertyChanged；ValueChanged 是便利事件，只有转换后的值改变时通知。内部属性连接先发布贡献，再通知外部消费者。

```csharp
_width.Finished += finalWidth => UpdateCompletedDisplay(finalWidth);
```

初始化、相同目标更新、中断、detach、Dispose 不触发 Finished。成功完成时读取当前订阅；已经被不同新目标取代的旧运行不发完成通知。回调中可以 UpdateTarget 或 Dispose；更新进入后续批次，释放阻止后续处理器。

## 宿主生命周期

Visual 入口可以在挂载前创建；离树期间只记录目标，不请求帧。同一轮树调整中的换父沿现有 Scope 重建规则恢复值、速度和帧基准。

真正 detach 后冻结最后的采样值，停止连接和帧需求；重新 attach 从该值、零速度和新时间基准向最新目标运行。回收后呈现另一个数据项时，消费者应 Dispose 旧状态并用当前目标创建新身份。

Scope 重建时先快照并撤销旧贡献，再建立新贡献发布冻结值，随后统一提交恢复运行。撤销/安装仍会触发属性通知，不承诺原子替换。跨根使用新时间原点。

恢复 spring 接续当前值与速度；tween/keyframes 等重新计算后续运行，不承诺保存原始剩余时长或 keyframe 阶段。

需要窗口或控制器持有 hoisted state 时，可以固定连接显式 Scope：

```csharp
var progress = AnimatedState.Create(AnimationScope.GetRoot(window), 0d);
```

该 Scope 结束会释放状态，不自动跟随其 Host 的后续 Scope 替换。Dispose 状态不 Dispose 调用者的 Scope，不影响兄弟运行。全部成员及通知要求 UI 线程。

## 失败与验证

线程、已释放状态和构造参数错误同步抛出。目标转换、spec 或公共通知失败停止该状态，保留最后可用值，通过现有 AnimationFailed 与 Label 报告；同一失败请求不会被布局自动反复重试。新目标、新 spec 引用或新有效连接可重新尝试。

单个属性连接安装/发布失败只撤销该连接，源动画和其他连接继续。修复后可 Dispose 并重新 Bind；Finished 回调失败报告诊断，不重发完成事件。

自动化测试见 AnimatedStateTest：合并提交、Measure 内通知、Runtime 中断对照、阈值、属性底层值、同根/跨根恢复、detach、模板目标替换、重入与错误隔离。

Catalog 示例：

```powershell
dotnet run --project modules/ComposeAnimation/Catalog/Catalog.csproj -- --demo state/target
```

演示的 Retarget、ReplaceTarget、ReconnectTarget 动作验证共享属性贡献与目标生命周期。手动帧/headless 数值验证不等于所有 native 平台的调度或逐像素一致性验证。
