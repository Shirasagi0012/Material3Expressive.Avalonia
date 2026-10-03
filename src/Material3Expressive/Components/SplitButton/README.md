# SplitButton

`m3:SplitButton` 是独立的双槽组合容器，不继承 Avalonia SplitButton。
依据 [固定基线](../../../../docs/upstream-baseline.json) 的 `SplitButton.kt`：
父级负责 trailing 宽度优先、共同高度、剩余 leading 宽度、间距和整体交互布局空间；
子按钮负责自身命令、切换、焦点与反馈。

```xml
<m3:SplitButton Spacing="2">
  <m3:SplitButton.LeadingButton>
    <Button Content="Save" Command="{Binding SaveCommand}"
            Theme="{StaticResource MaterialFilledSplitButtonLeadingTheme}" />
  </m3:SplitButton.LeadingButton>
  <m3:SplitButton.TrailingButton>
    <ToggleButton IsChecked="{Binding Expanded}"
                  Theme="{StaticResource MaterialFilledSplitButtonTrailingTheme}">
      <Path Data="M 6,9 L 12,15 L 18,9" Width="22" Height="22"
            Stroke="{Binding $parent[ToggleButton].Foreground}" StrokeThickness="2" />
    </ToggleButton>
  </m3:SplitButton.TrailingButton>
</m3:SplitButton>
```

```csharp
var split = new Material3Expressive.Components.SplitButton
{
    LeadingButton = saveButton,
    TrailingButton = optionsToggle,
    Spacing = 2
};
// 为实际子控件设置对应主题；事件、Command 和 Flyout 都配置在子控件上。
saveButton.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonLeadingTheme")!;
optionsToggle.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!;
```

## 主题、布局与适配

Filled/Tonal/Elevated/Outlined 分别提供 `Material{外观}SplitButtonLeadingTheme` 和
`Material{外观}SplitButtonTrailingTheme`。Leading 使用原生 Button，Trailing 可使用普通 Button
或原生 ToggleButton。`LeadingButton` / `TrailingButton` 为 Control 槽，也可以直接组合其他控件。
没有父级 Click/Command 转发或自动 Flyout 绑定；父级 IsEnabled 的继承仍遵循 Avalonia。

尺寸 class 配置在两个子按钮上：默认 Small，其他为 `xsmall` / `medium` / `large` / `xlarge`。
尺寸 Token 的高度为 32/40/56/96/136 DIP，但固定 LeadingButton/TrailingButton helper 内部仍使用
SmallContainerHeight 的 40 DIP 默认最小高度，所以默认 ExtraSmall 的实际高度为 40 DIP；
显式 Height 或更紧的父级约束可以限制它。子按钮默认最小宽度为 48 DIP；父级整体保留至少 48 DIP
交互布局空间，子按钮不分别加入这项额外留白。所有尺寸的标签默认仍为 LabelLarge。
副区图标尺寸由调用方设置，默认参考值为 22/22/26/38/50 DIP；公共 SplitButton Token 提供这些值。

布局按源码顺序测量：获得 trailing 首选宽度，查询受剩余宽度约束的 leading 高度，确定共同高度，
再测量 trailing 与剩余 leading。Avalonia 没有通用 intrinsic 查询，使用实际子控件的 Measure/DesiredSize
提供相应首选尺寸；自定义控件需正确实现其测量。RTL 通过 Avalonia 的逻辑布局镜像，Spacing 按物理像素舍入。
容器不裁剪极窄约束下的 trailing 溢出，保持 Compose 的布局行为。

Leading 外侧全圆、内侧小角，Trailing 镜像；按压改变内侧角。
可切换 Trailing 选中时变为 CircleShape，并在内容及交互层上绘制 contentColor × 0.10 状态层，
禁用时选中覆盖层仍保留。形状使用 DefaultEffects；形状集合变化保留上游动画状态，spring 变化才重建。
Trailing 光学居中使用动画角值，系数 0.11，按逻辑 Padding 限制并舍入。
源码外侧角为 100% CornerSize，选中 CircleShape 为 50%；光学角在绘制轮廓归一化前计算。

2026-10-04，维护者确认上述组合槽 API，不复用或继承 Avalonia SplitButton。
子控件继续采用原生输入、事件、命令、绑定和自动化语义，尾部 ToggleButton 的 Enter/三态/即时状态
适配见 [ToggleButton](../ToggleButton/README.md)。默认主题复用 Button 的颜色、elevation 与交互机制。
没有任意 Compose Shape 或 MutableInteractionSource 对象映射；静止、按压形状通过 CornerRadius 和
MaterialButton.PressedCornerRadius 表达，尾部选中形状使用默认 CircleShape。
ToggleButtonAppearance 的 CheckedCornerRadius、Icon 与 Colors 由独立 ToggleButton 主题消费，
SplitButton 子主题的内容使用 Content，颜色由 Background/Foreground 或 Token 设置。

## 验证

独立双端用例为 `SplitButtonCases.cs` / `SplitButtonCases.kt`，参考端调用真实 SplitButtonLayout 和
SplitButtonDefaults LeadingButton/TrailingButton；没有在参考夹具重写产品算法。
覆盖四种外观、五种尺寸、普通/可切换副区、选中、禁用、暗色、RTL、共同高度、间距及极窄尺寸，
以及两个区域独立按压、选择、反向、形状配置更换、取消、快速点击、禁用中断、键盘与主题更新。
2026-10-04 的 4× Headless self-test 完整执行 99 例、341 个采样点：80 withinTolerance、19 different，
执行错误为 0，全部普通重复捕获与额外被动截图检查稳定；72 个静态用例均 withinTolerance。
参考端与 Avalonia 都是本轮执行。不同结果保留原生 Enter 激活及属性/主题/禁用当帧的时机差异；
两个 hover 用例在 64ms 的共享 elevation 读数仍有约 0.0000003–0.0000006 DIP 精度残差，未认定为
已接受差异。默认形状及光学位置已对齐，选中动画中修改静止形状不会取消未变目标的动画。
文字、真实桌面操作、屏幕阅读器、非常大的非对称角值、运行中约束更换及任意自定义槽的 intrinsic
行为不由这些无字形 Headless 场景证明。控件、框架及共享组件回归范围见
[ToggleButton 验证](../ToggleButton/README.md#验证)。

产物：[双端概览](../../../../artifacts/visual-parity/runs/split-button-port-complete/summary.md)、
[验证与保留问题](../../../../artifacts/test-results/toggle-split/validation.md)。
