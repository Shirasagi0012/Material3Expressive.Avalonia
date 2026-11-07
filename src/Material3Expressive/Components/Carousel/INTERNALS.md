# Carousel 内部机制与维护说明

## 1. 上游基线与职责映射

AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 为逻辑依据，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 为桌面依据。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/carousel/MultiAspectCarousel.kt` 内容相同。
经典路径的 `Arrangement.kt`、`KeylineList.kt`、`Keylines.kt`、`Strategy.kt`、`KeylineSnapPosition.kt`、`Carousel.kt`、`CarouselState.kt` 和 `CarouselItemScope.kt` 亦逐文件核对相同。
焦点相关 `Pager.kt`、`UpdatableAnimationState.kt` 和 `BringIntoViewRequestPriorityQueue.kt` 两端相同；`ContentInViewNode.kt` 仅取消错误消息是否打印绝对值不同，算法与执行顺序相同。

| 上游职责 | 本地位置 |
| --- | --- |
| `getMask` / `getParallax` / `getMinSize` / `getMaskIntensity` | `MultiAspectCarouselItemDrawInfo.Calculate` |
| `Strategy.kt lerp`、UI util `MathHelpers.kt lerp` | 有界输入及 Float 插值 |
| `MultiAspectCarouselItemDrawInfo(index, LazyListState/LazyGridState)` | 实际视口测量 / `UseItemCrossAxisSize` / 显式 `DrawInfo` |
| `MaskParallaxNode` / `MaskShape` / `maskBorder` | `MultiAspectCarouselItem` / `Carousel.axaml`，复用圆角路径和 SurfaceOutline |
| `Arrangement` / `multiBrowseKeylineList` / `uncontainedKeylineList` / `heroKeylineList` | `CarouselArrangement` / `CarouselKeylines` |
| `KeylineList` / `Strategy` / `getSnapPositionOffset` | `CarouselKeylineList` / `CarouselStrategy` |
| 三种 Horizontal Carousel / `carouselItem` | `HorizontalCarousel.Variant` / `CarouselPanel` / `CarouselItem` |
| `LazyLayoutBeyondBoundsProviderModifierNode` / `PagerBeyondBoundsState` / Core `RootNodeOwner.handleFocusKeys` | `CarouselPanel.MoveFocus` / `HorizontalCarousel.OnKeyDown` |
| `PagerMeasure.measurePager` / `createPagesBeforeList` / `createPagesAfterList` | `CarouselPanel.Virtualization`：页范围、容器生成/回收与焦点页保留 |
| `PagerState.performScroll` / `PagerMeasureResult.copyWithScrollDeltaWithoutRemeasure` | `CarouselScrollPosition` / `CarouselPanel.LayoutStateAt` |
| `SnapFlingBehavior` / `PagerSnapLayoutInfoProvider` / `CarouselDefaults` | `CarouselFling` / panel 的 approach、snap 计算 / `FlingMode` |
| Core `DragGestureNode` / `Lsq2VelocityTracker` | `HorizontalCarousel.Input` / 现有共享 `VelocityTracker` |
| `pagerSemantics` / `PagerScrollScope.calculateDistanceTo` | `HorizontalCarousel.Automation` / panel 的 Pager 位移分支 |
| Core `MouseWheelScrollingLogic` / `ScrollableNode.onKeyEvent` | 共享 `MouseWheelScroll` / `HorizontalCarousel.Wheel` / `OnKeyDown` |
| `ContentInViewNode` / `BringIntoViewRequestPriorityQueue` / `DefaultPagerBringIntoViewSpec` / `UpdatableAnimationState` | `HorizontalCarousel.Focus` |

## 2. 关键机制与架构契约

MultiAspect 主轴尺寸不随遮罩变化；内容在原尺寸中绘制，再在固定遮罩内平移。遮罩边框在内容之后绘制，Canvas 使其不参与固有尺寸计算。
列表的宽高比使用视口交叉轴尺寸，网格使用项交叉轴尺寸。两端越界时优先处理起始边；插值输入有界。
固定上游在宽高比等于 1、16/9 时回到 1/4 分支，此不连续性保留。`MinSize` 是上游计算的属性，不能据其文档字面意思再钳制实际遮罩。
`size == 0` 的遮罩回退到完整内容边界，视口负责遮住不可见项；不能以此改变布局或销毁用户内容。

模板内 presenter 独立承担视差，保留用户内容自己的 RenderTransform。模板重建更新部件；LayoutUpdated / ScrollChanged 订阅随挂载释放。
`maskBorder` 的 MaskShape 由无 key 的 remember 创建，保留首次形状；`maskClip` 的节点会更新形状，两者不能合并成一个随 CornerRadius 更新的边框。边框缓存缩小时保留原分配，纯色位图以像素源矩形及 DIP 目标矩形绘制；内容单独 clip，外层控件不裁掉边框缓存的末缘采样。路径描边还保留原始 mask 左上角：反向擦除的 scale 中心位于缓存坐标，其前置 translate 顺序不能改为先把路径归零。
公开 ActualDrawInfo 通过 DirectProperty 通知。显式 DrawInfo 优先；清除后重新读取实际几何，不把旧快照当作当前布局。

经典路径先在物理像素中分配 keyline，保留 Float 的计算顺序、加权 lerp 和 Kotlin 的 roundToInt；最终排列及 mask 才换回 DIP。项目内容始终按大项目尺寸测量，遮罩随 keyline 插值，不能用压缩内容代替。主轴 padding 由 Strategy 分摊尺寸并恢复原 unadjustedOffset，交叉轴 padding 才作为普通布局留白。

虚拟页范围由遮罩前的整数 Pager 网格和含尾部 spacing 的可见区间决定；固定 Carousel.kt 为 MultiBrowse/Hero 指定两侧各两页，Uncontained 为零。
布局、最大滚动距离及吸附使用完整 Items.Count，不能使用已创建容器数。页内位置和 snap 只遍历当前可见页，不扫描整个数据集。
生成容器经原生 ItemContainerGenerator 的 prepare / prepared / clear 生命周期复用；直接 CarouselItem 只卸载，不能 clear 用户 Content。
离开范围会真正 detach，结束内容自己的动画和交互作用域。焦点页保留到 LostFocus 后的下一次布局；自动高度读取当前实现页与保留页。
ScrollIntoView 在首次布局后同步创建目标容器；普通 ScrollToItem 仍在下一次布局呈现。集合或 ItemTemplate 更新重新 prepare 数据。
保留页的布局对应 `calculatePagesOffsets`：可见页之前/之后的额外页连续放置，即使其索引有空洞；keyline 平移仍使用真实索引。不能用远处索引乘步长替代保留页位置。

Tab 搜索沿默认页范围逐次增加一页，上限是 `2 * max(1, viewport / (pageSize + spacing))`；成功或失败都同步释放临时范围。焦点本身使目标页保留。窗口继续处理搜索失败后的离开/回绕，RTL 的 Next/Previous 仍按逻辑页序。固定 Core `RootNodeOwner.skiko.kt` 只默认映射 Tab/Shift+Tab（以及 Enter/Exit），没有左右键映射。原先尝试给左右键默认移动的实验已按这份平台源码撤回，双端 `nav-next` 保留为无移动断言。

**2026-11-08 本轮授权的框架适配**：Avalonia 12.1.5 公开 `IFocusManager.FindNextElement` 与实际窗口的 Tab 路径不同；在新增虚拟页后，前者返回回绕目标，后者正确找到页内目标。组件局部绑定内部 `KeyboardNavigationHandler.GetNext`，只查询目标，实际焦点仍由原生 Focus 建立；用 `DynamicDependency` 保留裁剪目标，缺失方法明确失败。查询期间内部面板临时使用 Contained 导航，结束即释放，不阻止窗口离开。这样保留 TabIndex、组内遍历、自定义导航与子控件消费键盘的能力，而不复制整套 Avalonia 导航。此内部依赖须随 Avalonia 升级重跑测试；不推广为全库公共抽象。

首尾吸附位置随项目索引变化，不使用统一步长取代 `getSnapPositionOffset`。RTL 只翻转排列，不翻转内容内部绘图。模板独立承担遮罩与路径边框，不覆盖用户内容的变换。
经典 Carousel 的 `carouselItem` 在圆角 maskClip 和 maskBorder 外还施加矩形 keyline 裁剪；它与 MultiAspect 的外层不裁剪不同，不能省掉，否则小项目边框的末缘采样越界。

滚动保留物理整数位置和未消费的小数累加器；小位移沿用 Pager 的增量页内比例与当前页，不每帧无条件重选最近项。程序动画先按可见页数量预跳转，再按固定 CarouselState 计算带首尾 snap 差的位移；该版本并未扣掉当前 offsetFraction，故中途改向保留此行为。

Fling 分为 decay approach 与 spring snap 两阶段，接续速度及帧时刻、边界未消费量、低速 400 DIP/s 判定、56 DIP 位置阈值和半页阈值均沿用上游。复用 ComposeAnimation 的 spline decay、SuspendAnimation 和子作用域；无额外计时器。Animation 优先级输出保留底层 ScrollOffset 绑定；取消与卸载保留当前显示位置并释放作用域。

Core `CanDragCalculation` 排除鼠标；触屏／笔在主轴越过 18 DIP 后捕获指针，保留超出阈值的位移。固定 Core 的 `isDraggableVelocityTrackerFixEnabled=false`，因此速度采样不包括越过阈值之前的 MOVE。拖动事件通过共享 InteractionSource 配对，禁用／捕获丢失终止手势，程序导航和卸载取消旧手势与动画。配置变更的取消回弹使用已有 `ScheduleFrameCommit`，对应 Core 在重组后启动副作用；正常释放与捕获丢失不延迟，后续导航或卸载会撤销待提交回弹。

水平滚轮按固定 Core `OneDimensionalScrollValueAdapter` 选择主轴，RTL 在消费前转为逻辑方向。已开始的滚轮序列在 Tunnel 优先接收，否则在 Bubble 等待后代；静止时不能消费的方向继续传递。共享 MouseWheelScroll 保留批量合并、物理像素阈值、线性余量和差分速度，完成后调用原 Carousel fling；无鼠标 DragInteraction。导航和卸载取消旧滚轮运动，不让旧 timeout 启动迟到的 fling；Animation 优先级贡献保留 ScrollOffset 绑定。

PageUp/PageDown 对应固定 AndroidX/Core `ScrollableNode.onKeyEvent`，由聚焦后代冒泡到 Carousel；Ctrl、KeyUp 和已消费事件不处理。每次按键消费物理像素取整后的视口宽度，RTL 反转，不追加吸附。

焦点及显式 BringIntoView 请求按包含关系排序，不相交的新请求替换旧请求；帧后根据真实布局更新目标，保留速度的默认 float spring 对应 UpdatableAnimationState。使用分页网格而非 Carousel focal keyline 纠正位置，RTL 保留 layoutAwareFirstOffset 和最后物理滚动方向。最后方向仅在实际整数像素位移非零时改变；小数修正不能翻转目标。禁用用户滚动不阻止焦点滚入，正在进行的用户拖动/滚轮优先；导航和卸载取消旧请求。动画使用既有帧作用域和 Animation 优先级绑定。

本轮授权把原生 GotFocus / RequestBringIntoView 接入上述协议；Avalonia routed 请求没有可等待的调用方，故没有新增 await/cancel API。坐标取自绕过镜像策略的 CarouselPanel，不能直接用 owner 的镜像局部坐标替代 Compose 物理布局坐标。外层容器仍可继续处理 routed 请求。此前焦点未实现的原因是组件没有请求队列和逐帧重测连接；虚拟化现在另行接入容器生命周期。

视口缩小按 ContentInViewNode.onRemeasured 检查旧布局中的焦点矩形；RTL 预先扣除视口尺寸差，只有原本最大可见且现在被遮挡的焦点启动跟随。显式请求优先于跟随目标，焦点离开、导航、卸载或变为最大可见时结束跟随。尺寸/步长/吸附位置变化保留旧当前页与页内比例，按新的 Pager 网格取整重建位置；不能保留旧绝对像素偏移而改变所选项目。放大视口不启动新的跟随动画。

Core 的 afterFrame 不等同于本地强制完成一次全部布局：增量 Pager placement 延后到 draw；强制重测先应用整数页位置，图层平移仍沿用上一轮 draw。滚入循环在消费前保留真实目标矩形，按是否重测补上实际页位移，在该阶段判定请求并计算下一目标，之后才完成本地可视布局。此顺序由双端逐帧跟踪确认；直接使用最终可视矩形会令 RTL MultiBrowse 少滚一页。整数页的 RTL 排列先于 Float 图层平移，不重新结合表达式。

Pager 的绝对页坐标与实际视口 offset 分开计算边界；无法复用增量布局时，按当前页与 fraction 重建位置并重测。fraction 的半页钳制仍保留，不能仅累加物理 offset，否则 Uncontained 末端惯性会越过参考最终位置。

## 3. 已确认决定与已知差异

此前 MultiAspect 移植已获授权使用可模板化内容控件承载 modifier 的职责，提供独立数值读数供布局适配；不另建列表或通用 Pager 框架。
沿用仓库圆角路径；此前共享 SurfaceOutline 已新增显式路径描边分支，对应上游 Outline.Generic 的离屏双宽描边和反向路径擦除，详见 [共享决定 §29](../../../../docs/DECISIONS.md#29-carousel-路径描边)。Avalonia 内容变换参与原生命中测试，内容交互位置随显示移动；Compose draw translate 本身不重排 semantics / 输入坐标，真实辅助技术和复杂交互尚需专项核验。

自动接入限于 UI 布局更新的视口；不把 SmoothScroll 合成位移的延迟 UI 回读认作逐帧一致。Catalog 和双端场景使用原生物理 ScrollContentPresenter，未修改全局 ScrollViewer 主题或历史 AppBar 决定。

本轮维护者明确授权新增组件与必要 API，经典布局采用 `HorizontalCarousel` + `CarouselVariant` 和原生 ItemsControl 数据模板。原先“不另建列表或通用 Pager 框架”针对 MultiAspect modifier，原因是其自身不拥有滚动；该理由保留，经典 Carousel 则需要独立布局 owner。新增容器不改变 MultiAspect 入口，无破坏性迁移。程序导航使用现有 ComposeAnimation；完整虚拟 Pager 和手势不以原生 ScrollViewer 近似替代。

辅助技术沿用本轮适配授权：`IScrollProvider` 的大／小增减都映射为上游一项分页，百分比设置映射为逻辑滚动范围的即时定位；垂直操作不支持。分页单独保留 Pager 的 `calculateDistanceTo`（扣除当前 fraction、整数取整、范围钳制），不混用 CarouselState 的程序导航位移。禁用用户滚动时不公开该 provider；原生控件禁用时拒绝操作。此适配未增加公开自定义自动化 API。

**虚拟化决定与迁移**：2026-11-08 本轮授权新增共享能力与破坏性 API 迁移，改用 Avalonia VirtualizingPanel，保留既有 Carousel 页算法，不引入通用 Pager。
原先急切创建所有项是尚缺页生命周期的阶段性实现，并无已批准的永久保活语义；固定 AndroidX/Core PagerMeasure 与 Carousel.kt 的 beyondViewportPageCount 是新依据。
容器查询由“全量存在”变为“仅当前实现页存在”；调用方应先 ScrollIntoView 再访问远处容器，仓内 Catalog 和布局测试已迁移。
仍保留原公开滚动 API 与索引。自动预取、程序式二维焦点搜索及通用可保存页状态尚未移植，不能把视口虚拟化称为完整 Foundation Pager。

**尚未实现**：设备专用触控板 Pan/惯性、嵌套滚动；任意路径 Shape；SmoothScroll 合成线程遮罩接入。
滚轮共享抽取与设备元数据限制依据本轮授权，见 [共享决定 §33](../../../../docs/DECISIONS.md#33-pager-组件的桌面滚轮算法复用)。此前没有滚轮入口的原因是组件尚未连接桌面协议；现有 ScrollField 已提供同一固定 Core 算法，故复用而不另建引擎。无破坏性 API 迁移。

**未经验证**：实际触屏、精确滚轮／触控板硬件、辅助技术、跨平台栅格、非均匀/非纯色描边以及第三方虚拟化宿主的适配。双端失败如实保留，不修改基准或容差。

## 4. 验证入口

2026-11-08 虚拟页与 Tab 搜索增量：相关 Carousel 行为测试 75 项通过，解决方案构建通过（5 项既有警告）。`carousel-focus-search-delivery` 的 24 例 / 600 个采样全部全等，重复捕获稳定、错误为零，覆盖三种布局、LTR/RTL、连续 Tab、反向、稀疏可聚焦页与桌面方向键不移动。已查看稀疏焦点页的双端图像。`carousel-virtual-regression` 原有 78 例 / 558 个采样通过原门禁。

最终 `carousel-virtual-delivery` 24 例全等；`carousel-auto-height-delivery` 12 例通过原门禁（11 equal、1 withinTolerance），重复捕获稳定、错误为零；覆盖千项数据、远距离与反向动画、运行时可见页自适应高度。Catalog 增加万项数据模板、远距离定位和高度切换。

本次跨组件全量测试没有全部通过：最后一次 2461 项中 2453 通过、8 失败，7 项为 Headless
初始化/清理的 dispatcher 线程异常，另一项为 `NonInteractiveScrollBarTests.Fade_DelayRestartsOnObservedOffsetsAndScrollInterruptsFade`。
失败方法与菜单新实现合并复跑 125 项，124 通过，滚动条时间断言仍失败。未修改这些无关组件，
日志保留在 `artifacts/precision-pointer/full-tests-final.log` 与 `menu-and-failure-retry.log`。
VisualParity 系统测试 134 项通过；本段不覆盖上面分组件的独立通过结果，也不把历史全量通过记录当作本次结果。

新增页探针最终读取真实 `PagerMeasureResult.visiblePagesInfo + extraPagesBefore + extraPagesAfter`，对比本地活动实现容器。早期 `DisposableEffect` 的组合清单还包含已停用的可复用槽，不是活动测量页，故改正观测定义为 `carousel.measuredPageIndices`。未改参考组件、容差、图像或采样时间；初次错误观测、保留页位置差异等探索报告仍保存。Uncontained RTL 连续 Tab 可在两个视口搜索上限耗尽后回绕，这是固定 Core 实测结果，不以强制滚到焦点索引改写它。


2026-11-08 桌面交互与视口跟随增量：全解决方案构建通过；控件全量测试 2435 项、相关 Carousel/ScrollField 测试 75 项、VisualParity 系统测试 134 项通过。早期一次全量运行出现 AppBar 输入断言和五项测试清理异常，对应方法单独重跑 19 项及最终全量均通过；未修改这些无关组件。新增用法及 Catalog 焦点按钮、键盘/滚轮/缩窗演示说明齐备，无公开 API 破坏性迁移。

双端 self-test 共 181 例、4297 个采样，重复捕获稳定、执行错误为零；169 例通过原有门禁，12 例仍有差异。不是全部一致。报告位于 `artifacts/visual-parity/runs/<目录>/summary.md`：

| 目录 | 用例 | 通过 | 差异 | 覆盖 |
| --- | ---: | ---: | ---: | --- |
| `carousel-wheel-delivery` | 54 | 54 | 0 | 三变体、RTL、批量/反向/边界滚轮、禁用与取消 |
| `scroll-field-wheel-shared` | 10 | 10 | 0 | 共享算法抽取后的 ScrollField 回归 |
| `carousel-keyboard-phase` | 24 | 22 | 2 | PageUp/Down、重复按键及禁用 |
| `carousel-focus-phase` | 36 | 30 | 6 | 焦点请求、反向替换、导航取消、禁用 |
| `carousel-resize-delivery` | 18 | 14 | 4 | 缩窄、禁用下缩窄、动画中放大 |
| `carousel-fling-desktop-regression` | 21 | 21 | 0 | 页坐标消费调整后的 fling 回归 |
| `carousel-touch-desktop-regression` | 18 | 18 | 0 | 同一消费路径的真实触摸事件回归 |

剩余差异分别记录，不视为已接受的上游语义偏离：

- Uncontained 重复键盘分页的 LTR/RTL 两例、焦点导航取消的两例、LTR 视口变化的三例：结构化位置/尺寸一致，部分采样仍有裁剪/边缘栅格超差。已检查实际 diff 图；分离图层变换的实验未消除差异，已撤回，不放宽门禁。
- Uncontained `previous` / `reverse` 的 LTR/RTL 四例：固定 Core 未组合或停用的目标拒绝 FocusRequester，本地全部实现化的项目接受 Focus；RTL 还因此产生后续滚动差异。`focus.accepted` 直接采集真实结果，保留失败，不以屏蔽请求掩盖虚拟化缺口。
- MultiBrowse LTR `grow`：已有焦点动画在放大视口后 96–160ms 有短暂位置差异（96ms 为 Compose 141.5 DIP / Avalonia 141 DIP），240ms 起一致；中途重测与剩余弹簧目标更新仍待完全对齐。缩窄跟随及全部 RTL 尺寸用例的结构化事实一致。

尺寸夹具在输入后发送一帧，再执行布局，对齐 Compose 在该帧重组应用宽度的顺序；不在发送帧前额外强制 Avalonia 布局。固定参考组件、采样时间、探针语义与容差均未调整。探索失败报告保留。Catalog 真实窗口已验证滚轮与 PageDown；后续桌面控制由用户 Escape 停止，新焦点/缩窗功能由行为与双端测试覆盖，最新窗口人工验证仍未完成。

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
