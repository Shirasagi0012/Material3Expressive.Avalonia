# FloatingActionButton 维护说明

使用入口见 [README](README.md)。多个公开尺寸共享类型，但保留不同上游内容重载、布局与动画分支。

## 固定上游与职责

固定版本见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`；Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
Material3 前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游文件 / 符号 | 本地入口与职责 |
| --- | --- |
| [FloatingActionButton.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/FloatingActionButton.kt)：四尺寸 FAB / Extended FAB、`FloatingActionButtonDefaults` | [FloatingActionButton.cs](FloatingActionButton.cs)、[主题](FloatingActionButton.axaml)、[FabLayout](FabLayout.cs)与[ExtendedFabLayout](ExtendedFabLayout.cs) |
| `FloatingActionButtonMenu.kt`：`FloatingActionButtonMenu` / `FloatingActionButtonMenuItem` / `ToggleFloatingActionButton` | [菜单及条目布局](FloatingActionButtonMenu.cs)、[ToggleFloatingActionButton](ToggleFloatingActionButton.cs) |
| FAB / ExtendedFab / FabMenuBaseline 相关 tokens | [FloatingActionButtonTokenSource](FloatingActionButtonTokenSource.cs)及 defaults；Composable 修正不覆盖原始 Token |
| `FloatingActionButtonElevationAnimatable`、`animateFloatingActionButton` | [FloatingActionButtonElevation](FloatingActionButtonElevation.cs)、[FabVisibilityLayout](FabVisibilityLayout.cs)、[FabMotion](FabMotion.cs) |

两个固定基线的组件实现按原移植记录一致；平台行为仍需按 Core 复核，不可据此假定完整平台一致。
动画依赖版本取[中央版本](../../../../Directory.Packages.props)，当前接入见[公共指南](../../../../docs/GETTING_STARTED.md)。

## 已确认 API 与布局约束

[拆分前确认记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/FloatingActionButton/README.md)
保留 2026-10-03 维护者批准的尺寸化按钮类型、菜单 Button/Items 槽、受控
IsChecked/CheckedProgress/CheckedChangeRequested 与 AnimatedVisible。
原因是用 Avalonia 类型/属性表达 Compose 多入口，不是合并不同动画语义。

- 图标 FAB 的容器为 56/40/80/96 DIP，Small 外层分配 48；Extended 为 56/56/80/96 DIP 高
- Large 图标 36 DIP、Medium/Large Extended 的 12/16 DIP 间距保留 Composable 修正；不把原 Token 32 与 16/20 改掉
- Default Extended 使用整数宽度、分开的展开/收起 spring 与 alpha，返回 Visible 的中断默认 spring；Expressive 分支保留浮点宽度进度、FastSpatial/FastEffects，完全收起才移除文字槽
- 最小交互空间及内部对齐读数遵循[共享决定](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)，只预留布局空间

## 动画、输入与生命周期

Elevation 记录最后一个实际 press/focus/hover，而非仅从相同高度倒推状态。
Tonal elevation 固定使用配置 Default；更换配置先 snap 当前目标，再处理新交互。
动画优先级绑定随 scope 释放，不能让多个 FAB 共用运行状态。

Toggle 保留初始外层占位，内层朝逻辑 TopEnd 收缩；Oklab 颜色、动态圆角、固定阴影及显式
ripple 半径分别处理。内容按整数除法居中，图标尺寸按物理像素取整；反向 checked 按
AnimateAsState 取消并重启，不能用保留速度的普通 retarget 代替。
激活先发状态请求，后调用原生 Button；应用拥有 IsChecked，automation 的 IToggleProvider 同样只请求切换，禁用拒绝。

菜单保留底部开始的整数 stagger、条目独立 width/alpha、自然宽度和逻辑对齐。
增项在放置阶段初始化动画，条目 alpha 独立使其图层失效，不能依赖菜单总尺寸改变才重画。
受限列表使用内部 ScrollViewer/ScrollContentPresenter，保留原生滚轮、惯性与焦点带入视口，
收起期间关闭输入，Button 不滚动。显隐 alpha 为零才退出布局，部分透明时整层合成阴影和填充；
默认缩放枢轴在逻辑 BottomEnd 内缩一个物理像素，RTL 由视觉树镜像实现。

Button 命令、焦点与 activation 复用框架；[Enter 的共享适配](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)不改变本组件受控状态所有权。

## 已接受暂缺与历史依赖问题

维护者于 2026-10-04 确认 IconSize / IconColor 绑定入口。任意位图或复合内容的整层 SrcIn tint
留作独立基础能力任务；任意内容仍继承环境 Foreground，不把显式图标绑定当作完整 animateIcon。

原移植修正过 ComposeAnimation spring 预计时长的整数毫秒截断，否则 threshold=1 的菜单 stagger
会在帧边界多延后一帧。该原因须保留，但旧本地包版本不是当前安装要求，也不能根据依赖升级省略反向/中断回归。

## 验证与边界

[FloatingActionButtonTests](../../../../tests/Material3Expressive.Tests/FloatingActionButtonTests.cs) 覆盖尺寸、
显隐、状态请求/automation、颜色角色、图标绑定、整数放置、受限菜单焦点、detach / reattach。
[Avalonia 用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/FloatingActionButtonCases.cs)与
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/FloatingActionButtonCases.kt)
覆盖四尺寸/内容重载、主题、约束、RTL、elevation、输入、展开/反向、Toggle、菜单增项和显隐。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~FloatingActionButtonTests
./tests/VisualParity/parity.ps1 self-test --family floating-action-button --require-match
./tests/VisualParity/parity.ps1 prepare-reference --family floating-action-button
./tests/VisualParity/parity.ps1 run --family floating-action-button --require-match
```

拆分前自检记录中 FAB 场景重复采集稳定且位于既定 raster 容差内；这是旧基线环境记录，不是本次执行结果。
附加 `button.dynamic.hover.elevated` 的 Double/Float elevation 精确读数仍有差异，不能算该 Button 用例通过。
Raster withinTolerance 不等于原始像素相同；几何内容不评价文字栅格，headless 不替代真实桌面验收。
结果解释见 [VisualParity](../../../../tests/VisualParity/README.md)。
