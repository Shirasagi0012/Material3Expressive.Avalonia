# IconButton

四种主题（Standard / Filled / Tonal / Outlined）共用 `IconButton`，支持
XS / Small / Medium / Large / XL 尺寸。默认值依据
[固定上游](../../../../docs/upstream-baseline.json)的 `IconButton.kt` 与组件 Token。
当前提供 uniform 宽度和非 toggle 按钮；narrow / wide 与 toggle 入口尚未实现。

默认 Small 可见容器为 40 DIP，外层交互布局分配 48 DIP；XS class 对应上游显式 size(32dp)，
外层与容器均保留 32 DIP。其他尺寸 class 提供各自容器尺寸。
显式 Width/Height、MinWidth/MinHeight 保留调用方约束。尺寸默认值现在由模板 ButtonLayout 持有，
未显式设置的公开 MinWidth/MinHeight 为 0；需要布局读数时使用实际 Bounds，不能再用 MinWidth 推断容器尺寸。
这项共享布局修正由维护者在 2026-10-04 确认，依据见
[最小交互布局与工具栏对齐读数](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)。

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
Enter 沿用 Avalonia 原生按下激活，Space 遵循原生 ClickMode；已接受的适配依据见
[共享交互反馈与键盘激活职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)。

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

2026-10-04 FloatingToolbar 合入主分支后的 4× Headless self-test 使用当前 ComposeAnimation
0.2.7-alpha，完整重采双端 115 例、539 个采样点：56 例 withinTolerance、59 例 different，
执行错误为 0，所有普通重复与额外被动截图检查稳定。与分支交付的
`20261003-225547-883bb601` 对照，用例及采样事实的比较状态均无变化。
结果位于 `artifacts/visual-parity/runs/icon-button-floating-toolbar-merge-20261004/summary.md`。
此结果不等于全部视觉一致。
Small 默认 48 DIP 和 XS 显式 32 DIP 的外层、可见容器、图标几何已对齐；
以下既有默认值及受限图标差异仍保留，Enter 沿用已确认的原生按下激活。

固定按压和显式目标的圆角已与上游对齐。其余尚未对齐的事实包括：

- Standard / Outlined 的默认图标颜色：上游 `iconButtonColors()` / `outlinedIconButtonColors()`
  读取 `LocalContentColor`；本地使用 OnSurfaceVariant，禁用颜色也未随 ambient 颜色变化。
- Outlined 默认描边：上游 `outlinedIconButtonBorder()` 从 `LocalContentColor` 取色，宽度固定使用
  Small 的 1 DIP；本地使用 OutlineVariant，大尺寸仍消费 2/3 DIP 原始 Token。
- 属性变更当帧的即时更新，以及更换按压目标后部分状态层像素仍有差异。
- 强制小于图标默认尺寸的 Width/Height 下，本地 MaterialSymbol 仍可能溢出父约束；
  双端受限 20 DIP 用例的容器已一致，图标仍为本地 24 / Compose 20 DIP。

上游依据为 `docs/upstream-baseline.json` 固定 AndroidX 的 `IconButton.kt` / `IconButtonDefaults.kt`；
普通与 vibrant helper 保持不同语义，夹具使用公开组件的普通默认入口。这些差异未被认定为已接受的适配，
FloatingToolbar 接入修正了默认交互分配，未改动上述颜色、描边或阴影算法。

从项目根目录运行：

```powershell
./tests/VisualParity/parity.ps1 self-test --family icon-button
# 准备固定参考后，可以单独重采 Avalonia：
./tests/VisualParity/parity.ps1 prepare-reference --family icon-button
./tests/VisualParity/parity.ps1 run --family icon-button
```
