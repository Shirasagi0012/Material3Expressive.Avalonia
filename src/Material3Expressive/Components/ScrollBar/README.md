# ScrollBar

原生 `Avalonia.Controls.Primitives.ScrollBar` 的水平、垂直 Material 外观。
外观与 hover / drag 颜色反馈依据固定 Compose Multiplatform Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的
`compose/foundation/foundation/src/skikoMain/kotlin/androidx/compose/foundation/Scrollbar.skiko.kt`，
对应 `ScrollbarStyle`、`defaultScrollbarStyle` 和 `OldOrNewScrollbar`。
它是 Foundation 的交互式滚动条；Material3 的 `nonInteractiveScrollbar` 是另一种组件。
范围、测量、滑块位置 / 长度、输入及 automation 依据项目依赖的 Avalonia 12.1.2，
源码 tag commit 为 `d3c867a9e2de379249b03dbeb3495bd7f076a81a`。

## 使用

加载 MaterialTheme 后原生 ScrollBar 自动接入；ScrollViewer 和 TextBox 内部的滚动条也使用同一主题。
应用按架构说明在 TopLevel 加载内容之前初始化 `VisualAnimationScope`。

```xml
<ScrollBar Height="200" Minimum="0" Maximum="75" ViewportSize="25"
           Value="{Binding ScrollOffset, Mode=TwoWay}" />
<ScrollBar Width="240" Orientation="Horizontal"
           Minimum="0" Maximum="75" ViewportSize="25" />
<ScrollBar Height="200" Minimum="0" Maximum="75" ViewportSize="25"
           CornerRadius="2"
           m3:ScrollBarAppearance.Thickness="12"
           m3:ScrollBarAppearance.ThumbMinLength="24"
           m3:ScrollBarAppearance.UnhoverColor="#40126E60"
           m3:ScrollBarAppearance.HoverColor="#A0126E60"
           m3:ScrollBarAppearance.HoverDuration="0:0:0.3" />
```

`ScrollBarAppearance` 提供 Thickness、ThumbMinLength、UnhoverColor、HoverColor、HoverDuration；
形状使用原生 CornerRadius。厚度沿横轴应用，最小滑块长度沿主轴应用，两种方向共享参数。
默认厚度 8 DIP、最小长度 16 DIP、圆角 4 DIP、颜色 Tween 300ms。
hover 检查整个 ScrollBar，而非只有 Thumb；拖动离开轨道时保持高亮，原生释放或取消后恢复。
颜色动画支持中断，卸载与模板替换清理作用域及事件订阅。

默认颜色通过已有 `WithAlpha` 从 Material3 `OnSurface` 派生，普通 / 高亮 opacity 为 0.12 / 0.50，
跟随局部资源和亮暗主题；直接指定附加颜色属性会覆盖默认 Token。
`MaterialTokens.Comp.ScrollBar` 是本项目为 CMP Foundation 样式提供的主题槽，
不是上游生成的 Material3 ScrollBar Token。
`MaterialScrollBarTheme` 可通过 BasedOn 派生；自定义模板继续使用原生 Track、Thumb 和分页 Button 契约。

基础 FluentTheme 继续提供 `VerticalScrollBarContextFlyout` / `HorizontalScrollBarContextFlyout`。
本主题引用原菜单资源，未复制或修改菜单项、命令、文案；调用方设置的 ContextFlyout 保留优先级。
MaterialTheme 单独加载时不会凭空创建这些基础主题菜单。

## 已确认的适配与验证范围

2026-10-04，维护者确认“原生交互 + Material 外观”，逐项选择：

- 颜色使用 Material3 OnSurface，保留 0.12 / 0.50 opacity；CMP Foundation 的独立默认色原为黑色。
- 固定厚度，hover / drag 直接改变颜色目标；不使用 IsExpanded 驱动扩宽或隐藏。
- 保留原生菜单、LargeChange、滚轮 / 键盘 / 拖动 / 捕获 / 延迟滚动与 automation。
- 无需滚动时保留原生 Visibility 与 ScrollViewer 的布局语义，包括 Auto 收回空间和原模板的覆盖策略。
- 使用 CornerRadius，特殊形状与反向轨道由 ControlTheme 定制，不新增通用 Shape 或 reverseLayout API。
- 保留 Avalonia Track 的位置、长度、最小长度边界和布局取整，不要求使用 CMP 的几何算法。

模板保留透明分页按钮，不绘制箭头或轨道，也不新增 ripple、焦点环或禁用颜色规格。
原生范围与数值绑定由 ScrollBar / ScrollViewer 持有；内部 ScrollBarThumb 只观察输入结果并动画化颜色。

维护者明确要求本组件不做 Compose parity tests；这项交付不宣称双端像素或交互一致。
`tests/Material3Expressive.Tests/ScrollBarTests.cs` 检查主题、Token、原生输入接入、绑定、菜单资源复用、
颜色动画、配置更新与卸载清理。Catalog 的 ScrollBar demo 展示独立滚动条及原生 ScrollViewer 接入。
真实桌面、触屏 / 触控笔和屏幕阅读器仍需实际验收；headless 检查不代替这些场景。
