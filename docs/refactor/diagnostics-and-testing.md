# 诊断与测试策略

> **状态：runtime 诊断已落地，完整验证待用户允许执行。** 本文定义 Token/Value 架构必须具备的
> 失败语义、诊断信息和验证矩阵。
> 当前测试通过只能作为重构前 baseline，不能证明本文描述的目标行为已经存在。

相关模型见 [Raw Token 模型](./raw-token-model.md)、[Material Value](./material-values.md)和
[MaterialTheme Provider](./material-theme.md)；MCU 颜色边界见
[MaterialColorUtilities 集成](./material-color-utilities.md)，使用示例见
[Authoring 与 XAML](./authoring-and-xaml.md)。

## 1. 诊断原则

目标 runtime 必须遵循四条原则：

1. **miss 可组合，invalid 不可回退。** 没有定义允许继续查找；命中了非法定义必须立即失败。
2. **最近 Flat boundary 对已识别 key 负责到底。** 一旦识别 `MaterialValueKey<T>`，解析失败不得退回
   祖先 Flat provider。
3. **报告路径，不只报告最后一个 key。** 错误必须保留 Flat endpoint、Raw 依赖链、请求 theme 和
   能可靠取得的 source/scope 信息；Avalonia 未暴露的获胜 provider 不得伪造。
4. **异常是 authoring/configuration 诊断，不是普通控制流。** 正常 miss 通过 `false` 表达；图损坏、
   类型错误、cycle 和 projector failure 通过结构化异常表达。

第一版不保留跨请求 Flat cache。因此测试首先验证按请求解析的正确性，而不是 cache invalidation。

## 2. 必须区分的结果

| 情况 | 含义 | resolver 行为 |
|---|---|---|
| source miss | 当前 child/scope 没定义这个 Raw key | 继续同 scope 的其他 source，再继续祖先 |
| 全部显式 Raw source miss | Owner 资源链都没有定义 | 查询 composed `DefaultRawTokens` |
| 命中 `null` | source 声称有定义，但 definition/value 非法 | fail-fast；不得当作 miss |
| 错误类型 | definition、literal、alias target 或 value 与声明的 `T` 不一致 | fail-fast，报告 expected/actual |
| 非法依赖方向 | 依赖从基础层指向更高层，例如 Reference → System | fail-fast，报告 source/target layer |
| cycle | Raw alias/recipe graph 重新进入活动路径中的 token | fail-fast，报告完整闭环 |
| catalog-owned default missing | 完整资源链与 composed defaults 都没有定义 | 视为 generator/domain contribution/架构缺陷并 fail-fast |
| source-required custom missing | custom token 按设计没有 catalog baseline，资源链也未提供定义 | fail-fast，指出必须由 source 提供 |
| MCU named custom color missing | MCU family recipe 存在，但当前 Color Context 没有对应 palette | fail-fast，报告 name/role/tone/context；不得返回 Transparent |
| projector failure | Material Value plan 抛异常、返回 `null` 或产生错误类型 | fail-fast，附带 Flat key、dependencies 和 inner exception |
| 普通字典中的 Flat override 违反 key 契约 | `MaterialTheme` 之前已命中 presentation escape hatch | 不回退祖先；v1 不保证运行时诊断，值可能成为 unset 或被属性转换接受 |

### 2.1 Miss 不是错误

稀疏 source 的核心能力就是 miss：

```text
local Children miss
  → current host remaining resources
  → parent scopes
  → Application
  → DefaultRawTokens
```

`Clear` 删除当前 source 中的 definition，使结果重新变为 miss。它不创建一个 `null` definition，也不
直接选择 composed default。

### 2.2 命中 `null` 必须失败

Material resolver 产出的最终 value 不支持 `null`。Raw source 如果返回“命中”同时给出 `null`，说明 provider 契约
已经损坏；若把它当作 miss，会让错误配置在不同 scope 中得到不可预测的祖先值。

XAML `DynamicResource` 的最终路径也不能可靠表达“有效的 null resource”和 miss 的差异。因此目标
架构明确禁止 final value 为 `null`。需要表达“无值”的消费类型必须使用显式 option/value object，
不能借用 resource null。

