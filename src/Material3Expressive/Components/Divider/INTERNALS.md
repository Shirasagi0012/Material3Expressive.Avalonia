# Divider 实现说明

使用入口见 [README](README.md)。本组件保留原生 Separator 作为公开控件，用主题和三个附加参数
表达固定上游的两种 Divider，不增加通用布局框架。

## 固定上游与本地入口

基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）与 Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）。下述 Material3 源码路径相对于
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

两端固定 `Divider.kt` 相同，`tokens/DividerTokens.kt` 为 v0_117。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `Divider.kt`：`HorizontalDivider`、`VerticalDivider` | [MaterialDivider.cs](MaterialDivider.cs) 的 Orientation/Thickness/Color；[Divider.axaml](Divider.axaml) 的原生 Separator 主题 |
| `Divider.kt` 的 Canvas + drawLine | [DividerPresenter.cs](DividerPresenter.cs) 的布局与 Skia draw operation |
| `tokens/DividerTokens.kt` | [DividerTokenSource.cs](DividerTokenSource.cs)、[MaterialTokens.Comp.Divider.cs](../../Tokens/MaterialTokens.Comp.Divider.cs) |
| Core foundation-layout `Size.kt` 的 fill/约束；ui-graphics `CanvasDrawScope.drawLine` | 有界方向填满、厚度布局取整与原始 Float stroke 分开计算 |

固定上游读取方法见 [AGENTS](../../../../AGENTS.md#上游源码)。不实现已废弃的 Divider 函数入口，
尤其不能将其零厚度布局兼容逻辑混入新 Horizontal/VerticalDivider。

## 布局与绘制不变量

XAML 只负责主题与 TemplateBinding，内部 DividerPresenter 负责 Canvas 度量和绘制。
Orientation/Thickness 改变同时触发布局与绘制失效，Color 只影响绘制。

布局厚度按 Float density 和 roundToPx 取整，再服从父约束；绘制宽度用原始 Float thickness.toPx。
二者不能合并。水平线从 `(0, thickness/2)` 开始，竖直线从 `(thickness/2, 0)` 开始，
Butt 端帽、不因额外横截面布局空间重新居中。填充方向无界时返回零自然长度，不自行取任意默认长度。

Thickness=0 保留零布局厚度与 Skia hairline，不能换为废弃 API 的 1/density 布局。
Avalonia DrawingContext.DrawLine 会过滤零宽 Pen，因此通过 Skia lease 绘制，明确需要 Avalonia.Skia。
绘制操作 Bounds 包含画布外 stroke，使零厚度和受限尺寸仍能提交；模板不裁剪，祖先仍可裁剪。
负厚度保留上游零布局/Skia Paint 行为，不修正为默认值。

Separator 的尺寸与属性优先级保留原生语义；附加 Color 不读取 Background/Foreground。
默认不可聚焦、不可命中，禁用不变色，没有主动交互、状态回调或动画。

## 已确认选择

[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Divider/README.md)保留维护者在移植任务中提出的 Separator 主题与方向附加属性方案。
Thickness/Color 直接映射上游参数，不新增公开控件或通用布局层；方向仅把两个 Compose API
映射到同一原生模板，不改变线条算法。原记录未声称本组件新增已接受的视觉/算法偏离，
本次拆分也不从实现反推额外授权。

## 一致性边界与验证

[DividerTests](../../../../tests/Material3Expressive.Tests/DividerTests.cs) 检查真实模板、尺寸、参数变更
导致的失效、Token/绑定优先级、约束与编译 XAML。
[DividerCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/DividerCases.cs) 和
[DividerCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/DividerCases.kt) 独立调用真实组件，
覆盖两方向、亮暗、零/负/小数/粗厚度、alpha、长度/横截面、零尺寸、有限/无界、inset 与 RTL；
动态改变方向、厚度、颜色覆盖/清除、主题与父尺寸，没有人为添加过渡动画。
几何来自真实布局，截图检查实际线条，配置参数不能冒充绘制读数。
idle-layout 在不送帧/不绘制时检查最终布局，self-test 另有被动截图。

历史视觉参考只覆盖 Windows x64、density=4；其他 DPI、平台和真实 OS 无障碍客户端未经双端验收。
本次文档拆分未运行下列命令，不以编译通过替代渲染一致性：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~DividerTests"
./tests/VisualParity/parity.ps1 self-test --family divider
```

参考依赖与结果格式见 [VisualParity](../../../../tests/VisualParity/README.md)，当前主库依赖和主题
接入见 [公共接入](../../../../docs/GETTING_STARTED.md)。
