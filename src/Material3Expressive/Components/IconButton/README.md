# IconButton

`m3:IconButton` 是以 Material `Symbol` 显示单个动作的 Button 派生控件，提供 Standard、Filled、Tonal、Outlined
四种主题、五档尺寸与三档宽度。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
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
`Symbol` 默认 None，保留布局但不画图标。默认模板只消费 `Symbol`，不显示继承的 `Content`/`ContentTemplate`；
需要任意图形或自定义组合时可用普通[Button](../Button/README.md)组合内容，或自行替换模板。

## 外观、尺寸与动作

| 外观 | `Theme` 资源 |
| --- | --- |
| Standard（默认） | `MaterialStandardIconButtonTheme` |
| Filled | `MaterialFilledIconButtonTheme` |
| Tonal | `MaterialTonalIconButtonTheme` |
| Outlined | `MaterialOutlinedIconButtonTheme` |
| Vibrant Standard | `MaterialVibrantIconButtonTheme` |
| Vibrant Outlined | `MaterialOutlinedVibrantIconButtonTheme` |

```xml
<m3:IconButton Symbol="Add" Classes="medium"
               Theme="{StaticResource MaterialFilledIconButtonTheme}"
               AutomationProperties.Name="添加项目"
               Command="{Binding AddCommand}" />
```

假设 `DataContext` 提供 `ICommand` `AddCommand`；编译绑定需 `x:DataType`，参数用原生 `CommandParameter`。
事件方式可写 `Click="OnAddClick"` 并在视图 code-behind 实现处理器。
禁用使用 `IsEnabled=false`；Enter 沿用原生按下激活，`Space` 遵循原生 ClickMode。
普通 IconButton 没有 IsChecked；受控切换使用下面的 IconToggleButton。

### 普通与 Vibrant 颜色

普通 Standard（包括隐式主题）和 Outlined 使用父级 `TextElement.Foreground` 作为默认内容色；
禁用时将 alpha 替换为 DisabledContentOpacity，而不是乘以原 alpha。Outlined 默认描边也取父级内容色。

```xml
<StackPanel Orientation="Horizontal" TextElement.Foreground="#245A80">
  <m3:IconButton Symbol="Home" AutomationProperties.Name="主页" />
  <m3:IconButton Symbol="Home" Theme="{StaticResource MaterialOutlinedIconButtonTheme}"
                 AutomationProperties.Name="主页（描边）" />
  <m3:IconButton Symbol="Home" Theme="{StaticResource MaterialVibrantIconButtonTheme}"
                 AutomationProperties.Name="主页（Vibrant）" />
</StackPanel>
```

**从旧主题迁移：** 依赖 `Comp.StandardIconButton.*` / `Comp.OutlinedIconButton.*` 颜色 Token 的调用方，
分别改用 `MaterialVibrantIconButtonTheme` / `MaterialOutlinedVibrantIconButtonTheme`。
这两个主题保留容器、内容和描边颜色 Token；普通主题不再消费这些颜色槽。
尺寸和形状 Token 的用法不变。此行为迁移已于 2026-11-03 获维护者确认。

Standard（含 Vibrant）的波纹和状态层取父级 `TextElement.Foreground`；Filled、Tonal、Outlined
（含 Vibrant）取按钮当前 `Foreground`，包括用户覆盖和禁用色。比如 Filled 的 `Foreground="Red"`
会让图标与反馈一起变红，Standard 的同一设置只改变图标。Catalog 提供相应颜色对照。

旧 `Comp.*IconButton.*StateLayerColor` 键仍可供自定义主题引用，但内置主题不再消费这些键。
原先依靠它们单独改反馈色的调用方应迁移到上述颜色来源；需要保持独立反馈色时，在自定义
ControlTheme 的 `^ /template/ m3|MaterialSurface#PART_Container` 上设置 `StateLayerBrush`。
同时覆盖 `^:pointerover`、`^:focus-visible`、`^:pressed` 的同一目标 Setter，避免内置状态选择器
在交互时重新接管墨色。这保留显式定制能力，默认反馈始终按内容环境更新。

## IconToggleButton

`IconToggleButton` 提供 Standard（默认）、Filled、Tonal、Outlined 的受控切换入口，沿用 Symbol、尺寸和宽度模板。

```xml
<m3:IconToggleButton Symbol="Favorite" IsChecked="{Binding IsFavorite}"
                     CheckedChangeRequested="ChangeFavorite" AutomationProperties.Name="收藏" />
```

