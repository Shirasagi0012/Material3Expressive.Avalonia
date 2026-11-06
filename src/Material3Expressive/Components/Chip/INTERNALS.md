# Chip 内部机制与维护说明

## 1. 上游基线与职责映射

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [Chip.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Chip.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Chip.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `AssistChip` / `SuggestionChip` / `FilterChip` / `InputChip`、`Chip` / `SelectableChip` | [Chip.cs](Chip.cs)、[Chip.axaml](Chip.axaml) |
| colors、elevation、Shapes / ChipArrangement | [ChipAppearance.cs](ChipAppearance.cs) 的完整值、[ChipThemeConverters.cs](ChipThemeConverters.cs) 的主题组装、Chip.axaml 的状态默认与原生配置 |
| `ChipContent` / `AnimatingChipContent` | [ChipLayout.cs](ChipLayout.cs)、[ChipIconSlot.cs](ChipIconSlot.cs) |
| 四族 Token 与 `ChipsTokens` | [ChipTokenSource.cs](ChipTokenSource.cs)、[Token 入口](../../Tokens/MaterialTokens.Comp.Chip.cs) |
| `ChipElevation` | Chip.cs 的 ElevationTarget / 交互目标更新，连接共享 MaterialInteraction |

## 2. 关键机制与架构契约

ChipTokenSource 保存基线及系统 TokenRef；主题按变体、选中/禁用和槽存在性选择默认值，
Avalonia 合并后，Chip/模板只消费有效配置。`ChipColors` 的 nullable 逐槽包装及 `Colors` 入口已删除：
容器、标签、描边分别使用原生 Background/Foreground/BorderBrush，图标使用独立的
LeadingIconColor/TrailingIconColor（IBrush?）。条件样式只更新内部默认候选，再通过主题级绑定接入
公开属性；普通用户 Style、绑定和局部值不会被选中/禁用触发器压过。

布局部件的 `MaximumSelectableWidth` / `MinimumInteractiveSize` 由同名组件 Token 提供默认值，
仍保留显式 Width / MinWidth 优先与原像素取整，不向 owner 写入额外约束。

`ChipElevation` 的六个 double 与 `ChipSpacing` 的两个 double 是完整不可变 record 值，
公开属性不再可空。主题 MultiBinding 按平面/Elevated 等条件动态组装；运行时不再逐字段或按 null
读取默认 Token。零值是完整配置本身，Material 的 Dragged=8、前后间距=8 等基线只来自 TokenSource。
显式对象整组覆盖，ClearValue 后恢复当前主题默认。不可把一个局部对象中未写的字段重新补成 Material 默认。

`ChipShapes` 是三个确定 CornerRadius 的完整 record，Filter/Input 的主题用 UnselectedShape、
SelectedShape、PressedShape 动态组装；null 唯一表示关闭容器形变，之后使用有效 CornerRadius。
省略 Shapes 时默认启用三形状，不设额外 expressive class。系统 shape 资源更新仍通过 TokenRef 传播，
不同的三组圆角值会按原 key(shapes) 语义重置状态。

### 固定与形状重载的本地表达

固定 Chip.kt 同时提供 `shape` 与 `shapes` 重载。固定重载使用普通 colors、固定间距，图标 alpha
进入/退出为 SlowEffects/FastEffects，size 为 FastSpatial/DefaultEffects；shapes 重载另外选择 tonal
colors、按槽存在性生成 4/8 DIP arrangement，图标 alpha 使用 DefaultEffects，size 进出均为 FastSpatial。
这不是仅切换容器形状的上游开关。

维护者 2026-10-05 确认本地把这些职责拆开：主题默认提供三形状，但 Shapes 只控制形变；
间距、颜色和图标 motion 独立，继续采用原固定入口默认。具体为前后 gap=8，Filter 未选中前导图标
使用 Primary，alpha 进入/退出为 SlowEffects/FastEffects，size 进入/退出为 FastSpatial/DefaultEffects。
Input 的头像/图标 Padding 与自身颜色规则仍保留。需要另一上游重载的组合时，调用方分别设置完整
Spacing、相应状态的 LeadingIconColor，并覆盖 `Comp.Chip.IconAlphaEnterAnimation` /
`IconAlphaExitAnimation` 为 EffectsDefault、`IconSizeExitAnimation` 为 SpatialFast；
`IconSizeEnterAnimation` 默认已是 SpatialFast。不要重新以 Shapes 是否为 null 推导这些独立配置。
此选择只涉及该本地 API 与默认组合，不把尚未支持的通用 Arrangement 或任意 Shape 追认为已实现。

图标、头像尺寸/裁剪及四条 motion 默认从主题进入 ChipIconSlot 的内部属性，形状 motion 进入 Chip
的内部 ShapeAnimation；运行时从这些有效配置产生大小、alpha、圆角和阴影，不再绕过主题重取 Token。
模板替换、资源或主题变更、重新挂载均按当前消费环境接入；动画中间值不回写公开配置。

默认容器 32 DIP、交互最小高 48 DIP、LabelLarge。固定圆角默认 Small 8 DIP；Filter/Input 默认由主题提供三形状。
前后间距默认均为 8 DIP，与 Shapes 独立；Input Padding 保留 avatar/leading/trailing 组合规则。
布局按物理像素舍入，leading/trailing 先测量，标签消费剩余约束。
Selectable 的默认 1000 DIP 最大宽度不吞掉显式 Width/MinWidth；RTL 使用 Avalonia 子树反射。

槽存在性由内容或模板决定，Input 的 AvatarTemplate 单独设置时也优先于 leading 图标；ChipIconSlot 在退出动画完成前保留内容、模板和头像包装，完成后同时清理内容与模板。
普通图标的 MaterialSymbol.DefaultSize 使用 Chip.IconSize Token 18 DIP，头像使用独立的 24 DIP 默认值；这些默认值不限制自定义内容，显式图标尺寸仍优先。
头像默认圆形裁剪，禁用只降低头像包装层 opacity，不降低整条内容。
Suggestion 不显示 trailing；其他 trailing 与 Chip 共享动作区域。
Filter/Input 的 Click、Command、automation Toggle 只发请求，不写 IsSelected。
自动化身份为 CheckBox，外部 selection 变化发出 ToggleState 通知。

原生 paint/padding 及图标颜色的普通 Style、局部值和绑定保留属性优先级。Surface 的反馈墨色通过实时 contentColorFor 绑定，
透明或非主题背景沿用外层内容环境；标签和两个图标分别解析自己的颜色 provider。
因此只改背景、只改标签色或外层内容环境都需要检查反馈颜色，不可缓存初始化时的色值。

Shapes=null 使用有效 CornerRadius，不进行选中/按压形变；非空采用未选中/选中/按压三个圆角及 FastSpatial。
配置按有效的三个圆角值比较：等值对象不重置动画，不同值对应上游 key(shapes) 重建。
容器尺寸变化后必须重新计算轮廓，包括动画已经结束的选中状态。

Filter/Input 槽以 Transition 的 size/alpha 轨迹进入退出，退出保留最后非空内容，反向保持运行状态；
四条 motion 规格是独立模板配置，不随 Shapes 切换。模板替换和 detach 取消旧 effect/作用域，
释放调用方图标的父级关系，重新挂载按当前状态建立动画。

2026-11-05 补齐共享动画属性契约：图标 alpha 保留 Transition<Float> 与原 Clamp 顺序，
经 `BindAnimatedValue` 投影到 Opacity，不再逐帧或在重建前普通赋值。作用域清理恢复底层属性，
过渡重建不会把调用方局部 opacity 改写为 0/1。该内部部件仍由图标显隐动画拥有有效 alpha，
没有新增 Chip 外部 API；头像包装的禁用透明度是独立配置，未改成该显隐轨迹。
`ComponentMotionContributionTests` 验证退出、重新进入、过渡重建和卸载后的恢复。

当前 `chip-animation-contribution` 完整 self-test 共 247 例：162 equal、31 withinTolerance、54 different，
零执行错误；240 例重复/额外截图稳定。8 个 Filter/Input × fixed/morph 的 icons/reverse-icons 动态场景均
withinTolerance 且稳定，头像替换 equal。全族剩余差异包括 shadowElevation 读数（本轮最大 1 DIP）、
三处 clicks/selected 读数以及部分图像，未由此次属性绑定修改解决；不能以编译或图标回归推广为全族一致。
7 个 source-press 场景在 Core 的重复或额外截图中有 raster 变化，Avalonia 自检稳定，原始诊断保留在报告 `.work`。

Elevation 选择最后活跃的 press/focus/hover/drag；禁用 snap。
进入为 120ms FastOutSlowIn，退出 150ms Bezier(0.4,0,0.6,1)，hover 退出 120ms。
选中不改变高度策略；平面 Filter 默认 hover 为 0 而 Elevated 为 3 DIP。
Elevation 只消费完整有效值，全零配置关闭所有交互阴影；清除局部值恢复当前变体的主题配置。

### 交互源与高度 effect

高度交互列表、按压 collector 列表和记忆按压布尔值由组件独立维护。替换源时高度列表保留，
按压列表清空而布尔值保持到新流下一事件，indication 清空动画并只观察后续事件。
原生 Press 坐标为 owner DIP，呈现时映射到内部 Surface。

新高度目标先让旧动画采样当前帧，再取消并等待旧 effect 结束；lastInteraction 仅在动画或 snap 成功完成后更新。
高度使用 Float Animatable/Tween；`BindFrameCommittedValue` 捕获当帧显示值后，
`ScheduleFrameCommit` 再执行 effect。立即 snap 的结果在下一批呈现，全程保持动画优先级贡献。
模板替换和卸载释放绑定及提交句柄，恢复底层属性源。Source API 的授权见引入提交 e89f5fc8。

## 3. 已确认决定与已知差异

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Chip/README.md)。

