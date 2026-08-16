# Raw Token 模型

> **状态：目标架构，尚未实现。** 本文定义重构完成后 Raw token 的语义契约；代码片段是 API 草图，
> 不表示当前仓库已经提供这些类型或成员。总体结构见[架构总览](architecture-overview.md)，
> `MaterialTheme` 如何接入 Avalonia 资源树见 [MaterialTheme 与资源边界](material-theme.md)。

## 1. 职责

Raw token graph 保存 Material Design token 的**设计语义**，而不是某个 Avalonia 属性可以直接消费的值。
它回答的是“这个 token 的定义是什么，以及它依赖哪个更基础的 token”，例如：

```text
Component.FilledButton.ContainerColor
    → System.Color.Primary
    → Reference.Palette.Primary.40
    → #FF6750A4
```

Raw 层负责：

- Reference、System、Component 三层 token 的稳定身份；
- 字面量、alias 和纯 recipe 定义；
- 类型、依赖方向、循环和缺失定义诊断；
- 在 Avalonia 资源树中进行稀疏、可继承的局部覆盖；
- 为每次解析保留同一个资源边界与原始 `ThemeVariant`。

Raw 层不负责：

- 把 `Color + Opacity` 组合成 `IBrush`；
- 把若干尺寸组合成 `Thickness`；
- 根据控件的 Variant、Size、交互状态、Bounds 或 FlowDirection 选择值；
- 为控件生成完整 defaults snapshot；
- 作为 ControlTheme 的直接 `DynamicResource` 消费协议。

颜色/尺寸投影属于 [Material Value](material-values.md)，控件轴与 snapshot 边界属于 ControlTheme、组件
policy 或 Presenter；直接资源消费同样通过 Material Value 建立契约。

Raw descriptor 仍需公开给 typed override 与 alias authoring，但消费端不能直接请求 Raw key。Raw branch
在本地 miss 后必须把查找交还 Avalonia，只有一个完整的 Material Value resolution session 才知道整条
Owner 链何时结束并安全查询 `DefaultRawTokens`。需要原样 `T` 的消费槽使用 `DirectRaw<T>` value plan。

## 2. 强类型身份

每个 Raw key 是一个强类型 descriptor：

```csharp
public abstract class MaterialToken
{
    public string Path { get; }
    public TokenLayer Layer { get; }
    public Type ValueType { get; }
}

public sealed class MaterialToken<T> : MaterialToken
{
    // 构造入口由 generated catalog 或受控的动态 catalog 持有。
}
```

`T` 是 Raw token **解析完成后的类型**。例如，颜色 token 是 `MaterialToken<Color>`，opacity 是
`MaterialToken<double>`，shape role 是 `MaterialToken<ShapeRole>`。资源中保存的则是与 `T` 对应的
`TokenDefinition<T>`。

这是目标架构相对当前 runtime 的一项有意变化：当前 `MaterialToken<T>` 自身携带
`DefaultDefinition`；目标模型把 identity descriptor 与 baseline definition 分离，generated built-in
definition 统一进入 `DefaultRawTokens`。因此 token descriptor 可以只表达身份、层级和类型，不同时
承担 fallback storage，也不会让每个 placement 复制默认图。

descriptor 必须满足以下不变量：

1. descriptor 是不可变对象；
2. 正常资源查找使用对象身份，不在热路径比较 `Path` 字符串；
3. `Path` 稳定、全局可诊断，并用于错误消息和工具输出；
4. `Layer` 和 `ValueType` 是身份 schema 的一部分，发布后不能静默改变；
5. canonical key 必须由静态字段或 canonical factory 返回稳定的同一实例。

主 API 不使用一个有限 `enum` 表示所有 token。有限 enum 无法表达带参数的身份，例如自定义 palette
名称与 tone：

```csharp
var brand40 = ReferencePaletteTokens.Get(
    RefPaletteToken.Custom,
    tone: 40,
    customName: "Brand");
```

这类 factory 必须 canonicalize 输入，并对同一逻辑身份返回同一 descriptor。内部可以为生成 token
分配紧凑数值 ID，但该优化不能限制扩展身份，也不能成为公开 key。

## 3. 三层依赖

```text
Reference  → palette、primitive、基础 metric
System     → 应用级语义角色
Component  → 组件语义槽
```

依赖可以保持在同层，或指向更基础的层：

| 来源 | 可以依赖 | 禁止依赖 |
|---|---|---|
| Reference | Reference | System、Component |
| System | System、Reference | Component |
| Component | Component、System、Reference | — |

generator 应尽可能在生成期拒绝非法依赖；runtime 仍须检查动态 definition 与手写 recipe，不能假定所有
资源都来自 generator。层级合法也不代表允许循环，循环在任何层都必须失败。

