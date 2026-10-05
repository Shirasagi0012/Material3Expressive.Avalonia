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
  不回退到 FabLarge 原始 Token 32。显式 Width/Height 优先，`MaterialIcon.DefaultSize` 不强制缩放任意 Control
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
FabLabelClip.Gap 变化会失效自身测量缓存；Expressive 分支仅修改间距 Token 也会更新实际 label 位置和容器宽度。
Classic 整数宽度的记忆、目标取样和中断策略保持原逻辑，不随此缓存修正重新定义。
固定 elevation 进入/退出算法、Interrupted Visible→Visible 的默认 spring、整数居中及最小交互
48 DIP 保留为上游/已确认布局算法，不因出现常数而引入额外公共配置。

这些职责与无旧 Defaults 兼容包装按本次组件 Token 架构迁移确认；真实 whole-value
FloatingActionButtonElevation 保留，正常默认仍由主题的多值绑定动态组装。

## 动画、输入与生命周期

Elevation 记录最后一个实际 press/focus/hover，而非仅从相同高度倒推状态。
Tonal elevation 固定使用配置 Default；更换配置先 snap 当前目标，再处理新交互。
动画优先级绑定随 scope 释放，不能让多个 FAB 共用运行状态。

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

## 已接受暂缺与历史依赖问题

维护者于 2026-10-04 确认 IconSize / IconColor 绑定入口。Toggle FAB 单独提供 Icon / IconTemplate，
无图标时仍显示 Content；其默认图标尺寸提示固定为 Default/Medium/Large 24/28/36 DIP，不绑定动画中的 IconSize。
`IconSize` / `IconColor` 仍是显式 opt-in：调用方可绑定图标尺寸和颜色以复现 animateIcon，不能自动动画任意 Content。
任意位图或复合内容的整层 SrcIn tint 留作独立基础能力任务；任意内容仍继承环境 Foreground，不把显式图标绑定当作完整 animateIcon。

原移植修正过 ComposeAnimation spring 预计时长的整数毫秒截断，否则 threshold=1 的菜单 stagger
会在帧边界多延后一帧。该原因须保留，但旧本地包版本不是当前安装要求，也不能根据依赖升级省略反向/中断回归。

## 验证与边界

[FloatingActionButtonTests](../../../../tests/Material3Expressive.Tests/FloatingActionButtonTests.cs) 覆盖尺寸、
显隐、状态请求/automation、颜色角色、图标绑定、整数放置、受限菜单焦点、detach / reattach。
[FloatingActionButtonTokenConfigurationTests](../../../../tests/Material3Expressive.Tests/FloatingActionButtonTokenConfigurationTests.cs)
补充无文字的动态 Token、条件主题优先级、整函数覆盖、资源/主题替换和重挂载检查。
[Avalonia 用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/FloatingActionButtonCases.cs)与
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/FloatingActionButtonCases.kt)
覆盖四尺寸/内容重载、主题、约束、RTL、elevation、输入、展开/反向、Toggle、菜单增项和显隐。

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
