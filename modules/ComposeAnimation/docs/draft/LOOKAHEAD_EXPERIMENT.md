# Cooperative Lookahead 实验

状态：实验性。早期以 `LookaheadRoot` 为中心的设计已经由
[局部 Lookahead 设计](LOCAL_LOOKAHEAD_DESIGN.md) 和
[落地计划](LOCAL_LOOKAHEAD_IMPLEMENTATION_PLAN.md) 取代。本文描述当前已落地行为。

这套实现提供独立的目标测量/放置、真实 approach 布局、完整 min/max 约束、不可变目标快照，
以及协作坐标和 motion-frame 支持。它用于 `AnimatedBounds`、`LookaheadSize` /
`LookaheadPosition` 和 SharedBounds 的 `RemeasureToBounds`。它仍不是 Compose
`LookaheadScope`、Modifier 节点和快照读跟踪系统的完整移植。

## 域与入口

应用通常不需要显式 `LookaheadRoot`。下列控件在没有可加入的父域时会自动建立局部域：

- `AnimatedBounds`
- `LookaheadBox`、`LookaheadStackPanel`、`LookaheadScrollViewport`
- `LookaheadSize`、`LookaheadPosition`
- 使用 `SharedBounds.ResizeMode=RemeasureToBounds` 的端点
- 通过 [统一布局策略](LOOKAHEAD_PROTOCOL.md#推荐的统一布局策略) 接入的自定义控件

入口在原生 Measure 阶段建立或复用目标候选，并向普通 Avalonia 父级报告**终点**
`DesiredSize`；最终槽位到 Arrange 阶段才可知，因此完整目标矩形在 Arrange 中原子提交。
候选不是公开快照，不会启动动画。提交后才按当前样本测量和排列实际内容。

若节点已由协作父级注册，它加入父域，不创建第二个域。detach、视觉父级变化或从局部域迁入父域时，
旧上下文、约束状态和 approach 状态会被释放。普通父级不需要理解该协议。

`LookaheadRoot` 仍是兼容入口，适合应用明确需要固定有限 viewport 的场景。它不再是启用
Lookahead 的必需包装层，仍要求两轴有限，且仍不支持 `Padding`。

## Boundary 与嵌套域

`LookaheadBoundary` 是外域目标树的终点。Boundary 自身留在外域，普通内容通过私有 host
执行原生布局，并在继承出口把 Lookahead 上下文设为本地 null。DataContext、字体、
FlowDirection 和资源继承继续通过 host 传递。

Boundary 内容中允许自动入口或兼容 `LookaheadRoot` 建立独立内域。内外域有不同快照和坐标参考；
内域动画采样不重建外域目标，内域目标期望尺寸变化则按正常原生布局规则向上传播。
跨 Boundary 不承诺动画连续性。详见 [Boundary 说明](LOOKAHEAD_BOUNDARY_DESIGN.md)。

## 支持的节点

- `LookaheadBox`：叠放容器；`PropagateMinConstraints` 控制完整约束的 minima 是否传给子项。
- `LookaheadStackPanel`：非虚拟化横向/纵向堆叠，支持 `Spacing`。
- `LookaheadScrollViewport`：有限协作视口，提供 Offset/Extent/Viewport 和 motion frame；
  不等同于 ScrollViewer 模板、虚拟化或滚动锚定。
- `LookaheadTextBlock`：目标阶段独立进行文本排版；Inlines 和自定义文本模板仍有限制。
- `LookaheadSize`：按动画尺寸真实重测内容。
- `LookaheadPosition`：保留终点槽位并动画实际位置。
- `LookaheadBoundary`：让普通 Grid、UserControl 和模板子树成为原生终点内容。
- 固定 Width/Height 的普通控件仍可作为兼容的不透明叶子。

未知普通 Panel 不会被透明预测。需要预测 Grid、WrapPanel 或自定义容器的内部布局时，应实现
[统一布局策略](LOOKAHEAD_PROTOCOL.md#推荐的统一布局策略)，或把它放进 Boundary。

## 目标、实际与查询

目标算法不读取实时 `DesiredSize`、`Bounds` 或动画样本。它根据声明的直接子项和布局输入，
独立计算目标结果。目标输入失效后公开查询返回 null；下一次 Measure/Arrange 提交新的完整快照。
调用方持有的旧快照保持不可变。

```csharp
var snapshot = Lookahead.GetSnapshot(participant);
var target = Lookahead.TargetFor(participant);

Lookahead.AddTargetLayoutUpdatedHandler(participant, (_, e) =>
{
    var committed = e.Snapshot;
});
```

`Lookahead.GetSnapshot` 仅对当前域的实际成员返回结果。继承到上下文但未被目标树注册的普通包装控件
不是成员。`TargetLayoutUpdatedEvent` 是发给每个协作参与者的 Direct routed event。

失效入口按含义分开：

- `Lookahead.InvalidateTargetLayout(participant)`：布局输入改变，重建目标。
- `Lookahead.InvalidateApproachMeasure(participant)`：只重测实际样本，保留目标版本。
- `Lookahead.InvalidateApproachArrange(participant)`：只重新放置实际样本。

纯位置帧复用 approach 测量；尺寸样本才触发需要的真实重测。局部域的原生父级持续看到终点
`DesiredSize`，不会因中间样本每帧重新分配槽位。

## Bounds 和显示矩形

对 `AnimatedBounds`，Avalonia `Bounds` 是宿主从原生父布局得到的终点槽位；
`CurrentBounds` 是选定参考坐标下的当前显示矩形；`TargetBounds` 是同一参考下已提交的终点，
目标脏或尚未提交时为 null。内容由私有 presentation host 在当前矩形中真实布局。

因此 `Bounds` 不保证等于 `CurrentBounds`。位置动画不会通过逐帧改写宿主 Bounds 来迫使普通父级
重新布局，也不会占用用户 `RenderTransform`。完整行为见 [AnimatedBounds](ANIMATED_BOUNDS.md)。

## 坐标、滚动与移动

`LookaheadCoordinates` 可查询同域目标/实际坐标，并选择是否包含声明过的 motion frame。
`ReferenceScope` 必须是同域协作祖先。支持 `ILookaheadMotionFrameProvider` 的滚动位移不会单独
增加快照版本。RenderTransform、RTL 镜像、原生 ScrollViewer 的透明坐标适配和跨窗口转换仍不支持。

`Lookahead.MoveContent(content, move)` 可在同一附着域和坐标参考内同步移动现有内容，并保留符合
条件的 motion 状态。它不提供 keyed identity、退出保留、焦点迁移或跨域连续性。

## 与 Compose 的主要差异

| 项目 | 当前实现 |
| --- | --- |
| 域传播 | Avalonia 可继承附加上下文 + 已注册成员关系 |
| 目标计算 | 小型协作树的独立 builder；没有 Compose 快照读跟踪 |
| 自定义布局 | 单一 `ILookaheadLayoutPolicy.MeasureLayout`，三阶段复用 |
| 原生内容 | 通过 Boundary 截断并按终点原生布局 |
| 约束 | DIP/double 的四边界；兼容 Avalonia Size 路径 |
| 调度 | 域级目标失效，approach measure/arrange 分开；不是逐 Modifier 节点调度 |
| 叶子与模板 | 显式协作节点、Boundary 或固定尺寸不透明叶子；不透明接管任意模板 |

## 仍属实验范围

- API 和扩展协议仍可能调整。
- 不支持 intrinsic 测量、通用虚拟化、普通 ScrollViewer 透明适配。
- 普通 Grid/WrapPanel 不会自动变成协作布局。
- 跨域、跨 TopLevel 和任意 RenderTransform 坐标不提供连续动画保证。
- `LookaheadSize` 与 `LookaheadPosition` 使用独立动画状态，不等于 Compose 统一的
  `animateBounds` Modifier。

测试覆盖自动入口、显式 Root、完整约束、嵌套域、Boundary 继承出口、位置/尺寸动画、
滚动 motion frame、detach/reparent，以及目标与 approach 失效隔离。
