# AnimatedBounds

> [!WARNING]
> 本文介绍的协作式 lookahead 布局系统（`AnimatedBounds`、`Lookahead*` 容器及相关协议 API）目前为**实验性功能**，API 可能在未来版本中发生变化。

`AnimatedBounds` 是 ComposeAnimation 的旗舰边界动画控件，对应 Jetpack Compose 的 `animateBounds`：它把控件的**目标位置与目标尺寸作为完整矩形**一起做动画，并在动画过程中按当前矩形**实时重测真实内容**（例如文本会随宽度变化实时换行）。它不使用 `RenderTransform`，不改写 `Width`/`Height`，也不依赖截图或缩放。

它构建在协作式 lookahead 之上：域内的目标布局由协作式预测量先行算出并提交为不可变快照，实际布局再逐帧逼近目标。没有可加入的祖先域时，控件会**自动创建局部域**，因此可以直接放进普通的 `Grid`/`StackPanel`；`LookaheadRoot` 仍作为需要固定有限视口场景的显式（兼容）入口保留。在显式域内，`AnimatedBounds` 要求父级是 `ILookaheadPlacementHost`——所有 `Lookahead*` 容器与 `AnimatedContentSize` 都是。

- 程序集：`ComposeAnimation.Avalonia`，命名空间 `ComposeAnimation`
- XAML 前缀 `ca:`，xmlns `https://github.com/Shirasagi0012/ComposeAnimation`
- 目标框架 `net10.0`，Avalonia `12.1.2`

## 快速开始

把 `AnimatedBounds` 放进任意协作容器（这里用层叠容器 `LookaheadBox`），修改 `TargetSize` 或对齐方式即触发动画：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="Demo.MainWindow"
        Width="400" Height="320">
    <ca:LookaheadBox>
        <ca:AnimatedBounds x:Name="Card" TargetSize="150,230" Padding="8"
                           HorizontalAlignment="Left" VerticalAlignment="Top">
            <ca:LookaheadTextBlock FontSize="16" TextWrapping="Wrap"
                                   Text="这段真实文本会在矩形动画过程中按当前宽度实时重排。" />
        </ca:AnimatedBounds>
    </ca:LookaheadBox>
</Window>
```

```csharp
// 修改目标尺寸或对齐方式：矩形从当前采样值与速度平滑重定向
Card.TargetSize = new Size(270, 150);
Card.HorizontalAlignment = HorizontalAlignment.Right;
Card.VerticalAlignment = VerticalAlignment.Bottom;
```

要点：

- `TargetSize` 是**包含 `Padding`** 的外框目标尺寸；设为 `null` 时，目标由 `IntermediateLayout` 与内容的自然尺寸共同决定。
- 直接 `Child` 必须是协作控件（`LookaheadTextBlock`、`LookaheadBoundary`、嵌套的 `AnimatedBounds` 等）；普通控件请用 `LookaheadBoundary` 包裹（见下文「包裹普通内容」）。
- 首次布局直接在目标处初始化，之后的目标变更从当前矩形与速度继续。

## API 要点

### `AnimatedBounds`（`sealed`，继承 `Control`）

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Child` | `Control?` | `null` | 唯一子内容（`[Content]`，add-owner 自 `Decorator`），必须支持完整约束协议 |
| `Padding` | `Thickness` | `0` | 外框与内框之间的内边距（add-owner 自 `Decorator`） |
| `TargetSize` | `Size?` | `null` | 外框目标尺寸，**含 `Padding`**；`null` 表示由中间布局与内容自然尺寸决定 |
| `BoundsTransform` | `BoundsTransform` | `new BoundsTransform()` | 矩形动画策略；对外框与内框分别调用 |
| `IntermediateLayout` | `BoundsLayout` | `BoundsLayout.Empty` | 两个矩形动画之间的中间布局层 |
| `ReferenceElement` | `Control?` | `null` | 坐标参考祖先；`null` 使用域坐标参考。`ReferenceScope` 是同一属性的兼容别名；修改会重置动画 |
| `ParentLayoutMode` | `BoundsParentLayoutMode` | `TargetSize` | 实际布局向父级报告的尺寸，见下表 |
| `AnimateMotionFrameOfReference` | `bool` | `false` | 是否把滚动等已声明的连续位移并入动画目标；修改会重置动画 |
| `CurrentBounds` | `Rect?` | — | 只读。以 `ReferenceElement`（或域默认参考）为基准的当前外框动画矩形 |
| `TargetBounds` | `Rect?` | — | 只读。同一参考下已提交的目标矩形；未布局或目标失效时为 `null` |
| `IsRunning` | `bool` | — | 只读。内、外任一矩形动画仍在运行 |
| `ResetAnimation()` | 方法 | — | 取消内外两个边界的动画，下次布局直接在目标处初始化；用于错误恢复 |

