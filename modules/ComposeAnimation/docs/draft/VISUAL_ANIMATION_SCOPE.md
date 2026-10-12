> **历史设计稿**：本文早于 2026-10 的动画基础重构，名称已过时。现行名称见 `/workspace/m3e-anim-refactor/ROADMAP.md`：`SharedAnimationFrameClock` → 内部 `FrameClock`，`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`AnimationScope<T>` → `AnimationRun<T>`。

# VisualAnimationScope 与动画宿主集成设计草案

状态：已实现；使用与迁移见 [VisualAnimationScope](../usage/visual-animation-scope.md)，验证范围见 [实施记录](../VISUAL_ANIMATION_SCOPE_IMPLEMENTATION.md)。

日期：2026-09-30。

范围：ComposeAnimation.Runtime、ComposeAnimation.Avalonia、测试支持，以及仓库内消费者和 Material3Expressive 的迁移。

本文保留首个控件库消费者反馈及后续讨论形成的设计约定。下文的实施顺序和待设计条目是原始设计记录；当前接口、明确取舍与验收结果以上述使用文档和实施记录为准。

## 1. 目标与已确定的取舍

现有动画原语能够表达当前 Material 控件的主要动画流程。下一轮改进集中于生命周期、时钟、值消费和测试，使控件作者不必重复维护通用动画基础设施。

核心消费流程为：

> 创建 Scope → 从 Scope 创建动画 → 订阅或绑定动画值 → 启动流程 → 结束 Scope。

已确定的取舍：

1. 帧源在应用 TopLevel 的动画根环境初始化时确定，不支持运行中换钟。卸载、模板替换和重新挂载通过结束旧 Scope、创建新 Scope 处理。
2. 不保留旧 API 兼容层，不新增过渡别名。清楚记录破坏性变更，迁移实际消费者。
3. `CreateAnimatable` 属于 Scope 的消费入口，并建立真实的生命周期约束，不只是省略 converter 和 clock 参数。
4. Runtime 保留显式时钟构造，服务于底层测试和独立运行等实际用途；这不是兼容措施。
5. Elevation、Ripple 和形状等流程继续直接使用 `Animatable`。不为了复用绑定和生命周期设施而强制改换播放模型。
6. 保持 Runtime 的取消传播语义。正常中断在宿主任务观察边界处理，不能把内部取消改成成功，否则后续顺序步骤可能继续执行。
7. 暂不把 `LaunchLatest`、运行时钟迁移或全局时钟身份注册表纳入首版。

## 2. 保留的动画使用路径

| 场景 | 推荐模型 | 控件作者负责 |
| --- | --- | --- |
| 基础属性由样式、绑定或赋值决定 | `Animated.Properties` | 目标值与声明 |
| 交互顺序、并行播放、等待释放、形状反转 | `Animatable` + Scope | 动画流程与领域规则 |
| 多个值共同跟随离散状态变化 | `Transition` + Scope | 状态映射与规格选择 |
| 多个值持续循环 | `InfiniteTransition` + Scope | 循环规格与绘制关系 |

Material 继续负责交互优先级、elevation 规格、Ripple 快速点击与释放规则、形状反转时的进度和速度转换、第三目标处理等。

`Transition` 与 `Animatable` 的中断行为仍然不同。当前 `Transition` 在特定中断场景使用 interruption spring；不得假设它可以等价替代必须维持 tween 规则的流程。

## 3. Scope 的职责与层次

`VisualAnimationScope` 表示一组动画工作的所有权和生命周期。它与 Runtime 中表示单次动画逐帧状态的 `AnimationScope<T>` 是不同概念。

### 3.1 职责

| 职责 | 契约 |
| --- | --- |
| 动画环境 | 提供固定帧源及共享帧请求能力 |
| 动画对象生命周期 | 管理通过工厂创建的对象，结束后禁止新的变更操作 |
| 异步流程 | 接管 `Launch` 的任务，提供取消并完整观察失败 |
| 值消费资源 | 管理通过它建立的订阅和动画属性绑定 |
| 诊断 | 区分正常取消与失败，在根入口提供可定位、去重的错误报告 |

Scope 不决定 Material 目标值、动画规格、绘制算法或交互策略，也不承担任意业务资源的通用服务容器职责。

### 3.2 所有权层次

| 类型 | 创建时机 | 结束条件 |
| --- | --- | --- |
| 应用 TopLevel 根 Scope | 加载内容之前 | TopLevel 生命周期结束 |
| 控件 Scope | 控件挂载期间 | 卸载或显式释放 |
| 模板 Scope | 模板建立 | 模板替换或父 Scope 结束 |
| 临时效果 Scope | 一次效果开始 | 效果结束或父 Scope 结束 |
| 派生弹出宿主 Scope | 内部弹出宿主首次需要动画环境之前 | 弹出宿主结束或所属根环境结束 |

结束子 Scope 不影响兄弟 Scope；结束父 Scope 会结束全部子 Scope。

控件 Scope 由控件保存。`Create(owner)` 每次返回新 Scope，不是获取缓存中的控件单例。一个控件上的不同内部消费者可以拥有不同 Scope，但共享根环境。

`CreateChild()` 建立显式父子所有权关系。首版不沿视觉树猜测最近的任意控件 Scope。

## 4. 拟议 API 表面

以下是 API 轮廓，省略实现。具体重载、泛型推断和命名仍需通过编译及真实消费验证，但不得削弱本文契约。

```csharp
public sealed class VisualAnimationScope : IDisposable, IAsyncDisposable
{
    public static VisualAnimationScope Initialize(
        TopLevel host,
        IAnimationFrameClock? clock = null);