2026-10-03，维护者批准四个 Button 派生控件、外部选中职责、IsElevated、内容槽及颜色/elevation 配置，
并以 ChipShapes 圆角和 ChipSpacing 前后间距表达当前能力；不由此扩大到任意 Shape 或通用 Arrangement。
配置对象的当前归属与默认组合以上面的 2026-10-05 配置确认为准，旧 nullable 颜色/高度/间距回退不再保留。

2026-10-04，维护者批准平面 Filter 的默认 hover elevation 从固定 Compose 的 1 改为 0 DIP，
范围为 Shapes=null/非空、未选中/选中。透明平面 Filter 悬浮时，Skia Ganesh GPU drawShadow 会暴露容器内部灰环；
固定 Skiko `0.152.0-alpha02` 原生 GPU 探针与 Avalonia 桌面均复现，CPU 未出现同样灰环。
修改仅在该默认高度，保留 hover 状态层、形状动画和其余高度，不修改共享阴影算法。
完整 elevation 配置显式令 Hovered=1，或覆盖 `Comp.Chip.FlatFilterHoverElevation`，可恢复上游高度及相应 GPU 效果。
这与下述阴影坐标接入是独立决定，真实 Compose 默认仍留在参考端。

共享决定按原文范围使用，不复制或扩大其授权：

