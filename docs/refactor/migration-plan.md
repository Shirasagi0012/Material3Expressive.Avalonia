# Token 架构迁移计划

> 本文是目标架构的实施顺序与回滚边界，不表示这些阶段已经完成。每个里程碑只有在验收条件全部
> 满足后才能标记完成。

相关设计：

- [架构总览](architecture-overview.md)
- [Raw Token 模型](raw-token-model.md)
- [Material value](material-values.md)
- [MaterialTheme](material-theme.md)
- [MaterialColorUtilities 集成](material-color-utilities.md)
- [Generator](generator.md)
- [XAML authoring](authoring-and-xaml.md)
- [诊断与测试](diagnostics-and-testing.md)

## 1. 当前基线

仓库目前不是一套已经统一的新 runtime，而是两条路径并存：

```text
旧主路径
MaterialTheme
  → ResolvedTheme snapshot / inherited ResolvedThemeProperty
  → component Defaults / MaterialElementContext
  → ControlTheme / presenter

半完成 typed-token 路径
MaterialToken<T> + generated Keys/Overrides
  → requester-local MaterialTokenResolver / TokenReader
  → component Defaults adapter
  → ControlTheme / presenter
```

[`ResolvedTheme`](../../src/Material3Expressive/Foundation/Theming/ResolvedTheme.cs) 仍服务绝大多数控件，
[`MaterialThemeScope`](../../src/Material3Expressive/Foundation/Theming/MaterialThemeScope.cs) 仍发布兼容
snapshot。当前 generator 已为 token-set 生成 typed descriptor/default graph/override facade，但它们存在
并不等于控件已经消费这些资源。

颜色域还存在一条特殊桥接：MCU Avalonia 以 `ColorScheme → ResolvedColorScheme → inherited
ResolvedSchemeProperty/ColorTokenBinding` 服务 standalone authoring，而 M3E 又创建
`ReferencePaletteTokens`/`SystemColorTokens` wrapper，并在当前 `MaterialTheme.TryGetResource` 中直接读取
MCU snapshot。迁移必须收敛 canonical key owner，但不能把 MCU 算法复制进 M3E。

Button 是目前唯一完整走通 `TokenReader` 消费、资源变化重读与 defaults adapter 投影的组件：

- [`ButtonDefaults`](../../src/Material3Expressive/Controls/Defaults/ButtonDefaults.cs)
- [`ButtonDefaultsAdapter`](../../src/Material3Expressive/Controls/Defaults/ButtonDefaultsAdapter.cs)

因此 Button 是新目标架构的最佳 vertical slice，但它也需要从
`TokenReader -> semantic records -> adapter` 再迁移到
`MaterialTheme -> DynamicResource MaterialValueKey<T>`；不能把现有 Button 路径误称为最终状态。

## 2. 目标状态

迁移完成后的消费链为：

```mermaid
flowchart LR
    A["Reference/System/Component Raw graph"] --> B["scoped MaterialTheme placement"]
    B --> C["MaterialValueKey<T> resolution plan"]
    C --> D["Avalonia DynamicResource"]
    D --> E["ControlTheme / semantic property / presenter"]
```

目标状态具有以下性质：

- 每个 `MaterialTheme` 实例同时只挂在一个 resource placement；
- Raw lookup 以 placement Owner 为边界，alias 每一跳都从同一 Owner 重新查找；
- 请求者的原始 `ThemeVariant` 在完整解析中保持不变；
- scope/ancestor 全部 miss 后才使用共享、不可变的 `DefaultRawTokens`；
- `DefaultRawTokens` 组合 generated M3E definitions 与 MCU color contribution，contributor 顺序不构成
  override precedence；
- MCU Avalonia 唯一拥有 Reference palette/System color identity、Color Context 与 dynamic color 算法；
- `MaterialValueKey<T>` 是稳定消费契约，plan 只依赖显式声明的 typed Raw tokens；
- ControlTheme 优先通过原生 `DynamicResource` 消费 value key；
- 第一版没有跨请求 Raw/Flat cache，也没有中央 Flat enum；
- `ResolvedTheme`、`TokenReader` adapter 和旧 theme transport 最终退出组件消费路径。

## 3. 迁移约束