    public static VisualAnimationScope Create(Visual owner);

    public VisualAnimationScope CreateChild(string? label = null);

    public IAnimationFrameClock Clock { get; }
    public CancellationToken Lifetime { get; }
    public Exception? LastError { get; }

    public event EventHandler<AnimationFailureEventArgs>? AnimationFailed;

    public Task Launch(
        Func<CancellationToken, Task> operation,
        string? label = null);

    public void Dispose();
    public ValueTask DisposeAsync();
}
```

补充约定：

- 所有 Scope 操作在所属 UI 线程执行。
- `Initialize` 注册唯一根环境，重复初始化属于使用错误，不执行替换。
- `Create(owner)` 要求 owner 已关联有效宿主；创建内部派生弹出环境的规则见第 10 节。
- `Clock` 只读，返回本 Scope 的受控时钟入口，不返回可供消费者销毁的根分发器。
- `AnimationFailed` 作为根诊断入口使用，应用在 `Initialize` 返回的根 Scope 上订阅，不在每层重复报告。
- 工厂、订阅和绑定可以通过方法或扩展方法组织；从消费角度，它们都属于 Scope。

## 5. 工厂与动画对象生命周期

### 5.1 工厂负责什么

主要入口：

```csharp
var alpha = scope.CreateAnimatable(0d);
var offset = scope.CreateAnimatable(new Vector());
var shape = scope.CreateAnimatable(initialShape, shapeConverter);
var transition = scope.CreateTransition(initialState);
var infinite = scope.CreateInfiniteTransition();
```

工厂选择或接受 converter，注入 Scope 时钟，应用明确的类型阈值，并绑定对象生命周期。

不新增独立的静态 `Animatable.Create(...)`。泛型创建和生命周期登记作为基础能力，常见类型可以通过扩展重载提供。自定义类型继续显式传 converter，不引入全局类型注册。

工厂只创建对象，不隐式启动播放。`Transition.RunAsync` 与 `InfiniteTransition.RunAsync` 通过 `Launch` 显式接管。

### 5.2 比 Stop 更强的约束

`Animatable.Stop()` 仅取消当前操作，不能代表对象生命周期结束。Scope 工厂必须绑定一个不可恢复的内部生命周期状态。

| 状态 | 允许的行为 |
| --- | --- |
| Active | 正常变更与读取 |
| Ending | 禁止新变更；取消既有工作；允许内部清理 |
| Ended | 读取清理完成后的最终状态；禁止新变更 |

具体要求：

- 结束后新发起 `AnimateToAsync`、`AnimateDecayAsync`、`SnapToAsync`、`UpdateBounds` 等变更，抛出 `ObjectDisposedException`。
- Scope 结束导致既有操作退出，以任务取消表达。
- `Stop`、`Dispose` 等清理操作可以重复调用，保持安全、幂等。
- 检查既要覆盖入口，也要覆盖异步等待之后、提交变更之前的执行边界。
- `SnapToAsync` 不依赖下一帧，必须独立受生命周期约束。
- `Transition` 的目标更新、添加子动画等变更以及无限动画的配置变更，也必须遵守对象有效期。
- 对象不能转移到新 Scope；重新挂载时创建新对象，初始值由控件决定。
- 最后的显示值允许保留；速度、运行状态等字段可以在退出清理中按既有规则收敛，不能承诺取消瞬间所有字段永久冻结。

生命周期检查不得塞入 `IAnimationFrameClock.VerifyAccess()`。线程访问检查与对象有效性是两个独立概念，结束后的清理仍需要合法的线程访问。

实现采用内部生命周期机制，不增加公开动画包装类型，也不让 Runtime 依赖 Avalonia 的 `VisualAnimationScope` 类型。Runtime 显式构造的独立对象不绑定 Visual Scope 生命周期。

## 6. Launch、取消与诊断

### 6.1 启动与返回

- 从所属 UI 线程直接调用委托，执行到第一个未完成的 `await`。
- 不使用 `Task.Run`，不默认排队到下一轮 Dispatcher。
- 内部任务登记在调用用户委托前建立，覆盖同步完成、同步失败及同步重入。
- 委托执行中的同步异常进入返回任务和诊断流程。
- 空参数、错误线程、已结束 Scope 等调用错误可以同步抛出。
- 返回任务保留成功、取消或失败状态，不把取消包装成成功。
- 即使调用者使用 `_ = scope.Launch(...)`，Scope 仍完整观察失败。
- 完成后的任务登记及时解除，不保留永久任务历史。

### 6.2 独立任务与动画重定向

普通任务失败默认不取消兄弟任务。一个 Ripple 的失败不应取消同一按钮的其他 Ripple。

`Launch` 不预先停止已有动画。对同一个 `Animatable` 的新操作仍由它自己的互斥机制处理，以保留现有速度捕获和时间基准规则。

完整流程中的子任务应被等待，并传递对应 Scope 的取消令牌。Scope 无法接管用户在流程中任意创建并丢弃的任务。

### 6.3 错误入口与去重

正常取消和动画中断不报告错误；失败任务不能因为异常类型看起来像取消就被无条件吞掉。实现必须保留任务状态的区别。

`Scope.LastError` 提供最近失败。根 `AnimationFailed` 事件至少包含：

| 字段 | 含义 |
| --- | --- |
| `FailureId` | 同一失败传播过程的来源标识 |
| `SourceScope` | 首次产生受管理失败的 Scope |
| `Host` | 对应的实际 Visual 宿主 |
| `OperationLabel` | 可选操作标签 |
| `Exception` | 原始异常及其上下文 |

没有独立 Visual 的逻辑子 Scope 继承宿主信息，但保留自身来源身份。内部组件的 Scope 即使不直接暴露给应用，错误仍可以定位。

同一子任务失败经等待传播到父任务时，根入口只报告一次。根据受管理任务间的失败传播关系去重，不按消息文本去重，不建立永久增长的全局异常集合。

用户捕获异常后抛出未保留关联的新异常，视为新的失败。混合多个子失败或新失败的聚合异常应逐项识别，不能因其中一项已报告就吞掉其他失败。

旧 Scope 退出期间的错误仍需观察，并标注旧来源，不能覆盖新控件 Scope 的 `LastError`。诊断事件处理器自身抛错时的保护与报告策略需在实现前明确，不能打断必要清理或形成递归报告。

## 7. 结束顺序、异步清理与登记回收

### 7.1 结束顺序

1. 同步将 Scope 及其后代标记为 Ending，阻止新工作和新变更。
2. 先使受管理订阅失效，再撤销绑定，防止清理中的同步通知继续进入消费端。
3. 取消任务、停止动画、取消所属帧等待。
4. 完成资源清理，观察任务退出，转入 Ended。

排队的受管理通知在执行前也检查有效性。清理过程中一项操作失败，不得使其他必要清理被跳过。

准确的隔离承诺是：

> Scope 结束后，不再通过它管理的订阅和绑定发布更新；调用者的异步清理必须操作本次流程捕获的对象。

Scope 不能阻止任意用户 `finally` 修改已指向新模板的字段。示例应捕获本次 `motion`、`surface`、效果对象，而不是在延迟执行的代码中重复读取可变字段。

### 7.2 Dispose 与 DisposeAsync

- `Dispose()` 同步撤销有效性并发出取消，不阻塞 UI 等待任务结束。
- `DisposeAsync()` 在上述操作后，等待受管理任务和子 Scope 的清理实际退出。
- 不能在某个 Scope 接管的任务中等待该 Scope 或包含该任务的祖先 Scope 完整退出，否则会等待自身。实现应在可检测时明确报错。
- 父 Scope 的任务可以等待一个不包含该父任务的子 Scope 退出。
- 任意用户流程仍需合作式响应取消；不承诺强制终止忽略取消的代码。

### 7.3 登记回收

| 项目 | 回收时机 |
| --- | --- |
| 订阅、绑定 | 提前释放或 Scope 结束 |
| 已完成任务 | 完成观察后 |
| 已结束子 Scope | 移出活动子作用域集合 |
| 尚未完成的清理任务 | 保留到实际退出，然后解除登记 |

不能为使活动计数归零而提前丢失仍需等待的任务。也不能因按钮长期挂载，永久保留几千次已结束 Ripple 的对象、任务或子 Scope。

## 8. 固定帧源与按需共享

### 8.1 根环境

每个应用 TopLevel 初始化一个根环境。没有显式时钟时，根环境创建默认 Avalonia 帧源；显式注入的外部时钟由提供者拥有，Scope 不擅自销毁它。

根环境拥有共享分发器。子 Scope 持有自己的受控时钟入口，不拥有共享分发器的销毁权。

首版不提供：

- 可写的 `Scope.Clock`。
- 运行中换钟或跨时钟时间基准迁移。
- 任意控件上的时钟覆盖属性。
- 全局按原始时钟对象身份归并多个应用窗口的注册机制。

一个外部单消费者帧源直接被多个独立应用根环境同时使用，不属于首版保证。测试默认每个独立根使用自己的时钟；需要跨根共享时须另行明确源的多消费者能力。

### 8.2 分发约定

- 保留 `IAnimationFrameClock.NextFrameAsync` 按需等待模型。
- 第一个等待者出现时请求帧；同一根环境最多有一个有效底层等待。
- 一批消费者获得相同的原始时间戳。
- 没有等待者时停止请求，持有空闲 Scope 不产生持续帧开销。
- 单次操作取消只影响自己的等待；Scope 结束只取消自己的等待集合。
- 不重置时间戳、不按 Scope 从零计时、不把时长缩放混入原始帧源。
- 最后一个等待取消后又立即出现新等待，必须与旧底层等待退出协调，不能产生两个竞争请求。
- 分发批次中取消或结束 Scope，也必须阻止剩余失效等待者接收有效帧。

Avalonia 已排队的原生帧回调可能无法撤回。保证是没有继续生效的等待和更新、没有持续请求，而不是原生队列里绝对没有迟到回调。

相同时间戳不意味着跨控件属性通知的原子事务，也不意味着布局或渲染已经完成。

### 8.3 保留局部调度边界

属性动画当前依赖帧前基础值检查、同帧重定向和等待者恢复后的重入处理。这些职责继续留在局部协调器，不提升为窗口级分发器中的属性逻辑。

可以保留内部局部分发包装，但上游必须统一接入根环境，不能各自竞争原始帧源。

根分发泵的 UI 调度策略不能偶然继承第一个调用者的 `Loaded` 等上下文。现有 Render/Normal 调度回归必须继续通过。

帧源失败影响当前根环境中的消费者；动画规格或局部回调失败不能误伤整个根帧源。首版不增加自动重试和热替换机制。

## 9. 动画值消费

建议为现有值对象提供薄只读契约，不创建另一套状态容器：

```csharp
public interface IAnimationValue<out T> : INotifyPropertyChanged
{
    T Value { get; }
}
```

由 `Animatable<T>`、`Transition` 子动画及 `InfiniteTransition` 子动画实现。

Scope 辅助入口：

```csharp
IDisposable SubscribeValue<T>(
    this VisualAnimationScope scope,
    IAnimationValue<T> source,
    Action<T> observer);

