# AnimatedContent

`AnimatedContent` 是把 Jetpack Compose 的内容切换动画移植到 Avalonia 的容器（`ContentControl` 子类）。`Content` 就是目标状态：每次赋新值时，旧内容进入退出槽位、新内容进入进入槽位，两者**重叠呈现**，直到退出侧动画结束才移除旧槽位。每个存活槽位都冻结了创建时的内容值与 `ContentTemplate`。

- **何时用**：页面/步骤切换、计数器、标签页内容、按状态换视图等"内容从 A 变成 B"的场景。
- **与 `AnimatedVisibility` 的区别**：[AnimatedVisibility](./animated-visibility.md) 控制同一份内容的出现/消失；`AnimatedContent` 在新旧两份内容之间过渡。
- **与 `Crossfade` 的关系**：`Crossfade` 是 `AnimatedContent` 的预设子类——同时淡入淡出、无容器尺寸动画，只暴露一个 `Spec` 属性。
- **与 `AnimatedContentSize` 的区别**：[AnimatedContentSize](./animated-content-size.md) 不换内容，只在内容测量尺寸变化时动画自身尺寸；`AnimatedContent` 的 `SizeTransform` 动画的是新旧内容完整尺寸之间的容器尺寸。

控件位于 `ComposeAnimation.Avalonia` 程序集的 `ComposeAnimation` 命名空间，XAML 中使用前缀 `ca:` 与 xmlns `https://github.com/Shirasagi0012/ComposeAnimation`。

## 快速开始

最小示例：点击按钮更换 `Content`，使用默认的 fade-through 过渡（90ms 淡出 + 延迟 90ms 的 220ms 淡入与 0.92 缩放，外加容器尺寸弹簧动画）：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="AnimatedContentDemo.MainWindow"
        Title="AnimatedContent" Width="640" Height="480">
    <StackPanel Margin="16" Spacing="16">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Content="Foo" Click="ShowFoo" />
            <Button Content="Bar" Click="ShowBar" />
        </StackPanel>
        <ca:AnimatedContent x:Name="Pages" HorizontalAlignment="Left" VerticalAlignment="Top">
            <Border Width="200" Height="120" Background="#FFDB00" />
        </ca:AnimatedContent>
    </StackPanel>
</Window>
```

```csharp
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void ShowFoo(object? sender, RoutedEventArgs e)
        => Pages.Content = new Border { Width = 200, Height = 120, Background = Brush.Parse("#FFDB00") };
    private void ShowBar(object? sender, RoutedEventArgs e)
        => Pages.Content = new Border { Width = 80, Height = 80, Background = Brush.Parse("#FF8100") };
}
```

`Content` 也可以是普通数据对象，配合 `ContentTemplate` 渲染（每个存活槽位独立实例化模板）。初始内容不播进入动画；原生 `IsVisible=false` 会立即丢弃所有槽位。

## API 要点

### AnimatedContent

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Content` | `object?`（继承自 `ContentControl`） | `null` | 目标状态，赋值即触发过渡 |
| `ContentTransform` | `ContentTransform?` | `null`（回落到 fade-through） | 固定的过渡定义 |
| `TransitionSpec` | `Func<IAnimatedContentTransitionScope, ContentTransform>?` | `null` | 仅 C#；按初始/目标状态动态决定过渡，优先于 `ContentTransform` |
| `ContentKeySelector` | `Func<object?, object?>?` | `null` | 仅 C#；自定义状态相等键，键相同则不触发过渡 |
| `ContentAlignment` | `VisibilityAlignment` | `TopStart` | 尺寸框与槽位内容在视口内的对齐 |
| `Source` | `ContentSource?` | `null` | 外部内容状态源；设置后优先于 `Content` |
| `Label` | `string?` | `null` | 诊断标签 |
| `TargetContent` | `object?`（只读） | — | 当前目标内容 |
| `IsIdle` | `bool`（只读） | — | 无进行中的过渡且只剩至多一个槽位 |
| `VisibleSlotCount` | `int`（只读） | — | 当前存活的槽位数 |
| `TargetScope` | `AnimatedVisibilityScope?`（只读） | — | 目标槽位的作用域 |
| `SlotCreated` | `EventHandler<ContentSlotEventArgs>` | — | 每次创建槽位时触发，参数含 `Scope` 与 `Content` |

### ContentTransform 与 SizeTransform

`ContentTransform`（sealed）描述一次切换的进入/退出/尺寸/Z 轴行为：

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| 构造函数 | `ContentTransform()`、`(EnterTransition, ExitTransition, float targetContentZIndex = 0)`、`(Enter, Exit, float, SizeTransform?)` | — | 省略 `SizeTransform` 时使用 `SizeTransform.CreateDefault()`；显式传 `null` 则禁用容器尺寸动画 |
| `Enter` / `Exit` | `EnterTransition` / `ExitTransition` | `None` | 进入/退出过渡，不允许赋 `null` |
| `TargetContentZIndex` | `float` | `0` | 目标内容的层叠顺序（Avalonia 侧取整为 `ZIndex`） |
| `SizeTransform` | `SizeTransform?` | `CreateDefault()` | 容器尺寸动画；`null` 禁用 |
| `Using(SizeTransform?)` | 方法 | — | 返回替换尺寸动画后的**副本**，不改原实例 |
| `CreateFadeThrough()` | 静态方法 | — | 默认 fade-through：220ms 淡入+0.92 缩放（带 90ms 延迟）/ 90ms 淡出 |
| `enter.TogetherWith(exit)` | 扩展方法（`ContentTransformExtensions`） | — | 由进入+退出构建 `ContentTransform` |

