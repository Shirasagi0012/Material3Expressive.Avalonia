# Authoring 与 XAML 使用契约

> **重构中的目标架构。** Foundation Raw identity、`MaterialTheme` 与单 token `TokenOverride` 已开始落地；
> 组件消费面仍在迁移，本文不能作为已发布版本的完整 API 文档。
> 总体设计见[架构总览](./architecture-overview.md)，Raw 与 Flat 的职责分别见
> [Raw Token 模型](./raw-token-model.md)和 [Material Value](./material-values.md)，颜色 scheme 的所有权与
> source 形状见 [MaterialColorUtilities 集成](./material-color-utilities.md)。

本架构提供两条刻意分开的使用路径：

- 主题作者通过 canonical Raw identity 与单 token override 改写 Material 语义；
- `ControlTheme`、模板和其他消费端通过 `MaterialValueKey<T>` 取得 consumer-ready 值。

Raw override 是正式、可组合的主题定制 API。直接覆盖 Flat value key 是 Avalonia 原生资源系统自然提供的
展示层逃生口，但不应成为常规主题设计方式。

## 1. `MaterialTheme` 必须是资源 Provider placement

`MaterialTheme` 是 `ResourceProvider`，不是 `Application.Resources` 中的普通值。它必须进入
`ResourceDictionary.MergedDictionaries`，或由一个语义等价的 scope API 安装到该位置。

全局 placement 的目标写法如下：

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <m3:MaterialTheme />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

下面的写法无效，因为它没有把 `MaterialTheme` 作为 provider 加入资源查找链：

```xml
<!-- 无效示例：不要这样写 -->
<Application.Resources>
  <m3:MaterialTheme />
</Application.Resources>
```

一个 placement 建立一个 Flat 解析边界。控件的 `DynamicResource` 先命中最近的 placement；该
placement 随后以自己的 Owner 为固定查找起点解析整条 Raw graph。由此得到的是
**boundary-relative** 语义，而不是从每个控件重新开始的 requester-relative 语义。

这带来一条必须公开的规则：

> 如果要让某个子树中的 Raw override 参与该子树的 Flat 解析，该 override 所在 scope 必须同时建立
> 一个 `MaterialTheme` placement。

只有 override、没有新 placement 时，祖先 placement 已经成为 Flat 请求的解析边界；位于控件和该
边界之间的 Raw resource 不会被它看到。

## 2. `MaterialTheme.Children`

`MaterialTheme.Children` 是当前 placement 的本地、稀疏 Raw source 列表。第一版的通用 authoring
单元是 `TokenOverride`：一个实例只定义一个 canonical Raw key。颜色 scheme 等真正拥有结构化输入的
domain model 也可以直接作为 source。

以下示例中，`mcu:` 表示 MCU Avalonia 提供的 canonical color identity/source API，`m3:` 表示 M3E 的
Foundation、Component 与 Material Value API。M3E 不创建第二份 color key identity。

```xml
<m3:MaterialTheme>
  <m3:MaterialTheme.Children>
    <m3:TokenOverride
        Key="{x:Static mcu:SystemColorTokens.Primary}"
        Value="#FF6750A4" />
    <m3:TokenOverride
        Key="{x:Static m3:FilledButtonTokensKeys.ContainerColor}"
        Value="{m3:TokenRef Token={x:Static mcu:SystemColorTokens.Primary}}" />
  </m3:MaterialTheme.Children>
</m3:MaterialTheme>
```

Children 的目标语义是：

1. 每个 child 都是稀疏 source，未定义某个 Raw key 时必须返回 miss；
2. 同一 placement 内靠后的 child 优先，查询实现应从后向前查找并在首个命中处停止；
3. child 命中 `null`、错误类型或非法 definition 是配置错误，不得当作 miss；
4. 所有 Children 都 miss 时，当前 `MaterialTheme` 的 Raw 分支返回 miss，让 Owner 的剩余资源和祖先
   scope 继续参与查找；
