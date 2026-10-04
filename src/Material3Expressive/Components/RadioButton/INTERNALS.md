# RadioButton 实现说明

使用方法见 [README](README.md)。原生控件负责选中、分组和自动化，模板部件只负责绘制与动画。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [RadioButton.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/RadioButton.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/RadioButton.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| 公开 `RadioButton` / `RadioButtonImpl` | [RadioButton.axaml](RadioButton.axaml) 原生主题 |
| `RadioButtonColors`、`RadioButtonDefaults`、`RadioButtonTokens` v0_117 | [RadioButtonAppearance.cs](RadioButtonAppearance.cs)、[RadioButtonTokenSource.cs](RadioButtonTokenSource.cs)、[Token 入口](../../Tokens/MaterialTokens.Comp.RadioButton.cs) |
| requiredSize、padding 与 minimumInteractiveComponentSize | [RadioButtonLayout.cs](RadioButtonLayout.cs) |
| dotRadius / color 的 animateAsState 与圆环绘制 | [RadioButtonIndicator.cs](RadioButtonIndicator.cs) |

内部 StyleableRadioButton 不属于此处公开 API 的移植范围。

## 绘制、布局与生命周期

指示器画布 20 DIP，外留白 2 DIP，默认布局槽 48 DIP；外圈中心线半径 9 DIP、描边 2 DIP。
选中 dotRadius 动画目标为 6 DIP，实际填充半径减去半个描边即 1 DIP。
RadioButtonLayout 保留 requiredSize 的受限居中计算，没有将 48 DIP 强写为 owner MinWidth/MinHeight。
可选 Content 是本地组合，对应上游 RadioGroupSample 在整行使用 selectable 的方式；
透明根布局仅在有 Content 时让整行命中。RTL 由 Avalonia 镜像内容行。

dotRadius 使用 FastSpatial、颜色使用 DefaultEffects，目标在对应重组帧合并，重定向先取消旧任务。
传入的 motion spring 未指定 Dp threshold，固定 vectorization 取 0.01，不能误用未消费的
animateDpAsState 默认 dpDefaultSpring 的 0.1。禁用颜色直接到目标；重新启用从新目标建立颜色状态，
但禁用不停止选择圆点动画。VisualAnimationScope 负责注册、detach 清理与重新挂载。

默认色保留动态引用：Primary、OnSurfaceVariant、OnSurface×0.38、OnSurface×0.38。
Skia 使用 Float 物理像素运算；Surface 与共享控制器提供无界、固定半径 20 DIP ripple。
默认焦点使用 opacity indication，不能据此声明 inset focus-ring 分支已经实现。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/RadioButton/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-03，维护者选择“采用原生模板并接受上述范围（推荐）”：保留原生状态更新、组互斥、
Enter 按下激活和四槽颜色 API；状态/组处理先于 Click/Command，与 Compose 仅回调并由应用写回不同。
使用原生主题复用输入与自动化，不额外建立相似子类；接受范围不包含其他视觉差异。
同时明确本次不实现完全等价的 `onClick=null` 模式，原生选择自动化不能靠禁用普通输入移除。

另一个已确认视觉适配是消除负半径闪点。固定 Skiko `0.152.0-alpha02` 的 drawCircle 转到 drawOval，
负半径仍可通过排序后椭圆绘出中心点，使取消选择的尾段圆点消失后又闪现。
维护者选择“修正绘制，消除闪点（推荐）”：仅在最终物理绘制半径
`dotRadius * scale - strokeWidth / 2 > 0` 时绘制，保留弹簧状态、参数、阈值和时序。
正半径继续走相同椭圆路径，也消除了选中起始阶段小于 1 DIP 动画半径的幻影点。
这是有意偏离固定桌面基线，不应通过截断 spring 值或删掉尾段采样实现。

共享职责见[交互反馈与键盘激活](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)和
[Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。

## 一致性边界与历史观测

尚未实现 null 回调完全等价模式、外部 MutableInteractionSource、独立最小交互尺寸环境覆盖及
可选 inset focus-ring ripple 配置。文字视觉、实际 OS 无障碍客户端、真实桌面手工交互和非 Windows
平台未作双端验收。

双端 `dynamic.input.enter`、`dynamic.input.request-only` 保留已接受的原生差异。
两个 deselection-tail 场景对 Expressive / Standard 以 16ms 连续采样至 800ms，参考仍使用原始 Compose。
旧记录中闪点修正前的像素回归在 256ms 失败，正常和十倍时长均可复现；修正后的新增像素差异
局限于中心 1 DIP 半径区域，几何、选择、启用状态和回调计数保持相等。
这一观察不表示所有像素或所有平台相等；回归应保留尾段状态与实际像素两种读数。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/RadioButtonTests.cs)：分组、绑定/事件顺序、自动化、内容/RTL、禁用颜色独立规则、尾段 spring、正常/慢放闪点像素与 idle/模板生命周期。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/RadioButtonCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/RadioButtonCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~RadioButtonTests
./tests/VisualParity/parity.ps1 self-test --family radio-button
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。上述命令是复现入口，不表示本次文档整理已执行它们。
