# AnimatedVisibility

`AnimatedVisibility` 是把 Jetpack Compose 的显隐动画模型移植到 Avalonia 的内容容器（`ContentControl` 子类）。绑定它的 `Visible` 属性，内容在隐藏时会播放退出过渡，并且**直到作用域内所有动画结束才真正从视觉树移除**；反向切换可以无缝打断并回放。

- **何时用**：单个控件或面板的展开/收起、提示条、详情区、按条件显示的卡片等"出现/消失"场景。
- **与原生 `IsVisible` 的区别**：原生 `IsVisible=false` 立即隐藏，没有动画；`Visible` 是动画显隐的目标状态。二者不要同时绑定到同一个目标。
- **与 `AnimatedContent` 的区别**：`AnimatedVisibility` 控制**同一份内容**的显隐；[AnimatedContent](./animated-content.md) 在 `Content` 变化时做新旧内容的交叉过渡。
- **与 `AnimateEnterExit` 的关系**：`AnimateEnterExit` 是放在 `AnimatedVisibility` 内容内部的子级效果装饰器，让子元素拥有独立的进入/退出时序，并参与宿主退出完成的判定。

控件位于 `ComposeAnimation.Avalonia` 程序集的 `ComposeAnimation` 命名空间，XAML 中使用前缀 `ca:` 与 xmlns `https://github.com/Shirasagi0012/ComposeAnimation`。

## 快速开始

最小示例：`ToggleSwitch` 绑定 `Visible`，使用默认过渡（进入 `FadeIn + ExpandIn`，退出 `FadeOut + ShrinkOut`）：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="AnimatedVisibilityDemo.MainWindow"
        Title="AnimatedVisibility" Width="800" Height="520">
    <StackPanel Spacing="16" VerticalAlignment="Center">
        <ToggleSwitch x:Name="VisibilityToggle" IsChecked="True"
                      Content="显示详情" HorizontalAlignment="Center" />
        <ca:AnimatedVisibility Visible="{Binding #VisibilityToggle.IsChecked}"
                               HorizontalAlignment="Center" Label="Details">
            <Border Width="420" Background="#253D67" CornerRadius="12" Padding="24">
                <TextBlock Text="退出动画全部结束前内容保持存活"
                           TextWrapping="Wrap" Foreground="White" />
            </Border>
        </ca:AnimatedVisibility>
    </StackPanel>
</Window>
```

初始 `Visible=true` 的内容**不播放进入动画**（立即呈现）；初始隐藏的内容不占布局空间，其 `ContentTemplate` 在首次显示前不会被实例化。可运行示例见 `Catalog/Demos/Visibility`。

## API 要点

### AnimatedVisibility

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Visible` | `bool` | `true` | 动画显隐的目标状态，绑定它做显隐 |
| `Source` | `VisibilitySource?` | `null` | 外部显隐状态源；设置后优先于 `Visible` |
| `Enter` | `EnterTransition` | `new FadeIn() + new ExpandIn()` | 进入过渡声明 |
| `Exit` | `ExitTransition` | `new FadeOut() + new ShrinkOut()` | 退出过渡声明 |
| `Label` | `string?` | `null` | 动画诊断标签 |
| `TargetVisible` | `bool`（只读） | — | 当前解析出的目标显隐（跟随 `Source`/`Visible`） |
| `IsIdle` | `bool`（只读） | — | 作用域内没有进行中或待开始的动画 |
| `IsContentPresent` | `bool`（只读） | — | 内容是否仍在视觉树中（含正在退出的内容） |
| `Scope` | `AnimatedVisibilityScope?`（只读） | — | 当前呈现的作用域，无呈现时为 `null` |
| `ScopeCreated` | `EventHandler<VisibilityScopeEventArgs>` | — | 每次呈现创建作用域时触发一次，参数含 `Scope` |

### 过渡目录