IDisposable BindAnimatedValue<T>(
    this VisualAnimationScope scope,
    AvaloniaObject target,
    StyledProperty<T> property,
    IAnimationValue<T> source);
```

共同规则：

- 建立消费关系时立即提供当前值。
- 消费 `Value` 通知及空属性名的全量通知，不额外进行相等值去重。
- 在所属线程执行，Scope 结束后停止受管理发布。
- 返回值允许提前解除；提前解除时移出 Scope 登记。
- 绑定为单向、动画优先级，只贡献采样值，不再次创建过渡。
- 释放仅撤销自己的属性贡献，恢复下层属性来源。
- 不自动接管传入的外部动画对象；消费关系的所有权与对象工厂的所有权分开。

这不是 Compose 的读取依赖自动追踪。例如绘制同时依赖控件尺寸，尺寸变化引发的刷新仍由控件负责。

## 10. 根初始化与内部弹出宿主

### 10.1 应用和测试顺序

必须先初始化根环境，再加载或设置内容：

```csharp
public MainWindow()
{
    VisualAnimationScope.Initialize(this);
    InitializeComponent();
}
```

```csharp
var window = new Window();
VisualAnimationScope.Initialize(window, clock: testClock);
window.Content = control;
window.Show();
```

不能将“尚未调用 Show”作为内容不会开始挂载的保证。普通应用 TopLevel 缺少初始化时给出清晰错误，不允许各个控件自行创建默认帧源。

### 10.2 内部弹出宿主

应用无需取得每个内部 TopLevel 再手工初始化。统一 Scope 解析入口负责：

- 普通应用窗口：使用显式初始化的根环境。
- 独立 `PopupRoot`：依据所属窗口关系，建立派生宿主 Scope。
- Overlay 弹出层：沿用现有 TopLevel 环境。
- 嵌套弹出宿主：递归解析所属环境，避免重复创建。

派生宿主共享窗口已固定的帧源，拥有独立取消边界，诊断 Host 为实际弹出宿主。关闭弹出层不影响窗口其他动画；根环境结束则一并结束派生宿主。

环境必须在返回第一个控件 Scope 前建立，不能依赖后置的 `Popup.Opened` 事件。测试时钟也覆盖这些内部宿主，不能意外使用真实帧源。

因此，“一个物理 TopLevel 必须拥有独立帧源”不是本方案要求。一个应用窗口的固定动画环境可以包含它的内部弹出宿主。

本地 Avalonia 源码显示 `PopupRoot.ParentTopLevel` 提供所属关系，`Popup` 设置内容早于后续打开流程。此为实现依据；具体宿主创建、关闭、复用以及跨平台路径仍需运行验证。

## 11. 常见消费场景

以下示例省略 using、领域类型和规格定义。生命周期相关写法属于规范示例。

### 11.1 自绘控件

```csharp
private VisualAnimationScope? _motion;
private Animatable<double>? _alpha;

protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);

    var motion = VisualAnimationScope.Create(this);
    var alpha = motion.CreateAnimatable(0d);
    motion.SubscribeValue(alpha, _ => InvalidateVisual());

    _motion = motion;
    _alpha = alpha;
}

private void SetVisible(bool visible)
{
    var motion = _motion;
    var alpha = _alpha;
    if (motion is null || alpha is null) return;

    _ = motion.Launch(ct => alpha.AnimateToAsync(
        visible ? 1d : 0d, FadeSpec, cancellationToken: ct),
        label: "Visibility");
}

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    var motion = _motion;
    _motion = null;
    _alpha = null;
    motion?.Dispose();

    base.OnDetachedFromVisualTree(e);
}
```

卸载会自动结束相应 Scope，显式重复释放仍安全。字段先解除关联，减少同步清理重入时误用旧对象的机会。重新挂载的初始值由控件选择；Scope 不自动迁移旧对象。

### 11.2 Elevation 直接驱动属性

```csharp
var motion = _motion;
var surface = _surface;
var elevation = motion.CreateAnimatable(initialElevation);

motion.BindAnimatedValue(
    surface, MaterialSurface.ShadowElevationProperty, elevation);

// 在实际更新方法中捕获本次 motion、elevation、target 和 spec。
_ = motion.Launch(ct => enabled
    ? elevation.AnimateToAsync(target, spec, cancellationToken: ct)
    : elevation.SnapToAsync(target, cancellationToken: ct),
    label: "Elevation");
