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
    <m3:FloatingActionButton.Icon>
      <m3:MaterialSymbol Symbol="Add" />
    </m3:FloatingActionButton.Icon>
  </m3:FloatingActionButton>
</UserControl>
```

后续片段使用相同命名空间。`Icon` / `IconTemplate` 是 FAB 的独立图标槽；未设置二者时继续显示普通
`Content` / `ContentTemplate`，设置任一图标属性后显示图标槽。图标默认尺寸为 Default/Small 24、Medium 28、
Large 36 DIP，`MaterialIcon.DefaultSize` 提示由图标实现消费，显式 `Width` / `Height` 优先。

## 尺寸与 Extended 内容

| Size | 图标 FAB 容器 | Extended 高度 | 建议图标尺寸 |
| --- | --- | --- | --- |
| Default | 56 × 56 DIP | 56 DIP | 24 DIP |
| Small | 40 × 40 DIP，外层至少 48 DIP 分配 | 56 DIP | 24 DIP |
| Medium | 80 × 80 DIP | 80 DIP | 28 DIP |
| Large | 96 × 96 DIP | 96 DIP | 36 DIP |

尺寸用 `Size`，不是普通 Button 的尺寸 class。图标槽的默认尺寸由主题绑定到
`MaterialIcon.DefaultSize`；Large 使用独立的 `MaterialTokens.Comp.FabLarge.ContentIconSize`，
保留 Composable 的 36 DIP 修正，原始 `IconSize` Token 仍为 32 DIP。

```xml
<m3:ExtendedFloatingActionButton Content="新建" Size="Medium" Expanded="True">
  <m3:ExtendedFloatingActionButton.Icon>
    <m3:MaterialSymbol Symbol="Add" />
  </m3:ExtendedFloatingActionButton.Icon>
</m3:ExtendedFloatingActionButton>
```

Extended FAB 继承基类的 `Icon` / `IconTemplate` 属性，不另建一份图标状态。不设置图标时采用纯内容入口；
提供 Icon 或 IconTemplate 后，`Expanded` 控制图标+文字入口的展开/收起。Content / ContentTemplate、
Icon / IconTemplate 均支持普通数据和模板。字符串采用相应尺寸排版。
`Background`、`Foreground`、`CornerRadius`、`Command` / `CommandParameter`、Click 和 IsEnabled
使用按钮入口。绑定 Command 时 DataContext 需提供相应 ICommand；事件需在视图中实现处理器。

## 显隐与 elevation

用 `AnimatedVisible` 控制带缩放/透明度的显示，适用于普通和 Extended FAB；直接 IsVisible 不执行此过渡。
隐藏动画到 alpha 为零后才移除布局。可用 HiddenScale、VisibilityAlignment、VisibilityScaleSpec /
VisibilityAlphaSpec 定制；默认缩到 0.2，枢轴为逻辑 BottomEnd。

Elevation 配置可在 C# 中替换（`Material3Expressive.Components`）：

```csharp
fab.Elevation = new FloatingActionButtonElevation(1, 1, 1, 3);
fab.AnimatedVisible = false;
```

这里 `fab` 是视图中现有的 FloatingActionButton。默认 elevation 的静止/按压/焦点/悬停值为
6/6/6/8 DIP，主题从各状态 Token 动态组装；上述显式配置把它整组替换为 1/1/1/3。
使用 `fab.ClearValue(FloatingActionButton.ElevationProperty)` 恢复主题配置。阴影状态与 tonal elevation 的具体关联见维护说明。

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
      <m3:ToggleFloatingActionButton.Icon>
        <m3:MaterialSymbol Symbol="Add" />
      </m3:ToggleFloatingActionButton.Icon>
    </m3:ToggleFloatingActionButton>
  </m3:FloatingActionButtonMenu.Button>
  <m3:FloatingActionButtonMenuItem Content="编辑">
    <m3:FloatingActionButtonMenuItem.Icon>
      <m3:MaterialSymbol Symbol="Edit" />
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
菜单项 Icon / IconTemplate 的图标默认尺寸为 24 DIP。
受限高度下只滚动条目，按钮固定，收起期间关闭列表滚动输入；展开时从按钮 Tab / Down 可移入首个可见条目。

Toggle 的动画读数为 `CheckedProgress`、`IconSize` 和 `IconColor`。Icon / IconTemplate 存在时使用图标槽，
否则仍显示 Content。图标槽默认尺寸按 Default/Medium/Large 固定为 24/28/36 DIP，不跟随动画尺寸；
如需图标随进度变化，可显式绑定 `IconSize` 到图标宽高，并将 `IconColor`（Color）经 Avalonia
`ColorToBrushConverter` 转为画刷。任意 Content 不会自动获得该动画绑定。完整绑定见
[Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml)。
ContainerColor / ContainerSize / ContainerCornerRadius 接受完整进度函数，例如
`toggle.ContainerSize = progress => 80 - 24 * progress`。主题从当前变体及颜色、尺寸 Token
动态组装默认函数；显式 Style、Binding 或局部函数优先，清除后恢复当前主题。函数不会被动画写回，
不使用 null 表示“另找默认值”。Toggle 使用 Default / Medium / Large 三档默认造型，
不将普通 FAB 的 Small 40 DIP 规则套给它。

## 默认配置与覆盖

默认值按 `TokenSource → 主题 Token 绑定 → Avalonia 属性 → 布局/动画/绘制` 流动。
控件和内部部件只读取已生效的配置，不在运行时重新解析 Token 或逐字段补默认值。
`Size` 切换在主题绑定中选择配置，普通应用 Style 不会被内部尺寸条件覆盖；局部赋值、绑定及
`ClearValue` 遵循 Avalonia 优先级。局部 System / Component Token、主题变化和重新挂载会更新默认值。

`VisibilityScaleSpec` / `VisibilityAlphaSpec` 可以替换整个动画规格；默认 spring 经组件 Token
引用 System motion。任一 Visibility 规格为 null（包括绑定尚未提供值）时，新的显隐目标会等待，
两项规格恢复后再执行当前目标；null 不表示切换到另一套默认动画。Extended 的间距和尺寸、菜单的布局与 motion 通过主题直接进入内部部件属性，
不要求为每个内部参数新增公开控件属性。Medium / Large Extended 的实际图文间距覆盖入口为
`ExtendedFabMedium.ContentIconLabelSpace` / `ExtendedFabLarge.ContentIconLabelSpace`，原始
`IconLabelSpace` Token 保持 16/20 DIP，默认实际间距保持 12/16 DIP。

## 使用边界

任意内容不会自动获得图标 tint，整层 SrcIn tint 尚未实现。图标 Color 绑定仅适用于可设置颜色的内容。
菜单滚轮与触摸惯性由 Avalonia 平台决定；原生 Enter 按下激活的适配沿用共享决定。
几何双端测试不证明文字、字体图标、真实桌面输入或所有平台一致。
更多内部时序、历史差异及验证入口见 [INTERNALS](INTERNALS.md#验证与边界)。
