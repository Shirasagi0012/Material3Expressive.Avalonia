# RadioButton

使用 Avalonia 原生 `RadioButton` 表达一组互斥选项；Material 主题提供圆环、选中动画与反馈。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，不需要替换为库的子类。
维护依据与一致性边界见 [INTERNALS](INTERNALS.md)。

## 基础用法

在已加载 Material 主题的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel>
    <RadioButton GroupName="delivery" Content="普通配送" IsChecked="True" />
    <RadioButton GroupName="delivery" Content="快速配送" />
  </StackPanel>
</UserControl>
```

隐式主题为 `MaterialRadioButtonTheme`，也可显式引用或派生；后续片段使用相同命名空间。
相同非空 `GroupName` 在同一呈现根内互斥；未设组名时，在同一逻辑父级的 RadioButton 兄弟间互斥。
不同业务组应使用不同组名。

## 变体与属性

```xml
<RadioButton GroupName="delivery" Content="快速配送"
             IsChecked="{Binding ExpressSelected, Mode=TwoWay}"
             Command="{Binding SaveDeliveryCommand}" />
```

假设 `DataContext` 提供可写选择属性和 `ICommand`；编译绑定需声明对应 `x:DataType`。
`IsChecked` 原生默认双向绑定；只有 true 绘制选中，false/null 均未选中。
再次点击已选项不会取消选择，`IsThreeState` 不改变这一单选行为。
状态更新和组互斥发生在 `Click` / `Command` 前；不是仅向应用发出选择请求。
也可设置 `Click="OnDeliveryClick"`，并在视图 code-behind 实现处理器。
Enter 按下激活，`Space` 默认松开激活；`IsEnabled="False"` 阻止激活并使用禁用颜色。

### 共享交互源

`RadioButtonAppearance.InteractionSource` 可绑定 `MaterialInteractionSource`。该源观察原生交互，
也允许主动发送按对象身份配对的预览事件，不修改 IsChecked、GroupName 或调用 Click/Command：

```csharp
var source = new MaterialInteractionSource(); // Material3Expressive.Foundation.Interaction
RadioButtonAppearance.SetInteractionSource(radio, source);
var hover = new MaterialInteractionEvent.HoverEnter();
source.Emit(hover);
source.Emit(new MaterialInteractionEvent.HoverExit(hover));
```

调用须在 UI 线程；null 恢复内部源。替换源和重新挂载不回放先前事件。
XAML 可写 `m3:RadioButtonAppearance.InteractionSource="{Binding SelectionSource}"`，
绑定对象需提供该源。Catalog 的 shared feedback 行演示与 CheckBox 共用反馈、独立选择。

## 内容槽与定制

`Content` / `ContentTemplate` 支持文本和任意内容，默认 BodyLarge，保留内容对齐、access key 和 RTL。
标签位于 48 DIP 指示器槽旁，默认间距为 `Padding="8,0,0,0"`；无 `Content` 时忽略这层 `Padding`。
有内容时整行含间隙和 `Padding` 参与命中；无内容时默认鼠标命中为 24×24 DIP。
48×48 DIP 是默认布局空间，不是扩大鼠标或触屏命中区域的承诺。

`Foreground` 用于内容、状态层和 ripple，指示器颜色独立配置。
局部 Token 可保留主题和状态的联动：

```xml
<RadioButton Content="自定义颜色" IsChecked="True">
  <RadioButton.Resources>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+RadioButton.SelectedColor}">#008080</Color>
  </RadioButton.Resources>
</RadioButton>
```

整套替换用 `RadioButtonAppearance.Colors`。不可变 `RadioButtonColors` 的四个槽分别为
SelectedColor、UnselectedColor、DisabledSelectedColor、DisabledUnselectedColor。
在 C# 中调用 `RadioButtonAppearance.SetColors(radio, colors)`（`Material3Expressive.Components`），
传 null 恢复动态 Token；`with` 表达式可在已有配置上修改个别槽。

## 使用限制与注意事项

需要 Avalonia.Skia。指示器尺寸与外留白可覆盖对应组件的 `IndicatorSize` / `IndicatorPadding` Token，交互布局环境使用 `MaterialInteractiveSize.Minimum`。
`MaterialInteraction.FocusFeedback="InsetRing"` 启用内缩焦点环。选择自动化仍由原生控件提供，关闭普通输入后也保留该语义。
组合示例见 [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml)。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。
