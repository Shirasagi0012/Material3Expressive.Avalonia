# RadioButton

为原生 `Avalonia.Controls.RadioButton` 提供隐式 Material ControlTheme，移植固定 Compose 的公开
`RadioButton` 外观、颜色与动画。引入 `MaterialTheme.axaml` 后直接使用原生控件；
`MaterialRadioButtonTheme` 可显式引用或派生，不增加 RadioButton 子类。

## 使用与原生适配

```xml
<StackPanel>
  <RadioButton GroupName="delivery" Content="Standard"
               IsChecked="{Binding StandardSelected}" />
  <RadioButton GroupName="delivery" Content="Express"
               IsChecked="{Binding ExpressSelected}" />
</StackPanel>
```

```csharp
var radio = new Avalonia.Controls.RadioButton { GroupName = "delivery", IsChecked = true };
radio.Click += (_, _) => SaveSelection(radio.IsChecked == true);
RadioButtonAppearance.SetColors(radio,
    new RadioButtonColors(Colors.Teal, Colors.DarkGray, Colors.Gray, Colors.Gray));
// null 恢复动态主题颜色。
RadioButtonAppearance.SetColors(radio, null);
```

`IsChecked` 保持原生的可空、默认双向绑定属性；只有 true 绘制选中外观，false/null 均绘制未选中。
原生 Toggle 不会再次取消已选中的 RadioButton；`IsThreeState` 不改变这一单选行为。
相同非空 GroupName 在同一呈现根内互斥；不指定组名时，原生控件在同一逻辑父级的单选兄弟之间互斥。
状态更新和组互斥先于 Click/Command；Compose 则仅调用 onClick，由应用决定是否写回 selected。
Enter 在 KeyDown 激活，Space 在 KeyUp 激活；固定 Compose 的 Enter 在 KeyUp 激活。
输入、命令、access key 与选择自动化由原生控件负责，MaterialInteraction 只维护反馈。

2026-10-03，维护者确认“采用原生模板并接受上述范围（推荐）”：保留原生状态更新/组互斥和
Enter 按下激活；采用四槽颜色 API；本次不实现完全等价的 Compose `onClick=null` 模式。
原生控件始终保留选择自动化语义，禁用普通输入不能移除该语义；不以此伪装 null 回调模式。
该决定仅适用于本 RadioButton 的原生适配，不批准其他视觉差异。

Content 是已确认的 Avalonia 本地组合：48 DIP 指示器槽旁排列 ContentPresenter，默认间距 8 DIP，
Padding 控制内容留白，保留 ContentTemplate、内容对齐、access key 和 RTL。
Compose RadioButton 自身没有 Content 槽，应用通常在上层 Row 组合。

## 颜色、绘制与动画

`RadioButtonAppearance.Colors` 接收不可变 `RadioButtonColors`，对应 selected、unselected、
disabledSelected、disabledUnselected 四槽；C# record 的 `with` 可保留未覆盖槽。
未设置时动态消费 `MaterialTokens.Comp.RadioButton`：Primary、OnSurfaceVariant、
OnSurface × 0.38、OnSurface × 0.38。系统或组件资源覆盖与主题变化继续传播，不提前展开 Token。
Foreground 用于 Content、状态层和 ripple，与指示器颜色分开。

```xml
<RadioButton IsChecked="True">
  <RadioButton.Resources>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+RadioButton.SelectedColor}">#008080</Color>
  </RadioButton.Resources>
</RadioButton>
```

