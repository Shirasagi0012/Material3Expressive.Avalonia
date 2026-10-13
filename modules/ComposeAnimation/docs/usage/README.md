# ComposeAnimation 使用文档

ComposeAnimation 是把 Jetpack Compose 动画系统移植到 Avalonia 的动画库。它分为两层：`ComposeAnimation.Runtime` 是纯动画引擎（`Animatable<T>`、动画规格、`Transition<TState>`、`InfiniteTransition`、帧时钟等），不依赖任何控件；`ComposeAnimation.Avalonia` 在引擎之上提供控件层（命名空间 `ComposeAnimation`，XAML 前缀 `ca:`），包括 `Animated.Properties`、`AnimatedVisibility`、`AnimatedContent`/`Crossfade`、`AnimatedContentSize`、`AnimatedBounds` 与 Lookahead 家族、SharedTransition 家族。目标框架 net10.0，Avalonia 12.1.2。

## 安装与引用

在本仓库内，通过 `modules/ComposeAnimation` 下的项目引用直接构建和调试，无需预先打包。
外部项目通过 GitHub Packages 安装，包源和发布方式见[根 README](../../../../README.md#版本与发布)。
引用控件层包时会传递引入 Runtime；以下示例使用迁移后的版本，需待维护者发布后安装：

```xml
<ItemGroup>
  <PackageReference Include="Shirasagi0012.ComposeAnimation.Avalonia" Version="0.2.9-alpha" />
</ItemGroup>
```

如果只需要引擎，可只引用同版本的 `Shirasagi0012.ComposeAnimation.Runtime` 包。
启用中央包管理时，在消费项目的 `Directory.Packages.props` 中配置版本。

XAML 中通过统一 xmlns 声明引入控件层（程序集内已注册 `XmlnsDefinition`，推荐前缀 `ca`）：

```xml
xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
```

## 快速上手

下面是一个最小示例：`Border.Width` 变化时按弹簧规格平滑过渡，无需编写任何动画驱动代码。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
        x:Class="Demo.MainWindow">
    <StackPanel Spacing="16" VerticalAlignment="Center">
        <Border x:Name="Box" Width="100" Height="100" Background="Aqua">
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
        <Button Click="OnChange" HorizontalAlignment="Center">Change</Button>
    </StackPanel>
</Window>
```

```csharp
// Window 构造函数：必须在 InitializeComponent 之前建立根环境
public MainWindow()
{
    AnimationScope.Initialize(this);
    InitializeComponent();
}

// 直接改属性即可，AnimatedDouble 负责把变化变成动画
private bool _flag = true;

private void OnChange(object? sender, RoutedEventArgs e)
{
    Box.Width = _flag ? 500 : 100;
    _flag = !_flag;
}
```

更多可运行示例见 [Catalog](../../Catalog/README.md)，运行 `dotnet run --project Catalog/Catalog.csproj`。

## 组件地图

| 组件 | 用途 | 文档 | 状态 |
| --- | --- | --- | --- |
| `AnimationScope` | 根动画环境、控件 Scope 生命周期、任务与值消费 | [animation-scope.md](animation-scope.md) | 已实现 |
| `Animated.Properties` | 附加属性集合：任意 styled 属性的值变化按声明的规格播放动画 | [components/animated-properties.md](components/animated-properties.md) | 稳定 |
| `AnimatedVisibility` | 内容出现/消失的进出场过渡（`Visible` 驱动，支持子元素级联） | [components/animated-visibility.md](components/animated-visibility.md) | 稳定 |
| `AnimatedContent` / `Crossfade` | `Content` 切换时的进出场过渡与容器尺寸动画 | [components/animated-content.md](components/animated-content.md) | 稳定 |
| `AnimatedContentSize` | 内容测量尺寸变化时平滑动画容器大小 | [components/animated-content-size.md](components/animated-content-size.md) | 稳定 |
| `AnimatedBounds` + Lookahead 容器 | 基于协作式 lookahead 的布局边界动画 | [components/animated-bounds.md](components/animated-bounds.md) | 实验性 |
| SharedTransition 家族（`SharedTransitionHost`/`SharedElement`/`SharedBounds`） | 共享元素跨状态过渡 | [components/shared-transition.md](components/shared-transition.md) | 实验性 |
| Runtime 引擎（`Animatable`、规格、`Transition`、`InfiniteTransition`） | 纯 C# 动画引擎，控件层即建立在它之上 | [runtime/overview.md](runtime/overview.md) | 稳定 |

## 核心概念

- **规格（Spec）**：`IAnimationSpec<T>` 描述动画曲线（弹簧 `SpringSpec`、补间 `TweenSpec`、关键帧 `KeyframesSpec` 等），与值分离、可复用；未指定规格时默认使用弹簧。
- **帧时钟**：动画由 `IFrameSource` 逐帧驱动（拉模型），所有动画 API 必须在时钟所属线程（通常是 UI 线程）调用。
- **Task 异步与中断**：动画方法返回 `Task`（方法名带 `Async` 后缀）；动画被新动画、`Stop()` 或 `SnapToAsync` 打断时，旧 Task 以 `OperationCanceledException` 结束，调用方必须捕获。
- **Transition 状态驱动**：`Transition<TState>` 把一组属性动画绑定到状态机，改 `TargetState` 即整树切换。

详见 [runtime/overview.md](runtime/overview.md)。

## 文档目录

核心：

- [animation-scope.md](animation-scope.md) —— Scope 模型、重建与恢复（D17）、MoveContent、已知限制与图示
- [migration-from-visual-animation-scope.md](migration-from-visual-animation-scope.md) —— 从旧 Scope API 迁移

组件层：

- [components/animated-properties.md](components/animated-properties.md) —— `Animated.Properties` 属性动画
- [components/animated-visibility.md](components/animated-visibility.md) —— `AnimatedVisibility` 进出场
- [components/animated-content.md](components/animated-content.md) —— `AnimatedContent` 与 `Crossfade`
- [components/animated-content-size.md](components/animated-content-size.md) —— `AnimatedContentSize`
- [components/animated-bounds.md](components/animated-bounds.md) —— `AnimatedBounds` 与 Lookahead 容器（实验性）
- [components/shared-transition.md](components/shared-transition.md) —— SharedTransition 家族（实验性）

Runtime 引擎：

- [runtime/overview.md](runtime/overview.md) —— 引擎核心概念（帧时钟、异步模型、时长缩放）
- [runtime/animatable.md](runtime/animatable.md) —— `Animatable<T>`
- [runtime/animation-specs.md](runtime/animation-specs.md) —— 动画规格
- [runtime/converters-and-easing.md](runtime/converters-and-easing.md) —— 类型转换器与缓动
- [runtime/transitions.md](runtime/transitions.md) —— `Transition` 与 `InfiniteTransition`

进阶：

- [advanced/troubleshooting.md](advanced/troubleshooting.md) —— 常见问题与限制
- [advanced/custom-specs.md](advanced/custom-specs.md) —— 自定义动画规格
- [advanced/custom-lookahead-containers.md](advanced/custom-lookahead-containers.md) —— 自定义 lookahead 容器（实验性）

## 旧文档说明

`docs/draft/` 下是 2026-10 动画基础重构之前的设计与记录文档，每页顶部都有“历史设计稿”横幅：名称与行为可能已过时，仅作背景参考。现行行为以本目录为准。
