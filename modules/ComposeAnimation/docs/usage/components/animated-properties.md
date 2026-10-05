# 属性动画：Animated 附加属性

`Animated` 是一组附加属性，对标 Jetpack Compose 的 `animate*AsState`：给任意 styled 属性附加一条动画声明，属性原本的值来源（本地值、样式、绑定）照常提供**目标值**，动画引擎负责把中间过程按帧采样，以 `BindingPriority.Animation` 优先级写入，不覆盖底层值。弹簧物理驱动、可中断，播放途中改变目标会从当前显示值和速度平滑重定向。

何时使用：某个具体属性（如 `Width`、`Opacity`、`CornerRadius`）需要在值变化时平滑过渡，而目标值仍由普通 Avalonia 机制（绑定、样式、代码赋值）决定。

与相近组件的区别：

- 动画目标是**内容测量尺寸**（而非显式属性值）时，用 [`AnimatedContentSize`](./animated-content-size.md)；
- 控制内容**进入/离开**的过渡用 [`AnimatedVisibility`](./animated-visibility.md)，**内容切换**用 [`AnimatedContent`](./animated-content.md)；
- 与 Avalonia 自带的 `Transition`/`Animation` 不同，本组件基于 Compose 弹簧物理、逐帧采样、运行中可重定向且保留速度，采样值写在动画优先级层而不是临时改写在本地值上。

可运行示例见 `Catalog/Demos/Properties/PropertiesAnimationDemo.axaml`。

## 快速开始

下面的例子给 `Border.Width` 挂上 `AnimatedDouble` 声明，按钮在 100 与 500 之间切换宽度；`TextBlock` 绑定读到的是逐帧采样的实时宽度。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="SimpleAnimation.MainWindow"
        Title="AnimatedProperty" Width="800" Height="450">
    <StackPanel Spacing="16" VerticalAlignment="Center">
        <Border Width="100" Height="100" Background="Aqua" x:Name="Border1">
            <ca:Animated.Properties>
                <ca:AnimatedPropertyCollection>
                    <ca:AnimatedDouble Property="{x:Static Border.WidthProperty}">
                        <ca:AnimatedDouble.Spec>
                            <ca:DoubleSpringSpec DampingRatio="0.5" Stiffness="10" />
                        </ca:AnimatedDouble.Spec>
                    </ca:AnimatedDouble>
                </ca:AnimatedPropertyCollection>
            </ca:Animated.Properties>
        </Border>

        <Button Click="Button_OnClick" HorizontalAlignment="Center">Change</Button>
        <TextBlock Text="{Binding #Border1.Width, StringFormat='Width: {0:F2}'}" HorizontalAlignment="Center" />
    </StackPanel>
</Window>
```

```csharp
// MainWindow.axaml.cs
private bool _flag = true;

private void Button_OnClick(object? sender, RoutedEventArgs e)
{
    Border1.Width = _flag ? 500 : 100;
    _flag = !_flag;
}
```

要点：`Width = _flag ? 500 : 100` 是普通的属性赋值，它只决定**目标值**；动画声明截获目标变化并驱动中间值。省略 `Spec` 时使用声明类型的默认弹簧。

## API 要点

### `Animated`（static class）

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Animated.Properties` | `AttachedProperty<AnimatedPropertyCollection?>` | `null` | 该可视元素的动画声明集合，XAML 中以 `<ca:Animated.Properties>` 设置 |
| `Animated.DurationScale` | `AttachedProperty<double>`（继承） | `1` | 播放时长乘数；0 下一帧吸附，2 半速；不改变根帧时间戳 |

常用方法：`GetProperties` / `SetProperties`、`SnapToTargetAsync<T>(Visual, StyledProperty<T>)`、`GetOrCreateProperties`，以及流式扩展 `TDeclaration Animate<TDeclaration>(this Visual, TDeclaration)`（把声明追加到集合，按需创建集合并返回声明本身）。

### 声明类型

抽象基类 `AnimatedProperty : AvaloniaObject`；泛型 `AnimatedProperty<T>` 增加 `Spec`。封闭具体类见下表。

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `AnimatedProperty.Property` | `AvaloniaProperty?` | `null` | 要动画的宿主属性。必须是非继承、非 Direct 的 styled 属性；XAML 里必须用 `{x:Static ...}` 赋值，裸名称不会被解析 |
| `AnimatedProperty<T>.Spec` | `IFiniteAnimationSpec<T>?` | `null` | 下一次目标变化使用的规格。为 `null` 时回退到带类型可见性阈值的默认弹簧（无回弹阻尼、`StiffnessMedium`）；改动不打断已在播放的运行 |

`AnimatedPropertyCollection : AvaloniaList<AnimatedProperty>` 是声明的集合，可通过样式/资源在多个控件间共享——播放状态按控件各自保存在内部协调器里，共享同一份声明不会串扰。

### 声明类型与 XAML 规格别名对照

