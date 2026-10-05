# 局部 Lookahead 落地计划

状态：A–F 已实现，公开 API 仍为实验性。日期：2026-09-22。最终实现选择及验证证据见 [实施状态](LOCAL_LOOKAHEAD_IMPLEMENTATION_STATUS.md)。

对应 [API 设计稿](LOCAL_LOOKAHEAD_DESIGN.md)。本计划采用后续确认的方案：可继承的上下文附加属性传播域身份；Boundary 截断外域，内部布局入口自动建立独立域；不要求 IsDomainRoot 或重新开启开关。

本文保留实施前确定的完整目标、顺序与验收门槛，供后续维护追溯。当前使用方式以 [AnimatedBounds](ANIMATED_BOUNDS.md) 和 [布局协议](LOOKAHEAD_PROTOCOL.md) 为准。最终单策略接口名为 ILookaheadLayoutPolicy，旧双策略接口作为兼容路径保留。

## 1. 先保留什么，再改变什么

建议保留 Runtime、BoundsAnimationState、动画规格、BoundsLayout 的数学与组合规则、SharedTransition 的 preparation/session/handoff 协议，先改造布局上下文的承载和入口。

顺序是：抽离 Root 的协调职责 → 接入附加属性 → 做正确的 Boundary 继承隔离 → 加入无 Root 自动入口 → 接入独立内域 → 迁移 SharedBounds → 最后整理公开扩展协议。

不要在第一步同时改协议签名、重写容器、修改动画状态机和替换 Shared 后端。设计稿中的 Context/Result/Placeable 公共 API 是后期收敛方向，第一阶段仍可使用当前独立目标 builder 和完整约束桥。

## 2. 当前耦合与对应改造

| 现有文件/职责 | 实施方向 |
| --- | --- |
| LookaheadRoot：快照、订阅、版本、失效、移动事务、重入保护 | 将这些状态移入内部 LookaheadContext；Root 先保留为旧入口适配器 |
| LookaheadScope：向上找 Root，遇 Boundary 停止 | 改为读取继承上下文，并验证参与边和所有权 |
| LookaheadLayoutBuilder：持有 Root，完整 Measure/Place | 改持有 Context；支持分开的候选测量与完整排列提交 |
| LookaheadMeasure.State.Root | 改为域身份及成员代次，保留完整约束缓存键 |
| LookaheadPlacement.Frame.Root | 改为域身份；增加自动入口帧及域隔离的查找边界 |
| LookaheadCoordinates：Root 相对坐标 | 使用 Context 的坐标参考与本轮放置帧 |
| AnimatedBounds：FindRoot、父级 placement host、旧 Bounds 语义 | 使用成员状态；增加局部入口的布局槽位/显示矩形分离 |
| LookaheadBoundary：直接 Decorator.Child、嵌套 Root 禁令 | 引入内容继承出口和终点承载层，移除全面禁止内域的规则 |
| LookaheadMove、AnimatedBounds.MoveState | 用 Context 身份验证移动，不把附加属性继承等同于已提交迁移 |
| AnimatedContentSize | 保留普通路径，仅在显式协议测量链上加入父域 |
| SharedBoundsLookahead / SharedLayoutBounds | 用局部目标快照和参考坐标转换替代 Root 控件要求 |
| StackPanel/Box/TextBlock/ScrollViewport | 先适配 Context，不同时重写布局算法 |

实际源码入口见文末链接。

## 3. 内部对象怎样拆分

以下名称是建议的内部实现名称，不构成新的应用层 API 承诺。

### 3.1 稳定的域上下文

```csharp
internal sealed class LookaheadContext
{
    // 以下仅为职责草图，省略构造器及实现。
    internal Control Owner { get; }
    internal Visual CoordinateReference { get; }
    internal long MembershipEpoch { get; }
    internal long TargetInputRevision { get; }
    internal LookaheadLayoutSnapshot? CommittedSnapshot { get; }

    internal void InvalidateTarget();
    internal void InvalidateApproachMeasure(Control node);
    internal void InvalidateApproachArrange(Control node);
}
```

