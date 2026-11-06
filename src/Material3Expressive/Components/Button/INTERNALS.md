# Button 实现说明

本文面向维护者；使用方法见 [README](README.md)。范围是 Avalonia 原生 Button 的五种主题、
尺寸/内容适配，以及与共享 Surface、交互和动画能力的连接。IconButton 仅在复用相同能力时涉及，
其独立行为仍见[对应组件说明](../IconButton/README.md)。

## 固定上游与本地入口

基线以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：

- AndroidX：`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，Material3 `1.5.0-alpha27`，默认逻辑依据
- Compose Multiplatform Core：`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`，`v1.13.0-alpha01`，桌面平台分支依据

下表 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`
为前缀。按固定提交读取文件，不以仓库 HEAD 或最新包替代。

| 上游路径 / 关键符号 | 本地对应 |
| --- | --- |
| [Button.kt（AndroidX 固定提交）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Button.kt)：`Button`、`ElevatedButton`、`FilledTonalButton`、`OutlinedButton`、`TextButton` | [Button.axaml](Button.axaml) 的五种 `ControlTheme`；实际控件仍为 Avalonia Button |
| `Button.kt`：`ButtonDefaults.contentPaddingFor`、`textStyleFor`、各颜色/elevation 默认值、`outlinedButtonBorder` | [Button.cs](Button.cs) 的 `ButtonSizesTokenSource` 及五种外观 TokenSource；[Token 入口](../../Tokens/MaterialTokens.Comp.Button.cs) |
| `Button.kt`：可选 Icon/label 排列及 `ButtonDefaults.iconSizeFor` / `iconSpacingFor` | [MaterialButton.Icon / IconTemplate](Button.cs)、[ButtonContentRow](ButtonContentRow.cs)；IconButton 的专用 Symbol 入口不由此 API 替代 |
| `tokens/ButtonXSmallTokens.kt`、`ButtonSmallTokens.kt`、`ButtonMediumTokens.kt`、`ButtonLargeTokens.kt`、`ButtonXLargeTokens.kt`；`tokens/FilledButtonTokens.kt`、`ElevatedButtonTokens.kt`、`FilledTonalButtonTokens.kt`、`OutlinedButtonTokens.kt`、`TextButtonTokens.kt` | 原始尺寸/外观关系保存在上述 TokenSource；Composable 的默认值修正另行保留，不用原始 Token 覆盖 |
| `Button.kt` 的 `defaultMinSize`，`Surface.kt` / `InteractiveComponentSize.kt` 的 `minimumInteractiveComponentSize` | [ButtonLayout](ButtonLayout.cs)：容器默认尺寸、外层交互留白及像素取整；[MinimumInteractiveAlignment](../../Foundation/MinimumInteractiveAlignment.cs) 暴露内部对齐读数 |
| `Button.kt` 的 `ButtonElevation`，`internal/Elevation.kt` 的 `animateElevation` | [ButtonElevation / ButtonElevationController](Button.cs)，经 [MaterialInteractionController](../../Foundation/Interaction/MaterialInteractionController.cs) 连接每个 Surface |
| `Button.kt` 的固定 `shape` / 变形 `shapes` 入口、`shapeByInteraction`，`internal/AnimatedShape.kt` 的 `rememberAnimatedShape` / `AnimatedShape` | `MaterialButton.PressedCornerRadius` → [MaterialSurface](../Surface/MaterialSurface.cs) → [AnimatedCornerRadius](../../Foundation/Interaction/AnimatedCornerRadius.cs) |
| [Button.kt（Core 固定提交）](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Button.kt) 及桌面 focus / indication 分支 | 共享控制器、[MaterialInputMode](../../Foundation/Interaction/MaterialInputMode.cs)；平台差异按[已确认决定](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)处理 |

该映射用于定位关键职责，不构成全部上游依赖或完整一致性的证明；验证应按文末入口分别报告。

## 配置流与覆盖优先级