### 2.3 错误类型必须指出发生阶段

诊断至少要区分：

- Raw source 返回的对象不是该 `MaterialToken<T>` 接受的 definition 或 literal；
- alias target 的 `T` 不兼容；
- recipe dependency 声明的类型不匹配；
- recipe/projector 的最终返回值不符合 `MaterialValueKey<T>`；
- presentation-level Flat override 命中，但资源值与 key 的 `T` 不兼容。

前四类位于 Material resolver 的控制范围内；其错误应同时包含期望类型、实际类型、key path、请求
theme，以及能够可靠取得的 source 信息，不能只依赖最终 Avalonia property conversion error。

最后一类是明确的边界例外：普通 `ResourceDictionary` 会先于 `MaterialTheme` 返回同 key 的资源，
Provider 没有机会验证它或附加 Raw graph 上下文。此时资源系统不会因 key 的 `T` 契约而继续祖先，但
也不保证报错：`null` 可能成为 unset，某些不同类型可能被属性转换接受。未来可以用 analyzer 或 typed
authoring helper 强制检查；v1 只测试资源优先级和实际 Avalonia 行为，不把结构化异常写成契约。

### 2.4 非法层级方向

允许的 Raw 依赖方向为：

```text
Component → Component / System / Reference
System    → System / Reference
Reference → Reference
```

不允许：

```text
Reference → System / Component
System    → Component
```

generator 应优先拒绝静态默认图中的非法方向；runtime 仍要校验动态 override 中的 alias/recipe，防止
绕过生成期检查。

### 2.5 Cycle 与深度保护

cycle 诊断必须输出真正闭环：

```text
System.Color.Accent
  → System.Color.Container
  → System.Color.Accent
```

只报告“超过最大深度”不足以定位问题。最大深度可以作为恶意或异常图的额外防御，但不能替代活动
路径集合和完整 cycle path。

v1 的 Material Value plan 只允许 Flat → Raw，不允许 Flat → Flat，因此 Flat graph 本身不应产生
cycle。若将来开放 Flat → Flat，必须先扩展同等级的 cycle 诊断和测试，再改变该限制。

### 2.6 Catalog definition missing

`MaterialToken<T>` 只携带 identity、Path、layer 和 value type；baseline definition 不存放在 token
descriptor 上。`DefaultRawTokens` 是独立的 composite、不可变、共享 catalog，由 generated definitions
与 imported domain contributions 组成，并允许
variant-specific baseline。其 `TryGetDefinition(token, queryTheme)` 对每个 Raw key 按
exact → `InheritVariant` → Default 查询；命中某个 fallback 不得改写 session 保存的原始
`queryTheme`。

对于 catalog 声明应有 baseline 的 canonical token，default entry 缺失属于构建产物或架构错误，
而不是用户没有配置主题。

诊断应明确说明：

```text
explicit resource chain: miss
composed default catalog: miss
required by: Value.Button.Filled.ContainerBrush
missing raw token: Component.FilledButton.ContainerColor
```

generator/contribution 测试应尽量让此错误在编译期出现；runtime 检查是防御层。

参数化 custom token 是例外。它可能有稳定 descriptor，但按设计没有 `DefaultRawTokens` entry；此时必须
由当前或祖先 source 提供 definition。完整资源链 miss 应报告 `MissingTokenDefinition`（名称可在实现时
调整），并明确 `sourceRequired: true`，不能误报 generator 丢失 default，也不能凭空生成 literal。
分类必须来自 composed catalog/family membership metadata；不能依赖 path 前缀、类型或一次
`TryGetDefinition` miss 来猜测。

MCU parameterized custom family 可以在 catalog 中拥有通用 recipe，但没有无条件的 named palette 值。
recipe 找不到 `Brand` 等 palette 时，应报告 MCU configuration failure；这既不是 catalog definition 缺损，
也不是允许继续查找祖先 Flat provider 的 miss。

### 2.7 Projector failure