Context 保存域身份、参与者/视觉桥、输入订阅、目标候选、提交结果和生命周期。它不保存每一个节点的 Animatable，也不在每帧更换实例。旧 Root 和新局部入口可以使用不同的入口适配器，共用同一个协调核心。

### 3.2 可继承的附加属性

```csharp
internal static class LookaheadEnvironment
{
    internal static readonly AttachedProperty<LookaheadContext?> ContextProperty =
        AvaloniaProperty.RegisterAttached<Control, LookaheadContext?>(
            "Context", typeof(LookaheadEnvironment),
            defaultValue: null, inherits: true);
}
```

初期由库内部维护该属性，不允许应用直接注入或替换 Context。需要诊断时通过 Lookahead.GetSnapshot 或只读查询提供信息。

本地写入只发生在两个地方：域入口发布自己的 Context；Boundary 的内容出口设置本地 null。普通成员直接继承，不能给每个成员都写入同一个本地值，否则后续继承切换会失效。

必须区分：

- `SetValue(ContextProperty, null)`：建立明确的继承截断。
- `ClearValue(ContextProperty)`：去掉本地覆盖，重新继承上游值。

域入口自己写入的本地 Context 会遮住未来父级变化。离树、重新挂载和参与边变动时，必须由所有权记录释放旧上下文及其本地贡献，然后重新判定归属；不能只等入口自己的有效属性值发生变化。清理只撤销当前记录持有的贡献，防止旧回调清掉新 Context。

Context 的内部字段变化不通过附加属性传播。动画采样和 Target/Approach 阶段切换不能每帧 SetValue，从而触发整棵继承树的属性通知。

### 3.3 参与者状态和调用阶段

每个受管节点保存 LookaheadNodeState：当前 Context、成员代次、协议父边、完整约束请求、实际测量缓存以及入口所有权。附加属性只是可继承的候选环境，注册表才决定节点是否确实处于该目标树。

保留同步调用栈，Frame 至少包含 Context、Phase、Owner 和本轮原点。它用来携带 Target/Approach/原生边界调用，不能替代长期成员状态。

同域非法重入需要拒绝；不同域通过 Boundary 进行正常嵌套测量必须允许。例如外域目标测量调用 Boundary 的原生 Child.Measure，内部入口可以执行另一个域的目标测量，不能被一个全局 `isBuilding` 阻止。

## 4. 继承上下文怎样确定实际归属

只读到非空 Context 不够。普通 Grid、模板包装层以及尚未注册的节点也可能继承同一属性值，但没有独立目标测量能力。

接入时依次判断：

1. 当前是否有针对本节点的有效协议调用/已注册父边？若有，使用该边提供的 Context。
2. 若只有继承值，验证其拥有者仍有效，以及是否存在已声明的协议父边或库内模板桥；通过验证才能加入。
3. 没有可加入的父域且本节点支持自动入口，则建立自己的 Context，本地发布给其子树。
4. 仅具备参与能力、没有自动入口职责的节点，继续原生路径，等待真正的父域协议调用。
5. 父级正在构建目标树却遇到不支持的普通子项，仍须使用 Boundary/受支持叶子；不能偷偷穿过这个普通子项寻找更深层的协议节点。

节点被原生 LayoutManager 单独重测时也要复用已验证的成员状态，不能因没有当前同步 Frame 就把自己变成新域。

### 4.1 不要把接口能力等同于自动启用

源码中 AnimatedContentSize 已实现 ILookaheadLayoutNode，但普通路径支持装饰、数据模板和自定义模板；其目标路径仍有严格限制。SharedBounds 的默认 ScaleToBounds 同样不应突然进入受限重测路径。

建议的入口策略：

| 控件 | 没有父域时 |
| --- | --- |
| AnimatedBounds | 自动创建局部域 |
| LookaheadStackPanel/Box/ScrollViewport | 自动创建局部域 |
| 适配后的 LookaheadSize/Position | 自动入口；保留各自原有语义差异 |
| LookaheadTextBlock 等测量叶子 | 无动画消费者时可直接原生布局，加入协议父域后提供独立目标测量 |
| LookaheadBoundary | 保持普通单子项布局并截断内容环境；自身不必单独建立一个没有消费者的目标树 |
| AnimatedContentSize | 保留普通路径，仅在有效父域中参与 |
| SharedBounds.ScaleToBounds | 保留普通缩放路径 |
| SharedBounds.RemeasureToBounds | 自动创建或加入局部域 |
| 兼容 LookaheadRoot | 显式创建上下文，继续保留旧有限视口规则 |

