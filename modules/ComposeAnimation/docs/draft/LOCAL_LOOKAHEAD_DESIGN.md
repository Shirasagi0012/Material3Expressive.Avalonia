> **历史设计稿**：本文早于 2026-10 的动画基础重构，仅作历史参考，名称与行为可能已过时。现行用法见 [`docs/usage`](../usage/README.md)；迁移见 [迁移指南](../usage/migration-from-visual-animation-scope.md)。主要改名：`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`SharedAnimationFrameClock` → 内部 `FrameClock`，`IAnimationFrameClock` → `IFrameSource`，`AnimationScope<T>` → `AnimationRun<T>`。

# 局部 Lookahead：使用方式与 API 设计提案

> 生命周期、时钟与诊断条目已由 [VisualAnimationScope](usage/visual-animation-scope.md) 替代；下文旧附加 API 仅保留为设计历史。

状态：设计已落地，公开 API 仍为实验性。日期：2026-09-22。实现范围与验证见 [实施状态](LOCAL_LOOKAHEAD_IMPLEMENTATION_STATUS.md)。

本版已按后续讨论修订：通过可继承的上下文附加属性传播域；Boundary 截断外域，内部入口可自动建立独立域，不需要 IsDomainRoot。源码改造顺序和待验证细节见 [落地计划](LOCAL_LOOKAHEAD_IMPLEMENTATION_PLAN.md)。

本文保留设计决策及约束。当前使用说明见 [AnimatedBounds](ANIMATED_BOUNDS.md) 和 [布局协议](LOOKAHEAD_PROTOCOL.md)。最终实现保留旧双策略接口，新增 `ILookaheadLayoutPolicy` 承载单策略 API；没有破坏性地替换 `ILookaheadLayoutNode` 的原有签名。AnimatedBounds 和 Boundary 均通过 Control 自管 Child/Padding 与私有承载层。

## 1. 设计目标与核心决定

在普通 Avalonia 控件树中的局部区域启用 Lookahead，不要求用户额外放置 LookaheadRoot。实现协议的控件形成连续的目标布局树；LookaheadBoundary 将任意普通子树封装为终点布局叶子。内部仍保留协调上下文，但它不再是必须存在的真实控件，也不要求填满一个有限视口。

建议确定以下契约：

1. 支持自动入口的协议节点位于普通父级下时建立局部上下文；位于协议父级声明的子项边上时加入父域。仅兼容协议的既有控件不因此失去普通布局路径。
2. 普通节点不会被透明穿透。它下面的协议区域可以独立工作，但不能让上方协议区域假装知道这个普通节点内部的目标布局。
3. Boundary 自身参与外域，在内容继承入口将上下文显式置为 null；内部的自动入口可建立独立域。
4. 局部入口默认向普通父级报告目标期望尺寸；动画改变区域内的显示矩形，不把逐帧尺寸反馈给普通父级。
5. 目标布局和实际布局拥有独立的结果与失效原因。动画采样不能被当作目标输入。
6. 保留实际内容、原生绘制和交互。Boundary 不截帧、不冻结内容、不自动缩放。

第一版允许多个独立局部区域，不承诺跨区域共同预测普通祖先的终点布局。任意节点在完全未知的普通父级中既逐帧影响父布局、又稳定预测全局终点，不在本提案的保证范围内。

## 2. 应用层使用方法

以下片段假定声明：

```xml
xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation"
```

### 2.1 一个普通控件，保持终点排版

```xml
<Grid>
    <ca:AnimatedBounds TargetSize="{Binding CardSize}"
                       HorizontalAlignment="Left"
                       VerticalAlignment="Top">
        <ca:LookaheadBoundary ContentAlignment="TopStart">
            <local:CardView />
        </ca:LookaheadBoundary>
    </ca:AnimatedBounds>
</Grid>
```

CardSize 改变时，Grid 获得新的目标期望尺寸。CardView 按终点约束完成原生测量/排列，动画期间保持这一内部布局；AnimatedBounds 的显示矩形过渡，Boundary 默认裁切到当前外框。普通 Grid 不需要实现协议。

TargetSize 为 null 时，目标由内容测量和父级约束决定。使用普通数据模板时，在 Boundary 内放置 ContentControl/ContentPresenter，不新增另一套模板 API。

### 2.2 文本按中间宽度真正重新排版

```xml
<Grid>
    <ca:AnimatedBounds TargetSize="{Binding TextAreaSize}"
                       HorizontalAlignment="Left">
        <ca:LookaheadTextBlock Text="{Binding Description}"
                               TextWrapping="Wrap" />
    </ca:AnimatedBounds>
