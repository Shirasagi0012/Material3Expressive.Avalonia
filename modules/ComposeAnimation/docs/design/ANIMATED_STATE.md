# AnimatedState：目标驱动动画状态设计

状态：首版已落地。正式用法见 [animated-state.md](../usage/animated-state.md)。本文保留设计契约与适配依据；实现未迁移 M3E 调用方，未改变既有 Runtime 动画算法、帧调度或 Animated.Properties 的行为。

## 1. 定位与范围

`AnimatedState<T>` 是 Avalonia 控件作者使用的目标驱动动画状态，放在 `ComposeAnimation.Avalonia`，命名空间为 `ComposeAnimation`。

组件负责目标值、动画规格、布局和绘制规则；AnimatedState 负责目标提交、运行接管、通知及宿主连接。算法和中断仍消费现有 `Animatable<T>`，不建立第二套求值器。

已讨论的方向：

- 显式 `UpdateTarget(target, spec)`；中断语义固定采用对应 Runtime 行为，不提供 InterruptionMode。
- 可以在 PropertyChanged、MeasureOverride 等同步流程中反复调用。
- 更新请求与执行动画分开；在采样批次开始前合并提交，不在 UpdateTarget 中同步发布采样值。
- 状态身份、宿主连接、单次运行分别管理。
- 按维护者提出的两类模型拆分：AnimatedState<T> 管理唯一动画和 owner Scope 恢复，AnimatedStateConnection<T> 管理属性目标、物理绑定与目标附着状态；模板替换显式释放并重建连接，保留状态。
- Animated.Properties 的重设计留待以后，不迁移现有组件或修改其语义。

首版进一步采用的选择：

- 提供 Visual 自动连接与显式 AnimationScope 两个构造入口，同一状态只有一个运行连接。
- 真正 detach 后冻结值；reattach 从冻结值、零速度和新的时间基准向最新目标运行。
- 实现现有 IAnimationValue<T>，并保留 ValueChanged、Finished 便利事件。
- 用已有 Avalonia.Data.Optional<T> 区分未指定与显式指定 visibility threshold。

以下契约已由仓内实现和回归测试覆盖；完整曲线进度迁移及跨平台显示范围仍明确保留，不能由编译或 headless 测试推导。

不包含 SnapTo、Stop、Decay、速度设置、边界设置、Seek、底层 Animatable 访问或通用业务 effect 框架。需要这些能力时使用 Animatable、Transition 等现有 API。

## 2. 公开 API

以下为公开签名摘要，省略真实方法体：

```csharp
using System.ComponentModel;
using Avalonia;
using Avalonia.Data;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public sealed class AnimatedState<T> : IAnimationValue<T>, IDisposable
{
    // 自动跟随 owner 的挂载和 Scope 重建。
    public AnimatedState(
        Visual owner,
        T initialValue,
        ITwoWayConverter<T> converter,
        IAnimationSpec<T>? defaultAnimationSpec = null,
        Optional<T> visibilityThreshold = default,
        string? label = null) { /* ... */ }

    // 固定连接指定 Scope；不接管 Scope 的所有权。
    public AnimatedState(
        AnimationScope scope,
        T initialValue,
        ITwoWayConverter<T> converter,
        IAnimationSpec<T>? defaultAnimationSpec = null,
        Optional<T> visibilityThreshold = default,
        string? label = null) { /* ... */ }

    public T Value { get; }
    public T TargetValue { get; }
    public bool IsRunning { get; }
    public string Label { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<T>? ValueChanged;
    public event Action<T>? Finished;

    public void UpdateTarget(
        T targetValue,
        IAnimationSpec<T>? animationSpec = null) { /* ... */ }

    public IDisposable Bind(
        Visual target,
        StyledProperty<T> property) { /* ... */ }

    public void Dispose() { /* ... */ }
}
```

增加非泛型 `AnimatedState.Create(...)` 类型重载，覆盖当前 Animatable.Create 支持的 float、double、int、Point、Vector、Size、Rect、Thickness、CornerRadius、Color。提供 Visual 与 AnimationScope 两组入口，复用已有转换器和类型默认阈值。

普通用法不需要重复构造 converter，也不需要配置一个通用 options 对象：

```csharp
_width = AnimatedState.Create(
    owner: this,
    initialValue: GetTargetWidth(),
    label: "Rail minimum width");

_width.ValueChanged += _ => InvalidateMeasure();

// 属性变化或布局推导目标时：
_width.UpdateTarget(GetTargetWidth(), GetWidthSpring());

// 布局消费：
var width = _width.Value;
```

### 固定配置与读取语义

