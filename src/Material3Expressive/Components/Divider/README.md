# Divider

分割线使用 Avalonia 原生 `Separator`，由 Material 的隐式主题提供水平/竖直线条，
不需要另一个 Divider 控件。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
维护与绘制细节见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

以下视图无需 ViewModel，默认 Separator 是水平线：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="16">
    <TextBlock Text="第一组" />
    <Separator />
    <TextBlock Text="第二组" />
  </StackPanel>
</UserControl>
```

`MaterialDividerTheme` 也可用 `Theme="{StaticResource MaterialDividerTheme}"` 显式引用或派生。
后续片段沿用相同命名空间。

## 方向、厚度和颜色

附加属性 `MaterialDivider.Orientation` 选择 Horizontal/Vertical，`Thickness` 是 DIP 线宽，
`Color` 是颜色值；默认 1 DIP、动态 OutlineVariant。
颜色不读取 Separator.Background/Foreground，要覆盖线条颜色应设置附加 Color：

```xml
<Separator Margin="16,0" m3:MaterialDivider.Thickness="2"
           m3:MaterialDivider.Color="#6750A4" />
```

竖直线要有可用高度，例如放在有界 Grid：

```xml
<Grid Height="48" ColumnDefinitions="*,Auto,*">
  <TextBlock Grid.Column="0" Text="左侧" />
  <Separator Grid.Column="1" Margin="16,0" m3:MaterialDivider.Orientation="Vertical" />
  <TextBlock Grid.Column="2" Text="右侧" />
</Grid>
```

参数支持绑定及原生属性优先级，绑定时 DataContext 必须提供相应类型；编译绑定需 x:DataType。
C# 对应 `MaterialDivider.SetOrientation(separator, Orientation.Vertical)`、`SetThickness`、`SetColor`
（命名空间 `Material3Expressive.Components`、`Avalonia.Layout`）。
`separator.ClearValue(MaterialDivider.ColorProperty)` 恢复动态默认色；全局定制见
[Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。

## 布局与边界

水平线沿有界宽度填满，竖直线沿有界高度填满；该方向无界时不会自动得到长度，
应由父布局或显式 Width/Height 提供。Margin 是外部留白，没有额外内置 inset。
显式增加横截面尺寸不改变描边厚度，也不会让线条在该空间居中；中心仍在 thickness/2。

`Thickness="0"` 保留零布局厚度与 Skia hairline，不换成 1/density 的布局值；
这是当前 HorizontalDivider/VerticalDivider 的语义，不提供旧废弃 Divider 的兼容入口。
负厚度保留零布局和 Skia Paint 行为，不替换为默认值。
组件需要 Avalonia.Skia；主题本身不裁剪，祖先仍可裁剪外溢描边。

Separator 默认不可聚焦、不可命中，无内容槽、事件、命令或动画；禁用不改变颜色。
当前双端视觉验收范围仅固定 Windows x64、density=4，其他 DPI、平台和真实 OS 无障碍客户端未经验证。
Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 展示水平、竖直与定制厚度。