### 3.1 双轨不等于双重求值

迁移期允许旧、新两条架构同时存在于仓库，但一个具体消费槽在运行时只能有一个权威来源。例如 Button
的 `Background` 不能同时由旧 adapter 写入，又由 `DynamicResource` 提供。否则优先级、更新次数和
回滚行为均无法判断。

每个组件切片必须定义：

- 哪些 property/semantic slots 已切到 value key；
- 哪些仍由 legacy defaults 提供；
- 两组写入边界不重叠；
- 删除新切片后，旧路径能完整恢复。

### 3.2 不在迁移中重写设计事实

迁移首先改变 token 的分发和消费，不顺带改变视觉规范。发现 Compose/Avalonia 差异时，应先记录
现有结果与目标来源，再作为独立、带 provenance 的改动处理。这样 visual regression 才能归因。

### 3.3 每阶段保持可构建

不得先删除 `ResolvedTheme` 再补齐消费者，也不得让 generated Keys 与 default catalog 来自不同图。
基础设施可以先以未被生产控件使用的形式合入；组件以可回滚的 vertical slice 逐个切换。

## 4. 阶段计划

### M0：冻结基线与设计

工作内容：

- 将旧重构说明归档，以本目录文档作为目标架构依据；
- 记录当前 TokenGenerator、runtime、theme scope、MCU ColorScheme/binding 与 Button tests 的基线；
- 建立当前 `ResolvedTheme` consumers 与 token table coverage 清单；
- 明确术语：Raw token、Material value、placement、boundary-relative lookup。

验收：

- 当前相关单元测试、集成测试与 Button visual baseline 可重复运行；
- 清单能回答每个控件当前从哪里取得 color/type/shape/motion，并区分 MCU standalone binding 与 M3E
  internal consumer；
- 本阶段不改变运行时行为。

回滚边界：仅文档与测试清单，可整体撤销，不涉及兼容性。

### M1：建立目标 runtime primitives

工作内容：

- 冻结 `MaterialToken<T>` identity 与不可变 `TokenDefinition<T>`；
- 抽出不依赖 M3E 组件的共享 Raw contract assembly，使 MCU Avalonia 与 M3E 使用同一 descriptor、
  definition、recipe、source 与 catalog contribution contract；
- 引入 `MaterialValueKey<T>`、Direct/Recipe plan 与单次解析 session；
- 保留类型、依赖层级、完整 cycle path 与 non-null final value 诊断；
- plan 第一版只允许 Flat/Material value 依赖 Raw，不允许 value -> value；
- 不加入跨请求 cache。

验收：

- literal、alias、recipe、多依赖 recipe、错误类型、逆层依赖、cycle 均有单元测试；
- 同一 session 可 memoize 公共 Raw dependency，但 session 结束后无持久状态；
- runtime 不读取 requester state、bounds、时间或服务；
- 共享 contract 不引用 M3E、ControlTheme 或 MCU Core，package dependency 无环。

回滚边界：新 primitives 尚未接入生产 XAML，可以直接删除，不改变旧 consumer。

### M2：生成并组合完整 `DefaultRawTokens`

工作内容：

- 扩展现有 incremental generator，生成 M3E-owned、不可变的默认 definition contribution；
- 在 MCU Avalonia 建立 canonical Reference/System color keys、内部 Color Context token、color recipes、
  terminal baseline contribution、typed override/XAML factory 与 external declaration metadata；
- 由统一 `DefaultRawTokens` 显式组合 M3E generated contribution 与 MCU contribution；重复 key owner
  必须失败，组合顺序不表示 override；
- catalog 允许 variant-specific baseline，并按每个 key 的
  `exact → InheritVariant 链 → Default` 查找；
- 继续生成 raw transcription、typed descriptors 与 sparse typed overrides；
- 让 typed override facade 支持 Children source 与独立 Raw provider 两种安装形态及对应通知；
- 保留 provenance、存在性、类型、层级与 cycle 编译诊断；
- 保留参数化 custom key family，不引入有限 enum 替代；
- 生成 built-in/source-required membership metadata，不通过 token path 猜测 missing policy；
- M3E generator 对 color role 发射 MCU canonical singleton alias，不生成同 path wrapper 或第二套算法；
- 不生成 Flat enum/list/`ToRaw`，不引入 `*.values.jsonc`。