`EnterTransition`/`ExitTransition` 是抽象基类。`operator +` 把多个过渡组合为 `EnterTransitionGroup`/`ExitTransitionGroup`（XAML 中以 `[Content]` 列表声明子项）；**不同类效果并行播放，同类效果后写的覆盖先写的**。`EnterTransition.None`/`ExitTransition.None` 表示无过渡；`ExitTransition.KeepUntilTransitionsFinished` 只服务于 `AnimatedContent`，在 `AnimatedVisibility` 上被忽略。

| 进入 / 退出 | 常用属性（默认值） | `Spec` 类型 |
| --- | --- | --- |
| `FadeIn` / `FadeOut` | `InitialAlpha` / `TargetAlpha` = 0 | `IFiniteAnimationSpec<double>` |
| `ScaleIn` / `ScaleOut` | `InitialScale` / `TargetScale` = 0，`TransformOrigin` = Center | `IFiniteAnimationSpec<double>` |
| `SlideIn` / `SlideOut` | `InitialOffset` / `TargetOffset`（`Vector`）、`InitialOffsetRatio` / `TargetOffsetRatio`（`Vector`）、`InitialOffsetSelector` / `TargetOffsetSelector`（`Func<Size, Vector>?`） | `IFiniteAnimationSpec<Vector>` |
| `SlideInHorizontally` / `SlideInVertically`、`SlideOutHorizontally` / `SlideOutVertically` | `InitialOffset` / `TargetOffset`、偏移比例 = -0.5、`...OffsetSelector`（`Func<double, double>?`） | `IFiniteAnimationSpec<Vector>` |
| `ExpandIn` / `ShrinkOut` | `InitialSize` / `TargetSize`、`...SizeRatio`、`...SizeSelector`、`ExpandFrom` / `ShrinkTowards` = `VisibilityAlignment.BottomEnd`、`Clip` = true | `IFiniteAnimationSpec<Size>` |
| `ExpandHorizontally` / `ShrinkHorizontally` | `InitialWidth` / `TargetWidth` 系列、`ExpandFrom` / `ShrinkTowards` = `VisibilityHorizontalAlignment.End`、`Clip` = true | `IFiniteAnimationSpec<Size>` |
| `ExpandVertically` / `ShrinkVertically` | `InitialHeight` / `TargetHeight` 系列、`ExpandFrom` / `ShrinkTowards` = `VerticalAlignment.Bottom`、`Clip` = true | `IFiniteAnimationSpec<Size>` |

偏移/尺寸的终点按 `绝对值 + 比例 × 内容完整尺寸` 计算；提供了 Selector 时以 Selector 为准。`ExpandIn`/`ShrinkOut` 的锚点是 `VisibilityAlignment` 九值枚举（`TopStart`…`BottomEnd`），水平方向锚点是 `VisibilityHorizontalAlignment`。效果内部按固定版本 Compose 的物理 LTR 坐标计算，`Start`/`End` 不再额外随 `FlowDirection` 翻转；普通内容布局仍遵循 Avalonia 的 FlowDirection。注意 `ExpandVertically`/`ShrinkVertically` 的锚点用的是 Avalonia 自带的 `VerticalAlignment`（不支持 `Stretch`）。

XAML 中的动画规格别名：`DoubleTweenSpec`、`VectorTweenSpec`、`SizeTweenSpec`、`SizeSpringSpec` 等（`Double`/`Float`/`Int32`/`Point`/`Size`/`Vector`/`Rect`/`Thickness`/`CornerRadius`/`Color` 各有一套 Tween/Spring 版本）。`TweenSpec<T>` 构造参数 `TweenSpec(TimeSpan? duration = null, TimeSpan? delay = null, Easing? easing = null)` 均可省略；`*SpringSpec` 暴露可写的 `DampingRatio`/`Stiffness`。

尺寸过渡需要父布局允许控件改变尺寸。在固定高度的容器中，将 `AnimatedVisibility.VerticalAlignment` 设为 `Top` 或 `Center`；默认 `Stretch` 可能让排列高度保持不变，纵向收缩看起来就没有效果。水平轴同理。这是控件的布局对齐，与过渡的 `ExpandFrom` / `ShrinkTowards` 锚点不同。

