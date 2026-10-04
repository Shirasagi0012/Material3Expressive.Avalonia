# Card

`Card`、`ElevatedCard`、`OutlinedCard` 分别提供填充、阴影和描边卡片，适合组织同一主题的内容。
三者默认只是内容容器；显式设置 `IsClickable="True"` 才让卡片自身成为按钮。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)；实现与移植边界见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

下面不需要 ViewModel。卡片默认无 padding，用 `CardColumn.Margin` 为内容留白：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:Card>
    <m3:CardColumn Margin="16">
      <TextBlock Text="项目概览" m3:MaterialText.Style="{m3:Typography TitleMedium}" />
      <TextBlock Text="最近更新的内容" />
    </m3:CardColumn>
  </m3:Card>
</UserControl>
```

后续片段沿用相同命名空间。替换为 `ElevatedCard` 或 `OutlinedCard` 即可选择另外两种外观。

## 可点击、禁用与命令

卡片可复用原生 Button 的 `Click`、`Command` 和 `CommandParameter`：

```xml
<m3:ElevatedCard IsClickable="True" Command="{Binding OpenCommand}"
                 CommandParameter="{Binding SelectedItem}">
  <m3:CardColumn Margin="16">
    <TextBlock Text="打开项目" />
  </m3:CardColumn>
</m3:ElevatedCard>
```

该片段假设 DataContext 提供命令和参数，编译绑定视图还需对应的 `x:DataType`。
若用 `Click="OnCardClick"`，在视图 code-behind 中实现处理器。

普通模式不激活自身、不提供 Invoke automation，默认不可聚焦；内容里的原生按钮仍可交互，
`Command.CanExecute` 不影响普通卡片的启用状态。`IsEnabled="False"` 仍禁用整个内容树，
但普通模式不会选择可点击卡片的 disabled 颜色/高度。

可点击模式有至少 48 DIP 的默认交互布局分配，视觉 Surface 可以更小；这不是额外触屏命中扩展。
默认 Space 松开、Enter 按下时激活，使用原生 Button 键盘行为。
切到普通模式会取消卡片自身的按压与焦点，不应将模式切换当成一次点击。

## 内容与定制

`Content` / `ContentTemplate` 接收任意内容。`CardColumn` 从上到下、Start 对齐、零间距，
按顺序把剩余高度交给子项；高度耗尽后后续子项收到零高度约束。
默认 Stretch 在此按 Start 放置，显式 HorizontalAlignment 可指定其他对齐，RTL 跟随布局方向。
它不提供完整 ColumnScope 的 weight / alignBy；直接用 StackPanel 也可以，但保留其原生测量语义。
卡片 `Padding` 或内容 `Margin` 都可用于留白。

`Colors` / `Elevation` 是可共享配置，各可空槽或整个配置为 null 时使用当前外观默认值：

```xml
<m3:OutlinedCard IsClickable="True" IsEnabled="False">
  <m3:OutlinedCard.Colors>
    <m3:CardColors ContainerColor="#E8DEF8" ContentColor="#1D192B"
                   DisabledContentColor="#79747E" />
  </m3:OutlinedCard.Colors>
  <m3:OutlinedCard.Elevation>
    <m3:CardElevation Default="0" Hovered="2" Pressed="0" Disabled="0" />
  </m3:OutlinedCard.Elevation>
  <TextBlock Text="暂不可用" Margin="16" />
</m3:OutlinedCard>
```

原生 `Background` / `Foreground` / `BorderBrush` / `BorderThickness` / `CornerRadius`
的显式值或绑定优先于默认值。未设置内容色时，默认从启用容器颜色推导匹配的 On 色，
无法匹配主题角色时继承外层内容色。统一定制见[公共 Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。

普通卡片会记住初次挂载的 Default elevation；后来替换 Elevation 或默认 Token 不会更新此高度，
切换模式或重新挂载才重建，单独替换模板不重置。可点击卡片跟随最后一个活动交互更新高度，
禁用时直接切到目标值。这里控制的是阴影高度，不是 tonal elevation。

## 使用边界

没有公开可注入的 MutableInteractionSource；Drag 高度的外部注入尚未提供。
可配置 inset focus ring、任意路径/切角 Shape 及完整 ColumnScope 尚未实现；当前形状用四角 CornerRadius。
半透明 ElevatedCard 仍存在已观察到的共享阴影差异，动画也有已记录数值精度差异，不能视作完整视觉一致。
实际桌面手动交互与文字像素一致性尚未经验证。
Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 展示三种普通、可点击及禁用外观。