`SizeTransform`（sealed）：`Clip`（默认 `true`，尺寸动画期间裁剪到容器）、`Spec`、`CreateSpec`（`Func<Size, Size, IFiniteAnimationSpec<Size>>?`，按起止尺寸动态选 spec，优先于 `Spec`）、`CreateDefault()`。默认 spec 是裁剪的中低刚度弹簧（`SpringSpec<Size>(stiffness: Spring.StiffnessMediumLow)`）。

### Crossfade

`Crossfade : AnimatedContent`：新旧内容同时淡入淡出，无容器尺寸动画（尺寸取存活槽位最大值，退出槽位移除后一步到位）。在其上设置 `ContentTransform`/`TransitionSpec` **无效**。

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Spec` | `IFiniteAnimationSpec<double>` | `new TweenSpec<double>()` | 淡入淡出共用的 spec |

工厂方法 `CrossfadeTransforms.CreateTransform(spec)` 可在自定义场景复用同一变换。

### IAnimatedContentTransitionScope

`TransitionSpec` 收到的参数，描述当前切换段：`InitialState`/`TargetState`、`IsTransitioningTo(initial, target)`、`ContentAlignment`、`CurrentSize`/`TargetContentSize`、`FlowDirection`，以及按容器边缘计算偏移的 `SlideIntoContainer(ContentSlideDirection, spec, offset)` / `SlideOutOfContainer(...)`（`ContentSlideDirection` 取值 `Left`/`Right`/`Up`/`Down`/`Start`/`End`，`Start`/`End` 随 `FlowDirection`）。

`ContentSource` 静态工厂：`From(MutableTransitionState<object?>)`、`From<T>(MutableTransitionState<T>, Func<T, object?>?)`、`From<T>(Transition<T>, Func<T, object?>)`、`From<T>(Transition<T>)`。

## 常见模式/进阶用法

### Crossfade：同时淡入淡出

取自 demo 的 Alpha/Beta 切换：

```xml
<ca:Crossfade x:Name="Fader" HorizontalAlignment="Left">
    <TextBlock FontSize="24" Text="Alpha" />
</ca:Crossfade>
```

```csharp
Fader.Content = new TextBlock { FontSize = 24, Text = "Beta" };
// 可选：调整时长（默认 TweenSpec 300ms / FastOutSlowIn）
Fader.Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(500));
```

### 方向感知过渡：计数器

`TransitionSpec` 根据初始/目标状态决定滑动方向：

```xml
<ca:AnimatedContent x:Name="Counter" HorizontalAlignment="Left" Background="Transparent">
    <ca:AnimatedContent.ContentTemplate>
        <DataTemplate x:DataType="x:Int32">
            <TextBlock Text="{Binding}" FontSize="28" />
        </DataTemplate>
    </ca:AnimatedContent.ContentTemplate>
</ca:AnimatedContent>
```

```csharp
Counter.Content = 0;
Counter.TransitionSpec = scope =>
{
    var up = scope.TargetState is int to && scope.InitialState is int from && to > from;
    return (new SlideInVertically { InitialOffsetRatio = up ? 1 : -1 }
            + new FadeIn())
        .TogetherWith(new SlideOutVertically { TargetOffsetRatio = up ? -1 : 1 } + new FadeOut())
        .Using(new SizeTransform { Clip = true });
};

// 按钮里：Counter.Content = ++_count; / Counter.Content = --_count;
```

需要"相对容器边缘滑入滑出"（新旧内容尺寸不一致也对齐到容器边）时，用作用域自带的工厂方法（取自 `ComposeAnimation.Test/AnimatedContentTest.cs`）：

```csharp
host.TransitionSpec = scope =>
    scope.SlideIntoContainer(ContentSlideDirection.Left, new TweenSpec<Vector>(TimeSpan.FromMilliseconds(200)))
        .TogetherWith(scope.SlideOutOfContainer(ContentSlideDirection.Left, new TweenSpec<Vector>(TimeSpan.FromMilliseconds(200))))
        .Using(null); // null = 禁用容器尺寸动画
```

### ContentKeySelector：按键判定"是否同一内容"

默认用内容本身做相等键。当值变化但**不希望**触发过渡（如同一实体的属性更新）时，让选择器返回相同的键——新值会在原槽位原地更新，无过渡；反之值相等却想强制过渡时返回不同的键（取自 `AnimatedContentTest.cs` 的 `Keyed` 用例）：

```csharp
private sealed record Keyed(int Id, int Width);

