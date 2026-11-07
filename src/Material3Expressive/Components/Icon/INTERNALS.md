# Icon 内部机制与维护说明

## 1. 上游基线与职责映射

固定 AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Icon.kt` 相同。
测量与对齐来自同提交 `compose/ui/ui/src/commonMain/kotlin/androidx/compose/ui/draw/PainterModifier.kt`。

| 上游符号 / 职责 | 本地类型 / 成员 |
| --- | --- |
| Icon painter / bitmap / imageVector 入口 | MaterialIcon.Source 的 IImage / Bitmap / DrawingImage |
| defaultSizeFor / SmallIconButtonTokens.IconSize | DefaultSize 主题绑定；仅无固有尺寸或两轴无限来源使用 |
| PainterNode.modifyConstraints / ContentScale.Fit | MeasureOverride，Float 物理像素计算及整数像素测量 |
| PainterNode.draw / Alignment.Center | Render，取整尺寸定位，保留未取整绘制尺寸 |
| LocalContentColor / Color.Unspecified | 继承 Foreground / IsTintEnabled=false |
| Role.Image / contentDescription | Image automation peer / ContentDescription；null 为装饰节点 |

## 2. 关键机制与架构契约

不是固定 24×24 图片：部分有限的固有轴按 PainterModifier 分支处理；调用方固定尺寸/最小约束先参与 Fit，
再产生受限测量结果。绘制中心定位使用整数物理像素，绘制尺寸仍为 Float 计算所得值。
控件绕过原生 FlowDirection 图案镜像，AutoMirror 显式启用；位置仍由父布局决定。

着色用 source alpha 作 opacity mask、前景画笔作源，保留 SrcIn 的透明度相乘关系。
DrawingBrush 的 SourceRect/DestinationRect 明确使用实际布局边界，避免内部整数 DIP 的绘制数据包围盒再次缩放小数尺寸。
Bitmap.Draw 的源矩形实际使用像素，SizedImage 将 IImage DIP 源矩形映射到完整 PixelSize；矢量和自定义 IImage 保留原坐标契约。
DrawingImage.Invalidated 在挂载期订阅，换源/卸载解除，重挂恢复；不持有或释放调用方图片的所有权。

## 3. 已确认决定与已知差异

历史 `MaterialIcon` 默认尺寸附加属性类已于 2026-11-03 合并到 MaterialSymbol，理由是只有字体图标消费该提示。本轮授权新增的是 IImage 控件，职责不同；不恢复旧 DefaultSize API，也不撤回旧调用迁移。完整依据见 DECISIONS §3。

- 本轮维护者授权新增组件与 API：以原生 IImage 统一来源，用 Foreground 与 IsTintEnabled 表达着色及原色，
  原生绑定可提供动态颜色，未引入平行 Painter 框架或回调式状态系统。AutoMirror 在控件端表达方向性矢量元数据。
- 框架适配：Bitmap 固有大小遵循 Avalonia 的 DPI 元数据，Compose BitmapPainter 使用像素固有大小和当前 density。
  两端用例提供等效逻辑尺寸；这不宣称任意缺失 DPI 元数据的素材在两个框架中自然大小相同。
- 无旧 API 删除或仓内调用迁移。IImage 的可变自定义实现没有公共统一失效事件，调用方负责通知；DrawingImage 原生通知已接入。
- 尚未实现完整 Compose Painter API、toolingGraphicsLayer 元数据；未经验证真实平台辅助技术和第三方渲染后端。
  画笔支持是 Avalonia 接入方式，双端 SrcIn 证据针对纯色和透明度，不宣称复杂渐变逐像素一致。

## 4. 验证入口

- `MaterialIconTests`：固有尺寸、约束、Token、动态绘图、继承/覆盖、卸载重挂和 automation。
- 双端 `IconCases.cs` / `IconCases.kt`，族 `icon`：实际 painter、BitmapPainter、VectorPainter，缺失/部分固有轴、
  小数尺寸、RTL、透明来源、动态前景色/换图/尺寸及镜像。运行 `./tests/VisualParity/parity.ps1 self-test --family icon`。
- Catalog `IconDemo`：XAML 矢量、运行时位图、方向开关及按钮组合。
