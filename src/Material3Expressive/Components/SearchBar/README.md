# SearchBar

`DockedSearchBar` 是库内的停靠式搜索容器，组合输入槽、结果内容和展开收起动画。`SearchBarInputField` 提供配套单行输入。查询与展开状态由应用管理。

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

## 变体与属性

- `IsExpanded` 控制结果显隐；中途反向保留当前动画值。
- 输入字段获得焦点时发出 `ExpansionRequested`，Enter 发出 `SearchRequested`；两者不代写查询或展开状态。将两个控件的 `IsExpanded` 绑定到同一状态。
- 输入字段从展开变为收起且当时拥有焦点时，100ms 后清除焦点；重新展开和卸载取消待执行任务。焦点变化本身不会启动这个延迟。
- `CollapseRequested` 在未被子控件处理的 Escape 按下时发出请求；应用应将 `IsExpanded` 设为 `false`。事件不会自行改变该属性。
- `Background`、`Foreground`、`CornerRadius`、`DividerColor` 定制表面和分隔线；`TonalElevation`、`ShadowElevation` 默认均为零。
- 默认宽度为 360 DIP，可通过 `Width`、父级约束及组件 Token 调整。结果区域至少 240 DIP，最多为窗口高度的三分之二；较小窗口或父级约束会进一步限制高度。

## 内容槽与定制

`InputField` / `InputFieldTemplate` 承载始终可见的输入区，`Content` / `ContentTemplate` 承载展开后显示的结果。容器不会创建默认输入框、自动过滤或自动滚动；长结果请包裹 `ScrollViewer`。分隔线由容器提供。

可在局部资源覆盖 `MaterialTokens.Comp.SearchBar`，例如 `DefaultWidth`、`MinimumResultsHeight`、`ContainerColor` 和 `DockedContainerShape`。需要自定义整体布局时替换控件模板。

输入字段复用 `MaterialTextField.Placeholder`、`LeadingIcon`、`TrailingIcon` 及对应模板，保留原生 TextBox 的 Text/Selection/IME。默认输入背景透明、无指示线，图标向内偏移 4 DIP，首选宽度范围 360–720 DIP；父级可缩小它。`Foreground`、`Background`、`PlaceholderForeground` 等原生属性可覆盖默认颜色。无障碍默认名称和展开状态使用固定 Core 的本地化文案，显式 `AutomationProperties.Name` / `ItemStatus` 优先。

## 使用限制与注意事项

当前对应固定上游仍保留的布尔 expanded 重载；尚无新版 SearchBarState、全屏搜索和独立弹层搜索变体。输入字段用于无标签、无错误状态的单行搜索；其他 TextField 附加功能不属于此组件的上游契约。宽度和结果高度限制是默认布局行为，输入槽本身没有强制高度。

同一窗口存在多个展开的搜索容器时，Escape 请求交给视觉树中最后一个可见的容器。结果在退出动画完成后移出视觉树；业务状态应保存在视图模型中。

内部移植差异和验证范围见 [INTERNALS.md](INTERNALS.md)。
