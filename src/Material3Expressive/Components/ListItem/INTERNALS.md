# ListItem 内部机制与维护说明

## 1. 上游基线与职责映射

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

## 2. 关键机制与架构契约

测量顺序为 leading、trailing、overline、main、supporting；侧槽各预留 12 DIP 间隔。
有限宽度填满约束，无限宽度按侧槽与正文最大宽度求和；正文高度求和，再与侧槽最大高度、默认行高比较。
overline+supporting 同时存在或普通 supporting 首末文本基线不同，选择 88；任一辅助正文存在选 72，否则 56。
标题本身的换行不参与行数分类。Auto 对齐的内容区阈值为 `(88+72)/2-10-10=60`，来源是默认 Token，不跟实例 Padding 改动。
Avalonia Measure 的无限约束承担常见内容固有尺寸需求，不另建 Compose 的 min/max intrinsic 调用协议。

Side presenter 的最小交互环境为 `max(0, inheritedMinimum - start/endPadding - spacing)`，默认 20；
该值从呈现位置向槽内容传播，支持动态祖先环境和 Padding 更新。
RTL 由模板布局镜像；CornerRadius 是物理角，默认六状态及分组只使用左右对称值。

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

固定两端 ListItem.kt 都在 combinedClickable 上使用单一 `ripple(focusRingShape = shape)`。
共享控制器按 ListItem（含 SegmentedListItem）识别这一生命周期：切换 Opacity / InsetRing
仅更新配置，不按 Surface 的双 indication 路径清空 ripple 或重建焦点订阅。
复用 MaterialSurface 的环部件，轮廓取当前 AnimatedCornerRadius，无新增公共形状框架。

AnimatedShape 在重定向时将 progress 重设为 0，轮廓可能暂时不动，但 Compose 的可观察 progress
仍使绘制缓存失效。ListItemMotion 将这一 progress 变化传给 Surface 的内部 shapeStateChanged 输入，
让焦点环在相同帧重新读取主题色；只比较最终 CornerRadius 会让主题切换后的失焦首帧仍绘制旧颜色。
纯主题色变更仍保留静止环的缓存，未改变已确认的共享缓存语义。

## 3. 已确认决定与已知差异

维护者对本任务具体 API 与 XAML/C# 示例确认了：

- 两控件和显式 InteractionMode；受控 IsSelected / IsChecked；多选以 CheckedChangeRequested 回传，外部容器提供分组间距。
- IsInteractionEnabled 对应上游 enabled，IsEnabled 保留 Avalonia 子树禁用语义。
- 原先只确认 CornerRadius 六状态形变；任意 Shape 延期的原因是缺少共享路径/裁剪能力。2026-11-08 本轮维护者授权修订该边界，现复用共享 Surface 和 AnimatedCornerRadius，增加六个可选 MaterialShape 属性，每个状态独立覆盖圆角。原有 Shapes / 分组默认和调用方不需要迁移。
- LongClickLabel 映射 HelpText；辅助技术直接动作使用槽内同命令按钮。Avalonia 12.1.2 没有第二个 Invoke 动作入口。
- supporting 多行只自动识别普通文本；复杂模板合并首末基线的限制保留，本轮不新增显式行数属性。

配对交互源、继承最小交互尺寸和内部关闭 tonal 着色的共享决定见 [DECISIONS](../../../../docs/DECISIONS.md)。

固定 `ListItemDefaults.kt.shapeForInteraction` 在六项全为 CornerBasedShape 时使用 keyed AnimatedShape；任一任意路径会让整个配置走直接选择分支。
本地将切角参数保存为不可变 CornerShape.Cut 快照，配置属性变化在既有 composition 帧比较并重建；订阅随 motion 卸载释放。
圆角/切角混合时保留固定 `Interpolatable.lerp` 的 `<0.5` 选起点、否则选终点，而非仅根据 AnimatedShape 的概述注释立即切目标。
第三目标保存真实中间形状，反向保持原进度翻转和速度取反。路径绘制消费同一进度，不引入新的动画时钟。
Surface 的内部外部角值入口现也支持切角；ListItem 自己维护 ripple shape 的身份与重建，Surface 禁止重复重建反馈。
模板替换清除旧 Surface 轮廓，最终 Bounds 发布后再次解析百分比，避免子布局仍读到上一轮容器尺寸。

未经验证的范围包括真实桌面辅助技术与字体栅格。焦点环边缘保留已观测到的栅格差异，精确像素及容差内结果分别报告。

## 4. 验证入口

- `ListItemTests` 和 `Fixtures/ListItem.axaml` 覆盖五槽、受控选择、长按、嵌套按钮、分组、尺寸环境及生命周期。
- `InsetFocusRingTests` 覆盖普通和分段项的模式切换、波纹保留、形变、重挂和主题首帧。
- 双端 `ListItemCases` 的族名为 `list-item`，`FocusRingCases` 包含 `focus-ring.dynamic.list-item.*` 与 `segmented-list-item.*`。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~ListItemTests|FullyQualifiedName~InsetFocusRingTests'
./tests/VisualParity/parity.ps1 self-test --family list-item
./tests/VisualParity/parity.ps1 self-test --family focus-ring
```

本轮 `list-item-paths-final` 双端 self-test 完成 137 例，122 equal、15 withinTolerance、0 different、
执行错误 0；新增 66 例路径状态/动画场景全部匹配且重复稳定。全部 137 例中已有 `dynamic.key-long`
在 Core 的 time-176000 截图发生重复与额外截图变化，Avalonia 两项自检均稳定，故整组 self-test
仍返回非零；没有放宽重复性门禁，初始失败与修正后的原始运行均保留。

本轮同时补齐单一 indication 的形状裁剪与 Border.kt 零 inset Clear hairline。RTL 在无 pointer 历史的
键盘按压初值使用物理左上角；新增行为测试和双端 space 样本核实该映射。任意路径拖动态的初始
测试输入曾误用两个实例（Core 同一对象）；现两端统一对象身份。外部 Press 的 RTL 坐标也按各框架
的实际本地坐标约定映射，未改产品行为来迁就用例。
