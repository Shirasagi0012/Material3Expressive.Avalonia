# ButtonGroup

`ButtonGroup` 横向排列操作，按压时扩宽当前项并压缩相邻项；放不下的后缀项目进入下拉菜单。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)。维护入口见 [INTERNALS](INTERNALS.md)。

## 基本使用

在已加载 Material 主题、初始化窗口动画作用域的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:ButtonGroup MaxWidth="300">
    <m3:ButtonGroupAction Label="编辑" />
    <m3:ButtonGroupAction Label="分享" />
    <m3:ButtonGroupAction Label="归档" />
    <m3:ButtonGroupAction Label="删除" IsEnabled="False" />
  </m3:ButtonGroup>
</UserControl>
```

`ButtonGroupAction` 提供 `Click`、`Command` 和 `CommandParameter`。按钮和菜单执行同一个操作，
菜单在回调和命令之后关闭。`Command.CanExecute` 同时影响两处入口。绑定命令时，调用方的
DataContext 需提供对应 `ICommand`；例如 `Command="{Binding EditCommand}"`。

`ItemsSource` 与 `ItemTemplate` 也可使用，模板的根必须是 `ButtonGroupItem` 的具体子类。
内置 Action / ToggleAction 的按钮标签关闭自动换行，宽度压缩不会触发换行；文本中的显式换行符仍会保留。
CustomItem 的内容自行配置；原生按钮可用 `TextBlock.TextWrapping="NoWrap"` 关闭自动换行。

## 图标与切换状态

Action 的 `Icon` 是数据，`IconTemplate` 分别创建按钮和菜单里的实例；模板单独存在也会占图标槽。
`Icon` 不接受直接 Control。直接控件组合使用下节的 `CustomItem`，不要把同一实例放进两个视觉树。

```xml
<m3:ButtonGroupAction Label="编辑">
  <m3:ButtonGroupAction.IconTemplate>
    <DataTemplate><m3:MaterialSymbol Symbol="Edit" /></DataTemplate>
  </m3:ButtonGroupAction.IconTemplate>
</m3:ButtonGroupAction>
```

`ButtonGroupToggleAction` 是受控切换项：`IsChecked` 只表示当前状态，点击通过
`CheckedChangeRequested` 请求相反值，不自行写回属性。菜单使用普通带图标的操作项，和上游一致。

```xml
<m3:ButtonGroupToggleAction Label="收藏" IsChecked="{Binding IsFavorite}"
                            CheckedChangeRequested="OnFavoriteRequested" />
```

上述片段假定 ViewModel 有 `IsFavorite`；code-behind 将请求写回 ViewModel：

```csharp
private void OnFavoriteRequested(object? sender, IconCheckedChangeEventArgs e)
    => viewModel.IsFavorite = e.Value;
```

## 布局与自定义项目

`ExpandedRatio` 默认 0.15，0 关闭扩宽。`Spacing` 默认 12 DIP。
`HorizontalArrangement` 支持 Start、Center、End、SpaceBetween、SpaceAround、SpaceEvenly、SpacedBy。
SpacedBy 消费 `Spacing`；`SpacingAlignment` 用 Left / Center / Right 表示逻辑起点、居中、终点，RTL 随组镜像。
组内默认顶对齐，可设 `ItemVerticalAlignment`；项目的 `Alignment` 单独覆盖它。

`Weight` 默认为 NaN，表示按固有宽度分配；正值按权重分享剩余空间。没有有限宽度时，权重项以最小宽度
为目标分配，和上游相同。`CompressionLimit` 限制该项被邻项压缩的最大 DIP，默认来自 Token（24 DIP）。

`ButtonGroupCustomItem` 的 `Content/ContentTemplate` 与 `MenuContent/MenuContentTemplate` 是两份独立内容。
自定义内容通过 `PressSource` 显式指定实际交互控件；未指定时不参与扩宽，也不提供可压缩额度。

```csharp
var button = new Button { Content = "自定义操作" };
var group = new ButtonGroup();
var menuItem = new DropdownMenuItem { Content = "自定义操作" };
menuItem.Click += (_, _) => group.MenuState.Dismiss();
group.Items.Add(new ButtonGroupCustomItem
{
    Content = button,
    PressSource = button,
    CompressionLimit = 16,
    MenuContent = menuItem
});
```

自定义菜单自行决定关闭时机。`MenuState.Show()` / `Dismiss()` 可供代码调用；`OverflowIndicatorTemplate`
的 DataContext 是该 `ButtonGroupMenuState`。默认指示器为带 tooltip 的 Filled IconButton。

## Connected 组合与 Token

connected 使用原生 ToggleButton，首、中、尾主题只配置圆角；选择策略由调用方维护。
默认间距也来自 Token：

```xml
<StackPanel Orientation="Horizontal"
            Spacing="{m3:Token {x:Static m3:MaterialTokens+Comp+ConnectedButtonGroup.Spacing}}">
  <ToggleButton Content="工作" Theme="{StaticResource MaterialConnectedLeadingToggleButtonTheme}" />
  <ToggleButton Content="家庭" Theme="{StaticResource MaterialConnectedMiddleToggleButtonTheme}" />
  <ToggleButton Content="旅行" Theme="{StaticResource MaterialConnectedTrailingToggleButtonTheme}" />
</StackPanel>
```

主题基于现有 Filled ToggleButton；可通过原生颜色、圆角、附加按压/选中圆角属性或局部 Token 定制。
它们也可放在 ButtonGroupCustomItem 中。换行、纵向布局和单选策略由上层容器/状态提供。

默认配置统一来自 `ButtonGroupTokenSource`，没有 `ButtonGroupDefaults`。
例如在 `ButtonGroup.Resources` 中覆盖 `Comp.ButtonGroup.Spacing`，或直接设置 `Spacing`；
普通 Style、局部值、绑定及 ClearValue 保留 Avalonia 的优先级。

完整可交互示例见 [Catalog ButtonGroupDemo](../../../Material3Expressive.Catalog/ButtonGroupDemo.axaml)。
该页面按固定版本 `ButtonGroupSamples.kt` 的五个 catalog 示例排列；顶部宽度只约束两个 `ButtonGroup`，connected 行随窗口换行。
当前双端用例使用非文字内容，不证明文字栅格、屏幕阅读器或真实桌面体验一致；其他边界见维护说明。
