# FloatingActionButton

本组件族包含图标 FAB、Extended FAB、受控 Toggle FAB 和内联 FAB 菜单。
它们有独立控件类型，按钮命令与 Click 仍沿用 Avalonia Button。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，实现与移植边界见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:FloatingActionButton AutomationProperties.Name="新建">
    <m3:MaterialSymbol Symbol="Add" Width="24" Height="24" />
  </m3:FloatingActionButton>
</UserControl>
```

后续片段使用相同命名空间。图标内容自行提供尺寸及有意义的无障碍名称；容器不会缩放任意 Content。

## 尺寸与 Extended 内容

| Size | 图标 FAB 容器 | Extended 高度 | 建议图标尺寸 |
| --- | --- | --- | --- |
| Default | 56 × 56 DIP | 56 DIP | 24 DIP |
| Small | 40 × 40 DIP，外层至少 48 DIP 分配 | 56 DIP | 24 DIP |
| Medium | 80 × 80 DIP | 80 DIP | 28 DIP |
| Large | 96 × 96 DIP | 96 DIP | 36 DIP |

尺寸用 `Size`，不是普通 Button 的尺寸 class。建议图标尺寸也可在 C# 中取
`FloatingActionButtonDefaults.MediumIconSize` / `LargeIconSize`。

```xml
<m3:ExtendedFloatingActionButton Content="新建" Size="Medium" Expanded="True">
  <m3:ExtendedFloatingActionButton.Icon>
    <m3:MaterialSymbol Symbol="Add" Width="28" Height="28" />
  </m3:ExtendedFloatingActionButton.Icon>
</m3:ExtendedFloatingActionButton>
```

不设置 Icon 时采用纯内容入口；提供 Icon 后，`Expanded` 控制图标+文字入口的展开/收起。
Content / ContentTemplate、Icon / IconTemplate 均支持普通数据和模板。字符串采用相应尺寸排版。
`Background`、`Foreground`、`CornerRadius`、`Command` / `CommandParameter`、Click 和 IsEnabled
使用按钮入口。绑定 Command 时 DataContext 需提供相应 ICommand；事件需在视图中实现处理器。

## 显隐与 elevation

用 `AnimatedVisible` 控制带缩放/透明度的显示，适用于普通和 Extended FAB；直接 IsVisible 不执行此过渡。
隐藏动画到 alpha 为零后才移除布局。可用 HiddenScale、VisibilityAlignment、VisibilityScaleSpec /
VisibilityAlphaSpec 定制；默认缩到 0.2，枢轴为逻辑 BottomEnd。

Elevation 配置可在 C# 中替换（`Material3Expressive.Components`）：

```csharp
fab.Elevation = FloatingActionButtonDefaults.LoweredElevation();
fab.AnimatedVisible = false;
```

这里 `fab` 是视图中现有的 FloatingActionButton。默认 elevation 的静止/按压/焦点/悬停值为
6/6/6/8 DIP；LoweredElevation 为 1/1/1/3。阴影状态与 tonal elevation 的具体关联见维护说明。

## 受控 Toggle 与菜单

`ToggleFloatingActionButton` 激活时发送 `CheckedChangeRequested`，不自行改写 IsChecked。
菜单的 Expanded 也由应用管理，选择某项后是否关闭需应用决定。下面 XAML 片段放入视图，
并将 C# 处理器放入同一视图的 code-behind：

```xml
<m3:FloatingActionButtonMenu x:Name="Actions" MenuAlignment="End" MaxHeight="320">
  <m3:FloatingActionButtonMenu.Button>
    <m3:ToggleFloatingActionButton x:Name="ActionToggle" Size="Medium"
                                  CheckedChangeRequested="OnActionToggleRequested"
                                  AutomationProperties.Name="更多操作">
      <m3:MaterialSymbol Symbol="Add" Width="28" Height="28" />
    </m3:ToggleFloatingActionButton>
  </m3:FloatingActionButtonMenu.Button>
  <m3:FloatingActionButtonMenuItem Content="编辑">
    <m3:FloatingActionButtonMenuItem.Icon>
      <m3:MaterialSymbol Symbol="Edit" Width="24" Height="24" />
    </m3:FloatingActionButtonMenuItem.Icon>
  </m3:FloatingActionButtonMenuItem>
</m3:FloatingActionButtonMenu>
```

```csharp
private void OnActionToggleRequested(object? sender,
    Material3Expressive.Components.FabCheckedChangeRequestedEventArgs e)
{
    ActionToggle.IsChecked = e.IsChecked;
    Actions.Expanded = e.IsChecked;
}
```

菜单是内联布局，不是 Popup。Items 保持源顺序、从底部开始显示；Button / ButtonTemplate 是独立槽。
受限高度下只滚动条目，按钮固定，收起期间关闭列表滚动输入；展开时从按钮 Tab / Down 可移入首个可见条目。

Toggle 的动画读数为 `CheckedProgress`、`IconSize` 和 `IconColor`。上例刻意保持固定图标；
若要图标随进度变化，绑定 IconSize 到宽高，IconColor（Color）经 Avalonia `ColorToBrushConverter`
转为画刷。完整绑定见 [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml)。
ContainerColor / ContainerSize / ContainerCornerRadius 接受进度函数，可用
`ToggleFloatingActionButtonDefaults` 的映射函数定制。Toggle 使用 Default / Medium / Large 三档默认造型，
不将普通 FAB 的 Small 40 DIP 规则套给它。

## 使用边界

任意内容不会自动获得图标 tint，整层 SrcIn tint 尚未实现。图标 Color 绑定仅适用于可设置颜色的内容。
菜单滚轮与触摸惯性由 Avalonia 平台决定；原生 Enter 按下激活的适配沿用共享决定。
几何双端测试不证明文字、字体图标、真实桌面输入或所有平台一致。
更多内部时序、历史差异及验证入口见 [INTERNALS](INTERNALS.md#验证与边界)。