</Grid>
```

LookaheadTextBlock 直接接收动画的实际约束，因此随中间宽度换行。与 2.1 的区别由内容是否经过 Boundary 表达，不在 AnimatedBounds 上再添加一个含义重复的 Freeze/ResizeContent 开关。

### 2.3 同一局部区域混合两种内容

```xml
<Grid>
    <ca:AnimatedBounds TargetSize="{Binding PanelSize}">
        <ca:LookaheadStackPanel Spacing="12">
            <ca:LookaheadTextBlock Text="{Binding Heading}"
                                   TextWrapping="Wrap" />
            <ca:LookaheadBoundary>
                <local:EditorView />
            </ca:LookaheadBoundary>
        </ca:LookaheadStackPanel>
    </ca:AnimatedBounds>
</Grid>
```

Heading 随当前宽度重排；EditorView 保持目标宽度下的排版。Boundary 对外的尺寸仍受当前父级协议约束，对内的终点槽位不随动画改变。

### 2.4 在协作容器内部让兄弟跟随尺寸动画

```xml
<Grid>
    <ca:LookaheadStackPanel Spacing="8">
        <ca:AnimatedBounds TargetSize="{Binding CardSize}"
                           ParentLayoutMode="AnimatedSize">
            <ca:LookaheadBoundary>
                <local:CardView />
            </ca:LookaheadBoundary>
        </ca:AnimatedBounds>
        <ca:LookaheadBoundary>
            <Button Content="跟随上方卡片的当前尺寸排列" />
        </ca:LookaheadBoundary>
    </ca:LookaheadStackPanel>
</Grid>
```

LookaheadStackPanel 是局部入口，对外报告目标期望尺寸。它内部的实际布局可以采用 AnimatedBounds 的中间期望尺寸，因此第二个子项随布局移动。实际效果仍由父级布局策略决定；父级忽略 DesiredSize 或给出固定槽位时，不能保证兄弟移动。

AnimatedSize 只允许用于同一上下文中、通过协议测量和放置它的父级。放在普通 Grid 下应在布局配置验证时明确报错，不能静默切换模式。需要向普通父级报告动画尺寸时继续使用 AnimatedContentSize，其保证范围与本协议不同。

### 2.5 普通父级改变槽位

可以改变 2.1 中 AnimatedBounds 的 Grid.Row、对齐或 Margin。下一次原生排列提供新槽位后，局部入口得到新目标，内部显示矩形从当前值运动到该槽位。

承诺的是入口相对其直接原生父级的变化。更上层普通祖先的移动直接跟随；不推测它们尚未提交的未来位置，也不隐式改变原生滚动行为。

### 2.6 Boundary 内自动建立独立域

```xml
<ca:AnimatedBounds TargetSize="{Binding OuterSize}">
    <ca:LookaheadBoundary>
        <Grid>
            <ca:AnimatedBounds TargetSize="{Binding InnerSize}">
                <ca:LookaheadTextBlock Text="{Binding Description}"
                                       TextWrapping="Wrap" />
            </ca:AnimatedBounds>
        </Grid>
    </ca:LookaheadBoundary>
</ca:AnimatedBounds>
```

Boundary 的原生内容使用外域的终点约束；内部 AnimatedBounds 根据自己的目标启动动画。外域动画采样不改变内域入口约束，内域采样也不改变向外报告的目标期望尺寸。内域目标尺寸真正变化时仍通过普通布局通知向外传播。无需额外的 Root 控件或重入标记。

## 3. 推荐的应用层 public surface

### 3.1 AnimatedBounds

保留控件名称及 BoundsTransform、TargetSize、IntermediateLayout、ResetAnimation。新增 ParentLayoutMode 和可观察的动画矩形；ReferenceElement 为新名称，ReferenceScope 保留为同一属性的兼容别名。

以下为省略依赖属性字段、构造器和实现的 API 草图：

```csharp
public enum BoundsParentLayoutMode
{
    TargetSize,   // 默认：实际测量向父级报告目标期望尺寸
    AnimatedSize  // 仅协议父级：实际测量报告中间期望尺寸
}

public sealed class AnimatedBounds : Control, ILookaheadConstrainedLayoutNode
{
    public Control? Child { get; set; }
    public Thickness Padding { get; set; }
    public BoundsTransform BoundsTransform { get; set; }
    public Size? TargetSize { get; set; }
    public BoundsLayout IntermediateLayout { get; set; }
    public BoundsParentLayoutMode ParentLayoutMode { get; set; }
    public Control? ReferenceElement { get; set; }
    public bool AnimateMotionFrameOfReference { get; set; }