验收：

- 每个 generated Raw descriptor 与 MCU-owned canonical dependency 都能在 composed catalog 中取得
  definition；
- Light/Dark/custom baseline fallback 对每个 key 独立执行，跨 alias/recipe hop 始终保留原始
  `queryTheme`；
- 默认 Component -> System -> Reference 图未被提前拍扁；
- generator incremental、determinism、snapshot 与 compiled-XAML tests 通过；
- override facade 的两种安装形态、单 placement 约束和变化通知测试通过；
- M3E compatibility color facade 与 MCU catalog 返回同一 descriptor instance；
- MCU custom color 缺失在新 resolver 中明确失败，不使用 Transparent sentinel；
- 旧 generated API 如需暂时保留，有明确 adapter/deprecation 路径。

回滚边界：旧 per-descriptor default graph 与 M3E color wrapper 暂时保留为兼容输出，但 wrapper 必须转发
MCU singleton；新 resolver 未成为唯一入口前可回退。

### M3：实现 `MaterialTheme` placement 与资源语义

M3 显式依赖 M2 的 composed catalog；不使用临时 dense defaults 或测试 fixture 冒充目标 fallback。

工作内容：

- `MaterialTheme` 成为不可共享的单 placement `ResourceProvider`；
- 补齐 `HasResources`、owner add/remove、Children 增删/重排及事件订阅生命周期；
- Raw miss 继续同 scope/祖先查找，整条链 miss 后才进入 `DefaultRawTokens`；
- Flat/value 请求从 placement Owner 解析，原始请求 `ThemeVariant` 全程透传；
- Children 后添加者优先；一次 authoring 变化只发出一次资源失效通知；
- `ColorScheme` 或 MCU adapter 作为 sparse context source，只提供内部 Color Context；无本地 scheme 时
  继承祖先，全链 miss 时才使用 MCU terminal baseline；
- target `MaterialTheme` 不为每个 placement 自动创建会遮蔽祖先的本地默认 Tonal Spot source；
- 不订阅 Owner 事件来维护 cache，因为第一版没有持久 Flat cache。

验收：

- global/local placement、祖先 override、局部 alias 重绑定、Light/Dark/custom variant 回退通过；
- attach、detach、reattach、Children replace/clear 与 GC/lifetime 测试通过；
- 已识别 value key 的解析错误 fail-fast，不会越过当前 placement 偷用祖先 Flat provider；
- 同一实例同时添加到第二个 Owner 时给出明确错误；
- scheme seed/contrast/platform/spec/custom colors 变化整体替换 MCU snapshot，并只广播一次资源变化；
- M3E 新路径不读取 inherited `MaterialColor.ResolvedSchemeProperty` 或 `$self` ColorTokenBinding。

回滚边界：placement 先作为 opt-in provider 存在；旧 `MaterialThemeScope` snapshot transport 保留。

### M4：Button 目标架构 vertical slice

工作内容：

- 在 Button 组件旁用 C# 手写最小 `ButtonValueKeys`；
- 覆盖 Direct、Color、Color+Opacity、Thickness、shape、typography 等真实消费类型；
- variant、size、state 由 ControlTheme selector 或极薄组件 policy 选择具体 value key；
- 将选定 Button consumption slots 切为 `DynamicResource MaterialValueKey<T>`；
- bounds、flow direction、interaction 与 animation 继续由 presenter 处理。

验收：

- 默认外观与迁移前 Button parity baseline 一致；
- component/system/reference override 均能实时更新；
- 默认、祖先与局部 ColorScheme context，System color override 和 MCU terminal baseline 均通过；
- 局部 ColorScheme 不隐式清空祖先显式 System override，未覆盖 roles 使用局部 context；
- 明暗与 custom variant 切换、局部 scope 隔离、alias 重绑定通过；
- MCU named custom color 存在时解析、缺失时 fail-fast；Reference tone override 不被误当成重建
  `DynamicScheme` 的输入；
- Setter、ControlTemplate、local value 优先级符合 Avalonia 原生语义；
- 资源/variant 变化不会重复重算或造成 subscription leak；
- 针对每个已切槽位，运行时只有 value-key 路径写入。

