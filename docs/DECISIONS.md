# 共享能力的已确认决定

本文件只记录跨组件的共享能力决定。组件行为、适配与确认依据随各组件 README 保存；
通用职责与接入规则见 [ARCHITECTURE.md](ARCHITECTURE.md)，VisualParity 决定见其[测试目录文档](../tests/VisualParity/DECISIONS.md)。

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

## 共享交互反馈与键盘激活职责

决定：`MaterialInteractionController` 的键盘输入处理只维护交互反馈，激活由控件自身负责。
共享控制器不拦截 Enter，不通过 automation Invoke 或其他路径代为点击，也不维护组件类型排除名单。
原因：反馈接入不应改变应用命令的执行时机；接管 Enter 会与原生 Space 和导航项的键盘处理形成多套激活路径。
做法：移除专用 Enter 激活状态和 KeyUp 点击分支，保留 Space / Enter 的按压反馈，
并在释放、失焦或禁用时清理反馈。真实 Compose 参考、采样与比较规则保留，激活时机差异如实报告。
影响与范围：共享控制器的键盘反馈接入遵循上述职责；普通 Button / IconButton 保留 Avalonia 原生激活，
Enter 按下激活与固定 Compose 的松开激活不同，已作为框架适配接受，Space 遵循原生 ClickMode。
导航项已有的释放激活保留；该决定不统一各组件的键盘行为。具体行为见
[Button](../src/Material3Expressive/Components/Button/README.md#默认值与接入)与
[IconButton](../src/Material3Expressive/Components/IconButton/README.md#形状与接入)说明。
确认：2026-10-03，维护者要求“不需要Enter拦截了。然后将决策记录在案”，
并进一步要求移除共享控制器中非必要的 Enter 特殊处理；此决定替代此前在 KeyUp 经 automation Invoke 激活的决定。

## Surface 均匀圆角描边

决定：内部 SurfaceOutline 对均匀 Thickness / CornerRadius 直接移植固定 Compose Border.kt 的
物理像素 ceil 与内缩 stroke；描边作为内容上方的视觉层，不消耗内容尺寸。
小圆角使用外圆角减内矩形，大宽度使用填充；非均匀边框保留 Avalonia 原生 Border 路径。
原生零 Thickness 保留无描边语义，公共 API 不变。
原因：Chip 深色静态用例暴露角部差异，已有原生 Border 不符合固定上游的像素取整。
范围：所有 MaterialSurface 调用方；Surface / Button / Chip 的相关渲染和回归需要共同检查。
确认：2026-10-03，维护者明确批准共享 Surface 描边修正及 Surface / Button 回归。

## Core Desktop 原生焦点反馈

决定：共享控制器 Focused 跟随所有原生焦点，包括鼠标点击获得的焦点；控件继续负责激活。
指针按下反馈在 Button 原生类处理之后观察，保留 Focus → Press 的交互顺序。
原因：固定 AndroidX 的 RequestFocusOnClick 返回 false，固定 Core Desktop 返回 true；
仅接受 Tab / Directional 焦点会让 Chip 点击释放后错误地恢复 hover elevation。
范围：共享控制器调用方，不改变公开 API 或已确认的键盘激活职责。
确认：2026-10-04，维护者批准按 Core Desktop 修正共享焦点反馈并回归 Button / IconButton / 导航项。
实现同时按固定 Core `Indication.skiko.kt` 过滤 indication 的焦点源：首次鼠标聚焦不进入
状态层，已经显示的键盘焦点层保留到失焦；输入模式改变不合成 Focus / Unfocus。
内部按窗口保存输入模式，原生 Tab / Directional 导航接入 Keyboard，指针按键接入 Touch，
程序化聚焦沿用当前窗口模式。elevation 的原始交互源仍保留所有焦点。

## 本次保留 Tween / Bezier 精度差异

决定：Chip 移植继续消费 ComposeAnimation 0.2.1-alpha，不修改其 FloatTweenSpec /
CubicBezierEasing 的 double 运算。固定 Compose 的 Float 运算和不同求根路径可造成约
0.0000006 DIP 的 elevation 差异。原始 motion 事实继续精确比较并报告不同。
范围：本次 Chip 及共享回归所观察的数值差异，不接受时序、布局或可见效果的其他偏离。
确认：2026-10-04，维护者在依赖修正与保留差异的具体方案中选择“本次保留并记录该精度差异”。

## 本次保留阴影坐标接入差异

决定：本次不改变 MaterialSkiaShadow 的 DIP 路径及 density CTM，保留其与 Compose Desktop
物理像素路径 / 相应 CTM 的 raster 差异。公共 DIP elevation、光照和布局不变。
原因：透明 AssistChip 的 6 DIP elevation 暴露内部阴影差异；独立 Skiko 原生探针使用同一
轮廓、光照及标志，分别以 DIP 路径 + 4× CTM 和物理像素路径 + 1× CTM 绘制，复现两端结果。
原生 tessellator 对路径坐标的敏感性使这两种坐标接入不等价。
范围：本次观察到的阴影 raster，不接受其他布局、颜色或时序偏离；保留原始 PNG 和比较失败。
确认：2026-10-04，维护者在坐标适配方案与保留差异中选择“本次保留并记录阴影差异”。
