# ToggleButton

使用 Avalonia 原生 `ToggleButton`，提供 Filled、Tonal、Elevated、Outlined 四种 Material 主题。
依据 [固定基线](../../../../docs/upstream-baseline.json) 的 `ToggleButton.kt`、`Button.kt`、
`internal/AnimatedShape.kt` 与组件 Token；这些组件文件在固定 AndroidX/Core 提交中相同。

## 使用与默认值

```xml
<ToggleButton Content="Pin" IsChecked="{Binding IsPinned}"
              Theme="{StaticResource MaterialFilledToggleButtonTheme}" />
<ToggleButton Content="Pin" Classes="medium"
              Theme="{StaticResource MaterialTonalToggleButtonTheme}"
              m3:ToggleButtonAppearance.CheckedCornerRadius="16"
              m3:MaterialButton.PressedCornerRadius="12">
  <m3:ToggleButtonAppearance.Icon>
    <m3:MaterialSymbol Symbol="PushPin" />
  </m3:ToggleButtonAppearance.Icon>
</ToggleButton>
```

主题键为 `MaterialFilledToggleButtonTheme`、`MaterialTonalToggleButtonTheme`、
`MaterialElevatedToggleButtonTheme`、`MaterialOutlinedToggleButtonTheme`；隐式主题为 Filled。
默认 Small，`xsmall` / `medium` / `large` / `xlarge` class 选择其他尺寸。
背景最低高度为 32/40/56/96/136 DIP，外层保留至少 48 DIP 的交互布局空间，
没有普通 Button 的 58 DIP 默认最小宽度。显式尺寸与 MinWidth/MinHeight 优先。

`Content` / `ContentTemplate` 是标签槽，`ToggleButtonAppearance.Icon` / `IconTemplate` 是可选图标槽。
图标固定盒尺寸和间距遵循 `ButtonDefaults.iconSizeFor` / `iconSpacingFor`；XS 间距为 4 DIP，
不是原始 ButtonXSmall Token 的 8 DIP。自动字符串标签复用 Button 的内容环境与受约束排版。
图标内容需要使用前景色时，显式绑定 `Foreground` 或使用 `MaterialSymbol`。

`ToggleButtonAppearance.Colors` 可指定六槽颜色；null 使用实时主题 Token。
`Background`、`Foreground` 的局部值仍按 Avalonia 属性优先级覆盖默认主题。

```csharp
var toggle = new ToggleButton { Content = "Pin", IsChecked = false };
ToggleButtonAppearance.SetCheckedCornerRadius(toggle, new CornerRadius(12));
MaterialButton.SetPressedCornerRadius(toggle, new CornerRadius(6));
ToggleButtonAppearance.SetColors(toggle, new ToggleButtonColors(
    ContainerColor: Colors.LightGray, ContentColor: Colors.Black,
    DisabledContainerColor: Color.Parse("#1A1C1B1F"), DisabledContentColor: Colors.Gray,
    CheckedContainerColor: Colors.Navy, CheckedContentColor: Colors.White));
```

## 状态与适配

按压形状优先于选中形状，选中形状优先于静止形状。静止值来自 `CornerRadius`，选中目标来自
`ToggleButtonAppearance.CheckedCornerRadius`，按压目标复用 `MaterialButton.PressedCornerRadius`。
Small 的默认按压角为 6 DIP。形状使用 FastSpatial，描边宽度使用 FastSpatial 的 Dp 弹簧
（默认可见阈值 0.4 DIP），实色描边颜色使用 DefaultEffects。形状配置变化按上游重建状态，
反向与第三目标中断复用共享 AnimatedCornerRadius；绘制轮廓在插值之后按边界归一化。
Elevation、状态层、ripple 和生命周期复用已有共享能力。禁用颜色不区分选中状态。
描边按上游将动画宽度向上取整为物理像素，圆角中心线减去半个线宽。
焦点与 indication 的桌面输入模式过滤见[共享决定](../../../../docs/DECISIONS.md)。

2026-10-04，维护者确认采用原生 ToggleButton 和上述附加外观 API。
状态由原生控件切换，Click/Command 观察切换后的值，IsChecked 保留原生双向绑定默认值；
Enter 按下激活，Space 默认松开激活，自动化沿用原生 ToggleButton peer。
`IsChecked=null` 按未选中显示，原生三态为本地扩展。Compose 的受控 `checked/onCheckedChange`
调用关系通过绑定表达，属性写入当帧与 Compose 下一次重组更新的时机不同。
这些适配也适用于 SplitButton 的原生尾部 ToggleButton。

当前公开形状入口只表达圆角矩形，没有任意 Shape/CutCornerShape 入口；未提供 Compose
`MutableInteractionSource` 的对象映射。外层不裁剪，内部 Surface 仍裁剪背景、内容与反馈。

## 验证

独立双端用例为 `ToggleButtonCases.cs` / `ToggleButtonCases.kt`，实际调用产品主题和真实 Compose API。
覆盖四种外观、五种尺寸、选中/未选中、禁用、暗色、六槽自定义颜色、图标、空内容、RTL、尺寸约束，
以及选择、按压、释放、反向、第三目标、配置更新、取消、快速点击、禁用中断、键盘、主题更新。
颜色标记替代字体；这些测试不证明字体栅格、屏幕阅读器或真实桌面体验一致。
2026-10-04 的 4× Headless self-test 完整执行 104 例、306 个采样点：7 equal、76 withinTolerance、
21 different，执行错误为 0，全部普通重复捕获与额外被动截图检查稳定。80 个静态用例均 equal 或
withinTolerance。参考端与 Avalonia 都是本轮执行，没有复用旧参考或仅重比较。
不同结果保留原生 Enter 按下激活及属性/主题/禁用更新当帧的时机差异；三个 hover 用例在 64ms 的
共享 elevation 读数仍有约 0.0000003–0.0000006 DIP 精度残差，这项残差未认定为已接受差异。
颜色标记场景中的后续默认形状、光学位置、状态层与收尾帧已对齐到既定容差。

控件库 516 项和 Parity 框架 123 项测试通过，Release solution（含 Catalog）构建通过。
共享 Button/IconButton/CheckBox/RadioButton/Switch/NavigationBar 的 14 例自检均执行成功且稳定；
其中 8 例 equal/withinTolerance，另外 6 例保留现有 elevation、IconButton 40/48 DIP 分配及
Switch 松开后的 raster 差异，不能宣称共享组件全部 parity 通过。
非常大的非对称圆角、运行中更改约束与任意自定义内容排版组合尚未验证完整 Shape 等价性。

产物：[双端概览](../../../../artifacts/visual-parity/runs/toggle-button-port-complete/summary.md)、
[验证与保留问题](../../../../artifacts/test-results/toggle-split/validation.md)、
[控件测试](../../../../artifacts/test-results/toggle-split/toggle-split-controls.trx)。
