# NavigationRail 实现说明

## Token 配置路径

内部必需 spring 的 stiffness 必须大于零。模板首次挂载、重挂或资源暂时撤回时，未就绪配置不构造或提交运行时 spring；有效配置恢复后重新提交当前视觉目标。正在运行的通道继续使用已验证并捕获的规格，不回退 Expressive、Snap 或另一套默认值。

尺寸与运动规格由主题 Token 绑定到内部布局属性；按压映射读取同一有效宽度，不再重新查询 Token。颜色仍由公开颜色属性输入，动画只写内部呈现结果。固定按压偏移、布局舍入、时序及选择状态所有权不变。

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX NavigationRail.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationRail.kt)
及 [Core NavigationRail.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationRail.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

原移植记录核对两个固定仓库的 NavigationRail.kt 相同；交互、ripple 与 animate*AsState 提交顺序另按固定 Core 核对。

| 上游符号 / 文件 | 本地职责 |
| --- | --- |
| NavigationRail、NavigationRailDefaults | [NavigationRail.cs](NavigationRail.cs)、[NavigationRail.axaml](NavigationRail.axaml)、[NavigationRailLayout.cs](NavigationRailLayout.cs)：header、ItemsPresenter、尺寸与 inset |
| NavigationRailItem、placeIcon / placeLabelAndIcon | [NavigationRailItem.cs](NavigationRailItem.cs)、[NavigationRailItemLayout.cs](NavigationRailItemLayout.cs)：外部选择、内容槽、布局与动画 |
| NavigationRailItemColors、tokens/NavigationRailTokens.kt | [NavigationRailTokenSource.cs](NavigationRailTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.NavigationRail.cs) |

## 布局、反馈与动画

- `NavigationRail.axaml` 组合 MaterialSurface、header、Insets Border 和 ItemsPresenter。
  `NavigationRailLayout` 保留填满有限高度、80 DIP 最小宽、上下各 4 DIP、item 间距 4 DIP、
  header 到第一项 16 DIP 的关系。
- `NavigationRailItemLayout` 翻译 placeIcon / placeLabelAndIcon：独立测量 icon、label、
  动画指示器与完整 ripple 区域；保留实际 icon 尺寸推导 pill、至少 4 DIP 垂直留白、
  受约束布局和按物理像素舍入。selected-only label 在 sizeProgress 为零时不绘制。
- Icon presenter 将 NavigationRail.IconSize Token 24 DIP 作为 MaterialIcon.DefaultSize 传给无显式尺寸的
  MaterialSymbol；自定义图标仍按实际测量值布局，不通过外层尺寸属性裁限。
- Item 的 icon/text 颜色、指示器透明度和尺寸通过共享 `SelectionIndicatorAnimation` 使用四条 `AnimatedState`。
  EffectsDefault 控制颜色与透明度，SpatialFast 控制尺寸；尺寸只下限裁到零，保留过冲。
  目标合并、反向中断与 Scope 连接由 AnimatedState 管理；不再由组件手动等帧或启动动画任务。
  detach / 模板替换释放作用域，重挂载按当前目标初始化。
- 复用共享 ripple 与状态层。MappedInteractionSource 按 token 宽度差映射 `(12, 0)`，
  不减去实际 pill 位置。无标签的 pill/ripple 为圆形；有标签使用 ActiveIndicatorShape。
  透明度在独立合成层处理，避免把 graphicsLayer alpha 提前量化进背景或标签画刷。
  模板布局容器使用透明背景，让整个 Item 矩形参与 hover 与点击命中，包括图标与标签之间的空隙、
  胶囊外的留白；反馈仍仅绘制在胶囊内，对齐上游外层 Box.selectable 与内层 indication 的分工。
- `NavigationRailTokenSource` 保留动态 System Token 引用和禁用色 0.38 alpha 派生。
  容器默认内容色按上游 contentColorFor 的角色顺序匹配，未知容器色继承父级 Foreground；
  明确设置 Foreground 具有优先级。Item 的图标/标签颜色与 ripple 的容器环境色各自消费。

Item 的外部选择值与 Button 激活分离，Space / Enter 松开请求 Click；无论 Click、Command 还是 automation Select
都不修改 IsSelected。标签退场时 icon 语义立即恢复，仍被放置的 label 保留语义直到尺寸进度裁到零。
ISelectionProvider 报告状态，不替应用维护互斥。

共享[交互激活职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)、
[桌面焦点过滤](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)及
[聚合排版](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)仍由 Foundation 维护。

## 已确认适配

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationRail/README.md)；不以当前代码反推维护者授权。
2026-10-03 维护者确认“采用推荐 API 与内部适配”，包括内容/模板、外部选择、物理 WindowInsets
和固定 `(12, 0)` 的按压映射。内容 lambda 需要映射为 Avalonia 的内容对象/模板，桌面 inset 由宿主提供；
这保留 selectable 外部状态语义，并支持 C#/XAML 组合。范围仅为常规 Rail，不决定 Wide/Modal API，
也不创建应用导航框架或通用嵌套 inset 环境。

