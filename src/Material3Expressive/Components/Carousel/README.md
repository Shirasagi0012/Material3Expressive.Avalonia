# Carousel

`HorizontalCarousel` 提供 MultiBrowse、Uncontained 和 CenteredHero 三种横向布局，支持数据模板、项目跳转、滚轮、键盘分页和动画。`MultiAspectCarouselItem` 为不同宽高比的内容添加视口边缘遮罩和视差，可放在横向或纵向列表、网格及自定义滚动布局中。

## 基础用法

经典布局使用有限的视口宽度，并按需给项目设置圆角：

```xml
<m3:HorizontalCarousel xmlns="https://github.com/avaloniaui"
    xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
    Width="400" Height="160" ItemWidth="240" ItemSpacing="8">
  <m3:CarouselItem CornerRadius="24" Background="SkyBlue"><TextBlock Text="Mountains" Margin="24" /></m3:CarouselItem>
  <m3:CarouselItem CornerRadius="24" Background="Coral"><TextBlock Text="Coast" Margin="24" /></m3:CarouselItem>
  <m3:CarouselItem CornerRadius="24" Background="LightGreen"><TextBlock Text="Forest" Margin="24" /></m3:CarouselItem>
</m3:HorizontalCarousel>
```

MultiAspect 不拥有滚动容器。在已加载 MaterialTheme 的应用中，为它声明原生物理滚动视口模板，再放入内容项：

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

- `HorizontalCarousel.Variant` 选择 `MultiBrowse`（默认）、`Uncontained`、`CenteredHero`。
- `ItemWidth` 分别表示首选宽度、项目宽度和大项目最大宽度；省略时使用可用宽度。布局可能根据视口和项目数量调整实际宽度。
- `MinSmallItemWidth` / `MaxSmallItemWidth` 调整 MultiBrowse 小项目的首选范围，默认 40 / 56 DIP。
- `ItemSpacing` 是项目间距；`ContentPadding` 的左右值表示逻辑起始／末尾留白，RTL 时交换物理侧。上下值为内容区域留白。
- `ScrollOffset` 可绑定连续的逻辑滚动距离；`MaximumScrollOffset`、`CurrentItem` 和 `CurrentItemOffsetFraction` 是只读布局结果。
- 布局完成后调用 `ScrollToItem(index)` 或 `await AnimateScrollToItemAsync(index)`；后者可传 `IAnimationSpec<float>` 和取消令牌。新的导航会取消旧动画，卸载也会取消。
- 触屏／笔拖动使用 18 DIP 阈值，释放后按 `FlingMode` 吸附或衰减。`Default` 为 MultiBrowse / CenteredHero 选择 `SingleAdvance`，为 Uncontained 选择 `NoSnap`；也可显式选择 `MultiBrowse` 多项惯性吸附。
- `FlingAsync(velocity)` 接受逻辑方向的 DIP/s，返回未消费速度。`SnapAnimationSpec` / `DecayAnimationSpec` 可替换 fling 规格，规格内部值使用物理像素；不影响程序导航的单独规格。
- 水平滚轮输入自动合并并平滑滚动，结束后使用当前 `FlingMode`；纵向输入留给外层滚动区。RTL 自动反转逻辑方向，静止时首尾向外的事件可继续传递。
- 当项目内的控件持有键盘焦点时，PageUp / PageDown 按一个视口宽度即时滚动，不额外吸附；Ctrl 组合键和子控件已处理的按键不接管。
- 项目内的控件获得焦点或调用 `BringIntoView()` 时，自动按 Pager 分页网格弹簧滚入。可以直接调用内容按钮的 `Focus()`，无需先导航；`UserScrollEnabled="False"` 也允许焦点滚入。新导航和用户拖动会取消旧的滚入动画。
- 视口缩小时，原本可见的焦点内容会继续滚入可见范围；放大视口不主动启动焦点动画。尺寸变化按当前页和页内比例重新计算位置，保持当前项目。
- `UserScrollEnabled` 控制手势和键盘分页；程序导航不受影响。`IsScrollInProgress` 包括拖动和动画。`InteractionSource` 发布配对的 `DragStart` / `DragStop` / `DragCancel`，结束事件引用原开始对象。
- 辅助技术可通过原生滚动接口前后翻页或设置滚动百分比；`UserScrollEnabled="False"` 同时移除用户分页操作。Catalog 提供实际调用这一接口的前后翻页按钮。
- `CarouselItem.DrawInfo` 发布当前遮罩尺寸、最小／最大尺寸和局部遮罩矩形，可绑定实现内容淡化等效果。

