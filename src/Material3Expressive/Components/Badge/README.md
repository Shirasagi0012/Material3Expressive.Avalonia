# Badge / BadgedBox

移植固定 Compose Material3 `Badge.kt` 的两个公共入口及内部 `badgeBounds()`。
基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，桌面 Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。两端 Badge.kt 和 BadgeTokens.kt 相同。
源码路径为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

## 实现

[Badge.cs](Badge.cs) 和 [Badge.axaml](Badge.axaml) 提供可选内容、颜色环境及模板；
[BadgeLayout.cs](BadgeLayout.cs) 保留 `defaultMinSize → background → padding → Row` 的布局顺序。
没有内容时默认最小尺寸为 6 DIP；内容或内容模板存在时为 16 DIP，水平内边距各 4 DIP，
内容居中。存在但为空的内容仍进入大徽标分支。Width / Height / MinWidth / MinHeight
覆盖默认最小尺寸，有限最大约束可缩小徽标。组合内容可在 Content 中使用水平 StackPanel。

八项 Token 在 [BadgeTokenSource.cs](BadgeTokenSource.cs) 和
[MaterialTokens.Comp.Badge.cs](../../Tokens/MaterialTokens.Comp.Badge.cs) 定义，由 MaterialTheme 装配。
保留引用关系：两种内容分支均消费 `Comp.Badge.Color`，默认 Error；前景经共享 `contentColorFor`
按容器颜色匹配 On 色，未匹配时继承外层内容色。`LargeColor` 和 `LargeLabelTextColor`
保留在 Token 表，但固定上游的 Badge 并不消费它们。大徽标使用 LabelSmall 和 LargeShape。
字体、颜色和形状支持现有 Token 动态覆盖与控件本地覆盖。

[BadgedBox.cs](BadgedBox.cs) 使用 Content / ContentTemplate 作为锚点槽，Badge / BadgeTemplate
作为徽标槽，保留逻辑子内容、DataTemplate、替换与重挂。
[BadgedBoxLayout.cs](BadgedBoxLayout.cs) 先测徽标，再测锚点；自身尺寸由锚点决定，徽标可以溢出。
徽标包装槽测量宽度大于舍入后的 6 DIP 时使用水平 12、垂直 14 DIP 偏移，否则使用 6、6。
它按实际测量宽度判断，固定宽度的圆点也可能进入大徽标偏移分支。
锚点在自己的槽内居中，徽标按 Compose `placeRelative` 放置，RTL 不镜像文字。

NavigationRailItem、NavigationBarItem、ShortNavigationBarItem 和 WideNavigationRailItem 的内部布局
提供上方/末端边界。Avalonia 在祖先 ArrangeOverride 返回后才更新 Bounds，因此内部布局在
LayoutUpdated 读取最近提供者，边界变化时再安排一次。保持上游物理 ruler 与随后 RTL 相对放置的顺序，
包括 Start 图标不居中时的行为；这里没有额外裁剪。Tab 尚未移植，未引入其调用方。
组件没有上游动画或主动交互状态，也没有借用 Surface 造成输入阻挡。

## 使用

```xml
<m3:BadgedBox xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:BadgedBox.Badge>
    <m3:Badge Content="99+" AutomationProperties.Name="99 条未读通知" />
  </m3:BadgedBox.Badge>
  <m3:MaterialSymbol Symbol="Notifications" Width="24" Height="24" />
</m3:BadgedBox>
```

将上述 BadgedBox 放进导航项的 Icon 槽，即可使用该项提供的徽标边界。
省略 Badge.Content 显示圆点；设置 Content="" 表示存在的空内容。
原生 Background / Foreground / CornerRadius / Padding 和内容模板用于显式覆盖。

```csharp
using Avalonia.Controls;
using Material3Expressive.Components;
using MaterialSymbols;
using MaterialSymbols.Avalonia;

var badge = new Badge { Content = "8" };
var icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Search, Width = 24, Height = 24 };
var box = new BadgedBox { Content = icon, Badge = badge };
badge.Content = "999+";
badge.Content = null; // 切换为圆点
var item = new NavigationBarItem { Icon = box, Label = "通知" };

// 任意交互内容通过原生属性显式开启。
var actionBadge = new Badge
{
    IsHitTestVisible = true,
    Content = new Button { Content = "处理" },
};
```

默认内容不生成专属可访问性语义；应用可使用原生 AutomationProperties.Name 描述圆点的意义，
或按业务需求为锚点提供完整名称。Catalog 包含圆点、计数、宽内容、动态切换及 NavigationBar 集成。

## 已确认适配与缺口

2026-10-03，维护者确认保留 Avalonia 原生布局并记录基线能力缺口：
Compose BadgedBox 将锚点的 FirstBaseline / LastBaseline 透传给父布局；当前 Avalonia 没有对应的
通用 alignment-line 协议，本次未实现该透传，也未新增全库基线机制。
按文字基线排列的父布局不能获得 Compose 同等信息；范围为本次 BadgedBox。

同日，维护者允许默认穿透、显式开启交互；实现直接使用 Avalonia 原生 IsHitTestVisible，
在 Badge 主题中默认为 False。本地设为 True 可让整个内容子树参与命中。
Compose 的背景自身不消费输入，任意内容中的输入 modifier 可以自动参与命中；
本地交互内容需要上述显式设置。这是已接受的输入适配，没有新增命中属性或机制。

## 验证

[BadgeTests.cs](../../../../tests/Material3Expressive.Tests/BadgeTests.cs) 验证 Token 完整性、尺寸分支、
内容模板、动态替换、内容色、LabelSmall、本地覆盖、RTL、导航边界及卸载重挂。
独立双端入口为 [BadgeCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/BadgeCases.cs)
和 [BadgeCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/BadgeCases.kt)。
覆盖浅/暗色、无内容/空内容/组合/宽高内容、颜色与环境色、约束、宽度阈值、RTL、四种导航项边界，
以及内容、主题、颜色、方向、锚点/导航项尺寸变化和真实指针输入。
参考端调用真实 Compose API；可变 intrinsic 几何内容遵守父最大约束，不补偿组件位置。

本轮 63 个用例、84 个采样点的结构化事实与原始 4× 栅格全部精确相等，双端重复捕获及额外被动截图均稳定。
Compose 和 Avalonia 都为本轮新执行，self-test 不读取参考缓存。
完整证据见 [badge-final/summary.md](../../../../artifacts/visual-parity/runs/badge-final/summary.md)，
回归与环境说明见 [本次产物](../../../../artifacts/badge/README.md)。
尚未验证文字/MaterialSymbols 栅格、人工桌面操作和屏幕阅读器体验；当前 parity 按 AUTHORING 排除文字。

```powershell
./tests/VisualParity/parity.ps1 list --family badge
./tests/VisualParity/parity.ps1 self-test --family badge --require-equal
# 需要可复用参考快照时：
./tests/VisualParity/parity.ps1 prepare-reference --family badge
./tests/VisualParity/parity.ps1 run --family badge --require-equal
```
