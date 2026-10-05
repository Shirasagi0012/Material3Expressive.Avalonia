# SplitButton 实现说明

使用方法见 [README](README.md)。父级只有布局和槽生命周期，原生子按钮保持独立输入与状态。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [SplitButton.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SplitButton.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SplitButton.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `SplitButtonLayout` | [SplitButton.cs](SplitButton.cs) 与 [SplitButtonLayout.cs](SplitButtonLayout.cs) |
| `SplitButtonDefaults.LeadingButton` / `TrailingButton`、shapes/contentPadding/iconSize helpers | [SplitButton.axaml](SplitButton.axaml)、[SplitButtonTokenSource.cs](SplitButtonTokenSource.cs)、共享 [ButtonContentRow](../Button/ButtonContentRow.cs) |
| `HorizontalCenterOptically.kt` | [SplitButtonOpticalLayout.cs](SplitButtonOpticalLayout.cs) |
| checked/pressed shape 与 border 动画 | 共享 Surface、[ButtonSurfaceState](../ToggleButton/ButtonSurfaceState.cs)、[AnimatedButtonBorder](../ToggleButton/AnimatedButtonBorder.cs) |
| Button 颜色/elevation 默认值 | [Button.cs](../Button/Button.cs) 及四组子主题 |

## 配置流与默认值归属

SplitButtonTokenSource 保存两槽间距和五档形状/尺寸/Padding；颜色与完整 ButtonElevation 配置继续
复用 Button 族 Token。父级 Spacing 从 `Comp.SplitButton.Spacing` 经主题 Setter 成为有效属性，
模板传给 SplitButtonLayout；两个类型的属性元数据不再重复保存 2 DIP Material 默认值。

子按钮的尺寸相关 Padding/形状、禁用色与描边条件先写入内部 [ButtonThemeProperties](../Button/ButtonThemeProperties.cs)，
再通过主题级绑定形成原生属性和 `MaterialButton.PressedCornerRadius` 的有效配置。
普通用户 Style、绑定、局部值均可覆盖这些默认；移除后恢复当前尺寸、状态和资源环境的值。
容器高度、图标尺寸等模板专用配置直接进入内部部件；父级 48 DIP 留白使用共享
`Comp.Button.MinimumInteractiveSize`，子区仍显式关闭各自的留白，保留 48 DIP 最小宽语义。
XS 图标间距经 `Comp.ToggleButton.ExtraSmallIconSpacing` 引用共享 4 DIP Token。

维护者 2026-10-05 确认本轮仅调整配置归属和覆盖机制；光学校正、当前圆角/描边动画与输入状态仍属于
运行时呈现。不得因统一配置路径把 Split 的保留动画策略改成 Toggle 的配置更换重置策略。

## 测量顺序与光学位置

按固定布局顺序先取得 Trailing 首选宽度，再查询剩余宽度下 Leading 的高度，确定共同高度，
随后测量 Trailing 和剩余 Leading。Avalonia 无通用 intrinsic 接口，组件以对应宽高约束调用实际子控件
Measure/DesiredSize 作为适配；不能仅取两个预先缓存的 DesiredSize 替代。
Spacing 按物理像素舍入；RTL 由逻辑布局镜像。极窄约束下保留 Trailing 溢出，不增加裁剪或压缩补偿。
父级槽更换同步 LogicalChildren，使用者提供的 Control 不能同时挂入其他父级。

尺寸 Token 虽为 32/40/56/96/136，Leading/Trailing helper 的 defaultMinSize 仍保留 Small 的 40 DIP。
SplitButtonMinimumHeightConverter 保留这层 max 关系，因此默认 XS 背景高 40，子按钮最小宽 48。
父级整体承担 48 DIP 最小交互布局空间，子按钮不重复加入；所有档标签仍用 LabelLarge。

Leading 与 Trailing 模板均消费原生 Button/ToggleButton 上的 `MaterialButton.Icon` / `IconTemplate`；
非空 Icon 或 Template 都激活图标槽，未设置时沿用 `Content` 路径。Leading 图标提示尺寸跟随 Button 为
20/20/24/32/40 DIP，Trailing 为 22/22/26/38/50 DIP；XS 间距使用上游 `iconSpacingFor` 的 4 DIP，
其他档位复用 Button 间距 Token。图标-only 时不加入标签间距。Leading 保留原外层 Decorator Padding，
Trailing 保留 SplitButtonOpticalLayout 的 Padding 与光学校正；附加图标不会改变两个默认 Padding 表。

Leading 外侧全圆、内侧小角，Trailing 镜像；按压改变内侧角，选中 Trailing 用 CircleShape。
形状使用 DefaultEffects；配置改变保留当前上游动画状态，spring 改变才重建，
不能将 ToggleButton 的 ResetOnShapeChange 规则直接套用。未改变当前目标的静止配置更新不应取消选中动画。
选中 overlay 在内容/交互层上绘制 contentColor×0.10，禁用也保留。

光学校正消费动画角值，系数 0.11，按逻辑 Padding 限制，再舍入到物理像素。
上游外侧角是 100% CornerSize，选中 CircleShape 是 50%；光学角在绘制轮廓归一化之前计算。
若先把半径夹到可绘轮廓，虽最终圆角外观可能相近，内容位置仍会错误。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/SplitButton/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-04，维护者确认双 Control 槽 API，不复用或继承 Avalonia SplitButton。
这样父级只承担布局，实际子按钮保留原生命令、事件、焦点、绑定、Flyout 和自动化，不建立转发层。
尾部 ToggleButton 的即时状态、nullable/三态、Enter 按下等接受范围见
[ToggleButton 的确认记录](../ToggleButton/INTERNALS.md#已确认的组件选择)。
颜色/elevation 和反馈复用既有 Button/Surface 能力。Split 主题新增消费共享图标附加槽；尾部
`ToggleButtonAppearance.Icon` / `IconTemplate` 是 `MaterialButton` 属性的兼容别名，仍解析到同一属性 identity。
`ToggleButtonAppearance.Colors` 和 `CheckedCornerRadius` 不属于 Split 子主题的配置入口。

共享描边的像素 ceil、内缩及非对称算法由
[Surface 圆角描边](../../../../docs/DECISIONS.md#surface-圆角描边)维护；
[桌面焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)和
[键盘职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)仍按原范围接入。

## 一致性边界

尚未实现任意 Compose Shape 和外部 MutableInteractionSource。静止/按压使用圆角矩形，
尾部选中使用默认圆形，不存在完整 Shape 对象移植。

旧 4× headless 静态场景在既定容差内；动态仍保留原生 Enter 和属性/主题/禁用当帧时机差异。
两个 hover 场景在 64ms 的 elevation 约有 0.0000003–0.0000006 DIP 残差，旧记录未将其认定为已接受；
不能因为 Chip 的精度决定相似就扩大授权。形状/光学位置的旧结论也不替代当前复跑。

文字、实际桌面操作、屏幕阅读器、非常大的非对称角值、运行中约束更换，以及任意自定义槽 intrinsic
行为未由现有无字形 Headless 场景证明。共享描边和其他组件的历史代表性回归范围见
[ToggleButton 维护说明](../ToggleButton/INTERNALS.md#一致性边界)，不宣称共享组件全部 parity 通过。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/ToggleSplitButtonTests.cs)：双区独立输入、尾部原生切换、共同布局、窄宽溢出、光学偏移和选中动画中修改非活动形状；同时覆盖 Toggle 生命周期；[ButtonConfigurationTests](../../../../tests/Material3Expressive.Tests/ButtonConfigurationTests.cs) 检查条件默认与用户 Style 优先级。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SplitButtonCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SplitButtonCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~ToggleSplitButtonTests
./tests/VisualParity/parity.ps1 self-test --family split-button
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。
