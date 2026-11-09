# FloatingActionButton 维护说明

使用入口见 [README](README.md)。多个公开尺寸共享类型，但保留不同上游内容重载、布局与动画分支。

## 固定上游与职责

固定版本见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`；Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
Material3 前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游文件 / 符号 | 本地入口与职责 |
| --- | --- |
| [FloatingActionButton.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/FloatingActionButton.kt)：四尺寸 FAB / Extended FAB、`FloatingActionButtonDefaults` | [FloatingActionButton.cs](FloatingActionButton.cs)、[主题](FloatingActionButton.axaml)、[FabLayout](FabLayout.cs)与[ExtendedFabLayout](ExtendedFabLayout.cs) |
| FAB icon/content overloads | 基类 `FloatingActionButton.Icon` / `IconTemplate`；标准 FAB 在两者存在时用图标槽，否则保留原 Content 路径。Extended FAB 继承同一属性和状态 |
| `FloatingActionButtonMenu.kt`：`FloatingActionButtonMenu` / `FloatingActionButtonMenuItem` / `ToggleFloatingActionButton` | [菜单及条目布局](FloatingActionButtonMenu.cs)、[ToggleFloatingActionButton](ToggleFloatingActionButton.cs) |
| FAB / ExtendedFab / FabMenuBaseline 相关 tokens | [FloatingActionButtonTokenSource](FloatingActionButtonTokenSource.cs)及主题配置绑定；Composable 修正不覆盖原始 Token |
| `FloatingActionButtonElevationAnimatable`、`animateFloatingActionButton` | [FloatingActionButtonElevation](FloatingActionButtonElevation.cs)、[FabVisibilityLayout](FabVisibilityLayout.cs)、[FabMotion](FabMotion.cs) |

两个固定基线的组件实现按原移植记录一致；平台行为仍需按 Core 复核，不可据此假定完整平台一致。
动画依赖版本取[中央版本](../../../../Directory.Packages.props)，当前接入见[公共指南](../../../../docs/GETTING_STARTED.md)。

## 已确认 API 与布局约束

[拆分前确认记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/FloatingActionButton/README.md)
保留 2026-10-03 维护者批准的尺寸化按钮类型、菜单 Button/Items 槽、受控
IsChecked/CheckedProgress/CheckedChangeRequested 与 AnimatedVisible。
原因是用 Avalonia 类型/属性表达 Compose 多入口，不是合并不同动画语义。

- 图标 FAB 的容器为 56/40/80/96 DIP，Small 外层分配 48；Extended 为 56/56/80/96 DIP 高
- 基类 `Icon` / `IconTemplate` 使用同一 presence 条件（Icon 或模板非空），模板-only 也进入图标路径；Extended FAB
  通过继承复用属性。标准 FAB 无图标时保持 Content/ContentTemplate 的原自定义内容路径，不把普通 Button 的附加属性搬入 FAB
- 图标 Default/Small、Medium、Large 提示尺寸为 24/28/36 DIP；Large 的 36 是 `FloatingActionButton.kt` 的修正，
  不回退到 FabLarge 原始 Token 32。显式 Width/Height 优先，`MaterialSymbol.DefaultSize` 不强制缩放任意 Control
- Large 图标 36 DIP、Medium/Large Extended 的 12/16 DIP 间距保留 Composable 修正；不把原 Token 32 与 16/20 改掉
- Default Extended 使用整数宽度、分开的展开/收起 spring 与 alpha，返回 Visible 的中断默认 spring；Expressive 分支保留浮点宽度进度、FastSpatial/FastEffects，完全收起才移除文字槽
- 最小交互空间及内部对齐读数遵循[共享决定](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)，只预留布局空间

## 配置、运行状态与呈现

主题的 `FabSizeDefaultConverter` 只选择已经解析的变体值，公共属性的 Setter 保持 theme 优先级。
因此 CornerRadius、排版以及 Toggle 的函数默认值不会因 Size 的条件样式压过普通用户 Style。
默认 TokenSource 保存基线和引用关系，主题绑定随当前消费位置、资源替换、主题与挂载更新；
组件及布局没有 TokenResolver 或 Defaults 工厂回退。

Toggle 的三个公开函数属性是完整配置值。主题 `FabProgressFunctionConverter` 从动态端点组装函数，
保持 Float lerp 与 Oklab 颜色运算；闭合圆角仍在组装时由闭合容器尺寸除以二。
动画只更新 CheckedProgress，Refresh 读取函数结果写入 Surface 以及只读 IconSize / IconColor。
默认 icon hint 是独立配置，不随进度变化，也不强制改变任意 Icon / Content 的尺寸。
原始 FabLarge.IconSize=32 以及 Extended 的 16/20 图文间距不改写；独立 ContentIconSize /
ContentIconLabelSpace 保存上游 Composable 的 36 和 12/16 修正。
Toggle 初始圆角是上游的 16/20/28 常量，对应独立默认 Token，不伪造到 System Shape 的引用。

Visibility 规格、Toggle checked spring、Extended 各方向 spring 及菜单 reveal/stagger spring
由主题送入有效属性。`FabMotion` 仅把捕获的规格参数转换为 Float，阈值仍由动画量的单位决定。
当前运行的一段动画继续持有所捕获的规格，下次目标改变才消费新配置。默认值不重复写入属性元数据。
Nullable spring 表示主题配置尚未就绪；无效或缺失引用、零/非有限 spring 不得构造运行规格。
Visibility / Extended / Toggle 保留尚未提交的当前目标，配置恢复后经原共享下一帧调度重试；
菜单通过原 ruler/TargetValue 差异在布局中重试，不提前把目标标记为已提交。
Visibility 的公共 null 规格同样表示等待，两条通道同时就绪后才提交，不把 null 交给
Animatable 触发其内部默认 spring。detach 清理待提交状态，重新附着仍从当前公开目标初始化。

模板中手写的最外层 MultiBinding 明确使用 Template priority，内部属性的局部配置和 ClearValue
不会被绑定默认的 LocalValue 优先级遮住；ControlTheme Setter 内绑定仍由 Setter 确定优先级。
FabLabelClip.Gap 变化会失效自身测量缓存；修改间距 Token 会更新实际 label 位置和容器宽度。
Classic 稳态展开时按当前实测整数宽度同步保存值，避免旧动画宽度裁掉更新后的 label，
并让下次收起从实际显示宽度开始。依据固定 AndroidX/Core 的 `EnterExitTransition.kt`：
`trackActiveEnter` / `trackActiveExit` 在 Visible 稳态清除 active transition，`createModifier`
不再创建 sizeAnimation，measure 使用当前 measuredSize。这里不是额外的尺寸动画，也不是缺失规格回退。
在途展开重新测得完整宽度时，只重定向 classic 宽通道，保留当前整数值、向量速度和 alpha 任务。
这对应 `EnterExitTransition.measure` 每次以 fullSize 调用 DeferredAnimation，及固定 `Transition.kt`
的 `updateTargetValue` 仅在目标改变时重建动画。仅修改 spring Token 而宽度目标未变，不重启动画；
几何目标改变时才按当前 segment 选择规格，保留 Visible→Visible 的 400 spring 分支。
完成判断观察当前宽任务及原 alpha 任务；被尺寸重定向取消的旧宽任务不会使 transition 提前完成或永不完成。
待配置恢复的最新 extent 也参与完成条件。代次隔离反向/重新附着，settle 与 detach 都清除保存的任务引用。
未改变 ComposeAnimation 协议，也未新增公共 API 或通用动画调度器。
固定 elevation 进入/退出算法、Interrupted Visible→Visible 的默认 spring、整数居中及最小交互
48 DIP 保留为上游/已确认布局算法，不因出现常数而引入额外公共配置。

这些职责与无旧 Defaults 兼容包装按本次组件 Token 架构迁移确认；真实 whole-value
FloatingActionButtonElevation 保留，正常默认仍由主题的多值绑定动态组装。

## 动画、输入与生命周期

Elevation 记录最后一个实际 press/focus/hover，而非仅从相同高度倒推状态。
Tonal elevation 固定使用配置 Default；更换配置只在当前交互的目标高度改变时 snap，
仅修改其他状态的高度不会提前结束正在运行的动画。
动画优先级绑定随 scope 释放，不能让多个 FAB 共用运行状态。
交互流直接启动 elevation 动画，不额外排队一个重组帧；elevation 配置变更独立进入帧末提交。
运行值使用 Float Animatable，端点与中断值按 Dp 存储；共享 easing 求解器的精度边界保持不变。
`BindFrameCommittedValue` 先捕获本批显示高度，再执行 `ScheduleFrameCommit` 中的配置更新。
配置 snap 立即更新内部值，随后到来的交互从它启动；本帧 Surface 仍使用 effect 前捕获的值。
这是固定上游 `shadowElevation(...).value` 在重组中读取、`LaunchedEffect` 随后更新状态的顺序，
不是将配置和新交互一起延后一帧。共享决定见 [DECISIONS §26](../../../../docs/DECISIONS.md#26-重组消费的动画值与帧提交-effect)。
共享控制器仅在完整源事件结束时调用 FAB 的交互收集路径，构建 ripple 和属性更新不会伪造额外交互。
配置与交互分别对应固定上游 `updateElevation` / `animateElevation`；在等待配置提交时仍可处理真实交互。
中断沿用 Animatable 的当前值及最后帧时间。固定 Core 的自定义高度 2/4/6/9 DIP 用例在 64ms hover
读取约 6.295164 DIP，随后 press 的 80ms 采样约 6.180564 DIP；这些读数另有 headless 与双端探针覆盖。

普通与 Extended FAB 的 `InteractionSource` 直接连接共享交互控制器，null 使用私有源。
固定 `FloatingActionButtonElevation.animateElevation` 以 source 为 remember key，交互列表在 effect 内；
因此换源同时重建 indication 与 elevation animatable，从 Default 初始化，不复用普通 Button 跨源保留列表的路径。
开始对象按身份配对，外部交互不执行 Click。卸载断开订阅，重挂只接收未来事件。
固定 `FloatingActionButtonMenu.kt` 的 Toggle 与菜单项没有公开 interactionSource 参数，未增加对应 API。

Toggle 保留初始外层占位，内层朝逻辑 TopEnd 收缩；Oklab 颜色、动态圆角、固定阴影及显式
ripple 半径分别处理。内容按整数除法居中，图标尺寸按物理像素取整；反向 checked 按
AnimateAsState 取消并重启，不能用保留速度的普通 retarget 代替。
激活先发状态请求，后调用原生 Button；应用拥有 IsChecked，automation 的 IToggleProvider 同样只请求切换，禁用拒绝。

菜单保留底部开始的整数 stagger、条目独立 width/alpha、自然宽度和逻辑对齐。
增项在放置阶段初始化动画，条目 alpha 独立使其图层失效，不能依赖菜单总尺寸改变才重画。
受限列表使用内部 ScrollViewer/ScrollContentPresenter，保留原生滚轮、惯性与焦点带入视口，
收起期间关闭输入，Button 不滚动。显隐 alpha 为零才退出布局，部分透明时整层合成阴影和填充；
默认缩放枢轴在逻辑 BottomEnd 内缩一个物理像素，RTL 由视觉树镜像实现。

Button 命令、焦点与 activation 复用框架；[Enter 的共享适配](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)不改变本组件受控状态所有权。

## 显式容器形状

2026-11-08 本轮维护者授权补齐剩余 API 与必要共享能力。固定 AndroidX/Core FloatingActionButton.kt
一致，普通四尺寸及 Extended 的纯内容/图标文字重载均公开 shape；本地新增 FloatingActionButton.Shape，
Extended 继承，两个模板直接传至共享 Surface（决定 §35）。此前 CornerRadius 仅能表达圆角，
现有路径绘制、阴影、裁剪与 indication 能力已足够接入，不引入额外渲染层。默认 null 保留原 API，
无调用方迁移，不更改高度 collector、可见性或 Expanded 动画。切角是固定参数，展开过程中随实际
容器尺寸求值，不另加形状插值。固定 Toggle FAB 与菜单项没有公开 shape 参数，不扩大到这两种入口。

`FabShapeTests` 覆盖所有尺寸和普通/Extended、布局保持、真实激活、可观察形状修改、清除恢复、
百分比尺寸和卸载重挂；`fab-shape` 双端套件覆盖三种内容入口×四尺寸×三种形状，另测 RTL、约束、
暗色 tonal、收起、展开/反向、按压/取消、悬停与显隐反向。运行：
`./tests/VisualParity/parity.ps1 self-test -NoRestore --family fab-shape`。

## 已接受暂缺与历史依赖问题

维护者于 2026-10-04 确认 IconSize / IconColor 绑定入口。Toggle FAB 单独提供 Icon / IconTemplate，
无图标时仍显示 Content；其默认图标尺寸提示固定为 Default/Medium/Large 24/28/36 DIP，不绑定动画中的 IconSize。
`IconSize` / `IconColor` 仍是显式 opt-in：调用方可绑定图标尺寸和颜色以复现 animateIcon，不能自动动画任意 Content。
任意位图或复合内容的整层 SrcIn tint 留作独立基础能力任务；任意内容仍继承环境 Foreground，不把显式图标绑定当作完整 animateIcon。

2026-11-09 本轮维护者授权补齐缺失 API 与必要适配后，图片分支新增 `ToggleFloatingActionButtonIcon`。
原决定避免对任意 Content 隐式改色的理由继续成立，原 Icon / Content 路径不变。
固定两端 `ToggleFloatingActionButtonDefaults.animateIcon` 的 size 函数按 `roundToPx` 形成正方形固定约束，
color 函数对已录制层执行 SrcIn。本入口消费相同的 CheckedProgress、默认函数和物理取整，
通过已有 MaterialIcon 对 IImage 的实际绘制 alpha 施加整图 tint；可公开覆盖 Size / Color 函数。
绑定贡献在 Animation 优先级，卸载释放并恢复底层值；重挂只观察新的祖先 Toggle。
DrawingImage 的变更沿 MaterialIcon 已有 Invalidated 订阅传播；没有每帧轮询或额外时钟。
这完整覆盖位图/矢量/自定义 IImage 分支，但不是任意 Control 子树的 animateIcon 替代品。
后者仍未实现：Avalonia VisualBrush 不传播任意源子树的绘制失效，公共 Effect 也没有整体颜色滤镜入口；
不采用持续重录制或静态截图降低动态内容语义。

`ToggleFabIconTests` 验证默认尺寸、过渡跟随、自定义函数、密度取整、重挂、底层值恢复及图片语义。
`toggle-fab-icon` 双端用例覆盖三尺寸、三图片来源、选中态、RTL、受限布局、半透明重叠图像、
默认/自定义动画反向、图片替换与 DrawingImage 形状变更。
运行 `./tests/VisualParity/parity.ps1 self-test -NoRestore --family toggle-fab-icon`。

2026-11-09 `toggle-fab-icon-content-cache` 的 32 例全部重复稳定、零执行错误；30 例 withinTolerance，
所有几何、进度与内容宽度读数相等。2 个原位修改时间线保留真实截图差异：宽度从 20 改成 8 时，
Core 已重组且读数为 8，但 `animateIcon.drawWithCache` 录制层仍显示旧图；65ms 再触发 checked，
96ms 起缓存重录制后两端恢复容差内。直接 `MaterialIcon` 的原 32 例全部逐像素相等。
本轮用户授权下保留既有 MaterialIcon 的 DrawingImage.Invalidated 即时刷新合约，不故意缓存过期图片；
这项框架接入差异不扩展到其他动画时序，也不把 32 例整体称为视觉验收通过。原失败用例、截图与容差保持不变。
相关行为测试 71 项通过，Catalog 构建成功；实际屏幕阅读器与真实设备输入未经本单元验证。

原移植修正过 ComposeAnimation spring 预计时长的整数毫秒截断，否则 threshold=1 的菜单 stagger
会在帧边界多延后一帧。该原因须保留，但旧本地包版本不是当前安装要求，也不能根据依赖升级省略反向/中断回归。

## 验证与边界

`FabInteractionSourceTests` 检查普通/Extended 的多 press 身份、最后交互高度、换源重置、原生取消及重挂。
双端 `fab-source` 覆盖三种内容入口、四尺寸、RTL、重叠及换源；几何和真实截图使用既有 FAB 测量，
另被动读取双方实际 Surface 阴影高度，不用模拟反馈代替组件。
`fab-source-frame-commit` 曾确认配置提交修复，但保留 Tween 精度差异。
随后共享求根与 Float 插值补齐（[决定 §19](../../../../docs/DECISIONS.md#19-tween--bezier-精度历史保留与固定算法补齐)），
`fab-source-fixed-math` 的 27 例确认数值算法补齐后，仅换源当帧仍有高度差异。
本轮依据用户扩大授权，换源改为用现有 `ScheduleFrameCommit` 合并连接；它同时切换收集器、取消旧源
原生交互并重建 indication / elevation，对应上游 `remember(interactionSource)` 与 collector 的重组生命周期。
只延迟阴影初值会留下状态层/波纹和新源订阅窗口错误，因此没有采用单独延迟显示的替代方案。
首次挂载仍同步连接；同帧换回原源取消提交，卸载释放待提交句柄，重挂以最新属性初始化。
公共调用仍为 `fab.InteractionSource = next`；外部使用者不能再依赖 setter 返回时已经连接新源，
应使用已连接源表达交互，并遵守事件流不重放的契约。

`fab-source-lifecycle` 的 33 例全部结构化读数精确相等，24 withinTolerance / 9 different，执行错误为零。
新加的 replace-before-frame / replace-revert 验证同帧换源、旧/新/中间源事件和回退原源的实际反馈。
24 例重复稳定，9 例 Core 重复/额外截图仍不稳定，Avalonia 自检稳定。剩余 9 个不同用例都是高 elevation
配置场景的阴影绘制，不能记为整个套件通过。
随后由独立原生探针定位并修复共享曲线分支与物理像素路径（DECISIONS §20）。当前
`fab-source-native-curve` 为 2 equal、31 withinTolerance、0 different，所有结构化读数精确相等，
执行错误为零；26 例重复稳定，7 例 Core 重复/额外截图仍不稳定，Avalonia 自检稳定。
高高度图像失败已消除，重复性门禁仍如实保留，不能将整个 self-test 记为完全通过。

`configuration` 用例在同一 Compose elevation 工厂调用点修改参数。未使用状态高度更新时，两端都保留 hover 动画；
活动目标变更的旧失败由 `fab-config-current-batch` 重新复现。当前实现 224ms 两端精确为
39.23287582397461 DIP，240ms 均为 5；配置提交时序与这条路径的数值差异均已修复。
新增 configuration-interrupt 在 225ms 取消按压，240ms 均从 5 开始，256ms 均为
5.199723243713379，证明内部 snap 没有被一并延迟。configuration-coalesce 连续换配置只提交最后的 7。
三种 FAB 内容入口的上述九例均重复稳定，所有高度读数精确相等，不做取整。
该用例使用 40 DIP 自定义 pressed 高度，旧实现 208ms 的阴影截图超出门禁；共享阴影修正后
该图像回到既有容差内。微小高度余差与大阴影渲染差异分别定位，未扩大历史微小浮点接受范围。

[FloatingActionButtonTests](../../../../tests/Material3Expressive.Tests/FloatingActionButtonTests.cs) 覆盖尺寸、
显隐、状态请求/automation、颜色角色、图标绑定、整数放置、受限菜单焦点、detach / reattach。
[FloatingActionButtonTokenConfigurationTests](../../../../tests/Material3Expressive.Tests/FloatingActionButtonTokenConfigurationTests.cs)
补充无文字的动态 Token、条件主题优先级、整函数覆盖、资源/主题替换和重挂载检查。
[Avalonia 用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/FloatingActionButtonCases.cs)与
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/FloatingActionButtonCases.kt)
覆盖四尺寸/内容重载、主题、约束、RTL、elevation、输入、展开/反向、Toggle、菜单增项和显隐。
本次 elevation 生命周期修正后的既有 111 例复测为 2 equal、108 withinTolerance、1 different，
执行错误为零，重复捕获及额外被动截图稳定。剩余 `dynamic.menu.count` 在 append 事件当帧有结构差异；
这是阴影修正前的历史运行；本轮当前证据为上述新增配置与换源用例，不追认为全族当前已通过。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~FloatingActionButtonTests
./tests/VisualParity/parity.ps1 self-test --family floating-action-button --require-match
./tests/VisualParity/parity.ps1 prepare-reference --family floating-action-button
./tests/VisualParity/parity.ps1 run --family floating-action-button --require-match
```

拆分前自检记录中 FAB 场景重复采集稳定且位于既定 raster 容差内；这是旧基线环境记录，不自动适用于后续版本。
附加 `button.dynamic.hover.elevated` 的 Double/Float elevation 精确读数仍有差异，不能算该 Button 用例通过。
Raster withinTolerance 不等于原始像素相同；几何内容不评价文字栅格，headless 不替代真实桌面验收。
结果解释见 [VisualParity](../../../../tests/VisualParity/README.md)。

本轮新增 `fab-shape` 69 例（44 静态、25 动态）的双端自检记录在
`artifacts/visual-parity/runs/fab-shape-verified`：全部执行无错、重复捕获稳定；43 个静态形状在原门槛内，Extended 受限 32×32 场景仍有图像差异。Extended 的 RTL 反向动画与 content/extended 显隐反向的各一帧仍 different，
属于尚未消除的差异，不视为已接受；原始采样及门槛保持不变。
