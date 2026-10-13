# ComposeAnimation

androidx.compose.animation & androidx.compose.animation.core 的 Avalonia 移植。

Runtime项目大约50%人工完成，给 Avalonia 的高级能力几乎是100% vibe，~~Astra还是太狠了~~。

因为高级能力几乎纯vibe，谨慎使用，你大概率会遇到非常多的问题。基础的 `Animatable` `Transition` 这些问题不大。

Compose `animation` 的能力基本上全部移植了，也包括依赖 Compose 布局系统的 Lookahead pass 的东西，如 `AnimatedContentSize`，`AnimatedBounds`，`SharedElement`，`SharedBounds`。需要 Lookahead 动画不能直接在既有的树中使用，必须要使用一套实现了 Lookahead 所需协议的控件，详见文档。

动画用法见[/docs/usage](docs/usage)。~~让k3写的，感觉比gpt写得更容易看懂~~

本模块源码现由 Material3Expressive 主仓库维护，通过仓内 ProjectReference 调试。
三个包分别在各自 csproj 声明版本，统一由主仓库的手动工作流打包发布，见 [根 README](../../README.md#版本与发布)。
交互演示运行 `dotnet run --project Catalog/Catalog.csproj`，用法见 [Catalog](Catalog/README.md)。

## 目录结构

| 路径 | 内容 |
| --- | --- |
| `ComposeAnimation.Runtime` | 与 UI 无关的引擎：`Animatable`、`Transition`、`InfiniteTransition`、规格、转换器、`FrameScope` |
| `ComposeAnimation.Avalonia` | Avalonia 宿主：`AnimationScope`、组件、Lookahead 容器（[README](ComposeAnimation.Avalonia/README.md)） |
| `ComposeAnimation.TestSupport` | `ManualFrameSource`、`AnimationTestWindow` 等测试辅助（NuGet 包） |
| `ComposeAnimation.Test` | 单元测试，含文档示例编译检查 `DocSamples.cs` |
| `Catalog` | 交互演示与自动化动作（[README](Catalog/README.md)） |
| `docs/usage` | 现行文档（[入口](docs/usage/README.md)，[迁移指南](docs/usage/migration-from-visual-animation-scope.md)） |
| `docs/draft` | 重构前的历史设计稿 |

## 构建与测试

```bash
dotnet build ComposeAnimation.slnx
dotnet test ComposeAnimation.Test
dotnet run --project Catalog/Catalog.csproj -- --run --all --out /tmp/catalog   # 需要桌面
```

