# Shared bounds：Runtime 接入与重定向

本文记录第二阶段 Runtime bounds 原型。正式实现后来已接入 `SharedTransitionHost`、
`SharedElement` / `SharedBounds` 和协调器；原型中的显式两端与 Scratchpad 限制不再代表当前 API。
当前使用方式见 [SharedElement / SharedBounds](SHARED_TRANSITION.md)。

## 准备到动画的交接

1. 修改源端 Visible 之前获取 preparation lease。
2. 等待目标实际 Arrange，取得同一宿主坐标系下的两个矩形。
3. 在退出 Scope 的 Transition 注册 deferred Rect，强制初值为源端当前显示矩形，初速度为零。
4. 如有独立图片，注册实际控制图片弧线的进度动画。全部注册后立即释放 preparation。
5. Runtime 的真实动画负责保留退出内容，决定完成时刻并处理 DurationScale。窗口回调只观察布局、读取动画值、刷新画刷。

首次强制初值用于从已经存在的源端开始；后续目标布局变化调用 `bounds.Update(...)`。
不要在重定向时再次使用零速度的 forced-initial 重载，否则会丢失动量。

## 重定向约定

- RectConverter 的四维与 Compose 一致，依次是 Left、Top、Right、Bottom，单位为 DIP；原始速度向量对应四条边的 DIP/s。公开 API 仍使用 Avalonia.Rect（X、Y、Width、Height），转换回 Rect 时宽高由相对边相减得到；因此类型化 Velocity 的 Width/Height 仍表示宽高变化率。
- Rect 弹簧阈值同样按四条边解释。每条边使用 t 时应传 `new Rect(t, t, 0, 0)`，也可直接使用 `VisibilityThresholds.Rect`（每条边 0.5 DIP，保留与 Compose 1 px 的单位/阈值适配差异）。`Animatable<Rect>.UpdateBounds` 的上下界也约束四条边，而非独立约束宽高。
- 未发布阶段已将旧 X/Y/Width/Height 向量统一迁移为边缘向量：普通同进度线性插值的几何结果保持一致；Arc/弧线关键帧现在分别驱动左上角和右下角，弹簧结束时间及原始速度向量的后两项可能变化。自定义阈值和手工构造的四维速度须按上述语义编写。
- 布局只产生新目标，不读取动画尺寸，避免尺寸反馈循环。
- `Update` 从最后一次 Runtime 采样的值和速度接续。使用同一 Runtime 时间原点，不能混入窗口 Stopwatch 时间。
- 未改变的目标不会重启轨迹。目标改变时，SpringSpec 接收当前速度；运行中的 Tween 等非 SpringSpec 转用 Runtime 的 interruption spring。
- 反向时允许短暂继续原方向，再减速转向。速度连续不代表加速度连续，也不保证固定时长。
- 试验采用无回弹、stiffness=16、各维阈值 0.1 DIP 的慢弹簧以便观察。它是观察配置，不是已确定的产品默认值。
- 到达结束阈值后使用 Runtime 的精确目标值完成交接。阈值收尾与中断瞬间的速度连续是不同约束。

## 最后一帧和取消

仅在下一次窗口帧回调检查 `IsFinished` 不够：Runtime 的通知可能先触发 AnimatedVisibility 结束 Scope。
试验通过 `scope.Own(...)` 注册清理，在 `PresentedContent` 移除之前读取最后的 bounds / arc 值并交接绘制：
目标 Gate 恢复，源 Gate 保持隐藏，overlay 清除，动画注册释放。

外部 Detach 或重置也会触发清理，不能一律视为成功；检查动画是否完成，并用会话 generation 拒绝过期回调。
当前正式协调器还会验证布局 revision；`RemeasureToBounds` 额外验证 Session generation、
端点的 lookahead Context 身份和目标 Snapshot.Version，并等待真实 capture 到达最终矩形。
自动局部域和显式域之间可以配对，因为两端分别转换到同一个 SharedHost 坐标系；它们的
目标快照不会合并。此处其余内容仍是显式两端原型的历史说明。

## 验证与边界

自动测试验证迟到 60 秒后的动画注册、反复变更目标、相同目标不重启、可见性反向、Tween 中断转弹簧、
零时长、真实动画保留源端，以及 Scope 清理发生在内容离树之前。连续性测试读取新轨迹起点，并以微小时间差验证导数，
而非只比较缓存的 Velocity 属性。Detach 测试单独验证取消和资源释放。

桌面 smoke 覆盖正常往返、布局改变往返、嵌套往返、无保留失败对照、超时、取消、零时长。
每次布局重定向记录新轨迹起点的值和速度误差；这些数值断言不代替人工视觉验收。

自动匹配、跨 Scope 的通用会话所有权与刷新性能测量已在后续正式实现中扩展；
交互式 seeking 仍未实现。
后续第四阶段已补充 [目标内容 / 双内容绘制及失败回退](SHARED_CONTENT_SEMANTICS.md)。
图片仍是相对父卡片的人工弧线，不能据此宣称完整 SharedElement 已实现。
