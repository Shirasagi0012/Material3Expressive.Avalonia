# VisualParity 已确认的设计决定

本文记录当前有效的框架决定及确认依据。运行与格式版本见 [README](README.md)，
覆盖与采样规范见 [AUTHORING](AUTHORING.md)，测量与容差算法见 [MEASUREMENTS](MEASUREMENTS.md)。
组件特有的适配及已接受差异随组件 README 保存，不由本文件扩大范围。

## 主键指针时间与原生路由（2026-11-05）

本轮依据用户扩大授权重新决定：AvaloniaHost.Input 统一使用共享测试适配器 HeadlessMouseInput，
通过窗口自身的 MouseDevice/InputRoot/raw-input 路由送入逻辑毫秒时间，与 ComposeHost 的一秒原点相同。
依据锁定 Avalonia 12.1.5 / `5825bf8` 的 HeadlessWindowImpl：公开鼠标扩展使用 Stopwatch.ElapsedMilliseconds。
独立 PointerTimeTests 在旧实现要求 1001/1017/1033ms，实得 528/581/582ms；新实现核对精确时间、
捕获后越界移动、同一指针身份、主键状态与不额外发帧。它不固定速度或组件结果。
AppBar、Sheet、Swipe、Drawer、Modal Rail 的重复 raw-input 实现归入同一入口；仅测试项目共享源文件，
不增加产品 API。测试调用为 `new HeadlessMouseInput(window).Send(timeMs, "move", point)`，绘制由夹具显式完成。
改平台时间源会扩大到整个 Headless 实例，组件各自替换时间则掩盖真实输入，因此均不采用。
主键 trace 增加实际送入的 pointerTimeMs；旧实际捕获不能靠 recompare 修复，需重新采集。

新 pointer-velocity 双端用例使用真实 draggable / AppBarVerticalGesture，对照两点、三点、反向、
同时间戳、暂停与内容随拖拽移动。前五种实际释放速度相等；移动内容保留差异：Core 约 -0.000584 DIP/s，
Avalonia -1250.000732 DIP/s。固定 Core SyntheticEventSender 在布局后重送同时间戳的鼠标 Move，
isDraggableZeroDeltaConsumptionEnabled 为 true；本地没有对应的场景输入重送协议。
不向测试或组件补造重复样本来消除此差异，也不把原先墙钟偶然相等当成已验证。
单个 enabled 策略行为测试改为明确暂停超过 40ms 后释放，隔离其原本要验证的禁用策略；立即释放的失败
继续由上述真实双端用例及 search-app-bar.dynamic.drag 保留。此决定修复夹具时间，不接受或解决移动内容的速度差异。

## 独立用例、C# 定义与冻结配置

决定：C# 是用例定义的唯一来源，普通循环/LINQ 与组件配置类型生成具体 CaseMetadata；JSON 只作为运行产物。
每个组件维护独立 XxxCases 类，以 suite/scenario 标记执行身份，方法名不参与协议，具体用例 ID 保持稳定。
原因：避免手写配置与重复场景，不让中央 partial Cases、CaseCatalog 或入口表阻碍组件增长。
做法：C# 只扫描明确执行程序集，Compose 只扫描 producer 自己的 jar；先校验声明与展开数据，
再筛选并核对双端场景，捕获前冻结选中 JSON。两端读取同一 config，通过真实 API 构建场景；配置进入元数据哈希及结果。
每次执行创建新测试类实例，资源在场景内管理。删除手写 JSON 发现、重复配置入口、中央注册表、
composeEntry/avaloniaEntry、旧 compare 转换及别名，不保留兼容分支。
范围：用例定义、发现、配置身份和执行，不扩展生产组件能力；覆盖与采样由下方决定约束。
确认：2026-10-02，维护者先确认统一 C# 定义并冻结配置，再确认独立测试类、场景标记及自动发现。
后者替代前者的中央组织方式；历次 schema 版本由 README 的当前契约替代，不作为并行支持的格式。

## 覆盖与采样由用例作者负责

