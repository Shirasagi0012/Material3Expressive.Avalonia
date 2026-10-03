# 已确认的设计决定

## ShortNavigationBar 的内容、排列与测量适配

决定：ShortNavigationBar 使用 ItemsControl 的 Items / ItemsSource / ItemTemplate，提供 EqualWeight / Centered
两种 ShortNavigationBarArrangement、Background / Foreground 和物理 WindowInsets（桌面默认零）。
ShortNavigationBarItem 使用 Button 的 Click / Command / IsEnabled，提供 Icon / Label 及模板、外部 IsSelected、
非空的 NavigationItemIconPosition（默认 Top）以及与 Wide Item 同名的八项颜色属性。标签存在时始终显示；
点击仅请求导航。内容色直接更新，只有指示器使用 DefaultSpatial；位置直接切换，不复用 Wide 的位置动画或纵向平滑偏离。
默认 Foreground 依据上游默认容器的 SurfaceContainer 角色；自定义 Background 不改变默认内容色。
原因：Short 使用 NavigationItem，而 Wide 使用 AnimatedNavigationItem，两条上游入口的动画及布局语义不同。
做法：组件内部测量内容槽的固有尺寸，并翻译上游两种 MeasurePolicy；其他任意 Avalonia 内容使用原生 Measure。
没有引入通用 intrinsic 框架；沿用现有动画作用域、交互、Inset 转换和 MathUtils。
范围：本次 ShortNavigationBar / ShortNavigationBarItem。
确认：2026-10-03，维护者确认“采用推荐 API 与测量适配”。示例见组件 README。

## ShortNavigationBar 的固定宽度语义

决定：保留 Avalonia 原生 Width 语义，固定 Bar 的容器宽度，各 Item 按排列分配宽度。
原因：固定 Compose 的 Surface 传播 minWidth；两种 Short MeasurePolicy 又用原始 constraints.constrain
约束每项。直接 Modifier.width(352.dp) 时三项各为 352 DIP 并溢出；本次稳定双端捕获证实这一行为。
正常 parity 场景由外层宿主提供有限最大宽度，Bar 不设置 width；另外保留直接 width 用例的真实 different。
不修改固定参考、容差或归类规则。C# `new ShortNavigationBar { Width = 352 }` / XAML `Width="352"` 正常分配。
范围：ShortNavigationBar 的容器 Width；不改写 Centered / EqualWeight 的整数算法及其他组件。
确认：2026-10-03，维护者确认“采用原生 Width 语义并记录差异（推荐）”。证据见组件 README。

## CheckBox 合并两态/三态并保留原生状态与键盘

决定：为原生 Avalonia.Controls.CheckBox 提供隐式 ControlTheme，不继承、不新增两个状态控件。
IsChecked(bool?)、IsThreeState、双向绑定、Click/Command、键盘及自动化由原生实现持有；
CheckBoxAppearance 的附加颜色/描边配置与内部模板部件移植固定上游的外观、布局和动画。
原因：原生 CheckBox 已有两态/三态，模板能复用其状态和交互入口，避免相似控件与转发 API。
影响：点击自动写回状态；三态按 false → true → null → false 循环，Click/Command 观察到更新后的状态；
Enter 在 KeyDown 激活、Space 在 KeyUp 激活，与 Compose 外部写回、三态仅请求点击、Enter KeyUp 有区别。
Content/ContentTemplate 在指示器旁组合原生内容，不修改指示器画布。
范围：本次 CheckBox 两态/三态模板；不以此批准其他视觉或语义偏离。
确认：2026-10-03，维护者要求合并为原生 CheckBox、考虑仅替换模板，并明确
“这两方面保留原生状态切换和键盘行为即可。按这个方案实施”。使用方式见组件 README。

## CheckBox 的 null 回调模式暂不提供等价实现