### `BoundsParentLayoutMode`

| 值 | 说明 |
| --- | --- |
| `TargetSize` | 默认。向父级报告目标尺寸，父布局槽位保持稳定，动画由私有呈现层完成 |
| `AnimatedSize` | 向父级报告动画中的中间尺寸，可驱动同域兄弟布局；要求同域的参与式放置父级（`ILookaheadPlacementHost`），否则抛出 `NotSupportedException` |

### `BoundsTransform` 与 `BoundsLayout`

`BoundsTransform`（非密封类）为一次目标变更选择矩形动画规格：

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Spec` | `IFiniteAnimationSpec<Rect>` | `SpringSpec<Rect>`（`Spring.StiffnessMediumLow`，阈值 `VisibilityThresholds.Rect`） | 默认动画规格 |
| `Create(Rect initialBounds, Rect targetBounds)` | `virtual` 方法 | 返回 `Spec` | 可重写以根据起止矩形选择几何相关的路径（如弧线） |

`BoundsLayout`（抽象类）定义两层矩形动画之间的中间布局。组合顺序为**由外向内**：`a.Then(b)` 表示 `a` 包裹 `b`。

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Empty` | 静态属性 | 恒等布局（默认） |
| `FixedSize(Size)` | 静态工厂 | 为该层指定固定尺寸 |
| `Padding(Thickness)` | 静态工厂 | 为该层添加内边距（要求非负有限值） |
| `Offset(Vector)` | 静态工厂 | 物理 DIP 偏移；只改变放置，不扩大占位 |
| `Then(BoundsLayout)` | 实例方法 | 由外向内组合两层 |
| `Measure(LookaheadConstraints, Func<LookaheadConstraints, Size>)` | 抽象方法 | 自定义策略入口，契约见「注意事项」 |

自定义 `BoundsLayout` 返回 `BoundsLayoutResult(Size, Point ChildOffset)`，可选携带 `Func<BoundsPlacementContext, Point>? Placement` 动态放置回调；`BoundsPlacementContext` 提供 `(Size Size, Size ChildSize, Point Origin, bool IsLookingAhead)`。

### 协作容器