5. 只有完整 Owner 资源链都 miss 后，resolver 才查询 composed `DefaultRawTokens`；默认图不是一个
   需要由作者手工添加的 child；
6. Children 增删、重排或内容变化时，placement 发布一次 `ResourcesChanged`。

因此，局部空白不是“恢复硬编码默认值”，而是继续继承外层定义。`Clear` 也遵循同样语义。

### 2.1 MCU ColorScheme source

目标 MCU integration 允许 `ColorScheme` 本身作为 `IMaterialTokenSource`，或使用一个语义等价的轻量
adapter。推荐 authoring 形状概念上是：

```xml
<m3:MaterialTheme>
  <m3:MaterialTheme.Children>
    <mcu:TonalSpotScheme Color="#FF6750A4" />
  </m3:MaterialTheme.Children>
</m3:MaterialTheme>
```

准确的 XAML 类型名可在共享 contract 落地后冻结。M3E 如果迁移期保留 `MaterialTheme.ColorScheme`
convenience property，它必须把同一个 scheme 适配成上述本地 source 语义，不能同时再加入 Children。

这个 source 只提供内部 Color Context token，不直接发布整套 Reference/System color，因此仍然是 sparse：

- 当前 placement 没有 scheme source 时，Color Context 继续从祖先 placement 查找；
- 整条资源链都没有 scheme 时，MCU catalog contribution 提供 terminal Material baseline；
- 显式 System/Reference override 仍在完整资源链中先于 MCU recipe；
- 每个新 placement 不会因为自动创建默认 Tonal Spot scheme 而遮蔽祖先品牌 scheme。

修改 seed、contrast、platform、spec version 或 custom colors 会替换 MCU immutable snapshot，并通过 child
`Changed` 让 placement 发布一次资源变化。新 M3E 路径不要求作者设置 inherited
`MaterialColor.ResolvedSchemeProperty`。

## 3. 单 token Raw override

第一版不生成每个 token table 的 typed override facade。`TokenOverride.Key` 接受 canonical
`MaterialToken<T>` identity，`Value` 接受与 `T` 相符的对象或常见 XAML literal：

```xml
<m3:TokenOverride
    Key="{x:Static m3:SystemShapeTokens.CornerSmall}"
    Value="10" />
```

同类型 alias 使用 `TokenRef`，token identity 用 `x:Static` 取得：

```xml
<m3:TokenOverride
    Key="{x:Static m3:FilledButtonTokensKeys.ContainerColor}"
    Value="{m3:TokenRef Token={x:Static mcu:SystemColorTokens.Secondary}}" />
```

reference palette 等参数化 catalog 可以通过专门的 markup extension 构造 key：

```xml
<m3:TokenOverride
    Key="{x:Static m3:FilledButtonTokensKeys.ContainerColor}"
    Value="{m3:TokenRef Token={mcu:ReferencePaletteToken Primary, 40}}" />
```

`TokenOverride` 必须维持以下契约：

- literal、alias target 与 token 的 `T` 在装载期校验；
- alias 只能保持同层或指向更基础的层；
- `Key` 或 `Value` 为 `null` 表示当前 source 没有 entry，而不是产生一个值为 `null` 的 token；
- alias definition 不可变；
- 一个实例只命中自己的 `Key`，其他 key 全部返回 miss。

`TokenOverride` 既可作为 `MaterialTheme.Children` 的 source，也可作为后文
`ThemeDictionaries.MergedDictionaries` 中的独立 Raw provider。同一实例只能选择一个 authoring
placement；不要同时把它加入 Children 和资源字典，也不要在多个 placement 间共享实例。

面向某个 domain 的 typed convenience facade 可以以后按实际 authoring 需求增加，但它只能包装相同的
canonical identity 和 source 契约，不是第一版 runtime 或 generator 的组成部分。

