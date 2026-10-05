# 自定义协作式 Lookahead 容器

> [!WARNING]
> 本文介绍的协作式 lookahead 布局协议（`ILookaheadLayoutPolicy`、`LookaheadPanel` 等）目前为**实验性功能**，API 可能在未来版本中发生变化。

本文面向扩展作者：当你需要 `LookaheadBox`/`LookaheadStackPanel` 之外的布局规则，或需要让自定义控件参与域内的目标预测量时，可以实现协作式 lookahead 协议。协议的核心思想是**一套策略跑三个阶段**：普通原生布局、独立的目标预测量（Target）、以及已提交目标下的实际布局（Approach）。三个阶段共用同一份布局逻辑，因此两端点的内容布局在任何中间尺寸下都保持一致。

- 程序集：`ComposeAnimation.Avalonia`，命名空间 `ComposeAnimation`
- XAML 前缀 `ca:`，xmlns `https://github.com/Shirasagi0012/ComposeAnimation`

## 布局阶段与策略模型

策略接口只有一个方法：

```csharp
public enum LookaheadLayoutPhase
{
    Native,    // 协议未接管时的普通实际布局（无可用域）
    Target,    // 独立目标测量/放置：不得读取实时 Bounds、DesiredSize 或动画采样
    Approach   // 已提交目标下的实际布局：可用 TryGetTarget 读取直接子级的目标
}

public interface ILookaheadLayoutPolicy : ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost
{
    LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints);
}
```

`LookaheadMeasureContext` 提供测量期的全部能力：

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Phase` | `LookaheadLayoutPhase` | 当前阶段 |
| `Measure(Control, LookaheadConstraints)` | 方法 → `LookaheadPlaceable` | 测量一个已声明的直接 `LookaheadChildren` 子级，返回本轮句柄 |
| `TryGetTarget(Control, out LookaheadLayoutResult)` | 方法 → `bool` | 读取子级已提交的目标结果；仅 `Approach` 阶段可用 |
| `Layout(Size, Action<LookaheadPlacementContext>?, object? layoutData)` | 方法 → `LookaheadMeasureResult` | 结束本轮测量：报告尺寸，可选给出放置回调与不可变的布局数据 |

`LookaheadPlaceable` 是子级的一次性测量句柄，公开 `DesiredSize`。放置回调收到 `LookaheadPlacementContext`：

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Phase` | `LookaheadLayoutPhase` | 当前阶段 |
| `FinalSize` | `Size` | 本轮最终尺寸 |
| `Arrange(LookaheadPlaceable, Rect)` | 方法 | 把本轮测得的子级放进父级局部坐标的矩形槽位 |

`LookaheadMeasureResult` 公开 `Size` 与 `LayoutData`；`LayoutData` 会随目标结果存入快照（`LookaheadLayoutResult.LayoutData`），用于把测量期的不可变数据传递到放置阶段。

## 推荐路径：继承 `LookaheadPanel` / `LookaheadDecorator` / `LookaheadControl`

三个抽象基类已经实现了全部接线（`MeasureCore`/`ArrangeCore` 转发、域接入、detach 清理）。按子级模型选择基类，然后只重写一个 `MeasureLayout`：

| 基类 | 继承 | 适用 |
| --- | --- | --- |
| `LookaheadPanel` | `Panel` | 多子级容器；默认 `LookaheadChildren` 为 `Children`，`LookaheadChildrenChanged` 跟随 `Children` |
| `LookaheadDecorator` | `Decorator` | 单子级包装；默认 `LookaheadChildren` 为 `Child` |
| `LookaheadControl` | `Control` | 自管视觉子级的控件；在构造函数里自行加入 `LogicalChildren`/`VisualChildren` 并重写 `LookaheadChildren` |

可选重写（均为 `protected virtual`）：

- `LookaheadChildren`：声明参与预测量的直接子级；不得包含重复控件。
- `LookaheadChildrenChanged`：子级集合由迭代器投影时，提供稳定的集合变更通知源。
- `LookaheadProperties`：声明除常见布局几何属性之外、会影响目标布局的附加属性（这些属性的变化会自动重建目标）。