域内目标布局通过协作式预测量获得。下列容器全部实现 `ILookaheadPlacementHost`，可以直接承载 `AnimatedBounds`：

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `LookaheadBox` | `Panel`（`sealed`） | — | 层叠布局；期望尺寸为各子级逐轴最大值；子级 `Margin` 与对齐由放置辅助处理 |
| `LookaheadBox.PropagateMinConstraints` | `bool` | `false` | 完整约束测量是否把最小值传给子级（否则仅容器自身消费最小值） |
| `LookaheadStackPanel` | `Panel`（`sealed`） | — | 非虚拟化堆叠；堆叠轴方向按无限测量 |
| `LookaheadStackPanel.Orientation` | `Orientation` | `Vertical` | 堆叠方向 |
| `LookaheadStackPanel.Spacing` | `double` | `0` | 子级间距，要求有限且 ≥ 0 |
| `LookaheadScrollViewport` | `Decorator`（`sealed`） | — | 有限滚动视口，贡献 `-Offset` 作为运动参照系；**不是** `ScrollViewer` 模板适配器，无滚动条、惯性、虚拟化与锚定 |
| `LookaheadScrollViewport.Offset` | `Vector` | `(0,0)` | 滚动偏移；赋值时被强制夹取到 `[0, Extent - Viewport]` |
| `LookaheadScrollViewport.CanHorizontallyScroll` / `CanVerticallyScroll` | `bool` | `false` / `true` | 各轴是否可滚动（可滚轴上内容按无限测量） |
| `LookaheadScrollViewport.Extent` / `Viewport` | `Size` | — | 只读。内容范围与视口尺寸 |
| `LookaheadScrollViewport.MotionFrameChanged` | 事件 | — | 运动参照系（`Offset`）变化时触发 |
| `LookaheadBoundary` | `Control`（`sealed`） | — | 目标树的端点，用目标约束原生布局普通内容；`ClipToBounds` 默认为 `true` |
| `LookaheadBoundary.Child` | `Control?` | `null` | 普通内容（`[Content]`） |
| `LookaheadBoundary.Padding` | `Thickness` | `0` | 内容内边距 |
| `LookaheadBoundary.ContentAlignment` | `VisibilityAlignment` | `TopStart` | 目标内容槽位在当前动画内容区内的对齐 |
| `LookaheadBoundary.InvalidateTargetMeasure()` | 方法 | — | 刷新普通布局无法观测到的目标输入；配套事件 `TargetMeasureInvalidated` |
| `LookaheadTextBlock` | `TextBlock` | — | 纯文本的独立目标测量；目标测量暂不支持 `Inlines` |
| `LookaheadTextBlock.InvalidateTargetMeasure()` | 方法 | — | 外部依赖（字体集合等）变化后刷新文本目标测量 |
| `LookaheadPosition` | `Decorator`（`sealed`） | — | 仅放置动画（位置）；布局保留目标槽位，真实 `Bounds` 走轨迹；不支持 `Padding` |
| `LookaheadPosition.AnimationSpec` | `IFiniteAnimationSpec<Point>` | medium-low spring | 位置动画规格 |
| `LookaheadSize` | `Decorator`（`sealed`） | — | 尺寸动画；与 `LookaheadPosition` 组合；不支持 `Padding` |
| `LookaheadSize.TargetSize` | `Size?` | `null` | 可选的目标框；`null` 时目标由协作子级决定 |
| `LookaheadSize.AnimationSpec` | `IFiniteAnimationSpec<Size>` | medium-low spring | 尺寸动画规格 |

### 查询与门面

`static class Lookahead` 提供不暴露内部协调器的查询与失效入口（查询永不触发布局，只返回已提交的目标数据）：

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Lookahead.TargetLayoutUpdatedEvent` | 路由事件 | — | Direct 策略；每次完整目标代提交后在每个协作参与者上触发，配套 `AddTargetLayoutUpdatedHandler` / `RemoveTargetLayoutUpdatedHandler` |
| `Lookahead.GetSnapshot(Control)` | 方法 → `LookaheadLayoutSnapshot?` | — | 参与者所在域当前已提交的快照；目标脏、未提交或非成员时返回 `null` |
| `Lookahead.TargetFor(Control)` | 方法 → `LookaheadLayoutResult?` | — | 该参与者已提交的目标结果，不可用时为 `null` |
| `Lookahead.InvalidateTargetLayout(Control)` | 方法 | — | 使所在域的完整目标布局失效 |
| `Lookahead.InvalidateApproachMeasure(Control)` / `InvalidateApproachArrange(Control)` | 方法 | — | 仅使实际测量/放置失效，不丢弃已提交的目标快照 |
| `Lookahead.MoveContent(Control, Action)` | 方法 | — | 在同一域内同步跨父级移动内容，并保留在途动画（见下文） |

`LookaheadLayoutResult` 为 record：`(Size MeasureConstraints, Size MeasuredSize, Size DesiredSize, Rect Bounds)`，另有 `FullMeasureConstraints` 与 `LayoutData`；`LookaheadLayoutSnapshot` 提供 `Version`、`Viewport`、`Entry`、`CoordinateReference` 与 `Results`。

`LookaheadRoot : Decorator`（`sealed`）是显式有限视口域入口（兼容入口）：`Snapshot`、`TargetLayoutUpdated` 事件、`static TargetFor(Control)`、`InvalidateTargetLayout()`、`InvalidateApproachPlacement()`、`MoveContent(...)`。

## 常见模式与进阶用法

以下模式可结合 `Catalog/Demos/Bounds` 中的演示和 `ComposeAnimation.Test` 中的 `AnimatedBounds*Test.cs` 等测试阅读。

### `IntermediateLayout` 组合顺序

`Then` 由外向内组合；同一批工厂换个顺序，语义完全不同：

```csharp
// 外框 200×160，内容层再扣除 20 的 padding → 内容目标 160×120，并整体偏移 (50, 20)
Card.IntermediateLayout = BoundsLayout.FixedSize(new Size(200, 160))
    .Then(BoundsLayout.Padding(new Thickness(20)))
    .Then(BoundsLayout.Offset(new Vector(50, 20)));