    public Rect? CurrentBounds { get; }
    public Rect? TargetBounds { get; }
    public bool IsRunning { get; }
    public void ResetAnimation();
}
```

- 所有可配置属性为 StyledProperty；CurrentBounds、TargetBounds、IsRunning 为可绑定的只读 DirectProperty。
- 继续使用 Animated.FrameClock 和 Animated.LastError，不增加第二套时钟/错误属性。
- TargetSize 是目标尺寸输入，包含本控件 Padding；null 表示自然目标尺寸。
- Width/Height/Min/Max 仍是原生布局尺寸偏好，不重新解释为动画输出。需要独立指定目标盒子时优先使用 TargetSize。
- ParentLayoutMode 两种取值都不改变目标 pass；只改变实际 pass 向协议父级报告的期望尺寸。
- 模式切换使实际测量失效，在参考坐标和目标不变时保留动画采样；切换到不满足前提的 AnimatedSize 应报错。
- BoundsTransform、BoundsLayout 的既有内外两层边界及组合顺序保留。
- ClipToBounds 保持 false 的默认值；内容裁切由 Boundary 或调用者显式决定。

### 3.2 Bounds 和动画矩形的区别

这是有意的 API 行为变化，不能隐含在实现里：

| 几何 | 含义 |
| --- | --- |
| Avalonia 的 Bounds | 宿主在实际父布局中获得的布局矩形，遵循原生父级坐标规则 |
| CurrentBounds | 外层动画显示矩形，使用 ReferenceElement 或默认参考坐标，不含用户 RenderTransform |
| TargetBounds | 同一参考下已经提交的目标显示矩形；没有当前有效目标时为 null |

TargetSize 模式下，宿主可以已经处在目标槽位，而内部承载层仍位于动画的中间矩形。CurrentBounds 不保证等于 Bounds，也不等于整个屏幕的最终绘制 AABB。CurrentBounds 在目标重建期间可以保留上一次采样，但不得因此把旧 TargetBounds 宣布为有效。

CurrentBounds/TargetBounds 表示选定参考下的显示位置，包含当前声明的 motion-frame 位移。AnimateMotionFrameOfReference 决定这些位移是直接跟随还是进入动画，而不是决定是否在公开显示几何中遗漏它们。因此滚动可以改变这些属性而不增加目标 Snapshot.Version。BoundsTransform 仍在动画的参考基准中选择规格：排除 motion frame 时，其参数不包含直接跟随的位移，不应与公开显示矩形强行逐值比较。

内部承载层通过真实布局及私有偏移实现位置补偿；不占用用户的 RenderTransform。内容命中测试随真实承载层工作，仍受普通祖先的裁剪和原生输入规则约束。必须单独验证溢出目标槽位时的指针命中、焦点与裁剪，不能只检验渲染结果。

不通过篡改宿主 Bounds 来要求普通父级接受另一个槽位，也不强制普通父级每帧重新布局。

### 3.3 LookaheadBoundary

保留现有名称和大部分 API：

```csharp
public sealed class LookaheadBoundary : Control, ILookaheadConstrainedLayoutNode
{
    public Control? Child { get; set; }
    public Thickness Padding { get; set; }
    public VisibilityAlignment ContentAlignment { get; set; } // 默认 TopStart
    public void InvalidateTargetMeasure();
    // Child、Padding 通过 Avalonia 原属性 AddOwner 保留。
    // ClipToBounds 默认 true。
}
```

不新增 Freeze、AllowNestedLookahead、IsDomainRoot、独立 Spec 或 ResizeMode。Boundary 的 Child 子树不会注册到外部目标快照；其中支持自动入口的控件可在继承上下文为 null 时建立自己的域。普通控件继续原生布局。

Boundary 不阻止后代内容更新，也不改变业务状态。原有参与者移入 Boundary 时必须释放旧域归属和旧参考；之后允许按新位置建立独立域，不保留跨域动画连续性。AnimatedProperty、AnimatedVisibility 等普通动画照常工作。

Boundary 已改为 Control 自管 Child/Padding。Child 的逻辑父级仍为 Boundary，视觉及属性继承经过私有内容宿主；将 Boundary 当作 Decorator 的代码需要迁移。AnimatedBounds 也使用这一承载方式；Child/Padding 的属性身份和 XAML 内容语义保留。

### 3.4 叶子的定义

第一版推荐把 Boundary 作为显式的普通内容叶子。普通 TextBlock、Button、Grid、UserControl 都可以通过它进入目标树，不要求固定 Width/Height。

不根据当前 VisualChildren 为空自动判断一个控件是叶子：模板可能尚未实例化。不把所有未知控件自动视为叶子，否则协议子树被意外截断时很难发现。

现有“固定 Width/Height 的普通不透明叶子”可作为兼容路径保留，但不作为新文档推荐方式；在要求完整中间约束的路径中仍须显式 Boundary，不能把原生硬尺寸处理宣称为完整约束支持。

### 3.5 继续使用的布局控件

LookaheadStackPanel、LookaheadBox、LookaheadTextBlock、LookaheadScrollViewport 继续作为协作控件。支持自动入口的容器不需要被 Root 包裹；叶子可加入父域而不必单独创建上下文。区分协议能力与入口能力的具体兼容策略见落地计划。

普通 Grid 不会因为子项启用了协议而成为 LookaheadGrid。需要预测 Grid 内部排列时，需要未来的协作 Grid 或用户自定义布局策略；这项能力不伪装为附加属性开关。

LookaheadSize/LookaheadPosition 暂保留为已有实验 API，适配新上下文但不继续扩展。它们与 AnimatedBounds 的约束和布局输出不完全相同，不能直接无条件转成别名。

## 4. 坐标、查询与生命周期工具

ReferenceElement=null 时，整个局部区域默认以入口的直接原生视觉父级为参考。该父级本身不必实现协议，也不因此获得目标布局记录。入口从普通父级收到的最终槽位，经 Margin、对齐、尺寸限制和舍入处理后，才是该入口的目标矩形。

显式 ReferenceElement 首版限于同一区域内的协议祖先；不接受任意普通祖先、跨区域节点或 Boundary 内部节点。更换参考或切换 motion-frame 模式采用既有显式重置语义。

提供以下静态门面，避免用户访问内部协调器：

```csharp
public static class Lookahead
{
    public static LookaheadLayoutSnapshot? GetSnapshot(Control participant);
    public static LookaheadLayoutResult? TargetFor(Control participant);
    public static void InvalidateTargetLayout(Control participant);
    public static void InvalidateApproachMeasure(Control participant);
    public static void InvalidateApproachArrange(Control participant);
    public static void MoveContent(Control content, Action move);