决定：静态测试默认覆盖核心外观维度的全部有效组合，其他维度按实现关系和边界补充。
独立配置和行为展开为用例，观察时刻组织为采样点；测量定义复用、按需选择，可追加诊断读数。
视觉和行为是判断核心，结构化事实辅助源码调查，不要求 probe 完整或报告自动归因。
动态采样围绕行为阶段选择，双端输入时间线和观察时刻相同，按需加密并保留有回归价值的采样。
静态稳态由作者保证；持续动画用手动时钟从第 0 帧安排采集，不引入统一初始化或稳态检测机制。
范围：用例编写，确立前述定义迁移尚未决定的覆盖与采样原则，不改变测量、比较或门禁。
确认：2026-10-02，维护者逐项确认覆盖、按需测量、动态采样和作者负责初始状态，并要求写入规范。

## Avalonia 采集等待合成完成

决定：AvaloniaHost.Draw 在 Dispatcher 工作和合成批次全部完成后返回，包括渲染回调产生的后续提交；
不推进手动动画时钟，Snapshot 被动读取，循环上限只用于报错，不允许超限后采集旧画面。
原因：上一批次的完成回调可能提交新批次，Dispatcher 队列已空不能证明最新位图已生成。
做法：固定 Avalonia 12.1.2 的 UI 线程 Headless 宿主集中只读检查 Compositor 的待提交与待处理批次，
使用 Rendered 而非 Processed 判断完成。公开 API 缺少查询入口，不反复 RequestCommit 制造检查批次；
内部字段不匹配时报错，版本或线程模式升级须重新核验。
范围：视觉测试宿主的绘制同步，不修改 ComposeAnimation、生产组件或采样时间。
确认：2026-10-02，维护者同意修复绘制完成判断、局部检查内部状态，并要求实施。

## Compose 普通延迟与 timeout 共用宿主时间

决定：参考进程在初始化协程与 Skia 前启用 coroutines 1.9.0 的 Main delay 路由；
`ReferenceTimeoutClock` 仅将固定 Core `FlushCoroutineDispatcher`（以及显式 Main）上下文的
timeout 转给当前宿主的 TestCoroutineScheduler。后台 IO / Skiko FrameWatcher 继续使用原始
DefaultExecutor，宿主退出先取消场景任务，再解除测试调度器连接；同一进程只允许一个活动宿主。
原因：固定 Core 只重写 scheduleResumeAfterDelay，withTimeout / withTimeoutOrNull 的默认
invokeOnTimeout 原来走墙钟，导致 Tooltip 超时未发生和 ScrollBar 重复捕获不稳定。
做法：保留 Core 的 trampoline/frame 队列和组件代码，不追加帧；advanceTo 先执行到期任务，
frame 再发送原有的一批帧等待者。使用固定协程库的内部 DefaultExecutor 仅用于保持后台原始时间源；
升级 Core 或协程版本时必须核对上下文类型与 DefaultDelay 路径，并运行 hostTest。
范围：参考测试宿主，不改变生产组件或固定上游版本；不是修改组件动画时长或放宽比较门禁。
确认：本轮依据用户扩大授权重新决定，替代 Tooltip / ScrollBar 文档中的待定方案。
独立的真实 FrameRecomposer 测试先复现旧 timeout 不随虚拟时间结束，修复后覆盖普通延迟、
同期限顺序、取消、被动截图、帧分离、卸载、连续宿主和后台时间隔离；真实组件另做双端 self-test。

## 自包含结果、PNG 与独立报告

### Headless framebuffer 的 alpha 保留