决定：本次交付原生可交互两态/三态的 Material 模板，明确记录 Compose onClick=null 模式未实现。
原因：原生 CheckBox 没有移除 toggleable 自动化语义的开关，完全等价需要独立只显示指示器，
不能仅替换现有 CheckBox 模板完成。
做法与影响：应用可设置 IsHitTestVisible=false、Focusable=false、MaterialInteraction.IsInteractive=false
阻止普通用户输入与 Material 反馈，但原生 ToggleProvider 仍可改变状态，不视为 null 回调的等价移植。
范围：本次 CheckBox 交付；不新增独立只显示指示器，不改变已有原生自动化行为。
确认：2026-10-03，维护者选择“本次保留原生范围，记录 null 回调模式未实现（推荐）”。

## CheckBox 的描边连接始终消费实际参数

决定：Join=Round 始终画出圆角，按调用者的 Cap、Join、MiterLimit 设置实际 Skia Paint。
原因：固定 Core 的 SkiaBackedPaint 将 strokeJoin 的缓存初值设为 Round，底层 Skia 初值却为 Miter；
CanvasDrawScope.selectPaint 比较缓存后跳过首次 Round 的赋值，使请求圆角的勾号实际画成尖角。
固定依赖的独立 Paint 诊断确认了缓存 Round / native Miter，checkbox.static.stroke.round.on 的差异位于勾号转角。
做法与影响：保留正确的圆角参数含义，与桌面基线的首次 Round 绘制保留视觉差异；
默认 Miter 描边不受影响，双端用例继续使用真实组件并报告 different。
范围：本次 CheckBox 自定义描边；不扩展为其他上游 bug 的通用处理规则。
确认：2026-10-03，维护者明确“这是上游bug，始终按参数画出圆角”。

## NavigationBar 的内容、选择与共享环境适配

决定：NavigationBar 使用 ItemsControl 的 Items / ItemsSource / ItemTemplate，内置等宽布局和 8 DIP 间距，
提供 Background / Foreground、TonalElevation（默认 Level0）和物理 WindowInsets（Thickness，桌面默认零）。
NavigationBarItem 使用 Button 的 Click / Command / IsEnabled，提供 Icon / Label 及模板、外部 IsSelected、
AlwaysShowLabel 与常规 Rail 同名的七项状态颜色。点击仅请求导航，应用维持选中状态。
原因：保留 Compose RowScope 内容和 selectable 外部选择语义，同时支持 C# / XAML。
做法：现有 contentColorFor 绑定和物理 Insets 转换移至 Foundation，Rail / Wide / Modal / Bar 共享，
原有公共入口与行为不变。按压映射复用既有控制器，使用实际 Item 宽度与 Token 宽度的差及固定 12 DIP 纵向偏移。
范围：常规 NavigationBar / NavigationBarItem；不决定 ShortNavigationBar 的入口，不扩展 Wide 的纵向平滑偏离。
确认：2026-10-03，维护者确认“采用推荐 API 与内部共享适配”。使用示例见 NavigationBar 的组件 README。

## RangeSlider 重合时按首次拖动方向选择 Thumb

决定：按下时 ActiveRangeStart == ActiveRangeEnd 的手势，在首次有效水平拖动时按数值方向选起点或终点，随后固定该选择。
原因：固定上游 Slider.kt 的 rangeSliderPressDragModifier 在按下时选定 Thumb；重合时只能向被选端允许的方向拉开，难以操作。
做法与影响：减小数值选起点，增大数值选终点；RTL 换算方向。转移按压反馈，保留非交叉约束、slop、取消和完成回调。
仅在释放事件出现水平位移的手势同样按方向选择；没有拖动的点击和非重合手势保留上游规则。
范围：RangeSlider 的指针手势，不新增 API；拖动中才合并的区间不切换 Thumb，键盘和自动化不改变。
验证保留真实固定 Compose 参考，重合方向用例的行为和视觉差异不豁免或改写比较规则。
确认：2026-10-03，维护者回复“就按你说的改”，授权采用首次有效水平拖动方向方案。

## Slider 的数值入口、State 与内容模板