Material Value recipe/projector 是受约束的纯计算，但仍可能因为实现缺陷失败。以下情况必须包装为
`MaterialValueResolutionException`（名称可在实现时调整）：

- projector 抛出异常；
- projector 返回 `null`；
- 非泛型边界上出现实际输出类型错误；
- projector 读取未声明 dependency；
- projector 试图读取 Flat key、Owner、requester 或其他不允许的上下文。

异常保留原始 inner exception，同时列出已声明 dependency 及其解析路径。不要捕获后返回 miss。

## 3. Recognized Flat key 必须 claim

资源 provider 的普通语义是：返回 `false` 后继续查找祖先。对于未知 key，这是正确行为；对于已经被
当前 placement 识别的 `MaterialValueKey<T>`，则会产生危险的静默降级。

假设子树 placement 识别 `ButtonValueKeys.FilledContainerBrush`，但其 Raw graph 类型错误。如果它
返回 `false`，Avalonia 可能命中 Application 中另一个 MaterialTheme 的同名 Flat key，使子树看起来
“还能工作”，同时绕过了子树配置和诊断。

目标规则是：

```text
unknown resource key
  → return false

recognized MaterialValueKey
  → resolve and return a non-null T
  → or throw a structured resolution exception
  → never return false because resolution failed
```

同理，当前 scope 的普通 `ResourceDictionary` 若直接命中 public Flat key，但值为 `null` 或错误类型，
也不应被解释为“继续祖先 Flat provider”。该 escape hatch 必须承担它所覆盖 endpoint 的类型契约；
由于命中发生在 Provider 之外，v1 不验证该契约，也不保证 Material 或 Avalonia 会抛出异常。

placement 尚未拥有 Owner 时无法解析 Flat key。若框架在挂载前进行了显式查询，应抛出包含 placement
状态的明确异常，而不是空引用异常或祖先回退。

## 4. 结构化异常与完整路径

建议为所有解析错误提供共同基类，并以稳定枚举区分 failure kind。具体公开程度可在实现阶段决定，
但测试不应依赖拼接字符串才能判断错误种类。

概念数据结构：

```csharp
public enum MaterialResolutionFailureKind
{
    NullDefinition,
    WrongType,
    IllegalDependencyDirection,
    Cycle,
    MissingCatalogDefinition,
    MissingTokenDefinition,
    MissingDomainConfiguration,
    ProjectorFailure,
    UnownedPlacement,
}

public sealed record MaterialResolutionFrame(
    string Path,
    TokenLayer? Layer,
    Type ExpectedType,
    string? Source,
    ThemeVariant? RequestedTheme);
```

一次失败至少应能回答：

- 正在解析哪个 `MaterialValueKey<T>`；
- 当前 Raw token 及完整 alias/recipe 路径是什么；
- 失败发生在不透明的 Owner resource chain、composed default catalog 还是 projector；若 source 主动提供
  provenance，再报告具体 child/provider/host；
- 原始请求 `ThemeVariant` 是什么；
- expected/actual type 或 source/target layer 是什么；
- 如果是 projector failure，声明了哪些 dependencies，inner exception 是什么。

推荐的人类可读输出：

```text
Failed to resolve material value 'Value.Button.Filled.DisabledContainerBrush' (IBrush).
Requested theme: HighContrastDark.
Raw path:
  Component.FilledButton.DisabledContainerColor (Color)
  → System.Color.OnSurface (Color)
Failure: token source returned System.String; expected Color.
Source: local MaterialTheme child 'BrandOverrides'.
```

Path 使用 descriptor 的稳定诊断名称，但 resource identity 仍是 singleton object identity；不要在错误恢复
过程中根据 path 字符串重新查找 key。

`IResourceHost.TryFindResource` 只返回命中状态和值，不返回获胜的 host/provider。resolver 因而不能在不
复制 Avalonia 查找算法的前提下可靠区分“当前 host 普通资源”与“某个祖先 provider”。`Source` 字段必须
允许为空或使用 `OwnerResourceChain` 这样的不透明分类；只有 local child 自己抛错、definition 显式携带
provenance，或命中可识别的 catalog contribution 时，才报告更精确来源。上面的 `BrandOverrides` 是 local child
能提供的可用信息示例之一，不是所有 Owner-chain hit 的强制能力。

