> **历史设计稿**：本文早于 2026-10 的动画基础重构，仅作历史参考，名称与行为可能已过时。现行用法见 [`docs/usage`](../usage/README.md)；迁移见 [迁移指南](../usage/migration-from-visual-animation-scope.md)。主要改名：`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`SharedAnimationFrameClock` → 内部 `FrameClock`，`IAnimationFrameClock` → `IFrameSource`，`AnimationScope<T>` → `AnimationRun<T>`。

# AnimatedBounds（实验性）

`AnimatedBounds` 把目标位置和尺寸作为完整矩形动画，并按当前内层尺寸测量、放置真实内容。它可直接放在普通 Grid/StackPanel 中；没有可加入的协议父域时自动创建局部域。无需额外 Scope，不使用 VisualBrush 缩放，不改写用户 Width/Height 或 RenderTransform。

## 使用

```xml
<Grid Width="400" Height="300"
      xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation">
    <ca:AnimatedBounds x:Name="Card" TargetSize="140,180" Padding="8"
                       HorizontalAlignment="Left" VerticalAlignment="Top">
        <ca:LookaheadTextBlock TextWrapping="Wrap"
                              Text="Real content wraps during the flight." />
    </ca:AnimatedBounds>
</Grid>
```

```csharp
Card.TargetSize = new Size(240, 120);
Card.HorizontalAlignment = HorizontalAlignment.Right;
Card.VerticalAlignment = VerticalAlignment.Bottom;
```

`TargetSize` 包含 Padding；null 使用中间策略和内容的自然目标尺寸。入口 Measure 只产生测量候选，Arrange 获得原生槽位后才原子提交完整目标。首次布局直接初始化到终点；后续从当前采样及速度重定向。无限测量轴可以产生有限的自然尺寸，不要求为整个域提供固定视口；兼容 `LookaheadRoot` 自身仍保留有限视口契约。

直接 Child 必须支持完整约束协议。普通 Grid、Button、UserControl 等用 `LookaheadBoundary` 包装：

```xml
<ca:AnimatedBounds TargetSize="240,120">
    <ca:LookaheadBoundary ContentAlignment="TopStart" Padding="12">
        <local:CardView />
    </ca:LookaheadBoundary>
</ca:AnimatedBounds>
```

Boundary 的原生内容保持终点约束和局部槽位，外层采样只改变承载层位置和外框裁剪。它在私有内容出口以本地 null 截断可继承 Context；内容中的自动入口可以建立独立内域。内域有自己的快照、动画和正常内容失效，外域采样不主动给它传入中间约束。普通子树不能因继承了 Context 就自动变成协议节点。

## 父布局与公开几何

| API | 行为 |
| --- | --- |
| `ParentLayoutMode=TargetSize` | 默认。向父级报告目标尺寸，宿主保留父布局槽位，私有显示层动画 |
| `ParentLayoutMode=AnimatedSize` | 仅同域协议父级。向父级报告中间尺寸，可驱动兄弟布局 |
| `Bounds` | Avalonia 实际布局中的宿主矩形，使用父级局部坐标 |
| `CurrentBounds` | 当前外层动画显示矩形，使用 ReferenceElement 或域默认参考 |
| `TargetBounds` | 同一参考下完整提交的目标显示矩形；未布局或目标失效时为 null |
| `IsRunning` | 内外任一 Rect 动画仍在运行 |

CurrentBounds、TargetBounds、IsRunning 均为只读、可绑定的 DirectProperty。配置为 StyledProperty。CurrentBounds 不包含用户 RenderTransform，也不保证等于宿主 Bounds 或屏幕绘制 AABB。模式切换使实际布局失效，在参考及目标不变时保留当前采样；普通父级下使用 AnimatedSize 会报错。

```xml
<ca:LookaheadStackPanel Spacing="12">
    <ca:AnimatedBounds ParentLayoutMode="AnimatedSize" Padding="8">
        <ca:LookaheadTextBlock Text="Padding changes also move the next item." />
    </ca:AnimatedBounds>
    <ca:LookaheadBoundary><Button Content="Sibling" /></ca:LookaheadBoundary>