`MultiAspectCarouselItem` 的属性：

- 横向默认；纵向设置 `Orientation="Vertical"` 并启用视口纵向滚动。
- 未指定 `Viewport` 时，使用最近 ScrollViewer 的内容呈现器；可显式指定其他裁剪视口。
- 列表按视口的交叉轴尺寸计算宽高比；网格项设置 `UseItemCrossAxisSize="True"`，使用内容项自身的交叉轴尺寸。
- `CornerRadius` 设置遮罩圆角；`BorderBrush` / `BorderThickness` 绘制遮罩边框，不占用内容空间。
- 边框首次启用时记住圆角，后续 `CornerRadius` 更新仅更新内容遮罩；移除并重新启用边框才重新取形状。这保留固定版本的边框生命周期。
- 只读、可绑定的 `ActualDrawInfo` 提供当前遮罩起点、终点、尺寸和视差距离，单位为 DIP。

## 内容槽与定制

`HorizontalCarousel` 支持 `ItemsSource`、`ItemTemplate` 和 `ItemContainerTheme`，自动创建 `CarouselItem`。在容器主题中设置 `CornerRadius`、`BorderBrush`、`BorderThickness`；内容保留完整宽度，仅改变可见遮罩。可在内容中放图片、文字或按钮。

`MultiAspectCarouselItem.Content` / `ContentTemplate` 可承载图片、文字和交互控件。`Background` 和内容一起产生视差，边框保持在遮罩上；子控件自己的变换与绑定不被替换。

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

经典容器支持触屏／笔拖动、水平滚轮、PageUp/PageDown 及程序导航，鼠标左键拖动不启用。原生滚轮事件采用当前平台的默认单位换算；设备专用触控板 Pan/惯性和嵌套滚动尚未实现。普通纵向滚轮不会改为横向浏览，鼠标用户也可用外部滑块绑定 `ScrollOffset` 或导航按钮。它不虚拟化项目，适合有限集合；因此离屏内容仍可获得焦点，与 Compose 未组合项目的焦点拒绝存在差异。请勿在滚动方向无限测量的容器中使用。动画需要按接入指南初始化窗口的 `AnimationScope`。

`CarouselItem` 的遮罩与边框都记住首次形状，与 MultiAspect 生命周期不同；布局中的遮罩位置和尺寸仍持续更新。在吸附点直接调用单项 fling 可以消费速度但不换页。中途重定向程序动画按固定移植基线计算位移，可能保留起始页内偏移；需要精确定位时调用 `ScrollToItem`。

MultiAspect 自动测量针对 UI 布局坐标随滚动更新的视口。全局 Material ScrollViewer 使用合成线程平滑滚动，不能把其 UI 坐标当作逐帧最终显示坐标；当前使用上面的原生物理 presenter，或由自定义宿主显式提供同步的 `DrawInfo`。该组件不更改全局滚动主题。

`MultiAspectCarouselItem` 不拥有滚动、吸附、惯性或分页状态；滚动和键盘焦点仍由宿主及内容处理。视口必须裁剪溢出内容，沿滚动轴提供有限尺寸。当前支持圆角矩形遮罩。

Catalog 的 Carousel 窗口同时演示三种经典布局及 MultiAspect 的两轴滚动、交互内容、RTL、圆角切换和边框。移植范围与验证见 [INTERNALS.md](INTERNALS.md)。