ButtonSizesTokenSource 与各外观 TokenSource 保存基线和 TokenRef；主题负责按尺寸、外观及禁用状态
选择默认配置，Avalonia 属性系统合并用户 Style、绑定和局部值，模板再把有效配置送入布局和 Surface。
涉及公开配置的尺寸 class 与 `:disabled` 条件只更新内部 [ButtonThemeProperties](ButtonThemeProperties.cs) 的默认候选；
`Padding`、`CornerRadius`、`Background`、`Foreground` 等公开属性通过主题级绑定消费候选。
不能把条件 Setter 直接写回公开属性，否则 StyleTrigger 会压过普通用户 Style。
内部候选不持有另一套 nullable 默认值链，也不是供应用设置的 API。

`Comp.Button.MinWidth` 与 `MinimumInteractiveSize` 默认分别为 58/48 DIP，由 ButtonLayout 自身主题
送入内部属性；不再把 Material 数字存进该类型的属性元数据，也不回写 owner 的 MinWidth/MinHeight。
`ExtraSmallIconSpacing` 保存 `iconSpacingFor` 的 4 DIP 派生默认，供共享按钮族引用。
容器高度、图标尺寸/间距等只属于模板的配置直接绑定内部部件，不要求为每个 Token 增加公开属性。

例如 Medium Token → 内部 Padding 候选 → 主题级 `Button.Padding` → 内容布局；用户 Style 设置 Padding
后切换尺寸仍保持用户配置，清除局部值后按剩余样式与当前 Token 重新合并。
资源替换、主题变体与重挂载仍从当前消费者环境解析，动画只更新 Surface 呈现值，不修改公开配置。
`ButtonElevation` 保留为五个确定高度组成的完整值，由主题 MultiBinding 动态组装；它与每实例控制器的
运行状态分开，未被本次 nullable 配置清理删除。

这一路径按维护者 2026-10-05 确认的配置组织实施；目的仅是让已有条件默认值服从 Avalonia 覆盖机制，
不改变下文记录的输入、动画、精度及未决上游差异。

## 模板与默认值

`MaterialFilledButtonTheme` 持有完整模板和尺寸选择器；另四种主题用 `BasedOn` 改变颜色、描边和
高度配置。隐式 `{x:Type Button}` 主题基于 Filled。尺寸 class 仅选择默认值，不重新创建一套控件。

模板层次是 `ButtonLayout` → `MaterialSurface#PART_Container` → `ButtonContentPresenter`：

