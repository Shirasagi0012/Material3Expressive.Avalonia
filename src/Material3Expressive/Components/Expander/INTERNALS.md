# Expander 实现说明

## 标题

标题是 Expander 模板中的 `ToggleButton#ExpanderHeader`，使用内部键控主题 `MaterialExpanderHeaderTheme`
（与 Fluent 的 `FluentExpanderToggleButtonTheme` 对应）。保留 ToggleButton 是为了原生的 Space/Enter、
`:focus-visible` 与 `ExpanderAutomationPeer`。该资源键在 MaterialTheme 中可见，但不作为公共 API 记录。

标题排版通过 Foundation 的 [`PresenterTextStyle.DefaultStyle`](../../Foundation/Typography/PresenterTextStyle.cs)
设置在标题 ContentPresenter 上（与 GroupBox 共用，待维护者确认，见
[DECISIONS](../../../../docs/DECISIONS.md#presenter-子元素的默认文字样式)）。字符串标题生成的 TextBlock 的逻辑父级是
ToggleButton，但作为标题的控件已是 Expander 的逻辑子项，ToggleButton 上的 DefaultStyle 到不了它，所以放在 presenter 的子元素上。

## 箭头

箭头复用 DropDownButton 的旋转机制。原 `DropDownButtonChevron` 移到 Foundation 并改名为
[`ChevronRotation`](../../Foundation/Rendering/ChevronRotation.cs)（仅改名与位置，行为不变），由 `AnimatedDouble` 声明动画。
Expander 把 `ExpandDirection` 经内部附加属性 `ExpanderHeader.Direction` 转给标题，标题主题按方向与 `:checked`
给出目标角度：Down 0/180，Up 180/0，Left 90/-90，Right -90/90（折叠/展开）。

## 内容区

内容区是 ComposeAnimation 的 `AnimatedVisibility`（`Visible` 绑定 `IsExpanded`），进入/退出声明按方向用样式设置，
锚定在标题一侧：Down 为 Top，Up 为 Bottom，Left 为 End，Right 为 Start（水平锚点随 FlowDirection）。
没有使用 Fluent 的 `IsVisible` 绑定，因为原生 IsVisible 会立即隐藏，无法播放退出动画。

AnimatedVisibility 带尺寸效果时按子元素自身的期望尺寸排列它（与 Compose 相同），Expander 的实际宽度传不进去，
比 Expander 窄的内容会停在锚点处。为了只在排列阶段拉伸、不抬高期望尺寸，模板用两层内部元素：

- `ExpanderContentHost`（`PART_ContentStretch`）包住 AnimatedVisibility。排列时把自身在交叉轴上的最终尺寸交给内层；
  测量时报告的期望尺寸扣除内层因拉伸多出的部分，所以 Expander 仍测量为 max(标题, 内容)，展开/折叠不跳变
- `ExpanderContentFill`（`PART_ContentFill`）是 AnimatedVisibility 的内容，期望尺寸取 max(内容, 宿主给出的交叉轴尺寸)，
  AnimatedVisibility 据此把内容排列为拉伸后的尺寸

交叉轴尺寸变化时内层失效测量，布局多走一轮即收敛（宿主的期望尺寸不依赖该值）。
没有改 ComposeAnimation：让 AnimatedVisibility 在非动画轴上按子元素的 Stretch 排列虽然是更通用的做法，
但实测会改变现有使用者的排列（FloatingToolbar 的侧边内容在像素取整后稳定态也会被拉宽 1–2 DIP），
而 Avalonia 默认 Stretch 与 Compose 默认包裹内容的语义不同，会影响已对齐 Compose 的组件。

`PART_ContentPresenter` 仍由 Expander 注册为内容宿主，Avalonia 的 `ContentTransition` 会照常作用于它；
主题不做协调，README 中说明不建议与主题动画同时使用。

## 禁用色

禁用的标签与箭头为 OnSurface × `Sys.State.DisabledContentOpacity`（上游 List 的禁用色）。
`ExpanderHeader.Alpha` 是本地转换器，把 Token 颜色与不透明度合成为画刷。