轴向滑动的默认比例 `-0.5` 与 Compose 一致，只移动半个宽／高。需要完整滑出时，显式设置 `InitialOffsetRatio` / `TargetOffsetRatio` 为 `-1` 或 `1`，并按场景给外层视口设置 `ClipToBounds`。`ExpandIn` 同时改变宽高；`ExpandHorizontally` 保持高度，`ExpandVertically` 保持宽度。

### AnimateEnterExit

`Decorator` 子类，参与最近的 `AnimatedVisibility`（或 `AnimatedContent` slot）的完成过程。

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Enter` | `EnterTransition` | `new FadeIn()` | 子级进入效果（与宿主效果叠加） |
| `Exit` | `ExitTransition` | `new FadeOut()` | 子级退出效果，会延长宿主退出的存活时间 |
| `Label` | `string?` | `null` | 诊断标签 |

### AnimatedVisibilityScope 与 VisibilitySource

`AnimatedVisibilityScope`（sealed）代表一次呈现的动画与绑定集合：

- `Transition`：`Transition<EnterExitState>`，本次呈现的动画树（`EnterExitState` 取值 `PreEnter`/`Visible`/`PostExit`）。宿主拥有其生命周期，**不要自行 Run 或 Dispose**。
- `BeginPreparation()`：在目标变化前预留本次呈现（共享过渡场景用）。
- `Bind<T>(AvaloniaObject, StyledProperty<T>, Transition<EnterExitState>.TransitionAnimationState<T>) -> IDisposable`：把属于该作用域的动画以 Animation 优先级投影到某个属性，不替换原有绑定。
- `Own(IDisposable)`：让某个资源随本次呈现结束一起释放。

`VisibilitySource`（abstract）用静态工厂创建：`VisibilitySource.From(MutableTransitionState<bool>)`、`VisibilitySource.From<T>(Transition<T>, Func<T, bool>)`。注意**一个 `MutableTransitionState<bool>` 只能驱动一个活动宿主**，多宿主共享状态请改用父 `Transition`。

## 常见模式/进阶用法

### 自定义进入/退出组合

XAML 中用 `EnterTransitionGroup`/`ExitTransitionGroup` 列表组合多个效果：从顶部展开进入、向顶部收起退出，退出淡出有自定义时长：

```xml
<ca:AnimatedVisibility Visible="{Binding #VisibilityToggle.IsChecked}">
    <ca:AnimatedVisibility.Enter>
        <ca:EnterTransitionGroup>
            <ca:FadeIn />
            <ca:ExpandVertically ExpandFrom="Top" />
        </ca:EnterTransitionGroup>
    </ca:AnimatedVisibility.Enter>
    <ca:AnimatedVisibility.Exit>
        <ca:ExitTransitionGroup>
            <ca:FadeOut>
                <ca:FadeOut.Spec><ca:DoubleTweenSpec Duration="0:0:0.35" /></ca:FadeOut.Spec>
            </ca:FadeOut>
            <ca:ShrinkVertically ShrinkTowards="Top" />
        </ca:ExitTransitionGroup>
    </ca:AnimatedVisibility.Exit>
    <!-- 内容 -->
</ca:AnimatedVisibility>
```

C# 等价写法（`operator +` 组合，`ExpandFrom`/`ShrinkTowards` 是 Avalonia 的 `VerticalAlignment`）：

```csharp
host.Enter = new FadeIn() + new ExpandVertically { ExpandFrom = VerticalAlignment.Top };
host.Exit = new FadeOut { Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(350)) }
    + new ShrinkVertically { ShrinkTowards = VerticalAlignment.Top };
