# ExposedDropdownMenu 内部机制与维护说明

## 1. 上游基线与职责映射

固定版本为 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 的 AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 commonMain `androidx/compose/material3/ExposedDropdownMenu.kt` 一致。
平台行为取 Core `skikoMain/.../ExposedDropdownMenu.skiko.kt` 和
`internal/AccessibilityServiceStateProvider.skiko.kt`，不混入 Android 系统窗口规则。

| 上游职责 | 本地职责 |
| --- | --- |
| ExposedDropdownMenuBox / menuAnchor / expandable | ExposedDropdownMenuBox、AnchorType / IsAnchorEnabled、受控请求 |
| exposedDropdownSize / calculateMaxHeight | ExposedDropdownMenu.ConfigurePopupConstraints |
| ExposedDropdownMenuPositionProvider | 同名物理像素位置计算器 |
| DropdownMenuContent / Popup | 复用 DropdownMenu 的 Transition、表面、滚动和窗口内宿主 |

## 2. 关键机制与架构契约

菜单以整个 Box 的边界为锚点。优先从逻辑 start、逻辑 end 和窗口边缘选择 X；
Y 依次尝试锚点下、锚点上、窗口端点，只有最后一个候选才应用 48 DIP margin 或过高居中。
maxHeight 取上/下可用空间较大者，完全离开窗口的锚点使用带 margin 的整个窗口。
调用方约束先于 exposedDropdownSize，因此显式高度/最小高度可大于锚点旁空间。
缩放原点由真实菜单与锚点交集计算；RTL 保留物理原点和 Avalonia 子树镜像转换。

锚点挂载优先记住 primary 类型，点击时记住实际触发类型。输入在 tunnel 观察；secondary
消费按下，primary 不阻止原生编辑，离开边界/子内容消费移动/捕获丢失取消待释放手势。
锚点注册和输入查找都以最近的 Box 为归属，嵌套 Box 不向外层重复发布展开请求。
仅 KeyUp Enter/Space 请求展开，PrimaryEditable 排除 Space；其展开状态下 Up/Down/Tab
将菜单改为 focusable。请求不自行修改 IsExpanded、文本或选择。
source 替换、重挂、退出清理沿用现有 DropdownMenu 生命周期。

真实字段的 FocusInteraction 在菜单接管键盘时保持，使用范围及清理见 [DECISIONS §28](../../../../docs/DECISIONS.md#28-exposeddropdownmenu-的字段焦点与同窗口弹层)。

两个 ExposedDropdownTextFieldTheme 复用现有 TextField 模板，只覆盖固定 ExposedDropdownMenuDefaults
与普通 defaults 不同的槽：Filled disabledLabelColor 为不透明 OnSurface，Filled/Outlined disabled
prefix/suffix 为 OnSurface @ 0.38，复用同语义的 DisabledSupportingColor token。其余默认值逐项对照
FilledAutocompleteTokens / OutlinedAutocompleteTokens，保持相同主题数据流。原生 ComboBox 的历史配色选择不变。
TrailingIcon 使用固定 ArrowDropDown 向量、24×24 viewport 与立即 180° 旋转，不自行引入转动动画。

## 3. 已确认决定与已知差异

2026-11-06 本轮明确授权下新增独立 Box + Menu API，避免改写原生 ComboBox 的非虚拟关闭及
选择/输入耦合。现有 ComboBox 入口不迁移、不改变。Popup 的约束和默认位置策略增加内部虚方法，
用于真实复用同一动画/滚动/输入宿主；没有公开第二套弹层框架。

桌面 Core 的 accessibility service state 恒为 false，所以 PrimaryEditable / SecondaryEditable
默认非 focusable，PrimaryNotEditable 可聚焦。移动端 IME/inset、平台弹层安全选项未实现。
自动化适配由本轮授权决定：容器投影 ComboBox + ExpandCollapse，原生 TextBox 保留编辑接口；
不是在每个锚点注入 Compose semantics。所有锚点禁用时容器不发布操作，不隐藏字段子节点。
真实 OS 辅助技术、触屏、多屏 DPI 和输入法未经本轮验收。非圆角 Shape 仍受共享 Surface 范围限制。

已观察、尚未消除的差异：程序化展开空字段时，32/64/112/224ms 的标签位置与固定 Core 不同，
String 和 state 两种 TextField 重载都会复现；初态、稳定态、焦点及菜单尺寸读数一致。
上游 Box 在 SideEffect 请求焦点，本地原生焦点与现有 TextField 目标提交存在阶段边界，
尚未证明完整等价，不能只因字段焦点保持已修复而将这四例视为通过。
默认箭头在属性变更后、下一动画帧前的被动绘制也有提交差异：Core 保留旧方向，本地已翻转；
下一帧与稳定态一致。没有增加旋转动画或调整容差，此边界仍作为失败用例保留。

## 4. 验证入口

- [行为测试](../../../../tests/Material3Expressive.Tests/ExposedDropdownMenuTests.cs)：受控请求、焦点/键盘、禁用、尺寸、退出与次级锚点。
- [Avalonia 用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ExposedDropdownMenuCases.cs) / [Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ExposedDropdownMenuCases.kt)。
- [Catalog](../../../Material3Expressive.Catalog/MenuDemo.axaml)：可编辑和只读字段。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ExposedDropdownMenu
./tests/VisualParity/parity.ps1 self-test --family exposed-dropdown-menu
./tests/VisualParity/parity.ps1 self-test --family exposed-dropdown-field
./tests/VisualParity/parity.ps1 self-test --family exposed-dropdown-icon
```

2026-11-06 本轮结果（捕获错误为零，repeat 与 extraImages 全部稳定）：

- `exposed-dropdown-final`：62 例，20 equal、38 withinTolerance、4 different。覆盖两类 primary、
  LTR/RTL、窗口边缘/完全离屏、显式约束、移动/改宽、自然宽度、外部关闭及动画中断；
  8 个真实字段场景同时保留 String 与 state 参考重载，4 个展开动画差异见上文。
- `exposed-default-colors-final`：32 例，16 equal、16 withinTolerance；Filled/Outlined、
  enabled/disabled、error、light/dark 和 RTL 的默认字段槽位颜色全部通过现有门禁。
- `exposed-default-icon`：12 例均因非帧 toggle 样本标为 different；初始/下一帧的几何、状态和
  截图通过，涵盖正反方向、LTR/RTL 和 16/24/48 DIP。已实际检查翻转时两端图像。
- 行为测试 13 项通过（含嵌套 Box 的鼠标/键盘归属）；此前全库行为测试 2315 项、VisualParity 系统测试 134 项通过；
  解决方案和 Catalog 构建成功。测试未覆盖真实 OS 辅助技术或 IME。
- 复用弹层的 `exposed-dropdown-base-regression`：原 `dropdown-menu` 24 例全部通过，
  14 equal、10 withinTolerance，零错误且全部重复稳定；没有改变原菜单门禁。