决定：提供 Slider、VerticalSlider、RangeSlider，默认使用双向绑定数值属性（Value 或 RangeStart/RangeEnd），
另提供可选 SliderState / RangeSliderState；显式 State 拥有数值、范围及 steps。
Thumb/Track 支持内容和 DataTemplate，range 两端各自提供 Thumb 槽，模板接收实际 State。
原因：同时支持简洁的 C#/XAML 绑定、上游 hoisted state 与自定义内容组合，避免要求所有应用先建立 State。
做法与影响：便捷属性内部持有 State；显式 State 的回调保留调用者写回职责；默认模板、布局与绘制消费相同状态。
范围：Slider 家族新增 API；不改变其他组件或 Avalonia 原有 Slider。
确认：2026-10-03，维护者对带 C#/XAML 示例的推荐方案回复“采用推荐方案”。

## Slider 可选 InsetRing 焦点模式延期

决定：本次交付上游默认焦点模式，明确保留 InsetRing 未实现；不补齐共享 RippleTheme 焦点配置。
原因：固定基线的可选 InsetRing 会影响 Thumb 测量、padding 和 Track 间距，现有共享 Ripple 能力没有对应配置。
影响：默认按压/拖动/聚焦的 Thumb 缩窄、独立 range 焦点与自动化仍实现并验证；
不能据此声明全部 LocalRippleThemeConfiguration 分支已移植。
范围：本次 Slider、VerticalSlider、RangeSlider；后续共享焦点配置任务需补齐并验证该分支。
确认：2026-10-03，维护者回复“本次只交付默认焦点模式，明确记录 InsetRing 未实现”。

## Slider 禁用中断保留 Avalonia 即时绘制

决定：拖动中禁用时，当帧取消交互并恢复默认 Thumb 宽度，保留与 Compose 子组合重绘的一帧视觉差异。
原因：固定 Core 的 FrameRecomposer 先应用父组合的 enabled 更新，DragInteraction.Cancel 使 Thumb 的状态
在下一帧重组；同一 disabled 采样两端交互反馈已清空、数值与完成回调相同，Compose 位图仍为半宽 Thumb。
做法：不引入人为帧延迟，不修改参考、采样、容差或门禁；保留四个 horizontal/vertical disable 用例的真实差异。
范围：Slider 与 VerticalSlider 的拖动禁用瞬间；后续帧、RangeSlider、一般按压释放和键盘不据此豁免。
确认：2026-10-03，维护者对含证据和两种方案的问题回复“保留即时绘制并记录差异（推荐）”。

## Modal rail 鼠标拖动速度保留 Avalonia 原生事件历史

决定：使用 Avalonia 实际 PointerMoved 历史与固定 Compose Desktop 的 LSQ2 算法，不在组件或夹具中复制速度样本。
原因：固定 Core SyntheticEventSender 在布局后以原坐标、原时间戳重发鼠标 Move，零位移也参与拟合；
Avalonia 没有对应路径。反向快速拖动样例的速度为 Compose 1071.429、Avalonia 749.999 DIP/s。
范围：ModalWideNavigationRail 的鼠标拖动释放轨迹；保留不同轨迹的 parity 证据，不修改目标选择算法或容差。
确认：2026-10-03，维护者确认保留真实 PointerMoved 历史与相同 LSQ2 算法，并在组件 README 记录。

## Modal Wide NavigationRail 的 OverlayLayer 宿主与独立展开 Header

决定：在当前 TopLevel 的 OverlayLayer 中显示组件自有浮层，覆盖客户区、阻挡背景输入、限制并恢复焦点；
保留 HideOnCollapse 的常驻收起态与滑出隐藏两条路径、拖动关闭和固定上游的 Escape KeyUp 行为。
Header / HeaderTemplate 用于收起态；ExpandedHeader / ExpandedHeaderTemplate 用于展开态。
数据与模板可生成两份 Header，直接传入 Control 时分别提供两个实例。
原因：固定 Compose Desktop 使用 Dialog；Avalonia 的同一个 Control 无法同时属于两个父节点。
范围：Wide/Modal 宿主适配，不提供 Android predictive back；默认颜色、形状及动画仍来自固定 Compose。
确认：2026-10-03，维护者确认“采用 OverlayLayer 与独立 ExpandedHeader（推荐）”。