```

### 子级独立时序：AnimateEnterExit

宿主内容里的某个子元素可以有自己的进入/退出效果，并且它的退出动画会延长宿主内容的存活时间（取自 demo 中按钮的滑出效果）：

```xml
<ca:AnimateEnterExit Label="Details action">
    <ca:AnimateEnterExit.Enter>
        <ca:SlideInHorizontally InitialOffsetRatio="0.3" />
    </ca:AnimateEnterExit.Enter>
    <ca:AnimateEnterExit.Exit>
        <ca:SlideOutHorizontally TargetOffsetRatio="1">
            <ca:SlideOutHorizontally.Spec>
                <ca:VectorTweenSpec Duration="0:0:0.5" />
            </ca:SlideOutHorizontally.Spec>
        </ca:SlideOutHorizontally>
    </ca:AnimateEnterExit.Exit>
    <Button Content="子级效果有自己的时序" HorizontalAlignment="Left" />
</ca:AnimateEnterExit>
```

### 读取显隐状态

`TargetVisible`/`IsContentPresent`/`IsIdle` 是可绑定的只读属性（取自 demo 的状态栏）：

```xml
<StackPanel Orientation="Horizontal" Spacing="16">
    <TextBlock Text="{Binding #VisibilityDemo.TargetVisible, StringFormat='Target: {0}'}" />
    <TextBlock Text="{Binding #VisibilityDemo.IsContentPresent, StringFormat='Present: {0}'}" />
    <TextBlock Text="{Binding #VisibilityDemo.IsIdle, StringFormat='Idle: {0}'}" />
</StackPanel>
```

### 进阶：作用域自定义动画（ScopeCreated + Bind）

`ScopeCreated` 在每次呈现创建作用域时触发（通常在 `InitializeComponent` 之后注册）。在回调里用 `Scope.Transition` 上的 `Animate*` 扩展方法创建随显隐状态变化的动画，再用 `Scope.Bind` 投影到内容控件的属性：

```csharp
public MainWindow()
{
    InitializeComponent();
    VisibilityDemo.ScopeCreated += (_, e) =>
    {
        var radius = e.Scope.Transition.AnimateCornerRadius(
            state => new CornerRadius(state == EnterExitState.Visible ? 12 : 36),
            _ => new TweenSpec<CornerRadius>(TimeSpan.FromMilliseconds(400)));
        e.Scope.Bind(VisibilityCard, Border.CornerRadiusProperty, radius);
    };
}
```

（需要 `using ComposeAnimation.Runtime;`。）这样注册的自定义动画会**延长退出过程的存活时间**——宿主会等它播完才移除内容；呈现结束后绑定自动释放，属性恢复到普通绑定/本地值（行为见 `ComposeAnimation.Test/AnimatedVisibilityTest.cs` 的 `CustomAnimation_ExtendsExit_AndReleasesAnimationBinding`）。需要随呈现一起释放的其它资源用 `Scope.Own(...)` 登记。

### 进阶：与外部 Transition 共享状态

`Source` 可以把显隐挂到显式状态或外部动画树上：

```csharp
// 显式可变状态（首次赋值也会播进入动画）
var state = new MutableTransitionState<bool>(false) { TargetState = true };
host.Source = VisibilitySource.From(state);

// 或共享一个外部 Transition，由映射函数决定何时可见
host.Source = VisibilitySource.From(pageTransition, page => page == Page.Details);
```

设置 `Source` 后 `Visible` 不再写回状态， resolved 目标从 `TargetVisible` 读取。共享父 `Transition` 是跨控件协调（如共享元素过渡）的基础，应用示例见[共享元素过渡](./shared-transition.md)。

## 注意事项

### Lookahead 布局

`AnimatedVisibility`、`AnimateEnterExit` 和默认呈现路径已实现协作式布局协议。它们加入祖先 Lookahead 域，不自动创建局部域；域外继续使用普通显隐动画。

```xml
<ca:LookaheadRoot>
    <ca:LookaheadStackPanel>
        <ca:AnimatedVisibility Visible="{Binding IsExpanded}">
            <ca:LookaheadBox Height="200">
                <!-- 协作式内容；原生子树需显式使用 LookaheadBoundary -->
            </ca:LookaheadBox>
        </ca:AnimatedVisibility>
        <ca:LookaheadTextBlock Text="Header" />
    </ca:LookaheadStackPanel>