    public static readonly RoutedEvent<LookaheadTargetLayoutEventArgs>
        TargetLayoutUpdatedEvent;
}

public sealed class LookaheadLayoutSnapshot
{
    public long Version { get; }
    public Control Entry { get; }
    public Visual CoordinateReference { get; }
    public IReadOnlyDictionary<Control, LookaheadLayoutResult> Results { get; }
}

public sealed class LookaheadTargetLayoutEventArgs : RoutedEventArgs
{
    public LookaheadLayoutSnapshot Snapshot { get; }
}
```

- Snapshot 只包含完整提交的目标几何，Bounds 相对 CoordinateReference，排除 motion-frame 位移。LayoutData 按不可变契约保存，不包含实际动画采样。
- Version 只在所属上下文生命周期内递增；跨上下文不能仅凭 Version 比较身份。
- GetSnapshot 在未布局、目标已失效、处于 Boundary 内或不是参与节点时返回 null；不为了查询主动执行布局。内部正在执行的实际 pass 仍可安全持有其已捕获的不可变快照。
- TargetLayoutUpdatedEvent 使用 Direct 路由：每次提交向本次快照内的协议参与者发布一次，不向普通子树广播。回调只读取结果；新的目标修改排入后续布局，不重入当前 pass。
- InvalidateTargetLayout 用于外部布局策略输入变化；两个 Approach 入口用于自定义动画采样变化，不清空目标结果。
- MoveContent 保留现有同步、同一上下文/参考的限制；失败释放保留状态、不回滚树。不承诺跨两个自动局部区域保留动画。

LookaheadCoordinates 保留现有查询职责，但以 Try API 处理首轮布局前和暂时失效状态：

```csharp
public static bool TryGetTargetBounds(
    Control element, out Rect bounds,
    Visual? relativeTo = null,
    bool includeMotionFrameOfReference = false);

public static bool TryGetActualBounds(
    Control element, out Rect bounds,
    Visual? relativeTo = null,
    bool includeMotionFrameOfReference = false);