- owner 的尺寸/最小尺寸传给 ButtonLayout；背景、描边、静止/按压圆角传给 Surface
- owner 的 `Foreground` 通过 `TextElement.Foreground` 显式传给 Surface，防止 Surface 自身主题遮蔽按钮内容色
- 尺寸的 `LabelTextStyle` 作为 `MaterialText.DefaultStyle` 传入内容；聚合排版的共享规则见[对应决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)
- Surface.StateLayerBrush 绑定当前 Foreground，与固定 Ripple.kt 的 LocalContentColor 默认相同；透明度时序和 ripple 属于共享交互层
- Surface 绘制描边而不消耗内容尺寸，具体实现与适用范围见[Surface 圆角描边决定](../../../../docs/DECISIONS.md#surface-圆角描边)

默认值应沿用 Composable 的计算关系，不能直接把整张原始 Token 表搬到模板：

- `ContentPadding` 是本地复合槽，来自 `contentPaddingFor` 的默认参数（无首尾图标，
  `shouldUsePrecisionPointerComponentSizing=false`）。XS/S/M/L/XL 的水平/垂直值为
  12/6、16/10、24/16、48/32、64/48 DIP；XS 的水平 12 含上游函数修正。新增 Icon 槽不按
  presence 改写这组 Padding，避免改变原有无图标文本布局或引入未经批准的默认值分支
- `textStyleFor` 使 XS/S 使用 LabelLarge、M 使用 TitleMedium、L 使用 HeadlineSmall、XL 使用 HeadlineLarge
- Outlined 的默认描边宽度按 `outlinedButtonBorder()` 使用 1 DIP，所有尺寸一致；L/XL 的原始
  2/3 DIP Token 仍保留，主题不自动消费它们
- Text 内容色使用 Composable 的 Primary，而非原始 LabelColor 的 OnSurfaceVariant
- Filled/Elevated 禁用背景为 OnSurface × 0.10，Outlined 禁用描边为 OutlineVariant × 0.10；
  Filled/Elevated/Outlined/Text 禁用内容为 OnSurfaceVariant × 0.38。Tonal 保留 OnSurface × 0.12
  背景和 OnSurface × 0.38 内容；没有为此全局改变系统禁用容器 opacity
- Filled/Tonal 配置默认、按压、焦点、悬停、禁用高度为 0/0/0/1/0 DIP；Outlined/Text 的配置为 null

这里保留影响移植判断的派生关系，完整成员和值以 [Button.cs](Button.cs) 与
[MaterialTokens.Comp.Button.cs](../../Tokens/MaterialTokens.Comp.Button.cs) 为准。

## 布局与内容边界

ButtonLayout 分开处理可见容器的默认最小值与外层 48 DIP 留白。这样不会把 `defaultMinSize`
误写成 owner 的硬性 MinHeight；显式 Width/Height、正的 MinWidth/MinHeight 和父级约束仍能起作用。
背景居中按物理像素取整，受约束后的偏移另行取整，不能合并成一次连续 DIP 计算。
禁用仍保留布局空间；这不是 Android 输入层的触屏目标扩展。
共享读数及适用范围见[最小交互布局决定](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)。
注意普通 Button 的 `xsmall` 只改变容器默认高度，不像 IconButton 的 XS 那样显式约束外层为 32 DIP。

[ButtonContentPresenter](ButtonContentPresenter.cs) 仅为没有适用模板的字符串建立自动标签：
软换行后保留可用段落宽度，避免 Avalonia 按最长行回报宽度而使整段重新居中。
普通裁剪保持完整文本布局，在有限标签边界裁剪；省略号仍使用有限高度格式化。
直接提供的控件、显式 ContentTemplate 和匹配的 DataTemplate 不由此路径改写。
主题在 Button 上提供 `TextBlock.TextWrapping=Wrap` 默认值，Presenter 用 TemplateBinding 读取该附加属性，
自动标签继续通过原生继承消费。不得在 Presenter 上写死 Wrap，否则按钮的样式与局部设置无法传入。

`MaterialButton.Icon` 与 `IconTemplate` 附加到原生 Button，`ButtonContentRow` 在图标存在时以固定尺寸盒
排列图标和标签，标签仍使用原 `PART_ContentPresenter` 与 ButtonContentPresenter。presence 包含非空 Icon
或非空 IconTemplate；Icon-only 不占标签间距。图标提示尺寸为 XS/S/M/L/XL 20/20/24/32/40 DIP，
标签间距沿用 `iconSpacingFor`：XS 为 4 DIP，其余取各档 Button Token。图标按逻辑 leading 排列，
其与标签间距从标签 Padding 的内容边缘计算；无图标时沿用原 presenter 测量、分配、约束与 access key 流程。
显式图标尺寸优先于 `MaterialSymbol.DefaultSize`，任意 Control 不会因此被缩放。

### DropDownButton 尾部箭头

模板的 `ButtonContentRow` 有第三个子项 `PART_TrailingIconPresenter`。它在模板中是裸的 ContentPresenter：
没有绑定、Token 值或尺寸 class setter，其他 Button 只多一个零尺寸的空 presenter
（同一计数器测得 Filled/Tonal/尺寸 class 按钮整棵树的 `{m3:Token}` 绑定数与引入前相同）。
两子项模板如 ToggleButton / SplitButton 的布局不变。

箭头、`DefaultSize`（各尺寸档同前导图标的 Token）、对齐与旋转全部来自 Button.axaml 末尾的全局类型选择器
样式 `DropDownButton /template/ ContentPresenter#PART_TrailingIconPresenter`（及 `:flyout-open`、尺寸 class 变体），
而不是任何 ControlTheme：它们不论 owner 当前用哪个 Button 主题（隐式、键控或运行时切换）都会生效，
Button 主题本身也无需知道这个子类。自定义模板若没有该部件就没有箭头。
尾部盒复用 `IconSize`/`IconSpacing`，位于标签右 Padding 内侧，与前导图标对称：
`[左 Padding | 图标 | 间距 | 标签 | 间距 | 箭头 | 右 Padding]`。

尾部盒的有无只由其 Content/ContentTemplate 决定，不切换 `IsVisible`；空盒按零尺寸测量与排列。
由于零尺寸的空盒被填入后自身期望尺寸不会变化，`ButtonContentRow` 用 Panel 的
`AffectsParentMeasure` 声明子 presenter 的 Content/ContentTemplate 影响自身测量，不依赖测量期间重放样式。

旋转用 Foundation 的内部附加属性 [`ChevronRotation.Angle`](../../Foundation/Rendering/ChevronRotation.cs)（与 Expander 共用）（0 / `:flyout-open` 时 180）映射到 RenderTransform，
由 ComposeAnimation 的 `AnimatedDouble` 声明以默认弹簧动画。默认弹簧即 Compose 的 `spring()`，
对应上游 SplitButtonSamples 中尾部箭头的 `animateFloatAsState`；本库 SplitButton 本身不绘制箭头、
也不旋转（箭头由使用者作为 Icon 提供），因此没有可直接复用的箭头机制。

## 交互与动画职责

[MaterialInteractionController](../../Foundation/Interaction/MaterialInteractionController.cs) 持有交互记录，
向 Surface 提供状态层、ripple 和形状输入；Button 的点击与命令仍由原生控件负责。
高度从最后一个有效 press / hover / focus 记录选取，不能仅靠伪类优先级重建交互顺序。

`MaterialButton.InteractionSource` 由模板的 `ButtonLayout.CollectButtonInteractions` 接入，
`ButtonInteraction` 的生命周期跟随模板挂载；IconButton 保留其原有专用源入口。
固定 Button.kt / ToggleButton.kt / SplitButton.kt 的形状收集器以 source 为 effect key：
press 列表在换源时清空，remembered bool 保留，直到新源下一事件重算；只有需要按压变形的分支收集。
elevation 的列表位于 effect 外，换源保留开始对象，Release/Exit/Unfocus 按身份删除；Drag 不参与高度。
内部 ElevationInteraction 读数仅给已有 ButtonElevationController 选目标，不创建新的动画模型。
indication 换源清空状态层与波纹，但保留形状和高度作用域。真正卸载模板则断开并清理所有本地收集器。

普通按钮的 ripple 墨色取当前 Foreground，包括禁用默认、Style、绑定和显式覆盖。
原生禁用取消自己的 starts，外部 held press 必须由发送者配对结束。
测试入口为 `ButtonInteractionSourceTests` 与双端 `button-source`；后者保留属性即时更新首帧及浮点精度的真实差异，
不作为这些差异已获接受的依据。

`ButtonElevation` 是可共享的配置，每个按钮的 `ButtonElevationController` 才持有运行状态：

- 目标通过 `ScheduleFrameCommit` 合并提交；先由 `BindFrameCommittedValue` 捕获当前显示值，再运行 effect。禁用 snap 的值在下一提交批呈现，始终保持动画优先级贡献
- 内部高度和 Tween 使用 Float，公开 DIP 配置仍为 double；新目标取消并等待旧 effect 清理后开始，保留固定上游 LaunchedEffect 的中断顺序
- 离开状态按上一个目标值推断，依次比较 Pressed、Hovered、Focused，保留多个状态高度相同时的上游策略
- 进入交互为 120ms；离开 hover 为 120ms，其余离开为 150ms；缓动与 target 选择均在控制器中
- 配置移除、模板替换或呈现者解绑时清理旧作用域及动画绑定，恢复原 Surface 属性，不能共享播放状态

形状复用 Surface 的状态与共享 `AnimatedCornerRadius`，没有 Button 专属形状控制器：
端点先按当前尺寸夹取，Float 进度和插值保留；反向时翻转进度并取反速度，第三目标从当前形状重建起点。
圆角配置或 effects spring 更换会重建状态；换目标先取消旧 effect。形状与 elevation 的帧合并
不应扩展到原本拥有独立事件时序的状态层和 ripple，也不能为通过截图而修改弹簧参数或测试帧。

## 已确认的组件选择

以下确认依据保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/df8656750b2ac03d29ecfd3d81949bfadedcffdf/src/Material3Expressive/Components/Button/README.md)，不从现有代码反推授权。