问题：锁定 Avalonia.Headless 12.1.5 的 GetLastRenderedFrame 将 Premul framebuffer 的副本标为 Opaque，
导致半透明蓝色的原始 (0,0,128,128) 被输出成 (0,0,128,255)，完全透明像素也变为不透明。
依据是锁定包 nuspec 的 [5825bf8 固定源码](https://github.com/AvaloniaUI/Avalonia/blob/5825bf8c66fba0c11e67c980d8ece6444576bffa/src/Headless/Avalonia.Headless/HeadlessWindowImpl.cs)
以及独立 Skia Src 绘制、直接读取实际 framebuffer 和 PNG 的失败回归；不依据 TimePicker 目标图倒推颜色。
决定：本轮依据用户扩大授权重新决定，Snapshot 在 Headless 原始同步锁内复制已完成 framebuffer，
保留其实际 AlphaFormat，再沿用既有 PNG 规范化。调用仍为 host.Snapshot(...)，不请求 Draw/Frame。
替代方案：公开 GetLastRenderedFrame 无法保留元数据；事后反推 alpha 会制造事实；升级依赖超出此修复。
仅在测试宿主内集中检查私有字段类型，布局改变时明确失败并要求复核，不改生产控件或全局合成策略。
验证：透明、半透明、不透明三例和被动重复采集通过；不透明 Tab 动画的六张原图与修改前逐像素相同。
影响：含透明像素的 Avalonia 新产物纠正颜色和 alpha。历史 PNG 已丢失 alpha，不能靠 recompare 修复，
需要重新 capture-actual 或 self-test；参考基线、容差及 straight-sRGBA8 比较规则保持不变。

决定：Agent 按“用例概览 → 自包含的单采样点 YAML”读取结果，不依赖 inspect 查询接口或整包 agent.json。
公共描述和值合并，保留双端来源、语义差异、原值和不可用原因；概览计数不代表完成百分比。
PNG 是唯一持久化像素来源，不保存 raster.pixels、diffPixels 或重复 Base64；统一为 sRGB 8-bit straight alpha。
原因：重复 JSON、像素与报告服务增加存储和往返成本，不提供额外采集信息。
做法：C# 输出 Markdown、YAML、PNG 与 run/sample 导航摘要；Vue/TypeScript/Vite 按 run → case → sample 浏览。
Vite 插件只校验并提供只读文件，不执行用例或处理像素；前端 Worker 解码规范 PNG 并诊断，避免 Canvas 读回损失。
运行时判断仍权威，前端阈值仅影响展示。删除 .NET ReportServer、HTML 模板及 serve/render，不支持旧格式。
临时采集放入 .work，校验成功且无错误/不稳定时清理；失败证据保留。参考缓存直接读取，重采留在本轮目录，
不制造永久随机缓存；历史产物由维护者清理。
范围：结果存储、文件读取和报告，不改变生产控件或双端场景；精确比较语义保持，Raster 容差见下方决定。
确认：2026-10-02，维护者依次确认两层阅读、PNG 唯一持久化及独立 Vite 体系。
Vite 替代此前随结果输出 HTML 和 .NET 像素服务的方案，当前公开结果契约替代历史结果 schema 3～7；历史由 Git 保存。

## Raster 降采样容差试用

决定：保留固定 4× 捕获与原始精确差分，固定 4×4 sRGBA8 块平均得到 1× 比较数据。
整图及每个非空配对点布局区域要求最大通道差 ≤20、平均逐像素最大通道差 ≤1（0～255）。
原始不同但接受的结果单列 withinTolerance，不称 equal；结构化事实与重复性仍精确比较。
--require-match 使用容差，--require-equal 保持严格；报告保留原始与容差证据。
原因：允许试用排除渲染器抗锯齿边缘微差，同时用区域均值限制大面积偏色，阈值待更多组件验证。
范围：Raster 比较与报告，替代早期不使用 Raster 容差的限制，不修改生产组件；透明与阴影限制见 MEASUREMENTS。
确认：2026-10-03，维护者认可 4× 渲染、1× 比较，先试最大差 20、区域平均 0.2；
NavigationRail 混色验证后明确将区域平均改为 1，最大差仍为 20，不增加逐像素忽略规则。

## 独立参考准备与顺序执行

决定：prepare-reference 在单个常驻 JVM 内顺序补齐参考并发布不可变清单；run 固定清单、
重新采集 Avalonia，不启动 JVM 或隐式补采，配置与参考身份不适用时在采集前报错。
Avalonia 保持单进程、单 UI 线程，每例独立 fixture、Host 和时钟，不引入进程池、数据库或通用调度框架。
构建冻结到独立目录，用例、配置、环境与参考身份写入 plan.json；比较消费捕获证据，
recompare 不把复用证据或旧重复性结论当作本轮执行。结果自包含，逐例原子发布，进度追加 journal，结束生成完整索引。
环境哈希按运行复用，图像按样本处理，导出不反复回读 YAML；文件校验和 round-trip 测试保证格式完整。
原因：消除逐例启动和重复处理，以明确执行边界支撑扩容，控制维护复杂度。
范围：千级视觉测试准备与执行；共享产物库、精细依赖、并行执行和断点续跑延期，按阶段计时后再决定。
确认：2026-10-03，维护者认可精简方案并要求落地；旧 345 用例 26 分钟为维护者报告，不作为本轮计时证据。

## 2026-11-05：输入坐标与基线变更夹具的等价性修复

- Drawer 子按压：Compose 从场景包围框左边取 80 DIP，Avalonia 的局部 TranslatePoint 在 RTL 会镜像此偏移。
  修复前相同用例 trace 为 x=236 / x=412，图像波纹向相反方向扩展。改为两端都从场景包围框左边取物理偏移，
  已重跑原 child-press 和新增 state-child-press；没有更改控件、容差或采样时序。
- RichTooltip 动态显式基线：Avalonia 在同一槽根清除附加属性；参考夹具原来从提供基线的 Layout 分支切换到 Canvas，
  实际测试了内容节点替换，固定 Core 此时残留旧基线布局。SharedBaseline 场景改为保持 Layout，清空其 alignmentLines map，
  与本地操作保持相同身份及事实变更。原生 BaselineOffset 静态用例保留原分支；静态无基线尺寸 122 DIP 与清除后的读数一致。
  原失败报告 `shared-baseline-tooltip` 保留，修复后 `shared-baseline-tooltip-final` 不增加任何帧；两端结构化读数相等。
以上为本轮依据用户扩大授权修复的夹具问题，不把固定 Core 的节点替换行为或抽屉/滑动锚点异常改写成通过。

SnackbarHost 超时交接的夹具随后独立核验：固定 Avalonia 12.1.5 的 TemplatedControl 将 ClipToBounds 默认设为 true，
空主题 Button 继承它；参考 Canvas 没有裁剪。4096000µs 时两层 alpha/scale 完全相等，但本地 y=73/246 的边缘为白色，
参考为 (175,160,241)，原始 864 个不同像素全部落在这两条边缘。仅把标记 Button 的 ClipToBounds 显式设为 false 后，
16 个 Host 场景所有像素及结构事实均 equal（`snackbar-host-unclipped`），重复稳定。
生产属性绑定修复另在 `snackbar-host-bound` 验证仍 16 equal。该修复保留真实内容与原时序；它不是共享渲染器或阴影算法的改动。

## 2026-11-06：ButtonGroup 自绘内容的颜色失效

本轮依据用户扩大授权修复 Marker 夹具：它在 Render 中读取继承 Foreground，却没有登记 AffectsRender。
独立 `ButtonGroupMarkerRepaintsInheritedInkWithoutLayoutOrAnimationFrames` 在没有 ButtonGroup、Button、
波纹或后续动画帧的场景中，把父级红色改成蓝色；修复前像素保持红色，修复后变蓝，几何不变。
只补登记绘制依赖，未改组件、内容面积、时间线或门禁。原 connected 的 96ms 波纹/背景原本相等，
内容块颜色过期；`button-group-marker-current` 该帧现逐像素相等。81ms 非帧释放后的组合缓存差异仍保留。

Card ambient-color 另确认是宿主输入不等价。`card-ambient-inputs` 直接记录外层与组件实际颜色：
Core 外层 LocalContentColor 和 Card 内容均为 [0,0,0,255]；Avalonia Window 默认继承 OnSurface，
外层和内容均为 [29,27,32,255]。两端各自正确保留 ambient，不能修改 ContentColorFor 来消除输入差。
该场景显式给 Avalonia 宿主黑色前景，组件仍自行走非角色容器的 ambient 回退。`card-ambient-matched`
的颜色、几何和全部图像精确相等且重复稳定；继续保留实际 ambient/content 读数，未改比较器或隐藏内容。

Chip 自定义颜色夹具另有选择器范围错误：`:disabled` 没有限定类型，匹配了所有被禁用的模板后代，
将同一 `#50336699` 背景写入多个 Border。独立回归确认 Chip、Surface 及其有效背景已正确取禁用色，
但本应透明的四个后代 Border 也收到该色，修复前断言失败；因此不能调整 Surface alpha 或阴影来补偿。
仅把三条状态选择器限定为 `Is<Chip>()`，保留用户指定的状态颜色、原内容和时序。五种实际 Chip 的
后代隔离回归通过；原 10 个自定义颜色组合在 `chip-colors-*` 全部逐像素/结构精确相等且重复稳定。
这些证据与 `chip-spring-all` 的旧五个颜色失败一起保留，不属于重新定义基准或组件行为变更。

## 2026-11-06：ScrollField 数字项目的必要文字覆盖

原规则排除普通组件文字，是为了不把字体、排版和栅格后端差异混进无关组件几何验证；日期与时间选择器已有真实文字例外。
本轮维护者明确授权修订历史决定。新增公开 ScrollFieldNumber 对应固定 ScrollFieldDefaults.Item，核心功能是本地化数字和选中排版动画，
非文字替代物无法验证该功能，因此仅把 `scroll-field-number` 纳入真实文字例外。滚动容器、触摸与滚轮套件继续用非文字内容。
数字内容、实际字号与字重分别读取；文字图像按原容差比较，失败保留且不记为已接受差异。
参考明确使用固定 Core 的 `Typography(fontFamily=...)` 30-role 构造和 Expressive motion，与本地 token 输入一致；
无参 Typography 的兼容构造会把 emphasized 映射为普通角色，默认 MaterialTheme motion 为 Standard，不能拿这组不同输入反推产品参数。
另为 ScrollField 自绘 Marker 禁用 Avalonia 自动镜像，使双端 Canvas 的物理坐标相同；窄宽度仍保留真实越界绘图，不隐藏裁剪问题。

## SecureTextField 遮蔽呈现的必要文字覆盖

原规则排除普通组件文字，是为了隔离字体排版与栅格后端，原理由保持有效。本轮维护者明确授权修订历史决定，
新增 `secure-obfuscation` 的目标是默认末字符显示及 1500ms 隐藏，`secure-ime` 检查其真实组合输入状态，必须读取和捕获真实显示字符串。
因此仅这两个 suite 使用合成的普通/补充字符，绝不从应用或用户数据取密码。两端实际输入相同，记录显示文本、
原始 UTF-16 长度与焦点；通过公开 cursor color 入口禁用与该目标无关的光标闪烁，保留所有文字和装饰。
真实文字栅格、禁用状态提交及代码点映射差异继续进入报告，不移动图像、不修改基准或容差。
新字段已通过实际 shaping 后的 glyph cluster 投影完成默认补充字符的一 mask 显示，读数来自该 shaping 的原显示 runs，
不把为原生 UTF-16 索引保留的安全占位文本误当作显示内容。键盘文字使用固定 Core 所需的 AWT KEY_TYPED；
generic KeyDown 的 codePoint 并不触发桌面输入，不能把这类无效注入的空文本作为组件基准。

`secure-ime` 将真实 PlatformContext.startInputMethod 请求交给固定 Core DesktopTextInputService2，
使用上游 PlatformComponent.Empty 代替 OS 窗口，并发送真实 AWT InputMethodEvent；保留完整 adapter、
request.editText 和 Foundation input transformation，不以程序 state.edit 模拟 IME。仅通过反射接入固定内部 adapter，
平台类签名变化时必须重新核对固定源码，不能复制算法或悄悄换成手工事务。Avalonia 使用真实 TextInputMethodClient
的 SetPreeditText；提交先清除预编辑，再发送原生 TextInput。两端记录实际已提交/组合 UTF-16 长度及显示文字。
桌面 OS 的外部输入法窗口未参与，不能把这一验证宣称为真实系统 IME 验收。组合期间与取消后的已知差异进入报告，
不调整字段、图像或容差以隐藏尚未移植的事务分类和原生下划线差异。
