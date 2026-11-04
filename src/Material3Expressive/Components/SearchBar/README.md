# SearchBar

`SearchBar` 与 `ExpandedDockedSearchBar` 通过同一个 `SearchBarState` 组合收起表面和独立搜索弹层。`DockedSearchBar` 保留布尔展开状态的原位容器；`SearchBarInputField` 提供配套单行输入。查询与展开状态由应用管理。

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
    <Button Content="搜索组件" Width="360" Height="56" Click="OpenSearchPopup" />
  </m3:SearchBar>
  <m3:ExpandedDockedSearchBar State="{StaticResource SearchState}">
    <m3:ExpandedDockedSearchBar.InputField>
      <m3:SearchBarInputField Text="{Binding Query, Mode=TwoWay}" IsExpanded="True"
                             PlaceholderText="搜索" SearchRequested="SubmitSearch" />
    </m3:ExpandedDockedSearchBar.InputField>
    <ScrollViewer><ItemsControl ItemsSource="{Binding Results}" /></ScrollViewer>
  </m3:ExpandedDockedSearchBar>
</Grid>
```

`OpenSearchPopup` 调用 `await state.AnimateToExpandedAsync()`，选中结果后调用 `await state.AnimateToCollapsedAsync()`。Escape 和弹层外按下自动收起；结束后恢复之前的焦点。弹层声明本身不占布局空间。Catalog 的 **Search popup** 展示过滤、选中、主题及 RTL 切换，可用 `--search-popup` 单独启动。

## 变体与属性

- `IsExpanded` 控制结果显隐；中途反向保留当前动画值。
- 输入字段获得焦点时发出 `ExpansionRequested`，Enter 发出 `SearchRequested`；两者不代写查询或展开状态。将两个控件的 `IsExpanded` 绑定到同一状态。
- 输入字段从展开变为收起且当时拥有焦点时，100ms 后清除焦点；重新展开和卸载取消待执行任务。焦点变化本身不会启动这个延迟。
- `CollapseRequested` 在未被子控件处理的 Escape 按下时发出请求；应用应将 `IsExpanded` 设为 `false`。事件不会自行改变该属性。
- `Background`、`Foreground`、`CornerRadius`、`DividerColor` 定制表面和分隔线；`TonalElevation`、`ShadowElevation` 默认均为零。
- 默认宽度为 360 DIP，可通过 `Width`、父级约束及组件 Token 调整。结果区域至少 240 DIP，最多为窗口高度的三分之二；较小窗口或父级约束会进一步限制高度。

以上布尔属性和高度规则属于 `DockedSearchBar`。新弹层由 `State` 控制，宽度来自收起表面，**整个弹层**最小 240 DIP、最大窗口高度的三分之二。收起表面由其内容决定大小。`Progress` 范围为 0–1，`CurrentValue` 在底层进度不大于 .02 时变为 Collapsed；`TargetValue` 与 `IsAnimating` 用于观察目标和运行状态。反向操作会取消先前操作，并保留当前值和速度。方法接收 `CancellationToken`；被替代或卸载的操作可能以取消结束。

`new SearchBarState(SearchBarValue.Expanded)` 可初始化展开状态；`SnapToAsync(fraction)` 可在挂载前设定进度。动画需要挂载到已初始化动画作用域的窗口。默认展开采用 SlowSpatial，收起采用 DefaultSpatial；构造函数也可接收 ComposeAnimation 的自定义动画规格。

`IsAnimating` 只反映主进度动画；使用较长的自定义内容动画时，它可能先变为 false，而展开/收起方法返回的任务仍未完成。`SnapToAsync` 不改变内容动画，目标进度恰为 1 时 `TargetValue` 才为 Expanded。内部关闭在弹层移除时取消剩余动画，`Progress` 可能保留不大于 .02 的小正值；需要等待完整归零时，由应用调用并等待 `AnimateToCollapsedAsync()`。

## 内容槽与定制

`InputField` / `InputFieldTemplate` 承载输入区，`Content` / `ContentTemplate` 承载结果。旧 `DockedSearchBar` 的输入始终可见；独立弹层中的两个槽都随弹层创建和释放。容器不会创建默认输入框、自动过滤或自动滚动；长结果请包裹 `ScrollViewer`。分隔线由容器提供。

可在局部资源覆盖 `MaterialTokens.Comp.SearchBar`，例如 `DefaultWidth`、`MinimumResultsHeight`、`ContainerColor` 和 `DockedContainerShape`。需要自定义整体布局时替换控件模板。

输入字段复用 `MaterialTextField.Placeholder`、`LeadingIcon`、`TrailingIcon` 及对应模板，保留原生 TextBox 的 Text/Selection/IME。默认输入背景透明、无指示线，图标向内偏移 4 DIP，首选宽度范围 360–720 DIP；父级可缩小它。`Foreground`、`Background`、`PlaceholderForeground` 等原生属性可覆盖默认颜色。无障碍默认名称和展开状态使用固定 Core 的本地化文案，显式 `AutomationProperties.Name` / `ItemStatus` 优先。

## 使用限制与注意事项

`SearchBarInputField` 仍使用布尔展开状态，不接受 `SearchBarState`。上例以按钮打开弹层，展开输入只负责查询。全屏、带间隙弹层及搜索应用栏尚未提供。输入字段用于无标签、无错误状态的单行搜索。

独立弹层采用可聚焦的窗口内桌面弹层，形状通过 `CornerRadius` 配置；目前不提供移动端软键盘抑制和键盘避让。一个状态应配对同一窗口内一个收起表面和一个展开弹层。仅移动已存在的收起表面不会立即重定位静止弹层；重新打开或进度/窗口尺寸更新时读取最新位置。结果模板在弹层移除时释放，业务状态应保存在视图模型中。

同一窗口存在多个展开的布尔 `DockedSearchBar` 时，Escape 请求交给视觉树中最后一个可见的容器，其结果在退出动画完成后移出视觉树。独立弹层则由当前顶层弹层处理 Escape。

内部移植差异和验证范围见 [INTERNALS.md](INTERNALS.md)。