Raw token 仍是语义定制面。例如覆盖 system role 会让所有引用该 role 的 component token 重新绑定；
覆盖一个 component token 则只改变对应组件语义槽。

Raw key 的 public identity 用于 override 与 `TokenRef`，不表示它可以直接作为 ControlTheme 的
`DynamicResource` key。直接 Raw lookup 无法负责终端 default fallback；若属性需要原样的 Raw `T`，
应消费一个 `DirectRaw<T>` 的 `MaterialValueKey<T>`。

`MaterialToken<T>` 本身只描述 identity、Path、layer 和 value type，不携带默认 definition。canonical
token 的 baseline definition 位于独立的 composed `DefaultRawTokens` catalog。该 catalog 允许
variant-specific definition；`TryGetDefinition(token, queryTheme)` 对每个 Raw key 按
exact → `InheritVariant` → Default 查询，同时 alias/recipe 的每一跳仍使用原始 `queryTheme`。

参数化 custom token 可以没有 catalog entry；这种 token 必须由某个显式 source 提供，否则解析以
source-required missing 失败，而不是臆造一个默认值。MCU custom color family 是已知例外：catalog 可以
提供通用算法 recipe，但当前 Color Context 仍必须声明对应 named palette；缺失时 fail-fast，不返回透明色。

## 4. `MaterialValueKey<T>` 的 XAML 消费

`MaterialValueKey<T>` 是稳定、强类型的 Flat resource identity。内建 `ControlTheme` 和用户自定义
`ControlTheme` 应通过普通 `DynamicResource` 使用它：

```xml
<Setter Property="Background"
        Value="{DynamicResource {x:Static m3:ButtonValueKeys.FilledContainerBrush}}" />

<Setter Property="Padding"
        Value="{DynamicResource {x:Static m3:ButtonValueKeys.SmallContentPadding}}" />
```

必须使用 `x:Static` 传入 key 对象；不要把 `Path` 文本当作 resource key：

```xml
<!-- 错误：字符串不是 MaterialValueKey singleton -->
<Setter Property="Background"
        Value="{DynamicResource Value.Button.Filled.ContainerBrush}" />
```

value key 的输出已经是 consumer-ready 类型，例如 `IBrush`、`Thickness`、`double`、`ShapeRole` 或
一个确实由 presenter 原子消费的不可变 profile。XAML 消费端不负责遍历 alias、组合 opacity，也不应
假定 `double` 会自动转换为 `Thickness`。

variant、size 和 pseudo-class 由 `ControlTheme` 选择不同 value key，而不是由 provider 读取请求控件：

```xml
<Style Selector="^[m3|Variant=Filled]">
  <Setter Property="Background"
          Value="{DynamicResource {x:Static m3:ButtonValueKeys.FilledContainerBrush}}" />
</Style>

<Style Selector="^[m3|Variant=Filled]:disabled">
  <Setter Property="Background"
          Value="{DynamicResource {x:Static m3:ButtonValueKeys.FilledDisabledContainerBrush}}" />
</Style>
```

上例的 selector 仅表达职责边界，最终 selector 语法由实际 control API 决定。Bounds、RTL、pointer
interaction 和动画进度仍由 control/presenter 处理，不能作为 Material Value projector 的隐式输入。

## 5. 全局稀疏定制

应用级定制把单 token source 放在全局 `MaterialTheme.Children` 中：

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <m3:MaterialTheme>
        <m3:MaterialTheme.Children>
          <m3:TokenOverride
              Key="{x:Static mcu:SystemColorTokens.Secondary}"
              Value="#FF006A6A" />
          <m3:TokenOverride
              Key="{x:Static m3:FilledButtonTokensKeys.ContainerColor}"
              Value="{m3:TokenRef Token={x:Static mcu:SystemColorTokens.Secondary}}" />
        </m3:MaterialTheme.Children>
      </m3:MaterialTheme>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

