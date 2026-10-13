# ComposeAnimation Catalog

用于预览 ComposeAnimation 的 Avalonia 桌面应用。18 个演示按基础动画、显隐过渡、内容切换、布局动画、共享过渡和时间控制分组，每次展示一个演示。

```powershell
dotnet run --project Catalog/Catalog.csproj
```

卡片上方提供播放速度（0.25× / 0.5× / 1×）和重置。底部整行“调试数据”默认收起；展开后每 100 ms 采样一次，按项目、当前、目标和单位展示。展开栏使用 `AnimatedVisibility` 纵向展开（220 ms）与收起（180 ms），支持途中反转；不重建演示，也不改变舞台尺寸。重置重新创建实例并恢复 1×。

侧栏底部可切换系统、浅色或深色主题。“开发工具”列出当前演示的验证动作；执行前会重置演示，切换页面会取消动作。

## 本地验证

同一个应用同时承载交互预览与自动化。

```powershell
# 单独预览
dotnet run --project Catalog/Catalog.csproj -- --demo properties/basic

# 列出动作，不启动界面
dotnet run --project Catalog/Catalog.csproj -- --list-actions properties/basic

# 全部动画动作：截图、逐帧推进、断言
dotnet run --project Catalog/Catalog.csproj -- --run --all --out artifacts/catalog-actions

# 只运行一个动作
dotnet run --project Catalog/Catalog.csproj -- --run properties/basic --action ToggleWidth --out artifacts/catalog-actions

# 真实目录窗口：主题、导航、展开栏、重置、速度与最小窗口尺寸
dotnet run --project Catalog/Catalog.csproj -- --verify-ui artifacts/catalog-ui
```

`--run` 使用 `ProbeFrameSource`，以 1/60 秒推进动画；固定浅色主题。输出路径为 `<out>/<demo-id>/<action>/`，包含 PNG 和 `manifest.json`，每个 demo 另有聚合 manifest。`--verify-ui` 在真实桌面窗口中使用根级手动帧环境，输出浅／深色及最小窗口截图和 `ui-verification.json`；失败时输出 `failure.txt`。两种模式会短暂开窗并自动退出。

退出码：`0` 成功；`1` 验证失败；`2` 参数错误。不同运行建议使用不同输出目录，避免旧截图混入。

## 添加演示

在 `Demos/` 下添加继承 `DemoControlBase` 的控件，并提供 `public DemoName(DemoContext context)` 构造函数。用 `[Demo("stable/id", Title = "标题", Page = "分组", PageOrder = 0, Order = 0)]` 注册，目录会自动发现。实验性能力标记 `Experimental = true`。

演示遵循同一套呈现规则：

- 根布局用 `demo-root`，先舞台，再 `demo-controls` 操作栏。
- 舞台用 `demo-stage`，设置能容纳所有状态的固定高度；动画内容在舞台内自行布局。避免动画改变操作栏的位置。
- 演示显隐尺寸过渡时，将 `AnimatedVisibility` 在变化轴上设为 `Center` 或 `Top` / `Left`，避免固定舞台的 `Stretch` 抵消尺寸变化。滑动示例显式使用 -1 倍宽／高并裁剪视口；库保留 Compose 的 -0.5 默认比例。
- 使用主题令牌和 `demo-actor`、`demo-well`、`demo-caption` 等样式。按钮只写动作名称，不添加实现说明或常驻状态行。
- 通过 `AddDiagnostic("宽度", () => current, () => target, "DIP")` 提供调试数据。目标可省略；矩形用 `AddBoundsDiagnostics`。不要自行订阅逐帧事件来更新调试文字。
- 自建引擎对象使用基类 `Motion`（`AnimationScope.Get(this)`，重复调用返回同一 Scope、不需要释放；控件移动时自动重建、脱离时结束）的工厂、订阅和 `Launch`。外壳通过继承的 `AnimationScope.DurationScale` 调速；自动化在加载内容之前固定手动根帧源。
- 用 `[DemoAction]` 标记公开、无参数、返回 `void` 或 `Task` 的验证方法。通过 `AdvanceAsync`、`CaptureAsync`、`Assert`、`Click` 复用宿主设施。等待动画收敛应有轮询上限。

`DemoFrame` 负责实例生命周期与展开栏，`CatalogWindow` 负责导航与主题，`CatalogTheme.axaml` 维护统一的视觉样式。已有 demo ID 与动作名称保持稳定，可继续使用原来的调用方式。

## 弹层与新增页面

- `popup/flyout`（显隐过渡）：`Flyout` 中的 `AnimatedVisibility`。`--run` 使用真实桌面平台，弹层是真实的 `PopupRoot`，与窗口共用 `ProbeFrameSource`，因此动作是确定性的。页面只做公开可见的检查（推进、途中关闭后重新打开、所有者上的 `DurationScale` 继承到弹层内容）；内容 Scope 的父级身份与 `AnimationFailed` 计数需要内部 API，由单元测试覆盖（`AnimationScopeRebuildTest.FlyoutContent_ParentsToTheOwnerControlsScope` 等）。
- `lookahead/size-position`（布局动画）：`LookaheadSize` 与 `LookaheadPosition` 的重排、途中改尺寸与运行中换栏。
- `engine/infinite-transition`（时间控制）：`InfiniteTransition` 与 `Transition` 在 0.5×/1×/3× 速度下；改速时 `InfiniteTransition` 从初值重新开始，`Transition` 按新缩放从起点重算播放时间（与上游一致）。