回滚边界：保留现有 `TokenReader` Button adapter 直到整个切片验收。切换提交应能整体撤销以恢复 adapter，
而无需撤销 M1–M3 基础设施。

### M5：收敛 Material value 词汇

工作内容：

- 根据 Button 切片总结标准 projector/operator；
- 判断 leaf key 与 immutable profile key 的实际粒度；
- 再选一个结构不同的组件验证 Button 经验能否复用；
- 只有重复模式稳定后，才设计独立 `*.values.jsonc` schema。

验收：

- operator 可静态声明全部 typed dependencies；
- 没有 operator 读取 control/requester 或执行任意 C#；
- `.comp.jsonc` 未加入消费行为；
- 手写 C# 与未来 manifest 的责任边界有测试和示例。

回滚边界：不稳定 operator 保持组件本地 C#；可以不引入 values schema，不影响 Raw 架构。

### M6：按组件族迁移

建议按风险分批，而不是按文件数量机械转换：

1. 纯 leaf color/metric 的简单控件；
2. Button-family、toggle、selection 等共享状态模型的控件；
3. typography、shape profile 较重的控件；
4. adaptive/layout、bounds-dependent shape 与复杂 motion 控件；
5. overlay、scaffold 等跨 owner/lifetime 场景。

每个组件都重复 M4 的小闭环：盘点消费槽、声明 value keys、并行准备但单路径切换、parity/override/
lifetime 验证、删除该组件的旧写入。

验收：

- 组件清单逐项记录迁移状态，不以 generated token coverage 冒充 consumer coverage；
- 每批次拥有独立测试标签和视觉基线；
- 新组件默认直接采用目标路径，不再新增 `ResolvedTheme` consumer。

回滚边界：以组件或紧密组件族为单位提交与回退；禁止一个提交同时删除多个无关 legacy consumer。

### M7：切换默认 authoring/XAML 路径

工作内容：

- 文档与示例默认使用 `MaterialTheme` placement、typed Raw override 与 value-key `DynamicResource`；
- color authoring 默认使用 MCU canonical ColorScheme source/key API；M3E forwarding facade 只出现在兼容说明；
- 验证 Application、Window、局部 subtree 的标准 placement 方式；
- 将 legacy snapshot APIs 标为过渡兼容，不再用于新示例；
- 清点 presentation-level Flat override escape hatch，并记录其不进入 Raw graph。

验收：

- 新用户无需接触 `ResolvedTheme` 或 `TokenReader` 即可完成全局与局部主题定制；
- XAML 编译、热重载、attach/detach 和多 Window 场景通过；
- authoring 文档与实际 resource precedence 一致。

回滚边界：文档默认值和 API deprecation 可回退；尚未删除 legacy transport。

### M8：全库切换审计

工作内容：

- 搜索所有 `ResolvedTheme`、`ResolvedThemeProperty`、`TokenReader`、legacy adapter 与旧 binding helper；
- 搜索 M3E 内部独立 color intern table、`MaterialColor.ResolvedSchemeProperty` consumer 与
  `ColorTokenBinding` consumer；MCU standalone compatibility API 本身不算 M3E 遗留 consumer；
- 区分仍属 MaterialTheme 内部算法的临时使用和组件消费使用；
- 对全库执行 functional、visual parity、theme mutation、scope 与 GC 压力测试；
- 记录 public API breaking change 与升级说明。

验收：

- 没有生产组件从 `ResolvedTheme` 读取 Material value；
- 没有 production ControlTheme slot 同时被旧、新路径写入；
- 所有组件的 raw/value coverage 与测试清单闭合；
- 性能测量证明按请求解析可接受，或已有独立数据支持后续 cache 设计。

回滚边界：此阶段仍保留 legacy 类型，但应停止使用。若审计失败，回退具体组件切片，不在此阶段
临时加入全局 cache 或改变解析语义。

### M9：删除兼容架构

只有 M8 全部通过后才执行：