host.ContentKeySelector = s => s is Keyed k ? $"id:{k.Id}" : s;
host.Content = new Keyed(1, 100);
// 键相同（id:1）：同一槽位原地更新为宽 40，不播过渡
host.Content = new Keyed(1, 40);
```

### 进阶：槽位作用域动画、状态展示与慢动作调试

`SlotCreated` 给出每个槽位的 `AnimatedVisibilityScope`（与 `AnimatedVisibility.Scope` 同一类型），可注册随槽位显隐状态变化的自定义动画：

```csharp
host.SlotCreated += (_, e) =>
{
    if (e.Content is Border card)
    {
        var radius = e.Scope.Transition.AnimateCornerRadius(
            s => new CornerRadius(s == EnterExitState.Visible ? 12 : 36),
            _ => new TweenSpec<CornerRadius>(TimeSpan.FromMilliseconds(400)));
        e.Scope.Bind(card, Border.CornerRadiusProperty, radius);
    }
};
```

用只读属性观察运行状态（取自 demo 的状态栏）：

```csharp
Pages.PropertyChanged += (_, e) =>
{
    if (e.Property == AnimatedContent.IsIdleProperty || e.Property == AnimatedContent.VisibleSlotCountProperty)
        Status.Text = $"idle={Pages.IsIdle}  slots={Pages.VisibleSlotCount}";
};
```

调试技巧——慢动作（取自 demo，全局 2 倍时长，或对单个宿主的目标作用域设置）：

```csharp
MotionDurationScale.ScaleFactor = 2;                 // 全局，需在连接前设置
if (host.TargetScope is { } scope)
    scope.Transition.DurationScale = 2;              // 单个宿主
```

## 注意事项

- **动画规格默认值**：`FadeIn`/`SlideInVertically` 等过渡的 `Spec` 默认是 `new SpringSpec<T>(stiffness: 400)`（`Spring.StiffnessMediumLow`，无弹性），弹簧时长由物理参数决定；`TweenSpec<T>` 默认 `Duration` 300ms、`Easing` FastOutSlowIn、`Delay` 0。`SizeTransform` 省略时的默认尺寸弹簧同为 stiffness 400 且 `Clip=true`；但**直接写 `<ca:SizeSpringSpec />` 得到的 `SpringSpec` 默认 stiffness 是 1500**（`Spring.StiffnessMedium`），需要低速弹簧请显式指定 `Stiffness="400"`。
- **模板契约**：不要替换默认模板。自定义模板必须包含名为 `PART_ContentPanel` 的 `Border`（其子级为 `ContentSlotPanel`），且**不得**包含 `PART_ContentPresenter`——内容在槽位中呈现，违反任一条都会抛 `InvalidOperationException`。
- **`SizeTransform = null` 禁用容器尺寸动画**：过渡期间容器尺寸取存活槽位的最大尺寸，退出槽位移除后瞬间变为目标尺寸；省略 `SizeTransform` 才是默认的弹簧尺寸动画（`Using(null)` 与省略语义不同）。
- **内容对齐与裁剪**：`ContentAlignment`（默认 `TopStart`）决定尺寸框在视口内、以及各槽位内容在槽位内的对齐；`Start`/`End` 随 `FlowDirection`。尺寸动画期间按 `SizeTransform.Clip` 对 `PART_ContentPanel` 裁剪。背景/边框/Padding 跟随动画中的尺寸框而非拉伸的视口，需要整幅背景请包裹一层容器。
- **控件实例内容**：同一个 `Control` 实例不能同时出现在两个槽位中（例如在它仍在旧槽位退出时又以不同的键重新赋入），违反时抛 `InvalidOperationException`；键相同的中断/回切（A→B→A）会复用原槽位与动画。
- **`TransitionSpec` 优先于 `ContentTransform`**，都未设置时回落到 `ContentTransform.CreateFadeThrough()`；`TransitionSpec` 抛出的异常会经根 Scope 的 `AnimationFailed` 报告。
- **`ExitTransition.KeepUntilTransitionsFinished`**：把退出内容的移除推迟到整个 content transform（含尺寸动画）空闲之后；在 `AnimatedVisibility` 上无效。
- `TargetContentZIndex` 是 `float`（对齐 Compose），Avalonia 的 `ZIndex` 取整后相同的整数按子级顺序排，目标内容总在最上层。
- `ContentSource.From(MutableTransitionState<T>)` 的状态实例只能驱动一个活动宿主；共享外部 `Transition<T>` 时宿主不启动父级动画循环，父级 `RunAsync` 由调用方负责。

## 延伸阅读

- [AnimatedVisibility](./animated-visibility.md)：单内容的显隐动画（过渡目录、`AnimatedVisibilityScope` 与本页通用）
- [AnimatedContentSize](./animated-content-size.md)：内容尺寸变化时的自身尺寸动画
- [共享元素过渡](./shared-transition.md)：与外部 `Transition` 共享状态
- [使用文档入口](../README.md)
- [旧版设计文档 ANIMATED_CONTENT.md](../../draft/ANIMATED_CONTENT.md)（更偏实现细节，仍有效）