源码基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：
AndroidX 与 Compose Core 固定提交的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/RadioButton.kt` 相同，
Token 为 `RadioButtonTokens` v0_117；内部 StyleableRadioButton 不属于本次公开 API 移植。

- 指示器画布 20 DIP，留白 2 DIP，默认交互尺寸 48 DIP；外圈中心线半径 9 DIP、描边 2 DIP。
  选中动画目标 dotRadius 为 6 DIP，绘制半径再减 1 DIP。
- RadioButtonLayout 保留 requiredSize 与受限外部尺寸下的居中关系，未将 48 DIP 写入控件 MinWidth/MinHeight。
- dotRadius 使用 FastSpatial，颜色使用 DefaultEffects，消费现有 ComposeAnimation 本地 NuGet 包和
  VisualAnimationScope。目标在对应重组帧合并；固定 AnimateAsState 先取消旧任务，再启动新目标。
- 传入的 motionScheme spring 没有 Dp threshold，固定 vectorization 使用 0.01；
  不能套用 animateDpAsState 未被消费的默认 dpDefaultSpring 的 0.1。
- 禁用颜色直接到目标，重新启用的颜色从新目标初始化；禁用不停止选择圆点动画。
- 绘制使用 Skia Float 物理像素运算。Skiko 0.152.0-alpha02 的 Canvas.drawCircle 转发到 drawOval，
  负绘制半径仍画出经过排序的椭圆；本地显式排序椭圆矩形保留该桌面路径。
  因而选中/反向动画尾段仍可能出现小点，未擅自钳制半径或改变阈值。
- MaterialSurface 与共享交互控制器绘制不裁剪、固定半径 20 DIP 的 ripple、hover 和 focus 状态层。
  固定 RippleDefaults.ThemeConfiguration 使用 opacity focus；没有引入替代焦点环。

绘制需要 Avalonia.Skia。当前默认最小交互尺寸为 48 DIP；没有独立映射
`LocalMinimumInteractiveComponentSize` 的环境覆盖 API，应用仍可通过原生尺寸约束布局。
Compose 的外部 MutableInteractionSource 没有对象级跨框架映射；使用原生输入与 MaterialInteraction 观察反馈。
可选的全局 inset focus-ring ripple 配置属于共享能力未实现范围，不将默认 opacity focus 验证推广到该分支。

## 验证入口与边界

独立双端入口：

- [RadioButtonCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/RadioButtonCases.cs)
- [RadioButtonCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/RadioButtonCases.kt)
- [RadioButtonTests.cs](../../../../tests/Material3Expressive.Tests/RadioButtonTests.cs)

48 个 parity 用例：20 个静态覆盖选中/未选中 × 启用/禁用 × 亮暗主题、四槽自定义色、RTL、
自动尺寸与受限宽高；28 个动态覆盖双向选择/反向、标准 motion、禁用选择、转换中禁用、
禁用/启用、主题/颜色切换、按压/快速释放/取消、按压中禁用、禁用输入、指示器槽边缘点击、
hover、Space/Enter、已选中点击、RTL 输入、原生组互斥和回调不写回选择。
两端调用实际组件；参考端保留真实 Compose 回调所有权。
`dynamic.input.enter` 和 `dynamic.input.request-only` 如实保留已接受差异，不改比较门禁。

headless 测试检查真实模板、绑定写回/事件顺序、选择自动化、命名和隐式组、取消/禁用输入、
Content/ContentTemplate/RTL、动态 Token/颜色、独立禁用动画规则、尾段 spring 阈值、
idle 后重启、模板替换/卸载/重新挂载的清理。
文字视觉、实际 OS 无障碍客户端、真实桌面手工交互和非 Windows 平台未经双端验收。

2026-10-03 Windows 验证：控件库 Release 回归 387/387 通过，Catalog Release 构建通过；
RadioButton self-test 完成 48 个用例、317 个采样，46 个用例完全相等，另 2 个仅呈现上述已接受的
Enter 时序与回调不写回差异。没有执行错误或未经比较的字段，重复采集及增加被动图片采样均稳定。
该 self-test 重新采集两端，不发布参考快照，也未修改比较阈值。

在项目根目录运行：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release
dotnet build src/Material3Expressive.Catalog/Material3Expressive.Catalog.csproj -c Release
./tests/VisualParity/parity.ps1 self-test --family radio-button --output D:/Temporary/radio-button-parity
```
