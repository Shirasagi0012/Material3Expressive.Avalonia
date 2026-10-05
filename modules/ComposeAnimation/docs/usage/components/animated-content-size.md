# 内容尺寸动画：AnimatedContentSize

`AnimatedContentSize : ContentControl` 对标 Jetpack Compose 的 `Modifier.animateContentSize`：当内容的**测量尺寸**变化时，控件向父级报告的尺寸做动画，而不是瞬间跳变。子级始终按自然（目标）尺寸测量和布局——它不会把内容截图拉伸，也不会按每一帧的中间尺寸让内容重新换行；变的是容器向父级报告的大小，因此周围的兄弟元素会被平滑地推开/合拢。

何时使用：折叠面板、卡片展开收起、文本变长、子控件增删等"内容大小变了，容器跟着变"的场景。

与相近组件的区别：

- [`Animated` 属性动画](./animated-properties.md)针对**显式属性值**（如 `Width="200"` 变成 `Width="500"`）；`AnimatedContentSize` 动画的是布局**测量出来**的尺寸，目标值由内容决定；
- [`AnimatedContent`](./animated-content.md)处理新旧内容交替的淡入淡出/过渡编排，不改变容器测量语义。

可运行示例见 `Catalog/Demos/ContentSize`，行为测试见 `ComposeAnimation.Test/AnimatedContentSizeBehaviorTest.cs`。

## 快速开始

下面的卡片内容在两种自然尺寸之间切换，`AnimatedContentSize` 用弹簧把报告给父级的尺寸平滑过渡到新值：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="AnimatedContentSizeDemo.MainWindow"
        Width="860" Height="900">
    <StackPanel Margin="24" Spacing="10">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Content="Compact" Click="ShowCompact" />
            <Button Content="Expanded" Click="ShowExpanded" />
        </StackPanel>
        <ca:AnimatedContentSize x:Name="Card" HorizontalAlignment="Left">
            <ca:AnimatedContentSize.AnimationSpec>
                <ca:SizeSpringSpec />
            </ca:AnimatedContentSize.AnimationSpec>
            <Border Width="180" Height="72" Background="#3C78D8" CornerRadius="8" Padding="12">
                <TextBlock Foreground="White" Text="Compact content" />
            </Border>
        </ca:AnimatedContentSize>
    </StackPanel>
</Window>
```

```csharp
// MainWindow.axaml.cs：替换内容为不同自然尺寸的控件
private void ShowCompact(object? sender, RoutedEventArgs e)
{
    Card.Content = new Border
    {
        Width = 180, Height = 72,
        Background = Brush.Parse("#3C78D8"), CornerRadius = new CornerRadius(16),
        Padding = new Thickness(12),
        Child = new TextBlock { Text = "Compact content", Foreground = Brushes.White }
    };
}

private void ShowExpanded(object? sender, RoutedEventArgs e)
{
    Card.Content = new Border
    {
        Width = 320, Height = 150,
        Background = Brush.Parse("#674EA7"), CornerRadius = new CornerRadius(16),
        Padding = new Thickness(12),
        Child = new TextBlock { Text = "Expanded content with more room", Foreground = Brushes.White }
    };
}
```

不改变 `Content`、只修改现有子控件的尺寸（如 `leaf.Height = 140`）同样会触发动画。首次挂载的测量直接落到目标尺寸，不产生动画。

## API 要点

| 成员 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `AnimationSpec` | `IFiniteAnimationSpec<Size>` | `SpringSpec<Size>`（刚度 `Spring.StiffnessMediumLow` = 400、无回弹阻尼、可见性阈值 0.5 DIP/维） | 尺寸动画规格；不允许赋 `null`。改动只作用于下一次目标变化，不重新计时进行中的运行。XAML 中可用 `SizeSpringSpec` / `SizeTweenSpec` 别名 |
| `ContentAlignment` | `VisibilityAlignment` | `TopStart` | 动画期间完整内容在报告尺寸内的对齐；`TopStart`/`Top`/`TopEnd`/`Start`/`Center`/`End`/`BottomStart`/`Bottom`/`BottomEnd` 九种，随 `FlowDirection` 镜像 |
| `ClipToBounds` | `bool`（继承） | `true`（本控件重写默认值） | 以当前动画中的边界裁剪内容；可显式关闭 |
| `AnimationCompleted` | `event EventHandler<AnimatedContentSizeChangedEventArgs>` | — | 一次运行自然完成时触发；参数含 `InitialSize`（本次运行起点）与 `TargetSize`（完成结果，非受父约束截断后的 `Bounds`）。被重定向或取消的运行不触发 |
| `ResetAnimation()` | 方法 | — | 取消运动并忘记先前尺寸；用于回收复用或出错后重启，须在 UI 线程调用 |

另外，内容的 `HorizontalContentAlignment` / `VerticalContentAlignment` 默认值被重写为 `Stretch`。

## 常见模式与进阶用法

### 兄弟元素重排

`AnimatedContentSize` 报告的是动画中间尺寸，父布局逐帧重排，因此同容器的兄弟元素会连续移动：

```xml
<StackPanel Spacing="10">
    <ca:AnimatedContentSize x:Name="Reflow" HorizontalAlignment="Left">
        <Border Width="260" Height="56" Background="#6AA84F" CornerRadius="8" Padding="12">
            <TextBlock Foreground="White" Text="Short panel" />
        </Border>
    </ca:AnimatedContentSize>
    <Border Background="#F6B26B" CornerRadius="8" Padding="12">
        <TextBlock Text="Sibling marker — watch me move" />
    </Border>
