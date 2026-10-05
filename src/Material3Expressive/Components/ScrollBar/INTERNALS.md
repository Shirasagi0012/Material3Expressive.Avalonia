# ScrollBar 维护说明

使用入口见 [README](README.md)。组件保留 Avalonia 原生滚动能力，内部只负责 Material 外观和颜色动画。

## 固定依据与本地入口

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

## 已确认适配与机制

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

## 验证与边界

维护者明确要求本组件不做 Compose parity tests，因此不能把 headless 通过写成双端像素或交互一致。
[ScrollBarTests](../../../../tests/Material3Expressive.Tests/ScrollBarTests.cs) 检查主题、Token、原生分页/
拖动绑定、菜单复用、整轨 hover、离轨 drag、capture loss、颜色反向、配置更新和卸载重挂。
[ScrollViewerTests](../../../../tests/Material3Expressive.Tests/ScrollViewerTests.cs)补充实际 owner 集成。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ScrollBarTests
```

真实桌面、触屏/触控笔与屏幕阅读器未经上述 headless 检查证明。测试来源和历史确认范围保留，
后续结果需按实际执行范围报告，不能从测试源码或历史确认推定当前通过。
