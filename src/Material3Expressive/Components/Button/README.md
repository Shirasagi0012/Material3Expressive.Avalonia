# Button

使用 Avalonia `Button`，五种主题共享尺寸、内容布局与交互能力。默认值依据
[固定上游](../../../../docs/upstream-baseline.json)的 `Button.kt` 及组件 Token。

## 默认值与接入

- 默认按压不变形，静止形状使用按钮自身的 `CornerRadius`。尺寸 class 提供圆角默认值，
  显式局部值优先于这些默认值，模板将其传给 Surface。
- 可空附加属性 `m3:MaterialButton.PressedCornerRadius` 控制按压目标；默认 null 保持静止形状，
  设置值后复用已有圆角动画，恢复 null 关闭变形。Button 和 IconButton 使用同一入口。
  本次只改变形状入口和默认行为，仍保留当前 Expressive 尺寸与 Padding。
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
- 指针移出交互范围即取消手势，移回后释放不会点击。
- 普通 Button/IconButton 保留 Avalonia 原生键盘激活：Enter 在按下时激活，Space 遵循原生 ClickMode。
  共享控制器只观察键盘输入并维护按压反馈，不拦截 Enter 或代为调用点击。
  Enter 的激活时机与固定 Compose 的松开激活不同，已作为框架适配接受。

上述圆角、elevation、颜色及指针修复于 2026-10-03 经维护者确认实施；
受限内容夹具分别对应 Compose 的 size / requiredSize，读取实际排列几何，不补偿组件布局。
本次不调整弹簧参数或测试帧，阴影算法明确延期，不能据此接受阴影差异。

普通 Button 使用 Avalonia Button 加主题，共享反馈控制器接管 Enter 会改变应用命令时机，
并与原生 Space 及导航项的键盘处理形成多套激活路径。2026-10-03，维护者明确
“是的。不需要Enter拦截了。然后将决策记录在案”，替代此前在 KeyUp 经 automation Invoke 激活的决定。
当前 Enter 按下即激活，重复 KeyDown 沿用原生处理；已执行的命令不因随后失焦或禁用而撤回，
按压反馈仍在释放、失焦或禁用时清理。真实 Compose 参考、采样与比较规则保留，激活时机差异如实报告。
该适配仅决定普通 Button/IconButton 的键盘行为，导航项已有的释放激活保留，不决定它们的统一方案，
也不撤回其他 Button 修复。共享控制器的职责见[架构说明](../../../../docs/ARCHITECTURE.md#surface-与交互组合)。

```xml
<!-- 固定形状：按压时仍为 20 DIP -->
<Button Content="Save" CornerRadius="20" />

<!-- 显式启用变形：20 → 8 DIP，释放时恢复 -->
<Button Content="Save" CornerRadius="20"
        m3:MaterialButton.PressedCornerRadius="8" />

<!-- 使用已有 Small 按钮的按压 Token -->
<Button Content="Save"
        m3:MaterialButton.PressedCornerRadius="{m3:Token {x:Static m3:MaterialTokens+Comp+ButtonSmall.PressedContainerShape}}" />
```

C# 使用 `MaterialButton.SetPressedCornerRadius(button, new CornerRadius(8))`；传 null 恢复固定形状。
属性支持非对称 `CornerRadius`、样式和绑定。原先依赖自动按压变形的调用方需要显式设置这个属性；
旧的尺寸形状 Token 保留，但主题不再自动消费 `PressedContainerShape`。切换尺寸时，需要按尺寸改变
按压形状的调用方也应通过样式或绑定更新目标；显式目标不会被尺寸 class 覆盖。

2026-10-03，维护者确认使用 `m3:MaterialButton.PressedCornerRadius` 并要求实施。
固定上游 Button.kt / IconButton.kt 同时提供固定 shape 与变形 shapes 入口；本地用静止圆角和可空按压目标表达，
不拆成两个组件入口，也不新增 Shapes 配置或变形开关。默认形状行为对应上游固定 shape 入口，
当前 Expressive 尺寸与 Padding 保留，不随形状入口切换。
范围为五种 Button 主题和四种 IconButton 主题，其他消费 Surface 的组件不受影响。

## 验证与保留问题

2026-10-03 合并前的 Button 分支：控件库测试 274/274、Button 夹具测试 23/23 通过；4× Headless parity 完整执行
188 个用例、932 个采样点，执行错误为 0。18 个用例 equal、121 个 withinTolerance、49 个仍 different。
按压、释放、反向中断、禁用和 shape 配置更换的动画圆角、进度与运行状态已对齐。
首选尺寸遵守约束，强制尺寸允许溢出，双端夹具分别对应 Compose `size` 与 `requiredSize`。
五种主题新增默认固定形状按压用例，圆角读数均与上游一致；这五例的普通重复捕获与额外被动截图自检均稳定。
完整 188 例为普通 run，未重新证明其全部重复性。
上述结果对应保留 Enter 松开激活时的分支状态；恢复原生键盘激活后的合并状态尚未重跑完整双端 parity。

尚未对齐的事实包括阴影/elevation 残差、属性变更当帧的即时更新差异，以及更换 pressed shape 后的
状态层像素差异。它们未被确立为可接受的语义偏离；本轮按维护者要求延期阴影算法处理。
前一轮 `repress` 的 Compose 普通重复采集稳定，但额外截图模式两次均出现最大 1 个通道值的像素差异；
Avalonia 的普通重复与额外截图检查均稳定。字体场景和真实桌面视觉体验未在本轮验证。
