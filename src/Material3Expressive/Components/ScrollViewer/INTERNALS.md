# ScrollViewer 维护说明

使用入口见 [README](README.md)。这是 Avalonia / SmoothScroll 的集成主题，
不把它列作独立 Compose Material3 ScrollViewer 的等价移植。

## 实现依据与模板契约

[ScrollViewer.axaml](ScrollViewer.axaml) 保留原生 owner，`PART_ContentPresenter` 使用
`SmoothScroll.Avalonia.Controls.ScrollViewerPresenter`；原生 API 合约对应项目固定 Avalonia
12.1.5。SmoothScroll 原 NuGet 12.0.0.1 缺少异步释放事务；2026-11-09 本轮明确授权下，
从该制品 nuspec 的固定源码提交导入 [仓内模块](../../../../modules/SmoothScroll.Avalonia/README.md)，
通过 ProjectReference 使用本地补丁 12.0.0.2。固定来源、许可证及新增协议保留在模块目录，
不回到旧子模块浮动 HEAD。依赖准备只维护在[公共接入](../../../../docs/GETTING_STARTED.md)。

模板必须保留 `PART_HorizontalScrollBar` / `PART_VerticalScrollBar`，让 Avalonia 接入 range、value、
paging、visibility、AllowAutoHide 和 deferred drag。Grid 的 Auto 行列预留可见滚动条厚度；
`Disabled` 同时禁用轴向滚动与无限测量，其余模式允许无限测量，不能只改滚动条可见性。
owner 的 Content/Template、Padding、背景、内容对齐、Offset、chaining、inertia 与 snap 参数
都传给 presenter。逻辑滚动需保留内容声明的单位；TextField 的 PART_TextPresenter 合约不能被平滑滚动替换。

## 输入与合成接入

普通指针、直接滚轮与惯性逐帧位移共用 SmoothScroll tracker 增量消费入口；
AppBar 与 PullToRefresh 通过内部 MaterialScrollCoordinator 共用纵向参与槽，按实际祖先顺序
执行 pre、正文、反序 post；没有参与者时 pre/post 消费为零，横纵位移一起提交。
正文、程序化定位动画和几何校正使用有效范围。普通 touch/pen 保留弹性回弹，AppBar 主正文
按现有夹取规则处理；直接 Offset、BringIntoView 和程序化定位动画不经过 pre/post。

参与者协议来自 `SmoothScroll.Avalonia.Interaction`，合成输出来自
`SmoothScroll.Avalonia.Composition`。AppBarScope 根据 presenter 的实际能力连接，不核验模板身份。
屏幕组合及共享职责的确认依据见[普通物理滚动与 AppBar 的合成接入](../../../../docs/DECISIONS.md#普通物理滚动与-appbar-的合成接入)，
不要把这项决定扩展成修改原生所有滚动事件、布局坐标或程序化定位语义的许可。
[AppBar 维护说明](../AppBar/INTERNALS.md)保留组件相关消费顺序、嵌套注册与验证入口。

PullToRefreshBox 连接时临时贡献 `VerticalTouchSlop=18 DIP`，仅垂直 touch/pen 使用固定 Core 的
阈值识别与首次余量投递；释放或换视口后恢复原属性来源。普通滚动器保留原生阈值策略。
异步释放仅在有消费者时启用：先传递最新位移，再等待 UI pre-fling，返回剩余速度；合成线程不阻塞。
普通滚轮沿用原路径，不能用 idle 推断刷新释放。具体语义与未覆盖范围见 PullToRefresh INTERNALS。

## 验证与边界

[ScrollViewerTests](../../../../tests/Material3Expressive.Tests/ScrollViewerTests.cs) 检查主题单独加载、
物理视口、范围/绑定、Auto 收回空间、嵌套 chaining、逻辑内容和 TextField 集成。
[AppBarSmoothScrollTests](../../../../tests/Material3Expressive.Tests/AppBarSmoothScrollTests.cs)与
[AppBarCompositionTests](../../../../tests/Material3Expressive.Tests/AppBarCompositionTests.cs)
覆盖参与者和合成接入。组件没有独立 Compose ScrollViewer parity suite。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ScrollViewerTests
```

旧源码与子模块验收的环境及详细数字保留在[拆分前记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/ScrollViewer/README.md)。
其中旧 SmoothScroll 子模块提交的测试不自动证明当前发布包或最新依赖结果。
Catalog headless 检查在位置回读及同步布局后验证了交互，不证明动画期间布局坐标逐帧同步；
真实桌面、触屏/触控笔和屏幕阅读器仍需另行验收。
