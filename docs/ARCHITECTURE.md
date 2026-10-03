# 架构与已有能力

本文描述当前源码中的结构、可复用能力与接入约束，不代表所有既有设计均已作为长期方案获批，
也不代表已经完成上游一致性验证。开发流程见[移植指南](PORTING.md)，参考仓库与读取方式见 [AGENTS.md](../AGENTS.md#上游源码)。
组件决定随各组件 README 保存，跨组件的共享能力决定见 [DECISIONS.md](DECISIONS.md)。

## 结构与职责

| 位置 | 当前职责 |
| --- | --- |
| [Components/](../src/Material3Expressive/Components) | Button、IconButton、Surface、Badge、BadgedBox、Chip、NavigationRail、WideNavigationRail、ModalWideNavigationRail、NavigationBar、ShortNavigationBar、Slider、CheckBox、RadioButton、Switch、Divider 的实现与主题，按组件组织。组件可以组合其他组件，例如 Chip / rail / bar / CheckBox / RadioButton / Switch 的模板使用 Surface。 |
| [Foundation/Tokens/](../src/Material3Expressive/Foundation/Tokens) | Token 身份、资源组合、引用解析与绑定。 |
| [Foundation/Typography/](../src/Material3Expressive/Foundation/Typography/README.md) | 聚合 TextStyle、角色绑定、默认值作用域与原生文字属性投影。 |
| [Foundation/Interaction/](../src/Material3Expressive/Foundation/Interaction) | 输入与交互反馈的接入、状态层、ripple 和圆角动画。 |
| [Foundation/Motion/](../src/Material3Expressive/Foundation/Motion) | Material 动画描述及其到 ComposeAnimation 的转换。 |
| [Foundation/Overlays/](../src/Material3Expressive/Foundation/Overlays/README.md) | 窗口内 Dialog 与共享 ModalPresenter；组件独立持有原生 Popup，使用 PopupOverlayLayer。 |
| [Foundation/Rendering/](../src/Material3Expressive/Foundation/Rendering) | 图标接入与颜色合成等辅助能力。 |
| [Tokens/](../src/Material3Expressive/Tokens) | `MaterialTokens` 的 Reference、System、Component 键；组件 Token 目前仍集中在这里。 |
| [Baseline/](../src/Material3Expressive/Baseline) | 颜色、形状、字体、状态、elevation、motion 的默认 Token 来源。 |
| [MaterialTheme.axaml](../src/Material3Expressive/MaterialTheme.axaml) | 装配默认 Token 来源并引入组件主题。 |

`MathUtils` 提供最近整数舍入、DIP 与物理像素转换和线性插值。`Components` 需要这些操作时优先使用对应方法。详见 [Foundation/MathUtils.cs](../src/Material3Expressive/Foundation/MathUtils.cs)。

这是目录职责，不是已经由程序集隔离的严格分层。目前 `MaterialInteractionController` 仍直接引用
`MaterialSurface` 和 `ButtonElevationController`；修改相关能力时需要检查组件调用方，不能假定 Foundation 与组件无关。
是否调整这些关系属于单独的设计问题。

## Token 与主题

当前 Token 由手工定义的 `MaterialTokenKey<T>`、`ITokenSource`、`MaterialResource` 和 `{m3:Token}` 组成，
没有接入 Token 生成器或 JSONC 目录。

- 键以对象引用为身份；名称用于诊断，不能通过重新构造同名键访问已有 Token。
- `MaterialResource` 组合多个来源，后加入的来源优先；来源的变更通知传入 Avalonia 资源系统。
- `{m3:Token}` 在消费元素处解析值。每次跟随 `TokenRef` 都从同一消费位置重新查找，
  因而局部覆盖中间的 System Token 也能影响 Component Token。
- 当前派生形式包括 `WithAlpha`、`SpringSpecRef`、`TextStyleRef` 和 `TextUnit`。解析有跳数限制；缺失、循环或超过限制时失败，
  绑定通过 `UnsetValue` 回退，不改用外层同名定义掩盖无效的最近条目。
- 资源和主题变化触发消费值更新。保留引用关系，不要在组件初始化时把动态 Token 提前展开为永久常量。

组件主题可以参考现有 [Button 主题](../src/Material3Expressive/Components/Button/Button.axaml) 的消费方式：

```xml
<Setter Property="Background"
        Value="{m3:Token {x:Static m3:MaterialTokens+Comp+FilledButton.ContainerColor}}" />
```

这里的 `m3` 对应 `https://github.com/Shirasagi0012/Material3Expressive.Avalonia`。
嵌套类型在 XAML 中用 `+` 表达。示例展示当前接入方式，不替代新组件对上游 Token 的调查。

## 排版

普通 TextBlock 使用 `MaterialText.Style="{m3:Typography TitleLarge}"` 选择聚合角色；
容器通过 `MaterialText.DefaultStyle` 逐字段合并默认值。明确指定的 Style 不隐式合并环境，
未指定字段不产生原生 Setter。原生局部值与绑定保留，由 Avalonia 属性系统决定最终值。
投影样式位于控件自身 Styles 开头，具体就近规则与迁移影响见 [Typography](../src/Material3Expressive/Foundation/Typography/README.md)。

TextStyle 只保存 DIP；sp/em 在 TokenResolver 消费位置换算。角色基线派生自现有五个标量键，
覆盖整套角色后不再读取这些标量。Button 消费各尺寸 LabelTextStyle 并向内容提供默认值；
NavigationRail 标签消费 LabelMedium；WideNavigationRailItem 按目标位置和动画进度切换 LabelMedium / LabelLarge。
没有引入公开 MaterialTextBlock，也未替换 Avalonia 排版器。

## 颜色、图标与阴影

| 能力 | 本地入口与边界 |
| --- | --- |
| 颜色计算 | [MaterialColorUtilities](../modules/MaterialColorUtilities/README.md) 提供颜色算法；[ColorScheme](../src/Material3Expressive/Baseline/ColorScheme.cs) 将其结果接入本项目 Token。颜色角色是算法计算结果，局部覆盖输出的 reference palette 不会反向重新生成角色。 |
| 可变字体图标 | [MaterialSymbols.Avalonia](../modules/MaterialSymbols/MaterialSymbols.Avalonia/README.md) 提供 fill、weight、grade、optical size 等轴；[MaterialSymbol](../src/Material3Expressive/Foundation/Rendering/MaterialSymbol.cs) 是本项目入口，当前使用 Rounded 字体包。组件消费已有渲染器，不另行实现字体轮廓绘制。 |
| 阴影 | [MaterialSkiaShadow](../modules/MaterialSkiaShadow/README.md) 提供与 Compose Desktop/Skiko 对应的 Skia 阴影绘制；Surface 负责接入实际几何、缩放及绘制环境。模块目前支持单个平面简单轮廓的线段、二次曲线与 conic，不支持 cubic、透视、倾斜 Z 平面、方向光和非 Skia 后端。 |

MaterialSymbols 需要的 SkiaSharp 托管与原生包由中央包版本统一。具体依赖来源以项目文件和 NuGet 配置为准。
配套模块有各自的上游版本与限制；模块级的移植或渲染验证不自动证明本项目的组件集成正确。
遇到能力缺口时说明缺少的语义，不在组件中悄悄替换为近似算法。

## Surface 与交互组合

[MaterialSurface](../src/Material3Expressive/Components/Surface/MaterialSurface.cs) 是可被其他组件组合的容器组件，
承担背景、边框、裁剪、阴影和累积 tonal elevation。内部 Border 承载内容和裁剪，独立覆盖层绘制交互反馈。
几何使用 Avalonia DIP，Full 圆角按当前边界解析。
描边由末层 SurfaceOutline 绘制，不参与内容尺寸计算；均匀线宽保留 Compose 的物理像素 ceil，
均匀圆角使用内缩 stroke，非对称圆角使用外轮廓减内缩轮廓。非均匀 Thickness 沿用原生 Border。
描边动画提供连续线宽与原始外圆角，取整和内缩只由 SurfaceOutline 执行；普通零宽无描边，
动画零宽使用 hairline。Padding 仍由内容 Border 处理。

Button 模板内的局部 `ButtonLayout` 区分背景默认尺寸与外层交互留白：背景默认最小宽度为 58 DIP，
高度由 size Token 提供，外层默认预留至少 48 DIP 并居中放置背景。默认背景高度不再写入
`Button.MinHeight`；显式 Width/Height 或 MinWidth/MinHeight 仍由调用方设置，并覆盖相应默认值。
它移植 `defaultMinSize` 与 `minimumInteractiveComponentSize` 的布局职责，不实现触屏目标扩展。
自动字符串标签保持 Compose 的受约束段落宽度；普通裁剪保留完整文本布局，再在有限标签边界内绘制。
省略号仍使用有限高度格式化。显式内容控件、ContentTemplate 和匹配的 DataTemplate 保留优先级。

Button 的 `ContentPadding` 是本地复合槽，依据固定上游 `ButtonDefaults.contentPaddingFor` 的默认参数，
并非原始 Token：XS/S/M/L/XL 的水平/垂直值分别为 12/6、16/10、24/16、48/32、64/48 DIP。
基线使用上游默认的 `shouldUsePrecisionPointerComponentSizing=false`；启用该分支时 Small 垂直值为 8，
图标侧水平值为 12，当前不可变基线未实现这一分支。调用方可显式覆盖 Padding 或对应复合槽。

目前交互组件通过 `MaterialInteraction.IsInteractive` 接入共享控制器，Surface 作为模板中的反馈呈现者。
控制器处理输入、交互顺序、状态层和 ripple；主题提供颜色和形状等参数。
键盘输入只用于维护交互反馈，不由共享控制器拦截 Enter 或执行点击。
激活由控件自身负责，具体语义见各组件 README，已有导航项的键盘处理保留。
决定依据与适用范围见[共享交互反馈与键盘激活职责](DECISIONS.md#共享交互反馈与键盘激活职责)。
颜色、交互事实、反馈动画和绘制结果需要保持各自归属，不要仅用最终伪类状态替代具有顺序意义的交互记录。

Button / IconButton 的主题将 owner `CornerRadius` 传给 Surface，默认按压保持该形状。
可空附加属性 `MaterialButton.PressedCornerRadius` 显式启用按压变形，null 关闭；模板直接绑定该属性，
复用 Surface 的圆角状态和动画，不另建按钮形状控制器。尺寸 class 提供 owner 圆角的默认值，
局部 CornerRadius 和按压目标不被尺寸 class 覆盖。

[ToggleButton](../src/Material3Expressive/Components/ToggleButton/README.md) 使用原生状态及独立的四种主题，
在 Surface 内部增加选中形状输入，复用共享变形、描边动画、elevation、状态层和 ripple。
[SplitButton](../src/Material3Expressive/Components/SplitButton/README.md) 使用独立双槽容器，组合实际子控件；
内部 ButtonLayout 的最小宽度及交互留白可配置，普通 Button 的默认值不变。
这两个组件的公开 API、尺寸规则、配置重建差别与原生语义决定随组件记录。

NavigationRailItem 复用这一控制器，通过内部 `PressPositionMapper` 翻译固定上游的
`MappedInteractionSource` 坐标；状态层和 ripple 不另建实现。键盘原点与当前固定 Desktop
`AbstractClickableNode` 一致：初始为零，收到指针事件后缓存控件中心。禁用会取消交互并让既有
ripple 完成进入与退出；禁用引起的状态层退出在重组对应帧之后启动。
Rail 的布局、内容色匹配与 item 的四条动画轨迹保留在[组件目录](../src/Material3Expressive/Components/NavigationRail/README.md)。
NavigationBarItem 使用同一按压映射入口，按实际 Item 宽度和固定上游偏移映射至胶囊。
Rail / Wide / Modal / Bar 的动态 contentColorFor 绑定及物理 Insets 转换位于 Foundation/Rendering，
各组件仍负责选择背景属性、内容环境和布局。ShortNavigationBar 使用独立的 intrinsic 查询与 EqualWeight / Centered 布局；其 NavigationItem 只有指示器动画，
不扩展 Wide 的位置动画和平滑适配。见 [组件说明](../src/Material3Expressive/Components/ShortNavigationBar/README.md)。
NavigationBar 的等权布局与动画见
[组件说明](../src/Material3Expressive/Components/NavigationBar/README.md)。
contentColorFor 绑定及物理 Insets 的共享迁移于 2026-10-03 随 NavigationBar 方案确认，原有公共入口与行为不变。

Button elevation 的当前实现可作为职责示例：`ButtonElevation` 保存状态高度，
每个按钮自己的 `ButtonElevationController` 选择并动画化目标；Surface 的 `ShadowElevation` 消费当前绘制高度。
可共享配置，但不能共享按钮的运行状态。动画优先级绑定在清理后恢复原属性值，不能破坏原有样式与绑定。
Filled/Tonal 默认在 hover 升到 1 DIP；Outlined/Text 默认无 elevation 配置。
圆角与 elevation 的交互目标在下一共享时钟帧合并提交，对应 Compose 重组后的 effect；
状态层和 ripple 仍遵循各自的事件时序。圆角配置或 effects spring 改变时重建形状状态，
更换目标先取消旧 effect，再沿用状态。待提交目标和 effect 都受呈现者的取消生命周期约束。
这一示例不意味着其他组件都应采用 Button 的状态选择规则。

FAB 使用自己的 `FloatingActionButtonElevationController`：保留最后一个实际交互，
即使 press/focus/default 高度相同也不丢失状态身份；配置变更先 snap 当前目标。
它通过同一 `MaterialInteractionController` 和呈现者作用域接入，Surface 分别消费固定 tonal elevation
与动画 shadow elevation。尺寸入口、受控 Toggle 与菜单布局见 [FAB 说明](../src/Material3Expressive/Components/FloatingActionButton/README.md)。

Chip 使用自己的六状态 elevation 配置，消费共享控制器的最后活跃交互；形状和槽动画留在
[组件目录](../src/Material3Expressive/Components/Chip/README.md)，不使用 Button 的 elevation 配置。
共享控制器观察 Core Desktop 的所有原生焦点，指针按下位于原生焦点处理之后。
状态层的焦点源按 Desktop 输入模式单独过滤，elevation 保留完整焦点事实。
无指针历史的键盘 ripple 从 clickable 局部零点开始；导航项保留其源坐标映射。
ContentColorFor 的可选内部 ambient 绑定用于 Chip 在标签 provider 外选择墨色，仍消费同一套
实时背景 / Token / 环境绑定。相关确认依据见 [DECISIONS](DECISIONS.md)。

SecureTextField 的原生 Filled / Outlined 主题复用 TextField 装饰，
`MaterialSecureTextField.IsSecure` 负责单行、IME 提示和复制/剪切限制，显示仍由原生
PasswordChar / RevealPassword 控制。该原生方案未替换 automation peer 或 IME 客户端；
安全语义缺口和已确认的 RevealLastTyped 暂缺见[组件说明](../src/Material3Expressive/Components/SecureTextField/README.md)。

## 动画接入

动画执行通过 NuGet 依赖 ComposeAnimation，准备方式见 [README](../README.md#本地开发)。
Material 层仍负责 Token、目标选择、时序规则及绘制；不要在组件中另建动画求解器或逐帧驱动循环。

每个应用 TopLevel 在加载 XAML 或内容前调用 `VisualAnimationScope.Initialize(topLevel)`。
`VisualAnimationScope.Create(owner)` 为已附着的控件建立作用域；Ripple、Shape 等效果拥有各自的子作用域。
使用作用域的任务、订阅和动画值绑定来管理执行与清理。停止单个效果不应结束父作用域或兄弟效果；
detach、模板替换与重新附着需要处理旧作用域及绑定的生命周期。根帧源固定，运行中不切换时钟；根的
`AnimationFailed` / `LastError` 用于诊断。

现有 Material 动画入口：

- `SpringSpecRef` 在消费处解析 damping/stiffness，`SpringSpec.ToAnimationSpec()` 转换为运行时 spring。
  阈值属于所动画化标量的单位，例如圆角进度使用 0.01，不能把它当作所有几何的通用阈值。
- `AnimatedCornerRadius` 接受父作用域，拥有子作用域，实现 `IDisposable`；通过 `AnimateToAsync` 改变目标。
  一段运行中的形变保留捕获的 spec，下次目标变化再解析当前 Token。回调必须在端点与进度一致后发布，
  反向或第三目标切换也不能暴露暂时不一致的形状。
- `RippleAnimation` 同样拥有子作用域并可释放；启动 `AnimateAsync`，释放按压时调用 `Finish`。
  调用方需要观察任务并处理正常中断取消，取消和释放应清除等待。两类效果都不暴露手工 `Tick`。

## 当前覆盖与待确认项

现有源码是后续工作的起点，不是批准所有既有取舍的依据。旧注释中的 `design doc §...` 是历史引用，
不指向本文，也不构成当前规范；涉及相应代码时核对真实行为与固定上游。

- 已有 Button、IconButton 和 Surface，但仍需逐项检查其 API、交互及视觉覆盖。
  Button 的焦点环尚未实现；阴影绘制差异留待后续核对，不能据此修改上游默认 elevation。
- [CheckBox](../src/Material3Expressive/Components/CheckBox/README.md) 使用原生 Avalonia CheckBox 的模板，
  合并两态与三态外观；CheckBoxAppearance 提供颜色与描边，内部模板部件负责布局和 Skia 绘制。
  原生状态切换与键盘差异已确认保留，具体覆盖与未实现分支见组件说明。
- [RadioButton](../src/Material3Expressive/Components/RadioButton/README.md) 使用原生 Avalonia RadioButton 的模板，
  保留已确认的原生状态/组互斥、键盘与内容组合；四槽颜色、圆点动画、布局与绘制由模板部件负责。
  默认 ripple/focus 复用共享交互能力，具体边界见组件说明。
- [Switch](../src/Material3Expressive/Components/Switch/README.md) 使用原生 ToggleSwitch 的模板和
  SwitchAppearance 附加属性，保留原生状态写回、键盘、拖动与自动化；模板部件移植 ThumbNode 布局与动画。
  默认 ripple/focus 配置已实现，null 回调模式、可配置 inset focus ring 和外部 InteractionSource 映射未实现。
- [Divider](../src/Material3Expressive/Components/Divider/README.md) 使用原生 Separator 的主题，
  通过附加属性选择 Horizontal / Vertical 方向、厚度与颜色；内部 Canvas 保留布局取整、
  原始描边厚度、零布局 hairline 和约束行为，不提供已废弃的 Divider 入口。
- 已有常规 [NavigationRail / NavigationRailItem](../src/Material3Expressive/Components/NavigationRail/README.md) 和
  [WideNavigationRail / ModalWideNavigationRail / WideNavigationRailItem](../src/Material3Expressive/Components/WideNavigationRail/README.md)。
  Modal 使用当前 TopLevel 的 PopupOverlayLayer，通过组件自己的 Popup 和共享 ModalPresenter 接入。
  自定义 Window 模板需包含 `PART_VisualLayerManager`；普通 Content 内的 OverlayLayer 不满足该条件。
  Dialog 的关闭请求、退出保留和焦点约束见 [Overlays](../src/Material3Expressive/Foundation/Overlays/README.md)。
  组件负责输入阻挡、焦点限制/恢复及展开内容迁移；各组件的验证范围与保留问题见对应说明。
- 原有 Material 动画规则的源码核对基于 AndroidX `ff0a0a99c4f0cfe95d75d5df615e51d399575922`。
  当时记录了状态层 15/45/150ms 淡入淡出、ripple 75ms 进入透明度与 225ms 扩张/中心移动、
  释放后的 150ms 淡出；快速点击仍完成扩张。按压圆角使用 DefaultEffects（damping 1、stiffness 1600）。
  Elevation 进入为 120ms FastOutSlowIn，退出为 150ms（hover 120ms）Bezier(0.4, 0, 0.6, 1)，
  初始和禁用状态直接到目标。这些是原基线记录，不能据此宣称已完成对当前目标的全面对齐。
- [基线核验记录](UPSTREAM_BASELINE.md)保存了版本间的部分源码差异，协调版本时可查阅；ComposeAnimation 的新基线协调仍待完成。
  ripple 使用 common/desktop 路径，Android 原生 RippleDrawable 是另一个参考路径。
- 当前有 Token、主题、交互、生命周期与渲染等测试；[动画生命周期测试](../tests/Material3Expressive.Tests/AnimationLifetimeTests.cs)
  和 [motion 测试](../tests/Material3Expressive.Tests/MotionTests.cs)可作为已有检查入口。
  TestSupport 的 `AdvanceBy` 每次发送一个帧，不插入起始帧；跟踪任务的异常应在推进和释放时暴露。
  测试存在不代表本轮已运行，也不代表 Compose 视觉对比或真实桌面交互已经验收。
- [视觉一致性系统](../tests/VisualParity/README.md)使用独立的双端用例入口，作者显式构建场景、推进时钟和提交测量快照。
  每个组件通过独立测试类与 suite/scenario 标记自动发现，C# 数据源定义具体用例。
  prepare-reference 在单 JVM 中批量准备并发布参考清单；run 固定参考身份、重新顺序采集 Avalonia，不启动 JVM。
  身份和具体 config 冻结为 JSON，构建二进制按内容固定；每次捕获使用新测试类实例和宿主。
  采集证据与比较分离，recompare 可只重算既有结果。运行进度追加记录，结束时一次性生成完整索引。
  不维护手写用例 JSON，不定义通用场景或输入语言。Agent 读取用例概览和逐采样点详情，开发者可使用 HTML 报告。
  可选宿主将动画帧、框架绘制和被动捕获分开；系统重复性与组件一致性分别验证，不使用容差或文字遮罩消除差异。
  [用例编写](../tests/VisualParity/AUTHORING.md)说明接入与执行边界；[测量语义](../tests/VisualParity/MEASUREMENTS.md)说明事实与比较含义。
  全新克隆与 CI 的依赖限制见 [README](../README.md#本地开发)。