## 5. 单元与集成测试矩阵

下表是目标架构的最低验证范围，不是建议性的样例列表。

| 领域 | 必须覆盖的行为 |
|---|---|
| Raw literal | Reference/System/Component 各层 literal；非 null；类型正确 |
| Raw alias | 同层 alias、向基础层 alias、跨 scope 重绑定、共享依赖的单请求 memoization |
| Raw recipe | 显式 dependencies、纯求值、依赖类型、非法未声明读取 |
| source precedence | Children 靠后者优先；miss 继续；命中 invalid 不继续 |
| default graph | 仅在完整 Owner 链 miss 后使用；ancestor override 优先；generated/imported contribution 可组合；重复 owner 或 canonical default 缺失失败 |
| custom token | 有显式 source 时解析；无 catalog entry 且全链 miss 时报告 source-required missing |
| missing policy | built-in catalog 缺损与 source-required custom 使用不同 failure kind；未知扩展默认 source-required |
| MCU colors | canonical key identity、Color Context 继承、terminal baseline、scheme mutation、custom-missing 与 core parity |
| Material Value | Direct plan、multi-Raw recipe、consumer-ready 类型、non-null 输出 |
| direct consumption | ControlTheme 通过 Direct value key 原样消费 Raw；不把 Raw key 直接用作 `DynamicResource` |
| boundary | 最近 placement claim Flat key；控件到边界之间的 Raw override 不可见 |
| scope | Application、window/control 子树、嵌套 placement、sibling isolation |
| Flat escape hatch | 普通资源中的同一 Flat key 优先；违反 `T`/non-null 契约也不回退；记录实际 unset/转换行为，不要求异常 |
| diagnostics | resolver 内的 null、wrong type、illegal direction、cycle、missing default、projector failure 的结构化字段和完整路径；source provenance 按可得性报告 |

### 5.1 Scope 与 alias 重绑定场景

至少建立以下树形 fixture：

```text
Application placement
  System.Primary = Blue
  Component.Button.Container → System.Primary

├─ left subtree placement
│    System.Primary = Red
│    Button should resolve Red
│
└─ right subtree
     Button should resolve Blue
```

还要覆盖：

- 子 placement 没定义 `Component.Button.Container`，从祖先/default 得到 alias 后仍在子边界重查
  `System.Primary`；
- 子 placement 所有 Children miss 时，祖先 Raw override 先于 `DefaultRawTokens`；
- 只在控件与祖先 placement 之间添加 Raw override 时，该 override 按 boundary-relative 契约不可见；
- 给同一 scope 同时添加 override 和 placement 后立即可见；
- nested placement 的修改不影响 sibling。

### 5.2 ThemeVariant 矩阵

每项都要同时验证 Direct value 和多跳 alias/recipe：

| 请求 | 预期 |
|---|---|
| `Light` | Light → Default |
| `Dark` | Dark → Default |
| `ThemeVariant.Default` | Default；不得擅自变为 Light |
| `null` | Avalonia Default 语义；不得用 Owner theme 代替 |
| custom `HighContrastDark → Dark` | 每个 Raw key 都按 custom → Dark → Default 查找 |
| Owner 为 Light、显式请求 Dark | 整条 graph 使用 Dark 请求，不读取单主题 Owner snapshot |

custom variant 的关键回归场景是：Component token 在 Dark 命中 alias，而下一跳 System token 在
HighContrastDark 有 override。最终必须使用 HighContrastDark 的 System 值，证明原始请求 theme 没有
在第一跳被归一化。

### 5.3 生命周期与 GC

Provider 生命周期测试至少覆盖：

