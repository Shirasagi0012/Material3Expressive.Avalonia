# ToggleButton

使用 Avalonia 原生 `ToggleButton` 表达可开关的操作，Material 主题提供 Filled、Tonal、Elevated、Outlined
四种外观及选中/按压形状。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
维护依据和差异边界见 [INTERNALS](INTERNALS.md)。

## 最小示例

在已加载 Material 主题的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <ToggleButton Content="固定" IsChecked="True" />
</UserControl>
```

默认 Filled/Small；后续片段沿用上述命名空间。

## 外观、尺寸与状态

| 外观 | `Theme` 资源 |
| --- | --- |
| Filled（默认） | `MaterialFilledToggleButtonTheme` |
| Tonal | `MaterialTonalToggleButtonTheme` |
| Elevated | `MaterialElevatedToggleButtonTheme` |
| Outlined | `MaterialOutlinedToggleButtonTheme` |

```xml
<ToggleButton Content="固定" Classes="medium"
              Theme="{StaticResource MaterialTonalToggleButtonTheme}"
              IsChecked="{Binding IsPinned, Mode=TwoWay}"
              Command="{Binding SavePinCommand}" />
```

假设 `DataContext` 提供可写 `IsPinned` 与 `ICommand` `SavePinCommand`；编译绑定需声明 `x:DataType`。
原生控件先切换状态再发 `Click`/`Command`，`IsChecked` 默认双向绑定。
可用 `Click="OnPinClick"` 连接 code-behind 处理器。Enter 按下激活，`Space` 默认松开激活。
`IsChecked=null` 按未选中绘制；`IsThreeState` 是原生扩展，不代表另一种 Material 选中外观。
禁用使用 `IsEnabled=false`，禁用颜色不再区分选中。

默认 Small 不设 class，其他用 `xsmall` / `medium` / `large` / `xlarge`；一次选择一档。
容器最小高分别 32/40/56/96/136 DIP，外层默认预留至少 48 DIP 布局空间，不是鼠标目标扩展。
本族没有普通 Button 的 58 DIP 默认最小宽度；显式尺寸和 `MinWidth`/`MinHeight` 保留调用方约束。

## 标签、图标与圆角

`Content`/`ContentTemplate` 是标签槽，自动字符串复用 Button 的排版；图标使用附加槽，
其尺寸盒与间距自动跟随当前尺寸：

```xml
<ToggleButton Content="收藏">
  <m3:ToggleButtonAppearance.Icon>
    <m3:MaterialSymbol Symbol="Favorite" />
  </m3:ToggleButtonAppearance.Icon>
</ToggleButton>
```

`ToggleButtonAppearance.IconTemplate` 支持数据模板；`MaterialSymbol` 继承内容色，自定义图形需自行绑定前景色。
本族默认就有形变：按压目标优先于选中目标，选中优先于静止圆角。

```xml
<ToggleButton Content="固定" CornerRadius="20"
              m3:ToggleButtonAppearance.CheckedCornerRadius="12"
              m3:MaterialButton.PressedCornerRadius="6" />
```

静止用原生 `CornerRadius`，选中用 `CheckedCornerRadius`，按压复用 `MaterialButton.PressedCornerRadius`。
Small 默认按压角 6 DIP。显式局部值优先于尺寸默认值；若需完全固定圆角，将静止和选中设为相同值，
并将 `PressedCornerRadius` 设为 `{x:Null}`。

## 颜色与使用边界

`Background`/`Foreground`、`BorderBrush`/`BorderThickness`、`Padding` 和内容对齐可按原生属性定制。
整套颜色用附加 `ToggleButtonAppearance.Colors` 的不可变 `ToggleButtonColors`，
六槽为普通/禁用/选中的 container/content；C# 通过 SetColors 传入，null 恢复动态主题 Token。
局部 Token 更适合只改一个状态：

```xml
<ToggleButton Content="固定" IsChecked="True">
  <ToggleButton.Resources>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+FilledToggleButton.CheckedContainerColor}">#126E60</Color>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+FilledToggleButton.CheckedContentColor}">#FFFFFF</Color>
  </ToggleButton.Resources>
</ToggleButton>
```

本主题直接用 `Foreground` 作为 Surface 反馈墨色。局部颜色可能覆盖禁用默认值，需一并检查禁用状态。
当前形状只支持圆角矩形，没有任意 `Shape` 或外部 MutableInteractionSource 对象映射。
模板内部 Surface 裁剪背景、内容与反馈；替换模板需自行保留这些接入。
当前无字形回归不能证明真实字体、屏幕阅读器或桌面体验一致。
四种外观和尺寸见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
