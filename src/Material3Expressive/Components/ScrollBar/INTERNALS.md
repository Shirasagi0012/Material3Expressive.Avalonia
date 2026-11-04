# ScrollBar 维护说明

使用入口见 [README](README.md)。原生 ScrollBar 保留 Avalonia 滚动能力；NonInteractiveScrollBar 提供独立的 Material3 只读指示。

## 1. 上游基线与职责映射

[upstream-baseline.json](../../../../docs/upstream-baseline.json) 固定 Core 为
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。样式依据是
[Scrollbar.skiko.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/foundation/foundation/src/skikoMain/kotlin/androidx/compose/foundation/Scrollbar.skiko.kt)
的 `ScrollbarStyle`、`defaultScrollbarStyle` 和 `OldOrNewScrollbar`；它属于 Foundation，
不是 AndroidX Material3 `nonInteractiveScrollbar`。
原生范围、几何、输入与 automation 按 Avalonia 12.1.2，源码 tag 提交
`d3c867a9e2de379249b03dbeb3495bd7f076a81a` 核对。

- [ScrollBar.axaml](ScrollBar.axaml)：水平/垂直模板与原生命名部件、菜单资源引用
- [ScrollBarAppearance.cs](ScrollBarAppearance.cs)：厚度、最小滑块长、两态颜色、时长附加属性
- [ScrollBarThumb.cs](ScrollBarThumb.cs)：观察实际 hover / drag 并中断更新颜色，不拥有原生数值状态
- [ScrollBarTokenSource.cs](ScrollBarTokenSource.cs)：OnSurface 派生默认色；`Comp.ScrollBar` 是本项目为 CMP Foundation 样式建立的主题槽，并非上游生成的 Material3 Token

厚度、最小长度、颜色和时长的基线只保留在 ScrollBarTokenSource，附加属性不重复数值默认值。
方向样式只选择内部尺寸、菜单和模板候选，公开 Width/Height/ContextFlyout/Template 由普通
主题 Setter 绑定；用户 Style、Binding、LocalValue 不被方向触发器压制。

`NonInteractiveScrollBar` 独立移植固定 AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与
Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的 Material3 `Scrollbar.kt`，两文件相同。
本次剩余移植任务授权新增非破坏性组件 API；此入口不改变上述原生 ScrollBar 决定。

## 2. 关键机制与架构契约

依据[拆分前确认记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/ScrollBar/README.md)，
维护者于 2026-10-04 选择“原生交互 + Material 外观”：

- 用 Material3 OnSurface 替代 CMP 独立默认黑色，保留 0.12 / 0.50 opacity
- 固定厚度，hover / drag 直接改变颜色目标，不由 IsExpanded 驱动扩宽或隐藏
- 保留原生菜单、LargeChange、滚轮/键盘/拖动/捕获、延迟滚动与 automation
- 无需滚动时沿用原生 Visibility 和 ScrollViewer 布局语义，包括 Auto 收回空间及原模板覆盖策略
- 使用 CornerRadius；特殊形状与反向轨道由 ControlTheme 定制，不新增 Shape / reverseLayout API
- 保留 Avalonia Track 的位置、长度、最小长度边界及布局取整，不要求 CMP 几何算法

模板保留 `PART_Track`、`PART_Thumb` 与透明分页按钮，不新增箭头、轨道、ripple、焦点环或禁用颜色。
hover 检查 owner 而非仅 Thumb；拖动离轨保持高亮，到原生释放或取消才回落。
颜色中断从当前值继续，模板替换、卸载、配置改变必须清理或更新作用域和订阅，不能影响原生绑定。
动态引用基础主题的 `VerticalScrollBarContextFlyout` / `HorizontalScrollBarContextFlyout`，
不复制菜单命令或文案；MaterialTheme 单独加载不补造菜单。

### Material3 只读指示

