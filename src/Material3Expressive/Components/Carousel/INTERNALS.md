# Carousel 内部机制与维护说明

## 1. 上游基线与职责映射

AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 为逻辑依据，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 为桌面依据。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/carousel/MultiAspectCarousel.kt` 内容相同。

| 上游职责 | 本地位置 |
| --- | --- |
| `getMask` / `getParallax` / `getMinSize` / `getMaskIntensity` | `MultiAspectCarouselItemDrawInfo.Calculate` |
| `Strategy.kt lerp`、UI util `MathHelpers.kt lerp` | 有界输入及 Float 插值 |
| `MultiAspectCarouselItemDrawInfo(index, LazyListState/LazyGridState)` | 实际视口测量 / `UseItemCrossAxisSize` / 显式 `DrawInfo` |
| `MaskParallaxNode` / `MaskShape` / `maskBorder` | `MultiAspectCarouselItem` / `Carousel.axaml`，复用圆角路径和 SurfaceOutline |

## 2. 关键机制与架构契约

主轴尺寸不随遮罩变化；内容在原尺寸中绘制，再在固定遮罩内平移。遮罩边框在内容之后绘制，Canvas 使其不参与固有尺寸计算。
列表的宽高比使用视口交叉轴尺寸，网格使用项交叉轴尺寸。两端越界时优先处理起始边；插值输入有界。
固定上游在宽高比等于 1、16/9 时回到 1/4 分支，此不连续性保留。`MinSize` 是上游计算的属性，不能据其文档字面意思再钳制实际遮罩。
`size == 0` 的遮罩回退到完整内容边界，视口负责遮住不可见项；不能以此改变布局或销毁用户内容。

模板内 presenter 独立承担视差，保留用户内容自己的 RenderTransform。模板重建更新部件；LayoutUpdated / ScrollChanged 订阅随挂载释放。
`maskBorder` 的 MaskShape 由无 key 的 remember 创建，保留首次形状；`maskClip` 的节点会更新形状，两者不能合并成一个随 CornerRadius 更新的边框。边框缓存缩小时保留原分配，纯色位图以像素源矩形及 DIP 目标矩形绘制；内容单独 clip，外层控件不裁掉边框缓存的末缘采样。路径描边还保留原始 mask 左上角：反向擦除的 scale 中心位于缓存坐标，其前置 translate 顺序不能改为先把路径归零。
公开 ActualDrawInfo 通过 DirectProperty 通知。显式 DrawInfo 优先；清除后重新读取实际几何，不把旧快照当作当前布局。

## 3. 已确认决定与已知差异

本轮用户明确授权自主新增 API 与必要框架适配：使用可模板化内容控件承载 modifier 的职责，提供独立数值读数供布局适配；不另建列表或通用 Pager 框架。
沿用仓库圆角路径；共享 SurfaceOutline 新增显式路径描边分支，对应上游 Outline.Generic 的离屏双宽描边和反向路径擦除，详见 [共享决定 §29](../../../../docs/DECISIONS.md#29-carousel-路径描边)。Avalonia 内容变换参与原生命中测试，内容交互位置随显示移动；Compose draw translate 本身不重排 semantics / 输入坐标，真实辅助技术和复杂交互尚需专项核验。

自动接入限于 UI 布局更新的视口；不把 SmoothScroll 合成位移的延迟 UI 回读认作逐帧一致。Catalog 和双端场景使用原生物理 ScrollContentPresenter，未修改全局 ScrollViewer 主题或历史 AppBar 决定。

**尚未实现**：经典 MultiBrowse / Uncontained / CenteredHero keyline Carousel、Pager 状态及 fling；任意路径 Shape；SmoothScroll 合成线程遮罩接入。
**未经验证**：实际触屏、辅助技术、跨平台栅格、非均匀/非纯色描边以及第三方虚拟化宿主的适配。双端失败如实保留，不修改基准或容差。

## 4. 验证入口

- `MultiAspectCarouselTests`：宽高比与边界、双轴滚动、不变布局尺寸、换内容与用户变换、显式读数、卸载重挂。
- `MultiAspectCarouselCases.cs` / `.kt`：真实 Compose LazyRow/LazyColumn，横纵 × LTR/RTL × 六个宽高比分支，以及滚动、尺寸、圆角变化、边框和超宽项。
- Catalog：`CarouselDemo`。

2026-11-06 最终 62 例（152 个采样）重复捕获稳定、执行错误为零，所有图像通过既有门禁；
21 例全等、40 例在容差内、1 例含不可比较字段。报告为 `artifacts/visual-parity/runs/carousel-completed/summary.md`。
`static.offset-250` 中，Compose LazyList 的 visibleItemsInfo 仍包含主体已离开视口 2 DIP、但尾部 8 DIP 间距仍相交的项；
本地按主体几何可见性枚举，故该项读数缺失。图像一致，宿主可见项定义的边界如实记录，不以补造读数消除差异。
先前边框失败已定位并修正为保留上游路径原点、缓存分配及末缘采样，失败产物保留；未修改基准或容差。
Catalog 实际窗口核验了滚动、边缘遮罩和视差，外层滚动容器保证较矮窗口中的完整网格可达。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~MultiAspectCarouselTests
./tests/VisualParity/parity.ps1 self-test --family multi-aspect-carousel
```