这里只定义两个 Raw token。其余 Raw token 继续沿资源链查找，最终才落到 `DefaultRawTokens`。因此
一个局部品牌色不会意外替换 typography、shape 或其他 component token。

如果多个 child 定义相同 token，靠后的 child 获胜：

```xml
<m3:MaterialTheme>
  <m3:MaterialTheme.Children>
    <m3:TokenOverride Key="{x:Static mcu:SystemColorTokens.Primary}" Value="#FF6750A4" />
    <m3:TokenOverride Key="{x:Static mcu:SystemColorTokens.Primary}" Value="#FF005AC1" />
  </m3:MaterialTheme.Children>
</m3:MaterialTheme>
```

上例的有效 `Primary` 是 `#FF005AC1`。

## 6. 子树稀疏定制

子树 override 必须和新的 placement 一起安装：

```xml
<Border>
  <Border.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <m3:MaterialTheme>
          <m3:MaterialTheme.Children>
            <m3:TokenOverride
                Key="{x:Static m3:FilledButtonTokensKeys.ContainerColor}"
                Value="#FF7D5260" />
          </m3:MaterialTheme.Children>
        </m3:MaterialTheme>
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Border.Resources>

  <!-- 此处 descendants 使用局部 ContainerColor；其他 token 仍继承外层定义。 -->
</Border>
```

该 placement 查找 `ContainerColor` 时命中本地 child；查找其他 Raw key 时返回 miss，随后可以命中
祖先主题中的 override。继承来的 component alias 在每一跳仍从这个子树 placement 的 Owner 重新查找，
所以本地 system/reference override 能重绑定祖先或默认图中的 alias。

下面的写法不满足目标语义：

```xml
<!-- 错误：缺少同 scope 的 MaterialTheme placement -->
<Border.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <m3:TokenOverride
          Key="{x:Static m3:FilledButtonTokensKeys.ContainerColor}"
          Value="#FF7D5260" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Border.Resources>
```

此 override 可能位于最近 Flat provider 与 requester 之间，因此不会参与该 provider 的 Raw 解析。

## 7. ThemeVariant 与自定义 variant

theme 是资源请求的一部分。`DynamicResource` 从实际 requester 取得 `ActualThemeVariant`，并把同一个
请求 variant 传给 Material Value plan 的每一个 Raw dependency。`MaterialTheme` 不得用 Owner 的
`ActualThemeVariant` 覆盖它，也不得把 `ThemeVariant.Default` 简化成 Light。

theme-specific Raw override 使用 Avalonia `ThemeDictionaries`。每个 theme dictionary 仍可以通过
`MergedDictionaries` 放入 `TokenOverride`：

```xml
<ResourceDictionary>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
      <ResourceDictionary.MergedDictionaries>
        <m3:TokenOverride Key="{x:Static mcu:SystemColorTokens.Primary}" Value="#FF6750A4" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
      <ResourceDictionary.MergedDictionaries>
        <m3:TokenOverride Key="{x:Static mcu:SystemColorTokens.Primary}" Value="#FFD0BCFF" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>

  <ResourceDictionary.MergedDictionaries>
    <m3:MaterialTheme />
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

custom variant 遵循 Avalonia 的继承链。例如 `HighContrastDark → Dark → Default`。回退必须针对
**每一个 Raw key** 独立执行，同时在整条 alias/recipe 解析过程中保留最初请求的
`HighContrastDark`：

```text
请求 HighContrastDark
  Component token 可从 Dark 命中
  其 alias 指向的 System token 仍先查 HighContrastDark
  然后才回退 Dark、Default