| 值类型 | 声明类 | Tween 别名 | Spring 别名 |
| --- | --- | --- | --- |
| `double` | `AnimatedDouble` | `DoubleTweenSpec` | `DoubleSpringSpec` |
| `float` | `AnimatedFloat` | `FloatTweenSpec` | `FloatSpringSpec` |
| `int` | `AnimatedInt32` | `Int32TweenSpec` | `Int32SpringSpec` |
| `Point` | `AnimatedPoint` | `PointTweenSpec` | `PointSpringSpec` |
| `Size` | `AnimatedSize` | `SizeTweenSpec` | `SizeSpringSpec` |
| `Vector` | `AnimatedVector` | `VectorTweenSpec` | `VectorSpringSpec` |
| `Rect` | `AnimatedRect` | `RectTweenSpec` | `RectSpringSpec` |
| `Thickness` | `AnimatedThickness` | `ThicknessTweenSpec` | `ThicknessSpringSpec` |
| `CornerRadius` | `AnimatedCornerRadius` | `CornerRadiusTweenSpec` | `CornerRadiusSpringSpec` |
| `Color` | `AnimatedColor` | `ColorTweenSpec` | `ColorSpringSpec` |

`*TweenSpec` 可设 `Duration`（`TimeSpan`，默认 300 ms）、`Delay`（默认零）、`Easing`（默认 `FastOutSlowIn`）；`*SpringSpec` 可设 `DampingRatio`（默认 1.0，无回弹）与 `Stiffness`（默认 `Spring.StiffnessMedium` = 1500）。

## 常见模式与进阶用法

### C# 流式写法

`Animate` 扩展方法把声明追加到控件的集合（不存在则创建），适合代码后置或 C# 优先的界面：

```csharp
using ComposeAnimation;
using ComposeAnimation.Runtime;

border.Animate(new AnimatedDouble
{
    Property = Visual.OpacityProperty,
    Spec = new SpringSpec<double>(dampingRatio: 0.8, stiffness: 300)
});
```

之后任何对 `border.Opacity` 的普通赋值或绑定更新都会成为动画目标。

### 通过样式共享声明集合

同一份 `AnimatedPropertyCollection` 可以放进 `Style` 的 setter，应用于一类控件；每个控件拥有独立的播放会话：

```xml
<Window.Styles>
    <Style Selector="Border.fade">
        <Setter Property="(ca:Animated.Properties)">
            <ca:AnimatedPropertyCollection>
                <ca:AnimatedDouble Property="{x:Static Visual.OpacityProperty}">
                    <ca:AnimatedDouble.Spec>
                        <ca:DoubleTweenSpec Duration="0:0:0.1" />
                    </ca:AnimatedDouble.Spec>
                </ca:AnimatedDouble>
            </ca:AnimatedPropertyCollection>
        </Setter>
    </Style>
</Window.Styles>
```

两个 `Classes="fade"` 的 `Border` 共享同一个集合实例，但一个播放到一半时另一个不受影响。

### 根环境、调速与吸附

在窗口加载 XAML 或设置 Content 之前调用 `VisualAnimationScope.Initialize(window, myClock)`；省略 myClock 使用默认 UI 帧源。同根所有消费者自动共享按需请求，不支持运行中换钟。

`Animated.SetDurationScale(demo, 4)` 让该子树以 0.25× 播放，根时间戳保持原值。`await Animated.SnapToTargetAsync(border, Visual.OpacityProperty)` 立即结束当前属性动画并释放其贡献，直接显示当前基础目标；不修改声明、Spec、底层绑定或样式。同目标也会立即结束，之后的目标变化仍按声明播放。

## 注意事项

- **`Property` 约束**：只能动画**非继承、非 Direct 的 styled 属性**。对继承属性（如 `TextBlock.FontSize`）声明会抛 `NotSupportedException`，对 Direct 属性（如 `Layoutable.DesiredSize`）会抛 `InvalidOperationException`。
- **绑定方向**：目标值请使用 `OneWay` 表示层绑定；`TwoWay` 绑定可能把动画采样值写回数据源。可编辑的值请走独立路径。
- **同一属性只配置一个动画来源**：不要把本组件的声明与 Avalonia 自带 `Transition`/`Animation` 叠加在同一属性上。
- **非有限端点**：`Width="Auto"`（`NaN`）这类非有限数值没有插值轨迹，直接落到基础值；之后的有限值变化照常动画。
- **诊断**：根 Scope 的 `AnimationFailed` 报告失败来源、标签与异常，`LastError` 保留最近失败。规格失败释放该运行的贡献；帧源失败停止整个固定环境的后续源请求。
- **规格改动**：修改已在使用中的 `Spec` 只影响下一次目标变化，不重新计时当前运行。

## 延伸阅读

- [AnimatedContentSize：内容尺寸动画](./animated-content-size.md)
- [AnimatedVisibility：进入/离开过渡](./animated-visibility.md)
- [AnimatedContent：内容切换过渡](./animated-content.md)
- [运行时概览与时钟](../runtime/overview.md)
- 仓库内参考：程序集自述 `ComposeAnimation.Avalonia/README.md`、示例 `Catalog/Demos/Properties/PropertiesAnimationDemo.axaml`、行为测试 `ComposeAnimation.Test/AnimatedPropertyTest.cs`