- `HasResources` 在 provider 含可识别 Flat/Raw surface 时为正确值；
- attach 调用基类 owner 生命周期并使现有 `DynamicResource` 重新查询；
- detach 清理所有 child `Changed` 订阅，并通知 host；
- detach 后旧 Owner 的资源变化不再触发 placement；
- detach 后允许同一实例重新 attach，且任意时刻只有一个 Owner；
- 同一实例并发挂到两个 host 时明确抛错；
- detached placement、child source 和旧 host 在移除强引用后可被 GC；
- 多次 attach/detach 不累计 handler，不重复广播；
- `TokenOverride` 作为 child source 时以 `Changed` 通知 placement，作为独立 provider 时走 Avalonia
  资源通知；同一实例的双重安装被拒绝；
- 同 host 多 placement 不属于保证行为；若实现开发期诊断，验证其信息与触发条件。

GC 测试应使用 `WeakReference`、隔离创建方法和有界的强制收集循环，避免测试局部变量生存期造成假阴性。

### 5.4 变化通知

由于 v1 不做持久 Flat cache，`MaterialTheme` 不需要订阅 Owner 的 `ResourcesChanged` 或
`ActualThemeVariantChanged` 来重建快照。通知测试应验证：

- child 单次 mutation 导致 placement 发布一次资源变化；
- `BeginUpdate` 中多次 mutation 合并为一次变化；
- Children add/remove/reorder 各发布一次变化；
- placement 自己的广播不会经 Owner 事件回流并造成第二次 rebuild/broadcast；
- Raw override 变化后现有 `DynamicResource` 使用同一 Flat key 重新查询并取得新值；
- `ActualThemeVariantChanged` 由 Avalonia 驱动 DynamicResource 以新 theme 查询，无需 placement
  维护“当前主题”状态；
- sibling scope 不接收不相关 placement 的局部 mutation 结果；
- provider 动态插入和移除时，已有 resource expression 都刷新。

测试既要断言最终值，也要计数 lookup/broadcast，防止“结果正确但同步重入两次”的回归。

### 5.5 Compiled XAML

至少用启用 compiled bindings/XAML compiler 的真实 `.axaml` fixture 验证：

- Application 与子树中的 `MergedDictionaries` placement；
- `MaterialTheme.Children` collection content；
- generated typed facade 的 Color、double、shape、typography 等 literal converter；
- `TokenRef Token={x:Static ...}` 的同类型 alias；
- 参数化 reference/custom token markup extension；
- `DynamicResource {x:Static ...MaterialValueKey}` 作为 Setter 和 Template 值；
- Light/Dark `ThemeDictionaries` 中的 typed provider；
- 以 `x:Static MaterialValueKey` 作为 `x:Key` 的 Flat override；
- 非法 alias 类型与不可转换 literal 的结构化 Material 诊断；对 `null`、可转换和不可转换的 Flat
  override 记录 Avalonia 实际行为，并确认都不会因为 key 契约而回退到祖先 provider。

不要只实例化 markup extension 类做单元测试；那不能证明编译后的 AXAML metadata、resource key object
identity 和实际 `DynamicResource` 订阅链正确。

### 5.6 MaterialColorUtilities 集成

MCU color path 需要独立覆盖以下回归：

- generator alias 与 MCU public catalog 返回同一 descriptor instance，M3E 不创建 compatibility facade；
- composite catalog 只接受一个 canonical owner；缺少/版本不兼容的 MCU contribution 诊断 owner 与版本；
- 空子 placement 继承祖先 Color Context，全链无 scheme 时才使用 terminal baseline；
- 局部 ColorScheme 影响当前 subtree 而不影响 sibling；
- 局部 ColorScheme 不隐式清空祖先显式 System override，未覆盖 roles 才使用局部 context；
- seed、contrast、platform、spec version、custom color mutation 各只触发一次资源广播；
- Light/Dark/Default/custom-inherits-Dark 的 Reference/System 结果与相同 input 的 MCU Core 一致；
- 单个 System override 优先于 MCU recipe，并使 Component alias 重新绑定；
- 单个 Reference tone override 只影响直接依赖它的 graph，不错误重建 `DynamicScheme`；
- named custom palette 存在时正常解析，缺失时新 resolver fail-fast 且不返回 Transparent；
- `ResolvedColorScheme` 的内部 memoization 不缓存 Owner/resource winner，snapshot 替换后不返回旧颜色；
- MCU standalone legacy binding fallback 与 M3E 新 Raw resolver 的失败语义不串线。

