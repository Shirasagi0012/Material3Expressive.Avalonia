# ListItem 维护与移植

## 源码依据

基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 ListItem.kt、ListItemDefaults.kt、tokens/ListTokens.kt、tokens/ReorderListTokens.kt 在这些版本相同。
入口位于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`，仅新 ListItem / SegmentedListItem 的四类重载属于本轮范围。
桌面输入依据 Core foundation/Clickable.kt 的 CombinedClickableNode，长按时间来自平台 ViewConfiguration（500ms）。

| 上游职责 | 本地位置 |
| --- | --- |
| 新重载、语义、受控选择、组合输入 | ListItem.cs / ListItem.axaml |
| InteractiveListItemMeasurePolicy、五个 decorator | ListItemLayout.cs |
| ListColor Transition、animateDpAsState、shapeForInteraction | ListItemMotion.cs |
| ListItemDefaults、ListTokens、ReorderListTokens | ListItemTokenSource.cs / MaterialTokens.Comp.ListItem.cs |
| CollectInteractionsAsState、ripple | Foundation/Interaction 的配对源及共享 controller |

## 布局与环境

测量顺序为 leading、trailing、overline、main、supporting；侧槽各预留 12 DIP 间隔。
有限宽度填满约束，无限宽度按侧槽与正文最大宽度求和；正文高度求和，再与侧槽最大高度、默认行高比较。
overline+supporting 同时存在或普通 supporting 首末文本基线不同，选择 88；任一辅助正文存在选 72，否则 56。
标题本身的换行不参与行数分类。Auto 对齐的内容区阈值为 `(88+72)/2-10-10=60`，来源是默认 Token，不跟实例 Padding 改动。
Avalonia Measure 的无限约束承担常见内容固有尺寸需求，不另建 Compose 的 min/max intrinsic 调用协议。

Side presenter 的最小交互环境为 `max(0, inheritedMinimum - start/endPadding - spacing)`，默认 20；
该值从呈现位置向槽内容传播，支持动态祖先环境和 Padding 更新。
RTL 由模板布局镜像；CornerRadius 是物理角，默认六状态及分组只使用左右对称值。

## 状态、动画与释放

颜色优先级：disabled > dragged > selected > default，六个颜色轨道属于同一个 Transition。
形状优先级：pressed > dragged > selected > focused > hovered > base。
六形状配置整体改变时重建形状状态；上游 ripple 的 focusRingShape 身份随之改变，indication 重建并仅消费后续事件，独立的聚合交互状态保留。
颜色使用 DefaultEffects；形状和 elevation 使用 FastSpatial。测试显式设置双方 Expressive motion scheme，不能让 Compose 默认 Standard 与本地 Expressive 混配。
形状和 elevation 的参数先转换 Float，再交给动画求解器；阴影目标在交互收集后的 composition 帧更新。
固定版本 AnimateAsState 在目标改变时先取消旧 job，再启动 animateTo；elevation 因此以 Animatable 配合任务取消保留其速度清零和起始帧语义。
不能换成连续保速 retarget；形状自己的反向进度和速度规则仍由 AnimatedCornerRadius 保留。
拖拽 elevation 默认 0→8，ZIndex 取当前动画值是否大于 0；Surface 关闭 tonal 着色，保持 ListItem 自身颜色 Transition 的结果。
禁用时原生 press/focus/hover 发出取消/结束事件，外部源的拖拽不会被整行 enabled 改变误删。
combinedClickable 在 composition 中取消输入，形状及状态层按上游的后续帧顺序响应。

MaterialInteractionSource 以开始事件对象身份配对，状态层读取最后存活的非 press 交互，形状读取聚合 flags。
输入控制器处理取消与嵌套按钮所有权；ListItem 负责长按普通时间延时、选择回调和 Enter 的释放激活。
Core CombinedClickable 不在鼠标按下时请求行焦点，故仅取消行本身的 Pointer focus 请求，保留键盘和子控件焦点。
动画使用 child scope；模板替换释放旧 presenter lease，脱离视觉树释放 motion、源订阅和长按，重新挂载建立新连接。

## 已确认适配（2026-10-17）

维护者对本任务具体 API 与 XAML/C# 示例确认了：

- 两控件和显式 InteractionMode；受控 IsSelected / IsChecked；多选以 CheckedChangeRequested 回传，外部容器提供分组间距。
- IsInteractionEnabled 对应上游 enabled，IsEnabled 保留 Avalonia 子树禁用语义。
- CornerRadius 六状态形变；任意路径/切角 Shape、可配置 inset focus ring 明确暂缺。
- LongClickLabel 映射 HelpText；辅助技术直接动作使用槽内同命令按钮。Avalonia 12.1.2 没有第二个 Invoke 动作入口。
- supporting 多行只自动识别普通文本；复杂模板合并首末基线的限制保留，本轮不新增显式行数属性。

配对交互源、继承最小交互尺寸和内部关闭 tonal 着色的共享决定见 [DECISIONS](../../../../docs/DECISIONS.md)。

## 验证与边界

单元/headless 测试位于 `tests/Material3Expressive.Tests/ListItemTests.cs` 和 `Fixtures/ListItem.axaml`，
涵盖普通文本多行、五槽模板、受控选择、长按及取消、嵌套按钮、组形状、尺寸环境、automation 和生命周期。
VisualParity 的独立双端用例为 `Cases/ListItemCases.cs` 与 `reference/src/main/kotlin/cases/ListItemCases.kt`，suite 为 `list-item`。
覆盖模式/行数、明暗/禁用/选择色、分组位置、RTL、宽高约束、Padding、输入、拖拽、主题及形状替换。
按仓库规则使用非文字内容检查几何和颜色，不能据此宣称字体栅格一致。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ListItemTests
./tests/VisualParity/parity.ps1 self-test --family list-item
```

尚未实现：上述任意 Shape / inset focus ring。已接受适配：显式模式、受控事件、行启用语义、HelpText 长按替代和复杂模板基线限制。
未经验证：真实桌面辅助技术操作与字体栅格；headless automation provider 检查不替代屏幕阅读器验收。
