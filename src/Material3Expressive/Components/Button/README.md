# Button

使用 Avalonia `Button`，五种主题共享尺寸、内容布局与交互能力。默认值依据
[固定上游](../../../../docs/upstream-baseline.json)的 `Button.kt` 及组件 Token。

## 默认值与接入

- Outlined 的默认描边遵循 `outlinedButtonBorder()`，所有尺寸均为 1 DIP。
  大尺寸原始 Token 的 2/3 DIP 保留，显式 `BorderThickness` 仍可覆盖默认值。
- Filled/Elevated 的禁用背景使用 OnSurface × 0.10；Outlined 的禁用描边使用 OutlineVariant × 0.10。
  Filled/Elevated/Outlined/Text 的禁用内容使用 OnSurfaceVariant × 0.38，Tonal 使用 OnSurface × 0.38。
  Tonal 的禁用背景仍为 OnSurface × 0.12；系统禁用容器 opacity 未修改。
- Filled/Tonal 各新增五个 elevation Token：`ContainerElevation`、`PressedContainerElevation`、
  `FocusedContainerElevation`、`HoveredContainerElevation`、`DisabledContainerElevation`。
  默认高度依次为 0、0、0、1、0 DIP，仍使用已有 `MaterialButton.Elevation` / `ButtonElevation`。
  Outlined/Text 默认配置为 null；局部 null 或自定义配置保留原有覆盖语义。
- 圆角与 elevation 目标在共享时钟帧合并提交，对应 Compose 的重组与 effect。
  圆角配置或 spring 改变时，从当前选定形状重建状态；更换目标先取消旧 effect。
  圆角进度及插值按上游 Float 语义计算，弹簧参数与测试帧网格未调整。
- 指针移出交互范围即取消手势，移回后释放不会点击。Enter 在释放时激活，复用原生
  Click/Command/Flyout 路径；焦点丢失或禁用会取消待激活状态。

## 验证与保留问题

2026-10-03：控件库测试 268/268、Button 夹具测试 23/23 通过；4× Headless parity 完整执行
183 个用例、862 个采样点，执行错误为 0。137 个用例 equal/withinTolerance，46 个仍 different。
按压、释放、反向中断、禁用和 shape 配置更换的动画圆角、进度与运行状态已对齐。
首选尺寸遵守约束，强制尺寸允许溢出，双端夹具分别对应 Compose `size` 与 `requiredSize`。

尚未对齐的事实包括阴影/elevation 残差、属性变更当帧的即时更新差异，以及更换 pressed shape 后的
状态层像素差异。它们未被确立为可接受的语义偏离；本轮按维护者要求延期阴影算法处理。
`repress` 的 Compose 普通重复采集稳定，但额外截图模式两次均出现最大 1 个通道值的像素差异；
Avalonia 的普通重复与额外截图检查均稳定。字体场景和真实桌面视觉体验未在本轮验证。