```

对照 `BoundsLayoutTest`：`FixedSize(120,100).Then(Padding(10))` 外层报 120×100、内容收到 `Fixed(100,80)`；而 `Padding(10).Then(FixedSize(120,100))` 外层报 140×120、内容收到 `Fixed(120,100)`。`Offset` 为物理 DIP，只影响放置、不改变报告尺寸。

需要按测量结果动态对齐时，用 `Placement` 回调（示例取自演示工程的 `PlacementAlignmentLayout`）：

```csharp
private sealed class BottomRightLayout(Size childSize) : BoundsLayout
{
    public override BoundsLayoutResult Measure(LookaheadConstraints constraints,
        Func<LookaheadConstraints, Size> measureChild)
    {
        // 恰好调用 measureChild 一次；返回有限、受约束的尺寸
        var child = measureChild(constraints.Loosen().Constrain(LookaheadConstraints.Fixed(childSize)));
        return new BoundsLayoutResult(constraints.Constrain(child), default)
        {
            // 纯同步放置回调：只使用测量期捕获的不可变数据
            Placement = context => new Point(
                context.Size.Width - context.ChildSize.Width,
                context.Size.Height - context.ChildSize.Height)
        };
    }
}
```

`BoundsPlacementContext.Origin` 是该层当前的根相对原点；`IsLookingAhead` 区分目标阶段与实际阶段。内层矩形动画会补偿中间层的放置，使内容始终落在自己的矩形轨迹上。

### 用 `LookaheadBoundary` 包裹普通内容

`AnimatedBounds` 的直接子级必须支持完整约束协议。普通控件（`Grid`、`TextBox`、自定义 `UserControl` 等）用 `LookaheadBoundary` 包裹后，内容始终以**目标约束**做原生布局，动画只改变外框位置与裁剪，不随中间尺寸重排：

```xml
<ca:AnimatedBounds x:Name="BoundaryCard" TargetSize="160,80"
                   HorizontalAlignment="Left" VerticalAlignment="Top">
    <ca:LookaheadBoundary Padding="16" ContentAlignment="TopStart">
        <Grid RowDefinitions="Auto,*">
            <TextBlock Text="标题" FontWeight="SemiBold" />
            <TextBox Grid.Row="1" AcceptsReturn="True" TextWrapping="Wrap"
                     Text="普通 Grid 与 TextBox 保持目标布局；外框展开时它们不随中间尺寸重排。" />
        </Grid>
    </ca:LookaheadBoundary>
</ca:AnimatedBounds>
```

外部依赖（如字体集合变更）导致文本目标测量过期时，调用 `InvalidateTargetMeasure()` 刷新。

### 运动参照系：`LookaheadScrollViewport` 与 `AnimateMotionFrameOfReference`

`LookaheadScrollViewport` 把 `-Offset` 作为运动参照系贡献给后代。`AnimatedBounds.AnimateMotionFrameOfReference` 控制滚动位移与动画目标的关系（演示工程中有左右对照）：

```xml
<ca:LookaheadScrollViewport x:Name="Viewport">
    <ca:LookaheadBox Height="700">
        <!-- false（默认）：滚动立即带动内容，不重定向动画；布局自身的变化仍然动画 -->
        <!-- true：滚动并入动画目标，矩形朝新的参照系位置追赶 -->
        <ca:AnimatedBounds x:Name="Card" TargetSize="150,180" Margin="0,100,0,0"
                           AnimateMotionFrameOfReference="True"
                           HorizontalAlignment="Left" VerticalAlignment="Top">
            <ca:LookaheadTextBlock TextWrapping="Wrap"
                                   Text="滚动会改变这个动画的目标；再次滚动可以看到重定向。" />
        </ca:AnimatedBounds>
    </ca:LookaheadBox>
