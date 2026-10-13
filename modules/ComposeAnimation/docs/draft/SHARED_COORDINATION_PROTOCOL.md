> **历史设计稿**：本文早于 2026-10 的动画基础重构，仅作历史参考，名称与行为可能已过时。现行用法见 [`docs/usage`](../usage/README.md)；迁移见 [迁移指南](../usage/migration-from-visual-animation-scope.md)。主要改名：`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`SharedAnimationFrameClock` → 内部 `FrameClock`，`IAnimationFrameClock` → `IFrameSource`，`AnimationScope<T>` → `AnimationRun<T>`。

# SharedTransition 协调协议：准备与运行期 Session

已落地共同父 Transition 下的准备协调、显式更新事务，以及 Session 动画所有权与端点完成参与。`SharedTransitionScope`、
公开的 `SharedTransitionHost` / `SharedElement` / `SharedBounds` 和 VisualBrush 后端均位于正式 Avalonia 库；
仅创建 Scope 仍不会产生共享动画。

## 所有权

- 调用方拥有父 Transition 及其帧循环。SharedTransitionScope 不启动、停止、seek 或销毁父 Transition。
- SharedTransitionScope 管理参与者身份、准备请求、generation、准备 lease 和取消信号。
- AnimatedVisibilityScope 继续拥有内容实例。内部注册只接受共同父 Transition 的后代可见性 Transition。
- Session 通过共同父级下独立的子 Transition 持有共享动画，两端分别安装完成参与节点。准备 lease 只覆盖准备阶段。

## 入口

```csharp
using var sharedScope = SharedTransitionScope.Create(pageTransition);

// 普通路径：已有参与者注册时自动建立准备请求。
pageTransition.UpdateTarget(Page.Details);

