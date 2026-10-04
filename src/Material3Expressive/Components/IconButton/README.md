# IconButton

`m3:IconButton` 是以 Material Symbol 显示单个动作的 Button 派生控件，提供 Standard、Filled、Tonal、Outlined
四种主题和五档尺寸。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
实现映射及尚未解决的默认值差异见 [INTERNALS](INTERNALS.md)。

## 最小示例

在已加载 Material 主题与图标资源的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:IconButton Symbol="Add" AutomationProperties.Name="添加" />
</UserControl>
```

默认 Standard / Small；图标按钮应提供有意义的自动化名称。后续片段使用相同命名空间。
`Symbol` 默认 None，保留布局但不画图标。默认模板只消费 Symbol，不显示继承的 Content/ContentTemplate；
需要任意图形或自定义组合时可用普通[Button](../Button/README.md)组合内容，或自行替换模板。

## 外观、尺寸与动作

| 外观 | Theme 资源 |
| --- | --- |
| Standard（默认） | MaterialStandardIconButtonTheme |
| Filled | MaterialFilledIconButtonTheme |
| Tonal | MaterialTonalIconButtonTheme |
| Outlined | MaterialOutlinedIconButtonTheme |

```xml
<m3:IconButton Symbol="Add" Classes="medium"
               Theme="{StaticResource MaterialFilledIconButtonTheme}"
               AutomationProperties.Name="添加项目"
               Command="{Binding AddCommand}" />
```

假设 DataContext 提供 ICommand AddCommand；编译绑定需 x:DataType，参数用原生 CommandParameter。
事件方式可写 `Click="OnAddClick"` 并在视图 code-behind 实现处理器。
禁用使用 IsEnabled=false；Enter沿用原生按下激活，Space遵循原生ClickMode。
本组件没有IsChecked或选中切换入口。

默认Small不设置class；一次只选择一档尺寸：

| 尺寸 class | 默认容器边长 / 图标尺寸（DIP） |
| --- | --- |
| xsmall | 32 / 20 |
| 不设置（Small） | 40 / 24 |
| medium | 56 / 24 |
| large | 96 / 32 |
| xlarge | 136 / 40 |

Small的外层默认布局为48×48，容器40×40；XS显式约束外层与容器均为32×32。
其他尺寸按各自容器边长布局；留白不扩大鼠标点击区域。
Width/Height、MinWidth/MinHeight保留显式约束；未设置的公开MinWidth/MinHeight为0，
不要据其推断默认容器尺寸，需要布局读数时看实际Bounds。

## 形状与接入

默认按压不变形，CornerRadius配置静止形状，MaterialButton.PressedCornerRadius显式配置按压目标：

```xml
<m3:IconButton Symbol="Add" CornerRadius="20" AutomationProperties.Name="添加" />
<m3:IconButton Symbol="Add" CornerRadius="20"
               m3:MaterialButton.PressedCornerRadius="8" AutomationProperties.Name="添加" />
```

按压目标默认null；从旧自动变形行为迁移时需显式设置，也可绑定对应尺寸的Token：

```xml
<m3:IconButton Symbol="Add" AutomationProperties.Name="添加"
               m3:MaterialButton.PressedCornerRadius="{m3:Token {x:Static m3:MaterialTokens+Comp+IconButtonSmall.PressedContainerShape}}" />
```

尺寸class不覆盖局部静止圆角或显式按压目标；形状变化不改变内容/尺寸布局。
圆角方形通过CornerRadius或ContainerShapeSquare Token表达，没有独立square class。
Background/Foreground调整背景/图标，BorderBrush/BorderThickness调整描边。
状态层/ripple颜色来自主题Token，不保证随局部Foreground同步；需要各状态一致时优先
[Token覆盖](../../../../docs/GETTING_STARTED.md)，并检查禁用状态。

## 使用边界

- 当前只提供uniform宽度；narrow/wide和toggle入口尚未实现
- Standard/Outlined当前默认图标色为OnSurfaceVariant，不自动采用上游ambient内容色；Outlined描边用OutlineVariant，大尺寸仍为2/3 DIP。这些是未解决差异，不是已接受适配
- 强制Width/Height小于默认图标尺寸时，MaterialSymbol可能溢出；替换模板需自行维护尺寸、形状和反馈接入
- Material焦点环尚未实现，当前使用状态层；Material Symbols字形、镜像策略和真实桌面显示不由向量parity用例证明

主题示例见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)，工具栏组合见
[FloatingToolbarDemo](../../../Material3Expressive.Catalog/FloatingToolbarDemo.axaml)。