```

两个查询默认使用区域 CoordinateReference。relativeTo 只接受该参考或同区域协议祖先。Actual 查询表示实际布局矩形，AnimatedBounds 的飞行动画采样请读 CurrentBounds；不把两者混为一个结果。显式包含 motion frame 时组合当前位移，不修改目标快照。

原生父级以上的滚动和变换由普通视觉树直接带动。首版仅保证区域内的平移坐标和协议声明的 motion frame，不借移除 Root 的机会暗示已经支持任意旋转、镜像或跨窗口预测。

## 5. Boundary 的精确布局契约

需要分别保留三项几何：目标测量约束、目标内容槽位、当前边界外框。终点尺寸不等于把当前动画尺寸重新传给 Child。

| 输入变化 | 原生子树测量 | 原生子树排列 | 动画显示更新 |
| --- | --- | --- | --- |
| 首次布局/目标约束改变 | 按目标约束测量 | 按目标槽位排列 | 初始化或重定向 |
| 仅目标槽位变化 | 必要时复用测量 | 必要时排列 | 更新终点 |
| 仅动画位置/尺寸采样变化 | 不由该采样触发 | 保持子树局部槽位 | 更新外框、承载层偏移及裁剪 |
| 文本、模板、字体或业务内容改变 | 正常失效后重测 | 正常失效后排列 | 目标改变则重定向 |
| 仅 ContentAlignment 改变 | 不需要 | 子树局部槽位保持 | 调整承载层在当前外框中的偏移 |

用固定终点尺寸的内部承载层承载 Child。外层对齐变化移动承载层，而不改变 Child 的局部 Arrange Rect。父级只分配给 Boundary 一个当前外框时，Boundary 仍可报告服从该框的尺寸，但内部原生子树不需要服从同一中间尺寸；默认 ClipToBounds 处理溢出。

“不重新布局”指外域动画采样不增加内部子树 MeasureOverride/ArrangeOverride 的执行；内部独立域的动画和普通内容更新仍可执行自己的布局。它不保证整个动画期间绝无正常布局失效，也不禁止入口调用命中缓存的原生 Measure/Arrange。

目标 pass 允许通过 Boundary 的受控入口调用原生 Child.Measure。这会实例化模板并改变真实原生测量缓存；因为子树始终使用终点约束，这项副作用是该适配器的明确契约。只在正常布局调度中执行，不在属性变更回调中偷偷预布局，也不把真实 Child 当成两份独立布局状态来来回回切换。

内部原生测量产生的 DesiredSize 通知属于当前目标事务的输出，不能无条件触发同一事务无限重建。事务期间真正发生的新目标输入变化必须使候选结果作废，并调度下一轮。

## 6. 控件作者的协议

应用开发者只需组合控件。自定义容器作者应尽量只写一份布局策略；目标与实际阶段使用同一策略，通过上下文路由不同的子项测量与放置。

新策略使用完整约束模型。旧接口及 Size 兼容路径保留，通过默认接口实现接入新的单策略入口。以下草图省略继承接口中已有的子项与目标输入声明：

```csharp
public enum LookaheadLayoutPhase { Native, Target, Approach }

public interface ILookaheadLayoutPolicy : ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost
{
    LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context,
        LookaheadConstraints constraints);
}

public interface ILookaheadTargetInvalidationSource
{
    event EventHandler? TargetMeasureInvalidated;
}

public sealed class LookaheadMeasureContext
{
    public LookaheadLayoutPhase Phase { get; }
    public LookaheadPlaceable Measure(
        Control child, LookaheadConstraints constraints);
    public bool TryGetTarget(Control child, out LookaheadLayoutResult result);
    public LookaheadMeasureResult Layout(
        Size size, Action<LookaheadPlacementContext>? placement = null,
        object? layoutData = null);
}

public sealed class LookaheadMeasureResult
{
    public Size Size { get; }
    public object? LayoutData { get; }
    // 仅由 context.Layout 创建，内部保存该次测量结果与放置回调。
}

public sealed class LookaheadPlaceable
{
    public Size DesiredSize { get; }
    // 仅由 context.Measure 创建；代表本阶段对某个子项的测量记录。
}

public sealed class LookaheadPlacementContext
{
    public LookaheadLayoutPhase Phase { get; }
    public Size FinalSize { get; }
    public void Arrange(LookaheadPlaceable child, Rect slot);
}
```

这里的 Placeable 只是一份受约束的测量记录，不提供任意控件测量/视觉树操作能力，不重建 Compose 的整个 Modifier/Placeable 系统。

### 6.1 单一策略如何覆盖两个阶段

- Target：Measure 路由到子节点独立策略，Arrange 只记录目标放置。普通原生测量仅允许在 Boundary 的内部受控入口发生。
- Approach：Measure 路由到真实 Avalonia 测量桥；动画节点可以参考已完成的目标，再为内容选择中间约束。Arrange 执行真实放置并传递本轮坐标。
- Native：低层桥未接管时的普通实际 fallback，TryGetTarget 返回 false；自动入口通常先运行 Target，提交后运行 Approach。Native 不作为禁止后代建立独立域的全局状态。
- Native 阶段允许子项走普通原生布局，不要求为每个普通子项增加 Boundary；遇到支持自动入口的后代时，该后代可以启动自己的 Target/Approach 调用。
- TryGetTarget 仅在持有完整目标的 Approach 阶段成功；Target 阶段不可借它读取上一轮布局作为预测结果。
- 传给策略的约束已经处理节点自身的 Margin、尺寸偏好；Layout 返回节点本体的报告尺寸。框架只应用一次尺寸限制、Margin 和舍入。
- 子项 DesiredSize 包括子项自己的 Margin，Arrange 接受父局部槽位，再统一处理子项对齐、Margin、舍入。避免每个自定义 Panel 复制这些规则。
- Placement 回调只能使用测量捕获数据和当前 FinalSize；可以复用多次，但不能在其中 Measure、读取旧 Bounds 推断本轮祖先坐标或修改视觉树。

通用容器允许同一子项在一次测量中按不同约束顺序重测，以便实现接近 Avalonia Grid 的策略。只有最后一次测量句柄可用于本轮放置；同一可见子项至多放置一次。旧句柄、跨阶段句柄及跨布局代次误用必须被拒绝。未选择的中间测量不会进入提交的目标树。

这与现有 BoundsLayout 的“单子项恰好测量一次”契约不同。后者作为轻量中间布局策略保持原契约，不因新增通用 Panel 协议而放宽。

### 6.2 推荐扩展入口

提供 LookaheadPanel : Panel、LookaheadDecorator : Decorator 和 LookaheadControl : Control 三个辅助基类。它们处理原生布局入口、挂载、子项集合、目标属性观察和当前阶段路由。派生类主要重写：

```csharp
protected abstract LookaheadMeasureResult MeasureLayout(
    LookaheadMeasureContext context,
    LookaheadConstraints constraints);
