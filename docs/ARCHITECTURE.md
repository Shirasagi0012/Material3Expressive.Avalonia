# 架构与已有能力

本文描述当前源码中的结构、可复用能力与接入约束，不代表所有既有设计均已作为长期方案获批，
也不代表已经完成上游一致性验证。开发流程见[移植指南](PORTING.md)，参考仓库与读取方式见 [AGENTS.md](../AGENTS.md#上游源码)。

## 结构与职责

| 位置 | 当前职责 |
| --- | --- |
| [Components/](../src/Material3Expressive/Components) | Button、IconButton、Surface 的实现与主题，按组件组织。组件可以组合其他组件，例如 Button 的模板使用 Surface。 |
| [Foundation/Tokens/](../src/Material3Expressive/Foundation/Tokens) | Token 身份、资源组合、引用解析与绑定。 |
| [Foundation/Interaction/](../src/Material3Expressive/Foundation/Interaction) | 输入与交互反馈的接入、状态层、ripple 和圆角动画。 |
| [Foundation/Motion/](../src/Material3Expressive/Foundation/Motion) | Material 动画描述及其到 ComposeAnimation 的转换。 |
| [Foundation/Rendering/](../src/Material3Expressive/Foundation/Rendering) | 图标接入与颜色合成等辅助能力。 |
| [Tokens/](../src/Material3Expressive/Tokens) | `MaterialTokens` 的 Reference、System、Component 键；组件 Token 目前仍集中在这里。 |
| [Baseline/](../src/Material3Expressive/Baseline) | 颜色、形状、字体、状态、elevation、motion 的默认 Token 来源。 |
| [MaterialTheme.axaml](../src/Material3Expressive/MaterialTheme.axaml) | 装配默认 Token 来源并引入组件主题。 |

这是目录职责，不是已经由程序集隔离的严格分层。目前 `MaterialInteractionController` 仍直接引用
`MaterialSurface` 和 `ButtonElevationController`；修改相关能力时需要检查组件调用方，不能假定 Foundation 与组件无关。
是否调整这些关系属于单独的设计问题。

## Token 与主题

当前 Token 由手工定义的 `MaterialTokenKey<T>`、`ITokenSource`、`MaterialResource` 和 `{m3:Token}` 组成，
没有接入 Token 生成器或 JSONC 目录。

- 键以对象引用为身份；名称用于诊断，不能通过重新构造同名键访问已有 Token。
- `MaterialResource` 组合多个来源，后加入的来源优先；来源的变更通知传入 Avalonia 资源系统。
- `{m3:Token}` 在消费元素处解析值。每次跟随 `TokenRef` 都从同一消费位置重新查找，
  因而局部覆盖中间的 System Token 也能影响 Component Token。
- 当前派生形式包括 `WithAlpha` 和 `SpringSpecRef`。解析有跳数限制；缺失、循环或超过限制时失败，
  绑定通过 `UnsetValue` 回退，不改用外层同名定义掩盖无效的最近条目。
- 资源和主题变化触发消费值更新。保留引用关系，不要在组件初始化时把动态 Token 提前展开为永久常量。

组件主题可以参考现有 [Button 主题](../src/Material3Expressive/Components/Button/Button.axaml) 的消费方式：

```xml
<Setter Property="Background"
        Value="{m3:Token {x:Static m3:MaterialTokens+Comp+FilledButton.ContainerColor}}" />
```

这里的 `m3` 对应 `https://github.com/Shirasagi0012/Material3Expressive.Avalonia`。
嵌套类型在 XAML 中用 `+` 表达。示例展示当前接入方式，不替代新组件对上游 Token 的调查。

## 颜色、图标与阴影

| 能力 | 本地入口与边界 |
| --- | --- |
| 颜色计算 | [MaterialColorUtilities](../modules/MaterialColorUtilities/README.md) 提供颜色算法；[ColorScheme](../src/Material3Expressive/Baseline/ColorScheme.cs) 将其结果接入本项目 Token。颜色角色是算法计算结果，局部覆盖输出的 reference palette 不会反向重新生成角色。 |
| 可变字体图标 | [MaterialSymbols.Avalonia](../modules/MaterialSymbols/MaterialSymbols.Avalonia/README.md) 提供 fill、weight、grade、optical size 等轴；[MaterialSymbol](../src/Material3Expressive/Foundation/Rendering/MaterialSymbol.cs) 是本项目入口，当前使用 Rounded 字体包。组件消费已有渲染器，不另行实现字体轮廓绘制。 |
| 阴影 | [MaterialSkiaShadow](../modules/MaterialSkiaShadow/README.md) 提供与 Compose Desktop/Skiko 对应的 Skia 阴影绘制；Surface 负责接入实际几何、缩放及绘制环境。模块目前支持单个平面简单轮廓的线段、二次曲线与 conic，不支持 cubic、透视、倾斜 Z 平面、方向光和非 Skia 后端。 |

MaterialSymbols 需要的 SkiaSharp 托管与原生包由中央包版本统一。具体依赖来源以项目文件和 NuGet 配置为准。
配套模块有各自的上游版本与限制；模块级的移植或渲染验证不自动证明本项目的组件集成正确。
遇到能力缺口时说明缺少的语义，不在组件中悄悄替换为近似算法。

## Surface 与交互组合

[MaterialSurface](../src/Material3Expressive/Components/Surface/MaterialSurface.cs) 是可被其他组件组合的容器组件，
承担背景、边框、裁剪、阴影和累积 tonal elevation。内部 Border 承载内容和裁剪，独立覆盖层绘制交互反馈。
几何使用 Avalonia DIP，Full 圆角按当前边界解析。

目前交互组件通过 `MaterialInteraction.IsInteractive` 接入共享控制器，Surface 作为模板中的反馈呈现者。
控制器处理输入、交互顺序、状态层和 ripple；主题提供颜色和形状等参数。
颜色、交互事实、反馈动画和绘制结果需要保持各自归属，不要仅用最终伪类状态替代具有顺序意义的交互记录。

Button elevation 的当前实现可作为职责示例：`ButtonElevation` 保存状态高度，
每个按钮自己的 `ButtonElevationController` 选择并动画化目标；Surface 的 `ShadowElevation` 消费当前绘制高度。
可共享配置，但不能共享按钮的运行状态。动画优先级绑定在清理后恢复原属性值，不能破坏原有样式与绑定。
这一示例不意味着其他组件都应采用 Button 的状态选择规则。

## 动画接入

动画执行通过 NuGet 依赖 ComposeAnimation，准备方式见 [README](../README.md#本地开发)。
Material 层仍负责 Token、目标选择、时序规则及绘制；不要在组件中另建动画求解器或逐帧驱动循环。

每个应用 TopLevel 在加载 XAML 或内容前调用 `VisualAnimationScope.Initialize(topLevel)`。
`VisualAnimationScope.Create(owner)` 为已附着的控件建立作用域；Ripple、Shape 等效果拥有各自的子作用域。
使用作用域的任务、订阅和动画值绑定来管理执行与清理。停止单个效果不应结束父作用域或兄弟效果；
detach、模板替换与重新附着需要处理旧作用域及绑定的生命周期。根帧源固定，运行中不切换时钟；根的
`AnimationFailed` / `LastError` 用于诊断。

现有 Material 动画入口：

- `SpringSpecRef` 在消费处解析 damping/stiffness，`SpringSpec.ToAnimationSpec()` 转换为运行时 spring。
  阈值属于所动画化标量的单位，例如圆角进度使用 0.01，不能把它当作所有几何的通用阈值。
- `AnimatedCornerRadius` 接受父作用域，拥有子作用域，实现 `IDisposable`；通过 `AnimateToAsync` 改变目标。
  一段运行中的形变保留捕获的 spec，下次目标变化再解析当前 Token。回调必须在端点与进度一致后发布，
  反向或第三目标切换也不能暴露暂时不一致的形状。
- `RippleAnimation` 同样拥有子作用域并可释放；启动 `AnimateAsync`，释放按压时调用 `Finish`。
  调用方需要观察任务并处理正常中断取消，取消和释放应清除等待。两类效果都不暴露手工 `Tick`。

## 当前覆盖与待确认项

现有源码是后续工作的起点，不是批准所有既有取舍的依据。旧注释中的 `design doc §...` 是历史引用，
不指向本文，也不构成当前规范；涉及相应代码时核对真实行为与固定上游。

- 已有 Button、IconButton 和 Surface，但需要逐项检查其 API、交互及视觉覆盖。现有 flat Button
  主题未配置 elevation、焦点呈现等处理，应在相应移植任务中核对，不能自动推广为所有桌面组件的设计规则。
- 原有 Material 动画规则的源码核对基于 AndroidX `ff0a0a99c4f0cfe95d75d5df615e51d399575922`。
  当时记录了状态层 15/45/150ms 淡入淡出、ripple 75ms 进入透明度与 225ms 扩张/中心移动、
  释放后的 150ms 淡出；快速点击仍完成扩张。按压圆角使用 DefaultEffects（damping 1、stiffness 1600）。
  Elevation 进入为 120ms FastOutSlowIn，退出为 150ms（hover 120ms）Bezier(0.4, 0, 0.6, 1)，
  初始和禁用状态直接到目标。这些是原基线记录，不能据此宣称已完成对当前目标的全面对齐。
- [基线核验记录](UPSTREAM_BASELINE.md)保存了版本间的部分源码差异，协调版本时可查阅；ComposeAnimation 的新基线协调仍待完成。
  ripple 使用 common/desktop 路径，Android 原生 RippleDrawable 是另一个参考路径。
- 当前有 Token、主题、交互、生命周期与渲染等测试；[动画生命周期测试](../tests/Material3Expressive.Tests/AnimationLifetimeTests.cs)
  和 [motion 测试](../tests/Material3Expressive.Tests/MotionTests.cs)可作为已有检查入口。
  TestSupport 的 `AdvanceBy` 每次发送一个帧，不插入起始帧；跟踪任务的异常应在推进和释放时暴露。
  测试存在不代表本轮已运行，也不代表 Compose 视觉对比或真实桌面交互已经验收。
- [视觉一致性系统](../tests/VisualParity/README.md)使用独立的双端用例入口，作者显式构建场景、推进时钟和提交测量快照。
  元数据仅标识用例和两端入口，不定义通用场景或输入语言。Agent 读取用例概览和逐采样点详情，开发者可使用 HTML 报告。
  可选宿主将动画帧、框架绘制和被动捕获分开；系统重复性与组件一致性分别验证，不使用容差或文字遮罩消除差异。
  [用例编写](../tests/VisualParity/AUTHORING.md)说明接入与执行边界；[测量语义](../tests/VisualParity/MEASUREMENTS.md)说明事实与比较含义。
  全新克隆与 CI 的依赖限制见 [README](../README.md#本地开发)。
