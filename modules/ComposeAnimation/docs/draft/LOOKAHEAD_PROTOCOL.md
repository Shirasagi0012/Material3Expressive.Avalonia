> **历史设计稿**：本文早于 2026-10 的动画基础重构，仅作历史参考，名称与行为可能已过时。现行用法见 [`docs/usage`](../usage/README.md)；迁移见 [迁移指南](../usage/migration-from-visual-animation-scope.md)。主要改名：`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`SharedAnimationFrameClock` → 内部 `FrameClock`，`IAnimationFrameClock` → `IFrameSource`，`AnimationScope<T>` → `AnimationRun<T>`。

# 实验性 Lookahead 布局协议

协议是公开的实验 API，尚无稳定兼容性承诺。新代码推荐使用单一
`ILookaheadLayoutPolicy`；旧 `ILookaheadLayoutNode` /
`ILookaheadConstrainedLayoutNode` 和 `LookaheadMeasure` 双策略桥继续作为兼容路径。

Lookahead 域可由支持自动入口的控件局部建立，也可由兼容 `LookaheadRoot` 显式建立。
使用协议不再要求 Root 包装。域身份通过继承环境传播，但只有被目标树注册的节点才是成员。

## 约束

`LookaheadConstraints` 表达 DIP 下的
`MinWidth/MaxWidth/MinHeight/MaxHeight`。最小值必须有限且非负；最大值可为正无穷，
且不得小于最小值。

```csharp
var limits = new LookaheadConstraints(
    minWidth: 100, maxWidth: 300,
    minHeight: 80, maxHeight: 200);
var fixedSize = LookaheadConstraints.Fixed(new Size(180, 120));
var maximumOnly = LookaheadConstraints.AtMost(new Size(300, 200));
var loose = limits.Loosen();
```

`Constrain(Size)` 把尺寸压入范围；`incoming.Constrain(preference)` 让 incoming 在冲突时优先。
`Offset` / `Deflate` 同时调整 minima 和 maxima。`Unbounded` 是无界范围；
`default(LookaheadConstraints)` 是 fixed zero。

完整约束请求包含子项 Margin 所占空间。公共桥先扣 Margin，再把 Width/Height/Min/Max 偏好
压入父约束，最后处理布局舍入。属性本身不会被临时改写。目标结果的有效完整约束记录在
`LookaheadLayoutResult.FullMeasureConstraints`；Size 兼容路径该字段为 null。

## 推荐的统一布局策略

`ILookaheadLayoutPolicy` 用一个 `MeasureLayout` 策略处理三个阶段：

```csharp
public enum LookaheadLayoutPhase
{
    Native,
    Target,
    Approach
}

public interface ILookaheadLayoutPolicy :
    ILookaheadConstrainedLayoutNode,
    ILookaheadPlacementHost
{
    LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context,
        LookaheadConstraints constraints);
}
```

- `Native`：低层桥未接管时的普通实际 fallback。自动入口的常态流程是 Target 候选提交后进入 Approach。
- `Target`：独立目标测量和放置；不得读取实时 Bounds、DesiredSize 或动画样本。
- `Approach`：已提交目标下的实际布局，可用 `TryGetTarget` 读取直接子项目标。
- `context.Measure(child, constraints)` 返回本轮 `LookaheadPlaceable`。
- `context.Layout(size, placement, layoutData)` 完成本轮测量结果。
- placement 回调通过 `LookaheadPlacementContext.Arrange(placeable, slot)` 放置本轮 placeable。

同一 child 可在一次 measure 中顺序重测，但只有最后返回的 placeable 仍有效；旧 handle 不可放置。
同一目标 candidate 可针对不同 `FinalSize` 重复执行 placement；提交只采用最近一次 placement
实际选择的分支。目标快照只包含最终有效 placeable 可达且实际放置的可见 child。每个可见 child 在一次
placement 回调中最多安排一次。placeable 和 placement context 都不能缓存到下一阶段或下一代。
placement 回调不能再次调用 `context.Measure`，也不能修改声明的直接 `LookaheadChildren`；
子项列表在整个 measure/place 中不得改变、重复或形成环。

### 推荐基类

库提供三个自动入口基类：

- `LookaheadPanel`：直接子项来自 `Children`。
- `LookaheadDecorator`：直接子项来自 `Child`。
- `LookaheadControl`：默认无子项。

三个基类都提供可覆盖的受保护 `LookaheadChildren`、`LookaheadChildrenChanged` 和
`LookaheadProperties`，用于声明非默认子树、集合通知与额外目标输入。

派生类只需实现一个受保护的 `MeasureLayout`。例如：

```csharp
public sealed class SingleChildPanel : LookaheadPanel
{
    protected override LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context,
        LookaheadConstraints constraints)
    {
        var child = Children.Count == 0
            ? null
            : context.Measure(Children[0], constraints.Loosen());
        var size = constraints.Constrain(child?.DesiredSize ?? default);

        return context.Layout(size, placement =>
        {
            if (child is not null)
                placement.Arrange(child, new Rect(placement.FinalSize));
        });
    }
}
```

基类负责原生、目标和 approach 三阶段入口、完整约束桥、自动局部域、父域加入、
visual-parent 变化及 detach 清理。额外目标输入仍应出现在 `LookaheadProperties` 中；
非 AvaloniaProperty 输入或可变对象原地更新应实现 `ILookaheadTargetInvalidationSource`。

### 低层统一桥

不能继承基类的控件可实现 `ILookaheadLayoutPolicy`，并在核心入口使用：

```csharp
protected override Size MeasureCore(Size availableSize)
    => LookaheadLayout.TryMeasureCore(this, availableSize, out var desired)
        ? desired : base.MeasureCore(availableSize);

