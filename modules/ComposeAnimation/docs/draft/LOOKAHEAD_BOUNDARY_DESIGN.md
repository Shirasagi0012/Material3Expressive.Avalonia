# LookaheadBoundary 历史设计说明

状态：历史设计，已由 [局部 Lookahead 设计](LOCAL_LOOKAHEAD_DESIGN.md) 和
[落地计划](LOCAL_LOOKAHEAD_IMPLEMENTATION_PLAN.md) 取代。本文只保留已落地的 Boundary 契约；
旧版以 `LookaheadRoot` 为唯一作用域、禁止 Boundary 内容重开域的方案不再成立。

## 当前用途

`LookaheadBoundary` 是协作目标树的显式终点。Boundary 自身参加外域的目标测量和放置，
用户 `Child` 通过私有内容承载层执行原生 Avalonia 布局；外域快照包含 Boundary，不包含普通内容子树。
它不冻结位图、不缩放内容，也不拥有动画时钟。

公开行为保留：

- `Child`、`Padding`、`ClipToBounds`；`ClipToBounds` 默认 `true`。
- `ContentAlignment`，默认 `TopStart`，并遵循 `FlowDirection`。
- `InvalidateTargetMeasure()`，供非 AvaloniaProperty 的内容测量输入变化时显式失效。

实现现在由 `Control` 自管 Child/Padding，而非直接继承 `Decorator`。用户 Child 的逻辑父级仍是
Boundary；视觉父级和继承父级是私有 host。host 正常继承 DataContext、字体、FlowDirection 和资源环境，
只把内部 Lookahead 上下文设为本地 `null`，从而截断外域。

## 域边界

Boundary 自身继续继承外域。内容出口截断该上下文后：

- 普通 Grid、UserControl、模板内容继续使用原生布局。
- 支持自动入口的 `AnimatedBounds`、`LookaheadBox`、`LookaheadStackPanel`、
  `LookaheadScrollViewport`、`LookaheadSize`、`LookaheadPosition`，以及启用真实重测的 SharedBounds，
  可以在内容侧建立独立局部域。
- 内外域拥有不同的快照、成员代次和坐标参考。跨 Boundary 移动不保留动画连续性。
- 内域 approach 帧不会仅因 `IsMeasureValid` 变化而重建外域目标；内域对外目标期望尺寸变化仍可正常向上传播。

因此，Boundary 内嵌套 `LookaheadRoot` 不再被全面禁止。兼容 Root 可作为内容侧独立入口，
但仍保留它自己的有限 viewport 和 Padding 限制。应用通常无需为了启用动画而添加 Root。

## 布局契约

目标阶段根据终点约束原生测量私有内容 host，并把测量依赖纳入外域目标输入。目标放置只记录
Boundary 的终点几何；普通 Child 不进入外域 builder。

实际阶段中，内容保持终点排版。外域中间样本只移动/裁切 host，不向原生内容子树持续传递中间约束。
内容自身更新、内域动画、资源或继承属性变化仍可触发正常原生布局。

替换或移除 Child 时，旧 Child 的视觉和继承父级会解除；新 Child 保持正确的本地属性、DataContext、
字体和资源查找。Boundary 离树或目标测量失败时会释放订阅，不保留旧目标。

## 使用

```xml
<ca:AnimatedBounds TargetSize="500,320">
    <ca:LookaheadBoundary Padding="20" ContentAlignment="Center">
        <Grid>
            <!-- 普通 Avalonia 内容按终点尺寸排版 -->
        </Grid>
    </ca:LookaheadBoundary>
</ca:AnimatedBounds>
```

外层 `AnimatedBounds` 可直接在普通 Avalonia 父级中建立局部域。若应用本来就需要固定的显式 viewport，
仍可在更外层使用 `LookaheadRoot`。

## 验证范围

`LookaheadBoundaryTest` 和 `LookaheadBoundaryInheritanceTest` 覆盖目标/实际布局、裁切和对齐、
内容失效、子项替换、逻辑/视觉/继承父级、DataContext、字体、资源，以及 Boundary 内容中独立域的生命周期。
通用虚拟化、原生 ScrollViewer 透明适配和跨域动画连续性仍不属于该控件契约。
