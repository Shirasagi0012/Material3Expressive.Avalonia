# FloatingToolbar

`HorizontalFloatingToolbar` / `VerticalFloatingToolbar` 提供横向或纵向的浮动操作区，
可以组合常驻内容、可收起的两侧内容或 FAB。先完成[公共接入](../../../../docs/GETTING_STARTED.md)，
实现与移植依据见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:HorizontalFloatingToolbar>
    <m3:HorizontalFloatingToolbar.LeadingContent>
      <m3:IconButton Symbol="ArrowBack" AutomationProperties.Name="返回" />
    </m3:HorizontalFloatingToolbar.LeadingContent>
    <StackPanel Orientation="Horizontal">
      <m3:IconButton Symbol="Edit" AutomationProperties.Name="编辑" />
      <m3:IconButton Symbol="Share" AutomationProperties.Name="分享" />
    </StackPanel>
  </m3:HorizontalFloatingToolbar>
</UserControl>
```

后续片段沿用相同命名空间。工具栏是 ContentControl；按钮的 Click、Command、禁用和图标名称由内部
按钮自己处理，工具栏不代理操作。应用负责把它放到屏幕所需位置，例如用 Grid 对齐及 Margin。

## 槽位、收起与 FAB

普通入口的 `Content` 始终保留，Expanded 控制 LeadingContent / TrailingContent。
三处各有对应 Template 属性，允许普通数据或控件。若提供 FloatingActionButton 或其模板，
会切换成 FAB 入口，两侧槽不参与布局；收起只显示 FAB，但仍保留完整布局占位。

```xml
<m3:VerticalFloatingToolbar Classes="vibrant" FloatingActionButtonPosition="Top"
                           Expanded="True">
  <m3:VerticalFloatingToolbar.FloatingActionButton>
    <m3:FloatingActionButton Theme="{StaticResource MaterialVibrantFloatingToolbarFabTheme}"
                             AutomationProperties.Name="新建">
      <m3:MaterialSymbol Symbol="Add" Width="24" Height="24" />
    </m3:FloatingActionButton>
  </m3:VerticalFloatingToolbar.FloatingActionButton>
  <StackPanel>
    <m3:IconButton Symbol="Edit" AutomationProperties.Name="编辑" />
    <m3:IconButton Symbol="Share" AutomationProperties.Name="分享" />
  </StackPanel>
</m3:VerticalFloatingToolbar>
```

横向 FAB 位置为 Start / End，默认 End；纵向为 Top / Bottom，默认 Bottom。
内置 FAB 入口从 56 DIP 过渡到 80 DIP。自定义 FAB 应能 Stretch 到槽位尺寸；显式固定 Width/Height
会限制自身内容，不能期望工具栏自动取消该约束。
Expanded 可以绑定应用状态，启用编译绑定时声明 DataContext 类型。

## 默认值与定制

默认 Standard 背景为 SurfaceContainer，`vibrant` class 改为 PrimaryContainer；Foreground
通过内容色角色提供默认环境，子控件显式选色仍优先。常用入口是 Background、Foreground、Padding、
CornerRadius、ExpandedShadowElevation 和 CollapsedShadowElevation。

默认 Padding 为 8 DIP，交叉轴最小尺寸为 64 DIP；屏幕边缘 16 DIP 偏移由调用者 Margin/布局表达，
不会自动添加。ExpandedShadowElevation 为 null 时普通入口取 0、FAB 入口取 1 DIP，收起默认 0。
工具栏不强制内容 StackPanel 的 Spacing。

`MaterialStandardFloatingToolbarFabTheme` / `MaterialVibrantFloatingToolbarFabTheme` 分别提供
PrimaryContainer / TertiaryContainer FAB 背景；静止、按压、焦点高度 3 DIP，悬停 6 DIP。
FAB 入口的 AnimationSpec 接受 `IFiniteAnimationSpec<float>`，默认 FastSpatial；普通槽动画规格固定使用
相应 Material 默认值。嵌入的 IconButton 自身边界见 [IconButton](../IconButton/README.md)。

## 随滚动退出与展开

退出整个工具栏和展开槽位是不同能力。以下 C# 放在已初始化视图中，`toolbar` 与 `scrollViewer`
分别是现有 FloatingToolbar 和 ScrollViewer；使用 `Material3Expressive.Components` 命名空间。

```csharp
var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(
    FloatingToolbarExitDirection.Bottom);
toolbar.ScrollBehavior = behavior;
toolbar.ScrollSource = scrollViewer;
```

ScrollSource 观察实际已消费 OffsetDelta，不额外消费输入。公开状态 `FloatingToolbarState` 中
OffsetLimit、Offset、ContentOffset 的单位是 DIP，速度是 DIP/s，向上为负。
一个 behavior 只能连接一个可见工具栏；切换或卸载时会清理旧 settling 和订阅。

当前自动接入没有原生 nested-scroll 剩余 fling velocity 回调；自定义宿主如需结束后的 settling，
应在已挂载工具栏上调用 `await behavior.OnPostFlingAsync(remainingVelocity)`，没有剩余速度可传 0。
手动输入距离时用 OnPostScroll，避免与 ScrollSource 对同一距离重复报告。

只想根据阈值改变 Expanded 时，可使用独立展开辅助订阅：

```csharp
var subscription = FloatingToolbarDefaults.FloatingToolbarVerticalNestedScroll(
    scrollViewer,
    expanded: () => viewModel.ToolbarExpanded,
    onExpand: () => viewModel.ToolbarExpanded = true,
    onCollapse: () => viewModel.ToolbarExpanded = false,
    reverseLayout: false,
    expandScrollDistanceThreshold: 40,
    collapseScrollDistanceThreshold: 40);
```

此片段假设 viewModel 提供 ToolbarExpanded 并绑定工具栏。将返回的 IDisposable 保存在视图中，
在卸载或更换来源时调用 `subscription.Dispose()`；不要让短暂初始化方法的 using 声明立即结束订阅。
辅助订阅只发通知，不隐藏工具栏、不替应用写 Expanded；修改方向或阈值时重新订阅。
不要将这里的 DIP 协议与 [AppBar](../AppBar/README.md) 的物理 px 协议混用。

## 无障碍与使用边界

宿主需提供实际 `TouchExplorationEnabled` 状态，启用后取消滚动接入并强制展开。
automation 的 Expand/Collapse 保留独立的强制收起状态；Expand 只清除此状态，不保证在应用
Expanded=false 且未启用探索时展开。普通入口 Offset 非零时后代不可聚焦，回到原位后恢复。

真实触屏惯性、辅助技术服务和字体排版尚需桌面验收。几何双端测试不证明实际图标/文字相同；
旧依赖动画问题的当前范围见 [INTERNALS](INTERNALS.md#历史问题与验证入口)。
完整组合见 [FloatingToolbarDemo](../../../Material3Expressive.Catalog/FloatingToolbarDemo.axaml)。