这项差异由入口桥/辅助基类的策略表达，不新增用户必须配置的 IsDomainRoot，也不依赖一串不可扩展的类型判断。初期可为内部 adapter 配置入口能力；最终自定义布局基类默认支持自动入口，手工接入者通过桥明确声明能力。

## 5. Boundary 的具体结构

需要同时满足：Boundary 本身属于外域；原生内容继承不到外域；内容的内部槽位不随外域采样改变。

建议逻辑结构为：

```text
Boundary                         外域成员，继承 Context A
  BoundaryContentHost            本地 Context = null，处理终点尺寸/外框对齐
    用户 Child                   普通继承路径
      ...
      内部自动入口               发布自己的 Context B
```

BoundaryContentHost 可以同时承担上下文出口与固定终点尺寸的承载职责；不必为了命名拆成多层控件。它不是另一种 public Scope。

### 5.1 不能只改视觉树

Avalonia 的属性继承沿 InheritanceParent。当前 Decorator 的 ChildChanged 会直接把用户 Child 放入自身逻辑树和视觉树，单纯新增视觉 host 不会自然改变内容的属性继承来源。

推荐先做一个很小的继承结构原型，采用 ContentPresenter 已有的模式：内容保留预期的逻辑归属，视觉上由私有 host 承载，继承父级指向这个 host；卸载或替换时恢复到正确的当前逻辑父级。

ContentPresenter 已通过公开的 ISetInheritanceParent 完成这种工作，因此不需要新增 UnsafeAccessor。这个机制会影响所有继承属性，应让 host 正常继承 Boundary 的 DataContext、字体、FlowDirection 等，只在 host 上本地截断 Lookahead.Context。

不要为保持 `LookaheadBoundary : Decorator` 而在属性变更回调中反复搬动 base 已接管的 Child。首选评估改为 Control 自管 Child/Padding 和私有 host，保留 XAML 用法；`Decorator` 的继承关系、父类型样式匹配等变化必须作为实验 API 迁移项记录。若原型验证可直接复用 ContentPresenter 的承载模式，也应统一子项连接/拆除的所有权，避免两套逻辑树管理互相干扰。

必须验证 Child 替换、模板迟到创建、DataContext 更新、资源查找、继承字体/FlowDirection、视觉重挂及名字作用域。公开框架 API 可用不代表这些组合已经验证通过。

### 5.2 终点布局与域隔离是两项职责

host 保存终点测量约束、终点内容槽位、原生输入版本。外域的中间框只改变 host 的整体偏移及外层裁剪，Child 的局部布局槽位保持不变。

Context=null 只负责切断外域身份；它本身不会冻结任何布局。内部域 B 仍可按自己的采样布局。

保证应写成“外域采样不主动给原生子树传入中间约束或排列槽位”。内容更新、内域动画及原生 EffectiveViewport 等正常机制仍可能触发内部布局，不应通过吞掉这些失效来伪造零布局计数。

## 6. 去掉 Root 后的目标提交顺序

这是自动入口中最需要先做原型验证的部分。当前 Root 用已知有限视口完成 Measure+Place；新的入口在 Measure 时只知道可用约束，最终位置和拉伸后的尺寸要到 Arrange 才确定。

建议显式分开两类数据：

- TargetMeasureCandidate：目标测量结果、约束指纹、输入版本和节点记录，还没有可公开使用的完整 Rect。
- CommittedTarget：完成目标 placement 并验证输入版本后的不可变快照。

调用顺序：

```text
原生父级 Measure(available)
  -> 入口生成/复用目标测量候选
  -> 向普通父级返回目标 DesiredSize

原生父级 Arrange(slot)
  -> 按 Margin/Alignment/尺寸限制/舍入求入口目标 Bounds
  -> 完成目标 placement
  -> 原子提交完整快照
  -> 更新 Rect 动画目标
  -> 测量并排列内部显示承载层
```