## 4. Definition 模型

Raw definition 是不可变、可检查的封闭模型：

```csharp
public abstract record TokenDefinition<T>;

public sealed record LiteralTokenDefinition<T>(T Value)
    : TokenDefinition<T>;

public sealed record AliasTokenDefinition<T>(MaterialToken<T> Target)
    : TokenDefinition<T>;

public sealed record RecipeTokenDefinition<T>(ITokenRecipe<T> Recipe)
    : TokenDefinition<T>;
```

### 4.1 Literal

Literal 直接提供 `T`：

```csharp
Token.Literal(Colors.HotPink)
Token.Literal(0.38d)
Token.Literal(ShapeRole.CornerFull)
```

命中的 definition 如果值为 `null` 或类型错误，是配置错误；它不是资源 miss，也不能通过向外层回退来
掩盖。所有 built-in final token 必须解析为非 `null` 值。

### 4.2 Alias

Alias 只引用相同 `T` 的 Raw token：

```csharp
Token.Alias(SystemColorTokens.Primary)
```

alias 的目标不是在 definition 所在位置提前求值。每次跳转都要从当前 `MaterialTheme` placement 的
同一个 Owner 重新发起 Raw 查找，并原样传递本次请求的 theme。这样，祖先提供的 component alias
仍能绑定到当前边界可见的 system 或 reference override。

### 4.3 Raw recipe

Raw recipe 用于 graph 内确实需要计算的 token：

```csharp
public interface ITokenRecipe<out T>
{
    IReadOnlyList<MaterialToken> Dependencies { get; }
    T Evaluate(TokenRecipeContext context);
}
```

recipe 必须：

- 预先声明全部依赖，并且只能通过 `context.Get(token)` 读取它们；
- 只依赖 Raw token；
- 对同一组输入产生同一结果；
- 不自行访问资源树、requester、Owner、服务、时钟或其他环境状态；
- 遵守层级方向与非 `null` 输出约束。

Raw recipe 表达的是 Material token graph 中的派生关系。仅为适配 Avalonia 属性而进行的组合，应放到
Material Value plan 中，不能污染 Raw graph。

## 5. 本地 source 与资源优先级

`MaterialTheme.Children` 中的每个 source 都是稀疏 Raw definition provider。目标接口按 key 查询，
而不是先枚举并物化整个主题：

```csharp
internal interface IMaterialTokenSource
{
    bool TryGetRawToken(
        MaterialToken token,
        ThemeVariant? theme,
        out object? definition);

    event EventHandler? Changed;
}
```

同一 placement 内，后加入的 child 优先。查询实现从后向前寻找，第一个命中即返回：

```csharp
for (var i = Children.Count - 1; i >= 0; --i)
{
    if (Children[i].TryGetRawToken(token, theme, out definition))
        return true;
}
```

Child miss 必须返回 `false`，让 Avalonia 继续查询同一资源节点的其他 provider 与祖先节点。Child
不得自行使用 generated default，否则较近 placement 的默认值会错误遮蔽祖先显式 override。

查询 theme 属于原始资源请求，而不属于 source 或 Owner。一次 alias/recipe 解析中的每一步都透传同一
theme；具体 variant fallback 规则见 [MaterialTheme 与资源边界](material-theme.md)。

## 6. DefaultRawTokens

内置默认图由 generator 生成到一个只读、进程级共享的 `DefaultRawTokens` catalog。它保存每个
built-in descriptor 的 `TokenDefinition<T>`，包括所有默认 literal、alias 与 Raw recipe：

```csharp
internal static class DefaultRawTokens
{
    public static bool TryGetDefinition(
        MaterialToken token,
        ThemeVariant? queryTheme,
        out ITokenDefinition definition);

    public static bool RequiresGeneratedDefinition(MaterialToken token);
}
```

它是默认 graph 的唯一事实来源，不应复制到每个 `MaterialTheme`，也不应在生成时提前拍扁。保留
definition graph 才能让局部 override 重新绑定默认 alias。

catalog 可以为同一个 Raw key 保存 variant-specific baseline definition。对该 key 的一次查询按
`exact queryTheme → InheritVariant chain → Default` 查找；`null` 或 `ThemeVariant.Default` 直接查询
Default。这里的 fallback 只选择**当前 Raw key** 的 definition，不会归一化或修改 resolution session
保存的原始 `queryTheme`。

默认查找必须严格发生在**完整 Avalonia Raw 资源链 miss 之后**：

```text
placement.Children
    → 同一节点其他资源
    → 祖先 placement / 其他祖先资源
    → Application resources
    → 全部 miss
    → DefaultRawTokens.TryGetDefinition(token, originalQueryTheme)
```

