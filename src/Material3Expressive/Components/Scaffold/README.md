# Scaffold

`Scaffold` 是库提供的屏幕布局容器，组合正文、顶部栏、底部栏、Snackbar 和浮动按钮。
它继承 Avalonia `ContentControl`，只负责布局；各槽放入真实组件或自己的内容。
首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

Scaffold 需要有限宽高；普通页面的 Grid 可提供约束。放入纵向 StackPanel 或 ScrollViewer 时，
必须显式指定 Height，横向无界时同理指定 Width。下面可直接作为页面内容使用：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:Scaffold x:Name="Screen">
    <m3:Scaffold.TopBar>
      <m3:TopAppBar Title="收件箱" />
    </m3:Scaffold.TopBar>
    <m3:Scaffold.BottomBar>
      <m3:NavigationBar>
        <m3:NavigationBarItem Label="收件箱" IsSelected="True" />
        <m3:NavigationBarItem Label="收藏" />
      </m3:NavigationBar>
    </m3:Scaffold.BottomBar>
    <Border Padding="{Binding ContentPadding, ElementName=Screen}"
            m3:MaterialWindowInsets.ConsumePadding="{Binding ContentPadding, ElementName=Screen}">
      <TextBlock Margin="24" Text="页面正文" />
    </Border>
  </m3:Scaffold>
</UserControl>
```

正文默认从原点开始，Scaffold 不自动增加 Padding。示例同时应用 `ContentPadding` 并标记已消费，
避免嵌套 Scaffold 重复计算同一 Insets；ConsumePadding 只传播消费事实，不会自行加内边距。
后续片段沿用相同命名空间。

## 内容槽与浮动按钮

正文使用原生 `Content` / `ContentTemplate`；`TopBar`、`BottomBar`、`SnackbarHost`、
`FloatingActionButton` 各有对应的 `...Template`。槽接受控件或数据对象。

```xml
<m3:Scaffold FloatingActionButtonPosition="End">
  <m3:Scaffold.FloatingActionButton>
    <Button Content="新建" />
  </m3:Scaffold.FloatingActionButton>
  <TextBlock Text="此示例只展示浮动位置" />
</m3:Scaffold>
```

上例使用普通 Button 说明槽位；Scaffold 不会把槽内容转换为 Material FAB。
`FloatingActionButtonPosition` 可选 `Start`、`Center`、`End`（默认）、`EndOverlay`，Start/End 跟随 RTL。
默认避开底栏并留 16 DIP；EndOverlay 从底部 Insets 计算，不避让 BottomBar。
Snackbar 根据底栏与实际 FAB 大小向上避让。

Scaffold 没有点击事件、选择状态或消息队列；事件、命令和动态状态属于槽中组件。
需要 Snackbar 队列时使用 [Snackbar](../Snackbar/README.md) 的入口，需要滚动折叠栏时见
[AppBar](../AppBar/README.md#滚动联动)。

## Padding、Insets 与颜色

`ContentWindowInsets` 接受物理 left/top/right/bottom，桌面默认零，由应用提供实际安全区域。
`ContentPadding` 是只读逻辑 start/top/end/bottom，可直接绑定 Border.Padding。
存在顶/底槽时以其实际高度代替该侧剩余 Insets；即使槽高度为零，也算存在。
TopBar / BottomBar 自己处理 Insets；Snackbar/FAB 使用剩余水平和底部 Insets。
继承的 `Padding` 不是 ContentPadding 的替代入口。

普通滚动页面可把应用和消费 Padding 的容器放在 ScrollViewer 的内容中。
标准模板内置 AppBarScrollScope；栏声明 ScrollBehavior 后自动联动，正文必须采用 [AppBar 的主正文结构](../AppBar/README.md#滚动联动)：
填满 Scaffold 的正文根应用 Padding，内部才是物理主 ScrollViewer。两种结构的滚动目标不同，不宜混用。

`Background` 默认主题 Background 色；未显式设置 Foreground 时，内容色按主题角色匹配或回退到环境色。
可以使用原生属性局部覆盖，或按[公共接入](../../../../docs/GETTING_STARTED.md)覆盖 Token。

## 使用边界

Scaffold 不支持 intrinsic / wrap-content 测量，不自动接管系统栏或 IME，也不提供自己的动画。
实际背景、裁剪由 MaterialSurface 承担；替换整个 Template 时需保留五槽布局合约，
自动 AppBar 滚动连接还需 PART_ScrollScope，栏模板根需直接为 AppBar。
真实系统 Insets、屏幕阅读器与硬件触摸仍需专项验证。

Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 展示 FAB 位置、Bars、Snackbar 和 RTL。
测量顺序、共享决定与跟踪测试见 [INTERNALS.md](INTERNALS.md)。