不在 Measure 中把新尺寸和上一轮位置拼成 Rect，也不以 Dispatcher.Post 猜测父级稍后将分配的位置。

### 6.1 原生布局管理器可能直接重测后代

Avalonia LayoutManager 按祖先优先顺序处理 Measure，再执行 Arrange；祖先已完成 Measure 不等于已完成新目标 placement。因此不能让所有后代 Measure 都假定 CommittedTarget 已更新。

建议内部增加“目标准备中”的测量路径：必要的后代原生重测可以消费候选测量记录/安全的当前约束，但不得启动新的 Rect 动画、发布混合目标或改变公开动画采样。入口在 Arrange 完成提交后，必须按已提交代次及当前样本重新确认内部实际测量，再排列显示层。

这条路径需要在第一版自动入口原型中验证：确保没有未完成目标的中间状态进入渲染；若测量缓存来自准备阶段，进入正式实际阶段时，即使 MaxSize 相同也应按阶段/代次判定是否需要失效。不能通过布局中递归调用入口自身 Measure 或 UpdateLayout 强行解决。

初期保留最后一次提交结果供正在执行的实际 pass 使用，公开查询在目标失效时仍可返回 null。准备候选不冒充公开快照。

### 6.2 原生 Bounds 与显示矩形

入口向普通父级报告终点尺寸，宿主 Bounds 保留原生槽位。内部私有显示承载层使用当前动画 Rect；由它补偿到当前原点、安排内容及承担命中测试位置。

AnimatedBounds 的内外 BoundsAnimationState 继续使用；改动主要在矩形如何映射到承载层，而非重新实现 spring、retarget 或中间布局策略。不能写入用户 RenderTransform，也不能只让图像移动而把输入位置留在目标。

纯位置帧只排列显示层，尺寸帧才按需要重测；普通父级不应因这个区域的中间 DesiredSize 变化而持续重测。

## 7. 嵌套域的失效不能互相污染

当前 LookaheadBoundary.IsNativeContentStale 将 Child.IsMeasureValid=false 视为需要重新预测。若 Child 是内域入口，这会把内域每次实际动画失效变成外域目标失效。

改造时需要区分：

| 事件 | 应影响哪里 |
| --- | --- |
| 外域采样变化 | 外域实际显示/裁剪，不改变内域终点约束 |
| 内域采样变化 | 内域实际布局，不改变外域目标 |
| 外域目标约束改变 | 内域收到新的原生输入，按需要更新目标 |
| 内域目标期望尺寸改变 | 正常向外传播 DesiredSize，必要时更新外域目标 |
| 内域内部位置变化但对外目标尺寸不变 | 不必重建外域目标树 |

已知内域入口用 TargetMeasureRevision 和对外 TargetDesiredSize 判定边界目标依赖，不仅看实际 IsMeasureValid。未知普通子树失效时仍允许保守地在终点约束下复核原生测量；若外域可见的目标几何和边界输入都没有变化，可以复用外域快照。

上下文属性的继承通知不直接当作几何输入；由成员迁移逻辑决定哪些域需要失效。内部 Approach 导致的缓存失效要带有来源记录，防止经原生通知又被误分类成 TargetChanged。

## 8. 坐标、缓存、移动与资源释放

- 自动入口默认参考其直接原生视觉父级；旧 Root 入口仍以 Root 为参考。Context 保存参考身份，坐标工具不再假定存在一个 Root 控件。
- 当前 Arrange 的原点必须由调用 Frame 提供。Avalonia 在 ArrangeOverride 返回后才写父 Bounds，继续使用旧 Bounds 会产生嵌套补偿误差。
- Frame 查找应受 Context 身份和已声明的视觉桥限制；进入 Boundary 原生内容时停止向外域 frame 回溯，内域建立自己的 frame。不能因 Context B 在 Context A 的调用栈里而复用 A 的原点。
- 完整约束缓存继续包含 min/max，并加入 Context 身份、成员代次和测量阶段；仅最大 Size 相同不代表缓存可复用。
- 重挂及重新加入父域时释放旧订阅、实际约束模式和动画等待；Context.Dispose 不得因为外域结束就误销毁仍附着的内域，正常树 detach 才会分别清理两边。
- MoveContent 验证的是同一已确认的 Context/参考。跨 Boundary 进入独立域属于域切换，不能借继承值短暂相同保留旧时间线。
- 生命周期重入需要代次保护；旧异步动画回调不能清理新成员状态或新本地 Context。

