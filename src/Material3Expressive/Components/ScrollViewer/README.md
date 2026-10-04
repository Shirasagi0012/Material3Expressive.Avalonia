# ScrollViewer

使用原生 Avalonia `ScrollViewer`，Material 主题提供默认外观和 SmoothScroll presenter。
公开内容、Offset、滚动条、键盘命令与 automation 仍是 Avalonia API。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)；实现与滚动协议见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <ScrollViewer Height="240" HorizontalScrollBarVisibility="Disabled"
                VerticalScrollBarVisibility="Auto">
    <StackPanel Spacing="16">
      <TextBlock Text="滚动内容" />
      <Border Height="600" Background="#EADDFF" />
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

滚动方向需要有限的视口约束；示例用 Height，也可由 Grid 的有限行高提供。
沿滚动方向放入无限测量的外层布局时，内容可能全部展开而无需滚动。
本主题自身不依赖 FluentTheme；若需滚动条的基础菜单或其他原生控件主题，仍按公共接入加载基础主题。

## 滚动与内容

- `Auto` 在需要时显示滚动条并预留其厚度，不需要时收回空间
- `Hidden` 允许滚动但隐藏滚动条；`Disabled` 同时关闭该方向滚动与无限测量
- `Content` / `ContentTemplate`、`Padding`、内容对齐、chaining、inertia 和 snap 属性继续从 owner 传给 presenter
- `Offset` 使用原生 `Vector`；可以绑定应用状态，或在 C# 中设置 `viewer.Offset = new Vector(0, 120)`（`Avalonia`）
- 使用 `ScrollChanged` 观察实际变化；指针滚轮量不等于最终消费距离

逻辑滚动内容实现 `ILogicalScrollable` 时，extent / viewport / offset 和步进使用内容声明的逻辑单位，
不能一概当作 DIP。TextField 内部的 ScrollViewer 同样使用这个主题，编辑器行为仍归原生 TextBox。
滚动条颜色、厚度与独立使用见 [ScrollBar](../ScrollBar/README.md)。

## 主题与 AppBar 接入

`MaterialScrollViewerTheme` 可通过 `BasedOn` 派生，`MaterialScrollViewerTemplate` 可供自定义主题复用。
默认透明 Background 让空白视口也能接收指针输入。替换模板时需保留原生命名部件及相应绑定；
普通 ScrollContentPresenter 不会自动具有 SmoothScroll 的参与者能力。

AppBar 的屏幕组合使用 `AppBarScrollScope`，根据实际 presenter 能力连接，不要求某个 Theme 身份，
也不改写应用的 Theme / Template。主正文、Padding 放置和物理 px / px/s 协议见
[AppBar](../AppBar/README.md)，不要与 FloatingToolbar 的 DIP 回调混用。
普通滚动与 AppBar 的输入分配、程序化定位例外见 [INTERNALS](INTERNALS.md#输入与合成接入)。

实际桌面触屏/触控笔、惯性、辅助技术及动画期间坐标的完整验收仍需针对平台执行。
Catalog 的 [ScrollBarDemo](../../../Material3Expressive.Catalog/ScrollBarDemo.axaml) 展示普通 ScrollViewer，
[MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 展示 Scaffold 组合。
