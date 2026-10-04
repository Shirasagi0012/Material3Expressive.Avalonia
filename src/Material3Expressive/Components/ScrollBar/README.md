# ScrollBar

Material 主题为原生 `Avalonia.Controls.Primitives.ScrollBar` 提供水平/垂直外观和悬停、拖动反馈。
范围、数值、输入及 automation 使用 Avalonia 的公开 API。先完成[公共接入](../../../../docs/GETTING_STARTED.md)，
实现与已确认适配见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <ScrollBar Width="240" Orientation="Horizontal"
             Minimum="0" Maximum="75" ViewportSize="25" Value="20" />
</UserControl>
```

原生 ScrollBar 会自动使用 Material 外观，ScrollViewer / TextBox 内部滚动条也一样。
独立 ScrollBar 只维护数值，不会自行滚动某个内容；普通应用滚动优先使用 [ScrollViewer](../ScrollViewer/README.md)。

## 数值、方向与定制

后续片段使用相同命名空间。下面假设 DataContext 提供可写数值 `ScrollOffset`；
启用编译绑定的视图还需设置对应 `x:DataType`。

```xml
<ScrollBar Height="200" Minimum="0" Maximum="75" ViewportSize="25"
           Value="{Binding ScrollOffset, Mode=TwoWay}"
           CornerRadius="2"
           m3:ScrollBarAppearance.Thickness="12"
           m3:ScrollBarAppearance.ThumbMinLength="24"
           m3:ScrollBarAppearance.UnhoverColor="#40126E60"
           m3:ScrollBarAppearance.HoverColor="#A0126E60"
           m3:ScrollBarAppearance.HoverDuration="0:0:0.3" />
```

默认方向为垂直；横向设置 `Orientation="Horizontal"`。默认厚度 8 DIP、最小滑块长 16 DIP、
圆角 4 DIP；厚度沿横轴、最小长度沿主轴应用。`ValueChanged`、LargeChange、滚轮、键盘、
拖动/捕获及延迟滚动仍由原生 ScrollBar 处理。

颜色默认从 OnSurface 派生，普通/高亮 opacity 为 0.12 / 0.50，颜色动画为 300ms。
悬停范围是整条 ScrollBar；拖动指针离开轨道时仍高亮，释放或取消后恢复。
它保持固定厚度，不使用 IsExpanded 实现扩宽或隐藏；透明轨道不绘制箭头、ripple 或焦点环。
局部颜色属性覆盖 Token 默认值，圆角直接使用原生 `CornerRadius`。

`MaterialScrollBarTheme` 可通过 BasedOn 派生；特殊形状、反向轨道等需要定制 ControlTheme。
基础 FluentTheme 提供水平/垂直右键菜单资源，本主题复用其原菜单；应用显式 ContextFlyout 优先。
仅加载 MaterialTheme 不会创建这些基础菜单。无需滚动时的可见性和占位规则由原生
ScrollViewer 决定，不由颜色反馈改变。

## 使用边界

这是原生滚动交互与 Material 颜色的适配，既不是 Material3 `nonInteractiveScrollbar`，
也不承诺 Compose 的滑块几何。没有额外禁用颜色规格。
真实桌面、触屏/触控笔与屏幕阅读器需实际验收；本组件按维护者要求没有 Compose parity suite。
完整示例见 [ScrollBarDemo](../../../Material3Expressive.Catalog/ScrollBarDemo.axaml)。
