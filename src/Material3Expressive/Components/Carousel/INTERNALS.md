# Carousel 内部机制与维护说明

## 1. 上游基线与职责映射

AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 为逻辑依据，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 为桌面依据。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/carousel/MultiAspectCarousel.kt` 内容相同。
经典路径的 `Arrangement.kt`、`KeylineList.kt`、`Keylines.kt`、`Strategy.kt`、`KeylineSnapPosition.kt`、`Carousel.kt`、`CarouselState.kt` 和 `CarouselItemScope.kt` 亦逐文件核对相同。

| 上游职责 | 本地位置 |
| --- | --- |
| `getMask` / `getParallax` / `getMinSize` / `getMaskIntensity` | `MultiAspectCarouselItemDrawInfo.Calculate` |
| `Strategy.kt lerp`、UI util `MathHelpers.kt lerp` | 有界输入及 Float 插值 |
| `MultiAspectCarouselItemDrawInfo(index, LazyListState/LazyGridState)` | 实际视口测量 / `UseItemCrossAxisSize` / 显式 `DrawInfo` |
| `MaskParallaxNode` / `MaskShape` / `maskBorder` | `MultiAspectCarouselItem` / `Carousel.axaml`，复用圆角路径和 SurfaceOutline |
| `Arrangement` / `multiBrowseKeylineList` / `uncontainedKeylineList` / `heroKeylineList` | `CarouselArrangement` / `CarouselKeylines` |
| `KeylineList` / `Strategy` / `getSnapPositionOffset` | `CarouselKeylineList` / `CarouselStrategy` |
| 三种 Horizontal Carousel / `carouselItem` | `HorizontalCarousel.Variant` / `CarouselPanel` / `CarouselItem` |
| `PagerState.performScroll` / `PagerMeasureResult.copyWithScrollDeltaWithoutRemeasure` | `CarouselScrollPosition` / `CarouselPanel.LayoutStateAt` |
| `SnapFlingBehavior` / `PagerSnapLayoutInfoProvider` / `CarouselDefaults` | `CarouselFling` / panel 的 approach、snap 计算 / `FlingMode` |
| Core `DragGestureNode` / `Lsq2VelocityTracker` | `HorizontalCarousel.Input` / 现有共享 `VelocityTracker` |
| `pagerSemantics` / `PagerScrollScope.calculateDistanceTo` | `HorizontalCarousel.Automation` / panel 的 Pager 位移分支 |

## 2. 关键机制与架构契约

MultiAspect 主轴尺寸不随遮罩变化；内容在原尺寸中绘制，再在固定遮罩内平移。遮罩边框在内容之后绘制，Canvas 使其不参与固有尺寸计算。
列表的宽高比使用视口交叉轴尺寸，网格使用项交叉轴尺寸。两端越界时优先处理起始边；插值输入有界。
固定上游在宽高比等于 1、16/9 时回到 1/4 分支，此不连续性保留。`MinSize` 是上游计算的属性，不能据其文档字面意思再钳制实际遮罩。
`size == 0` 的遮罩回退到完整内容边界，视口负责遮住不可见项；不能以此改变布局或销毁用户内容。

模板内 presenter 独立承担视差，保留用户内容自己的 RenderTransform。模板重建更新部件；LayoutUpdated / ScrollChanged 订阅随挂载释放。
`maskBorder` 的 MaskShape 由无 key 的 remember 创建，保留首次形状；`maskClip` 的节点会更新形状，两者不能合并成一个随 CornerRadius 更新的边框。边框缓存缩小时保留原分配，纯色位图以像素源矩形及 DIP 目标矩形绘制；内容单独 clip，外层控件不裁掉边框缓存的末缘采样。路径描边还保留原始 mask 左上角：反向擦除的 scale 中心位于缓存坐标，其前置 translate 顺序不能改为先把路径归零。
公开 ActualDrawInfo 通过 DirectProperty 通知。显式 DrawInfo 优先；清除后重新读取实际几何，不把旧快照当作当前布局。

经典路径先在物理像素中分配 keyline，保留 Float 的计算顺序、加权 lerp 和 Kotlin 的 roundToInt；最终排列及 mask 才换回 DIP。项目内容始终按大项目尺寸测量，遮罩随 keyline 插值，不能用压缩内容代替。主轴 padding 由 Strategy 分摊尺寸并恢复原 unadjustedOffset，交叉轴 padding 才作为普通布局留白。
首尾吸附位置随项目索引变化，不使用统一步长取代 `getSnapPositionOffset`。RTL 只翻转排列，不翻转内容内部绘图。模板独立承担遮罩与路径边框，不覆盖用户内容的变换。
经典 Carousel 的 `carouselItem` 在圆角 maskClip 和 maskBorder 外还施加矩形 keyline 裁剪；它与 MultiAspect 的外层不裁剪不同，不能省掉，否则小项目边框的末缘采样越界。

滚动保留物理整数位置和未消费的小数累加器；小位移沿用 Pager 的增量页内比例与当前页，不每帧无条件重选最近项。程序动画先按可见页数量预跳转，再按固定 CarouselState 计算带首尾 snap 差的位移；该版本并未扣掉当前 offsetFraction，故中途改向保留此行为。

Fling 分为 decay approach 与 spring snap 两阶段，接续速度及帧时刻、边界未消费量、低速 400 DIP/s 判定、56 DIP 位置阈值和半页阈值均沿用上游。复用 ComposeAnimation 的 spline decay、SuspendAnimation 和子作用域；无额外计时器。Animation 优先级输出保留底层 ScrollOffset 绑定；取消与卸载保留当前显示位置并释放作用域。

Core `CanDragCalculation` 排除鼠标；触屏／笔在主轴越过 18 DIP 后捕获指针，保留超出阈值的位移。固定 Core 的 `isDraggableVelocityTrackerFixEnabled=false`，因此速度采样不包括越过阈值之前的 MOVE。拖动事件通过共享 InteractionSource 配对，禁用／捕获丢失终止手势，程序导航和卸载取消旧手势与动画。配置变更的取消回弹使用已有 `ScheduleFrameCommit`，对应 Core 在重组后启动副作用；正常释放与捕获丢失不延迟，后续导航或卸载会撤销待提交回弹。

## 3. 已确认决定与已知差异

此前 MultiAspect 移植已获授权使用可模板化内容控件承载 modifier 的职责，提供独立数值读数供布局适配；不另建列表或通用 Pager 框架。
沿用仓库圆角路径；此前共享 SurfaceOutline 已新增显式路径描边分支，对应上游 Outline.Generic 的离屏双宽描边和反向路径擦除，详见 [共享决定 §29](../../../../docs/DECISIONS.md#29-carousel-路径描边)。Avalonia 内容变换参与原生命中测试，内容交互位置随显示移动；Compose draw translate 本身不重排 semantics / 输入坐标，真实辅助技术和复杂交互尚需专项核验。

自动接入限于 UI 布局更新的视口；不把 SmoothScroll 合成位移的延迟 UI 回读认作逐帧一致。Catalog 和双端场景使用原生物理 ScrollContentPresenter，未修改全局 ScrollViewer 主题或历史 AppBar 决定。

本轮维护者明确授权新增组件与必要 API，经典布局采用 `HorizontalCarousel` + `CarouselVariant` 和原生 ItemsControl 数据模板。原先“不另建列表或通用 Pager 框架”针对 MultiAspect modifier，原因是其自身不拥有滚动；该理由保留，经典 Carousel 则需要独立布局 owner。新增容器不改变 MultiAspect 入口，无破坏性迁移。程序导航使用现有 ComposeAnimation；完整虚拟 Pager 和手势不以原生 ScrollViewer 近似替代。

辅助技术沿用本轮适配授权：`IScrollProvider` 的大／小增减都映射为上游一项分页，百分比设置映射为逻辑滚动范围的即时定位；垂直操作不支持。分页单独保留 Pager 的 `calculateDistanceTo`（扣除当前 fraction、整数取整、范围钳制），不混用 CarouselState 的程序导航位移。禁用用户滚动时不公开该 provider；原生控件禁用时拒绝操作。此适配未增加公开自定义自动化 API。

**尚未实现**：经典 Carousel 的虚拟 Pager、滚轮／触控板、嵌套滚动和焦点自动滚入；任意路径 Shape；SmoothScroll 合成线程遮罩接入。
**未经验证**：实际触屏、辅助技术、跨平台栅格、非均匀/非纯色描边以及第三方虚拟化宿主的适配。双端失败如实保留，不修改基准或容差。

## 4. 验证入口

- `MultiAspectCarouselTests`：宽高比与边界、双轴滚动、不变布局尺寸、换内容与用户变换、显式读数、卸载重挂。
- `MultiAspectCarouselCases.cs` / `.kt`：真实 Compose LazyRow/LazyColumn，横纵 × LTR/RTL × 六个宽高比分支，以及滚动、尺寸、圆角变化、边框和超宽项。
- Catalog：`CarouselDemo`。
- `CarouselTests` 和 `CarouselCases.cs` / `.kt`：经典三变体、RTL、首中末项目、少量集合、内容 padding、窄视口、分数尺寸、程序动画、fling、触屏事件与辅助技术分页；参考调用真实固定 Core 组件。

2026-11-06 经典 Carousel 本轮交付验证：全解决方案构建通过（仅现有 AVA3001 警告），全量行为测试 2354 项通过。四组双端 self-test 共 141 例、2097 个采样，重复捕获稳定，错误、超差及不可比较字段均为零。报告保存在 `artifacts/visual-parity/runs/<family>-delivery/summary.md`：

| family | 用例 | 采样 | 覆盖 |
| --- | ---: | ---: | --- |
| `carousel` | 78 | 558 | 三变体几何、边框、动态形状、跳转、远距离与反向动画 |
| `carousel-fling` | 21 | 609 | 三种 fling 模式、正负低高速、吸附点、边界及 RTL |
| `carousel-touch` | 18 | 522 | 三变体 × LTR/RTL × 快速释放、停留释放、途中禁用 |
| `carousel-semantics` | 24 | 408 | 三变体 × LTR/RTL × 下一页、末尾前一页、页内偏移、禁用 |

参考触屏夹具显式使用固定 Core `DesktopViewConfiguration` 的密度缩放 18 DIP 阈值，避免离屏空平台上下文默认 18 物理像素改变语义。配置变化通过真实重组帧提交；中途替换动画在发送下一帧前排空取消延续。辅助技术用例调用参考 LayoutNode 实际 `PageLeft` / `PageRight` 动作，而非用程序导航模拟。所有探针定义与图像门禁沿用现有规则，探索阶段失败产物保留。

Catalog 实际窗口已核验经典布局、前后导航、RTL 和 fling；辅助技术按钮调用原生 provider，provider 行为由行为测试与双端用例覆盖。真实屏幕阅读器和触摸硬件仍属于未经验证范围。

此前 MultiAspect 验证的 62 例（152 个采样）重复捕获稳定、执行错误为零，所有图像通过既有门禁；
21 例全等、40 例在容差内、1 例含不可比较字段。报告为 `artifacts/visual-parity/runs/carousel-completed/summary.md`。
`static.offset-250` 中，Compose LazyList 的 visibleItemsInfo 仍包含主体已离开视口 2 DIP、但尾部 8 DIP 间距仍相交的项；
本地按主体几何可见性枚举，故该项读数缺失。图像一致，宿主可见项定义的边界如实记录，不以补造读数消除差异。
先前边框失败已定位并修正为保留上游路径原点、缓存分配及末缘采样，失败产物保留；未修改基准或容差。
Catalog 实际窗口核验了滚动、边缘遮罩和视差，外层滚动容器保证较矮窗口中的完整网格可达。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~MultiAspectCarouselTests
./tests/VisualParity/parity.ps1 self-test --family multi-aspect-carousel
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~CarouselTests
./tests/VisualParity/parity.ps1 self-test --family carousel
./tests/VisualParity/parity.ps1 self-test --family carousel-fling
./tests/VisualParity/parity.ps1 self-test --family carousel-touch
./tests/VisualParity/parity.ps1 self-test --family carousel-semantics
```