```

例如一个仅承载单个子项的自定义 Panel：

```csharp
public sealed class SingleChildPanel : LookaheadPanel
{
    protected override LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        if (Children.Count == 0)
            return context.Layout(constraints.Constrain(default(Size)));
        if (Children.Count != 1)
            throw new InvalidOperationException("Exactly one child is supported.");

        var child = context.Measure(Children[0], constraints.Loosen());
        return context.Layout(
            constraints.Constrain(child.DesiredSize),
            placement => placement.Arrange(child, new Rect(placement.FinalSize)));
    }
}
```

此例不需要判断 Root，也不需要维护 DesiredSize 的目标副本。新增策略输入需列入 LookaheadProperties 或发出 TargetMeasureInvalidated；基类观察公共几何属性和子项集合。基类负责原生回退，不要求派生类再写一份同等 MeasureOverride/ArrangeOverride。

### 6.3 已有继承体系的接入

已有控件可能必须继承 TextBlock 等基类，不能强制所有节点更换基类。新单策略接口提供以下低层桥；旧双策略接口继续使用 LookaheadMeasure：

```csharp
public static class LookaheadLayout
{
    public static bool TryMeasureCore(
        Control owner, Size availableSize, out Size desiredSize);
    public static bool TryArrangeCore(
        Control owner, Rect finalRect, out Rect bounds);
    public static void Forget(Control owner);
}
```

手工实现者在 MeasureCore/ArrangeCore 中调用桥，处理成功时使用返回值，未接管时走原生 base；离树时调用 Forget。协议声明与原生入口桥两者都必须实现，仅挂一个 interface 不算完成接入。内部桥同时识别显式的协议子项请求与普通父级发起的局部入口请求，不再以 FindRoot 是否返回控件作为唯一开关。

普通模板节点不能只凭祖先查找自动被忽略。框架自带模板通过显式内部桥连接其逻辑参与者与真实视觉子项；外部控件在没有适配时使用 Boundary。

## 7. 自动上下文与提交顺序

协调器保留为内部类型，用户不创建、不持有、不 Dispose。以可继承的 AttachedProperty<LookaheadContext?> 传播当前域身份，默认 null；普通参与节点继承，入口发布自己的实例，Boundary 的内部内容入口设置本地 null。设置 null 用于阻断继承，ClearValue 用于恢复继承，不能混用。

长期域身份与当前 Target/Approach 阶段分别管理：前者是稳定的附加属性值，后者是带域身份的同步调用上下文。参与边、代次、订阅和生命周期仍由协调器维护；继承到上下文不等于一个普通控件已成为协议节点。

1. 支持自动入口的节点从普通父级收到 Measure 时，先检查继承上下文与协议参与边；能加入父域则加入，否则建立或复用局部上下文。Boundary 后的 null 允许启动新域。
2. 沿明确声明的参与子项边执行目标测量，Boundary 截断递归；不同普通父级下面的入口互相独立。
3. 测量阶段返回目标 DesiredSize，允许最大约束含无穷；结果尺寸必须有限。布局策略无法在无穷轴给出有限结果时明确报错。
4. 父级 Arrange 提供入口槽位；框架先解析入口的目标矩形，再执行目标 placement。测量结果与最终槽位共同构成完整目标代次。
5. 完整目标全部验证成功后一次提交，再启动或重定向 Rect 动画。不能将新目标尺寸与旧目标位置拼成一次重定向。
6. 入口内部以最新采样执行实际布局。如果真实子内容需要中间尺寸测量，首次获得完整目标的 Arrange 可在安排内部承载层前完成所需的子项测量；不递归重测入口的普通父级。
7. 父级最后分配尺寸与测量预期不同、导致实际测量结果不再适用时，按正常调度补一次实际测量；不展示一个被误认作有效目标的中间结果。
8. 后续动画仅使相关实际测量/放置失效；目标不变就复用目标代次。

普通父级可能先后使用多组约束，或在一轮布局里产生多次有效分配。入口采用正常布局给出的最新目标；不会声称预知该普通父级所有后续行为。需要比它更强的终点稳定性时，必须让该父级也参与协议。

首次出现直接初始化到目标。目标暂时失效时允许保持最后采样等待正常布局，但不能无限请求帧或宣称旧目标仍有效。目标构建失败不提交部分结果，并释放受影响区域的活动布局动画；其他独立区域不受影响。

## 8. 失效与生命周期

| 变化 | 处理 |
| --- | --- |
| 内容/字体/尺寸策略/子项集合变化 | 目标失效，正常布局重建后从当前值和速度重定向 |
| 动画只有位置变化 | 实际 Arrange 失效，尽量复用实际 Measure |
| 动画改变内容约束 | 实际 Measure/Arrange 失效，目标保持有效 |
| Boundary 原生内容布局失效 | 保守判断为目标输入可能变化，复核终点；不忽略正常更新 |
| 普通父级新槽位 | 更新局部入口目标，参考以上的运动直接跟随 |
| 普通 detach / hide / 换上下文 | 释放状态，后续首次布局直接初始化 |
| 换时钟 | 保留当前采样，重建时间线；不保证旧速度/时间原点 |
| ResetAnimation | 忘记动画状态，下一次有效布局直接到目标 |
| 同上下文 MoveContent | 延续既有同步移动保留契约 |
| 移入 Boundary | 释放旧域和旧参考；普通节点走原生布局，自动入口可建立独立新域 |

公共目标失效与动画失效应使用独立入口。内部因 Approach 传播的原生缓存失效必须有来源记录，不能回流成 TargetInvalidated。普通自定义输入没有专用通知时允许保守重建，但不要把每帧全树预测当作默认兜底。

布局策略错误继续抛出并清理相关状态；动画规格/时钟错误延用 Animated.LastError 的恢复契约。事件处理器异常与旧代次回调不得阻止其他资源释放。

## 9. SharedTransition 如何使用

SharedTransitionHost 仍负责匹配、overlay 和退出保留；它不是要求用户添加的 Lookahead 布局范围。SharedBounds.RemeasureToBounds 应能自动成为局部参与入口：

```xml
<!-- 仍位于同一 SharedTransitionHost 的 AnimatedVisibility 分支内。 -->
<ca:SharedBounds Key="card" ResizeMode="RemeasureToBounds">
    <ca:LookaheadBoundary>
        <local:CardView />
    </ca:LookaheadBoundary>
