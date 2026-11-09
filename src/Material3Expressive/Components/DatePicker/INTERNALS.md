# DatePicker / DateRangePicker 实现说明

使用方式见 [README](README.md)。原生滚轮和日历主题不在这里。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）。
日期核心与 Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的对应文件一致。
不要用上游 HEAD 替代固定提交。

| 上游                                                    | 本地                                                                                                                                              |
|---------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| `DatePicker`、`DateInput`、`SwitchableDateEntryContent` | [DatePicker.cs](DatePicker.cs)、[DateEntryController.cs](DateEntryController.cs)、[DateEntryContent.cs](DateEntryContent.cs)                      |
| `DateRangePicker`、`DateRangeInput`、`setSelection`     | [DateRangePicker.cs](DateRangePicker.cs)、[DateRange.cs](DateRange.cs)                                                                            |
| 月网格、范围底色                                        | `DateMonthPanel`                                                                                                                                  |
| `VerticalMonthsList`、Core `DefaultFlingBehavior`、`ContentInViewNode` | `DateRangeMonthList`：按实际月份高度虚拟化、纵向 spline 惯性、焦点滚入 |
| `DatePickerModalTokens`、`DateInputModalTokens`         | [DatePickerModalTokenSource.cs](DatePickerModalTokenSource.cs)、[DateInputModalTokenSource.cs](DateInputModalTokenSource.cs) 与 Tokens 下同名文件 |
| Core `material3/l10n` 的日期字符串                      | [DateTimeStrings.cs](../DateTimePicker/DateTimeStrings.cs)                                                                                        |

公开入口是控件属性，不是可提升的状态对象。维护者于 2026-10-11 选择直接绑定：
`SelectedDate` 为 `DateOnly?`，`SelectedRange` 为一次写回的 `DateRange`。
UTC 当天零点毫秒数只留在与 Compose 对照时使用，不进入公开 API。
因此多个控件默认只共享有效日期；未完成的输入文本、年份列表展开和显示月份不会跟着另一个控件走。
`DisplayedMonth` 是单独属性，应用需要同步当前月份时可以绑定它。

打开、关闭和确认不属于控件。点选一天会立刻更新 `SelectedDate` 或 `SelectedRange`，不会等应用提交。

## 选择、输入与模式

### 日期与时间共享译表

本轮依据用户扩大授权重新决定并修复 DateTimeStrings 的区域键与导入：此前 JSON 使用 zhcn/ptpt 等
Kotlin 函数名，查询却使用 zh-cn/pt-pt；as/in/is 的反引号函数也未被导入。问题影响日期、范围和三个时间入口。
现在按固定 Core `l10n/Translations.kt` 的真实 localeTag 映射导入 84 个语言/地区、52 个日期时间字符串，
保留全部原有翻译正文，补入缺失语言与 24 小时后缀。运行时先 language-region，再 language，最后英文，
忽略脚本但保留地区（zh-Hant-HK → zh-HK），沿用 he/iw、id/in 别名。没有从未知中文区域猜测繁简体。
这是 Core 桌面字符串平台分支；AndroidX 的 Android 资源选择不混入桌面规则。

API 不变；以前依赖错误英文回退的应用将看到对应译文。显式 Title/Headline 仍可覆盖。
维护译表时按 docs/upstream-baseline.json 的固定 Core 提交核对 localeTag 映射与译文，不消费本机 HEAD。
本轮对旧正文逐键检查无改动。
真实 TimeInput editor/error live region 与 DatePicker/DateRangePicker 调用方共同验证共享查找。

日历点选：单日直接替换。范围在没有终点或新日期早于起点时重新开始，否则补上终点。
不可选日期、年份范围外的日期不改变当前值。`NormalizeSelection` 会去掉越界日期；范围缺少起点，或终点早于起点时，两端一起清空。