- 初始 Value 和 TargetValue 都是 initialValue；IsRunning 为 false，不播放初始化动画，不触发 Finished。
- converter、默认 spec、threshold override、Label 在创建后不更换。改变身份或转换语义时创建新状态。
- `Value` 是最后发布的采样值，不在 getter 中启动动画或提交目标。
- `TargetValue` 是最新请求目标，包括尚未提交和脱离树期间的更新。
- `IsRunning` 表示已登记的受管运行，包括接管时等待旧运行清理的阶段；单纯待提交请求不算正在运行。
- 中断接管或同一帧的 Scope 恢复不向外暴露内部清理产生的短暂 false/true。
- Dispose 后 Value、TargetValue、Label 保留最后的快照，IsRunning 为 false；UpdateTarget 抛 ObjectDisposedException。
- 全部操作和通知在创建线程执行；两个宿主入口均要求 Avalonia UI 线程。

## 3. 目标、规格与阈值

### 一次请求是目标与规格的组合

`UpdateTarget(target, null)` 和省略 spec 均表示使用创建时的默认 spec，不沿用上一次调用显式提供的 spec。

没有提供默认 spec 时，使用缓存的默认 spring。默认 spring 的阈值沿用现有 converter 类型默认值；未知 converter 使用 Runtime 的向量默认值。

每次调用保存本次 target 与所选 spec。连续更新时一起替换，不分别合并：

```text
UpdateTarget(240, springA)
UpdateTarget(180, tweenB)
采样截止点：提交 (180, tweenB)
```

相同目标也接受配置更新，但不因此重启正在运行的动画。保存两类配置：最新请求配置，以及当前运行启动时的配置。临时 Scope 恢复且目标未变时继续使用当前运行配置；真正 detach 后的重新启动使用最新请求配置。

Spec 是现有可变对象，UpdateTarget 保存引用，不在调用中执行 Vectorize 或深复制。内建 spec 在运行启动时向量化，参数由此固定到本次运行。仅修改 spec 对象而不更新目标，不承诺触发新的运行；自定义 spec 的可变性规则仍由该 spec 自身负责。

### 相等比较

采用 EqualityComparer<T>.Default，不使用距离容差作为目标相等规则。visibility threshold 决定动画何时完成，不决定是否接受新目标。

- 请求阶段与最新请求比较。相同目标只更新配置，不因为新的 spec 实例而制造帧需求；已有待提交项则更新其所选 spec。处于失败状态时新的 spec 引用可以安排一次重试。
- 提交阶段与当前受管运行的目标比较，决定是否接管。
- 处于空闲或重新连接状态时，还要比较当前值与目标；不能因为旧运行目标相同就忽略尚未到达的目标。
- TargetValue 在记录阶段立即可读；其 PropertyChanged 通知延后到提交或重新连接阶段。
- 目标应具有稳定的值相等语义；可变引用类型需要由调用者在更新时提供稳定值。

例如运行目标为 200，提交前收到 240 → 180 → 200，则取消待提交的其他目标，继续原运行。不能只修改 TargetValue 而遗留 180 的队列记录。

### Visibility threshold

未指定 override 时尊重显式 SpringSpec 的阈值；显式指定 override 时，仅对 SpringSpec 覆盖阈值，保留 dampingRatio 和 stiffness。Tween、Keyframes 等不受该参数影响。

泛型 `T? = default` 无法可靠地区分值类型的“未指定”与显式 default(T)，所以采用现有 Optional<T>，不新增包装类型。调用仍可以是 `visibilityThreshold: 0.1f`。

实现需注意当前 SpringSpec<T> 用 default(T) 判定阈值是否存在。不能把 Optional 的值直接传给该构造函数而丢失显式 default(T)；可在 Avalonia 层内部解析覆盖后的向量化 spring，复用 Runtime 的 VectorizedSpringSpec。若需要改变 Runtime 公共 API，另行审阅，不作为本设计的隐含授权。

阈值通过 converter 解释，不在这层引入 DIP/PX 换算或新的颜色空间约定。Color 的常用工厂沿用当前向量默认阈值，不用颜色值近似 Oklab 每通道阈值。

允许 IAnimationSpec<T>，不在这个 API 中额外限制为有限动画；无限规格不会触发 Finished。具有多轨循环语义的场景仍优先使用 InfiniteTransition。

## 4. UpdateTarget 与 MeasureOverride

UpdateTarget 执行线程/释放状态检查、记录最新请求、登记提交需求。它不执行以下工作：

- 启动、停止或等待底层动画；
- 调用 converter/spec 求值；
- 修改 Value；
- 发布 PropertyChanged、ValueChanged 或 Finished；
- 调用 InvalidateMeasure、UpdateLayout 或用户回调。