### 固定圆角加可空按压目标

2026-10-03，维护者确认 `MaterialButton.PressedCornerRadius` 并授权实施。
固定上游同时提供固定 shape 与变形 shapes 入口；本地以 owner `CornerRadius` 和可空按压目标表达，
不拆成两个组件入口，也不新增 Shapes 配置或变形开关。默认对应固定 shape 行为，
但保留现有 Expressive 尺寸和 Padding，不因形状入口切换而切换布局。
范围是五种 Button 主题与四种 IconButton 主题；其他 Surface 调用方不自动套用此选择。

同日确认的 Button 修正还包括上述默认颜色、描边、elevation、帧提交及指针移出取消规则。
受限内容夹具按 Compose 的 `size` / `requiredSize` 读取实际排列几何，不给组件加布局补偿。
当时阴影算法延期不等于接受所有阴影差异；后来已接受的范围仅按下列共享决定执行。

### 共享决定索引

本轮依据用户扩大授权重新决定普通 Button 墨色接入。原模板按伪类选择独立 Token，意图保留
状态色定制，但自定义 Foreground 后反馈仍取默认色。固定两端 Button.kt 向 Surface 传 contentColor，
Ripple.kt 默认读取 LocalContentColor；新黑色内容用例实际复现白色 ripple。沿用 Toggle/Split 已有的
Foreground→Surface.StateLayerBrush 模板绑定，删除重复状态 Setter。保留旧 Token 数据定义供显式
模板样式使用；按有效属性优先级猜测用户是否覆盖会引入隐含分支，也无法表达“内容和墨色独立”，
不采用。现有独立 StateLayerBrush 入口已经足够，无新 API；使用者迁移示例见 README。