- [Tween / Bezier 精度](../../../../docs/DECISIONS.md)：Chip 的 double/Float 差异继续精确报告，不取整读数
- [阴影坐标接入](../../../../docs/DECISIONS.md)：历史 DIP+CTM 差异已由 §20 的物理像素路径与固定原生分支替代；具体 Chip 组合仍以当前运行验证
- [Surface 圆角描边](../../../../docs/DECISIONS.md)：物理像素 ceil、内缩与不占用内容空间
- [Core Desktop 焦点](../../../../docs/DECISIONS.md)：真实焦点影响 elevation，indication 单独过滤首次鼠标聚焦
- [共享键盘职责](../../../../docs/DECISIONS.md)：保留原生 Enter 按下激活

尚未实现任意路径/切角 Shape 和通用 Arrangement。文字栅格和真实辅助技术未经验证。

历史未解决且未接受的 `morph.source-disable` 阴影差异涉及 Filter、ElevatedFilter 和 Input。
复现使用 `new ChipElevation(2, 3, 4, 5, 9, 1)`，外部 Press 后 65ms 禁用，在 160ms 捕获。
2026-11-05 本轮依据用户扩大授权继续核验：`chip-shadow-outline` 当前捕获确认 160ms 的尺寸、
1 DIP 高度和实际圆角 `7.6246843338012695` DIP 都相等，仍有 raster 差异。新增 Surface 静态透明容器
用例可独立复现，因此不能归因于 Chip 禁用/形变时序，也不属于已接受的高度精度差异。
固定 Skiko 的独立 ambient-only 原生绘制复现内部渐变；详见 [Surface 记录](../Surface/INTERNALS.md)。
物理像素路径实验没有改善；替换 quadratic 分支仅改善部分样本且导致回归，均已撤回。
该历史诊断随后由共享决定 §20 的固定非 Ganesh 分支与物理像素坐标证据替代；不能继续把旧报告当作当前失败。
新增圆角观察器在全部 10 个 fixed/morph source-disable 配置运行成功，零执行错误且全部稳定；
运行目录为 `shadow-observation-<kind>.<fixed|morph>`。这些用例仍报告真实时序、Float/double 和
raster 差异；观察器读取现有缓存 outline，不通过重新计算期望轮廓来代替实际呈现状态。
部分重叠 Press 的 Core 重复捕获与额外截图存在最大单通道 1 的不稳定像素，现有稳定性门禁仍保留失败。

## 4. 验证入口