## Wide item 的 float alpha 合成能力暂缓补齐

决定：暂留当前 Indicator 的 SaveLayer 合成差异，不新增绘制基础能力，不视为已经对齐。
依据：固定 Skiko `0.152.0-alpha02` 的 ModulateAlpha 使用每个 paint 的 float alpha 乘法；
Avalonia 12.1.2 的普通 Opacity 在画刷消费处截断到 byte，RequiresFullOpacityHandling 使用 SaveLayer。
独立 Skia 复现与 Wide Top item 退选 64ms 对应：Compose `#EEE7FA`，当前 SaveLayer `#EEE6F9`。
范围：Wide item 选择/退选过程的 Indicator 混色；保留双端失败用例与复现，后续再决定内部绘制器。
确认：2026-10-03，维护者选择“暂留差异，之后处理”。
补充：同日维护者正在调整 visual parity 容差，以容忍大片每通道差不超过 1 的色块，
并明确无需为此追加测试。保留合成路径差异与已有证据；旧版本容差下的失败不推断为新门限下仍失败。

## Wide NavigationRail 的状态、内容槽和排列 API

决定：WideNavigationRail / ModalWideNavigationRail 使用 ItemsControl 的内容与模板入口，
共享可绑定 WideNavigationRailState；构造值为 Collapsed / Expanded，公开只读 TargetValue、CurrentValue、
IsAnimating 和 ExpandAsync / CollapseAsync / ToggleAsync / SnapToAsync。动画方法在挂载后调用，
卸载取消运行；重新挂载从保存的目标恢复。多个仍挂载的消费者可共享同一个状态。
Rail 提供 Header / HeaderTemplate、WindowInsets、Background / Foreground、ContentPadding、CornerRadius
及六种标准 Arrangement 和 SpacedBy。WideNavigationRailItem 通过 RailExpanded 默认跟随所属 rail，
允许覆盖 Top / Start IconPosition，以及固定或展开/收起两套 IndicatorPadding；选中标签颜色分为 Top / Start。
Icon / Label 内容与模板、外部 IsSelected、Click / Command 沿用已确认的内容槽及 selectable 适配。
原因：保留上游 hoisted state、AnimatedNavigationItem 和 Arrangement 的职责与分支，支持 C# / XAML 使用。
范围：Wide/Modal 的上述公共入口；Modal 的窗口宿主及双 Header 见单独决定。
确认：2026-10-03，维护者确认“采用推荐 API”。

## 聚合 TextStyle 与 Token 层文字单位换算

决定：通过不可变 TextStyle、MaterialText.Style/DefaultStyle、Typography 角色绑定及派生 XAML 扩展，
让普通 TextBlock 消费聚合排版；不要求公开 MaterialTextBlock。DefaultStyle 合并环境，Style 显式选择。
角色保留标量来源并增加聚合 Style Token；Button/Rail 的标签接入同一默认值机制。
sp/em 在 Token 消费位置换算为 DIP，保持资源覆盖与字体缩放的动态性，TextStyle 本身只保存 DIP。
原因：保持 Compose 的样式选择、合并与提供默认值语义，同时通过 Avalonia 属性优先级保留原生覆盖。
范围：聚合 API 与当前可投影字段；未授权用近似行为补齐 LineHeightStyle 等底层排版缺口。
具体使用方式、原生样式就近规则、旧 Button Component 标量槽迁移及未实现项见
[Typography 说明](../src/Material3Expressive/Foundation/Typography/README.md)。
确认：2026-10-03，维护者认可 API 方案并要求落地，同时提出在 Token 层完成 sp/em → DIP 换算。

## ComposeAnimation 颜色转换与原始动画终值

