# CheckBox 实现说明

使用入口见 [README](README.md)。本组件只替换原生 CheckBox 的外观，不接管其状态、命令和自动化。

## 默认配置数据流

指示器画布尺寸与外留白由组件 `IndicatorSize` / `IndicatorPadding` Token 同步传入内部布局、Surface 和绘制；布局槽取画布加两侧留白，并分别与最小交互宽高比较。默认值仍为 20 DIP 与四边 2 DIP，描边、波纹和焦点配置独立保留。

内部 motion 输入以 null 表示尚未绑定；挂载、重挂或资源临时失效时不构造无效弹簧，恢复后提交当前目标。已有动画保留其已捕获规格，不以 Expressive 常量补齐配置。

勾号形变及选中/取消颜色动画规格由组件 Token 引用系统 motion，经主题传入 Indicator 配置属性；动画只读取当前有效规格，不在内部重新解析 Token 或补 Expressive 默认。完整 CheckBoxColors 与描边的 null 语义保持不变。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [Checkbox.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Checkbox.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Checkbox.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `Checkbox` / `TriStateCheckbox`、`CheckboxImpl` | [CheckBox.axaml](CheckBox.axaml) 隐式原生主题 |
| `CheckboxColors`、`CheckboxDefaults` 与 Checkbox Token | [CheckBoxAppearance.cs](CheckBoxAppearance.cs)、[CheckBoxTokenSource.cs](CheckBoxTokenSource.cs)、[Token 入口](../../Tokens/MaterialTokens.Comp.CheckBox.cs) |
| `minimumInteractiveComponentSize`、padding、requiredSize | [CheckBoxLayout.cs](CheckBoxLayout.cs) 区分指示器与可选内容 |
| `CheckboxImpl` 的 Transition、勾号路径与绘制分支 | [CheckBoxIndicator.cs](CheckBoxIndicator.cs)；Skia 绘制与 VisualAnimationScope 生命周期 |
| Core `CanvasDrawScope` / `SkiaBackedPaint` | 指示器中 Float 物理像素路径、Stroke 的实际 Paint 配置 |

## 模板、状态与绘制不变量

固定基线 `isCheckboxStylingFixEnabled=false`：画布为 20 DIP、外留白 2 DIP、圆角 2 DIP、
默认布局槽 48 DIP。不提前采用 flag 开启后的 18 DIP 画布或新勾号坐标。
`DisabledCheckmarkColor` 保留公开槽，但此分支仍用 checkedCheckmarkColor 绘制禁用勾号。
透明色使用透明黑，不能用 Avalonia 透明白替换而改变颜色插值。
默认描边按 `floor(2 * density)` 取物理像素宽度，显式 Stroke 则将 DIP 直接乘 density；
Dashes / DashOffset 也是绝对 DIP，不是 Avalonia Pen 的笔宽倍数。

CheckBoxLayout 的受限居中先按正常 48 DIP 槽舍入，再对受约束偏移截断，不能合并为一次连续计算。
有 Content 时透明根布局让整行参与命中；无 Content 时只保留默认 24×24 DIP 指示器命中区域。
该组合对应上游 `CheckboxWithTextSample` 把 toggleable 放在外层行上的用法，
不意味着 Checkbox Composable 自带标签槽。RTL 镜像内容行，指示器绕过自动镜像以保留勾号方向。

Indicator 不写原生状态。勾号片段以 Skia PathMeasure 绘制，draw fraction 和中心吸附共享 Transition：
进入选中/不确定使用 DefaultSpatial；离开至 false 的相关分支保留 100ms Snap delay。
颜色消费 Animatable，选中使用 DefaultEffects，未选中使用 FastEffects；
禁用 box/border 直接到目标，重新启用时从新目标建立颜色状态，勾号颜色仍走自身动画。
目标在共享时钟的相应重组帧合并；只有状态目标改变才启动 Transition 帧循环，完成即取消等待，
避免 idle 异步唤醒让首帧注册漂移。detach 和模板替换释放订阅、帧等待与动画。

Surface 与共享交互控制器提供无界、固定 20 DIP 半径的 ripple。输入、命令和自动化仍属于
原生 CheckBox；不要用共享反馈控制器再次切换状态。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/CheckBox/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-03，维护者要求合并为原生 CheckBox，并确认“这两方面保留原生状态切换和键盘行为即可。按这个方案实施”。
因此复用两态/三态循环、默认双向绑定、状态先于 Click/Command 的顺序和 Enter 按下激活，
避免两套相似控件及转发 API。Compose 的外部状态写回、三态只请求动作及 Enter 松开激活不同；
这一接受仅覆盖原生适配，不批准其他视觉或语义偏离。

自定义描边另有明确例外：固定 Core 的 SkiaBackedPaint 缓存初始 Join 为 Round，底层实际为 Miter；
首次请求 Round 可跳过底层赋值。独立 Paint 诊断将 `checkbox.static.stroke.round.on` 差异定位在勾号转角。
维护者同日要求“这是上游 bug，始终按参数画出圆角”。本地始终将 Cap / Join / MiterLimit 写入真实 Paint，
默认 Miter 不受影响；仅适用于 CheckBox 自定义描边，不构成修复所有上游 bug 的通用授权。

同日维护者选择“本次保留原生范围，记录 null 回调模式未实现（推荐）”。原生控件没有移除
ToggleProvider 的开关，因此本阶段不增加独立只显示指示器，也不伪装完全等价的 null 回调模式。

共享反馈与桌面焦点由[键盘职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)及
[Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)维护。

## 一致性边界

可选 InsetRing 通过共享呈现部件接入，FocusRingCornerRadius=6 对应固定 24 DIP 反馈区域的
RoundedCornerShape(25%)；背景与无界 ripple 形状保持原有输入，不将焦点形状当成背景裁剪。
配置与生命周期规则见 [Interaction](../../Foundation/Interaction/README.md)。
固定 Checkbox.kt 使用同一个 ripple node；切换 Opacity / InsetRing 仅更新配置，
不清空按压波纹、状态层或焦点交互。共享呈现部件在两种模式下均维持同一次订阅。

尚未实现任意 PathEffect 的跨框架映射与完全等价的 null 回调模式。
关闭 `IsHitTestVisible`、`Focusable`、`MaterialInteraction.IsInteractive` 只阻止相应输入与反馈，
不会移除原生 ToggleProvider。

双端场景覆盖六种状态转换/反向、第三目标、标准 motion、主题/禁用、粗线/虚线/端帽/连接、
RTL 和尺寸约束。`dynamic.input.enter`、`dynamic.input.tri-cycle` 与 `static.stroke.round.on`
仍呈现上述已接受差异，参考端不重写原生循环或绘制以消除它们。
文字视觉、实际 OS 无障碍客户端和真实桌面手工输入未经双端验收；无字形 parity 不能证明这些能力。

## 外部交互源

`CheckBoxAppearance.InteractionSource` 将源通过模板传到 Indicator，挂载后连接已有交互控制器。
null 使用该模板实例的内部源；替换源重建 indication，清除旧 ripple 和状态层，不重放旧事件。
卸载/模板替换断开源，原生未结束输入通过对应 Cancel/Exit 事件结束。选择和三态循环仍由原生控件负责。
此入口对应固定 CheckboxImpl 的 triStateToggleable interactionSource 参数，两份固定上游文件一致。

`SelectionInteractionSourceTests` 覆盖身份配对、不激活、原生事件、替换及重挂。
双端 `selection-source` 对 boolean / tri / radio 覆盖外部 press、重叠、hover/drag、替换、禁用、
重挂和 null 回退。三态 null-native 保留已批准的原生三态循环差异。
重叠 ripple 和禁用期间的部分 Compose 重复/额外截图出现像素不稳定，本地重复捕获稳定；
不是整族严格通过的声明，也没有改变比较器容差。普通按压与重挂等场景的布局、选择和回调一致。
2026-11-03 产物 `artifacts/visual-parity/runs/20261103-101906-e7957986` 共 21 项：
13 equal、7 withinTolerance、1 different（三态循环）；零执行错误。3 项参考端稳定性检查差异
均已检查为最多 1 个通道值，本地 repeat/extraImages 稳定。

## 验证入口

- [SelectionControlHitTests.cs](../../../../tests/Material3Expressive.Tests/SelectionControlHitTests.cs)：
  有内容整行的指示器留白、间隙、Padding 命中；LTR/RTL、内容模板、禁用、取消及动态增删 Content
- [控件测试](../../../../tests/Material3Expressive.Tests/CheckBoxTests.cs)：原生状态/绑定写回与事件顺序、键盘/自动化、内容/RTL、颜色 Token、idle 重启和模板/卸载清理。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/CheckBoxCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/CheckBoxCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~CheckBoxTests
./tests/VisualParity/parity.ps1 self-test --family checkbox
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。
