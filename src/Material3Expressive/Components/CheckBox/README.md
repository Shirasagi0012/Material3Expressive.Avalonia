# CheckBox

使用 Avalonia 原生 `CheckBox` 表达两态选择或三态聚合选择；Material 主题负责指示器、动画和反馈，
无需改用另一个控件。首次使用先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
内部映射、原生适配依据与验证边界见 [INTERNALS](INTERNALS.md)。

## 最小示例

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

## 状态、绑定与内容

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
Enter 在按下时激活，Space 默认在松开时激活；禁用阻止输入。

`Content` / `ContentTemplate` 支持字符串、控件与数据模板，默认 BodyLarge 排版，保留 access key 和 RTL。
标签位于 48 DIP 指示器槽旁，默认间距来自 `Padding="8,0,0,0"`；无 Content 时不使用这层 Padding。
有内容时整行（含间隙和 Padding）参与命中；无内容时默认鼠标命中区域为 24×24 DIP 指示器区域。
48×48 DIP 默认布局留白不等于扩大鼠标命中区，也不是触屏输入层的目标扩展。

## 颜色与描边

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

## 使用边界

- 需要 Avalonia.Skia 绘制；公共接入说明包含宿主配置
- 不提供完全等价于 Compose `onClick=null` 的只显示模式。关闭命中、焦点和 Material 交互仍保留原生 Toggle 自动化语义
- 自定义 Stroke 支持 dash 参数，不支持任意 Compose PathEffect 对象
- 未提供外部 `MutableInteractionSource` 对象映射；实际 OS 无障碍客户端、文字视觉和真实桌面双端体验尚未验收

两态、三态、描边和全选组合见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