`ScrollIndicatorState` 的三个观察值通过 ContentSize / ViewportSize / ScrollOffset 绑定表达，不引入公共状态框架。
原生 Avalonia 值为 DIP，绘制入口转换为上游整数物理像素；其余几何保留 Float 计算与运算顺序。
组件在 Grid 中覆盖视口，不改变内容测量；自身不消费输入。垂直逻辑末端、水平底边和 RTL 位移独立计算。
轨道是矩形，滑块半圆角；offset 不额外夹取。内容无须滚动时先退出，不观察 offset。

配置 Token 来自 NonInteractiveScrollbarDefaults，不伪称生成的组件 Token。
ThumbColor 的默认来源与 ThumbOpacity 分开绑定，局部 converter 保留 copy(alpha) 的替换语义；
不能使用 WithAlpha 乘以原始 Outline alpha。公开 ThumbColor 的本地值不再乘默认 0.7。

淡出保留容量 1 的事件通道：绘制观察到新 offset 后 snap 到 1，等待期间的新事件重新计时；
进入 animateTo(0) 后滚动取消旧任务并 snap 回 1。普通等待复用 AnimationScope.DelayAsync，
不以动画帧替代时间；Tween 使用默认 FastOutSlowIn。配置更新对应 wrapper 重新 delegate，重置 alpha 并基线化 offset；
尺寸/offset 更新仅失效绘制。卸载取消通道、等待与子作用域，重挂对当前 offset 重新基线化。

## 3. 已确认决定与已知差异

维护者明确要求原生 ScrollBar 不做 Compose parity tests，因此不能把 headless 通过写成双端像素或交互一致。
真实桌面、触屏/触控笔与屏幕阅读器未经上述 headless 检查证明。测试来源和历史确认范围保留，
后续结果需按实际执行范围报告，不能从测试源码或历史确认推定当前通过。

默认延迟的双端动画尚未验收：固定 Core 的 `FlushCoroutineDispatcher` 实现
`scheduleResumeAfterDelay`，却没有实现 `invokeOnTimeout`。Scrollbar 使用 `withTimeoutOrNull`，
其 timeout 不由现有 TestCoroutineScheduler 驱动，受实际执行时间影响；首轮曾出现 Compose 重复图像不稳定。
零延迟额外场景独立检验 Tween、反向/再滚动及配置重建，不替换原有默认延迟场景。
推荐在共享参考宿主中显式接管 timeout 调度并补充框架自测；须维护者决定时间源接入范围，
不能通过延长本地等待、修改基线或跳过重复捕获来掩盖。该方案未批准、未实施。
调用示例仍为现有 `host.advanceTo(400_000); host.frame(400_000)`：若最后一次 offset 变化在 0，
应由普通虚拟时间触发 400ms timeout，并从随后动画帧开始采样淡出。
接管范围会影响参考端所有 withTimeout 使用者和取消顺序，需先补普通延迟/timeout/帧三者的顺序自测。

## 4. 验证入口

[ScrollBarTests](../../../../tests/Material3Expressive.Tests/ScrollBarTests.cs) 检查主题、Token、原生分页/
拖动绑定、菜单复用、整轨 hover、离轨 drag、capture loss、颜色反向、配置更新和卸载重挂。
[ScrollViewerTests](../../../../tests/Material3Expressive.Tests/ScrollViewerTests.cs)补充实际 owner 集成。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ScrollBarTests
```

新增 `NonInteractiveScrollBarTests` 检查真实绘制、两轴 RTL、范围和越界 offset、延迟续期、淡出中断、
配置重建、卸载清理、颜色 alpha 替换及真实 ScrollViewer 三项绑定。
`non-interactive-scrollbar` 双端族独立调用固定 Material3 API，覆盖静态几何及连续 16ms 动画时间线。
Catalog 的 ScrollBarDemo 同时保留原生示例和绑定真实滚动内容的双轴只读指示。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~NonInteractiveScrollBarTests
./tests/VisualParity/parity.ps1 self-test --family non-interactive-scrollbar --require-match
```