- 删除 `ResolvedTheme` 的组件消费职责及 inherited transport；
- 删除 `TokenReader` consumer adapters 与仅为它们存在的 bridge；
- 删除旧 `ThemeBinding`/snapshot invalidation plumbing；
- 删除 M3E 对 MCU inherited snapshot/binding 路径的消费与独立 color key table；
- 保留 MCU standalone compatibility API，除非 MCU 自己通过独立 breaking migration 改变它；
- 移除迁移期 obsolete API 与 suppression；
- 更新 breaking-change migration note。

验收：

- 全库构建、单元/集成/compiled-XAML/visual parity tests 通过；
- public API 基线只包含明确批准的 breaking changes；
- `rg` 审计没有遗留旧消费入口；
- 默认、局部 override、variant、lifetime 行为与 M8 基线一致。

回滚边界：M9 必须是独立的清理提交/PR。若出现回归，首先恢复兼容类型与 transport；不得通过让部分
组件重新依赖已删除 snapshot 来形成第三种混合状态。

## 5. 每个组件的迁移清单

组件切片至少回答以下问题：

- Raw facts 来自哪些 `.comp.jsonc` / System / Reference tokens？
- 哪些 slots 是 Direct，哪些需要纯 multi-Raw recipe？
- variant、size、state 在 selector、control policy 还是 presenter 中决定？
- 哪些值依赖 bounds、RTL、interaction 或 animation，因而不能提前投影？
- 哪些 legacy AP/record 是真实稳定消费契约，哪些只是 adapter 传输结构？
- local value、style、template、Flat override 与 Raw override 的预期优先级是什么？
- resource/theme mutation 后由谁触发重新求值？
- 切换后可以删除哪些订阅、binding 与 snapshot reads？

推荐为每个组件维护一个短表：

| 消费槽 | 旧来源 | 新 `MaterialValueKey<T>` | 选择轴 | 最终 owner | 状态 |
|---|---|---|---|---|---|
| 示例：Background | defaults adapter | `FilledContainerBrush` | variant/state | ControlTheme | planned |

这张表比“该组件的 token key 已生成”更能准确表示迁移进度。

## 6. 通用验收门槛

任何组件或组件族只有同时满足下列条件才算完成：

1. **正确性**：默认值、Raw alias、recipe、scope override，以及每 key
   `exact → InheritVariant → Default` 且跨 hop 保留原始 query theme 的语义通过。
2. **Avalonia 集成**：`DynamicResource`、Setter、template、local value 和资源优先级通过。
3. **生命周期**：attach/detach、资源替换、热重载路径与 GC 无泄漏。
4. **视觉**：Compose/Avalonia parity baseline 无未解释变化。
5. **诊断**：missing、wrong type、cycle、invalid layer 能定位完整 key path 与 placement。
6. **单一权威**：该槽位不存在旧、新双写。
7. **回滚**：旧路径删除前，有一个可单独撤销的切换边界。

## 7. 性能与 cache 决策

第一版明确不实现持久 Flat cache。性能验收先测量真实场景：

- 初次 template materialization；
- theme/variant mutation 后的 DynamicResource 重新查询；
- 深 scope 与多级 alias/recipe；
- Button 列表等高密度场景；
- allocation 与 event subscription 数量。

只有 profiling 表明按请求解析是实际瓶颈，才单独设计 cache。届时至少需要以
`(MaterialValueKey, ThemeVariant, generation)` 区分结果，并重新论证失效顺序、custom variant、内存上限
与 owner lifecycle。cache 优化不得夹带在组件迁移提交中，也不得改变已冻结的解析语义。

## 8. 计划完成的判定

整个迁移完成不是“所有 JSON 都生成了 descriptor”，而是：

- 所有生产 ControlTheme/presenter 都从 `MaterialValueKey<T>` 或明确的 presenter-owned profile 消费；
- 所有 Raw override 都按 placement boundary 和完整 resource cascade 生效；
- `DefaultRawTokens` 是唯一内置默认图；
- MCU 是 Reference/System color identity 与算法的唯一 owner，M3E 只消费其 contribution；
- `ResolvedTheme`/`TokenReader` 不再承担组件 token 分发；
- Generator、runtime、XAML、diagnostics 与 visual parity 测试共同覆盖架构契约；
- M9 的兼容清理已独立完成。

在达到这些条件前，文档和发布说明必须继续把架构标为迁移中。