事件接收 `IconCheckedChangeEventArgs.Value`，由应用写回 IsChecked；忽略请求时状态不变。
支持继承的 Click / Command，但它们也不隐式写回状态。无障碍采用 Checkbox / Toggle 模式。
默认未选中颜色来自父级内容色，选中使用 Primary，禁用替换 ambient 色的 alpha；
Foreground、CornerRadius 等仍可由用户 Style / Binding / LocalValue 覆盖。
切换主题分别使用 `MaterialStandardIconToggleButtonTheme`、`MaterialFilledIconToggleButtonTheme`、
`MaterialTonalIconToggleButtonTheme`、`MaterialOutlinedIconToggleButtonTheme`。Filled 未选中使用 SurfaceContainer / OnSurfaceVariant，选中使用
Primary / OnPrimary；Tonal 未选中使用 SecondaryContainer / OnSecondaryContainer，选中使用
Secondary / OnSecondary。Outlined 未选中时使用透明容器与父级内容色，选中使用 InverseSurface 及其
`contentColorFor` 内容色；选中时移除默认描边（禁用的选中态也无描边）。禁用优先于选中配色，颜色立即更新，没有颜色过渡。

Vibrant 配色使用 `MaterialVibrantIconToggleButtonTheme` 或 `MaterialOutlinedVibrantIconToggleButtonTheme`。
两者未选中使用 OnSurfaceVariant，禁用使用 OnSurface 的 DisabledContentOpacity；Standard 选中使用 Primary，
Outlined 选中使用 InverseSurface / InverseOnSurface，未选中描边使用 OutlineVariant。禁用的选中态同样不绘制描边。
Standard 的交互反馈取父级内容色，Outlined 的反馈取当前 Foreground。它们与普通切换主题共享受控选择及形变规则。

```xml
<m3:IconToggleButton Symbol="Favorite" Classes="morph"
                     Theme="{StaticResource MaterialOutlinedVibrantIconToggleButtonTheme}"
                     IsChecked="{Binding IsFavorite}" CheckedChangeRequested="ChangeFavorite"
                     AutomationProperties.Name="收藏" />
```

六槽颜色可分别通过 `Comp.StandardIconToggleButton` / `Comp.OutlinedIconToggleButton` 局部覆盖；
这些槽仅供新增 Vibrant 切换主题使用，普通主题仍读取内容环境。

```xml
<m3:IconToggleButton Symbol="Favorite" Classes="morph"
                     Theme="{StaticResource MaterialOutlinedIconToggleButtonTheme}"
                     CheckedChangeRequested="ChangeFavorite" AutomationProperties.Name="收藏" />
```

```xml
<m3:IconToggleButton Symbol="Favorite" IsChecked="{Binding IsFavorite}"
                     Theme="{StaticResource MaterialFilledIconToggleButtonTheme}"
                     CheckedCornerRadius="12" m3:MaterialButton.PressedCornerRadius="8"
                     CheckedChangeRequested="ChangeFavorite" AutomationProperties.Name="收藏" />
```

`CheckedCornerRadius` 默认 null，保留静止 `CornerRadius`。显式设置后按 DefaultEffects 动画切换；
按压目标优先于选中目标，反向和重挂载沿用共享 Surface 生命周期。示例处理器：

```csharp
private void ChangeFavorite(object? sender, IconCheckedChangeEventArgs e) =>
    ((IconToggleButton)sender!).IsChecked = e.Value;
```

