# Material 3 多层 Token 目标架构

> **状态：目标设计，尚未实现。**
>
> 本目录定义下一轮 Theme/Token 重构的架构契约、模块边界和迁移顺序。当前仓库行为仍以
> [`../architecture/theming.md`](../architecture/theming.md) 和代码为准；上一轮未完成设计已移至
> [`../archive/theme-token-refactor-v1/`](../archive/theme-token-refactor-v1/README.md)。

目标是在 Avalonia 原生资源系统上承载 Material Design 3 的 Reference → System → Component
多层 token graph，并让 ControlTheme 通过标准 `DynamicResource` 消费最终值。重构保留当前 typed
token runtime 的类型、层级和循环诊断能力，但将消费路径从 `ResolvedTheme`/Defaults Adapter 逐步
替换成单 placement 的 `MaterialTheme` provider 与 typed `MaterialValueKey<T>`。

## 阅读目录

| 文档 | 内容 |
|---|---|
| [架构总览](architecture-overview.md) | 目标、非目标、整体数据流、模块职责和核心不变量 |
| [Raw token 模型](raw-token-model.md) | `MaterialToken<T>`、literal/alias/recipe、三层依赖、默认图和覆盖语义 |
| [Material values](material-values.md) | `MaterialValueKey<T>`、Direct/Recipe plan、消费粒度和组件运行时边界 |
| [MaterialTheme provider](material-theme.md) | Children、scope、主题透传、查询顺序、生命周期和通知 |
| [MaterialColorUtilities 集成](material-color-utilities.md) | 颜色 token 所有权、Color Context、catalog contribution、兼容与缓存边界 |
| [Generator](generator.md) | JSONC 输入、composed default catalog、诊断以及未来 values manifest |
| [XAML 与 authoring](authoring-and-xaml.md) | 全局/子树 placement、typed override、DynamicResource 和最终值覆盖 |
| [诊断与测试](diagnostics-and-testing.md) | miss/invalid/cycle 语义、测试矩阵、性能验证和验收门槛 |
| [迁移计划](migration-plan.md) | 当前残留、Button 纵向切片、双轨迁移、清理与回滚边界 |

## 一句话模型

```text
DynamicResource MaterialValueKey<T>
    → 最近的 MaterialTheme placement
    → MaterialValue plan 读取一个或多个 typed Raw tokens
    → 每个 Raw dependency 从 placement.Owner 重新沿资源树查找
    → 整条资源树 miss 时才读取 composed DefaultRawTokens
    → 返回 consumer-ready、非 null 的 T
```

这是一套 **boundary-relative resolution**：控件先找到最近的 `MaterialTheme`，之后 Raw graph 的每一跳
都从该 placement 的 `Owner` 开始。子树 token override 因而必须与一个 `MaterialTheme` 边界一起放置；
普通 requester 与该边界之间的 Raw resource 不参与解析。

## 已冻结的决策

1. Raw 和 Flat 使用不同的 typed key domain：`MaterialToken<T>` 与 `MaterialValueKey<T>`。
2. `MaterialTheme` 是单 Owner、单 placement 的 `ResourceProvider`，通过 `MergedDictionaries` 安装。
3. 第一版没有跨请求 Raw/Flat materialization cache；查询按请求即时解析。
4. Avalonia 传入的 `ThemeVariant?` 是权威参数，在同一次解析中原样贯穿所有 Raw dependency。
5. Children 是本 placement 的 sparse Raw sources，later child wins；本地 miss 必须允许同 scope 其他
   provider 和祖先继续查找。
6. `DefaultRawTokens` 是不可变共享的 composite catalog，由 generated M3E definitions 与显式导入的
   domain contributions（第一版包括 MCU colors）组成；它只在完整 Owner 资源链 miss 后作为终端
   fallback，绝不作为每个 placement 的本地 dense source。
7. Flat schema 是显式的消费契约，不是全部 Component Raw token 的机械镜像。
8. `MaterialValueKey<T>` 的 plan 第一版只有 Direct Raw 与显式依赖的纯 Raw recipe；禁止 Flat → Flat、
   requester/Owner/Bounds/service 访问和 nullable final value。
9. Variant、size、interaction、bounds、RTL 和动画仍由 ControlTheme、selector 或 presenter 负责；
   MaterialTheme 不成为控件行为引擎。
10. Raw override 是正式语义定制面；同 key 的普通 Avalonia final resource 只是 presentation escape hatch。
11. Material resolver 控制范围内的配置错误、类型错误、非法层级、缺失默认值和 cycle 必须 fail-fast，
    不得静默回退到祖先 Flat provider；普通字典中的 Flat escape hatch 不受这套运行时类型检查保护。
12. 只有实测证明需要时才增加持久 cache；设计不能预先依赖 cache 才正确。
13. Raw key 公开用于 typed authoring 与 alias identity，但不是控件的直接 `DynamicResource` 消费协议；
    原样消费也通过 `DirectRaw<T>` value key，确保终端 default fallback 只发生在完整解析 session 中。
14. MCU Avalonia 是 Reference palette 与 System color key/算法的 canonical owner；M3E 不复制 color
    descriptor 或 `DynamicScheme` 规则，只组合 MCU catalog contribution。

## 第一版明确不做

- 不生成中央 `FlatToken enum`、全量 Flat 列表或 `Flat → Raw` switch。
- 不允许公开任意 `Func` 作为 value projector。
- 不让 MaterialTheme 根据 `Owner.ActualThemeVariant` 重写请求 theme。
- 不自动为约两千个 Raw token 生成公开 Flat API。
- 不把 variant × size × state 展开成完整笛卡尔积。
- 不在 Raw `.comp.jsonc` 中加入 Avalonia 控件行为 DSL。
- 不在第一阶段删除 `ResolvedTheme`；它在双轨迁移完成前保留兼容职责。

## 仍需由纵向切片验证

以下内容保留为实现期验证点，而不是尚未定义的基础语义：

- leaf value 与 immutable component profile 的实际粒度；
- Button 所需的最小标准 projection 集合；
- 哪些 value keys 构成 public ControlTheme API，哪些仅为 library AXAML glue；
- 是否开放受约束的应用自定义 value-key factory，或让 v1 catalog 仅由库拥有；
- 是否需要独立 `*.values.jsonc`，以及它应支持的有限 operator；
- 无持久 cache 时大规模 `ResourcesChanged` 广播的实际成本。

这些验证不得改变 Raw/Flat 隔离、theme 透传、终端 default fallback 或纯 recipe 等核心不变量。