因此同一个方法可以在属性变化和 MeasureOverride 中使用，不需要 Layout 模式、自动探测布局栈或手工 TargetCommit 选项。

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var target = CalculateTarget(availableSize);
    _width.UpdateTarget(target, GetSpec());
    return MeasureChildren(_width.Value);
}
```

在正常的同步布局调用期间，UpdateTarget 不改变本次布局读到的采样值。调用者在布局中自行泵送 Dispatcher、执行嵌套事件循环不属于这个保证；不能仅靠公开布局 API 检测或修复这种重入。

### 最新请求与最终布局结果

保证的是“采样截止点之前的最新请求”，不保证某次 Measure 是最终呈现约束，也不自动识别探测、intrinsic 或被父级放弃的测量分支。

组件决定哪些测量结果可以成为目标。需要独立目标测量或 placement 选择时，使用已有 Lookahead 协议；AnimatedState 不建立第二套目标布局。

目标与消费应避免无意反馈：用动画中的约束测量子内容，再把该测量结果当作新的终点，可能导致目标追逐或振荡。调度合并不承诺使这种关系收敛。

### 初值只能在布局中确定时

首版保留明确的 initialValue，不增加未初始化状态、HasValue 或隐式首目标初始化协议。可以在第一次适合呈现的附着测量中创建字段：

```csharp
// _size 是控件字段，类型为 AnimatedState<Size>?。
// 本例由组件确认 measuredTarget 是可提交目标，不是离树探测结果。
_size ??= AnimatedState.Create(this, measuredTarget, label: "Measured size");
_size.UpdateTarget(measuredTarget, spec);
var current = _size.Value;
```

第一次创建采用该 initialValue，不播放初始化动画；后续 pass 的新目标正常合并。这个选择不承诺把第一次可呈现测量自动升级为“最终布局结果”。若需要首轮目标布局稳定后才初始化，应单独设计该组件的初值协议。

## 5. 帧提交与按需唤醒

每个现有根帧环境有一份待提交状态集合。它只协调 AnimatedState 的目标请求，不改变根时间戳、FrameClock.ResumePriority 或已有宿主的调度方式。

采用当前帧前钩子，明确顺序：

```text
根 Scope 排空/重建
  → 取得 AnimatedState 请求快照
  → 提交快照中的运行接管
  → FrameClock 取得等待者快照
  → 本批次等待者采样
  → 通知消费者，排队布局失效