这些测试应同时经过 Raw resolver 与至少一个真实 `DynamicResource MaterialValueKey<T>` consumer，不能只
直接调用 `ResolvedColorScheme.GetColor`。

## 6. 性能测试

第一版的性能目标是证明“无持久 Flat cache 的按请求解析足够便宜且分配可控”，而不是预先追求某个
纳秒数字。benchmark 必须在 correctness tests 通过后建立，并记录硬件、runtime、构建配置和
Avalonia 版本。

最低 benchmark 组合：

| 维度 | 建议样本 |
|---|---|
| Raw 深度 | literal、1/3/8/16 跳 alias、cycle 防御上限附近 |
| scope 深度 | Application-only、3/10/30 层 resource host |
| plan | Direct、Color+Opacity、Thickness、4/8 dependency profile |
| precedence | 本地首个 child 命中、最后 child 命中、ancestor 命中、default 命中 |
| theme | Light、Dark、custom inheritance |
| workload | 单次 cold query、同一 control 多 value query、资源变化后的批量重新查询 |

记录：

- 平均值、P95/P99 和吞吐；
- 每次查询 allocation；
- 单请求 memoization 的命中数；
- scope 深度、alias 深度与 child 数量的增长曲线；
- 1/100/1000 个现存 DynamicResource 在一次 theme/override 变化后的更新时间与广播次数。

性能基线应和迁移前实际 control workload 比较，不能只测一个脱离 Avalonia resource host 的字典读取。
只有 benchmark 证明按请求解析成为瓶颈，才考虑 `(MaterialValueKey, ThemeVariant, generation)` 等持久
cache；届时必须新增 notification ordering、cache generation、custom variant 和内存占用测试。

## 7. Generator 验证

生成期应尽早阻止静态错误：

- token path 唯一且稳定；
- literal 类型与 token `T` 一致；
- alias/recipe dependency 存在且类型一致；
- Reference/System/Component 依赖方向合法；
- composed generated/imported default graph 无 cycle，canonical key 没有重复 owner；
- 每个 canonical Material Value dependency 在 defaults catalog 中可达；source-required custom
  dependency 必须显式标记并由相应 fixture/source 覆盖；
- MCU external declaration 的 path/type/layer/version 与实际 canonical symbol 一致；
- public `MaterialValueKey<T>` 的字段名、Path 和 `T` 不发生未经审查的兼容性变化；
- `TokenOverride` 的 literal converter 与 canonical token 类型一致，`TokenRef` alias 保持同类型。

运行时仍保留同类防御，因为用户可以通过动态 provider、C# API 或 XAML override 引入 generator 看不到
的 definition。generator 与 runtime 诊断应共享术语和 failure kind，避免同一错误出现两套含义。

## 8. Baseline 与完成条件

重构开始前，现有相关测试集合曾有 **32 个测试通过**。这只能说明当前过渡实现的既有行为在该次运行
中保持稳定，可用于发现迁移造成的非预期回归；它们没有实例化本文的 boundary-relative Flat
provider、`MaterialValueKey<T>` plan 或无 cache resolver，因此**不能声称验证了目标架构**。

目标架构可进入组件批量迁移的最低门槛是：

1. 上述 failure kinds 都有独立、结构化诊断测试；
2. scope、alias 重绑定和 ThemeVariant 矩阵完整通过；
3. compiled XAML fixture 验证正式 authoring 形状；
4. owner lifecycle、notification 次数和 GC 测试通过；
5. Button vertical slice 同时覆盖 Direct、Color+Opacity、Thickness、shape/typography semantic value；
6. benchmark 建立可复现 baseline，且没有证据要求第一版增加持久 Flat cache；
7. 迁移前 32 个相关测试继续作为 compatibility baseline 运行，但逐项标记其旧路径或新路径归属。
8. MCU Color Context 的 scope、mutation、custom missing、canonical identity 与 core parity 矩阵通过。

具体阶段与退出条件见[迁移计划](./migration-plan.md)，文档入口见[目录](./README.md)。
