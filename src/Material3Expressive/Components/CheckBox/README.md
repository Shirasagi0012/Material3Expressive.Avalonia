# CheckBox

本组件为原生 `Avalonia.Controls.CheckBox` 提供隐式 Material ControlTheme，
合并固定上游 `Checkbox` / `TriStateCheckbox` 的外观实现，不增加 CheckBox 子类。

## 使用

引入 `MaterialTheme.axaml` 后，普通 `CheckBox` 自动采用模板。
`MaterialCheckBoxTheme` 是可显式引用或派生的资源。
状态、双向绑定、Click/Command、Content/ContentTemplate、键盘和自动化仍由 Avalonia 负责。

```xml
<CheckBox Content="Accept" IsChecked="{Binding Accepted}" />
<CheckBox Content="Group" IsThreeState="True"
          IsChecked="{Binding GroupState}" Command="{Binding ToggleGroupCommand}" />
<CheckBox IsChecked="{x:Null}" IsThreeState="True" IsEnabled="False" />
```

```csharp
var checkBox = new Avalonia.Controls.CheckBox { IsChecked = true };
var group = new Avalonia.Controls.CheckBox { IsThreeState = true, IsChecked = null };
// Click/Command 发生时，原生 CheckBox 已更新 IsChecked；应用可继续写回聚合状态。
group.Click += (_, _) => group.IsChecked = CalculateGroupState();
```

`IsChecked` 默认双向绑定。两态循环 false → true → false；
`IsThreeState=true` 时 false → true → null → false。
Enter 在 KeyDown 激活，Space 在 KeyUp 激活。
这些原生语义由维护者确认保留，与 Compose 的外部状态写回、三态点击只发出请求、
Enter 在 KeyUp 激活存在差异。采用模板复用原生状态和交互，避免两套相似控件与转发 API。
2026-10-03，维护者要求合并为原生 CheckBox，并确认“这两方面保留原生状态切换和键盘行为即可。按这个方案实施”。
范围仅为本次两态/三态模板，不以此批准其他视觉或语义偏离。

Content 是 Avalonia 本地内容组合：48 DIP 指示器槽旁排列原生 ContentPresenter，
默认间距 8 DIP，Padding 控制内容留白，支持 RTL、内容模板和 access key。
Compose Checkbox 自身没有 Content 槽；通常由上层 Row 组合标签。
有 Content 时，模板根布局用透明背景让整个控件矩形参与命中，包括指示器槽、内容间隙与 Padding；
对应上游 `CheckboxWithTextSample` 把 `toggleable` 放在整行上的组合方式，行高仍使用本地已确认的 48 DIP 默认布局。
没有 Content 时保留默认 24 × 24 DIP 指示器鼠标命中范围；上游预留的 48 × 48 DIP 是布局空间，
不能等同于默认鼠标命中范围或触摸输入层的目标扩展。
当前 parity 用例按仓库规定不包含文字，内容布局另有 headless 检查。

## 颜色与描边

`CheckBoxAppearance.Colors` 接收不可变 `CheckBoxColors`，保留上游 13 个颜色槽。
不设置时动态消费 `MaterialTokens.Comp.CheckBox`，可在资源中覆盖单个槽。
`Foreground` 用于内容、状态层和 ripple，不替代勾号或边框颜色。

```csharp
window.Resources[MaterialTokens.Comp.CheckBox.CheckedBoxColor] = Colors.Teal;
CheckBoxAppearance.SetColors(checkBox, new CheckBoxColors(
    Colors.White, Color.FromArgb(0, 0, 0, 0),
    Colors.Teal, Color.FromArgb(0, 0, 0, 0),
    Colors.Gray, Color.FromArgb(0, 0, 0, 0), Colors.Gray,
    Colors.Teal, Colors.DarkGray, Colors.Gray, Colors.Gray, Colors.Gray, Colors.White));
CheckBoxAppearance.SetCheckmarkStroke(checkBox,
    new CheckBoxStroke { Width = 3, Cap = PenLineCap.Round, Join = PenLineJoin.Round });
CheckBoxAppearance.SetOutlineStroke(checkBox,
    new CheckBoxStroke { Width = 2, Dashes = [3, 2], DashOffset = 1 });
```

```xml
<CheckBox IsChecked="True">
  <m3:CheckBoxAppearance.CheckmarkStroke>
    <m3:CheckBoxStroke Width="3" Cap="Round" Join="Round" />
  </m3:CheckBoxAppearance.CheckmarkStroke>
</CheckBox>
```

