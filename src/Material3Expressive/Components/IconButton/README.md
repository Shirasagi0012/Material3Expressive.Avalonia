# IconButton

四种主题（Standard / Filled / Tonal / Outlined）共用 `IconButton`，支持
XS / Small / Medium / Large / XL 尺寸。默认值依据
[固定上游](../../../../docs/upstream-baseline.json)的 `IconButton.kt` 与组件 Token。
当前提供 uniform 宽度和非 toggle 按钮；narrow / wide 与 toggle 入口尚未实现。

## 形状与接入

默认按压不变形。`CornerRadius` 配置静止形状，
`m3:MaterialButton.PressedCornerRadius` 显式配置按压目标；默认 null。
与 Button 使用同一入口，尺寸 class 不覆盖局部静止圆角或显式按压目标。

```xml
<m3:IconButton Symbol="Add" CornerRadius="20" />
<m3:IconButton Symbol="Add" CornerRadius="20"
               m3:MaterialButton.PressedCornerRadius="8" />
```

需要旧的自动变形行为时，显式设置目标，也可绑定对应尺寸的
`Comp.IconButtonSmall.PressedContainerShape` 等 Token。形状变化不改变尺寸和内容布局。
形状入口的原因、范围及 2026-10-03 的维护者确认见 [Button 说明](../Button/README.md#默认值与接入)。
Enter 沿用 Avalonia 原生按下激活，Space 遵循原生 ClickMode；共享控制器只维护按压反馈。

## 一致性测试

独立 suite 为 `icon-button`，双端入口位于 `IconButtonCases.cs` / `IconButtonCases.kt`。
115 个用例覆盖四种主题 × 五种尺寸 × round/square、禁用、深色、ambient 内容色、布局约束、
空内容、RTL、非对称圆角、自定义描边、固定/变形按压、取消、中断、运行时修改及键盘输入。
圆角读数来自实际绘制轮廓，交互读数来自真实事件与回调。

为避免字体轮廓影响，Avalonia 夹具仅将模板内的字体绘制部件替换为矢量箭头。
替代部件继承项目的 MaterialSymbol，保留其测量、StyleKey、模板关联、原始 Template 优先级尺寸和
前景色继承；产品的尺寸 class 仍控制图标布局。Compose 端使用同一矢量路径和真实组件 API。
矢量内容与 Compose Canvas 一样不自动镜像；字体图标的镜像策略不在本轮验证范围。
空内容通过产品的 `Symbol=None` 输入表达，保留模板的实际测量；不删除模板部件来模拟空内容。
这套测试不验证 Material Symbols 字形或真实桌面显示效果。

2026-10-03 合并前的 IconButton 分支：夹具测试 13/13 通过。4× Headless self-test 完整执行
115 个用例、539 个采样点，执行错误为 0；25 个用例 withinTolerance、90 个 different。
114 例普通重复与额外被动截图均稳定；`dynamic.reverse` 的 Compose 普通重复采集在三个采样点出现
最大 1 个通道值的像素差异，额外截图检查和 Avalonia 两项检查稳定。此结果不等于全部视觉一致。
五种尺寸的 `Symbol=None` 夹具调整后分别重跑自检，结果稳定；`dynamic.reverse` 单例复跑仍出现
相同三个采样点、最大 1 个通道值的 Compose 重复差异。
上述结果对应分支的 Enter 松开激活；保留原生键盘行为的合并状态尚未重跑完整双端 self-test。

固定按压和显式目标的圆角已与上游对齐。其余尚未对齐的事实包括：

- Small 的外层交互分配：上游 `minimumInteractiveComponentSize` 默认分配 48 DIP，本地为 40 DIP，
  可见容器均为 40 DIP；XS 的显式 32 DIP 尺寸由调用方约束，两端均保留。
- Standard / Outlined 的默认图标颜色：上游 `iconButtonColors()` / `outlinedIconButtonColors()`
  读取 `LocalContentColor`；本地使用 OnSurfaceVariant，禁用颜色也未随 ambient 颜色变化。
- Outlined 默认描边：上游 `outlinedIconButtonBorder()` 从 `LocalContentColor` 取色，宽度固定使用
  Small 的 1 DIP；本地使用 OutlineVariant，大尺寸仍消费 2/3 DIP 原始 Token。
- 属性变更当帧的即时更新，以及更换按压目标后部分状态层像素仍有差异。

上游依据为 `docs/upstream-baseline.json` 固定 AndroidX 的 `IconButton.kt` / `IconButtonDefaults.kt`；
普通与 vibrant helper 保持不同语义，夹具使用公开组件的普通默认入口。这些差异未被认定为已接受的适配，
本轮只落地形状 API 和测试，未改动上述默认值或阴影算法。

从项目根目录运行：

```powershell
./tests/VisualParity/parity.ps1 self-test --family icon-button
# 准备固定参考后，可以单独重采 Avalonia：
./tests/VisualParity/parity.ps1 prepare-reference --family icon-button
./tests/VisualParity/parity.ps1 run --family icon-button
```
