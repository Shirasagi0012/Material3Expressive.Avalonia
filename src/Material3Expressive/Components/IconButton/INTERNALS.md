# IconButton 实现说明

使用方法见 [README](README.md)。四种主题共享 IconButton 类与 ButtonLayout/Surface，Symbol 是默认模板的内容入口。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [IconButton.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/IconButton.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/IconButton.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

默认值还需核对固定上游 `IconButtonDefaults.kt`；普通与 vibrant helper 不能混用。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `IconButton` / `FilledIconButton` / `FilledTonalIconButton` / `OutlinedIconButton` | [IconButton.cs](IconButton.cs) 的 Button 派生类与 [IconButton.axaml](IconButton.axaml) 四种主题 |
| `IconButtonDefaults` 颜色、尺寸/形状与 outline 默认值 | IconButton.cs 的尺寸/四外观 TokenSource、[Token 入口](../../Tokens/MaterialTokens.Comp.IconButton.cs) |
| XS/Small/Medium/Large/XLarge IconButton Token | 尺寸 class、MaterialSymbol 模板部件与 [ButtonLayout](../Button/ButtonLayout.cs) |
| fixed shape / pressed shapes 与 `AnimatedShape` | MaterialButton.PressedCornerRadius → MaterialSurface → 共享 AnimatedCornerRadius |
| clickable/indication | 原生 Button 负责激活，共享 MaterialInteractionController 负责反馈 |

## 配置流与覆盖优先级

尺寸与四外观 TokenSource 保存基线/系统 Token 引用；IconButton.axaml 把它们接成主题默认配置。
共享 [ButtonThemeProperties](../Button/ButtonThemeProperties.cs) 承接尺寸和禁用条件的候选，
公开 Background/Foreground、描边、CornerRadius 及 XS Width/Height 通过主题级绑定消费。
条件选择器不直接把普通用户 Style 覆盖掉；Style、绑定与局部值仍由 Avalonia 形成有效配置。
ButtonLayout 和 MaterialSymbol 只消费模板传入的尺寸，不再读取另一份组件默认值。

XS 的 32 DIP 是主题选出的 Width/Height 默认值；普通 Style 可以改成 44，清除局部值后再按当前
class 和资源恢复。ButtonLayout 的共享 48 DIP 留白来自 `Comp.Button.MinimumInteractiveSize`，
容器宽高仍由 IconButton 自己的尺寸 Token 提供，不能误用普通 Button 的 58 DIP 容器最小宽。
资源、主题或挂载作用域改变时保留实时 Token 绑定；形状动画的当前呈现不写回配置属性。
此组织按维护者 2026-10-05 的配置迁移确认实施，不解决或追认下文的 ambient 颜色、描边和受限图标差异。

## 模板与布局不变量

Standard 主题持有完整模板，Filled/Tonal/Outlined 以 BasedOn 修改颜色/描边；隐式主题为 Standard。
模板为 ButtonLayout → MaterialSurface#PART_Container → MaterialSymbol，没有 ContentPresenter。
Symbol=None 保留真实模板与测量，不应通过删除部件模拟空图标。
Surface 通过 TextElement.Foreground 重新接入 owner 内容色，防止自身隐式主题遮蔽图标色。
状态选择器顺序 hover < focus-visible < pressed，只选墨色，透明度/ripple 时序仍归共享控制器。

尺寸采用 32/40/56/96/136 DIP 容器、20/24/24/32/40 DIP 图标。
Small 默认容器 40、外层 48；XS 通过 owner Width/Height 的主题默认 32 约束，对应上游 size(32dp)。
ButtonLayout 持有默认容器尺寸而非写 owner MinWidth/MinHeight，不能用公开最小值重建布局。
受限布局要保留正常容器与外层的分别取整；说明与确认范围见
[最小交互布局](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)。

默认固定形状，显式 PressedCornerRadius 才形变；尺寸 class 只改变默认静止形状，不覆盖用户局部值。
共享形状的反向、第三目标和生命周期细节见 [Button 维护说明](../Button/INTERNALS.md#交互与动画职责)，
替换模板时不得留下旧 Surface 交互或动画绑定。

## 已确认的选择与未解决问题

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/IconButton/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-03 确认的固定圆角+可空按压目标同时覆盖四种 IconButton 主题，原因、原始确认及边界见
[Button 组件决定](../Button/INTERNALS.md#固定圆角加可空按压目标)。
原生 Enter 按下激活按[共享键盘职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)接受，
焦点/elevation 与 indication 关系按[Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。
2026-10-04 的布局修正仅覆盖最小交互分配及其读数，不代表下列问题已接受或修复。

**尚未实现：** narrow/wide 宽度选项、toggle 入口、Material 焦点环。
对应原始 Token 可以存在，不意味着模板已经消费或有公开入口。

**已观察到、尚未解决：**

- Standard/Outlined 普通 colors helper 读取 LocalContentColor；本地使用 OnSurfaceVariant，禁用色也未随 ambient 变化
- Outlined 默认 border 从 LocalContentColor 取色且固定 Small 的 1 DIP；本地 OutlineVariant，大尺寸仍消费原始 2/3 DIP
- 属性变更当帧的即时更新、更换按压目标后部分状态层像素仍有差异
- 受限 20 DIP 用例的容器已对齐，但本地 MaterialSymbol 仍可 24 DIP 而 Compose 为 20 DIP，默认图标可能溢出

这些事实未认定为已接受适配。IconButton.cs 中的历史“recorded deviation”注释只能说明实现，
不能代替维护者批准。FloatingToolbar 接入修正也未授权改变颜色、描边或阴影算法。

## 验证范围与历史观测

旧 FloatingToolbar 主线合入后的 4× headless 重采记录仍有不同用例；
与此前分支交付相比，用例/采样事实的分类状态未变，重复和额外被动截图稳定。
可见容器、Small48/XS32 外层分配和固定/显式目标的圆角已对齐，不等于全部视觉一致。
历史计数和临时报告只描述对应代码、依赖与环境；当前状态应通过跟踪用例重新采集确认。

双端用例覆盖四主题×五尺寸、round/square、暗色/禁用、ambient 内容色、RTL、空 Symbol、
约束、非对称圆角、描边、固定/变形按压、取消、中断、运行时更改和键盘。
为隔离字体，仅在夹具将模板字体绘制部件换为继承 MaterialSymbol 的向量箭头，保留测量、StyleKey、
模板关联、原始 Template 优先级尺寸与前景继承。产品尺寸 class 仍控制图标布局，Compose 调用真实组件 API。
矢量不自动镜像；测试不能证明 Material Symbols 字形、字体镜像策略或真实桌面显示效果。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/IconButtonThemeTests.cs)：实际主题、四外观、禁用/主题切换、尺寸 class 和交互接入；[ButtonConfigurationTests](../../../../tests/Material3Expressive.Tests/ButtonConfigurationTests.cs) 检查普通 Style 优先、XS 尺寸覆盖和清除恢复；Token 默认值另见下方测试。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/IconButtonCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/IconButtonCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~IconButtonThemeTests
./tests/VisualParity/parity.ps1 self-test --family icon-button
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。

[IconButtonTokenTests.cs](../../../../tests/Material3Expressive.Tests/IconButtonTokenTests.cs) 检查尺寸/颜色/描边 Token；
形状和 ButtonLayout 共享回归见 [Button 维护入口](../Button/INTERNALS.md#一致性边界与验证)。