输入模式的日期数字不超过 `yyyy`、`MM`、`dd` 拼出的 8 位；按固定 `DateVisualTransformation`
的字段边界自动显示分隔符，也接受粘贴同区域的带分隔符日期。未输满时清空这一侧并去掉错误；输满后按区域顺序解析，再检查闰日、年份、禁选和范围顺序。非法字符被拒绝，已输入的数字保留。
Avalonia TextBox 没有独立显示变换入口，因此内部 `DateInputTextBox` 在 `CoerceText`
中格式化显示文本，仍调用原生基类维护编辑事务及撤销栈；控制器验证时提取数字，不更改 TextField 的共享
API。光标和选区按插入的分隔符映射；分隔符旁的 Backspace/Delete 选择相邻数字一起交给原生删除，避免删除后立即补回分隔符而卡住。
占位符使用带分隔符的大写格式。格式来自 `CultureInfo.ShortDatePattern` 的日月年顺序，并把年份规范成四位。它不是 Compose
`DateInputFormat` 的完整 skeleton，区域差异未逐个核对。

`DateEntryContent` 在单日模式切换时做垂直滑入和淡化，范围模式切换时只做交叉淡化。
年份列表用 `AnimatedVisibility` 覆盖在保留的星期/月网格上展开；年份内容为 335 DIP 加 1 DIP divider，与下层的 48 + 288 DIP
高度一致。背景跟随当前日期控件，切换过程不隐藏或移除月视图，不通过 Dialog 最小高度补偿。动画规格来自 `SpatialDefault`、
`EffectsDefault` 和 `EffectsFast`。
`DateMonthPanel.Render` 绘制透明全区域命中面，使日期按钮之间和空日期单元格也能收到月份滚轮/横滑输入；范围底色仍单独绘制。

## 已确认的适配

历史决定（2026-10-11）为缺少共享滚动能力时先提供可用入口，接受原生滚动和简化月份手势，不承诺拖动与惯性等于 Compose Foundation。
2026-11-08 按本轮维护者明确授权修订单日月份部分：仓内已有 Float 动画、速度追踪和 SnapFling，固定 DatePicker 的月份是等宽 LazyRow，能够直接移植而不引入通用 Pager。当时范围纵向列表与年份列表仍保留原适配，避免在尚无对应实测时把原生惯性声明为 Foundation 一致；TimeScroll 的后续实现见其自身文档。

本轮维护者再次明确授权缺失交互、必要共享能力及历史决定修订，范围部分改为 `DateRangeMonthList`。
固定 Core `Scrollable.desktop.kt` 的默认衰减是 spline，`DefaultFlingBehavior` 以 |velocity|>1 开始、
每帧消费 value-last，余量绝对值超过 .5 物理像素停止，持续运动的 DurationScale 固定为 1；不做月份吸附。
复用既有 SplineBasedDecay、VelocityTracker 与 MouseWheelScroll，滚轮平滑结束后不追加 fling。
月份按首项索引、整数 offset 与 Float 余量布局，标题实测、日网格六行；末端回填前月，避免无限日期范围绝对坐标。
保留焦点所在的视口外月份；日期获焦和 BringIntoView 使用最近边弹簧，每帧重算距离并保留重定向速度。
超过触摸阈值后捕获，让日按钮取消点击；禁用、外部定位、卸载取消手势与运动。
不增加公共 LazyList、嵌套滚动或预取框架，年份列表继续原适配。
滚轮在边界可继续向外冒泡；触摸已被列表捕获后，未消费的拖动量没有跨控件 nested-scroll parent 协议，
不能据本列表的双端运动通过推断嵌套滚动已经完成。