protected override void ArrangeCore(Rect finalRect)
{
    if (LookaheadLayout.TryArrangeCore(this, finalRect, out var bounds))
        Bounds = bounds;
    else
        base.ArrangeCore(finalRect);
}

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    LookaheadLayout.Forget(this);
    base.OnDetachedFromVisualTree(e);
}

protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);
    if (change.Property == VisualParentProperty)
        LookaheadLayout.Forget(this);
}
```

低层桥控件在 visual parent 变化时用公开 `Forget` 释放旧入口；优先使用提供的基类。
`LookaheadLayout.Forget` 清除实际策略缓存、完整约束状态和拥有的局部域。

## 自动入口与提交顺序

自动入口在 Measure 阶段建立目标测量候选，并向普通 Avalonia 父级返回终点
`DesiredSize`。最终位置和拉伸尺寸由父级 Arrange 决定，所以完整矩形在 Arrange 中提交：

```text
native Measure
  -> build/reuse target candidate
  -> return target DesiredSize

native Arrange(slot)
  -> resolve target Bounds
  -> complete target placement
  -> atomically publish snapshot
  -> measure and arrange approach content
```

准备中的候选不属于公开 API，不启动动画，也不会把新尺寸和旧位置拼成临时目标。
同一 candidate 可在父级给出不同最终尺寸时重新 placement；每次提交只发布最近 placement
实际安排的分支。已测量但未安排的 child 及其后代不获得默认 Rect，也不进入公开快照，
因此不会产生 `TargetBounds` 或启动动画。其候选测量只能由框架在内部 actual pass 中安全借用，
例如 AnimatedBounds 在目标提交前后的 presentation 测量。
提交失败不会发布部分快照。纯位置帧复用实际测量，只重新运行必要的 placement。

入口若发现自己已被协作父级注册，会加入父域并释放原先拥有的局部域。detach、reparent、
Boundary 穿越和完整约束模式切换都会清理与父级/域身份绑定的缓存。

## 兼容的双策略协议

已有控件可继续实现 `ILookaheadConstrainedLayoutNode`：

```csharp
Size MeasureLookahead(
    LookaheadLayoutBuilder builder,
    LookaheadConstraints constraints);

Size MeasureApproach(LookaheadConstraints constraints);
```

前者是纯目标策略，后者是真实 approach 测量。实际核心入口继续使用
`LookaheadMeasure.TryMeasureCore` / `TryArrangeCore` / `Forget`。

| 请求 | 目标分支 | 实际分支 |
| --- | --- | --- |
| 要求完整能力 | `builder.Measure(child, limits)` | `LookaheadMeasure.Measure(child, limits)` |
| 允许旧控件边界 | `builder.MeasureChild(child, limits)` | `LookaheadMeasure.MeasureChild(child, limits)` |
| 最大尺寸兼容路径 | `builder.Measure(child, size)` | `LookaheadMeasure.Measure(child, size)` |

严格完整请求要求 `ILookaheadConstrainedLayoutNode`。兼容 `MeasureChild` 只有在没有正 minima
时才可回退到 Size 路径；需要保留 minima 时，子项必须支持完整协议，或由父策略明确 `Loosen`。
切回 Size 路径应调用 `LookaheadMeasure.Measure(child, Size)`，不能用相同 MaxSize 的
`child.Measure` 暗示模式切换。

旧桥要求 `ArrangeOverride` 返回传入的完整 finalSize，容器通过
`LookaheadPlacement.ArrangeChild` 安排直接视觉子项。缺少核心桥会在完整请求时报告错误。
这条路径用于现有内置控件和渐进迁移；新自定义布局优先使用统一策略。

## 目标输入、快照和查询

域观察标准尺寸、Margin、Alignment、IsVisible、UseLayoutRounding、FlowDirection、标准内容属性，
以及节点声明的 `LookaheadProperties` 和子集合通知。

目标算法不得任意调用真实 `Control.Measure/Arrange`，也不得读取实时 Bounds/DesiredSize。
`LookaheadBoundary` 的私有原生内容入口是明确例外；Boundary Child 不进入外域目标树。

目标提交后使用：

```csharp
var snapshot = Lookahead.GetSnapshot(participant);
var target = Lookahead.TargetFor(participant);

