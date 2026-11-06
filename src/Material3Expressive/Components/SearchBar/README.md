# SearchBar

`SearchBar` 与独立停靠、带间隙或全屏展开控件通过同一个 `SearchBarState` 组合收起表面和搜索呈现。`DockedSearchBar` 保留布尔展开状态的原位容器；`SearchBarInputField` 提供配套单行输入。查询与展开状态由应用管理。

## 基础用法

加载 `MaterialTheme`，窗口按项目约定初始化动画作用域后使用：

```xml
<m3:DockedSearchBar xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    x:Name="Search" IsExpanded="{Binding IsExpanded}" CollapseRequested="CollapseSearch">
  <m3:DockedSearchBar.InputField>
    <m3:SearchBarInputField Text="{Binding Query, Mode=TwoWay}" PlaceholderText="搜索"
                           IsExpanded="{Binding IsExpanded, ElementName=Search}"
                           ExpansionRequested="OpenSearch" SearchRequested="SubmitSearch" />
  </m3:DockedSearchBar.InputField>
  <ScrollViewer>
    <StackPanel Margin="16" Spacing="8">
      <Button Content="第一个结果" />
      <Button Content="第二个结果" />
    </StackPanel>
  </ScrollViewer>
</m3:DockedSearchBar>
```

`OpenSearch` 将视图模型的 `IsExpanded` 设为 true；`CollapseSearch` 设为 false；`SubmitSearch` 使用当前 `Query` 发起搜索。Catalog 的 **Docked search** 提供完整接线、查询过滤和选择结果示例。

独立弹层使用同一个状态实例，收起和展开输入使用不同控件实例：

```xml
<Window.Resources>
  <m3:SearchBarState x:Key="SearchState" />
</Window.Resources>
<Grid>
  <m3:SearchBar State="{StaticResource SearchState}">
    <m3:SearchBarInputField State="{StaticResource SearchState}"
                           Text="{Binding Query, Mode=TwoWay}" PlaceholderText="搜索组件" />
  </m3:SearchBar>
  <m3:ExpandedDockedSearchBar State="{StaticResource SearchState}">
    <m3:ExpandedDockedSearchBar.InputField>
      <m3:SearchBarInputField Text="{Binding Query, Mode=TwoWay}" State="{StaticResource SearchState}"
                             PlaceholderText="搜索" SearchRequested="SubmitSearch" />
    </m3:ExpandedDockedSearchBar.InputField>
    <ScrollViewer><ItemsControl ItemsSource="{Binding Results}" /></ScrollViewer>
  </m3:ExpandedDockedSearchBar>
</Grid>
```

输入字段自动展开共享状态，也可调用 `await state.AnimateToExpandedAsync()`；选中结果后调用 `await state.AnimateToCollapsedAsync()`。Escape 和弹层外按下自动收起；结束后恢复之前的焦点。弹层声明本身不占布局空间。Catalog 的 **Search popup** 展示过滤、选中、主题及 RTL 切换，可用 `--search-popup` 单独启动。

带间隙弹层使用 `SearchBarState.CreateWithGap()` 创建共享状态，再将上例的 `ExpandedDockedSearchBar` 替换为 `ExpandedDockedSearchBarWithGap`，两个插槽的写法相同：

```xml
<m3:ExpandedDockedSearchBarWithGap State="{StaticResource SearchState}"
                                  DropdownGapSize="2" DropdownCornerRadius="12"
                                  DropdownScrimColor="#52000000">
  <m3:ExpandedDockedSearchBarWithGap.InputField>
    <m3:SearchBarInputField State="{StaticResource SearchState}" PlaceholderText="搜索"
                           Text="{Binding Query, Mode=TwoWay}" />
  </m3:ExpandedDockedSearchBarWithGap.InputField>
  <ScrollViewer><ItemsControl ItemsSource="{Binding Results}" /></ScrollViewer>
</m3:ExpandedDockedSearchBarWithGap>
```

全屏使用 `ExpandedFullScreenSearchBar`；保留独立胶囊输入的全屏变体使用 `ExpandedFullScreenContainedSearchBar`。输入槽显式 Stretch，避免原生控件自身对齐限制它填满插槽：