Width、Dashes、DashOffset 使用绝对 DIP；dash 长度不是 Avalonia Pen 的笔宽倍数。
Cap、Join、MiterLimit 映射到 Skia Stroke。
默认笔宽保留上游 `floor(2 * density)` 的物理像素取整，显式 Stroke 则直接乘 density。
上游 Stroke 的任意 PathEffect 没有跨框架对象映射，目前提供 dash 参数。

Join 始终按参数设置实际 Skia Paint。固定 Compose 桌面基线中，SkiaBackedPaint 的
strokeJoin 缓存初值为 Round，底层却为 Miter；首次直接请求 Round 会跳过赋值而画成尖角。
维护者已确认本地正确画出圆角，并接受 checkbox.static.stroke.round.on 的转角差异；
固定依赖的独立 Paint 诊断已确认缓存 Round / native Miter，差异位于勾号转角。
2026-10-03，维护者明确“这是上游bug，始终按参数画出圆角”。Cap、Join、MiterLimit 始终传入实际 Paint；
默认 Miter 外观不受影响。范围仅为 CheckBox 自定义描边，不推广为其他上游 bug 的通用处理规则。

## 固定基线与实现

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)。
AndroidX 与 Compose Core 的 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Checkbox.kt`
在当前固定 commit 相同；桌面绘制进一步核对 Core 的 CanvasDrawScope、SkiaBackedPaint。

固定基线默认 `isCheckboxStylingFixEnabled=false`：
画布 20 DIP、2 DIP 留白、圆角 2 DIP、默认最小交互尺寸 48 DIP；
不提前采用 flag 开启后的 18 DIP 画布和新勾号坐标。
`DisabledCheckmarkColor` 虽然公开，关闭 flag 的基线不消费它，禁用勾号仍用 checkedCheckmarkColor。
默认透明色使用透明黑色，避免 Avalonia `Colors.Transparent` 的透明白色改变颜色插值。

模板中的 CheckBoxLayout 保留受限尺寸下的中心计算；
CheckBoxIndicator 用 Skia PathMeasure 绘制勾号片段，按上游分支选择 draw fraction、
中心吸附、100ms snap delay、DefaultSpatial、DefaultEffects / FastEffects。
颜色消费 ComposeAnimation 的 Animatable，形状消费同一 Transition；
禁用时 box/border 直接到目标，启用时重新建立已经在目标的颜色状态。
动画通过 VisualAnimationScope 注册、卸载和取消；只在状态目标变化后启动 Transition 的帧循环，
完成后取消等待，避免 idle 异步唤醒导致首帧注册不确定。
模板复用 MaterialSurface 与共享交互控制器，固定不裁剪的 20 DIP ripple 半径。
绘制需要 Avalonia.Skia。

## 验证与边界

独立双端入口：
- [CheckBoxCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/CheckBoxCases.cs)
- [CheckBoxCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/CheckBoxCases.kt)

76 个用例覆盖两态/三态、亮暗主题、启用/禁用、自定义颜色、
圆/方端帽、圆/miter 连接、虚线、粗描边、RTL、自动尺寸和受限尺寸；
动态覆盖六种状态转换、六种反向、第三目标中断、标准 motion、
禁用/启用、主题切换、按压/快速释放/取消、按压中禁用、hover、Space、Enter 和三态原生切换。
两端都调用真实组件 API；参考端不模拟原生状态循环，也不豁免既有差异。
已接受的差异保留在 dynamic.input.enter、dynamic.input.tri-cycle 和 static.stroke.round.on 用例中。

[CheckBoxTests.cs](../../../../tests/Material3Expressive.Tests/CheckBoxTests.cs)
检查实际模板、绑定写回、事件顺序、原生键盘和自动化、内容与 RTL、Token 与颜色、
idle 后动画重启，以及模板替换/卸载/重新挂载的清理。

原生 CheckBox 没有移除 toggleable 自动化语义的开关，完全等价需要独立的只显示指示器，不能仅替换模板完成。
2026-10-03，维护者确认“本次保留原生范围，记录 null 回调模式未实现（推荐）”。
本次不提供 Compose `onClick=null` 的完全等价模式（含移除 toggleable 语义），不新增独立只显示指示器，
也不改变原生自动化行为。
应用可同时设置 IsHitTestVisible=false、Focusable=false、MaterialInteraction.IsInteractive=false
阻止普通输入与 Material 反馈，但仍保留原生自动化 ToggleProvider；
不能将它描述为完全等价的 null 回调模式。外部注入 Compose MutableInteractionSource 没有对象级映射。
文字视觉、实际 OS 无障碍客户端和真实桌面手工交互未作双端验收。
