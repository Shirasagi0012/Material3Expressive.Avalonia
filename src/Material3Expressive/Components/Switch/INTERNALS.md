# Switch 实现说明

使用方法见 [README](README.md)。原生 ToggleSwitch 负责状态、输入和自动化，模板连接固定 Switch 视觉。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [Switch.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Switch.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Switch.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `Switch`、`SwitchImpl`、`SwitchDefaults`、`SwitchColors` | [Switch.axaml](Switch.axaml)、[SwitchAppearance.cs](SwitchAppearance.cs) |
| `SwitchTokens` v0_210 与禁用色 Surface 合成 | [SwitchTokenSource.cs](SwitchTokenSource.cs)、[SwitchTrack.cs](SwitchTrack.cs)、[Token 入口](../../Tokens/MaterialTokens.Comp.Switch.cs) |
| `ThumbNode` 的测量、尺寸/偏移动画与 thumbContent | [SwitchThumbLayout.cs](SwitchThumbLayout.cs) |
| 最小交互布局与本地外部标签组合 | [SwitchLayout.cs](SwitchLayout.cs) |
| Core `material3-ripple/{CommonRipple,RippleAnimation}.kt` | 共享交互控制器与 RippleAnimation；组件只连接几何与 presenter |

## 模板契约与动画交接

`PART_SwitchKnob` / `PART_MovingKnobs` 保留原生拖动契约；Canvas.Left 是原生拖动位置，
不是 Material 动画输出。SwitchTrack 按背景、拇指内容及反馈、描边的顺序绘制，轨道 52×32、描边 2 DIP。
拇指尺寸/偏移遵循 ThumbNode：首次测量惰性建立 Animatable，物理像素截断，FastSpatial spring，
按压 SnapSpec，普通中断延续速度。原生 KnobTransitions 不控制这两条动画。
颜色按状态即时选取；默认禁用色预合成 Surface，显式 SwitchColors 已是最终色。

原生拖动开始后，位置直接跟随 Canvas.Left，停止旧偏移动画。释放时原生依次清除 Canvas.Left、
移除 `:dragging`、写回 IsChecked；不能在第一步就以旧状态启动回程。
SwitchThumbLayout 保留最后显示位置与尺寸，等同帧状态更新后把两条 Animatable 同步到该姿态并清零旧速度，
再以 FastSpatial 收尾。端点释放只收缩拇指；物理截断仍可造成小于 1.5px 的中心量化变化。
捕获丢失或禁用同样从当前姿态返回，恢复下一次原生拖动位置，卸载取消待交接任务。

波纹与状态层仍由共享层拥有。按压测量先读取当前尺寸再启动 snap，因此内部
`InitializeRippleDrawGeometry` 保留这次测量决定的起始半径；Switch 单独启用
`RippleFollowsPresenterCenter`，让每次绘制中心跟随正在放大、收缩或拖动的 thumb。
起始/目标半径、扩散/淡出和生命周期不由组件复制。相关确认及范围只在
[Ripple 几何决定](../../../../docs/DECISIONS.md#ripple-首次绘制几何与尺寸动画衔接)维护。

## 已确认的组件选择

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Switch/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-03，维护者先接受保留原生拖动，随后确认直接采用 ToggleSwitch、模板和附加属性。
最终方案复用原生状态写回、Click/Command 顺序、Enter 按下 / Space 默认松开、3 DIP 拖动阈值和半程选择。
固定 Compose 没有相同 swipe，拖动改变状态也不产生原生 Click/Command；接受仅针对这些具体语义。
nullable IsChecked / IsThreeState 和原生 Toggle 自动化身份保留，null 外观仍为 unchecked。
固定 Avalonia 12.1.2 peer 报告 Button + TogglePattern，没有另建 Role.Switch 映射。

同日维护者要求“按下瞬间放大继续保持现状，拖动和动画的交接需要补齐”。上面的交接只处理原生拖动，
不改变普通切换、按压 SnapSpec、释放 spring 或公开 API。

维护者亦选择“本次保留原生范围，记录 null 回调模式未实现（推荐）”，并明确仅交付默认 ripple/focus。
原生控件没有同时移除输入、48 DIP 槽与 ToggleProvider 的开关；不新增独立只显示控件，
不能把关闭命中/Focusable/MaterialInteraction 当成完整 null 回调模式。
共享焦点和键盘边界另见 [DECISIONS](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。

## 一致性边界与历史观测

尚未实现完全等价的 `onCheckedChange=null`、可配置 `Focus.InsetRing` 与外部 MutableInteractionSource。
真实 OS 无障碍客户端、手工桌面输入和文字视觉未经双端验收。

双端场景读实际 ThumbNode 布局，不复制拇指算法生成期望值；覆盖图标/空内容、preferred/required
内容约束、RTL、切换/反向、禁用、主题、按压中断及原生拖动。拖动检查释放前、释放后未送帧、首帧与收尾，
并包含端点释放、中途反向、拖出取消与动画后拖动。波纹还覆盖动画后 unchecked 按压、图标与 RTL。

旧记录中波纹同心修复后，静态场景完全相等，动态仍保留原生输入及已确认中心跟随造成的不同；
原有场景的状态、回调和 thumb 几何未变，共享调用方代表性回归的 YAML/PNG 与此前产物相同。
这只是当时覆盖范围的观察，不是当前全库或跨平台通过声明。默认/有界 ripple 中心不能随 Switch 改动。

## 验证入口

- [InteractionTests.cs](../../../../tests/Material3Expressive.Tests/InteractionTests.cs)：
  首次绘制的 ripple 半径、跟随 presenter 中心的选择，以及默认/有界中心不受 Switch 选项影响
- [控件测试](../../../../tests/Material3Expressive.Tests/SwitchTests.cs)：实际模板、绑定/命令顺序、键盘/自动化、LTR/RTL 拖动与取消、当前姿态交接、颜色/内容、波纹同心（含慢放）与模板/卸载清理。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SwitchCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SwitchCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~SwitchTests
./tests/VisualParity/parity.ps1 self-test --family switch
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。
