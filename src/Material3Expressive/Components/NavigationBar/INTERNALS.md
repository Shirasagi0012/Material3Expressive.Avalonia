# NavigationBar 实现说明

## Token 配置路径

内部必需 spring 的 stiffness 必须大于零。模板首次挂载、重挂或资源暂时撤回时，未就绪配置不构造或提交运行时 spring；有效配置恢复后重新提交当前视觉目标。正在运行的通道继续使用已验证并捕获的规格，不回退 Expressive、Snap 或另一套默认值。

尺寸与运动规格由主题 Token 绑定到内部布局属性；按压映射读取同一有效指示器宽度。默认 Insets 也来自组件 Token。颜色配置与内部颜色/尺寸动画结果分开，保留固定 12 DIP 偏移与原始动画目标提交时机。

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX NavigationBar.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationBar.kt)
及 [Core NavigationBar.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationBar.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

原移植记录核对两个固定仓库的 NavigationBar.kt 相同；Row 分配还对照 Foundation 的
RowColumnMeasurePolicy 与 Arrangement.SpacedAligned。

| 上游符号 / 文件 | 本地职责 |
| --- | --- |
| NavigationBar、NavigationBarDefaults | [NavigationBar.cs](NavigationBar.cs)、[NavigationBar.axaml](NavigationBar.axaml)：ItemsControl、Surface 与物理 Insets |
| Row 权重分配 | [NavigationBarItemsPanel.cs](NavigationBarItemsPanel.cs)：权重、余数、无权重内容 |
| NavigationBarItem、NavigationBarItemLayout、placeIcon / placeLabelAndIcon | [NavigationBarItem.cs](NavigationBarItem.cs)、[NavigationBarItemLayout.cs](NavigationBarItemLayout.cs)：外部选择、独立内容槽、选择动画与实际布局 |
| tokens/NavigationBarTokens.kt、NavigationBarItemColors | [NavigationBarTokenSource.cs](NavigationBarTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.NavigationBar.cs) |

## 布局与动画不变量

- 模板组合 MaterialSurface、物理 Insets 和 ItemsPresenter。有限宽度默认填满，默认高度 80 DIP；
  `TonalElevation` 使用入口的 Level0，未将生成 Token 中未使用的 Level2 代入入口。
  Surface 保留累积 tonal elevation 及仅对主题 Surface 色施加 tint 的规则。
- NavigationBarItem 等权分配扣除 8 DIP 间距后的宽度，按上游 float 权重舍入，将余数逐个分给前面的项。
  数据模板生成的 Item 使用同样的布局。其他内容保留普通 Row 内容的无权重测量，居中排列在交叉轴上。
- ItemLayout 翻译独立 icon / label / indicator / ripple 测量及 placeIcon / placeLabelAndIcon。
  胶囊尺寸由实际 icon 尺寸加 Token padding 推导；纯图标项也使用 32 DIP 指示器高度。
  指示器选择动画从中心扩展。选中态标签与 icon 的组合居中，最少保留 4 DIP 垂直 padding；
  selected-only 标签随 icon 移动并淡入，尺寸进度为零时不绘制。
- Icon presenter 将 NavigationBar.IconSize Token 24 DIP 作为 MaterialIcon.DefaultSize 传给无显式尺寸的
  MaterialSymbol；显式尺寸和自定义图标布局不受槽约束。
- icon/text 颜色与指示器 alpha 使用 DefaultEffects，尺寸使用 FastSpatial；保留 spring 过冲。
  通过共享 `SelectionIndicatorAnimation` 使用四条 `AnimatedState`，目标合并、中断与 Scope 连接由 AnimatedState 管理。
  detach 和模板替换释放作用域，重挂载按当前选中/禁用状态初始化。
- 整个 Item 矩形参与输入，包括胶囊与标签的空隙；反馈只在胶囊区域绘制。
  复用已有共享交互控制器，按实际 Item 宽度与 Token 指示器宽度映射 `((itemWidth − 56) / 2, 12)`。
  这一偏移不由实际 ripple 位置、实际 icon 尺寸或标签位置反算。
- 状态颜色及 LabelMedium TextStyle 保留动态 System Token 引用，禁用色按 0.38 alpha 派生。
  容器 contentColorFor 角色匹配与未知色的父级 Foreground 回退使用 Foundation 的共享绑定，
  显式 Foreground 具有优先级。布局计算消费现有 MathUtils。

## 选择、语义与生命周期

Item 继承 Button，Click / Command 与选择值分离；Enter / Space 松开时请求激活。
自动化暴露 TabItem、外部选中状态和选择容器，Select 请求 Click。不要把 ISelectionProvider 的
CanSelectMultiple=false 误认为控件会强制各项互斥。显示标签时抑制 icon 子树描述，退场标签仍放置期间保留语义。

共享反馈与控件激活边界见[共享交互决定](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)，
实际焦点/indication 过滤见 [Core Desktop 焦点决定](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。
组件不得再建立另一套点击或 ripple 状态；模板替换与 detach 应释放旧动画作用域，重新挂载按当前状态初始化。

## 已确认适配

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationBar/README.md)；不以当前代码反推维护者授权。
2026-10-03 维护者选择“采用推荐 API 与内部共享适配”：Compose 内容 lambda 映射为内容对象与模板，
WindowInsets 为宿主提供的物理 Thickness、桌面默认零，保留外部选择和 Row 内容组合。
这是为 C#/XAML 提供明确内容与状态入口的适配；范围仅为常规 NavigationBar / NavigationBarItem，
不决定 Short 的 API，不引入应用路由，也不扩展 Wide 的纵向平滑偏离。

聚合排版、颜色转换与 Surface 职责分别见[排版决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)、
[颜色转换决定](../../../../docs/DECISIONS.md#composeanimation-颜色转换与原始动画终值)及
[架构说明](../../../../docs/ARCHITECTURE.md#surface-与交互组合)。当前依赖按中央版本和公共接入恢复，历史包号不作安装指引。

## 一致性边界

- 尚未提供 Compose 的嵌套 inset 消费环境；不能把 Scaffold 的 ConsumePadding 自动推广到此 Bar
- 旧双端完整自检的结构化事实全部相等，图像在退选、按压中禁用、RTL 按压有容差内差异。
  退选 16ms 的原始 4× 图有 32 个边缘像素不同，最大通道差 4；另外两项各 2 像素、最大差 1。
  当时重新采集双端并自检稳定，未修改容差；这是特定采样的历史观测，不是严格像素一致或当前回归结果
- 真实文字、Material Symbols 栅格、桌面人工操作和屏幕阅读器体验未经该几何 parity 验收

## 验证入口

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~NavigationBarTests
./tests/VisualParity/parity.ps1 prepare-reference --family navigation-bar
./tests/VisualParity/parity.ps1 run --family navigation-bar
./tests/VisualParity/parity.ps1 self-test --family navigation-bar
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[NavigationBarTests.cs](../../../../tests/Material3Expressive.Tests/NavigationBarTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/NavigationBarCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/NavigationBarCases.kt)是已跟踪的复现入口。
Catalog 入口为 [MainWindow.axaml](../../../Material3Expressive.Catalog/MainWindow.axaml)。

测试覆盖权重/余数、数据模板、实际内容尺寸与约束、七项颜色、内容色继承及 tonal elevation；
动态用例覆盖选择反向、两种 motion、全矩形 hover、按压/取消/快点、禁用、换主题、键盘和非标准图标按压映射。
两端调用真实组件，使用同样事件顺序与连续 16ms 帧网格；几何标签/图标用于隔离字体栅格。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationBar/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。

## 共享选择动画（2026-10-15）

Bar 与 Rail 使用 [SelectionIndicatorAnimation](../NavigationBar/SelectionIndicatorAnimation.cs)。
组件负责选择目标颜色、绑定布局进度和将采样应用于模板部件；共享对象只持有四条状态、提交规格与目标并统一释放。
同类型布局属性通过 `AnimatedState.Bind` 建立 Animation 优先级连接；颜色到画刷和透明度裁限仍由呈现端处理。
调用方不另建子 Scope，不缓存 pending 标记，也不包装每条轨迹的异步任务。

本次保留原有生命周期：模板替换或 detach 释放整个选择动画身份，重新挂载按当前状态初始化。
这不同于长期保留 AnimatedState、仅替换输出连接的用法，是当前导航组件已有的重置语义。

AnimatedState 的新运行在到达帧进入采样，不再额外等待原实现的 NextFrameAsync 路径；
静止端点与 spring 参数保留。当前 AnimatedState 还会在目标替换时接续速度，
旧本地实现则先 Stop 清零速度。维护者已确认采用当前 AnimatedState 默认行为，
组件不添加中断或等帧补偿；固定上游的实际中断轨迹仍待独立研究，不据此声称逐帧相等。
本次没有修改 Compose 参考或比较容差，也没有重新执行双端视觉验证。