```

如果第一跳命中 Dark 后就把整个 session 归一化为 Dark，后续依赖会错过 custom variant override。

`null` 与 `ThemeVariant.Default` 都不表示 Light。它们按 Avalonia 的 Default 资源语义处理；需要 Light
值时必须由实际 requester 或显式查询提供 Light variant。

## 8. Raw 与 Flat 的定制边界

| 需求 | 推荐机制 | 影响 |
|---|---|---|
| 修改品牌或 Material 语义 | System/Reference Raw override | 所有依赖该语义的 value 重新解析 |
| 修改某个组件语义槽 | Component Raw override | 只影响相应组件 token graph |
| 修改普通 Avalonia 属性 | `Style`、`ControlTheme` 或 local value | 遵循原生属性优先级 |
| 强制替换一个 public value endpoint | 直接 Flat resource override | 完全绕过该 endpoint 的 Raw plan |

对 public `MaterialValueKey<T>`，作者可以在更近的普通 `ResourceDictionary` 中定义同一个 key：

```xml
<Border.Resources>
  <ResourceDictionary>
    <SolidColorBrush
        x:Key="{x:Static m3:ButtonValueKeys.FilledContainerBrush}"
        Color="#FFFF00FF" />
  </ResourceDictionary>
</Border.Resources>
```

这是有意保留的 presentation-level escape hatch。因为普通资源先于 merged provider 命中，它不会执行
`MaterialTheme` 的 Raw resolution plan，也不会跟随 system/component alias 重绑定。库应记录它、测试
它，但不应把它包装成另一套主要主题 API。

作者应让 Flat override 的值符合 key 的 `T` 且非 `null`。普通字典会在 `MaterialTheme` 之前命中，
因此 Provider 无法验证这种值或补充 Raw graph path；`null` 可能成为 unset，不同类型也可能被 Avalonia
目标属性转换接受，v1 不承诺一定产生错误诊断。这次命中不会因违反 key 契约而重新查询祖先 Flat
provider。需要 Material resolver 的非空/类型检查与结构化诊断时应使用 Raw override；强制检查 Flat
escape hatch 需要未来的 analyzer 或 typed authoring helper。详细边界见
[诊断与测试](./diagnostics-and-testing.md)。

## 9. Owner 与 placement 约束

一个 `MaterialTheme` 实例同时只能有一个 Owner。不得把同一实例同时加入 Application 和多个子树：

```csharp
// 错误：同一个 provider instance 不能被两个 host 共享。
application.Resources.MergedDictionaries.Add(theme);
panel.Resources.MergedDictionaries.Add(theme);
```

需要相同配置时，应创建不同的 placement 实例，并共享不可变的配置数据或构造输入。detach 后重新
attach 同一个实例可以支持热重载和字典替换，但任意时刻仍只能有一个 Owner。

同一个 resource host 建议只安装一个 `MaterialTheme` placement。一个 placement 的多个 source 用
`Children` 组合；不要依赖多个 `MaterialTheme` 在 `MergedDictionaries` 中的相对顺序表达主题合成。
实现可以在开发模式下对同 host 多 placement 发出诊断，公共契约则只保证单 placement 行为。

## 10. Authoring 检查清单

- `MaterialTheme` 位于 `MergedDictionaries`，而不是普通资源值中；
- 每个子树 Raw override 与同 scope 的 placement 一起安装；
- 通常一个 host 只有一个 placement，一个实例只有一个 Owner；
- 每个 `TokenOverride` 只定义一个 key，`Key`/`Value` 为 `null` 表示无 entry；
- alias 用 typed `TokenRef`，value 消费用 `DynamicResource + x:Static MaterialValueKey`；
- 不用 `DynamicResource` 直接请求 Raw key；原样值也通过 Direct value key；
- MCU `ColorScheme` 作为 sparse context source；空 placement 继承祖先 scheme，不手工安装 dense defaults；
- theme-specific override 使用 `ThemeDictionaries`，不自行把 Default 当作 Light；
- 日常主题定制使用 Raw，直接 Flat override 仅用于明确的展示层替换；
- 不把 requester state、Bounds、RTL 或动画逻辑塞入 token/value projector。

剩余组件迁移顺序见[迁移计划](./migration-plan.md)，Provider 生命周期细节见
[MaterialTheme](./material-theme.md)。
