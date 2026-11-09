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

FAB 可以只提供模板，不必另设内容对象：

```xml
<m3:HorizontalFloatingToolbar Expanded="True">
  <m3:HorizontalFloatingToolbar.FloatingActionButtonTemplate>
    <DataTemplate>
      <m3:FloatingActionButton Icon="{m3:Symbol Add}" AutomationProperties.Name="新建"
                              Theme="{StaticResource MaterialStandardFloatingToolbarFabTheme}" />
    </DataTemplate>
  </m3:HorizontalFloatingToolbar.FloatingActionButtonTemplate>
  <m3:IconButton Symbol="Edit" />
</m3:HorizontalFloatingToolbar>
```

模板与内容对象同样选择 FAB 布局、默认 elevation 和收起占位；清除二者才回到普通三槽布局。

## 默认值与定制

默认 Standard 背景为 SurfaceContainer，`vibrant` class 改为 PrimaryContainer；Foreground
通过内容色角色提供默认环境，子控件显式选色仍优先。常用入口是 Background、Foreground、Padding、
CornerRadius、Shape、ExpandedShadowElevation 和 CollapsedShadowElevation。

默认 Padding 为 8 DIP，交叉轴最小尺寸为 64 DIP；屏幕边缘 16 DIP 偏移由调用者 Margin/布局表达，
不会自动添加。ExpandedShadowElevation 的主题默认值为普通入口 0、FAB 入口 1 DIP，收起默认 0。
该属性是有效 double 配置；清除局部值会恢复当前入口的主题默认值。
工具栏不强制内容 StackPanel 的 Spacing。

`MaterialStandardFloatingToolbarFabTheme` / `MaterialVibrantFloatingToolbarFabTheme` 分别提供
PrimaryContainer / TertiaryContainer FAB 背景；静止、按压、焦点高度 3 DIP，悬停 6 DIP。
FAB 入口的 AnimationSpec 接受 `IFiniteAnimationSpec<float>`，主题提供动态 FastSpatial 默认值；普通槽动画规格由
组件 motion Token 提供。嵌入的 IconButton 自身边界见 [IconButton](../IconButton/README.md)。

## 独立进入与退出过渡

`FloatingToolbarDefaults.HorizontalEnterTransition` / `HorizontalExitTransition` 和对应的
`VerticalEnterTransition` / `VerticalExitTransition` 可以直接配置已有 `ComposeAnimation.AnimatedVisibility`，
让自定义操作组使用相同的展开/收起效果，无需放进完整工具栏。

```csharp
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Motion;

var group = new AnimatedVisibility
{
    Visible = false, Content = actions,
    TargetCommitPhase = AnimationCommitPhase.AfterFrame
};
var spring = MotionScheme.Expressive.FastSpatial; // Explicit fixed scheme in this example.
group.Enter = FloatingToolbarDefaults.HorizontalEnterTransition(
    group, VisibilityHorizontalAlignment.Start, spring);
group.Exit = FloatingToolbarDefaults.HorizontalExitTransition(
    group, VisibilityHorizontalAlignment.End, spring);
group.Visible = true;
```

横向对齐使用 `VisibilityHorizontalAlignment.Start / Center / End`；与固定上游 EnterExit 一样，
这里的 Start / End 是物理左 / 右，即使内容为 RTL 也不反转。纵向使用 `Avalonia.Layout.VerticalAlignment`
的 Top / Center / Bottom。四个工厂保留零尺寸起点/终点、裁剪和固定上游的尺寸弹簧行为；可在宿主挂载前调用。
工厂结果属于传入的实际 `AnimatedVisibility`，不要把同一结果复用到另一个宿主，density 与反向状态由该宿主提供。
`TargetCommitPhase=AfterFrame` 使用共享帧末提交，表达 Compose 布尔入口的目标提交阶段；工厂本身只返回过渡，
不会修改宿主的目标或时钟。默认 BeforeFrame 仍保留 AnimatedVisibility 原有即时目标行为。

`animationSpec` 是显式参数，不会偷偷读取或缓存主题。应用使用动态主题时，将
`Sys.Motion.Spring.SpatialFast` Token 绑定到自己的 spring 属性，属性更新时重新调用工厂并设置 Enter/Exit；
工厂消费弹簧的 DampingRatio / Stiffness，尺寸终止阈值仍采用上游独立的物理像素规则。
工具栏内置槽也复用这四个工厂。[Catalog 示例](../../../Material3Expressive.Catalog/FloatingToolbarDemo.axaml.cs)
提供完整的主题属性绑定、两轴组合和展开开关。更换过渡不会更换内容或负责选择状态。

## 随滚动退出与展开

退出整个工具栏和展开槽位是不同能力。以下 C# 放在已初始化视图中，`toolbar` 与 `scrollViewer`
分别是现有 FloatingToolbar 和 ScrollViewer；使用 `Material3Expressive.Components` 命名空间。

```csharp
var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(
    FloatingToolbarExitDirection.Bottom);
toolbar.ScrollBehavior = behavior;
toolbar.ScrollSource = scrollViewer;
```

`ScrollSource` 指向同一窗口中启用 Material 物理滚动的纵向 `ScrollViewer` 时，自动连接实际消费位移及释放后的吸附。
滚动容器应设置 `HorizontalScrollBarVisibility="Disabled"`；`IsScrollConnected` 报告自动连接是否生效。
连接不额外消费输入，正文滚动结束后，工具栏会继续处理剩余速度并吸附到展开或退出位置。
程序化设置正文 `Offset` 不伪造用户滚动；四个退出方向都使用正文的纵向消费。公开状态 `FloatingToolbarState` 中
OffsetLimit、Offset、ContentOffset 的单位是 DIP，速度是 DIP/s，向上为负。
一个 behavior 只能连接一个可见工具栏；切换或卸载时会清理旧 settling 和订阅。

逻辑滚动、双轴或其他原生模板保留原来的 `OffsetDelta` 观察，不提供自动释放吸附；此时 `IsScrollConnected=False`。
自定义宿主在已挂载工具栏上调用 `await behavior.OnPostFlingAsync(remainingVelocity)`，没有剩余速度可传 0。
手动输入距离时用 OnPostScroll，避免与 ScrollSource 对同一距离重复报告。
新内容拖动、替换来源或行为、启用触摸探索及卸载会取消旧吸附。默认普通滚轮沿用平台策略，不额外制造触摸释放。

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

## 容器形状

横向和纵向工具栏的 `Shape` 接受 `CutCornerShape`、`RoundedCornerShape` 或自定义 `MaterialShape`。
它控制背景、裁剪和阴影，展开/收起时按当前容器尺寸重新求值，清除后恢复 `CornerRadius`。
工具栏与 FAB 的形状独立配置；专用 Standard/Vibrant FAB 主题也保留 FAB.Shape。

```xml
<m3:HorizontalFloatingToolbar>
  <m3:HorizontalFloatingToolbar.Shape>
    <m3:CutCornerShape TopStart="20%" TopEnd="8" BottomEnd="20%" BottomStart="8" />
  </m3:HorizontalFloatingToolbar.Shape>
  <m3:IconButton Symbol="Edit" AutomationProperties.Name="编辑" />
</m3:HorizontalFloatingToolbar>
```

Catalog 的 Floating toolbar 窗口展示独立工具栏切角与 FAB 百分比圆角，可切换 Expanded 观察。