- Float spring 修正后的全族 `chip-spring-all` 共 247 例：165 equal、71 withinTolerance、11 different，
  执行错误为零；7 例参考端重复/额外截图不稳定，不能把状态计数等同于全部通过。
  相比高度修正后的 `chip-elevation-current`，三个形变 source-disable 的圆角余差已消除。
  剩余为五个平面 Filter hover 默认适配、一个 Enter 激活时序及五个自定义颜色图像场景；
  自定义颜色夹具后续修正及独立证据见 VisualParity DECISIONS，不改容差或源事件时间。
  原 10 个自定义颜色组合在 `chip-colors-*` 修正选择器范围后全部 equal、稳定且无错误；
  其五个旧图像失败已消除。其余六个 different 对应已记录的平面 Filter hover 与 Enter 时序适配，
  Core 部分重叠 Press 的重复性失败仍独立存在，不能据此宣称整族通过。

- 2026-11-06 本轮依据用户扩大授权复用决定 §19/§26，修复高度 Float 运算和 snap 时丢失底层绑定。
  `elevation-commit-three` 三个 Chip 配置/禁用/合并场景读数精确相等、图像容差内、重复稳定。
  `chip-elevation-frame` 的 elevated-filter.morph.source-disable 所有高度读数精确相等，图像全部
  相等或容差内且稳定；当时 161/176ms 圆角仍有约 4.77×10⁻⁷ DIP 的独立差异。
  该余差随后由共享 Float spring 边界修复消除，`chip-spring-current` 高度和圆角均精确相等、
  图像在原容差内且稳定，未改门禁或补帧；范围及 Double 保留规则见共享决定 §19。
  该结果仅覆盖实际执行组合，不推论全部 Chip 或 GPU。无公开 API 迁移；贡献与生命周期测试验证
  禁用和模板替换后恢复最新底层绑定。

- 2026-11-06 共享阴影修正后的 `shadow-current-chip.dynamic.elevated-filter.morph.source-press`：
  所有已请求图像精确相等或在原容差内、执行错误为零；仍有 5 个 double/Float 高度读数不同，
  例如 32ms Core 为 2.0499308109283447、本地为 2.0499308072030544 DIP。
  Core 重复截图仍有不稳定，不能记为用例整体通过。该实际调用方确认形变/重叠输入下的阴影接入，
  不替代整个 Chip 族重测，也不把精度差异转为放宽比较规则。

- `ChipTests`、`ChipTokenConfigurationTests` 检查完整配置、属性优先级、槽动画、选中请求、交互源及高度中断。
- `ElevationSourceLifetimeTests` 检查 source、模板和卸载的状态归属。
- 双端 `ChipCases.cs` / `ChipCases.kt` 的族名为 `chip`，包含 source 的重叠、替换、禁用与重挂场景。
- 透明 Chip 的测试环境需匹配 Core 外层默认黑色 content color，注入 Press 按两端同一 Surface 局部原点换算。
- 2026-11-04 `chip-focus` 运行 `20261104-081632-75d70692`：76 例均稳定、执行错误 0，
  45 例完全相等、30 例容差内、1 例不同。覆盖七种外观、固定/形变入口、深色、选中、焦点反向、
  禁用、反馈模式切换、换源与重挂。`ChipTests` 增加七种外观的焦点/轮廓/源生命周期断言；
  Catalog **Inset focus ring** 提供四类 Chip 示例。
- 唯一超差的 `filter-chip.morph.press-to-ring` 保留真实上游默认 hover。追加高度观测的运行
  `20261104-082531-63164a55` 显示 160ms Core 为 1 DIP、本地为 0，图像差异位于阴影；
  400ms 起两端恢复像素相等。它对应上文已确认的平面 Filter hover 高度决定，没有修改参考或扩展豁免。
- 三个形变 `press-to-ring` 用例追加精确高度数值后，ElevatedFilter 的运行 `20261104-083529-2d0011c6`
  仍然重复稳定、像素容差内，但五个动画采样数值不同：例如 208ms Core 为 2.9001383781433105，
  本地为 2.9001383861416996 DIP。保留该精度差异，不修改数值比较规则。
  Input 的运行 `20261104-083549-5de21c91` 数值/像素完全相等且稳定；因此当前新增高度观测后的套件
  有两个 different 用例，不能沿用追加观测前的单一失败结论。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter "FullyQualifiedName~ChipTests|FullyQualifiedName~ChipTokenConfigurationTests|FullyQualifiedName~ElevationSourceLifetimeTests"
./tests/VisualParity/parity.ps1 self-test --family chip
./tests/VisualParity/parity.ps1 self-test --family chip-focus
```