```xml
<m3:ExpandedFullScreenContainedSearchBar x:Name="FullScreenSearch" WindowInsets="0">
  <m3:ExpandedFullScreenContainedSearchBar.InputField>
    <m3:SearchBarInputField State="{StaticResource SearchState}" PlaceholderText="搜索"
                           HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                           Background="{m3:Token {x:Static m3:MaterialTokens+Comp+SearchBar.ContainerColor}}"
                           Text="{Binding Query, Mode=TwoWay}" />
  </m3:ExpandedFullScreenContainedSearchBar.InputField>
  <ScrollViewer><ItemsControl ItemsSource="{Binding Results}" /></ScrollViewer>
</m3:ExpandedFullScreenContainedSearchBar>
```

```csharp
var state = SearchBarState.CreateContained();
CollapsedSearch.State = state; // 页面中负责打开搜索的 SearchBar
FullScreenSearch.State = state;
```

普通全屏使用 `new SearchBarState()`；contained 工厂使用 FastSpatial 和独立的延迟内容淡入。需要自定义规格时仍使用 `SearchBarState` 的完整构造函数。返回按钮或选择结果后调用 `AnimateToCollapsedAsync()`。Catalog 的 **Search popup with gap**、**Full-screen search**、**Contained search** 提供完整示例，对应启动参数 `--search-gap`、`--search-full-screen`、`--search-contained`。

搜索顶栏可代替上述收起表面，继续和独立展开弹层共享状态：

```xml
<m3:AppBarWithSearch x:Name="SearchAppBar" State="{StaticResource SearchState}">
  <m3:AppBarWithSearch.NavigationIcon>
    <m3:IconButton Symbol="Menu" AutomationProperties.Name="导航" />
  </m3:AppBarWithSearch.NavigationIcon>
  <m3:AppBarWithSearch.InputField>
    <Button Content="搜索" Height="56" Click="OpenSearchPopup"
            Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:AppBarWithSearch.InputField>
  <m3:IconButton Symbol="Settings" AutomationProperties.Name="设置" />
</m3:AppBarWithSearch>
```

`NavigationIcon` 是起始插槽，直接子项是末尾操作。Catalog 的 **Search app bar** 展示搜索过滤、主题/RTL、直接拖动和显式滚动策略，使用 `--search-app-bar` 启动。

## 变体与属性

`MaterialInteraction.FocusFeedback="InsetRing"` 为新版输入启用键盘内缩焦点环；可继承容器配置，圆角跟随输入的 `CornerRadius`，描边颜色和尺寸沿用共享焦点环配置。Catalog 搜索弹层示例可切换焦点反馈。

新版输入通过 `State` 配合独立弹层：Tab 聚焦保留收起状态，文本长度增加或按 ↓ 展开；已展开时 ↓ 将焦点移到下方建议。点击释放也会展开已聚焦输入，指针聚焦则立即展开。键盘模式收起保留焦点，指针模式收起后等待 100ms 清焦；重新展开会取消清焦。`IsReadOnly` 保留展开与导航，`IsEnabled=false` 阻止原生输入。`SearchRequested` 仍由应用处理，不自动收起。

收起和展开输入各用一个控件实例，并把 `Text` 双向绑定到同一查询。首次挂载或更换 `State` 时若已经展开，该输入不会启动“文本增长时展开”的观察；这通常对应弹层内输入。外部 `InteractionSource` 的 Release 也会请求展开，Cancel 不会。更换源后，新版输入的点击观察在下一个动画帧末切换；setter 返回时不要假定新源已有订阅。

- `IsExpanded` 控制结果显隐；中途反向保留当前动画值。
- 输入字段获得焦点时发出 `ExpansionRequested`，Enter 发出 `SearchRequested`；两者不代写查询或展开状态。将两个控件的 `IsExpanded` 绑定到同一状态。
- 输入字段从展开变为收起且交互源当时处于聚焦状态时，100ms 后清除窗口焦点；重新展开和卸载取消待执行任务。焦点变化本身不会启动这个延迟。
- `CollapseRequested` 在未被子控件处理的 Escape 按下时发出请求；应用应将 `IsExpanded` 设为 `false`。事件不会自行改变该属性。
- `Background`、`Foreground`、`CornerRadius`、`DividerColor` 定制表面和分隔线；`TonalElevation`、`ShadowElevation` 默认均为零。
- 默认宽度为 360 DIP，可通过 `Width`、父级约束及组件 Token 调整。结果区域至少 240 DIP，最多为窗口高度的三分之二；较小窗口或父级约束会进一步限制高度。