一个最小容器只需：

```csharp
public sealed class OffsetPanel : LookaheadPanel
{
    protected override LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        var placeable = context.Measure(Children[0], constraints.Loosen());
        var size = constraints.Constrain(placeable.DesiredSize);
        return context.Layout(size, placement =>
            placement.Arrange(placeable, new Rect(new Point(10, 10), placeable.DesiredSize)));
    }
}
```

由于三个阶段共用同一份代码，测量回调里写一次、放置回调里写一次，目标布局与实际布局自然一致。

## 完整示例：`SharedCardLayout`

下面的自定义布局示例可用于 `SharedBounds` 的 `RemeasureToBounds`，在折叠/展开两个端点间做共享元素过渡。它是一个自管子级的 `LookaheadControl`：两张 `LookaheadTextBlock` 参与预测量，`Expanded` 只影响静止目标，几何计算同时服务于布局与渲染。

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation;

/// <summary>一张活动卡片：共享矩形同时驱动色块几何与文本布局。</summary>
public sealed class SharedCardLayout : LookaheadControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SharedCardLayout, string?>(nameof(Title));
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SharedCardLayout, string?>(nameof(Description));
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<SharedCardLayout, Color>(nameof(Color));
    public static readonly StyledProperty<bool> ExpandedProperty =
        AvaloniaProperty.Register<SharedCardLayout, bool>(nameof(Expanded));

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public bool Expanded { get => GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }

    // 自管子级：纯文本协作控件，参与目标预测量
    private readonly LookaheadTextBlock _title = new()
    {
        FontSize = 18, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis,
        UseLayoutRounding = false
    };
    private readonly LookaheadTextBlock _description = new()
    {
        FontSize = 15, Foreground = new SolidColorBrush(Color.Parse("#C8C8D1")),
        TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false
    };
    private IBrush _colorBrush = Brushes.Transparent;

    static SharedCardLayout()
    {
        // 目标输入属性必须进入 AffectsMeasure，并被 LookaheadProperties 声明
        AffectsMeasure<SharedCardLayout>(ExpandedProperty, TitleProperty, DescriptionProperty);
        AffectsRender<SharedCardLayout>(ColorProperty);
    }

    public SharedCardLayout()
    {
        ClipToBounds = true;
        // LookaheadControl 不托管子级集合：自行维护逻辑树与视觉树
        LogicalChildren.Add(_title);
        LogicalChildren.Add(_description);
        VisualChildren.Add(_title);
        VisualChildren.Add(_description);
    }

    // 向协议声明子级与附加目标输入
    protected override IEnumerable<Control> LookaheadChildren => [_title, _description];
    protected override IEnumerable<AvaloniaProperty> LookaheadProperties =>
        [ExpandedProperty, TitleProperty, DescriptionProperty];

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty) _title.Text = Title;
        else if (change.Property == DescriptionProperty) _description.Text = Description;
        else if (change.Property == ColorProperty)
        {
            _colorBrush = new SolidColorBrush(Color);
            _title.Foreground = _colorBrush;
        }
    }

    // 三个阶段共用同一份策略
    protected override LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        // Expanded 只决定静止目标；飞行中的固定约束同时决定两个端点的实际布局，
        // 因此中途反转不会切换到第二条轨迹。
        var size = constraints.Constrain(Expanded ? new Size(440, 380) : new Size(160, 150));
        var geometry = Geometry(size);
        var textConstraints = LookaheadConstraints.AtMost(new Size(geometry.TextWidth, double.PositiveInfinity));
        // 每个直接子级恰好测量一次，拿到本轮 placeable 句柄
        var title = context.Measure(_title, textConstraints);
        var description = context.Measure(_description, textConstraints);
        return context.Layout(size, placement =>
        {
            // 放置回调按本轮 FinalSize 重新推导几何，再放置测量得到的句柄
            var current = Geometry(placement.FinalSize);
            placement.Arrange(title, new Rect(current.TextLeft, current.TitleTop,
                current.TextWidth, title.DesiredSize.Height));
            placement.Arrange(description, new Rect(current.TextLeft,
                current.TitleTop + title.DesiredSize.Height + 16, current.TextWidth,
                description.DesiredSize.Height));
        });
    }

    // 由当前尺寸推导全部几何：进度驱动内边距、色块高度与文本宽度
    internal static CardGeometry Geometry(Size size)
    {
        var progress = Math.Clamp((size.Width - 160) / (440 - 160), 0, 1);
        var inset = 16 * (1 - progress);
        var height = 84 + (170 - 84) * progress;
        var textLeft = 16 + 8 * progress;
        return new(new Rect(inset, inset, Math.Max(0, size.Width - 2 * inset), height),
            12 * (1 - progress), textLeft, Math.Max(0, size.Width - 2 * textLeft),
            inset + height + 12 + 10 * progress);
    }

    internal readonly record struct CardGeometry(
        Rect Swatch, double Radius, double TextLeft, double TextWidth, double TitleTop);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        // 渲染与布局使用同一套几何，任何中间尺寸下两者一致
        var geometry = Geometry(Bounds.Size);
        using (context.PushClip(new RoundedRect(new Rect(Bounds.Size), 16)))
            context.DrawRectangle(_colorBrush, null, geometry.Swatch, geometry.Radius, geometry.Radius);
    }
}
```

XAML 中使用（前缀与命名空间按本文开头约定声明一次即可）：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        xmlns:demo="clr-namespace:SharedTransitionDemo"
        x:Class="SharedTransitionDemo.MainWindow">
    <demo:SharedCardLayout Title="Lagoon" Description="折叠时是调色板卡片，展开后是详情页。"
                           Color="SteelBlue" Expanded="True" />
</Window>
```