</StackPanel>
```

把 `Reflow` 的内容换成更高的面板（示例工程中 `ShowTallPanel` 换成 260×180 的内容），下方的兄弟 `Border` 会被平滑推开。

### 文本换行

子级按**目标约束**测量：动画期间文本保持目标布局，容器围绕它改变尺寸，而不是逐帧重排文字：

```xml
<Border Width="360" BorderBrush="#999999" BorderThickness="1" Padding="6">
    <ca:AnimatedContentSize x:Name="TextBox" HorizontalAlignment="Left">
        <TextBlock Width="330" TextWrapping="Wrap" Text="One line of text." />
    </ca:AnimatedContentSize>
</Border>
```

换成多行文本时高度平滑增长。若需要内容按中间宽度真实重新换行，属于实验性 lookahead 范畴，见下文注意事项。

### 完成事件与内容对齐

```csharp
Card.AnimationCompleted += (_, e) =>
    Status.Text = $"Animation completed: {e.InitialSize.Width:0}×{e.InitialSize.Height:0} → {e.TargetSize.Width:0}×{e.TargetSize.Height:0}";
```

事件处理器里可以再次更换内容、调用 `ResetAnimation()`、替换时钟或移除控件，旧运行不会覆盖新状态。`ContentAlignment` 决定内容（按自然尺寸排列）在动画报告尺寸内的落位，例如 `BottomEnd` 让内容钉在右下角；单独修改 `ContentAlignment` 或 `FlowDirection` 会立即触发重排，无需等待下一次尺寸变化。

### 回收复用：ResetAnimation

把同一个实例回收用于另一项数据时，先重置再换内容，避免从旧尺寸开始动画：

```csharp
SizeHost.ResetAnimation();
SizeHost.Content = nextItem;
```

`ResetAnimation()` 取消当前运行、忘记先前尺寸，下一次测量直接初始化到新目标，也不报告被取消运行的完成事件。不要在普通的 `Content` 变化时随手调用，否则会丢失尺寸过渡。普通的临时移除/重新挂载不需要它——重挂后控件会保留当前尺寸并恢复趋近目标。

## 注意事项

- **裁剪**：`ClipToBounds` 默认被重写为 `true`，内容超出当前动画边界的部分会被裁掉；不需要裁剪时显式设回 `false`。
- **装饰的归属**：模板内的 `Padding` 与 `BorderThickness` 计入被动画的内容尺寸；`Margin` 在外部。希望装饰不参与动画区域时，把装饰控件放到 `AnimatedContentSize` 外面。
- **空内容与模板**：支持 `ContentTemplate`、空内容到零尺寸的过渡，以及自定义 `ControlTemplate`（按完整的直接视觉根测量排列）。
- **时钟与诊断**：帧源在根 Scope 初始化时确定；播放/规格/时钟失败通过根 Scope 的 `AnimationFailed` 诊断，失败的运行直接落到目标尺寸，更换目标或规格后可继续动画。
- **零时长**：`MotionDurationScale.ScaleFactor = 0` 或零时长规格在收到首个动画帧时完成。
- **实验性 lookahead 模式**：把控件显式放入 lookahead 域（`LookaheadRoot` 或自动局部域）后，切换为协作式目标测量——内容目标尺寸被独立预测。该路径限制为：默认模板、直接的 `Control` 内容、内容对齐保持 `Stretch`、无 `ContentTemplate` / `Padding` / `BorderThickness`；不满足时明确抛 `NotSupportedException`。协议与支持范围仍可能变化，详见 [AnimatedBounds 与 lookahead](./animated-bounds.md)。这些限制只属于实验路径，普通用法不受其约束。

## 延伸阅读

- [旧版 AnimatedContentSize 深入文档](../../draft/ANIMATED_CONTENT_SIZE.md)（普通路径的行为细节、生命周期与 Compose 对应关系仍然有效）
- [属性动画：Animated 附加属性](./animated-properties.md)
- [AnimatedContent：内容切换过渡](./animated-content.md)
- [AnimatedBounds 与协作式 lookahead](./animated-bounds.md)
- [运行时概览与时钟](../runtime/overview.md)