Lookahead.AddTargetLayoutUpdatedHandler(participant, OnTargetLayoutUpdated);
Lookahead.RemoveTargetLayoutUpdatedHandler(participant, OnTargetLayoutUpdated);

Lookahead.InvalidateTargetLayout(participant);
Lookahead.InvalidateApproachMeasure(participant);
Lookahead.InvalidateApproachArrange(participant);
```

`GetSnapshot` 只对当前域成员返回已提交且未失效的不可变快照。普通控件即使继承到环境上下文，
没有注册进目标树也不是成员。每次提交还会向各参与者直接发送
`Lookahead.TargetLayoutUpdatedEvent`。

`builder.SetLayoutData` / `context.Layout(..., layoutData)` 可保存本代不可变辅助数据。
框架保存对象引用，不深拷贝；不可变性由策略实现者负责。

## 实际放置与坐标

协作容器必须通过 `LookaheadPlacement.ArrangeChild(parent, child, localSlot)` 放置每个直接视觉子项，
并返回完整 finalSize。helper 携带本轮父原点，使后代在 Avalonia 提交父 Bounds 前也能获得正确坐标。
不能跨 ContentPresenter、Border 或模板层把非直接视觉后代传给 helper。

`ILookaheadMotionFrameProvider` 为直接子项提供连续位移，例如滚动的 `-Offset`。
motion-frame 变化只使 approach placement 失效，不重建目标快照。
`LookaheadCoordinates` 可查询同域目标/实际 Bounds 或位置，并选择是否包含 motion frame。
reference 必须是同域协作祖先；RenderTransform、RTL 镜像和跨窗口转换不在当前协议内。

`Lookahead.MoveContent` 可在同一附着域和坐标参考中同步移动内容并保留符合条件的 motion 状态。
它不提供 keyed identity、退出保留或跨域连续性。

## LookaheadBoundary

Boundary 自身属于外域，目标快照包含 Boundary；普通 Child 由私有 host 原生布局且不进入外域快照。
host 保留 DataContext、字体、FlowDirection 和资源继承，只对 Lookahead 上下文设置本地 null。

因此 Boundary 内容可建立独立自动域或使用兼容 Root。内外域的目标、approach 失效和坐标参考隔离。
Boundary 不再禁止内容中重开域。详细生命周期见
[Boundary 说明](LOOKAHEAD_BOUNDARY_DESIGN.md)。

## 内置策略和限制

- `LookaheadBox` 默认自己消费 minima；`PropagateMinConstraints=true` 时传给子项。
- `LookaheadStackPanel` 在堆叠轴给子项无界最大值，不实现 Row/Column 权重。
- `LookaheadScrollViewport` 要求有限 viewport，不提供虚拟化或 ScrollViewer 模板适配。
- `LookaheadTextBlock` 使用独立目标排版和真实 approach 排版；Inlines/自定义模板仍有限制。
- `LookaheadSize`、`LookaheadPosition` 已支持完整协议与自动入口，但仍使用两个独立动画状态。
- `AnimatedBounds` 使用私有 presentation host，使宿主 Bounds 保持原生终点槽位；
  `CurrentBounds` 才是动画显示矩形。
- 普通 Grid/WrapPanel 不会透明参与。使用统一策略实现协作容器，或用 Boundary 把它作为原生叶子。
- 固定 Width/Height 的普通控件仍是兼容不透明叶子，不等同于完整约束支持。
- 没有 intrinsic 测量、Compose Modifier 链、快照读跟踪或逐节点 approach 调度。
