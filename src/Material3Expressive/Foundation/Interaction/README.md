# 交互反馈与内缩焦点环

组件主题通过 `MaterialInteraction.IsInteractive` 接入原生控件的交互反馈。
点击、键盘激活和自动化仍由原生控件负责。默认焦点反馈为 Opacity 状态层。

`MaterialInteraction.FocusFeedback` 可继承，设为 InsetRing 后使用内缩双描边：

```xml
<StackPanel m3:MaterialInteraction.FocusFeedback="InsetRing">
  <Button Content="保存" CornerRadius="24,8,0,16" />
  <ToggleSwitch />
  <Button Content="保留透明状态层"
          m3:MaterialInteraction.FocusFeedback="Opacity" />
</StackPanel>
```

命名空间 `m3` 为 `https://github.com/Shirasagi0012/Material3Expressive.Avalonia`。
焦点环不占布局、不接受输入，也不使用原生 FocusAdorner；实际焦点不等于一定显示焦点环。
Desktop indication 按输入模式过滤焦点：首次鼠标聚焦不显示，键盘导航聚焦显示，
程序化聚焦沿用窗口当前模式。输入模式变化本身不生成焦点事件。

默认先绘制 inner（内缩 1 DIP、宽 3 DIP、OnSecondary），然后绘制 outer
（内缩 0 DIP、宽 2 DIP、Secondary）。两条描边有重叠，不能当成互不重叠的边框。
颜色跟随当前 Token；可通过可继承的 `FocusRingOuterColor` / `FocusRingInnerColor`
覆盖，null 恢复实时主题颜色。尺寸通过不可变 `InsetFocusRingMetrics` 配置，单位 DIP：
宽度为 0 时遵循上游 hairline 语义，绘制一个物理像素，不表示关闭该描边。

```csharp
MaterialInteraction.SetFocusFeedback(panel, FocusFeedback.InsetRing);
MaterialInteraction.SetFocusRingMetrics(panel, new InsetFocusRingMetrics(
    OuterStrokeInset: 0, OuterStrokeWidth: 2,
    InnerStrokeInset: 1, InnerStrokeWidth: 3));
MaterialInteraction.SetFocusRingOuterColor(panel, Colors.Red);
MaterialInteraction.SetFocusRingInnerColor(panel, Colors.White);
```

进入使用 SpatialFast，退出使用 EffectsFast。一个进度同时缩放线宽和内缩量，保留 spring
过冲和中断速度；每帧按物理像素执行上游取整，并非透明度淡入淡出。
首次订阅只观察后续交互，不回放外部 InteractionSource 的旧 starts。
Desktop 焦点过滤计数属于各次订阅，重建或卸载时清除。固定 Core 收到订阅前 Focus 的
Unfocus 时仍直接递减计数；因此新订阅先收到旧 Unfocus 后，第一个新 Focus 可能被过滤。
这是固定上游的边界行为，由 `source-old-end` 双端用例保留，不把旧计数带入新订阅来补偿。
Surface / Switch 切换焦点模式会重建 indication；CheckBox / RadioButton 只更新现有 ripple
配置，保留活动波纹、状态层和焦点进度。
固定上游在仅改变主题色时不主动使静止环的绘制缓存失效：绑定值立即更新，画面在下一次
动画、尺寸变化等绘制失效时读取新颜色。本地保留该时机；显式颜色或尺寸配置变更会触发重绘。

## 接入范围

交互 Surface 内部承担默认呈现，Button 及复用同一表面的模板不必逐个增加焦点环节点。
支持矩形、统一圆角与四角不等的圆角矩形；形变时跟随 Surface 当前有效圆角。
Switch 在 track 上绘制环，thumb 的 Surface 仅承担原有 ripple / 状态层。
CheckBox 显式使用 24 DIP 反馈区域的 25% 圆角，RadioButton 使用该区域的圆形轮廓。

这不代表所有 Surface 调用方已通过独立的组件视觉验证。导航、菜单、组合控件等仍须按各自
上游形状、坐标映射及裁剪核对。Slider / RangeSlider 的专用 inset padding 和轨道间距分支
仍未实现；任意 Path 形状不在当前能力范围。

维护职责、验证入口和限制见 [架构说明](../../../../docs/ARCHITECTURE.md)，
组件特殊结构见 [Surface](../../Components/Surface/INTERNALS.md) 和
[Switch](../../Components/Switch/INTERNALS.md)。

## 验证入口

[InsetFocusRingTests](../../../../tests/Material3Expressive.Tests/InsetFocusRingTests.cs) 检查主题与局部覆盖、
Desktop 输入模式、快速反向、Source 配对/重建、卸载取消、跟随形变、布局不变及
100% / 125% / 150% / 200% 物理像素取整。
双端 [FocusRingCases](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/FocusRingCases.cs)
以真实 Button、Switch、CheckBox、RadioButton 对照固定 Compose API，覆盖稳定外观及进入/退出、
过冲、反向、hover/失焦交错、尺寸、禁用、主题和模式切换。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~InsetFocusRingTests
./tests/VisualParity/parity.ps1 self-test --family focus-ring --require-match
```

`self-test` 实际采集双端并检查重复性，不消费参考缓存。equal、withinTolerance、
执行完成和重复性稳定是不同结论；真实桌面 GPU、跨屏 DPI 切换与其他组件组合仍需分别验证。