</ca:LookaheadStackPanel>
```

Width/Height/Min/Max 仍为原生尺寸偏好，不是动画输出；传入完整固定范围时按 incoming 优先规则处理。活动内层以 `LookaheadConstraints.Fixed(动画尺寸)` 真实测量 Child，包括其 Margin。初次布局及内层完成后恢复目标请求。普通内容经 Boundary 保留原生尺寸/溢出语义；这不等于让所有普通控件获得完整约束协议。

AnimatedBounds 默认不裁剪，Boundary 默认裁剪。真实承载层负责显示位置和输入位置；仍受祖先裁剪及原生命中规则影响。文字换行是离散变化。

## BoundsTransform 与中间布局

```csharp
Card.BoundsTransform = new BoundsTransform
{
    Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(400))
};
Card.TargetSize = null;
Card.IntermediateLayout = BoundsLayout.FixedSize(new Size(200, 160))
    .Then(BoundsLayout.Padding(new Thickness(20)))
    .Then(BoundsLayout.Offset(new Vector(50, 20)));
```

默认使用无回弹、StiffnessMediumLow 的 spring，每条边阈值为 0.5 DIP。可派生 BoundsTransform，根据 Create(initialBounds, targetBounds) 的两个完整矩形选规格。ArcAnimationSpec 通过 RectConverter 分别驱动两角。

组合顺序：外层 Rect 动画 → TargetSize → Padding → IntermediateLayout → 内层 Rect 动画 → Child。内外状态独立，共享本控件时钟；一次目标更新可能调用两次 BoundsTransform。没有变化的边界不重新调用策略，替换策略不重新计时正在运行的动画。

Then 按外到内组合。上述外层目标为 200×160、内层 160×120、内层偏移 (70,40)；Padding 放到 FixedSize 前会改变外层尺寸。Offset 为物理 DIP，只改变放置、不扩大占位。控件 Padding 遵循自身布局舍入；策略工厂使用原始 DIP。

派生 BoundsLayout 可重写 `Measure(constraints, measureChild)`：同步调用 measureChild **恰好一次**，返回有限、受约束的 BoundsLayoutResult。漏测、重复调用、返回后再使用测量回调均被拒绝。此单子项策略的契约与通用 LookaheadPanel 的顺序重测不同。

```csharp
return new BoundsLayoutResult(reportedSize, default)
{
    Placement = context => new Point(
        context.Size.Width - context.ChildSize.Width,
        context.Size.Height - context.ChildSize.Height)
};
```

Placement 接收已保存的测量尺寸和本轮原点，可以在纯位置帧复用。Origin 相对域 CoordinateReference；目标原点排除 motion frame，实际原点包含实时位移。IsLookingAhead 区分两阶段。Then 传递各层原点；内层动画补偿中间放置，使内容落在自己的 Rect 轨迹上。回调不能修改树、调用真实 Measure/Arrange 或读旧 Bounds 推断当前祖先位置。

策略实例应不可变；替换 IntermediateLayout 自动使目标失效。外部输入变化使用 `Lookahead.InvalidateTargetLayout(Card)`。仅实际放置改变用 `Lookahead.InvalidateApproachArrange(Card)`；它不会重建目标。

## 坐标与 motion frame

```csharp
Card.ReferenceElement = ContentBox; // 同域协议祖先，null 使用默认参考
Card.AnimateMotionFrameOfReference = false;
```

ReferenceScope 是 ReferenceElement 的兼容别名。自动域默认参考入口的直接原生视觉父级；兼容 Root 域仍参考 Root。选择参考不创建新域，不能跨 Boundary 引用其他域、引用普通未注册祖先或非祖先。修改参考或 motion-frame 模式显式重置动画。

默认 false 时，滚动等已声明位移立即跟随，不重定向动画、不重建快照；true 时参考以下的位移进入 Rect 动画目标。参考以上的普通视觉运动直接带动整个区域。CurrentBounds 和 TargetBounds 两者都包含当前声明的 motion-frame 位移；BoundsTransform 的 From/To 使用动画坐标基准，排除位移时不能与公开显示矩形强行逐值比较。

`LookaheadCoordinates.TryGetTargetBounds` / `TryGetActualBounds` 在目标不可用时返回 false；默认参考 CoordinateReference，默认排除 motion frame。旧 TargetPosition/ActualPosition 保留默认包含位移的行为。Actual 查询表示宿主实际布局几何；动画显示请读 CurrentBounds。快照 Bounds 始终排除连续位移。

LookaheadScrollViewport 是独立协作滚动适配器，支持 Offset/Extent/Viewport、双轴选择、滚轮与边缘冒泡；自身需有限视口，内容在可滚轴无限测量。它支持自动入口，无需 Root。没有原生 ScrollViewer 模板、滚动条、惯性、BringIntoView、虚拟化或锚定适配；第三方可实现 ILookaheadMotionFrameProvider。

## 同一实例跨父级移动

```csharp
Lookahead.MoveContent(Card, () =>
{
    Source.Children.Remove(Card);
    Destination.Children.Add(Card);
    Card.TargetSize = new Size(240, 120);
});
```

移动必须同步完成，并留在同一已确认域、窗口和参考祖先下。它保留已初始化 AnimatedBounds 的当前值、四边速度及时间线，再从新目标重定向；普通 Remove/Add 仍释放动画。入口自身不能以这种方式跨域移动。

不允许 await、async void、推进时钟、运行布局或嵌套移动事务。失败取消保留状态、安排布局，**不回滚树**。隐藏、Reset、换参考或换时钟会放弃事务内保留。此机制没有 keyed identity、异步离树、焦点或退出保留，也不提供跨裁剪的 overlay。

跨 motion-frame 边移动时，默认模式累计改变边的位移差；未改变边仍即时跟随。自定义绝对关键帧应以 BoundsTransform 的 From/To 构造，因为其累计补偿基准不保证与公开显示矩形逐值一致。

## 时序、查询与错误恢复

- Measure 候选尚无完整位置；不公开候选、不拼接新尺寸和旧位置。后代准备阶段重测使用候选尺寸，提交后重新确认实际测量。
- 尺寸/约束变化重测实际内容；纯位置帧复用 Measure，仍执行必要的祖先补偿。默认模式不向普通父级反馈中间 DesiredSize。
- `Lookahead.GetSnapshot(participant)` 是纯查询；目标脏或非成员返回 null。快照保存 Entry、CoordinateReference、Version 和不可变目标结果。Version 只在同一 Context 生命周期内可比较。
- `Lookahead.TargetLayoutUpdatedEvent` 为 Direct 路由，每次提交通知协议参与者。回调可以使后续目标失效，但不能重入当前布局。正在执行的实际 pass 可以继续读取它已捕获的完整快照。
- 默认域身份通过内部继承附加属性传播，动画采样不每帧替换 Context。detach、隐藏和跨域迁移重置，Boundary 内新域独立清理。
- 替换时钟保留采样、重建时间线，不保证旧速度/时间原点。ResetAnimation 忘记两套状态并释放等待。
- 动画规格失败/非有限样本落到目标，通过 Animated.LastError 报告并抑制同一失败目标重试。时钟失败等待换钟、重挂或 Reset。
- 中间策略、真实测量、放置等结构错误停止相关布局动画并传播异常；目标构建失败不提交部分快照。其他独立域不因此销毁。

## 兼容变化及范围

AnimatedBounds 和 LookaheadBoundary 现在继承 Control，自管 Child/Padding，并使用原属性 AddOwner；XAML 内容语义保留。依赖 Decorator 基类或基类样式选择器的代码需迁移。旧显式 LookaheadRoot 仍可用，边界内也允许独立 Root；新的推荐用法是自动入口。

这仍不是 Compose 框架完整移植：没有任意普通容器的透明预测、完整 Modifier 链、intrinsics、任意变换/RTL 镜像/跨窗口预测或框架级 approach 跳过。区域内支持协议平移和声明的 motion frame。完整扩展协议见 [LOOKAHEAD_PROTOCOL](LOOKAHEAD_PROTOCOL.md)，设计与阶段证据见 [实施状态](LOCAL_LOOKAHEAD_IMPLEMENTATION_STATUS.md)。

AnimatedContentSize 保留普通 animateContentSize 路径。SharedBounds.RemeasureToBounds 使用自身局部域和 Shared session，无需再包 AnimatedBounds；默认 ScaleToBounds 保持普通缩放路径。

## 示例与验证

- `dotnet run --project Catalog/Catalog.csproj -- --demo bounds/basic`：布局动画交互演示；其它 Bounds 演示可在 Catalog 中选择。
- `dotnet run --project Catalog/Catalog.csproj -- --run --all --out artifacts/catalog-actions`：运行现有 Catalog 验证动作。

旧 Scratchpad 演示及其 `--local`、`--verify-local` 入口已移除，下面保留历史验证记录。

2026-09-22 桌面验证通过：宿主终点槽位稳定、按钮在显示位置命中、焦点保持、Boundary 裁剪输入、普通内容不随外层帧重新布局、完成后无帧等待。截图已检查。Headless 的 ParentLayout、AutomaticDomain、LocalTransaction、坐标/移动及错误恢复测试补充数值与生命周期验证；这些工作计数不是性能基准。