</ca:LookaheadRoot>
```

收起时目标布局立即移除宿主的外层占位，实际布局继续展开/收缩动画；退出内容的完整目标几何仍保留到呈现结束。宿主自己的 Margin/最小尺寸不会留下目标占位，需要保留空间时在外部容器设置尺寸。原生内容需要 Boundary，不能通过把整个 AnimatedVisibility 包在 Boundary 中获得内部元素预测。

控件模板替换会重新连接呈现层，保留现有 Scope、通道和速度；替换 Content/ContentTemplate/Source 仍重建呈现。外部 `DeferredTransition` 的 pending 可见目标会提前准备内容；提交或取消仍使用同一状态映射。此支持不包含 MutableTransform/veil 等手动效果交接能力。

### 显隐与动画

- **绑定 `Visible` 才有动画**。原生 `IsVisible=false` 仍然立即隐藏并结束当前呈现；两者不要绑到同一个目标。
- **动画规格默认值**：所有内置过渡的 `Spec` 默认是 `new SpringSpec<T>(stiffness: 400)`（即 `Spring.StiffnessMediumLow`，`DampingRatioNoBouncy` 无弹性），弹簧没有固定时长，由物理参数决定；而 `TweenSpec<T>` 默认 `Duration` 300ms、`Easing` 为 FastOutSlowIn、`Delay` 为 0。想精确控制时长用 Tween，想物理感用 Spring。
- **组合规则**：不同类效果（淡入+展开）并行；同类效果后写的覆盖先写的（写两个 `FadeOut` 只有后一个生效）。过渡组不允许成环或含 `null` 子项。
- **XAML 中写 `None`**：用 `{x:Static ca:EnterTransition.None}` / `{x:Static ca:ExitTransition.None}`。
- **`AnimateEnterExit` 的默认效果是淡入/淡出，会与宿主的淡入淡出叠加**；子级不需要额外淡入时显式设为 `EnterTransition.None`。
- **`Scope.Bind` 请用 OneWay 呈现绑定**：TwoWay 绑定会把动画采样写回源属性。`Bind` 的动画必须属于该作用域的 `Transition`，否则抛 `ArgumentException`。
- **`MutableTransitionState<bool>` 单宿主限制**：一个状态实例同时只能驱动一个活动宿主，违反时抛 `InvalidOperationException`；多宿主共享请用 `VisibilitySource.From(Transition<T>, ...)`。
- **隐藏的宿主不报告尺寸**：内容不存活时 `DesiredSize` 为零（即使有 `Margin`/`MinHeight`），退出过程中布局尺寸随 `ShrinkOut`/`ShrinkVertically` 连续变化。
- `ExitTransition.KeepUntilTransitionsFinished` 在 `AnimatedVisibility` 上是 no-op，它是为 `AnimatedContent` 设计的。

## 延伸阅读

- [AnimatedContent](./animated-content.md)：`Content` 变化时的新旧内容过渡
- [共享元素过渡](./shared-transition.md)：跨控件共享动画状态
- [属性动画](./animated-properties.md)：`Animate*` 扩展方法与动画规格体系
- [使用文档入口](../README.md)
- [内部结构与适配决定](../../../ComposeAnimation.Avalonia/AnimatedVisibility/INTERNALS.md)
- [历史设计文档 ANIMATED_VISIBILITY.md](../../draft/ANIMATED_VISIBILITY.md)（部分结构与行为已过时）

## Scope 与换父 · Scope and reparenting

`AnimatedVisibility` 在 Scope 重建时恢复在途的进入/退出过渡（D17），不会重新起播；`ResetAnimation` 与脱离后未挂回（排空时释放）仍会重置。放在 `Flyout`/`ContextMenu`/`ToolTip` 中时以所有者控件的 Scope 为父（D15）。 详见 [AnimationScope](../animation-scope.md#d17恢复而不是重启)。