## 9. SharedTransition 的接入顺序

先让 SharedBoundsLookahead 读取 Context 及提交快照，保持现有目标占位、真实重测和最终布局交接逻辑；再允许 RemeasureToBounds 没有 Root 时自动创建入口。

Shared 后端至少区分三种几何：终点布局矩形、当前飞行矩形、真实 capture 的布局结果。局部入口改用内部显示层后，不能继续把宿主 Bounds 当作当前显示状态。

各域的目标通过各自 CoordinateReference 转换到同一 SharedTransitionHost。不同域可以被 Host 配对，但不因此合并快照或坐标参考。保留现有平移/变换限制，避免把去掉 Root 误扩展为任意矩阵下的 fixed-constraint 重测。

PendingHandoff 继续等待 capture 真正到达最终 Rect；检查 context identity/target revision/session generation，避免目标重建或域迁移后完成旧交接。

## 10. 分阶段交付与验证门槛

### A. Context 抽离，旧 Root 行为不变

改 Root、Scope、Builder、Measure/Placement、Coordinates、Move 的内部依赖，将 AnimatedBounds 和 SharedBounds 的内部 Root 引用替换为 Context。

旧 Root 继续控制有限视口、占位及起始测量；不在这一步改公开属性、默认尺寸输出和 Boundary 嵌套行为。

验收：现有相关测试行为不变；域引用和订阅没有额外残留。修改前先重新记录测试基线，不能把先前会话中的测试通过结果当成本阶段的验证结果。

### B. 属性继承与 Boundary 内容出口

加入 ContextProperty，Root 只在入口本地发布一次；实现 Boundary 私有 host 的本地 null 及正确继承父级。先以 Boundary 内嵌旧 Root 验证两个独立 Context，暂不同时引入自动创建。

验收：外域仍包含 Boundary、不包含内域节点；内域具有独立快照；同一普通内容的 DataContext、字体、FlowDirection、资源和替换/重挂行为保持正确。验证“本地 null 阻断 / ClearValue 恢复”及恢复继承父级的生命周期。

### C. 最小无 Root 自动入口

先支持 Grid 下 AnimatedBounds + Boundary 与 AnimatedBounds + LookaheadTextBlock，加入目标候选/排列提交、原生槽位/显示层分离。

验收：首次不动画；尺寸/位置同时变化只产生完整目标；宽高未约束和父级 Stretch 有明确定义；反向与目标变化接续；纯动画不驱动普通父级尺寸反馈；新目标准备中的直接后代 Measure 不导致异常、混合目标或闪帧。

### D. 内域自动建立和协作容器组合

将同一入口桥用于协作容器及 Boundary 内的自动入口，加入 ParentLayoutMode 和已知内域的目标依赖追踪。

验收：外动内静、内动外静、同时运动、外目标改变内约束、内目标改变外期望尺寸这五类情况；外域/内域的 Snapshot.Version 与工作计数符合事件归属，不形成每帧跨域重建。

### E. SharedBounds 与已有高级行为

迁移无 Root RemeasureToBounds，验证 motion frame、模板桥、嵌套共享元素、反向、取消、目标改动和最终交接。保持默认 ScaleToBounds 与 AnimatedContentSize 普通路径。

验收：复用现有 session/coordination/Boundary realization 回归，并增加内域与共享飞行同时活动的场景。Shared Host 拆除后无 capture、订阅或帧等待残留。

### F. 收敛公开协议与文档

在自动入口和嵌套域时序稳定后，再把现有 builder/bridge 适配到统一 Context/Result/Placeable 和辅助基类。发布 CurrentBounds、ParentLayoutMode、ReferenceElement 及查询门面，明确实验 API 的行为变化。

验收：第三方容器示例能只写一份策略；顺序重测/旧句柄拒绝/纯位置放置复用成立；更新旧文档与 demo，不同时遗留两种互相冲突的 Boundary 契约。

## 11. 测试如何迁移