</ca:LookaheadScrollViewport>
```

```csharp
Viewport.Offset += new Vector(0, 60); // 强制夹取到 [0, Extent - Viewport]
```

`ReferenceElement` 可选择同域的参与式祖先作为坐标参考。把参考设为滚动内容本身后，参照系之外的滚动同样立即跟随；修改参考会重置当前动画。

### `Lookahead.MoveContent`：跨父级移动且保留在途动画

`Lookahead.MoveContent` 在同一域内同步地把**同一个控件实例**移动到另一个父级，保留矩形动画的当前值、四条边的速度与时间线，再从新目标重定向：

```csharp
Lookahead.MoveContent(MoveCard, () =>
{
    from.Children.Remove(MoveCard);
    to.Children.Add(MoveCard);
    // 也可以在同一个事务里重定向
    MoveCard.TargetSize = new Size(140, 120);
});
```

移动必须同步完成，且留在同一个已附加的域、窗口与坐标参考祖先下；不允许 `await`/`async void`、推进时钟、运行布局或嵌套移动事务。移动回调失败会取消保留状态并安排布局，但**不回滚树**。普通的 `Remove`/`Add`（不走 `MoveContent`）在同一轮内重挂、且运动参考基准不变时，会在下一次 Scope 排空时恢复动画；否则（不再挂回、换了坐标参考祖先或运动帧）在该次排空释放。显式域入口本身不能用这种方式跨域移动。

### `LookaheadPosition` / `LookaheadSize`：轻量组合

只需要位置或尺寸其中之一时，可以用两个轻量控件代替完整的 `AnimatedBounds`：

```xml
<ca:LookaheadStackPanel x:Name="Stack" Spacing="10">
    <ca:LookaheadPosition>
        <Border Width="40" Height="40" Background="Tomato" />
    </ca:LookaheadPosition>
    <ca:LookaheadPosition>
        <Border Width="40" Height="40" Background="SeaGreen" />
    </ca:LookaheadPosition>
</ca:LookaheadStackPanel>
```

```csharp
Stack.Children.Move(0, 1); // 重排后两个子级平滑交换位置（布局槽位立即更新，真实 Bounds 走轨迹）
```

`LookaheadSize` 动画尺寸，与 `LookaheadPosition` 组合得到移动 + 缩放的内容（`LookaheadCompositionTest` 中两种嵌套顺序都受支持）：

```xml
<ca:LookaheadPosition>
    <ca:LookaheadSize x:Name="Resizable" TargetSize="140,180">
        <ca:LookaheadTextBlock TextWrapping="Wrap" Text="尺寸按当前动画框实时重测这段文本。" />
    </ca:LookaheadSize>
</ca:LookaheadPosition>
```

```csharp
Resizable.TargetSize = new Size(300, 100); // 目标尺寸变化触发尺寸动画
```

两者都可用 `AnimationSpec` 替换动画规格，且都不支持 `Padding`（需要留白时请给子级设置 `Margin`）。

### 自定义 `BoundsTransform.Create`：弧线路径

`BoundsTransform.Create` 收到完整的起止矩形（坐标基准为 `ReferenceElement` 坐标系），可以据此选择与几何相关的路径。例如换成 `ArcAnimationSpec`，让两个角沿椭圆弧插值：

```csharp
Card.BoundsTransform = new BoundsTransform
{
    Spec = new ArcAnimationSpec<Rect>(ArcMode.ArcAbove, TimeSpan.FromMilliseconds(800),
        easing: new LinearEasing())
};
```

也可以派生 `BoundsTransform`，按起止矩形动态构造规格（例如带绝对路径点的 `KeyframesSpec<Rect>`）：

```csharp
private sealed class ArcTransform : BoundsTransform
{
    public override IFiniteAnimationSpec<Rect> Create(Rect initialBounds, Rect targetBounds)
        => new ArcAnimationSpec<Rect>(ArcMode.ArcAbove, TimeSpan.FromMilliseconds(800),
            easing: new LinearEasing());
}
```

替换 `BoundsTransform` 只影响下一次目标变更，不会给已在运行的动画重新计时。`ArcAnimationSpec` 等动画规格属于 `ComposeAnimation.Runtime`，详见[动画规格（Runtime）](../runtime/animation-specs.md)。

### 读取目标快照并叠加目标轮廓

`Lookahead.GetSnapshot` 是纯查询；配合 `TargetLayoutUpdated` 事件可以绘制「目标轮廓」之类的辅助可视化（演示工程用它绘制绿色目标框）：

```csharp
Lookahead.AddTargetLayoutUpdatedHandler(Root, (_, _) => UpdateOutline());
Root.LayoutUpdated += (_, _) => UpdateOutline();