以下决定由共享文档维护，组件不另行复制或扩大其理由、授权及范围：

- [共享交互反馈与键盘激活职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)：包括普通 Button 的 Enter 按下激活适配
- [Core Desktop 原生焦点反馈](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)：真实焦点、输入模式与 indication 的区别
- [最小交互布局与工具栏对齐读数](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)
- [聚合 TextStyle 与 Token 层文字单位换算](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)
- [Surface 圆角描边](../../../../docs/DECISIONS.md#surface-圆角描边)
- [本次保留 Tween / Bezier 精度差异](../../../../docs/DECISIONS.md#本次保留-tween--bezier-精度差异)
- [本次保留阴影坐标接入差异](../../../../docs/DECISIONS.md#本次保留阴影坐标接入差异)

## 一致性边界与验证

2026-11-06 本轮依据用户扩大授权，将高度接入既有共享帧提交能力（决定 §26），替代仅等待
NextFrame 的方案；没有新增组件队列或公开 API。`elevation-commit-three` 的三个 Button 配置/禁用/
合并场景高度读数全部精确相等且重复稳定；其暴露的自定义 Foreground 墨色差异随后由内容色接入修正。
`elevation-commit-ink` 九个真实组件场景均图像在原容差内、读数精确一致、重复稳定。
`button-elevation-frame` 的 elevated.disable 已消除 80/208ms 高度差异；65/193ms 非帧输入后的
原生 enabled 即时属性/颜色与 Core 重组缓存差异仍精确报告。不能把高度修复当成整个用例通过。
`ComponentMotionContributionTests` 验证禁用期间底层绑定保持、卸载恢复其最新值及零待处理帧。
`button-ink-background` 的五种外观在所有实际帧采样中内容颜色精确一致、图像相等或原容差内，
覆盖半透明颜色替换、清除覆盖、外部 held press 与禁用；全部重复稳定。129/193/257/321ms 的
非帧修改仍报告原生属性立即更新与 Core 组合缓存的差别，所以五例整体均为 different。
`ButtonInteractionSourceTests` 还验证旧独立墨色可通过 README 的模板 Style 迁移，并恢复内容色绑定。
`button-source-current` 对普通、Toggle、Split 共享源的 73 例全量复核为 1 equal、17 withinTolerance、
55 different，执行错误为零；18 例含 Core 重复/额外截图不稳定。该运行早于后续 Float spring 修正，
保留换源、非帧属性与其他组件形状的实际失败，不用九个高度场景推论整个按钮族已经一致。

可选 InsetRing 已由 Surface 的独立视觉部件接入；配置与通用维护规则见
[Interaction](../../Foundation/Interaction/README.md)。默认仍为 Opacity，原生 FocusAdorner 保持关闭。
RTL 非对称描边曾因纯视觉子层缺少方向继承而与背景反向；已在 Surface 内绑定各层的方向，
保留公开圆角值与既有描边算法。`button-focus` 同时检查描边、悬停状态层、焦点环和按压轮廓的组合。

### 尚未实现

- `shouldUsePrecisionPointerComponentSizing=true` 的尺寸/Padding 基线分支；固定 `Button.kt` 在此分支
  将 Small MinHeight 设为 36 DIP、垂直 Padding 设为 8 DIP、图标侧水平 Padding 设为 12 DIP，
  当前不可变 TokenSource 未表达这些选择

固定 Core `skikoMain/.../PrecisionPointer.skiko.kt` 的 `EnsurePrecisionPointerListenersRegistered`
只调用 content，没有注册精确指针监听；不能依据 Avalonia 当前鼠标设备自动开启 Android 的紧凑分支。
该开关在上游同时影响 Button、ToggleButton、Menu、ListItem 和 AlertDialog，组件局部 bool 会拆散同一环境语义。
推荐先由维护者决定共享输入配置的范围，例如作用域显式选择 `MaterialPrecisionPointer.SetIsEnabled(window, true)`
（仅调用提议，未提供此 API），再通过各组件的 Token 候选映射实现尺寸/Padding 选择。影响包含运行时重新测量、
图标侧 Padding、继承与局部覆盖优先级，以及桌面默认是否继续与 Core 一致；该公共共享能力未获批准、未实施。

### 未决差异与未经验证项

- `button-source` 的 disable/enable、换源和原生 release 事件采样仍保留 Avalonia 属性即时更新与 Core 下一重组帧的差异。
  Elevated disable 后首个 80ms 采样的阴影高度为 Core 1 / Avalonia 0 DIP；不能归入微小数值精度适配。
  Toggle 重叠按压取消时另观察到约 0.0000014 DIP 圆角差，结构化事实继续标为 different。
- 重叠波纹的部分 Compose 重复采集/额外被动截图有单通道差；Avalonia 对应自检稳定。
  因此这些场景未达到稳定双端验收，不修改帧、基线或容差规避。

- 焦点环的五种样式 × 五档尺寸 × 圆形/方圆角、快速反向、键盘按压固定/变形及 RTL 非对称悬停
  已有双端矩阵；任意自定义形状、尺寸和资源动态组合仍未经完整验证

- 既有记录中的属性变更当帧即时更新、替换 pressed shape 后状态层像素差异仍未解决，不能列为已接受偏离
- `dynamic.repress` 的 Compose 普通重复采集曾出现至多 1 个通道值的像素不稳定；保留为参考采集问题，
  不以放宽比较器或修改采样掩盖
- 既有 Button 回归未证明字体场景和真实桌面视觉体验的一致性；本次文档整理也没有补做这些验证
- 已有动画/布局回归覆盖不等于所有状态、DPI、字体和平台均通过；上游升级后旧结论不会自动升级

### 已接受的适配或差异

固定/变形形状的本地入口按上面的组件确认执行；Enter 激活时机、所记录的 Tween / Bezier 精度和
阴影坐标接入差异只在各自共享决定的范围内接受。其他失败不会因为执行了回归而获得授权。

### 可追踪的验证入口

`button-focus` 的 70 例、720 个采样已完成（`20261105-042758-7e58b61a`）：48 equal、
19 withinTolerance、3 different；执行错误为零，双端重复/额外截图均稳定，所有截图在现有门禁内。
三个 different 是 Filled / Tonal / Elevated 的焦点与悬停组合中 shadowElevation 的精确数值尾差，
最大约 `5.01e-7` DIP；没有舍入读数或调整容差。该矩阵显式使用相同的 Secondary / OnSecondary
及 Expressive MotionScheme。RTL 非对称 Outlined 的原描边翻转已修复；历史失败产物保留。

```powershell
./tests/VisualParity/parity.ps1 self-test --family button-focus
```

交互源接入后的既有 `button` 191 例复测完成：132 equal、35 withinTolerance、24 different，
执行错误为零；190 例自检稳定，另 1 例参考捕获不稳定。属性变更当帧、pressed shape 替换、
Enter 和高度精度等残余仍按上面的边界保留，不从执行完成推导整族一致。

| 范围 | 仓库内入口 |
| --- | --- |
| 默认主题、变体与 Token | [ButtonConfigurationTests](../../../../tests/Material3Expressive.Tests/ButtonConfigurationTests.cs)（普通 Style 优先级、清除恢复、实时布局 Token、完整 elevation 和模板动效配置）、[ButtonThemeTests](../../../../tests/Material3Expressive.Tests/ButtonThemeTests.cs)、[ButtonVariantThemeTests](../../../../tests/Material3Expressive.Tests/ButtonVariantThemeTests.cs)、[ButtonSizesTokenTests](../../../../tests/Material3Expressive.Tests/ButtonSizesTokenTests.cs)、[ButtonStyleVariantTokenTests](../../../../tests/Material3Expressive.Tests/ButtonStyleVariantTokenTests.cs) |
| 布局与字符串模板 | [ButtonLayoutTests](../../../../tests/Material3Expressive.Tests/ButtonLayoutTests.cs)、[ButtonTextLayoutTests](../../../../tests/Material3Expressive.Tests/ButtonTextLayoutTests.cs) |
| 原生输入、形状与高度生命周期 | [ButtonInputTests](../../../../tests/Material3Expressive.Tests/ButtonInputTests.cs)、[ButtonShapeApiTests](../../../../tests/Material3Expressive.Tests/ButtonShapeApiTests.cs)、[ButtonElevationTests](../../../../tests/Material3Expressive.Tests/ButtonElevationTests.cs) |
| 双端场景与动态采样 | [Avalonia ButtonCases](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ButtonCases.cs)、[Compose ButtonCases](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ButtonCases.kt)、[ButtonTimelineTests](../../../../tests/VisualParity/Material3Expressive.Parity.Tests/Cases/ButtonTimelineTests.cs) |

运行方式与如何阅读结果见 [VisualParity](../../../../tests/VisualParity/README.md)。
新增或修正用例遵循[用例编写规则](../../../../tests/VisualParity/AUTHORING.md)，分别报告源码对照、
单元/headless、双端视觉和桌面交互的实际执行情况；测试源码是复查入口，不是一次运行通过的证明。
