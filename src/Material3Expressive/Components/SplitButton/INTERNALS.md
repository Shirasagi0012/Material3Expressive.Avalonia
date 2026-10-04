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
| `SplitButtonDefaults.LeadingButton` / `TrailingButton`、shapes/contentPadding/iconSize helpers | [SplitButton.axaml](SplitButton.axaml)、[SplitButtonTokenSource.cs](SplitButtonTokenSource.cs) |
| `HorizontalCenterOptically.kt` | [SplitButtonOpticalLayout.cs](SplitButtonOpticalLayout.cs) |
| checked/pressed shape与border动画 | 共享Surface、[ButtonSurfaceState](../ToggleButton/ButtonSurfaceState.cs)、[AnimatedButtonBorder](../ToggleButton/AnimatedButtonBorder.cs) |
| Button颜色/elevation默认值 | [Button.cs](../Button/Button.cs) 及四组子主题 |

## 测量顺序与光学位置

按固定布局顺序先取得Trailing首选宽度，再查询剩余宽度下Leading的高度，确定共同高度，
随后测量Trailing和剩余Leading。Avalonia无通用intrinsic接口，组件以对应宽高约束调用实际子控件
Measure/DesiredSize作为适配；不能仅取两个预先缓存的DesiredSize替代。
Spacing按物理像素舍入；RTL由逻辑布局镜像。极窄约束下保留Trailing溢出，不增加裁剪或压缩补偿。
父级槽更换同步LogicalChildren，使用者提供的Control不能同时挂入其他父级。

尺寸Token虽为32/40/56/96/136，Leading/Trailing helper的defaultMinSize仍保留Small的40 DIP。
SplitButtonMinimumHeightConverter保留这层max关系，因此默认XS背景高40，子按钮最小宽48。
父级整体承担48 DIP最小交互布局空间，子按钮不重复加入；所有档标签仍用LabelLarge。

Leading外侧全圆、内侧小角，Trailing镜像；按压改变内侧角，选中Trailing用CircleShape。
形状使用DefaultEffects；配置改变保留当前上游动画状态，spring改变才重建，
不能将ToggleButton的ResetOnShapeChange规则直接套用。未改变当前目标的静止配置更新不应取消选中动画。
选中overlay在内容/交互层上绘制contentColor×0.10，禁用也保留。

光学校正消费动画角值，系数0.11，按逻辑Padding限制，再舍入到物理像素。
上游外侧角是100% CornerSize，选中CircleShape是50%；光学角在绘制轮廓归一化之前计算。
若先把半径夹到可绘轮廓，虽最终圆角外观可能相近，内容位置仍会错误。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/SplitButton/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-04，维护者确认双Control槽API，不复用或继承Avalonia SplitButton。
这样父级只承担布局，实际子按钮保留原生命令、事件、焦点、绑定、Flyout和自动化，不建立转发层。
尾部ToggleButton的即时状态、nullable/三态、Enter按下等接受范围见
[ToggleButton的确认记录](../ToggleButton/INTERNALS.md#已确认的组件选择)。
颜色/elevation和反馈复用既有Button/Surface能力，不代表独立ToggleButton外观附加属性在Split主题有效。

共享描边的像素ceil、内缩及非对称算法由
[Surface 圆角描边](../../../../docs/DECISIONS.md#surface-圆角描边)维护；
[桌面焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)和
[键盘职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)仍按原范围接入。

## 一致性边界

尚未实现任意Compose Shape和外部MutableInteractionSource。静止/按压使用圆角矩形，
尾部选中使用默认圆形，不存在完整Shape对象移植。

旧4× headless静态场景在既定容差内；动态仍保留原生Enter和属性/主题/禁用当帧时机差异。
两个hover场景在64ms的elevation约有0.0000003–0.0000006 DIP残差，旧记录未将其认定为已接受；
不能因为Chip的精度决定相似就扩大授权。形状/光学位置的旧结论也不替代当前复跑。

文字、实际桌面操作、屏幕阅读器、非常大的非对称角值、运行中约束更换，以及任意自定义槽intrinsic
行为未由现有无字形Headless场景证明。共享描边和其他组件的历史代表性回归范围见
[ToggleButton维护说明](../ToggleButton/INTERNALS.md#一致性边界)，不宣称共享组件全部parity通过。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/ToggleSplitButtonTests.cs)：双区独立输入、尾部原生切换、共同布局、窄宽溢出、光学偏移和选中动画中修改非活动形状；同时覆盖Toggle生命周期。
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
不得修改门禁来隐藏差异。上述命令是复现入口，不表示本次文档整理已执行它们。