void UpdateOutline()
{
    if (Lookahead.GetSnapshot(Root) is not { } snapshot ||
        !snapshot.Results.TryGetValue(Card, out var target)) return;
    var position = LookaheadCoordinates.TargetPosition(Card, Root);
    Canvas.SetLeft(Outline, position.X);
    Canvas.SetTop(Outline, position.Y);
    Outline.Width = target.Bounds.Width;
    Outline.Height = target.Bounds.Height;
}
```

快照是不可变的：一次目标提交后 `Version` 递增，目标失效期间查询返回 `null`。

## 注意事项

- **实验性**：整套协作式 lookahead API 可能变化；升级时请核对变更日志。
- **子内容协议**：`AnimatedBounds` 的直接 `Child` 必须支持完整约束协议（`LookaheadTextBlock`、`LookaheadBoundary`、`LookaheadSize`/`LookaheadPosition`、嵌套 `AnimatedBounds` 等协作控件）；普通控件请用 `LookaheadBoundary` 包裹。具有显式 `Width`/`Height` 的普通控件也可作为不透明叶子被协作容器测量，但其子树不参与预测。
- **`AnimatedSize` 模式**要求同域的参与式放置父级（`ILookaheadPlacementHost`）；普通父级下使用会抛出 `NotSupportedException`。`TargetSize` 模式（默认）对父级没有额外要求。
- **`BoundsLayout` 契约**：`Measure` 必须纯同步地调用 `measureChild` **恰好一次**，返回有限且落在传入约束内的尺寸与有限偏移；漏测、重复测量或在策略返回后再调用测量回调都会被拒绝。`Placement` 回调同样是纯同步的：不得测量/排列控件、不得读取实时 `Bounds`，只应使用测量期捕获的不可变数据。策略实例应当不可变；输入变化时请替换 `IntermediateLayout` 实例。
- **不裁剪、不改写属性**：`AnimatedBounds` 默认不裁剪内容（`LookaheadBoundary` 默认裁剪），也不改写 `Width`/`Height`/`RenderTransform`；文字换行属于离散变化，不是逐帧连续插值。
- **滚动适配**：`LookaheadScrollViewport` 不是 `ScrollViewer` 模板适配器——没有滚动条、惯性、`BringIntoView`、虚拟化与锚定；它要求有限视口，内容在可滚动轴上按无限测量。
- **错误恢复**：动画规格失败或非有限采样会通过根 Scope 的 `AnimationFailed` 报告并把该动画落到目标；结构性错误（中间策略、真实测量/放置失败）停止相关动画并传播异常。`ResetAnimation()` 取消内外两个边界的动画，下次布局直接在目标处初始化。
- **失效入口的选择**：外部输入变化用 `Lookahead.InvalidateTargetLayout`（重建目标）；只是实际放置过期用 `Lookahead.InvalidateApproachMeasure`/`InvalidateApproachArrange`（保留已提交目标）。
- **`LookaheadPosition`/`LookaheadSize`/`LookaheadScrollViewport` 不支持 `Padding`**，请改用子级 `Margin`。

## 延伸阅读

- [协作式 Lookahead 实验概览](../../draft/LOOKAHEAD_EXPERIMENT.md)
- [AnimatedBounds 设计与行为参考](../../draft/ANIMATED_BOUNDS.md)
- [Lookahead 布局协议参考](../../draft/LOOKAHEAD_PROTOCOL.md)
- [自定义协作式 Lookahead 容器](../advanced/custom-lookahead-containers.md)
- [动画规格（Runtime）](../runtime/animation-specs.md)
- 可运行示例：`Catalog/Demos/Bounds`（`dotnet run --project Catalog/Catalog.csproj -- --demo bounds/basic`）