## 约束与辅助 API

`LookaheadConstraints`（record struct）以 DIP 表达四边界约束：最小值必须有限非负，最大值可为正无穷。`default(LookaheadConstraints)` 是固定零，`new()` / `Unbounded` 是无界。

| 成员 | 说明 |
| --- | --- |
| `MinWidth` / `MaxWidth` / `MinHeight` / `MaxHeight` | 四个边界 |
| `MinSize` / `MaxSize` | 边界组成的尺寸 |
| `HasMinimum` / `HasFixedWidth` / `HasFixedHeight` | 约束形态判断 |
| `Unbounded` | 无界范围 |
| `AtMost(Size)` / `Fixed(Size)` | 常用工厂：上限约束 / 固定约束 |
| `Loosen()` | 去掉最小值（最小值归零） |
| `Constrain(Size)` / `Constrain(LookaheadConstraints)` | 把尺寸压入范围；或把另一范围夹进本范围（冲突时 `this` 优先） |
| `Offset(double, double)` / `Deflate(Thickness)` | 同时平移四个边界（下限在 0 处截断，无界上限保持无界） |

放置与测量辅助：

| API | 说明 |
| --- | --- |
| `LookaheadPlacement.ArrangeChild(parent, child, slot)` | 在父级局部坐标中放置直接视觉子级，并传播 approach 坐标；`ILookaheadPlacementHost` 的约定入口 |
| `LookaheadMeasure.Measure(control, constraints)` / `Measure(control, Size)` | 完整约束 / 原生上限的真实测量入口 |
| `LookaheadMeasure.MeasureChild(control, constraints)` | 兼容边界：有能力的子级走完整协议，普通子级仅在没有正最小值时走原生路径 |
| `LookaheadMeasure.TryMeasureCore` / `TryArrangeCore` / `Forget` | `MeasureCore`/`ArrangeCore` 转发与 detach 清理（基类已代劳） |
| `LookaheadCoordinates.TryGetTargetBounds` / `TryGetActualBounds` | 读取已提交目标 / 实际布局的矩形，可选参考与运动参照系 |
| `LookaheadCoordinates.TargetPosition` / `ActualPosition` | 相对某祖先的目标 / 实际位置（默认含运动参照系位移） |