</ca:SharedBounds>
```

换成 LookaheadTextBlock 则是真正按共享 Rect 重排；Boundary 内容保持终点布局。两种情况都不再要求 LookaheadRoot。

目标端布局从各局部快照取得，并显式转换到 SharedTransitionHost 坐标；源端飞行接续读取当前显示状态。普通 Bounds 与 CurrentBounds 分离后，Shared 后端必须分别处理这两个来源，不能继续混用 Capture.Bounds 或宿主 Bounds 猜目标。

同一 Host 可以协调两个独立局部区域的共享会话，但不因此合并它们的目标布局树。坐标转换仍只保证支持的几何变换，外部普通祖先只提供当前已分配的位置。最终真实布局就绪之后才交接的既有协议继续保留。

## 10. 现有 API 的迁移

| 当前 API/行为 | 提议 |
| --- | --- |
| 必须放置 LookaheadRoot | 协议入口自动管理上下文，新示例删除 Root |
| Root 填满有限视口 | 新入口按普通内容测量，不继承该规则；有限动画 Rect 仍是要求 |
| Root.Snapshot / TargetFor | Lookahead.GetSnapshot / TryGetTargetBounds |
| Root.InvalidateTargetLayout / MoveContent | 静态门面按参与节点定位上下文 |
| ReferenceScope | ReferenceElement；旧属性作为过渡别名，明确新默认参考 |
| AnimatedBounds.Bounds 常被当作动画矩形 | 调用者改读 CurrentBounds；原生 Bounds 保留父布局含义 |
| AnimatedBounds 向父级报告中间尺寸 | 新默认 TargetSize；协议父级下要保留旧行为时显式 AnimatedSize |
| 分开的 Size / full-constraint 协议 | 新策略统一使用 LookaheadConstraints，旧协议通过过渡适配器迁移 |
| ILookaheadPlacementHost + ArrangeChild | 新策略由 PlacementContext 承担放置桥；旧入口保留兼容期 |
| Boundary 内禁止嵌套 Root | 截断外域后允许独立内域；兼容 Root 若作为独立入口，仍保留其旧占位规则 |

LookaheadRoot 可在兼容期保留原有有限视口/占位语义，作为显式旧入口连接同一内部协调器。不能直接改变旧 Root 的布局语义而声称完全兼容。新代码不需要它，旧代码可分阶段迁移。

这是实验 API 的有意变更，特别是默认报告尺寸、默认参考以及 Bounds 的观察语义，需要迁移说明和针对性测试。

## 11. 建议的实现顺序与验收

### 第一阶段：验证最小局部链路

- 抽离内部协调上下文，但先保留已有协议实现，避免同时改所有接口。
- 实现普通 Grid 下 AnimatedBounds + Boundary 和 AnimatedBounds + LookaheadTextBlock。
- 验证自然尺寸、有限/无限测量约束、父级对齐/换槽、首次布局、反向与目标变化。
- 验证完整目标提交之前不会用混合 Rect 启动动画。
- 验证原生父级只在目标变化时因该区域重新测量，纯动画采样不制造反馈循环。

### 第二阶段：隔离和组合

- 固定终点内容承载层，实现 Boundary 的测量、排列隔离。
- 同一普通父级下两个独立区域；Boundary 内自动入口建立独立域，外域快照不包含内域节点。
- 协作父级内部 TargetSize/AnimatedSize 两种输出、嵌套动画、motion frame、相同上下文移动。
- 加入真实命中测试、焦点、父级裁剪和溢出场景，不只检查数值轨迹。

### 第三阶段：公开扩展协议

- 提供统一 Context/Result/Placeable 与辅助基类，再迁移内建容器和 TextBlock。
- 验证顺序重测、旧句柄拒绝、放置复用、模板桥和普通 Native 回退。
- 接入 SharedBounds，验证独立局部区域的目标读取、反转与最终交接。

关键验收指标是工作次数和外部行为：目标不变的动画中 Snapshot.Version 不变；Boundary 原生子树无额外 MeasureOverride/ArrangeOverride；协作文本确实 reflow；纯位置帧不重测；不存在无限目标重建、父布局反馈或结束后一帧跳变。

## 12. 本轮不承诺的能力

不包括任意原生 Panel 的透明双轨布局、虚拟化、完整 Modifier 链、跨窗口/跨区域 movableContent、任意 render transform/镜像坐标、Boundary 内重新接入外部同一目标树，以及任意普通父级下的全局终点预测。Boundary 内建立独立域属于本提案范围。

上下文附加属性负责域标记，不把普通控件变成协议控件；不需要 IsDomainRoot/AllowNestedLookahead。暂不公开协调器注册/提交/释放 API。应用层主要面对 AnimatedBounds、Boundary 和协作容器；扩展作者才需要上下文协议。

## 13. 设计依据

- 当前局部化的主要耦合：[LookaheadRoot](../ComposeAnimation.Avalonia/Lookahead/LookaheadRoot.cs)、[LookaheadScope](../ComposeAnimation.Avalonia/Lookahead/LookaheadScope.cs)、[LookaheadPlacement](../ComposeAnimation.Avalonia/Lookahead/LookaheadPlacement.cs)。
- 现有完整约束和实际布局入口：[LookaheadMeasure](../ComposeAnimation.Avalonia/Lookahead/LookaheadMeasure.cs)、[LookaheadConstraints](../ComposeAnimation.Avalonia/Lookahead/LookaheadConstraints.cs)。
- 现有隔离语义基础：[LookaheadBoundary](../ComposeAnimation.Avalonia/Lookahead/LookaheadBoundary.cs)、[已实现的 Boundary 设计](LOOKAHEAD_BOUNDARY_DESIGN.md)。
- 既有动画与中间策略：[AnimatedBounds](../ComposeAnimation.Avalonia/AnimatedBounds.cs)、[BoundsLayout](../ComposeAnimation.Avalonia/BoundsLayout.cs)。
- Shared 接入点：[SharedBoundsLookahead](../ComposeAnimation.Avalonia/SharedTransition/SharedBoundsLookahead.cs)。
- 本地上游对照：`D:/source/Avalonia/src/Avalonia.Base/Layout/Layoutable.cs` 的单套原生 Measure/Arrange 缓存与布局入口，以及 Compose 的 `LookaheadScope.kt`/布局节点内建双轨 pass。上述自动入口和隔离设计是针对本仓库的提案，不是上游已提供的 Avalonia 功能。