决定：在相邻 ComposeAnimation 修正现有 ColorConverter，保留公开 API 与 double 向量接口，
按固定 Compose 的 sRGB / XYZ / Oklab 路径保留矩阵、Float 边界、两次 Half / 10 位 alpha 存储及舍入。
TargetBasedAnimation 保留原始起点和目标，结束时返回原目标，避免有损颜色往返改变终值。
原因：固定上游 Color.VectorConverter 调用 ui-graphics Color.convert，其中间存储会改变动画颜色；
Rail 反向 65ms 的 R=46/47 已由独立数值复现定位。范围为现有 byte sRGB 颜色 API，未决定扩展 ColorSpace API，
也未授权全局改变 core 的 double 精度或亚毫秒处理；若仍有相关差异，另行提出具体方案。
接入：依照现有本地 NuGet 流程打包 Runtime / Avalonia / TestSupport，版本统一为 `0.2.1-alpha`。
确认：2026-10-03，维护者批准修复方案，并指定“打包版本用0.2.1-alpha”。

## 常规 NavigationRail 的 Avalonia 入口与按压映射

决定：Rail 使用 ItemsControl，提供 Header / HeaderTemplate、物理 WindowInsets（Thickness，Desktop 默认零）
和 Background / Foreground；Item 继承 Button 的 Click / Command，提供 Icon / IconTemplate、Label / LabelTemplate、
IsSelected、AlwaysShowLabel 与七项状态颜色。选中状态由应用持有，点击只请求导航。
原因：把 Compose 内容 lambda 转换为 Avalonia 内容槽与模板，并保留上游 selectable 的外部选中语义。
做法：复用共享交互控制器，增加内部 PressPositionMapper，按固定上游 token 差值映射 `(12, 0)`；
不根据实际指示器位置反算，也不新增 ripple 实现。尺寸及动画计算继续按固定源码翻译。
适用范围：常规 NavigationRail / NavigationRailItem；不决定 Wide 或 ModalWide 的 API，也不新增应用导航框架。
确认：2026-10-03，维护者确认“采用推荐 API 与内部适配”；调用示例见[组件说明](../src/Material3Expressive/Components/NavigationRail/README.md)。

## Raster 降采样容差试用