```

根 Scope 的 DrainStale 已在根创建时注册。新协调器在其后注册；与 Animated.Properties 等其他帧前消费者保持既有注册顺序，不在本任务中修改其行为。

请求快照包含状态、连接 generation、target 与 spec。先截取整个集合再执行提交；提交或通知期间收到的新请求留给下一批，不能一边遍历一边追到队列为空。

结构性失效优先于快照：状态 Dispose、真正断开或连接 generation 改变后，跳过旧项。普通目标更新不改变连接 generation。

### 空闲启动

只有帧前钩子不会驱动 FrameClock。第一个有效待提交请求需要启动一个共享的唤醒等待者，主动请求下一帧。帧前提交启动的新 Animatable 等待者加入到达帧的等待者快照，不另加一次 NextFrameAsync 延迟。

共享唤醒流程在本批次等待者恢复后检查是否还有待提交请求：有则登记下一帧等待，无则退出。特别是提交期间的新请求，不能只登记一个也会在本批次消耗掉的等待者，否则可能失去后续唤醒。

- 每个根环境最多一个提交唤醒流程，不为每次 Measure 创建 Task、CTS 或 Dispatcher 回调。
- 活动动画仍通过当前 Animatable/FrameClock 等待帧。
- 相同目标的布局重复调用不因重新构造等效 spec 而请求额外帧；有失败需要重试时按失败规则处理。
- 脱离状态不进入根待提交集合；移除状态后不能由集合保留它的 Visual。
- 不制造时间戳，不用墙钟替代帧时钟，不增加 Loaded 回调作为通用提交路径。

### 批次截止点与通知重入

在快照取得之前的更新属于本批次；之后的更新属于下一批。即使后者在另一个状态开始提交之前发生，也不修改已经截取的请求快照。

这保证批次有界，但不保证所有状态的事件构成一个原子快照。多个轨道需要状态机和一致的转换描述时使用 Transition；不向独立 AnimatedState 额外承诺跨状态通知原子性。

调用发生在截止点之后会进入下一批，不保证当前显示帧生效。它没有“先等一帧提交，再等一帧首次采样”的固有两阶段等待；真实显示延迟需要单独测量。

## 6. 单次运行与中断

每个状态只有一个内部 Animatable。新目标提交时直接调用现有 AnimateToAsync，使用其 MutatorMutex、速度捕获和上一帧时间衔接。不先 Stop 再 await 旧运行，以免破坏中断信息。

保存当前受管运行的身份、目标与启动规格。运行身份用于过滤旧 Task 的完成、取消与错误后续；请求合并和连接失效使用各自的身份，不用一个 generation 同时承担全部意义。

正常结束时，只有以下条件全部满足才触发 Finished：

1. 该运行仍是有效的受管运行，状态未释放，连接仍有效。
2. Runtime 返回正常完成，而不是取消、失败或其他结束原因。
3. 最新请求目标仍与该运行终点相同，没有被新的不同目标取代。

完成判定不是简单比较“请求版本未变化”。例如完成前请求 B 后又请求回当前终点 A，B 尚未提交，则原 A 运行仍可正常完成。

初始化、重复相同目标、被新运行接管、detach、Scope 结束及 Dispose 都不触发 Finished。完成后没有新目标时重复 Measure 不重复触发。

## 7. 状态与连接生命周期

### Visual 自动连接

状态可以在 owner 尚未挂载时创建。先保存初值和请求，附着后解析 AnimationScope.Get(owner)，采用其 DurationScale、Popup 父关系和根帧时钟。

AnimationScope.Initialize 的现有要求不变：应用 TopLevel 必须在加载内容前初始化。本设计不自动建立缺失的根环境。

| 情况 | 状态与连接行为 |
| --- | --- |
| 尚未 attach | 保留 Value/TargetValue，不启动动画，也不请求帧 |
| 普通目标变化 | 合并请求，在采样边界接管 |
| 临时 detach 后在 Scope 排空前挂回 | 暂时停止外部通知，沿既有重建规则恢复连接 |
| 同根 Scope 重建 | 保存当前值、速度、帧基准、目标和运行规格，在新 Scope 恢复 |
| 跨根环境换父 | 携带值和速度，丢弃旧根时间戳，在新根建立首次采样基准 |
| 排空时仍脱离 | 撤销连接资源、取消运行，冻结最后的 Value，清除运动速度和时间基准 |
| 真正脱离期间 UpdateTarget | 更新请求；不通知、不请求帧 |
| 再次 attach | 从冻结值、零速度、新基准向最新请求目标运行；值已等于目标时保持空闲 |
| 根环境结束 | 停止并断开；即使 owner 暂时仍在树中，也不能因后续布局重新接入已结束根 |
| 显式 Dispose | 永久结束，取消自己的运行，移除事件连接及待提交记录 |

Visual 入口的连接结束不自动 Dispose 状态。owner 随后进入新的有效根环境仍可建立新连接；留在已结束根中的 UpdateTarget 只能保存请求。

真正 detach 后清除速度是明确的平台适配：避免不可见期间的旧运动动量在重新呈现时继续。暂时换父不采用这项重置。

### 恢复承诺

沿用现有 D17 的值、速度和帧基准恢复模型，不宣称恢复所有 spec 的原始播放进度：

- spring 从当前值和速度朝目标继续，复用 Runtime 中断机制。
- tween/keyframes/repeatable 在 Scope 取消后重新启动朝目标的运行，时长/阶段可能重新计算；不保证保留原剩余时长或 keyframe play time。
- 跨根环境不能直接携带旧 LastFrameTime；不同根的时间原点可能不同。

完整曲线进度迁移需要额外的运行状态协议，未纳入首版，不能通过记录 AnimationState.Value/Velocity 假装已支持。

### 显式 AnimationScope 连接

适用于 hoisted state 或由窗口/控制器统一持有的状态。Scope 固定，不自动跟随其 Host 的后续 Scope 替换。Scope 结束时自动 Dispose 此状态；需要跟随控件换父时选择 Visual 入口。

AnimatedState.Dispose 只结束自己的运行和注册，不 Dispose 调用者提供的 Scope，不取消兄弟动画。

同一状态可被多个同线程消费者读取和订阅，但只有一个运行连接。首版不公开运行宿主的 Attach/Detach/Rebind 或多宿主 provider 接口；独立构造入口保留状态与运行宿主的职责区别。Bind 创建的是输出属性连接，不为状态添加另一个运行宿主。

### 回收与模板

DataContext 改变不自动判定动画身份改变。临时 detach 与回收后呈现不同数据项也不自动等价。

消费者需要新身份时 Dispose 旧状态，再用当前目标创建新状态。模板部件的状态归该部件持有；跨模板保留的状态由稳定 owner 或显式 Scope 持有。Finished/ValueChanged 的闭包不应长期捕获已替换模板部件。

状态活跃时可以由连接持有；脱离后移除所有根环境记录。仅有 state 与 owner 之间的对象/事件关系不应使根窗口继续持有离树控件。

## 8. 通知和消费

实现 IAnimationValue<T>，因此现有 SubscribeValue、BindAnimatedValue 和 C# Value 绑定可以消费此状态。

ValueChanged 是便捷事件，与 PropertyChanged(Value) 使用同一次发布，无额外调度。只在 EqualityComparer<T>.Default 判定采样值变化时发布 Value 通知；整数/颜色转换导致相同值的帧不额外要求布局。该行为不修改 Runtime Animatable 自身的逐帧通知。

发布顺序：

1. 保存新 Value。
2. PropertyChanged(Value)。
3. ValueChanged(newValue)。
4. 正常结束时更新 IsRunning，再执行有效性检查并触发 Finished。

内部属性连接在保存新 Value 后、上述公开通知前更新贡献，让用户处理器读到已更新的目标属性。

每个用户通知返回后检查释放及连接有效性。通知中 UpdateTarget 仅登记下一批请求；通知中 Dispose 会阻止后续发布。

事件订阅保持到显式取消或状态 Dispose，不需要调用者在每次 attach 时重新订阅。未连接期间不发布通知；重新连接时补发必要的 TargetValue/IsRunning 变化，Value 变化仍按实际采样发布。

SubscribeValue 在订阅时立即提供当前值的既有语义不变，这不意味着 UpdateTarget 会同步提供值。

### Avalonia 属性贡献与两类对象

维护者已确认：动画采样写入 Avalonia 属性时必须使用 BindingPriority.Animation，不能用普通属性 setter 或未指定动画优先级的 Value 绑定代替。Binding 是提供值的方式，Animation 是贡献优先级，两者不冲突。

继续采用现有 BindAnimatedValue 的基本形式：以单向 Animation 优先级绑定采样，释放时只撤销自己的贡献，让底层绑定、样式或本地值重新显现。内部字段和直接参与 Measure/Render 的数值不属于 Avalonia 属性贡献，无需引入属性优先级。

现有 Scope.BindAnimatedValue 的属性贡献仍归调用时指定的 Scope 所有，Scope 结束会撤销，不改变其既有契约。新 AnimatedState.Bind 是长期输出连接的入口，由 AnimatedStateConnection<T> 管理每一代物理贡献。

| 对象 | 持有的状态与职责 |
| --- | --- |
| AnimatedState<T> | 唯一 Animatable、最新目标/配置、运行协调、owner/显式 Scope 连接、Scope ending 快照与恢复、属性连接集合 |
| AnimatedStateConnection<T> | 源状态引用、目标 Visual、StyledProperty<T>、目标附着状态、当前物理绑定、暂停/释放标记及物理绑定 generation |

Connection 不持有 Animatable、不请求动画帧、不自行决定目标或规格，也不为每条连接创建子 Scope。多个目标消费同一状态时，共享一条运行和采样流。

公开入口采用 `IDisposable Bind(Visual target, StyledProperty<T> property)`。返回对象的实现是 AnimatedStateConnection<T>，首版作为内部 sealed 类，不公开构造函数或手工 Activate/Suspend；调用者只需保存并 Dispose。

Bind 只建立输出关系，不读取目标属性的底层值作为动画目标，不自动跟随目标 PropertyChanged。目标仍由 UpdateTarget 显式提交；不能把动画输出重新当作目标输入。

首版要求状态与属性同一个 T，不隐式转换数值类型。支持 StyledProperty 的动画贡献，包括无需读取隐藏底层值的继承属性；非 Visual 的 AvaloniaObject 输出留待有具体 owner 接入需求时扩展，不为它先增加第三类宿主。

### 输出连接的生命周期

- 可以在源状态或目标尚未附着时建立逻辑连接；当源具有有效运行 Scope 且目标已附着时，建立物理绑定并立即贡献 state.Value。Bind 本身可能同步引发目标属性通知，不套用 UpdateTarget 的无同步通知保证。
- 物理绑定使用源状态当前的 Scope 管理资源；目标自己的 Scope 不驱动状态，也不创建额外动画环境。源决定时钟/DurationScale，目标附着状态决定是否贡献。
- 目标 Detach 时先禁用该连接的发布、使物理 generation 失效，再撤销自己的贡献。逻辑连接保留，目标重新 Attach 时可贡献源的最新 Value。
- 单个目标脱离或连接 Dispose 不停止源动画，不影响其他属性连接、布局/绘制订阅。
- 源 Scope ending 时所有连接 Suspend；新 Scope 激活时仅恢复仍未释放且目标已附着的连接。
- Connection.Dispose 幂等，取消目标树事件、撤销自己的绑定，并从源的连接集合移除；之后 Attach/ScopeChanged/旧采样都不能重新激活它。
- State.Dispose 先使全部连接失效并释放，再取消自己的运行；不释放目标 Visual 或调用者的 Scope。
- 成功完成动画后，连接继续贡献终值，直到目标脱离、源暂停、连接或状态释放。显式目标未必等于目标属性的底层值，不能因为动画完成就自动暴露底层值。

连接有意持有目标 Visual，目标 Detach 不等于逻辑连接 Dispose。模板替换必须显式释放旧连接，避免长期状态继续持有旧模板部件；不引入弱引用来替代这项明确所有权。

连接安装和发布会触发应用属性处理器，必须处理同步重入：安装前记录物理 generation，返回 Bind 句柄后复查释放/附着/Scope 身份；如果期间失效，立即释放返回的句柄。移除旧贡献时先失效再 Dispose，避免属性回退回调误用旧连接。

### Scope 重建顺序

采用维护者提供的序列图，保持“先恢复贡献，再恢复运行”：

1. 旧 Scope 标记 ending 后，在取消运行之前保存 Value、Velocity、FrameTime 和当前运行配置。
2. 状态暂停输出；通知所有连接 Suspend，连接先禁用发布，再撤销旧物理贡献。
3. 旧 Scope 执行现有的订阅/绑定释放与运行取消；清理不能反向 Dispose 长期状态或逻辑连接。
4. ScopeChanged(newScope) 到达后，状态安装新的运行宿主，并依次激活仍有效的输出连接；新贡献先发布冻结的当前值。
5. 将恢复运行登记到同一批次的提交协调器。激活贡献触发的属性回调可能请求新目标，此时统一合并；避免先启动旧目标再立即接管。
6. 采样截止点按恢复快照及最新请求启动一次有效运行，加入可用批次；后续采样经连接发布。

恢复与提交仍服从第 5 节的批次截止点；新连接不能擅自提前采样。目标仍未附着时保持 Suspend，源运行可以为其他消费者恢复。

撤销旧贡献会同步显现底层值，重新安装会再次引发属性通知。上述顺序不承诺属性通知的原子替换；同批次恢复也不单独证明所有平台的显示无闪烁，须按真实调度验证。

### 模板替换用法

_opacity 由稳定控件 owner 持有，在构造时创建一次；模板只管理返回的连接：

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

模板替换保留 _opacity 的对象身份、Value 和 TargetValue，不因换输出部件主动重启动画。若同时触发 owner Scope 重建，则按前述流程恢复运行，而不承诺保留旧 Task 身份。新部件连接时取得当前值，而不是重新从 initialValue 开始。独立于模板的状态创建、目标更新与 Dispose 规则保持不变。

DirectProperty 不具备此贡献机制，应显式保留不支持的边界，不以普通赋值降级。对已有普通属性赋值动画的清理属于后续消费者迁移，不在当前设计任务中修改组件。Animated.Properties 的目标自动追踪/XAML API 重设计仍留待以后，长期采样连接不以其重设计为前提。

在布局场景中订阅一次 InvalidateMeasure 即可；绘制场景用 InvalidateVisual。具体失效范围由组件选择，不新增通用失效策略枚举。

## 9. 释放与失败

### 释放顺序

Dispose 幂等，UI 线程执行：

1. 标记永久释放并使当前运行、连接回调失效。
2. 移除待提交记录和连接事件；关闭向外发布的通道。
3. 撤销自己的 Scope 资源，取消自己的 Animatable 运行。
4. 清除消费者委托和连接引用。

Dispose 不阻塞等待异步清理，不泵送 Dispatcher，不提供 DisposeAsync。返回后不再有状态通知；Runtime 的清理仍由现有 Scope 的受管运行跟踪完成，清理异常仍可进入该 Scope 的诊断。

### 错误边界

- 错误线程、已释放状态、构造参数错误同步抛错。
- converter/spec 求值错误在提交或采样时处理：停止该状态的运行，保留最后可用的 Value，IsRunning 为 false，通过现有 AnimationFailed 报告原始错误和 Label。
- 非有限动画目标或采样不能进入布局消费；在转换/采样阶段验证向量，失败按上述路径处理，不静默 Snap 到 NaN/Infinity。
- 不因每次 Measure 重复同一个失败请求而自动重试。新的目标、新 spec 引用或新有效连接可以重新尝试；修复了可变 spec 时应提交新的 spec 对象。
- Value/PropertyChanged 通知抛错沿上述失败路径停止当前运行，不把异常吞成成功；其他状态和根帧源不因此取消。
- Connection 的物理绑定安装/发布失败由该连接处理：禁用并撤销自己的贡献，通过源的当前有效 Scope 报告错误；不停止共享的源动画或其他输出连接。内部连接回调不把此类错误继续抛入状态的公共通知链。同一物理连接失败不逐帧重试，显式重建连接或有效生命周期重新激活时才重试。
- Finished 抛错时运行已经结束，报告错误，不重新运行或重发 Finished。
- 任务取消依据 Task 的取消状态与运行身份判断；Faulted 的 OperationCanceledException 仍是失败，不能统一按异常类型忽略。
- 帧源故障复用现有根环境诊断与停止请求规则，不用本地 timer 兜底。

失败后的 TargetValue 仍表示用户请求，可能不同于冻结的 Value。不新增 Result/Option 返回值或第二套错误事件。

## 10. 内部组织

AnimatedState<T> 保存初值、最新请求、输出状态、事件、单个 Animatable、运行宿主及属性连接集合，负责 Visual/显式 Scope、Scope ending 快照、断开和恢复。AnimatedStateConnection<T> 单独管理输出目标与每一代物理绑定；运行宿主连接与输出属性连接不能共用同一个 generation。

每根一个提交协调器，负责待提交集合、快照与共享唤醒流程。没有待提交状态时不产生帧需求。可以使用当前 FrameClock 的帧前钩子，无需先公开一个通用帧阶段 API。

这些是实际职责，不要求每项建立独立接口或多层转发。Visual/Scope 两种连接可以在一个内部实现中表达；没有真实第三种宿主之前不引入 provider 注册框架。

相同请求的重复 Measure 应是有界的局部比较；提交集合内每个状态最多一个位置。提交需要的快照和资源按实际规模组织，不在首版承诺未经测量的零分配。

不会修改当前 FrameClock 的跳转优先级、时间戳或既有动画宿主，也不把 Animated.Properties/AnimatedContentSize 迁移到此机制作为实现前提。

## 11. 与固定 Compose 基线的关系

依据根 docs/upstream-baseline.json：AndroidX f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea，Compose Multiplatform Core a9e47e8aa04c95cfe572ab4297c770f3cbfb234e。

| 行为 | 本设计 |
| --- | --- |
| 首次值采用初始目标 | 保留；显式 initialValue 由调用者提供 |
| 目标变化驱动动画 | 保留；由 UpdateTarget 表达 |
| 相同目标不因 spec 变化重启 | 保留 |
| 当前值/速度的中断接续 | 使用现有 Runtime，不独立重写 |
| 最新完成回调 | 事件在有效完成时读取当前订阅 |
| composition SideEffect 提交 | 适配为采样批次前的合并提交，不宣称等价于 composition commit |
| composition 身份和生命周期 | 适配为显式状态对象和宿主连接 |
| 默认/显式 Spring visibility threshold | 按解析规则区分，不能丢失存在性 |
| Label 动态改变/Converter 更换 | 首版固定构造配置，重新创建状态表达身份变化 |
| 完整动画曲线跨宿主迁移 | 首版不保证；明确恢复范围 |

首版提供的是核心目标动画能力和 Avalonia 接入扩展，不据此宣称覆盖 Compose 的所有 tooling 或 composition 行为。

## 12. 验证与实施边界

已取得的依据：

- 阅读固定 AndroidX AnimateAsState.kt 的初值、SideEffect、目标与 spec 更新、完成回调路径。
- 阅读固定 AndroidX AnimationModifier.kt 的 measure 目标更新和内容尺寸目标测量路径。
- 核对 Avalonia 12.1.2 NuGet repository commit 与本地 tag，阅读 Measure/InvalidateMeasure、LayoutManager、MediaContext 和 DispatcherPriority。
- 阅读当前 FrameClock、AvaloniaFrameSource、Animatable/MutatorMutex、Scope ending/rebuild 和 AnimatedContentSize。
- 2026-10-14 已运行现有 FrameClockTest.BeforeFrameWork_JoinsTheArrivingFrame 与 Hooks_RunInRegistrationOrder_AroundEachWaiter，2/2 通过。只证明基础帧机制，不证明尚未实现的 AnimatedState。

实施后需要验证：

| 场景 | 必须观察的结果 |
| --- | --- |
| 构造后未更新 | Value 等于初值，无帧请求、无 Finished |
| 空闲启动 | 提交与首次采样加入同一到达批次，没有额外提交等待帧 |
| 一次布局多次目标变化 | 只提交截止点最新组合，无 Measure 内通知 |
| A → B → A，B 未提交 | 原 A 运行继续，无不必要接管 |
| spec 改变、目标相同 | 当前轨迹不变；后续目标采用对应请求 spec |
| 每帧目标变化 | 连续中断按 Runtime 值、速度和时间基准执行 |
| 回调中更新目标 | 当前快照有界，新请求在下一批处理，且不丢失唤醒 |
| 回调中 Dispose | 后续通知/Finished/旧队列工作不能继续消费 |
| 临时换父、同根与跨根 | 连接归属正确；跨根不使用旧时间原点 |
| 真正 detach/reattach | 无离树帧需求；从冻结值、零速度向最新目标重新启动 |
| 显式 Scope 结束 | 自动释放状态，兄弟运行不受影响 |
| 多条属性连接 | 只有一条动画运行，全部使用 Animation 优先级，各自独立撤销 |
| 输出目标 detach/reattach | 只暂停该物理贡献，源动画继续；重新挂回立即取得当前值 |
| 模板替换 | 旧连接永久释放、新连接贡献现值，源状态身份保留；同时发生的 Scope 重建走恢复流程 |
| Scope 重建的属性连接 | 先快照/撤销旧贡献，后贡献冻结值，再按最新请求恢复运行 |
| 绑定安装/撤销中的重入 | 已释放或过期的物理句柄不能留存、发布或重新激活 |
| 单个属性连接失败 | 撤销失败连接，不停止共享状态与其他连接；保留原始诊断 |
| 初值来自测量 | 不因构造发布回调，不把离树探测结果隐式当作呈现初值 |
| 阈值解析 | 未指定尊重传入 spec；显式 override 仅覆盖 Spring 且保留存在性 |
| 转换、spec、通知、Finished 故障 | 诊断保留原异常，不误报取消，不重复布局重试 |
| 长期空闲和大量重复 Measure | 无持续帧、无每次调用 Task/CTS 分配 |

另需用真实 UI 验证滚动条引发的多次布局、窗口尺寸变化、Popup 和模板替换。手动帧/headless 验证与 native 调度、显示帧验证分别报告。

### 首版落地验证（2026-10-14）

- 新增 AnimatedStateTest 26 个测试结果，覆盖目标提交、布局内更新、Runtime 中断/阈值对照、连接底层值、同根/跨根恢复、detach、模板目标替换、重入、错误隔离及帧源取消。
- ComposeAnimation 全量测试 925/925 通过，文档公开 API 示例参与编译。
- Catalog state/target 的 Retarget、ReplaceTarget、ReconnectTarget 三个 native 窗口动作通过，11 个断言、5 张截图；截图确认共享宽度输出。
- 默认 Catalog Debug 输出被现有进程占用，验证构建使用独立输出目录。
- Runtime 只增加 FrameClock.IsUnavailable 内部只读查询，用于区分永久帧源失败与单批次钩子失败；既有求值、中断、时间戳和跳转优先级不变。
- AnimationSampleObservable 增加默认开启的订阅时发布选项；新连接先取得可释放句柄再首次发布，既有使用者保持默认行为。
- 尚未执行 Compose 双端 pixel parity、Linux/macOS native 验证或性能基准；恢复范围仍以本稿声明为准。

本次实现范围为 modules/ComposeAnimation 的库、测试、Catalog 和文档。M3E 消费者迁移、进一步 Runtime 语义调整及包发布属于后续工作。

## 源码入口

- [固定基线](../../../../docs/upstream-baseline.json)
- [AnimationScope 用法与 D17](../usage/animation-scope.md)
- [IAnimationValue](../../ComposeAnimation.Runtime/IAnimationValue.cs)
- [Animatable](../../ComposeAnimation.Runtime/Animatable.cs)
- [FrameClock](../../ComposeAnimation.Runtime/FrameClock.cs)
- [AnimationScope](../../ComposeAnimation.Avalonia/AnimationScope.cs)
- [AnimatedContentSize](../../ComposeAnimation.Avalonia/AnimatedContentSize.cs)
- [固定 AndroidX AnimateAsState.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/animation/animation-core/src/commonMain/kotlin/androidx/compose/animation/core/AnimateAsState.kt)
- [固定 AndroidX AnimationModifier.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/animation/animation/src/commonMain/kotlin/androidx/compose/animation/AnimationModifier.kt)
- [Avalonia 12.1.2 Layoutable](https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Base/Layout/Layoutable.cs)
- [Avalonia 12.1.2 LayoutManager](https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Base/Layout/LayoutManager.cs)