```

Material 决定交互与规格，Scope 管理任务和绑定。`SnapToAsync` 可结束目标未变的在途播放；无需切换播放模型。

### 11.3 Ripple 的流程和资源归同一个临时 Scope

```csharp
var parentMotion = _motion;
var surface = _surface;

_ = parentMotion.Launch(async _ =>
{
    await using var rippleMotion = parentMotion.CreateChild("Ripple");
    using var ripple = new MaterialRipple(rippleMotion, origin);

    AddRipple(surface, ripple);
    try
    {
        await rippleMotion.Launch(
            ct => ripple.PlayAsync(ct),
            label: "Ripple playback");
    }
    finally
    {
        RemoveRipple(surface, ripple);
    }
}, label: "Ripple lifetime");
```

`MaterialRipple` 是 Material 层类型，内部从 `rippleMotion` 创建动画。播放及释放信号等待使用子 Scope 的令牌。父任务可以等待子任务，但实际流程归子 Scope 所有。

单独结束子 Scope 也能结束等待释放的流程。父任务在子任务退出后等待子 Scope 清理，不构成子任务等待自身。清理只操作捕获对象。

指针释放仍通过 Ripple 的领域信号触发正常收尾；不能等同于销毁作用域。

### 11.4 模板替换

```csharp
private void ConnectTemplate(MaterialSurface surface)
{
    var previous = _templateMotion;
    _templateMotion = null;
    previous?.Dispose();

    var motion = _motion.CreateChild("Template");
    var elevation = motion.CreateAnimatable(initialElevation);
    motion.BindAnimatedValue(
        surface, MaterialSurface.ShadowElevationProperty, elevation);

    _templateMotion = motion;
}
```

模板替换可早于控件卸载。延迟流程必须捕获当次模板 Scope 和部件，不能在 `finally` 中对 `_templateMotion` 字段直接执行清理。

### 11.5 状态过渡与无限动画

```csharp
var motion = _motion;
var transition = motion.CreateTransition(CheckState.Unchecked);
var progress = transition.AnimateDouble(
    state => state == CheckState.Checked ? 1d : 0d,
    _ => CheckSpec);