布局迁移：原模板 StackPanel 丢失正文的最大高度，范围视口曾固定为本地 400 DIP Token。
改用 Auto/Auto/* Grid 传递标题与分隔线之后的真实剩余约束；范围列表按固定 LazyColumn 要求有限高度。
移除无上游依据的 `RangeMonthListHeight` Token，调用方用控件 Height/MaxHeight；Catalog 外层滚动示例已迁移。
普通 DatePicker 仍按正文自然高度测量。该变更是本轮授权的布局约束迁移，并非调整双端基准。

`DateMonthStrip` 按 firstVisibleItemIndex + 整数物理像素 offset 存放位置，保留 LazyList 的半像素累积；只实例化与视口相交的月份。视口外的日按钮不保留在控件树中。
箭头和日期键盘跨月使用 `LazyLayoutScrollScope.animateScrollToItem` 的搜索/可见目标两阶段弹簧：目标首次可见时按整数 itemOffset 重启并保留速度/帧时间，最后精确定位。此内部入口只用于相邻月份，不提供通用远距离列表导航或预取协议。
释放用固定 `DatePickerDefaults.rememberSnapFlingBehavior`：approach 恒为 0，DefaultEffects 弹簧，400 DIP/s 门槛，低速取最近月份，等距时选后者。复用 Foundation 的 SnapFling、VelocityTracker 和 MouseWheelScroll。
Core 的 18 DIP 触摸阈值、排除鼠标按钮拖动、横向滚轮轴选择和 RTL 符号均保留；设备专用 trackpad 元数据仍受 Avalonia 事件限制。
日期按钮会消费按住时的 PointerMoved，因此月份容器在隧道路由中判断横向阈值；超过阈值才捕获，日期按钮通过 capture lost 取消点击/波纹。坐标取事件的物理顶层坐标，再应用一次 RTL 符号。原始触摸测试覆盖实际日期按钮、LTR/RTL、1x/4x、快/慢释放，不只调用吸附算法；早期真实触摸未移动的失败报告保留在 `date-month-verified`。
`DisplayedMonth` 回写第一个仍可见的月份，动画中不由该回写重新定位。外部空闲赋值和年份选择直接定位；卸载取消动画/捕获，禁用与方向变化结束当前手势。

迁移：公开日期属性保持不变。依赖旧“竖向滚轮立即换月”或“鼠标左键横拖翻页”的应用应使用导航按钮、触摸或横向滚轮；依赖 Click 后同步读取新月份的调用方应观察 `DisplayedMonth`，其值现在随真实滚动变化。Catalog 与仓内断言已迁移。

年份键盘路径按固定 `YearPicker` / `MonthsNavigation` 补齐：打开时定位当前年份，年份的 Shift+Tab 返回月份标题，Tab 通过 divider 之后的焦点位置离开网格；反向进入 divider 时回到当前年份。Avalonia 的 `Focus`、`TryMoveFocus` 与 `FindNextElementOptions.FocusedElement` 对应 FocusRequester / FocusManager，不给每个年份建立额外状态对象。初始滚动位置按 displayedYear - firstYear - 3 计算。
日期首尾键盘规则按固定 `dayOnKeyEvent` 的 first/last selectable date 路径移植：先处理首日 Shift+Tab / 尾日 Tab，滚动期间吞掉其余首尾按键；左右移动跳过禁选项，跨月等待月份动画完成再聚焦。年份选择使用立即定位并取消旧月份动画，避免新年份被旧动画写回。
`DateKeyboardTests` 覆盖 LTR/RTL 的年份进入、方向移动、正反 Tab、Enter 选择、禁选日期跳过和键盘跨月，以及年份选择取消尚未完成的月份动画。整月均不可选时的跨月焦点及年份列表超出视口的二维焦点搜索尚未逐项双端验证。

2026-11-08 平台选择（本轮维护者明确授权）：年份方向键采用 AndroidX 的二维焦点导航意图，通过 Avalonia `TryMoveFocus` 实现。固定 Core `RootNodeOwner.skiko.kt.getFocusDirection` 只映射 Tab、DirectionCenter、Back，`YearPicker` 本身没有方向键处理，因此此路径与 Core 桌面执行结果不同；不把 AndroidX 与 Core 的实现拼成一个上游版本，也不宣称桌面年份方向键一致。
`date-keyboard-verified` 的 2 个 LTR/RTL 用例重复稳定、零执行错误，但均 different：自动打开后的焦点、后续焦点/选择时间线及已有外观仍有差异。用例保留自动打开采样，再对真实年份节点调用 RequestFocus/Focus 来检查内部按键路径；没有改写参考组件。除上述明确选择的年份方向键平台差异外，其余失配仍未接受，也未全部定位。日期首尾键盘路径已有行为测试，尚未取得独立双端通过结论。

`date-picker`
视觉用例允许真实日期文字。这只放宽“用例不得含文字”的限制，不放宽比较容差，也不把失配收成已接受差异。见 [AUTHORING.md](../../../../tests/VisualParity/AUTHORING.md)。

日期输入通过 `DisplayMode.Input` 提供，没有独立公开 `DateInput` 类型；对话框已有独立 [DatePickerDialog](../DatePickerDialog/README.md)，不属于缺失项。

## 尚未实现或未验证

范围月份标题的 `customScrollActions` 已接入 `DateRangeMonthHeading` 的两个虚拟 Invoke 子 peer，
使用固定 AndroidX/Core `DateRangePicker.kt` 相同的前后可滚动判断与 firstVisibleItemIndex ± 1。
两个动作在边界仍存在，不能执行时禁用；不向布局或 Tab 序列添加按钮。Avalonia 没有命名动作列表，
本轮维护者授权以已有 BottomSheet 的虚拟动作表达复用此适配，并新增对应的两个 TryScroll 方法供应用调用。
两入口在范围列表直接定位并接受末尾约束。首个可见月以完整月份容器（包含标题）判断。
原外部 DisplayedMonth 改变会重建全部日历，无法保留动作持有的视口；现定位原实例而不重建。
动作入口无签名迁移；高度约束迁移见上文。验证见 `DateRangeNavigationTests` 与 `date-range-navigation` 双端族；
真实屏幕阅读器客户端尚未验证。

`DateRangeCalendarPanel` 通过 Avalonia 12.1.5 的 `ICustomKeyboardNavigation` 返回 `(true, null)`，
使原生 Tab 搜索从整个月历外继续，直接对应固定 `DateRangePickerContent.onPreviewKeyEvent` 的正反向逃逸。
不在视觉或辅助技术树中添加哨兵；从外进入与日期方向键保留原生搜索。`DateRangeKeyboardTests` 覆盖有/无模式按钮与 RTL，
`date-range-keyboard` 调用真实键盘及固定 Core 日期焦点语义。

`date-range-navigation-verified` 的 8 例（48 个采样）命名动作、接受结果、首个可见月与起止日期全部一致，
重复稳定、零执行错误；48 张图片仍不同。参考宿主同时设置并恢复平台 Locale 与 state.locale，
前者提供动作译文，后者提供日期格式，不修改固定参考组件。
`date-range-keyboard-final` 的 4 例（12 个采样）日历获焦、Tab 到后续控件及 Shift+Tab 回到标题/前一控件全部一致，
同样重复稳定、零执行错误。已检查真实 RTL 图片，仍可见范围标题字形/顺序、标题高度、模式图标和月份排版差异。
这些截图不作为整个范围选择器视觉一致的通过证据；首轮错误地查询 ContentDescription 的夹具失败记录也保留，
固定 Day 实际将日期描述放在 SemanticsProperties.Text，改为真实字段后才取得上述焦点结果。
最终夹具的前后焦点色块按 Compose Column 的 Start 对齐；两端 LTR/RTL 色块的实际像素边界均已核验相等。

- 年份列表仍未移植 Compose LazyList 的完整滚动协议；两类月份列表没有跨组件 nested scroll、远距离 teleport/预取或平台 overscroll。
- 标题日期使用 `ddd, MMM d`，未对照 `DatePickerFormatter` 的完整 skeleton。
- 真实 DatePicker 双端仍有标题本地化、文字/图标、日期网格与总高度 1 DIP 差异；本轮保留失败证据，不调整容差或将其视为已接受。
- 范围标题仍共用单日的 120 DIP 最小高度与左边距；固定 `DateRangePicker` 使用 128-60 DIP 最小高度、64 DIP 起始边距。三个独立标题文字已由下述公开内容单元补齐；范围标题字体的模板优先级仍未修复。这些既有几何差异会影响真实范围视口与切月时刻，不能用非文字滚动用例的通过覆盖。
- 无障碍遍历顺序、触摸滑动阈值和全部区域的输入格式未逐项对照上游测试。

逻辑检查在 `tests/Material3Expressive.Tests/DateTimeEntryTests.cs`。
双端用例是 `tests/VisualParity/Material3Expressive.Parity/Cases/DatePickerCases.cs` 与 reference 侧同名 Kotlin。
命令是 `./tests/VisualParity/parity.ps1 list --family date-picker`，再对该 family 做 `prepare-reference` 和 `run`。

本轮验证：`DateMonthScrollTests` + `DateTimeEntryTests` 覆盖速度门槛、等距吸附、首个可见月、反向中断、年份边界、桌面输入、外部赋值和卸载重挂。
`date-month-motion` 的 24 个双端用例直接对照固定 Core LazyRow + DatePickerDefaults 的真实 fling，使用相同几何内容独立检查位移；`date-month-scroll` 的 8 个用例操作真实 DatePicker，保留完整截图和选中/显示月份。
报告：`artifacts/visual-parity/runs/date-motion-verified` 24 例全部相等且重复稳定。真实控件最终证据为 `date-month-routed-verified` 的六个非 RTL 触摸用例，以及 `date-touch-rtl-fast-verified` / `date-touch-rtl-slow-verified`；后两者修正了 Avalonia 本地原点在 RTL 下位于右边缘、旧用例触点落到控件外的问题，使用与 Core 相同的物理左上角定位。
这八组真实控件时间线的月份与选中日期读数全部一致，重复稳定且零执行错误；外观仍全部 different，包含已有标题、文字/图标、网格与高度差异。`date-month-first`、`date-month-verified` 等早期失败报告保留。精确月份运动通过不代表整个 DatePicker 外观已完成。

## 自定义日期 formatter（2026-11-09）

本轮维护者授权新增 `IDatePickerFormatter`，直接对应固定 `DatePickerFormatter` 的两个操作、可空结果与描述标志；
沿用已确认的 DateOnly/CultureInfo 边界，不将 UTC 毫秒引入公开 API。单日/范围标题、月份导航/范围月份标题、
日期按钮的朗读描述与禁选日期错误消息均调用该接口。默认标题按上游保留 Polite live region 和本地化当前选择描述。
替换实例只更新已实现的呈现节点，不重建日期输入或滚动容器；未实现的月份首次创建时读取当前 formatter。

固定 Core 桌面 formatter 在 `PlatformDateFormat` 使用 Java MEDIUM/FULL 和 `LLLL yyyy`；AndroidX Android 分支使用 skeleton。
本轮没有混用两者或宣称默认格式一致：未指定 formatter 时保留原有 .NET 格式，完整默认 skeleton 仍未完成。
可空结果遵循上游占位规则；禁选错误中的 null 按 Kotlin 字符串插值显示 `null`，空字符串不回退。

`DatePickerFormatterTests` 检查两个入口、默认回退、空字符串、草稿/焦点/选区保留和错误重格式化。
`date-formatter` 双端用例覆盖 en-US/de-DE、两个模式、空/已选日期、null 返回和运行时替换，并同时保留首帧与稳定帧。
初轮 `date-formatter-delivery` 36 例零执行错误且重复稳定，测得标题描述和选择值一致，但图片全部 different；
真实截图显示 Core 首帧月历尚未进入及既有图标、排版差异。用例与失败证据保留，未放宽容差。

稳定帧复测 `date-formatter-verified`：36 例、84 个样本的标题描述与起止日期读数全部精确一致，
零执行错误且重复稳定；84 张图仍全部 different。已检查稳定帧，仍见字体/图标、月份标题与日期网格布局差异，
不是 formatter 字符串差异。未把这些视觉问题或默认格式缺口记为已接受。

**公开标题与大标题内容（2026-11-09）**

本轮维护者授权新增四个可独立复用的控件，直接对应固定 AndroidX/Core 的
`DatePickerDefaults.DatePickerTitle/DatePickerHeadline` 与
`DateRangePickerDefaults.DateRangePickerTitle/DateRangePickerHeadline`；两份相应函数已逐份读取。
`DatePickerHeaderContent.cs` 复用既有译表、Culture 与 IDatePickerFormatter，不创建第二套日期状态。
标题按显示模式选择文案；单日 headline 保留一行及 polite 描述；范围 headline 按上游调用顺序分别
请求起止视觉和详细文本，保持 null 占位与空字符串的区别，用三个 TextBlock、4 DIP 间距和垂直居中呈现。
局部 Row 面板按剩余宽度依次测量，不以水平 StackPanel 的无限宽测量替代；排列直接复用已有
MaterialHorizontalArrangement.SpacedBy(4)，在物理像素中夹取耗尽宽度后的间距，范围 peer 合并为一个 Text 节点。
Typography 与外层 Padding 由调用方提供，颜色默认经主题 Token 配置，显式 Foreground 仍可覆盖。

DateEntryController 的默认内容迁入这些公开组件，配置变更复用实例，完整 picker 继续负责选择合法性。
独立范围 headline 不归一化起止输入，因为上游该内容函数不负责状态校验。原 Title/Headline 及模板优先级不变；
已有自定义模板仍接收原字符串数据。模板内部 PART_Headline.Content 的默认值现在是控件，读取内部结构的
调用方应迁为读取 DatePickerHeadline 或范围三个实际文本；仓内 formatter 回归已迁移。
范围默认分隔符从旧拼接的 en dash 改为上游 ASCII '-'，字形和三槽间距属于新增内容实现的必要变化。

默认 DateFormatter=null 继续沿用完整 picker 已有的 .NET 日期显示；它不等同 Core skeleton formatter，
该既有未完成差异未在本轮接受。自定义 formatter 两端直接接收同一日期/Locale，替换时不改选择或草稿。
`DatePickerHeaderContentTests` 覆盖独立/集成入口、模式、区域、null/empty、RTL、4 DIP 间距、合并语义和模板恢复；
与 formatter、范围键盘/导航及日期输入回归共 33/33 通过，Catalog Release 构建通过。
随后补齐耗尽宽度的排列回归，新增 Header 与 formatter 定向 13/13 通过；没有修改共享排列能力。
`date-header` 独立双端族保留真实文字与完整截图，覆盖单日/范围、两模式、空/部分/完整选择、formatter
空结果/替换、区域/RTL、有限宽度和默认 formatter。

`date-header-public-final` 完成 30 例、38 个样本，零执行错误且全部重复/额外截图稳定。
26 个自定义 formatter 用例的 204 项文本、描述、polite、宽高事实全部相等；四个默认 formatter 用例
仅视觉文本事实不同（例如 Core `Feb 10, 2024`、本地 `Sat, Feb 10`），详细日期描述相等。
38 张图全部 different；已打开同文字的单日/范围、有限宽度及默认格式双端图像，前者仍有字形栅格差异，
后者还直接显示既有格式差异。这些视觉问题不视为通过，不放宽容差或以自定义 formatter 覆盖默认入口。
复用共享排列后的最终 `date-header-arranged` 再次完成全部 30 例（38 样本）：零错误、全部稳定，
224 项事实相等，仍仅默认 formatter 的四项 headline 文本不同；全部图像差异原样记录。
最终范围布局包含上述耗尽宽度行为回归；这些用例不宣称日期默认 skeleton 或字体后端已补齐。

## 范围纵向月份验证（2026-11-09）

`DateRangeMonthMotionCases` 的 15 个用例将变量高度月份的滚动几何与真实 `DateRangePicker` 分开验证，
所有输入与采样时间线双端相同。9 个非文字用例覆盖 spline 正反向释放、末端回填、真实触摸快/慢释放、
滚轮、外部定位中断、部分可见月份中的日期获焦及视口缩小时的焦点保持；最终每例 23 个采样的几何、
首项索引/offset 和全图均完全相等。6 个真实日期用例覆盖 LTR/RTL 的触摸、滚轮和中断；
每例 23 个采样的首个可见月、起止日期是否选中均精确一致，图片仍全部不同。重复性检查全部稳定，执行错误为零。

首轮 `date-range-month-motion-selftest` 保留了参考宿主输入环境错误的证据：`PlatformContext.Empty`
提供固定 18 物理像素 touchSlop，在 4× 捕获中等价于 4.5 DIP；真实桌面的固定 Core
`ComposeSceneMediator.DesktopViewConfiguration` 使用 `18.dp.toPx()`。本套件现在显式提供该桌面
ViewConfiguration，其他属性继续委托默认配置；没有改变触点、组件源码、共享宿主或比较容差。
修正后真实触摸的月份时间线与释放速度一致，非文字触摸全图也完全相等。

焦点动画保留固定 `ContentInViewNode` / `UpdatableAnimationState` 的帧阶段语义。
`LazyListMeasureResult.copyWithScrollDeltaWithoutRemeasure` 只在首项不变、首尾可见项边界仍严格容纳
本次整数位移时走仅 placement 路径；`afterFrame` 此时读取绘制前的旧子项坐标，跨可见项边界的
同步 remeasure 才立即提供新坐标。此前立即 `UpdateLayout` 读取新坐标，会错误地消除上游实际的
小幅越过最近边现象并改变整条弹簧曲线。现按源码条件区分两条路径，未硬编码测试终点或采样时刻。
视口缩小时只追踪原先最大可见的焦点；用户已滚出视口的焦点不会把月份拉回。

证据目录按先后保留：`artifacts/visual-parity/runs/date-range-month-motion-desktop-selftest`
完成全部 15 例，其两个 focus 条目仍记录修复前的差异；最终焦点证据位于
`artifacts/visual-parity/runs/date-range-focus-verification/<case-id>`。
该目录的 `dynamic.geometry.focus`、`dynamic.geometry.focus-resize` 和回归 `dynamic.geometry.fling`
均全量相等；最终真实触摸证据是同目录的 `date-range-month-motion.dynamic.picker.touch.ltr`，
其 69 项行为事实相等、23 张图片仍不同。已打开焦点过程及真实日期滚动后的双端图片检查：
月份内容正确续接和裁剪，仍存在原有范围标题高度/边距、标题字形与字号、模式图标及月份排版差异。
这些视觉缺口没有被非文字用例的通过覆盖，也没有记为已接受。

最后一次源码修改后，`DateRangeMonthScrollTests` 12 例、`DateRangeKeyboardTests` 4 例和
`DateRangeNavigationTests` 6 例合计 22/22 通过，日志为 `artifacts/date-range-final-tests.log`。
范围列表仍没有跨控件触摸 nested-scroll parent 协议、远距离 teleport/预取或平台 overscroll；
年份列表也仍使用原有适配。上述验证只覆盖本次月份视口、输入、惯性与焦点能力，不宣称这些共享协议已完成。
