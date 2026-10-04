# ScrollViewer

MaterialTheme 为原生 `Avalonia.Controls.ScrollViewer` 提供默认主题；无需 FluentTheme。
公开类型、Offset、滚动条、键盘命令和 automation 仍由 Avalonia 持有，模板的
`PART_ContentPresenter` 使用 SmoothScroll 子模块的 `ScrollViewerPresenter`。
这是 Avalonia 平滑滚动集成，不是 Compose Material3 的独立 ScrollViewer 组件移植。

```xml
<ScrollViewer HorizontalScrollBarVisibility="Disabled"
              VerticalScrollBarVisibility="Auto">
  <StackPanel><!-- Scrollable content --></StackPanel>
</ScrollViewer>
```

`MaterialScrollViewerTheme` 可通过 BasedOn 派生；`MaterialScrollViewerTemplate` 可在自定义主题中复用。
AppBarScrollScope 按实际 SmoothScroll presenter 的能力接入，不核验模板身份，也不修改应用的 Theme / Template。
内容、ContentTemplate、Padding、内容对齐、Offset、chaining、inertia 和 snap 参数从 owner 绑定到 presenter。
透明背景让空白视口也能接收指针输入。

水平 / 垂直滚动条使用 Material ScrollBar 主题和原生 `PART_HorizontalScrollBar` /
`PART_VerticalScrollBar`。Grid 的 Auto 行列预留可见滚动条厚度；Auto 无需滚动时收回空间，
Hidden 允许滚动但不显示滚动条，Disabled 同时关闭该轴滚动与无限测量。
range、value、paging、AllowAutoHide 和 deferred drag 继续消费 Avalonia 原生绑定。

`ILogicalScrollable` 的 extent / viewport / offset 和步进保持内容声明的逻辑单位；
Material TextField 内部原生 ScrollViewer 也使用该主题，保留 TextPresenter 与原生编辑语义。
AppBar 的物理 px / px/s 协议及屏幕组合接入规则见 [AppBar](../AppBar/README.md)。

SmoothScroll 的普通指针、直接滚轮和惯性逐帧位移共用 tracker 的增量消费入口；
AppBar 作为可选纵向 pre/post 参与者接入。未安装参与者时 pre/post 消费为零，
横纵向位移一起提交；正文、定位动画和几何校正共用有效范围。
普通 touch/pen 保留弹性回弹，AppBar 保留当前夹取正文的规则；直接 Offset、BringIntoView
和程序化定位动画不经过 pre/post。参与者协议使用 SmoothScroll.Avalonia.Interaction，
合成输出使用 SmoothScroll.Avalonia.Composition；原生 ScrollViewer 的 C#/XAML 用法不变。

SmoothScroll 子模块通过 ProjectReference 接入，依赖准备方式见[根 README](../../../../README.md#本地开发)。
[ScrollViewerTests](../../../../tests/Material3Expressive.Tests/ScrollViewerTests.cs) 覆盖普通物理布局、逻辑内容与 TextField 集成；
AppBar 正文输入与嵌套接入见 [AppBar 的验证边界](../AppBar/README.md#验证边界)。

2026-10-05 合入主分支时，Release 全解决方案构建、973 项控件测试及 124 项 Parity 系统测试通过。
固定 SmoothScroll 提交 `81f5516` 的 133 项测试在显式 `AvaloniaVersion=12.1.2` 下通过；
子模块默认仍声明 12.0.0，主库最终解析 12.1.2。MainWindow Scaffold demo 的 Headless 检查
在位置回读并同步布局后验证鼠标显示/关闭消息、FAB、Bars/RTL/位置切换及窗口关闭取消。
该检查不证明动画期间的布局坐标逐帧同步，也不代替实体桌面输入验收。