原记录中的反向选择颜色问题已通过共享 ColorConverter 修正；原因、精度范围及批准依据以
[颜色转换决定](../../../../docs/DECISIONS.md#composeanimation-颜色转换与原始动画终值)为准，不在组件内另复制修复规则。
当前包接入以公共文档和中央版本为准。

## 已有观测与未验证范围

- 历史退选 16ms 的 4× 图仅指示器边缘 32 像素不同，最大通道差 7；1× 最大差 1，Item 区域均值 0.00223
- 历史反向 160ms 的 4× 图有一个 G 通道差 1 的像素，1× 精确相等；结构化事实相等，既有容差未调整
- 旧完整 suite 是单次双端捕获，只有反向中断另行做了重复自检。不能将该单项稳定扩展为当时整套新包捕获均已自检
- 已修复的颜色场景为反向 65ms：真实图标色两端均为 `[46,43,58,255]`，该帧截图也相等。
  原先 size-progress 临时诊断因内部浮点表示差异移除，最终几何、实际颜色、回调、语义和截图仍检查反向中断
- 原记录同时记有共享 Button 回归的旧失败与未扩大修改范围。这属于历史上下文；Button 后续行为以
  [Button INTERNALS](../Button/INTERNALS.md)为准，不将旧圆角、Enter 或禁用色失败当作当前事实
- 真实文字/Material Symbols 栅格、桌面人工操作及屏幕阅读器体验未经几何 parity 验收；窄 Rail 没有采用 Wide 的连续化纵向布局

## 验证入口

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~NavigationRailTests
./tests/VisualParity/parity.ps1 prepare-reference --family navigation-rail
./tests/VisualParity/parity.ps1 run --family navigation-rail
./tests/VisualParity/parity.ps1 self-test --family navigation-rail
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[NavigationRailTests.cs](../../../../tests/Material3Expressive.Tests/NavigationRailTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/NavigationRailCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/NavigationRailCases.kt)是已跟踪的复现入口。
Catalog 入口为 [MainWindow.axaml](../../../Material3Expressive.Catalog/MainWindow.axaml)。

[NavigationRailHitTestingTests](../../../../tests/Material3Expressive.Tests/NavigationRailHitTestingTests.cs)另覆盖实际命中区。
主测试涵盖 Token、布局、命令/CanExecute、自动化、动态内容色、模板替换与重挂载。
双端覆盖三种标签策略、selection/enabled、Header、RTL/Insets、内容尺寸、受限布局、状态色和两种 motion，
并以同一事件帧与 16ms 网格检查按压、取消、禁用、主题和键盘输入。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationRail/README.md)，复核以跟踪测试和新生成报告为准。
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
