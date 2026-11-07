# ScrollField 内部机制与维护说明

## 1. 上游基线与职责映射

AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ScrollField.kt` 相同。
桌面 Pager 依据同一 Core 提交的 Foundation `pager/PagerState.kt`、`PagerMeasure.kt`、
`gestures/snapping/PagerSnapLayoutInfoProvider.kt`、`SnapFlingBehavior.kt` 与 `SnapPosition.kt`。

| 上游职责 | 本地实现 |
| --- | --- |
| `rememberScrollFieldState` / `ScrollFieldState` | `ScrollFieldState`：100,000 页、当前周期定位、目标与当前选项 |
| `VerticalPager` 的固定页与中心吸附 | `ScrollFieldPanel`：可见页按身份实现、整数物理像素布局 |
| `animateScrollToPage` / `performScroll` | State：远距离预跳、默认 spring、局部整数位置与小数累积 |
| `ScrollFieldDefaults.Item` | `ScrollFieldNumber`：FastSpatial 排版与颜色动画、本地数字符号 |
| Core `MouseWheelScrollingLogic` | `ScrollFieldWheel`：输入与吸附；共享 `MouseWheelScroll`：合并、即时阈值、线性插值、结束等待 |
| `clearAndSetSemantics` / `setProgress` | 字段 peer：单节点 RangeValue、目标页范围与选项描述 |

## 2. 关键机制与架构契约

选项数必须为正。初始页面为 `50,000 - 50,000 % itemCount + coercedIndex`，保持有限虚拟页范围。
选项 setter、点击和动画均相对于当前页的周期计算；点击下一周期的零选项可能返回本周期起点，不能改成最近路径。
方向键则从目标页面增减，因此连续按键能在未完成的动画上继续导航，且在选项边界循环。

页面高度由 Float `200f / 3f` 换算成整数物理像素；中心位置分别做整数除法 `viewport/2 - page/2`。
只实现可见页，页面身份不随当前选项换成槽位身份。背景圆角不裁剪项目，Pager 裁剪主轴，横轴保留上游的 30dp 阴影空间。
所有距离运算使用页面索引和局部偏移，避免 50,000 页附近的 Float 绝对位置精度损失。
默认数值项目的两条 AnimatedState 都使用帧末提交、取消后重启；默认颜色也使用 FastSpatial。
动画贡献在卸载时释放；自定义模板只收到选中状态，不强加默认数字动画。

点击承载复用 ClickableSurface 的输入，专用页面模板把内容与反馈 Surface 并列，保留 clickable Box 的不裁剪内容语义；不增加最小交互布局留白。页不能通过键盘独立聚焦。
状态一次只挂载一个字段；替换或卸载中止其运动，不影响旧状态保留的当前值。
字段的 InteractionSource 只对应上游显式 focusable，不能把内部 Pager 的 drag 或项目 press 混入它。
触摸超过 18 DIP slop 后捕获，鼠标按钮不拖动。吸附复用共享 SnapFling，atMost(1) 的 approach 为零，
snap 使用 400 stiffness 与 1 物理像素阈值，程序导航仍使用默认 Float spring。
吸附 remaining distance 回调按 PagerWrapperFlingBehavior 更新目标页，不能只等当前位置跨过中点才更新目标。
输入保持 UserInput 优先级；默认异步导航遇到输入会取消。显式 SelectedOption 请求取消当前滚动并定位，
但不取消仍按下的指针手势；后续位移重新进入 UserInput。滚轮接收任务也保留取消期间新到达的事件。

滚轮采用 6dp 即时阈值、1dp/ms 线性速度、100ms 最大动画和 50ms 结束等待；等待事件期间保持帧请求，
通过动画根 TimeProvider 处理超时。每帧合并待处理 Delta；沿用 AnimationState 的时间戳进行连续重定向。
速度采用差分 Impulse 算法，不能复用触摸的 LSQ2 绝对位置算法。事件坐标与速度在物理像素中计算。
公开 PointerWheelEventArgs 没有 AWT scrollAmount、block-scroll 或 precise-device 标志，因此当前严格对应
Core 无原生元数据事件的分支（amount=1、平滑开启），没有猜测设备类型。Windows 的长度/20、Linux 的 sqrt(length)、
macOS 的 10dp 换算分别保留；只有 Windows 已运行双端，系统滚动行数、整页滚动和非 Windows 精密设备元数据尚未接入。

## 3. 已确认决定与已知差异

本轮维护者明确授权新增组件、API 和必要共享能力。采用独立状态与模板上下文，保留上游数据关系；
初次引入时没有将 TimeScroll 于 2026-10-11 接受的原生 DateTimePickerPanel 适配扩大到通用字段，也没有迁移该历史调用方。
2026-11-07 后续批次依据维护者本轮授权让 TimeScroll 复用此字段，原决定理由与模板迁移见 [时间选择器维护说明](../TimePicker/INTERNALS.md)。
共享 SnapFling 提取范围见 [共享决定 §30](../../../../docs/DECISIONS.md#30-pager-组件的吸附动画复用)。
没有破坏性公开 API 调整。

尚未实现：原生滚轮设备元数据、触控板 Pan/惯性专用协议、跨组件 nested scroll、通用 Pager API、自动状态保存与恢复。
未经验证：实体触摸、非 Windows 滚轮及操作系统辅助技术客户端。
已观察到而未接受的差异：触摸首次按压时的波纹启动相位；数字文字的排版/字体栅格。对应真实双端失败用例保留。
程序赋值的即时布局与 Compose 请求重测/重组的提交边界可能产生差异；双端用例保留请求后的立即采样。
未为这些范围接受近似效果或改变比较容差。

## 4. 验证入口

- `tests/Material3Expressive.Tests/ScrollFieldTests.cs`：状态、虚拟化、方向键、反向/取消、模板、周期边界、自动化与生命周期。
- `ScrollFieldCases.cs` / `.kt`：真实双端自定义几何内容，覆盖明暗、RTL、高度、窄约束、项目数与动画轨迹。
- `./tests/VisualParity/parity.ps1 self-test --family scroll-field`；默认数字的栅格不在该无文字套件内。
- `scroll-field-touch`：快速/停顿/小幅/反向真实触摸、拖动中的异步导航与强制定位，LTR/RTL；状态和位置精确一致，首次按压波纹仍有图像差异。
- `scroll-field-wheel`：单次、小量、大量、合并和反向滚轮，LTR/RTL；10 例结构读数精确一致，图像通过既有容差。
- `scroll-field-navigation`：方向键、重复按键、鼠标点击当前项/下一项/跨周期项，LTR/RTL；12 例通过既有门禁。
- `scroll-field-number`：真实默认数字、三种 locale、选中与反向动画；文字内容、字重和 Float 插值逐项检查，栅格差异不作通过处理。
- Catalog `ScrollFieldDemo` / `--scroll-fields`。

2026-11-06 本轮实际结果（产物位于 `artifacts/visual-parity/runs/`）：

- `20261106-151241-44853ff7`：32 个布局/程序动画用例全部精确相等，包括窄宽约束。
- `20261106-151030-b2d72215`：12 个键盘/鼠标用例通过，无执行错误且重复稳定。
- `20261106-151834-63c40789`：10 个滚轮用例通过，包含取消后事件接收的生命周期修正，无执行错误且重复稳定。
- `20261106-151455-822de2de`：6 个数字用例的文字、字号、字重读数精确一致；图像仍为 different。
  检查真实两端图像与差分图可见字形边缘差异，没有放宽容差或替换字体来绕过比较。
- `20261106-150742-8a2df250` 的触摸用例保留首次按压 ripple 相位失败；其中强制定位曾有真实行为偏差，
  修正后 `20261106-151532-a8b1cc4f` 的 LTR 与 `20261106-152022-c4ca5c59` 的 RTL 读数全部一致，
  只在首次按压反馈保留图像差异。
- `20261106-152104-8ab5cc69`：共享吸附提取后的 Carousel fling 21 例全部通过，无执行错误且重复稳定。

解决方案构建成功；控件库全量 2,371 项、ComposeAnimation 974 项、VisualParity 自身 134 项测试通过。
随后对开始拖动通知中卸载控件的指针捕获补充回归，ScrollField/Carousel 相关 50 项再次通过。