## 低层协议接口

一般情况下不需要直接实现这些接口——基类已经封装；无法继承基类时才用到：

| 接口 | 成员 | 说明 |
| --- | --- | --- |
| `ILookaheadLayoutNode` | `LookaheadChildren`、`LookaheadChildrenChanged`、`LookaheadProperties`、`MeasureLookahead(builder, Size)`、`ArrangeLookahead(builder, Rect)` | 独立目标策略；回调中不得测量/排列实时控件或读取 `DesiredSize`/`Bounds` |
| `ILookaheadConstrainedLayoutNode` | `MeasureLookahead(builder, LookaheadConstraints)`、`MeasureApproach(LookaheadConstraints)` | 完整约束参与者 |
| `ILookaheadPlacementHost` | （标记接口） | 约定：子级必须经 `LookaheadPlacement.ArrangeChild` 放置 |
| `ILookaheadMotionFrameProvider` | `GetMotionFrameOffset(Control)`、事件 `MotionFrameChanged` | 为直接子级贡献连续位移（如滚动）；偏移必须来自当前输入，不得来自动画采样或 `Bounds` |
| `ILookaheadTargetInvalidationSource` | 事件 `TargetMeasureInvalidated` | 属性系统之外的目标输入变化通知；即使实际布局已失效也应触发 |
| `ILookaheadLayoutPolicy` | `MeasureLayout(context, constraints)` | 统一策略接口（见上文） |

无法继承 `LookaheadPanel` 等基类时（例如已有其他基类），可以用 `static class LookaheadLayout` 低层桥：实现 `ILookaheadLayoutPolicy`，在 `MeasureCore`/`ArrangeCore` 开头分别调用 `LookaheadLayout.TryMeasureCore(this, ...)` / `TryArrangeCore(this, ...)`（返回 `false` 时回退 `base`），并在 `OnDetachedFromVisualTree` 调用 `LookaheadLayout.Forget(this)`。`ComposeAnimation.Test` 的 `LookaheadPolicyApiTest.cs` 中有完整示例（`ManualPolicyControl`）。

## 注意事项

- **协议不变量由运行时强制执行**：子级只能在 `MeasureLayout` 执行期间用 `context.Measure` 测量；每个可见子级每轮放置最多 `Arrange` 一次；`LookaheadPlaceable` 与 `LookaheadPlacementContext` 不能跨阶段、跨布局代使用（过期句柄会抛出 `InvalidOperationException`）；测量与放置期间不得修改子树。
- **Target 阶段的纯净性**：目标预测量不得读取实时 `Bounds`、`DesiredSize` 或动画采样，也不要保留 `measureChild` 之类的回调越界使用。需要把数据从测量传到放置时，用 `LayoutData` 或测量期捕获的不可变值。
- **声明目标输入**：影响目标布局的属性放入 `LookaheadProperties` 并 `AffectsMeasure`；属性系统之外的输入实现 `ILookaheadTargetInvalidationSource`，或主动调用 `Lookahead.InvalidateTargetLayout`。
- **结果必须来自当前上下文**：`MeasureLayout` 必须返回由本次 `context.Layout(...)` 创建的 `LookaheadMeasureResult`；尺寸必须有限非负，放置矩形必须有限非负。
- **基类之外的清理**：自定义成员若挂接了外部事件或时钟，记得在 detach 路径释放；基类已处理协议层面的 `Forget`。
- **更进一步**：非属性目标输入、嵌套域、Boundary 交互与快照语义的完整约定见协议参考文档。

## 延伸阅读

- [Lookahead 布局协议参考](../../draft/LOOKAHEAD_PROTOCOL.md)
- [协作式 Lookahead 实验概览](../../draft/LOOKAHEAD_EXPERIMENT.md)
- [AnimatedBounds](../components/animated-bounds.md)
- 协议测试参考：`ComposeAnimation.Test/LookaheadPolicyApiTest.cs`
