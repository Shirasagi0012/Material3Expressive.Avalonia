# CheckBox

使用 Avalonia 原生 `CheckBox` 表达两态选择或三态聚合选择；Material 主题负责指示器、动画和反馈，
无需改用另一个控件。首次使用先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
内部映射、原生适配依据与验证边界见 [INTERNALS](INTERNALS.md)。

## 基础用法

在已加载 Material 主题的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel>
    <CheckBox Content="接收通知" IsChecked="True" />
    <CheckBox Content="部分选中" IsThreeState="True" IsChecked="{x:Null}" />
    <CheckBox Content="暂不可用" IsChecked="True" IsEnabled="False" />
  </StackPanel>
</UserControl>
```

隐式主题是 `MaterialCheckBoxTheme`，也可显式引用或用 `BasedOn` 派生。
后续片段沿用上述命名空间。

## 变体与属性

```xml
<CheckBox Content="全选" IsThreeState="True"
          IsChecked="{Binding GroupState, Mode=TwoWay}"
          Command="{Binding ToggleGroupCommand}" />
```

这里假设 `DataContext` 提供可写的 `bool? GroupState` 和 `ICommand ToggleGroupCommand`；
启用编译绑定时声明对应 `x:DataType`。`IsChecked` 本身默认双向绑定。
两态点击依次 false → true → false；三态依次 false → true → null → false。
原生控件先更新状态再发出 `Click` / `Command`，聚合选择可在处理器或命令中写回计算结果，
并非仅请求应用切换。`Click="OnToggleClick"` 需要视图 code-behind 提供该处理器。
Enter 在按下时激活，`Space` 默认在松开时激活；禁用阻止输入。

### 共享交互源

`CheckBoxAppearance.InteractionSource` 接受已有的 `MaterialInteractionSource`，可观察原生按压、
释放、hover 和焦点事件，也可在 UI 线程主动发送配对交互以预览反馈：

```csharp
var source = new MaterialInteractionSource(); // Material3Expressive.Foundation.Interaction
CheckBoxAppearance.SetInteractionSource(checkBox, source);
var hover = new MaterialInteractionEvent.HoverEnter();
source.Emit(hover);
source.Emit(new MaterialInteractionEvent.HoverExit(hover));
```

交互不会设置 IsChecked 或触发 Click/Command。结束事件必须引用同一个开始事件对象。
传 null 恢复内部交互源；替换源、模板重建或重新挂载只观察后续事件，不回放旧按压。
XAML 可用 `m3:CheckBoxAppearance.InteractionSource="{Binding SelectionSource}"`，
其中 SelectionSource 是视图模型持有的源；共享源只共享反馈，不联动选择。

## 内容槽与定制

由整个列表行处理选择时，行内使用只展示状态的 `CheckBoxDisplay`：

```xml
<m3:CheckBoxDisplay IsChecked="{Binding GroupState}" />
<m3:CheckBoxDisplay IsChecked="{x:Null}" />
```

它支持 false / true / null、`IsEnabled` 状态色和正常切换动画，默认尺寸 24×24 DIP；
不自动切换、不占 48 DIP 交互留白，不接收焦点或提供切换自动化。父级须提供操作、名称和选择语义。
`Colors`、`CheckmarkStroke`、`OutlineStroke` 为普通属性，类型与原生 CheckBox 的 Appearance 配置相同；
尺寸、颜色和运动沿用组件 Token。无需替换现有可交互 CheckBox。


`Content` / `ContentTemplate` 支持字符串、控件与数据模板，默认 BodyLarge 排版，保留 access key 和 RTL。
标签位于 48 DIP 指示器槽旁，默认间距来自 `Padding="8,0,0,0"`；无 `Content` 时不使用这层 `Padding`。
有内容时整行（含间隙和 `Padding`）参与命中；无内容时默认鼠标命中区域为 24×24 DIP 指示器区域。
48×48 DIP 默认布局留白不等于扩大鼠标命中区，也不是触屏输入层的目标扩展。

`Foreground` 控制内容与交互反馈，不直接替代勾号、填充或边框颜色。
按状态调色时优先局部覆盖组件 Token，例如：

```xml
<CheckBox Content="完成" IsChecked="True">
  <CheckBox.Resources>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+CheckBox.CheckedBoxColor}">#008080</Color>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+CheckBox.CheckedBorderColor}">#008080</Color>
  </CheckBox.Resources>
  <m3:CheckBoxAppearance.CheckmarkStroke>
    <m3:CheckBoxStroke Width="3" Cap="Round" Join="Round" />
  </m3:CheckBoxAppearance.CheckmarkStroke>
</CheckBox>
```

`CheckBoxAppearance.CheckmarkStroke` 和 `OutlineStroke` 分别控制勾号和外框；
`Width`、`Dashes`、`DashOffset` 使用绝对 DIP，虚线长度不是笔宽倍数。
端帽、连接和尖角限制分别由 `Cap`、`Join`、`MiterLimit` 控制。
未指定 Stroke 时使用上游默认的物理像素取整，显式 Stroke 按所给 DIP 绘制。

需要整套最终颜色时，用 C# 创建不可变 `CheckBoxColors` 并传给
`CheckBoxAppearance.SetColors(checkBox, colors)`（`Material3Expressive.Components`）；
其十三个槽见 [CheckBoxAppearance.cs](CheckBoxAppearance.cs)。传 `null` 恢复动态 Token。
默认基线不消费 `DisabledCheckmarkColor`，禁用勾号仍使用 `CheckedCheckmarkColor`。

## 使用限制与注意事项

需要 Avalonia.Skia。指示器尺寸与外留白可覆盖对应组件的 `IndicatorSize` / `IndicatorPadding` Token，交互布局环境使用 `MaterialInteractiveSize.Minimum`。
`MaterialInteraction.FocusFeedback="InsetRing"` 启用内缩焦点环。选择自动化仍由原生控件提供，关闭普通输入后也保留该语义。
自定义描边支持 DIP 线宽、端帽、连接和 dash 参数。
组合示例见 [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml)。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。