例如，子 placement 没有定义 `System.Color.Primary`，祖先把它覆盖为红色；子 placement 必须使用祖先
红色，而不能先被内置默认紫色截断。

DefaultRawTokens 对所有 generated built-in token 必须完备。若完整资源链与默认 catalog 都缺少某个
built-in token，这是 generator 或架构错误，应抛出可诊断异常。动态/custom descriptor 不受有限枚举
约束，也不因此自动获得 default：没有 catalog entry 时，必须由当前 scope 可见的 token source 提供
definition；否则解析为 missing configuration error，系统不能凭空构造一个值。

resolver 因而还需要 generated membership metadata 来区分“本应有 catalog definition 的 built-in”与
“按设计必须由 source 提供的 key”。该 metadata 可以由 `DefaultRawTokens.RequiresGeneratedDefinition`
或独立的 generated identity catalog 提供；它只表达 missing policy，不保存 definition，也不把
`DefaultDefinition` 放回 descriptor。未知的应用扩展 key 默认按 source-required 处理，不能根据 `Path`
命名约定猜测类别。

如果 catalog 在 inherited variant 中命中一个 alias 或 recipe，其每个后续 Raw dependency 仍使用同一个
原始 `queryTheme`：先重新查询完整 Owner 资源链，整链 miss 后再以该原始 theme 查询 catalog。不得把
本 hop 命中的 inherited variant 当成下一 hop 的 query theme。

默认 catalog 的生成、校验与版本策略见 [Generator](generator.md)。

## 7. 单次解析 session

第一版不保留跨资源请求的 Raw 或 Flat cache。每次 Material Value 查询创建一个短生命周期 session：

```text
Resolve MaterialValueKey
    → resolve declared Raw roots
    → follow literal / alias / recipe
    → memoize this session only
    → discard session
```

session memoization 以 Raw descriptor 为 key，用于合并同一次请求中的公共依赖。它不能跨
`ResourcesChanged` 或 theme 变化存活，因此不需要维护 generation，也不会返回旧主题的结果。

session 还维护当前解析栈。发现循环时应报告完整路径，例如：

```text
Component.Button.ContainerColor
→ System.Color.Primary
→ System.Color.Brand
→ System.Color.Primary
```

深度上限只能作为防御措施，不能替代实际环路检测。

## 8. 覆盖与 Clear

override provider 只保存显式设置的 definition：

```csharp
overrides.Set(FilledButtonTokensKeys.ContainerColor, Colors.Red);
overrides.Alias(
    FilledButtonTokensKeys.LabelTextColor,
    SystemColorTokens.OnPrimary);
overrides.Clear(FilledButtonTokensKeys.ContainerColor);
```

`Clear` 的唯一含义是删除当前 provider 的 entry，使该 key 重新成为 miss 并继续外层查找。`null` 可以
作为 generated facade 中表示 `Clear` 的 authoring 语法，但绝不是一个可解析的 token value 或
definition。有关 C#/XAML authoring 见 [Authoring 与 XAML](authoring-and-xaml.md)。

## 9. 解析失败

以下错误必须 fail fast，不能返回 miss 后静默寻找更外层 definition：

| 错误 | 最低诊断内容 |
|---|---|
| 命中的对象不是 `TokenDefinition<T>` 或允许的 `T` literal | token path、期望类型、实际类型、可得的来源 |
| Alias 类型或层级非法 | 来源与目标 path、两侧类型与层级 |
| Recipe 读取未声明依赖 | recipe、调用 token、已声明依赖 |
| 循环 | 完整环路 |
| Recipe/final value 为 `null` | token path、可得的 definition 来源 |
| built-in default 缺失 | token path、catalog/version |

只有“当前 provider 确实没有此 key”是 miss。诊断与测试基线详见
[诊断与测试](diagnostics-and-testing.md)。

## 10. 规范性不变量

实现与评审必须保持以下性质：

1. Raw key 是强类型 descriptor，不是字符串或全局有限 enum；
2. definition 只允许 Literal、Alias、显式依赖的纯 Recipe；
3. 每次依赖跳转使用同一个 placement Owner 和同一个原始 theme；
4. source 保持稀疏，miss 不截断 Avalonia 资源级联；
5. DefaultRawTokens 只在完整 Raw 资源链 miss 后使用；
6. generated default graph 保持为 graph，不预先拍扁；
7. final value 非 `null`，`Clear` 表示删除 entry；
8. 第一版只做单次 session memoization，不做持久 Raw/Flat cache；
9. 非法类型、依赖方向、未声明依赖和循环都 fail fast；
10. Variant、Size、state、Bounds 与平台投影不进入 Raw resolver；
11. Raw key 不作为 ControlTheme 的直接 `DynamicResource` key；原样消费仍通过 Direct Material Value。