使用 ViewModel 绑定时在处理器写回 ViewModel 属性。Outlined toggle 已按获批的颜色构造逻辑实现，
不复制上游依赖调用顺序的缓存问题，见 [INTERNALS](INTERNALS.md#2026-11-03-颜色与缓存决定)。

默认 Small 不设置 class；一次只选择一档尺寸：

| 尺寸 class | 默认容器边长 / 图标尺寸（DIP） |
| --- | --- |
| `xsmall` | 32 / 20 |
| 不设置（Small） | 40 / 24 |
| `medium` | 56 / 24 |
| `large` | 96 / 32 |
| `xlarge` | 136 / 40 |

`WidthOption` 默认 Uniform；Narrow / Wide 只改变容器宽度，不改变图标尺寸或高度：

| 尺寸 | Narrow / Uniform / Wide 容器宽度（DIP） |
| --- | --- |
| XS | 28 / 32 / 40 |
| Small | 32 / 40 / 52 |
| Medium | 48 / 56 / 72 |
| Large | 64 / 96 / 128 |
| XL | 104 / 136 / 184 |

```xml
<m3:IconButton Symbol="Add" Classes="medium" WidthOption="Wide"
               Theme="{StaticResource MaterialTonalIconButtonTheme}" AutomationProperties.Name="添加" />
```

Small 的外层默认至少 48×48，Uniform 容器 40×40；XS 的主题默认 Width 使用所选宽度、Height 为 32。
其他尺寸按各自容器边长布局；留白不扩大鼠标点击区域。
`Width`/`Height`、`MinWidth`/`MinHeight` 保留显式约束；未设置的公开 `MinWidth`/`MinHeight` 为 0，
不要据其推断默认容器尺寸，需要布局读数时看实际 `Bounds`。

## 形状与接入

`Classes="square"` 选择当前尺寸的方形圆角；`Classes="morph"` 显式启用按压形变。
两者可以与尺寸 class 组合，切换按钮还会采用对应的选中形状：圆形静止 → 方形选中，
方形静止 → 圆形选中。默认不含 morph，仍保持固定形状。

```xml
<m3:IconButton Symbol="Add" Classes="medium square morph" />
<m3:IconToggleButton Symbol="Favorite" Classes="large morph"
                     Theme="{StaticResource MaterialFilledIconToggleButtonTheme}"
                     CheckedChangeRequested="ChangeFavorite" />
```

预设保持动态 Token 引用；普通 Style 或显式 CornerRadius、PressedCornerRadius、CheckedCornerRadius
仍优先，显式设置可空目标为 null 可关闭对应形变。移除 morph 会恢复无目标的默认配置。


默认按压不变形，`CornerRadius` 配置静止形状，`MaterialButton.PressedCornerRadius` 显式配置按压目标：

```xml
<m3:IconButton Symbol="Add" CornerRadius="20" AutomationProperties.Name="添加" />
<m3:IconButton Symbol="Add" CornerRadius="20"
               m3:MaterialButton.PressedCornerRadius="8" AutomationProperties.Name="添加" />
```

按压目标默认 null；从旧自动变形行为迁移时需显式设置，也可绑定对应尺寸的 Token：

```xml
<m3:IconButton Symbol="Add" AutomationProperties.Name="添加"
               m3:MaterialButton.PressedCornerRadius="{m3:Token {x:Static m3:MaterialTokens+Comp+IconButtonSmall.PressedContainerShape}}" />
```

尺寸 class 提供默认值，普通用户 Style、局部值或绑定均可覆盖静止圆角、按压目标和 Width/Height；
XS 的 32 DIP 也不是强制尺寸。形状变化不改变内容/尺寸布局。
圆角方形可用 `square` class，或通过 `CornerRadius` / `ContainerShapeSquare` Token 显式配置。
`Background`/`Foreground` 调整背景/图标，`BorderBrush`/`BorderThickness` 调整描边。
普通 Standard 按上游从外层内容环境选状态层/ripple 墨色；普通 Outlined 和 Filled/Tonal/Outlined toggle
使用有效 Foreground。Vibrant 兼容主题保留状态层颜色 Token，不保证随局部 Foreground 同步。
配置后应同时检查禁用状态和交互反馈。

主题默认颜色和描边同样可被普通 Style、局部值或绑定覆盖，禁用不会强制替换这些设置；
需要禁用差异时用自己的状态样式或对应 Token。清除局部值后恢复剩余样式或当前尺寸/状态的主题默认值，
如 `iconButton.ClearValue(IconButton.WidthProperty)`（`iconButton` 为当前实例）会重新采用当前尺寸默认值。
资源和主题变更会继续更新未被覆盖的默认配置。

## 使用边界

- 尚未提供任意路径 Shape；详见维护说明
- Outlined 默认描边按上游 helper 对所有尺寸使用 Small 的 1 DIP；显式 BorderBrush / BorderThickness 仍可覆盖默认值
- 模板内部图标按两轴约束分别缩小（例如 20×40 容器获得 20×24 图标盒）；替换模板需自行维护尺寸、形状和反馈接入
- 默认焦点反馈为状态层；可通过 `MaterialInteraction.FocusFeedback="InsetRing"` 启用共享内缩焦点环，各尺寸/变体尚未独立完成双端焦点环验收
- Material Symbols 字形、镜像策略和真实桌面显示不由向量 parity 用例证明

主题示例见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)，工具栏组合见
[FloatingToolbarDemo](../../../Material3Expressive.Catalog/FloatingToolbarDemo.axaml)。

## 外部交互源

`IconButton` 和 `IconToggleButton` 的可选 `InteractionSource` 接受已有 `MaterialInteractionSource`。
设置同一个源可把原生按压、悬停与 `TooltipLabel` 连接：

```csharp
var source = new MaterialInteractionSource();
iconButton.InteractionSource = source;
tooltipLabel.InteractionSource = source;
```

类型命名空间为 `Material3Expressive.Foundation.Interaction`。外部 Emit 仅控制反馈和按压形状，
不会执行 Click/Command 或修改 IsChecked；释放/取消应引用原来的 Press 对象。
多个按压中最后一个结束才恢复形状。源不重放事件；换源清除旧波纹，原生开始事件配对取消。
尚未结束的外部按压则按上游保留已记忆状态和正在进行的形变，直到新源首个事件重新计算状态。
卸载停止订阅。
从固定形状切换到形变会重新开始形状状态收集；按住期间才设置 PressedCornerRadius 不会重放旧按压。