以上布尔属性和高度规则属于 `DockedSearchBar`。新弹层由 `State` 控制，宽度来自收起表面，**整个弹层**最小 240 DIP、最大窗口高度的三分之二。收起表面由其内容决定大小。`Progress` 范围为 0–1，`CurrentValue` 在底层进度不大于 .02 时变为 Collapsed；`TargetValue` 与 `IsAnimating` 用于观察目标和运行状态。反向操作会取消先前操作，并保留当前值和速度。方法接收 `CancellationToken`；被替代或卸载的操作可能以取消结束。

| 展开控件 | 尺寸与外观 | 关闭交互 |
| --- | --- | --- |
| `ExpandedDockedSearchBar` | 整体最小 240 DIP，最大窗口高度 2/3；含分隔线 | 外部任意鼠标按钮按下、Escape |
| `ExpandedDockedSearchBarWithGap` | 最大窗口高度 1/2；默认 2 DIP 间隙、12 DIP 结果圆角；无分隔线 | 有遮罩时主按钮点击释放；`DropdownScrimColor="Transparent"` 时外部任意按钮按下；Escape |
| `ExpandedFullScreenSearchBar` | 表面从锚点扩展至客户区；含分隔线 | Escape、应用返回按钮或选中结果 |
| `ExpandedFullScreenContainedSearchBar` | 全客户区表面淡入；输入左右 8 DIP、顶部 4 DIP；无分隔线 | 同上 |

全屏的 `WindowInsets` 为物理左/上/右/下安全边距，桌面默认零；排除祖先已经消费的边距，再把消费结果传递给嵌套内容。顶部边距随进度插值，左右和底部边距保留上游布局顺序。全屏内的空白区域不会触发外部关闭。contained 表面默认 SurfaceContainerLow，输入颜色由输入槽配置。

`new SearchBarState(SearchBarValue.Expanded)` 可初始化展开状态；`SnapToAsync(fraction)` 可在挂载前设定进度。动画需要挂载到已初始化动画作用域的窗口。默认展开采用 SlowSpatial，收起采用 DefaultSpatial；构造函数也可接收 ComposeAnimation 的自定义动画规格。

`CreateWithGap(initialValue)` 改用 DefaultSpatial 展开、FastSpatial 收起，内容进入为 50ms 延迟后的 100ms 淡入，退出为 100ms 淡出；`CreateContained(initialValue)` 的主进度两个方向均为 FastSpatial。两个工厂的运动配置继续响应主题 Token。

`AppBarWithSearch` 的 `Background` 必须为纯色或 null，`SearchBarContainerColor` 单独配置内部搜索表面。两个 nullable 的 `ScrolledContainerColor` / `ScrolledSearchBarContainerColor` 为 null 时保留对应基础色。导航/操作颜色通过各自 `*ContentColor` 配置；子控件显式前景色优先。基础应用栏背景为透明哨兵时，elevation 应用在搜索表面，否则应用在外层。`CornerRadius` 只配置搜索形状，`ContentPadding` 为逻辑方向，`WindowInsets` 为物理方向并排除祖先消费。

可选 `SearchBarScrollBehavior` 提供 enter-always 策略，挂载后支持直接拖动栏区域。嵌套滚动宿主显式接入：

```csharp
var scrolling = new SearchBarScrollBehavior();
SearchAppBar.ScrollBehavior = scrolling;
float preConsumed = scrolling.OnPreScroll(availableY);
// 宿主将 availableY - preConsumed 交给内容滚动，然后报告实际结果。
float postConsumed = scrolling.OnPostScroll(contentConsumedY, remainingY);
float remainingVelocity = await scrolling.OnPostFlingAsync(availableVelocityY, cancellationToken);
```