决定：保留固定 4× 捕获和原始精确差分，另以固定 4×4 sRGBA8 块平均得到 1× 比较数据。
整图与每个非空配对点布局区域都要求最大通道差 ≤20、平均逐像素最大通道差 ≤1（0～255）。
原始不同但接受的结果单列 withinTolerance，不改称 equal；结构化事实及重复性自检仍精确比较。
公开结果升级为 schema 8，报告显示原始与容差证据；--require-match 使用容差，--require-equal 保持严格。
原因：允许试用排除渲染器抗锯齿造成的边缘微差，并以区域均值限制大面积偏色。阈值尚待更多组件验证。
适用范围：tests/VisualParity Raster 比较与报告；替代较早决定中 Raster 不使用容差的限制，不修改生产组件。
具体算法、透明通道及阴影区域限制见 [MEASUREMENTS](../tests/VisualParity/MEASUREMENTS.md#raster-容差判定)。
确认：2026-10-03，维护者认可 4× 渲染、1× 比较方案，先试最大差 20、区域平均 0.2；
在 NavigationRail 混色差异验证后，明确将区域平均上限改为 1，最大差仍为 20，不增加逐像素忽略规则。

## 视觉一致性结果的文件阅读结构

决定：公开结果使用两层阅读结构：Markdown 用例概览、自包含的逐采样点 YAML；运行时直接输出概览。
Agent 使用文件系统和通用文本/数据工具读取，不再依赖 inspect 查询接口或完整 agent.json。
原因：原概览和多份整包 JSON 重复数据、占用上下文；多级查询增加往返成本。
做法：公共描述和值合并，但保留双端来源、语义差异、原值和不可用原因。结果 schema 3 与内部采集 schema 2 分开。
HTML 从公开结果回读生成；原始采集、自检重复包保存在独立缓存。概览计数不代表完成百分比，精确比较语义不变。
适用范围：tests/VisualParity 的结果存储、Agent 阅读和报告生成；不改变生产组件或双端场景执行。
确认：2026-10-02，维护者确认“概览 + 单采样点详情”、文件系统读取及 Markdown/YAML 输出方案，并要求实施重构。

## 用例统一由 C# 定义并冻结配置

决定：C# 是用例定义的唯一来源；单独用例和矩阵都展开为具体 CaseMetadata，JSON 只作为运行产物。
原因：避免大量手写配置和重复场景代码，同时保留每个配置独立运行、定位和按需阅读的能力。
做法：用例作者使用普通循环/LINQ 和组件自己的配置类型；定义只生成数据。全部定义校验后筛选，
捕获前冻结选中用例的 JSON；双端读取同一 config，并各自通过真实 API 构建场景。配置纳入元数据哈希和结果。
移除手写 JSON 发现入口、重复的配置专用场景入口及旧 compare 转换命令，不保留旧格式兼容分支。
元数据升级为 schema 3，公开结果升级为 schema 4，采集协议仍为 schema 2；替代前一决定中的版本号，
两层阅读和精确比较语义不变。覆盖矩阵、采样密度与用例选择要求仍待讨论，不由本次迁移确立。
适用范围：tests/VisualParity 的定义、发现、配置传输、缓存身份和结果保存；不扩展生产组件能力。
确认：2026-10-02，维护者确认统一使用 C# 编写定义，配置放入生成的元数据 JSON，并要求落地且不保留兼容债。

## 独立测试类与场景自动发现

决定：每个组件维护独立的 XxxCases 类，以 suite/scenario 标记执行身份，静态 C# 数据源生成具体用例。
原因：中央 partial Cases、CaseCatalog 和手工入口表不适合持续增加组件、变体和场景。
做法：C# 只扫描明确的执行程序集，Compose 只扫描 producer 自己的 jar。先校验声明与展开数据，
再筛选并核对选中的双端场景，最后冻结 JSON 和捕获。每次执行创建新测试类实例，资源在场景内管理。
方法名不参与协议，具体用例 ID 保持稳定。删除中央定义/入口表和 composeEntry/avaloniaEntry，不保留别名。
元数据升级为 schema 4、公开结果 schema 5，采集协议仍为 schema 2；替代前述决定中的版本和组织方式，
C# 单一数据源、文件阅读结构和精确比较语义不变。批量复用 JVM 和覆盖规范不属于本次实施。
适用范围：tests/VisualParity 的用例组织、发现、配置身份与执行；不改变生产控件或测量算法。
确认：2026-10-02，维护者确认独立测试类、场景标记、代码数据源和自动发现的方案并要求落地。

## 视觉一致性用例编写原则

决定：静态测试默认覆盖核心外观维度的全部有效组合，其他维度按实现关系和边界问题补充。
独立配置和行为展开为用例，同一次执行的观察时刻组织为采样点；测量定义复用、按需选择，允许追加诊断读数。
视觉和行为是判断核心，结构化事实辅助调试者结合源码寻找原因，不要求 probe 完整或报告自动归因。
动态采样围绕行为阶段选择，双端保持相同输入时间线和观察时刻，按需加密并保留有回归价值的采样。
静态采集的稳态由作者保证；持续动画由作者使用手动时钟安排从第 0 帧开始采集，不新增统一初始化或稳态检测机制。
适用范围：tests/VisualParity 的用例编写；本条确立前述迁移中暂未决定的覆盖和采样原则，
不改变现有测量算法、精确比较或门禁语义。具体规范与示例集中在[编写指南](../tests/VisualParity/AUTHORING.md)。
确认：2026-10-02，维护者逐项确认覆盖、按需测量、动态采样和作者负责初始状态的原则，并要求写入文档。

## 视觉采集等待合成批次完成

决定：AvaloniaHost.Draw 在 Dispatcher 工作与合成批次都处理完成后返回，包含渲染回调触发的后续提交；
不推进手动动画时钟，Snapshot 保持被动读取。循环上限仅用于报错，不允许超限后采集旧画面。
原因：处理上一批次的完成回调可能提交新批次，即使 Dispatcher 队列已空，最新位图仍未生成。
做法：针对固定 Avalonia 12.1.2 的 UI 线程 Headless 渲染，在宿主内集中只读检查 Compositor 的
待提交与待处理批次，使用 Rendered 而非 Processed 判断完成。公开接口缺少只读待处理工作查询，
不通过反复 RequestCommit 创建检查用批次；内部字段结构不匹配时报错，版本与线程模式升级须重新核验。
适用范围：视觉测试宿主的绘制同步；不修改 ComposeAnimation、生产组件或用例采样时间。
确认：2026-10-02，维护者同意修复绘制完成判断、局部检查内部状态，并要求实施。


## 视觉产物只持久化 PNG

决定：公开结果 schema 6、采集协议 schema 3，元数据仍为 schema 4；删除 raster.pixels 与 diffPixels，
不保留旧格式兼容分支。PNG 是唯一持久化像素来源，按现有 straight-sRGBA8 规则解码，精确比较语义不变。
HTML 引用现有 PNG，不内嵌像素；parity serve 使用现有 .NET/Skia 提供本地只读报告及按需解码，
完整像素交互不经过浏览器 Canvas 读回，直接打开 HTML 仍可查看事实和图片。
临时采集统一放在运行目录 .work；导出校验成功且无错误/不稳定时清理本次临时数据，失败证据保留并提供路径。
参考缓存直接读取，重采留在本轮工作目录，不再制造永久随机缓存。历史产物由维护者自行清理。
原因：重复的 RGBA、HTML Base64 与原始包占据大部分空间，不增加采集信息。
确认：2026-10-02，维护者要求实施产物精简，优先让 HTML 引用已有图片，并允许本地服务。

## 独立 Vite 报告与前端图像分析

决定：以 Vue 3 / TypeScript / Vite / Vue Router 应用替代 .NET ReportServer 和 HTML 模板，
单个服务按 run → case → sample 浏览全部当前格式运行。C# 只输出 Markdown、YAML 和 PNG，
增加小型 run.yaml 导航索引与 case.yaml.sampleSummaries；Agent 仍按用例概览和样本详情两层读取。
Vite 插件只负责列举配置根目录中的 run 和校验后提供只读文件；不提供像素服务或执行入口。
图像解码、取色、叠加、阈值及区域分析放入前端 Worker，直接使用规范 PNG，避免 Canvas 读回损失。
双端在采集时把 PNG 规范化为 sRGB 8-bit straight alpha；这使浏览器读取字节与 .NET 比较一致。
公开结果及 run 索引升级为 schema 7，采集协议为 schema 4，元数据仍为 schema 4。
删除 serve / render 及旧模板，不支持旧结果兼容；替代上一决定中的报告服务和格式版本，
PNG 唯一持久化像素来源及临时目录清理边界保持不变。运行时结果仍是权威判断，前端阈值仅影响展示。
原因：避免每次运行重复 HTML、像素副本和服务实例，并让报告作为普通前端应用维护。
确认：2026-10-02，维护者要求完成新 Vite 体系并清理旧代码，无需兼容。

## 千级视觉测试的独立参考准备与顺序执行

决定：prepare-reference 在单个常驻 JVM 内顺序补齐参考，发布不可变清单；普通 run 固定清单、
重新采集 Avalonia，不启动 JVM 或隐式补采。配置与参考实现不适用时在采集前报错。
Avalonia 保持单进程、单 UI 线程，每例独立 fixture、Host 和时钟；不引入进程池、数据库或通用调度框架。
构建内容冻结到独立目录；用例、配置、运行环境与参考身份写入 plan.json。比较继续消费独立捕获证据，
recompare 使用既有 run 重算比较，不把复用证据或旧重复性结论当成本轮执行。
结果仍自包含，保持概览和逐样本文件阅读；完成用例原子发布，进度追加到 journal，完整索引只在结束时生成。
环境哈希按运行复用，图片按样本处理，导出不反复回读 YAML；格式完整性由文件校验及 round-trip 测试保证。
共享产物库、精细组件依赖、并行执行和断点续跑延期，按阶段计时证明收益后再决定。
原因：优先消除逐例启动与重复处理，以明确的数据和执行边界支撑以后扩容，控制当前维护复杂度。
确认：2026-10-03，维护者认可精简方案并要求落地；先前 345 用例 26 分钟为用户报告的旧基准。

## Wide NavigationRail 纵向动画连续化

决定：Wide 与 Modal 的内置 item 布局在展开/收起过程中保留小数物理像素，不再复现固定 Compose 的整数抖动。
原因：125% 缩放的实际日志和固定源码对应到多层 roundToPx、整数 lerp、居中截断与排列取整；
维护者也确认上游存在相同抖动，接受在这一行为上改善体验。
做法：现有动画驱动连续的项最小高度、间距、纵向 padding、内容高度、位置与排列；
静态尺寸按原规则取整，并平滑补偿居中和排列的端点残差。移动容器关闭 LayoutRounding，
图标和文字仍在独立内容根节点保留测量取整。无新公共 API、独立动画或停稳后切换。
保留 spring 参数、过冲、中断速度、宽度算法、标签透明度和字体样式切换，以及布局本身的平缓折返。
适用范围：WideNavigationRail、ModalWideNavigationRail 和 WideNavigationRailItem 的内置模板及纵向布局；
不改变常规 NavigationRail、ComposeAnimation、视觉测试参考与容差。动态 parity 的真实差异保留，静态端点继续核验。
确认：2026-10-03，维护者在分析和方案讨论后明确要求落地；不新增专门测试，改后的真实桌面视觉体验交其核验。


## Button 的 Compose 时序与 Avalonia 输入接入

决定：沿用共享时钟，将圆角与 Button elevation 的目标合并到下一帧，对应重组后 effect，
不在用例中偏移帧或改变弹簧参数。圆角配置/动画 spec 改变时重建状态；目标改变先取消旧 effect，
再沿用状态。Float 进度和插值复现固定 AnimatedShape.kt 与 MathHelpers.kt 的计算语义。
指针移出交互范围即取消手势，通过释放本控件的实际 capture 同时取消 Avalonia Button 的待点击状态。
普通 Button/IconButton 的 Enter 在 KeyUp 经原生 automation Invoke 路径激活，保留 Click/Command/Flyout；
NavigationRailItem 与 WideNavigationRailItem 保留已有的组件键盘处理。
上述普通 Button/IconButton 的 Enter 激活决定已由下方“保留 Avalonia 原生键盘激活”替代。
受限内容夹具分别对照 Compose size 与 requiredSize，读取实际排列几何，不补偿组件布局。
新增 Filled/Tonal 各五个 elevation token，沿用既有 ButtonElevation API；禁用颜色、opacity 与默认描边遵循固定上游实际 helper。
确认：2026-10-03，维护者确认 Button 修复方案并要求实施；阴影绘制问题明确延期，本次不修改阴影算法。

## 普通 Button/IconButton 保留 Avalonia 原生键盘激活

决定：普通 Button/IconButton 保留 Avalonia 原生键盘激活语义，不对齐固定 Compose 的 Enter 松开激活。
原因：普通 Material Button 使用 Avalonia Button 加主题；共享反馈控制器接管激活会改变应用命令时机，
并与原生 Space 及导航项自身的键盘处理形成多套激活路径，增加职责协调成本。
做法：移除共享控制器的 Enter 拦截、待激活状态及释放时的 automation Invoke；
键盘事件继续驱动 Material 按压反馈，点击、Command 和 Flyout 由控件处理。
影响：Enter 按下时激活，重复 KeyDown 沿用原生处理；Space 遵循原生 ClickMode。
按下时已执行的命令不会因随后失焦或禁用而撤回；反馈仍在释放、失焦或禁用时清理。
保留真实 Compose 参考、采样和比较规则，如实报告 Enter 的激活时机差异，不将其改写为一致。
范围：普通 Button/IconButton 及共享控制器的键盘激活职责；导航项现有的释放激活适配保留，
不由本条决定其统一方案，也不撤回 Button 的圆角、elevation、颜色或指针手势修复。
确认：2026-10-03，维护者明确回复“是的。不需要Enter拦截了。然后将决策记录在案”。