// 需要批量更新业务数据时使用显式事务。
using (sharedScope.BeginUpdate())
{
    selectedItem = item;
    pageTransition.UpdateTarget(Page.Details);
}
```

参与者注册及准备请求的消费接口保持 internal，由 Host / SharedElement / SharedBounds 和协调器使用。
以上代码只展示底层状态入口；公开 UI 用法见 [使用文档](SHARED_TRANSITION.md)。

Runtime 新增内部同步 TargetChanging / TargetChanged 通知，包围目标修改和后代动画重配置。
准备 lease 在前通知中预约，并在 Runtime 发布普通属性通知之前完成注册。
后通知将准备请求排入 Runtime 的通知队列，消费者看到的后代目标已更新。
这同时覆盖 `UpdateTarget` 和关联 MutableTransitionState 的直接 TargetState 更新，不依赖事后观察 PropertyChanged 来抢保留时机。

## 显式事务

- BeginUpdate 记录已有参与者身份及其切换前 CurrentState / TargetState，并为父级和已注册可见性分支取得准备保留。
- 事务内父 Transition 的状态修改仍正常发生；事务合并的是共享准备请求，不是业务状态回滚或通用属性通知事务。
- 嵌套事务按逆序 Dispose；最外层提交后发布最终准备请求。错误的释放顺序抛出异常，句柄仍可按正确顺序释放。
- 任意一层 Cancel 会取消整批共享协调，撤销其他句柄；已修改的业务状态保持原值。重复 Dispose / Cancel 是无操作。
- 在父 Transition 的通知回调内使用事务时，准备、目标写入、提交/取消按 Runtime 队列顺序处理；事务取消不会让排队中的旧目标写入重新触发自动准备。
- 不跨 await 持有事务。布局等待发生在事务提交后的准备阶段；事务本身不暂停旧动画。
- 没有参与者时提交立即释放准备，不产生异步等待。

`using` 遇到异常也会 Dispose，因此默认仍表示提交。需要取消共享协调时应显式调用 Cancel：

```csharp
using var update = sharedScope.BeginUpdate();
try
{
    UpdatePageData();
    pageTransition.UpdateTarget(Page.Details);
}
catch
{
    update.Cancel();
    throw;
}
```

## 请求身份和迟到参与者

每个新意图有递增 generation；每次参与者注册有独立 Id，即使 Key 相同也不复用身份。
准备时保存状态快照，并调用注册时提供的布局读取函数，保存切换前的源矩形。读取函数须为同线程、无副作用的读取，
返回同一宿主坐标空间中的最终布局边界；尚未布局、已离树或无法提供有效边界时返回 null。零尺寸、非有限值均视为无效。
源读取异常记录为准备错误，该 Key 不会在状态切换后重新猜测源位置。视觉内容捕获仍由后端负责。

目标模板迟到注册时，以同一个 generation 重新发布候选集合，并保留原切换前快照。
同 generation 的请求因此可能发布多次，消费者须按 generation 管理幂等准备，不能每次通知都创建新动画。
任何参与者注销都会保守取消当前准备；按 Key 选择替代端点的策略尚未实现。

新意图先取得替代 lease，再释放旧 lease。旧请求 CancellationToken 被取消，迟到回调通过 generation 校验被拒绝。
Scope / 父 Transition / 参与端销毁、进入 manual seeking，都会清理准备。这里只取消准备，不提供交互式 seeking 支持。

## 准备提交和失败

底层 `TryCommitPreparation(generation, installSession)` 只接受当前有效请求，先调用安装函数，再释放准备。
运行期协调使用其上的 `TryCommitSessions(generation, install)`，在一个同步 batch 内安装这一意图的所有配对。
batch.Start 建立 Session、动画与两端完成参与；batch.Retarget 更新同一个 Session，保留其动画宿主和端点身份。
每个注册在同一批内最多参与一对；同一个可见性分支可通过不同注册参与多个 Session。

全部配对安装完毕后才释放准备，随后激活完成通知。任何一对安装失败，整批已创建或重定向的 Session 都取消，交由后端回退；
不尝试恢复已经修改的动画轨迹或业务导航。重入产生更新的准备请求时，旧批次不能释放新请求的 lease。
batch 仅能在安装回调内使用，提交后不能继续往其中追加配对。

成功提交时，上一意图中未被 Retarget 接纳的 Session 以 Superseded 结束；空批次表示没有配对，退出旧共享表现。
取消新准备同样会取消尚未接纳到新 generation 的旧 Session。等待新布局期间旧动画仍可继续采样，但旧 generation 不能完成绘制交还。
协调层负责撤销动画和端点参与，具体绘制资源的回退仍由 Session 的结束回调负责。

等待目标布局的超时由 Host 安排，超时调用按 generation 校验的取消入口；
`PreparationTimeout` 提供默认值，也可设为负值禁用。
准备 token 在提交、取消、替代或销毁后结束，不能继续使用旧请求。

准备消费者或 token 取消回调异常记录到 LastPreparationError，失败请求被清理，不使父 Transition 的通知队列中断并遗留 lease。
新准备会清除之前的错误记录。库不改变 Visible 或页面导航状态。

## Session 时间轴与反向

```text
共同父 Transition（调用方的帧循环）
├─ 源端可见性分支：普通进入/退出动画 + Session 完成参与
├─ 目标可见性分支：普通进入/退出动画 + Session 完成参与
└─ Session 子 Transition：bounds、内容 fade 等真实动画
```

Session 使用固定状态映射的子 Transition，因此父状态 A → B → A 不会自动重配置其中的动画。
布局协调器通过已有动画的 Update 显式更新目标，沿用 Runtime 的采样值与速度接续机制。
反向只交换 Source / Target 角色并接纳新的 generation，不迁移动画宿主，不替换动画实例。
当前 Retarget 仅接受同一组注册身份；第三个端点或替代注册需要另建 Session，几何与速度如何转交由后续匹配器处理。

端点完成参与节点不采样动画，只阻止各自可见性分支完成，Duration 镜像 Session 的真实 TotalDuration。
因此父级总时长仍取各动画的最大值，不叠加两端时长，不设置人为的超长动画。
同一 Session 中 bounds 结束而 fade 未结束时，Session 继续运行；普通可见性动画不阻止共享绘制提前交还，但仍独立决定自己的退出完成。

## 结束与绘制交还

状态流为 `Running → AwaitingHandoff → Completed`，运行或等待中均可转为 `Cancelled`。

1. 所有 Session 动画及其子动画完成后，进入 AwaitingHandoff，两端完成参与仍保持未完成。
2. 当前 generation 且没有待提交事务或准备时，发出内部 HandoffReady 通知。通知回调内排队的动画更新须经过 Runtime mutation 队列后再检查完成。
3. 后端选择合适的绘制提交时机，调用 `session.TryComplete(generation)`。过期 generation、未完成动画或新准备都会拒绝交还。
4. TryComplete 同步调用安装 Session 时提供的 ending 回调。此时两端参与仍未释放，后端可提交末帧、切换原位置绘制并清理 overlay。
5. 回调返回后才释放两端参与、销毁 Session 子 Transition；可见性分支在后续帧评估是否结束。

DurationScale=0 只使真实动画在下一帧取终值，仍需完成上述交还。没有真实动画的 Session 可以直接进入等待交还，但通知一定发生在准备提交之后。
HandoffReady 不会自动释放内容，也不代表实际像素已显示。本层只验证生命周期顺序；
Avalonia 后端在布局完成后校验 capture 几何并执行交还，但这仍不是 GPU 上屏栅栏。

注销端点、销毁可见性分支 / Scope / 父级、进入 manual seeking，都会取消相应 Session 并清理参与。
手动 seeking 尚不支持共享内容预览。Session 结束回调也接收取消原因，以便执行对应的恢复策略。
HandoffReady 或 ending 回调异常记录在 LastSessionError，并确保动画与保留得到清理；后端需自行保证资源清理的 finally 路径。

## 验证及下一步

准备阶段新增 17 项验证：自动入口、直接 state 写入、嵌套合并、取消、通知回调内的事务顺序、
反向意图、迟到注册、过期提交、安装失败、异常清理、父循环所有权、无参与者、跨父拒绝、seek/dispose，
以及真实 AnimatedVisibility 的 Exit=None / DurationScale=0 保留与卸载。

运行期新增 24 项验证，全库 331 项通过（2026-09-09）：Session 与动画实例在反向时稳定、采样值与速度连续、多个配对一起提交、
最长内容动画、普通可见性效果独立完成、零时长与空 Session、过期交还、未匹配回退、整批安装失败、回调异常、通知内重定向、
嵌套父级销毁，以及真实 AnimatedVisibility 两端内容在交还前保留、交还后仅卸载退出端。

自动配对与布局阶段新增 24 项验证，全库 355 项通过（2026-09-09）。覆盖切换前源快照、迟到模板、无效尺寸、重复 Key、
稳定端点、部分配对超时、旧超时回调、反向与布局速度接续、布局 revision、失败清理和真实同窗口 AnimatedVisibility 布局。

## 自动匹配与布局消费

`SharedTransitionCoordinator` 必须在第一次准备前连接到 Scope，每个 Scope 只能连接一个协调器。
其注册入口将布局读取器与参与者身份绑定；参与者退出时解除引用，协调器销毁时清理自身注册和 Session，父 Transition 仍由调用方拥有。

匹配器按 Key 分组，不按控件顺序或注册顺序选择端点：

- 出端：切换前目标是 Visible，切换后不再是 Visible；运行中 Session 的旧端点也可成为反向的出端。
- 入端：当前目标是 Visible。恰好一出一入才构成配对；多出或多入诊断为 AmbiguousKey 并回退该 Key。
- 稳定可见、未参与本次切换的注册不会单独生成动画；缺少源或源快照无效时直接回退。
- 缺少目标或目标尚未有效布局时保持准备，等待宿主下一次 LayoutCompleted。迟到模板重新发布同 generation 后也必须重新经过布局完成。
- 同一组身份的反向复用已有 motion / Session / presentation，以当前采样矩形作为源，并保留速度。替代身份或第三端点目前另建 Session，尚无三端连续接力策略。

调用 LayoutCompleted 才尝试消费准备请求，保证不在模板刚注册时就读取未完成布局。
入端已注册但尚未有效布局时整批暂不安装。已经有可用配对时，缺少目标的 Key 立即回退，不挡住其余配对（列表到详情中未选中的项即如此）。没有任何配对且仍可能迟到时保持准备。
宿主的准备截止回调调用 ExpirePreparation(generation)，可以提交已准备好的 Key 并回退剩余 Key。
若截止前未完成过布局，则整批取消；过期 generation 的截止回调无作用。协调器本身不创建计时器、帧循环或强制布局。Host 提供可配置的准备超时。

运动开始后，每次 LayoutCompleted 重新读取目标最终布局。不同矩形通过原 Bounds 动画 Update 接续，相同矩形不重启动画。
目标变成无效布局时取消共享表现，避免交还至过时坐标。绘制矩形不得反馈进布局读取器。

`SharedTransitionHandoff` 同时携带 generation 和 LayoutRevision。布局改变会递增 revision 并撤回待交还状态；
即使还是同一页面意图、后来的动画也已完成，旧 revision 也不能通过 motion.TryComplete。
绘制后端负责在正确的布局/渲染阶段使用最新凭据，且不能绕过 motion 直接按 generation 结束 Session。

内部后端接口分工：Create 建立内容捕获、绘制抑制及内容动画；Retarget 更新端点角色/布局；HandoffReady 安排绘制提交；End 恢复绘制并释放资源。
SharedBounds 的实验性 RemeasureToBounds 已接入此路径：目标读取器消费端点所在 Context 的完整快照；没有外部 Context 时端点自动建立局部入口。Sample 将现有 motion.Rect 作为两个端点实际布局的 fixed 请求，父级仍得到各端点自己的目标尺寸。模板中的真实 suppress/capture/presenter 层传递完整约束与当前原点。VisualBrush 仅搬运已重排的内容，重测端使用 Stretch.None。

配对不要求两个端点属于同一 lookahead 域。每个域从自己的 `CoordinateReference` 转换到同一 SharedHost 坐标系，快照、版本及失效仍各自独立；这不会把两个域合并成共同的 target pass。局部入口只在原生父链完成 Arrange 后发布完整目标矩形，因此协调器不会读取仅完成 measure 的候选。

Host 在协调器 LayoutCompleted 检查目标变化之后调用后端布局完成处理，刷新已经提交布局的 brush；Remeasure 的 HandoffReady 保留凭据，等最终实际几何就绪后才 TryComplete。除 motion 的 generation/LayoutRevision 外，端点还校验当前 Session generation、Context 身份和目标 Snapshot.Version，并确认 capture 已实际位于最终矩形。目标重建、域迁移或 session 替换都会使旧凭据失效。普通运行帧可在目标暂时 dirty 时沿用上一个完整快照显示，不能据此完成 handoff。仅布局重定向不反转 fade，只有端点角色交换才反转。取消和错误通过 End 清除两个端点的 motion 所有权，不会清除后来 motion 的样本。
Create 内的 Runtime 动画初值更新可能仍排队，首帧绘制应读取 Pair.SourceBounds，而非假设 Bounds.Value 已应用强制初值。
Create 抛异常时负责清理自身部分资源；返回后 Session 已被重入取消的 presentation 也会收到一次 End。

正式 `SharedTransitionHost`、`SharedElement` / `SharedBounds`、默认布局读取器、准备超时与 VisualBrush 后端已接入。见 [使用文档](SHARED_TRANSITION.md)。
真实控件测试使用 Enter/Exit=None，并由测试读取同一 Canvas 中的最终布局；它不证明一般情况下 TransformToVisual 能剥离父级 scale、slide、expand 等动画效果。
这些坐标转换、嵌套裁剪和绘制提交边界由宿主后端处理。SharedBounds 重测最初使用有限协作 Root；当前实现已扩展为自动局部域或显式域、ContentSize 风格占位及布局完成交接。它仍不是 GPU 提交栅栏、完整 GraphicsLayer 或所有 SharedTransition 模式的等价实现。