保留数值、约束、速度连续性和资源释放等独立于 Root 的断言。测试 fixture 可逐步参数化为旧 Root 入口、无 Root 入口、Boundary 内独立入口，避免复制整套轨迹测试。

需要有意改写的旧语义包括：

- LookaheadBoundaryTest.NestedLookaheadRoot_IsDiagnosed：新设计应验证独立域的正确性；不能仅删除旧异常断言。
- InnerContent_CannotUseOuterTargetCoordinates_BoundaryStillCan：保留“不能读外域”断言，增加内部自动入口可读自己目标的断言。普通 Probe 仍不成为协议节点。
- 无 Root 的 AnimatedBounds 立即布局：改为自动入口动画；保留首次初始化和不支持内容的诊断。
- 直接把 AnimatedBounds.Bounds 当作动画采样的测试：分别验证原生布局槽位和 CurrentBounds/真实显示子树，不能机械改断言掩盖位置错误。
- Boundary 原生 Measure/Arrange 计数：分别记录外域采样、内域采样和真实内容更新，不能将允许的内域布局误当回归。

新增测试组可按以下职责组织：

| 建议测试组 | 关键验证 |
| --- | --- |
| LookaheadContextInheritanceTest | null 截断、继承切换、模板桥、旧本地值清理 |
| LookaheadLocalEntryTest | 普通父级、自然尺寸、无限测量、最终槽位、准备阶段 |
| LookaheadNestedDomainTest | 域身份/坐标隔离、目标依赖、失效归属、嵌套重入 |
| LookaheadBoundaryPresentationTest | 固定终点槽、动态对齐、输入位置、正常内容更新 |

先跑改动直接影响的测试，再在阶段边界跑全库。真实桌面 demo 重点检查开始/结束的一帧跳变、外框裁剪、文字稳定性和点击位置；Headless 数值测试不足以替代这部分验证。

## 12. 建议首先实施的最小变更

第一个代码变更只做 A：新增内部 Context，将旧 Root 的协调职责搬过去，保持用户可见行为及测试不变。随后 B 用已有 Root 验证附加属性和 Boundary 的双域隔离。这样能分别回答“上下文传播是否正确”和“无 Root 的布局时序是否可行”，失败时容易定位。

两个优先原型是 Boundary 的继承出口，以及 Measure 候选到 Arrange 提交的自动入口。它们的结构通过后，再扩大 API 表面和调整所有 demo。

## 13. 源码依据

- [当前 Root 协调器](../ComposeAnimation.Avalonia/Lookahead/LookaheadRoot.cs)
- [当前 Scope 查找](../ComposeAnimation.Avalonia/Lookahead/LookaheadScope.cs)
- [目标布局 Builder](../ComposeAnimation.Avalonia/Lookahead/LookaheadLayout.cs)
- [完整约束与缓存](../ComposeAnimation.Avalonia/Lookahead/LookaheadMeasure.cs)
- [实际放置 Frame](../ComposeAnimation.Avalonia/Lookahead/LookaheadPlacement.cs)
- [Boundary 与原生内容失效](../ComposeAnimation.Avalonia/Lookahead/LookaheadBoundary.cs)
- [AnimatedBounds](../ComposeAnimation.Avalonia/AnimatedBounds.cs)
- [AnimatedContentSize 的普通与协议路径](../ComposeAnimation.Avalonia/AnimatedContentSize.cs)
- [SharedBounds 重测接入](../ComposeAnimation.Avalonia/SharedTransition/SharedBoundsLookahead.cs)
- [Shared 坐标转换](../ComposeAnimation.Avalonia/SharedTransition/SharedLayoutBounds.cs)
- [现有 Boundary 测试](../ComposeAnimation.Test/LookaheadBoundaryTest.cs)

本机 Avalonia 对照：`D:/source/Avalonia/src/Avalonia.Base/AvaloniaProperty.cs` 的 inherits 参数；`StyledElement.cs` 的 InheritanceParent；`Avalonia.Controls/Decorator.cs` 的 ChildChanged；`Avalonia.Controls/Presenters/ContentPresenter.cs` 的继承父级接管与恢复；`Avalonia.Base/Layout/LayoutManager.cs` 的祖先优先 Measure/Arrange 调度。