motion.SubscribeValue(progress, _ => InvalidateVisual());
_ = motion.Launch(ct => transition.RunAsync(ct), label: "Check transition");

transition.UpdateTarget(CheckState.Checked);
```

```csharp
var motion = _motion;
var loop = motion.CreateInfiniteTransition();
var rotation = loop.AnimateDouble(0d, 360d, RotationLoopSpec);

motion.SubscribeValue(rotation, _ => InvalidateVisual());
_ = motion.Launch(ct => loop.RunAsync(ct), label: "Progress loop");
```

Scope 结束时，既要取消帧等待，也要让处于等待状态变化的循环退出。

### 11.6 声明式属性动画

```xml
<Border>
    <ca:Animated.Properties>
        <ca:AnimatedPropertyCollection>
            <ca:AnimatedDouble Property="{x:Static Visual.OpacityProperty}" />
        </ca:AnimatedPropertyCollection>
    </ca:Animated.Properties>
</Border>
```

窗口已初始化根环境，内部协调器创建自己的 Scope。声明可共享，播放状态和生命周期按宿主隔离。

## 12. 单属性立即落值

此能力与 Scope 配合，但仍属于属性播放会话 API：

```csharp
Task Animated.SnapToTargetAsync<T>(Visual owner, StyledProperty<T> property);
```

契约：结束指定宿主、指定属性的 ComposeAnimation 当前过渡，撤销动画贡献，使当前基础目标重新生效，并保留原声明。

- 同目标仍在播放时也必须结束。
- 读取命令执行时的基础值，不能用滞后的 `Animatable.TargetValue`。
- 不覆盖本地值、样式或绑定，不改变共享声明。
- 不依赖下一帧到达。
- 完成后旧播放不能再发布样本、撤销新绑定或报告过期会话错误。
- 无会话时直接完成，不为此创建会话。
- 后续基础值变化仍可产生过渡；这是一次命令，不是持续禁用策略。
- 只撤销自身贡献，不控制其他动画来源。

`SnapSpec` 仍表示动画规格，修改声明 `Spec` 不应被解释为终止当前播放。

## 13. 测试支持

提供可被控件库引用的独立测试支持项目。手动时钟与 Avalonia 调度辅助分离。

```csharp
clock.SendFrame(TimeSpan.Zero);                 // 绝对时间戳
clock.AdvanceBy(TimeSpan.FromMilliseconds(60));  // 增加时间并发送一帧
```

规则：

- 一次 `AdvanceBy` 只发送一帧，不隐式补首帧或模拟中间多帧。
- 新动画通常以首帧建立时间原点，测试显式发送该帧。
- 时间戳不倒退。
- 支持多个等待者、取消、释放及待处理请求计数。
- 帧批次确定后，新登记等待留给后续帧；协调器专门的帧前重定向参与规则独立验证。
- 发帧不承诺所有异步 continuation、布局和绘制已完成。
- Avalonia 测试宿主提供有明确条件和上限的 Dispatcher 推进与完成等待，不使用无限的模糊 RunUntilIdle。
- 帧等待数、活动任务数、活动子 Scope 数和清理等待项都参与泄漏验收；仅帧等待归零不足以证明无泄漏。

## 14. 破坏性 API 变化与迁移

不提供旧 API 别名、兼容开关或双入口长期并存。

| 当前入口或先前提议 | 新方案 |
| --- | --- |
| `Animated.FrameClockProperty` / `GetFrameClock` / `SetFrameClock` | 删除；根 Scope 初始化注入 |
| 控件分别解析和创建默认帧源 | 删除；统一从根环境获取 |
| 运行中换钟及监听分支 | 删除 |
| 面向控件作者的 `SharedAnimationFrameClock` 手动包装 | 收为内部共享设施 |
| 默认 Avalonia 帧源构造 | 由宿主集成内部完成 |
| 独立静态 `Animatable.Create(...)` 提议 | 不新增；使用 Scope 工厂 |
| `Animated.LastError` | 迁到 Scope 诊断和根失败事件 |
| Material 的通用 `MaterialMotionController` | 由 Scope 替代其通用职责 |
| 手动订阅、过滤和解绑动画值 | 使用 Scope 管理的消费辅助入口 |
| `Animated.Properties` | 声明模型保留，内部生命周期改接 Scope |
| Runtime 显式时钟构造 | 作为独立底层能力存在 |

仓库内迁移覆盖所有帧源消费者，而不仅是属性动画：AnimatedContent、AnimatedVisibility、AnimatedContentSize、AnimatedBounds、Lookahead、SharedTransition 相关宿主、Catalog、Scratchpad 和测试。

外部 parent Transition 的连接必须尊重原有所有权：使用者的 Scope 可以拥有自己的连接、子过渡和订阅，不能因消费关系解除而销毁外部 parent Transition。

Material3Expressive 迁移覆盖 elevation、state layer、Ripple、形状，以及 Checkbox/进度条等值消费验证。保留 Material 领域规则，删除通用基础设施重复代码。

### 14.1 Catalog 特殊迁移

Catalog 当前会遍历控件树注入演示时钟，并借助时钟实现独立速度控制。

- 删除逐控件注入及布局后再次注入。
- 测试时钟移到根初始化。
- 演示独立速度控制应迁成明确的 Scope 播放策略，单独定义缩放语义，不能重新开放任意换钟。
- 不能通过删除速度功能来宣称迁移完成。该策略若尚未实现，必须明确记为未完成迁移。

### 14.2 文档迁移

更新使用文档和 CURRENT_STATUS，删除失效的 FrameClock 用法及与实现不一致的结构描述。突出 Scope 初始化顺序、对象有效期、Launch 时序、取消语义、Transition 中断行为及测试首帧规则。

## 15. 延后能力与待完成设计

### 15.1 LaunchLatest

首版不加入。其潜在语义是取消同组完整流程、等待退出、仅执行最后一个待启动请求，不自动传递速度或领域状态。

它不能无条件替代普通 `Animatable` 重定向：当前 Runtime 在取消前捕获速度，而宿主若先取消整个流程，退出清理可能先清零速度。形状反转需要专门验证取消前状态捕获。

### 15.2 实现前还需定稿的细节

- Runtime 内部生命周期机制及所有变更提交边界的完整覆盖。
- 受管理任务失败来源标识、传播去重和记录回收；聚合异常与诊断处理器失败策略。
- 自身及祖先 DisposeAsync 等待检测。
- 帧分发批次、局部帧前钩子和 UI 调度上下文的衔接。
- 派生弹出宿主在创建、复用、隐藏、关闭和嵌套场景中的生命周期。
- Scope 消费关系对已有其他动画优先级贡献的共存行为。
- Catalog 所需的独立播放速度策略，统一不同动画驱动器的缩放行为。

以上属于实现设计和验证工作，不重新开放热换钟、兼容旧 API、取消转成功等已排除方向。

## 16. 验收与实施顺序

### 16.1 必须覆盖的行为

| 类别 | 验收重点 |
| --- | --- |
| 对象有效期 | Scope 结束后 SnapTo/UpdateBounds/新播放不能修改状态；排队操作不能越过生命周期边界；最终值可读 |
| Launch 时序 | 同步启动；同步完成、同步抛错与重入正确登记；返回状态保留 |
| 取消与失败 | 正常中断无误报；独立 Ripple 失败隔离；子父传播只报告一次；新失败不被误去重 |
| 销毁 | 先失效后回调；受管理更新停止；旧清理使用捕获对象；清理失败不跳过其他资源 |
| 异步退出 | DisposeAsync 等到实际退出；自身和祖先等待不会静默死锁 |
| 临时效果 | 单独结束 Ripple Scope 能取消释放信号等待；完成任务和子 Scope 不积累 |
| 帧源共享 | 多控件、多动画只有一个有效根请求；一个 Scope 退出不影响其他；全空闲后不持续取帧 |
| 帧重入 | 批次中取消、取消后立即请求、旧原生回调、同帧重定向和调度优先级 |
| 属性控制 | 同目标立即结束、基础目标刚变化、绑定保留、共享声明隔离、不依赖新帧 |
| 内部宿主 | PopupRoot 首次动画已有环境；Overlay 与嵌套弹出层正确；测试不混入真实时钟 |
| 迁移 | Material 领域行为保持；Catalog 速度及验证功能不丢失；旧入口清除 |

压力验收：长期挂载的按钮连续创建并结束大量 Ripple 后，活动任务、子 Scope、订阅、绑定与帧等待回到基线。

集成验收：同一测试帧源驱动多个控件，每个控件同时运行直接动画和属性过渡；卸载一个控件不影响其余控件，全部退出后没有有效等待及旧生命周期更新。

### 16.2 实施批次

1. 固化测试时钟语义，补生命周期、帧分发和调度边界测试。
2. 实现 Runtime 内部对象生命周期约束，保持独立构造用途。
3. 实现根环境、受控时钟、Scope 树、Launch、诊断和清理机制。
4. 接入工厂、值订阅和属性绑定，完成单属性立即落值。
5. 迁移内置宿主及派生弹出环境，删除旧时钟和诊断入口。
6. 迁移 Material、Catalog、Scratchpad 和测试，更新 API 变化及使用文档。

最终验收不以新增类型数量或删除代码行数为标准。应证明 Material 能删除通用动画基础设施，同时快速交互、禁用、模板替换、卸载重挂和内部弹出层行为仍然正确。

## 17. 当前源码依据

以下是草案分析时核对的主要实现，随迁移可能改变：

- `ComposeAnimation.Runtime/Animatable.cs`：中断前速度捕获、Stop、SnapTo 和退出清理。
- `ComposeAnimation.Runtime/MutatorMutex.cs`：操作替换及等待旧操作退出。
- `ComposeAnimation.Runtime/SharedAnimationFrameClock.cs`：共享等待、批次和取消。
- `ComposeAnimation.Runtime/AnimationFrameClock.cs`：原生帧请求与取消后的迟到回调。
- `ComposeAnimation.Avalonia/AnimatedHostCoordinator.cs`：宿主会话、局部帧前检查和重入队列。
- `ComposeAnimation.Avalonia/AnimatedPropertySession.cs`：动画优先级贡献、基础值读取及释放。
- `ComposeAnimation.Test/AnimationLoopSchedulingTest.cs`：UI 调度优先级约束。
- `Catalog/Framework/DemoFrame.axaml.cs`：逐控件时钟注入及演示生命周期。
- 外部 Material3ExpressiveRefactor 的 `MaterialMotionController`、`ButtonElevationController`、`RippleAnimation`、`AnimatedCornerRadius` 与 `MotionTestClockScope`：首个消费者的迁移和验收样本。
- 本地 Avalonia `PopupRoot.cs`、`Popup.cs`，及项目所用 Avalonia 12.1.2 包文档：内部宿主关系和初始化顺序依据。

本草案作为设计依据保留；当前实现与实际完成的验收范围见实施记录。