距离以 DIP、速度以 DIP/s 表达，向下为正。`State` 保留 scroll offset、limit 和 content offset；limit 随完整栏高更新，修改 limit 不主动重新夹取现有 offset。`CanScroll=false` 门控新嵌套滚动回调，已启动的吸附继续运行；直接拖动使用 behavior 首次挂载时的 CanScroll 值，后续修改不会改变它。需要改变直接拖动开关时替换 behavior，可复用同一个 `SearchBarScrollState`。`ReverseLayout` 控制反向布局的消费分支。末端无需吸附时返回零；其他情况下 fling 方法返回衰减剩余速度，不能把它当作已消费速度。一个 behavior 仅绑定一个挂载顶栏，替换、卸载或新的直接拖动取消已有吸附。

`IsAnimating` 只反映主进度动画；使用较长的自定义内容动画时，它可能先变为 false，而展开/收起方法返回的任务仍未完成。`SnapToAsync` 不改变内容动画，目标进度恰为 1 时 `TargetValue` 才为 Expanded。内部关闭在弹层移除时取消剩余动画，`Progress` 可能保留不大于 .02 的小正值；需要等待完整归零时，由应用调用并等待 `AnimateToCollapsedAsync()`。

## 内容槽与定制

`InputField` / `InputFieldTemplate` 承载输入区，`Content` / `ContentTemplate` 承载结果。旧 `DockedSearchBar` 的输入始终可见；独立弹层中的两个槽都随弹层创建和释放。容器不会创建默认输入框、自动过滤或自动滚动；长结果请包裹 `ScrollViewer`。普通停靠和普通全屏的分隔线由容器提供。

可在局部资源覆盖 `MaterialTokens.Comp.SearchBar`，例如 `DefaultWidth`、`MinimumResultsHeight`、`ContainerColor` 和 `DockedContainerShape`。需要自定义整体布局时替换控件模板。

输入字段复用 `MaterialTextField.Placeholder`、`LeadingIcon`、`TrailingIcon` 及对应模板，保留原生 TextBox 的 Text/Selection/IME。默认输入背景透明、无指示线，图标向内偏移 4 DIP，首选宽度范围 360–720 DIP；父级可缩小它。`Foreground`、`Background`、`PlaceholderForeground` 等原生属性可覆盖默认颜色。无障碍默认名称和展开状态使用固定 Core 的本地化文案，显式 `AutomationProperties.Name` / `ItemStatus` 优先。

`InteractionSource` 可观察输入的焦点与按压，也可与外部装饰共享；默认 null 使用模板私有源。它与 `MaterialTextField.InteractionSource` 是同一属性：

```csharp
var interactions = new MaterialInteractionSource(); // Material3Expressive.Foundation.Interaction
Query.InteractionSource = interactions;
interactions.Interaction += interaction => Debug.WriteLine(interaction.GetType().Name);
```

外部 Focus 参与收起时的判断，但不改变原生键盘焦点、不发出 `ExpansionRequested`。展开请求仍来自输入控件实际获得焦点。替换源不回放焦点，也不会重新启动或取消已经开始的清焦延迟；配对、替换和原生编辑边界见 [TextField](../TextField/README.md)。Catalog 的 **Docked search** 显示实际交互事件。

## 使用限制与注意事项

`SearchBarInputField.State` 非空时直接控制共享状态；`IsExpanded` 和 `ExpansionRequested` 仅在 `State=null` 时生效。输入字段用于无标签、无错误状态的单行搜索。`AppBarWithSearch` 尚未自动连接普通 `ScrollViewer` / SmoothScroll；上述显式回调需要宿主按真实嵌套滚动顺序调用。

独立弹层采用可聚焦的窗口内桌面弹层，形状通过 `CornerRadius` 配置；目前不提供移动端软键盘抑制、自动键盘避让和预测返回手势。一个状态应配对同一窗口内一个收起表面和一个展开弹层。仅移动已存在的收起表面不会立即重定位静止弹层；重新打开或进度/窗口尺寸更新时读取最新位置。结果模板在弹层移除时释放，业务状态应保存在视图模型中。

同一窗口存在多个展开的布尔 `DockedSearchBar` 时，Escape 请求交给视觉树中最后一个可见的容器，其结果在退出动画完成后移出视觉树。独立弹层则由当前顶层弹层处理 Escape。

内部移植差异和验证范围见 [INTERNALS.md](INTERNALS.md)。

状态式输入合并同一次 UI 调用中的文字修改：先输入再撤销不会触发展开。更换 `State` 后，帧提交前的文字是新观察器的初始值；其后的长度增长才触发展开。
