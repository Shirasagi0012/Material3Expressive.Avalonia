# NavigationDrawer

本目录提供 `ModalNavigationDrawer`、`DismissibleNavigationDrawer`、`PermanentNavigationDrawer`，
对应的三种 DrawerSheet、目的地 `NavigationDrawerItem` 及可由应用持有的 `DrawerState`。
首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

下面使用永久抽屉，因此无需开关状态或事件处理器。Drawer 的正文放页面，DrawerContent 放 Sheet；
Sheet 内用普通 Panel 组织目的地。示例中的选择只是初值，应用负责更新。

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:PermanentNavigationDrawer>
    <m3:PermanentNavigationDrawer.DrawerContent>
      <m3:PermanentDrawerSheet>
        <StackPanel Margin="12,16" Spacing="4">
          <m3:NavigationDrawerItem Content="收件箱" IsSelected="True">
            <m3:NavigationDrawerItem.Icon>
              <m3:MaterialSymbol Symbol="Inbox" Width="24" Height="24" />
            </m3:NavigationDrawerItem.Icon>
            <m3:NavigationDrawerItem.Badge><TextBlock Text="24" /></m3:NavigationDrawerItem.Badge>
          </m3:NavigationDrawerItem>
          <m3:NavigationDrawerItem Content="收藏" />
        </StackPanel>
      </m3:PermanentDrawerSheet>
    </m3:PermanentNavigationDrawer.DrawerContent>
    <TextBlock Margin="24" Text="页面内容" />
  </m3:PermanentNavigationDrawer>
</UserControl>
```

后续片段沿用这些命名空间。Sheet 自动宽度为 240–360 DIP（仍受父约束限制），填满有限高度。
显式 Width 使用调用方约束；Item 的默认最小背景高度为 56 DIP，12 DIP 外侧留白由调用方添加。

## 选择容器与控制状态

- Permanent：始终显示，为页面分配剩余宽度，无 State
- Dismissible：通过 State 打开/关闭，推动仍保留原测量宽度的页面
- Modal：在本地布局中覆盖页面并绘制 scrim，不是窗口级 Popup

Modal / Dismissible 默认各自创建一个 Closed 状态。Sheet 的 State 是可选项；绑定容器的同一个实例，
可以启用打开过冲补偿及原生 BackRequested。不要分别创建两个无关状态。

```xml
<m3:ModalNavigationDrawer x:Name="Drawer">
  <m3:ModalNavigationDrawer.DrawerContent>
    <m3:ModalDrawerSheet State="{Binding State, ElementName=Drawer}">
      <m3:NavigationDrawerItem Content="收件箱" IsSelected="True" />
    </m3:ModalDrawerSheet>
  </m3:ModalNavigationDrawer.DrawerContent>
  <TextBlock Text="页面内容" />
</m3:ModalNavigationDrawer>
```

上述片段初始关闭。所属视图中在挂载后使用 C# 打开，或由绑定的 ViewModel 命令调用同一个 State：

```csharp
// Drawer 是上方已挂载的具名控件；在 UI 线程调用。
await Drawer.State.OpenAsync();
await Drawer.State.CloseAsync();
await Drawer.State.SnapToAsync(Material3Expressive.Components.DrawerValue.Open);
```

异步方法接受 CancellationToken；后续操作、外部取消或卸载可能取消进行中的任务。
`CurrentValue` / `IsOpen` 表示已完成状态，`TargetValue` 表示目标，`CurrentOffset` 是逻辑起始轴 DIP，
首次布局前为 NaN。不要用 CurrentValue 判断动画中的实际绘制位置。
需要确认状态变化时，在 C# 构造 `new DrawerState(DrawerValue.Closed, target => ...)`；
该回调沿用上游语义，拒绝状态变化并不保证已改变的 offset 回滚，详情见 [INTERNALS](INTERNALS.md#状态与布局不变量)。

## 目的地、内容槽与定制

Drawer 提供 `ContentTemplate` / `DrawerContentTemplate`，Sheet 使用普通 `Content` / `ContentTemplate`。
Item 的标签是 `Content` / `ContentTemplate`，图标和徽标分别是 `Icon` / `IconTemplate`、`Badge` / `BadgeTemplate`。
字符串标签默认 LabelLarge。

```xml
<m3:NavigationDrawerItem Content="收件箱"
                         IsSelected="{Binding InboxSelected}"
                         Command="{Binding OpenInboxCommand}" />
```

片段假设 DataContext 提供这些成员，编译绑定还需 `x:DataType`。Click / Command、Enter/Space 松开、
自动化 Select 只请求导航，不修改 IsSelected 或自动关闭 Drawer。应用负责互斥选择及是否调用 CloseAsync。
也支持原生 CommandParameter、IsEnabled 与 CanExecute。

Sheet 可使用 `Background`、`Foreground`、`CornerRadius`、`TonalElevation` 和物理 `WindowInsets`；
Modal 的 scrim 用 `ScrimBrush`。Item 可分别覆盖 Selected/Unselected 的 Container、Icon、Text、Badge 颜色。
这些属性不同于环境 Foreground；统一主题定制见[公共 Token 接入](../../../../docs/GETTING_STARTED.md)。

## 使用边界

`GesturesEnabled="False"` 关闭拖动和 scrim 点击，不阻止程序化或键盘关闭。
打开完成后请求抽屉内焦点，关闭时退出命中和 Tab 遍历；当前没有焦点陷阱，也不恢复关闭前焦点。
抽屉内 Escape KeyUp 可关闭，绑定 State 的 Sheet 响应离散 BackRequested；predictive-back 连续变形尚未实现。
系统 Insets 由应用提供，真实 OS 触摸、文字及屏幕阅读器仍需单独验证。

Catalog 示例见 [NavigationDrawerDemo.axaml](../../../Material3Expressive.Catalog/NavigationDrawerDemo.axaml)；
状态确认顺序、已接受数值差异与验证入口见 [INTERNALS.md](INTERNALS.md)。
