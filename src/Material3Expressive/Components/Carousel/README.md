# Carousel

`MultiAspectCarouselItem` 为不同宽高比的内容添加视口边缘遮罩和视差。内容保留原布局尺寸，可放在横向或纵向列表、网格及自定义滚动布局中。

## 基础用法

在已加载 MaterialTheme 的应用中，为原生物理滚动视口声明模板，再放入内容项：

```xml
<ScrollViewer xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
              Width="320" Height="160"
              HorizontalScrollBarVisibility="Hidden" VerticalScrollBarVisibility="Disabled">
  <ScrollViewer.Template>
    <ControlTemplate TargetType="ScrollViewer">
      <ScrollContentPresenter Name="PART_ContentPresenter"
          Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"
          Offset="{TemplateBinding Offset}" CanHorizontallyScroll="True"
          CanVerticallyScroll="False" ClipToBounds="True" />
    </ControlTemplate>
  </ScrollViewer.Template>
  <StackPanel Orientation="Horizontal" Spacing="8">
    <m3:MultiAspectCarouselItem Width="240" Height="160" CornerRadius="24" Background="SkyBlue">
      <TextBlock Text="Landscape" Margin="24" />
    </m3:MultiAspectCarouselItem>
    <m3:MultiAspectCarouselItem Width="100" Height="160" CornerRadius="24" Background="Coral">
      <TextBlock Text="Portrait" Margin="12" />
    </m3:MultiAspectCarouselItem>
  </StackPanel>
</ScrollViewer>
```

## 变体与属性

- 横向默认；纵向设置 `Orientation="Vertical"` 并启用视口纵向滚动。
- 未指定 `Viewport` 时，使用最近 ScrollViewer 的内容呈现器；可显式指定其他裁剪视口。
- 列表按视口的交叉轴尺寸计算宽高比；网格项设置 `UseItemCrossAxisSize="True"`，使用内容项自身的交叉轴尺寸。
- `CornerRadius` 设置遮罩圆角；`BorderBrush` / `BorderThickness` 绘制遮罩边框，不占用内容空间。
- 边框首次启用时记住圆角，后续 `CornerRadius` 更新仅更新内容遮罩；移除并重新启用边框才重新取形状。这保留固定版本的边框生命周期。
- 只读、可绑定的 `ActualDrawInfo` 提供当前遮罩起点、终点、尺寸和视差距离，单位为 DIP。

## 内容槽与定制

`Content` / `ContentTemplate` 可承载图片、文字和交互控件。`Background` 和内容一起产生视差，边框保持在遮罩上；子控件自己的变换与绑定不被替换。

网格项目应明确启用自己的交叉轴尺寸，例如在横向滚动的 Grid 内：

```xml
<m3:MultiAspectCarouselItem Grid.Row="1" Grid.Column="2"
    UseItemCrossAxisSize="True" CornerRadius="20" Background="SkyBlue">
  <Image Source="{Binding Thumbnail}" Stretch="UniformToFill" />
</m3:MultiAspectCarouselItem>
```

显式提供读数：

```csharp
using Material3Expressive.Components;

item.DrawInfo = MultiAspectCarouselItemDrawInfo.Calculate(
    viewportStart: 0, viewportEnd: 320,
    itemOffset: -40, mainAxisSize: 240, crossAxisSize: 160);
// 恢复自动测量：
item.DrawInfo = null;
```

同一次计算的参数必须使用相同坐标系和单位；赋给控件的结果使用 DIP。自定义布局负责在滚动、尺寸、可见项变化时更新读数。

## 使用限制与注意事项

自动测量针对 UI 布局坐标随滚动更新的视口。全局 Material ScrollViewer 使用合成线程平滑滚动，不能把其 UI 坐标当作逐帧最终显示坐标；当前使用上面的原生物理 presenter，或由自定义宿主显式提供同步的 `DrawInfo`。该组件不更改全局滚动主题。

容器不拥有滚动、吸附、惯性或分页状态；滚动和键盘焦点仍由宿主及内容处理。视口必须裁剪溢出内容，沿滚动轴提供有限尺寸。当前支持圆角矩形遮罩。

Catalog 的 **Multi-aspect carousel** 演示两轴滚动、交互内容、RTL、圆角切换和边框。移植范围与验证见 [INTERNALS.md](INTERNALS.md)。
