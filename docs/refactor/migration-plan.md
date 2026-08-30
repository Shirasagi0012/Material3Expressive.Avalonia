# Token 架构迁移计划

> **状态：实施中。** Foundation runtime 已完成破坏性切换。尚未迁移的组件通过编译排除隔离；本计划
> 不提供旧 snapshot、scheme authoring 或 requester-relative resolver 的兼容回退。

相关设计：

- [架构总览](architecture-overview.md)
- [Raw Token 模型](raw-token-model.md)
- [Material value](material-values.md)
- [MaterialTheme](material-theme.md)
- [MaterialColorUtilities 集成](material-color-utilities.md)
- [Generator](generator.md)
- [XAML authoring](authoring-and-xaml.md)
- [诊断与测试](diagnostics-and-testing.md)

## 1. 当前状态

活动生产切片只有一套 Foundation 路径：

```text
canonical MaterialToken<T> identities
    → MaterialTheme.Children / Avalonia Owner resource chain
    → composed DefaultRawTokens
    → MaterialResolutionSession
    → MaterialValueKey<T>
    → DynamicResource / ControlTheme
```

已经完成：

- shared `MaterialToken<T>`、literal/alias/recipe 与 catalog contribution contracts；
- M3E generated descriptor/default contribution 与 MCU color contribution 的组合；
- Foundation Shape、Motion、Typography、TypeScale canonical Raw identities；
- boundary-relative `MaterialTheme` placement 与 per-request resolution session；
- 通用单 entry `TokenOverride` 与 `TokenRef` alias authoring；
- 删除 `ResolvedTheme`、`MaterialThemeScope`、`ThemeBinding`、旧 scheme authoring；
- 删除 requester-relative token runtime、generated typed override facade 与根级
  `MaterialElementContext` theme transport；
- token-dependent 组件与其 AXAML 从当前生产编译切片排除。

当前只保留 token-independent Scaffold 组件。排除区中的控件源码仍可能出现已删除类型名；这些不是活动
runtime，必须在对应组件恢复时消除。

## 2. 不可变迁移约束

### 2.1 排除优于兼容桥

未迁移组件留在编译排除区。不得为了让它暂时编译而重新建立：

- whole-theme 或 component snapshot；
- `ShapeScheme`/`MotionScheme`/`TypographyScheme` authoring；
- requester-relative Raw resolver；
- Defaults adapter 双写；
- path 相同但 identity 不同的 MCU color wrapper；
- generated `XxxTokensOverrides` facade。

恢复失败的组件应重新进入排除区，而不是恢复旧 runtime。

### 2.2 一个消费槽只有一个权威来源

Material 默认值由 `DynamicResource MaterialValueKey<T>` 提供。普通 Avalonia local value、Style 和
ControlTheme 仍按原生优先级覆盖它，但生产组件不得同时由 adapter 或 property callback 写入同一 Material
默认槽。

### 2.3 Raw facts 与控件行为分离

`.ref/.sys/.comp.jsonc` 只保存设计事实与默认 Raw graph。Variant、Size、state、Bounds、RTL、pointer 与
animation 属于 ControlTheme、组件 policy 或 presenter，不能进入 Raw resolver 或 Material Value 的隐藏
输入。

### 2.4 暂不增加持久 cache

第一版仅在单次 `MaterialResolutionSession` 中 memoize Raw dependency。只有实测数据证明需要时，才单独
设计跨请求 cache；组件迁移不能依赖 cache 才正确。

## 3. 剩余阶段

### M4：验证 Foundation 最小切片

在用户允许执行验证后：

1. 构建 shared contracts、generator、M3E Foundation 与 Scaffold；
2. 运行 focused Raw/MaterialValue/MaterialTheme tests；
3. 验证 global/local placement、alias 重绑定、variant 透传、Children 变化和 detach/reattach；
4. 验证 MCU scheme source、terminal baseline 与 explicit System override 的优先级。

构建或测试失败时修复新 Foundation 本身，不恢复已删除 API。

### M5：恢复第一个简单组件族

优先建议 `Divider + Surface`，因为它们状态轴少，可以验证完整链路而不先引入复杂 motion/interaction。

每个组件必须：

- 确认 canonical Component/System/Reference Raw roots；
- 声明最小 consumer-ready `MaterialValueKey<T>` 集合；
- 由 ControlTheme 使用 `DynamicResource`；
- 删除旧 Defaults/snapshot/controller 引用；
- 将 C#、AutomationPeer 与 AXAML 作为一个完整族恢复编译；
- 做 focused functional 和最小视觉检查。

### M6：按组件族恢复

建议按依赖复杂度推进：

1. leaf color/metric：Divider、Surface、Badge；
2. 简单 selection：Checkbox、RadioButton、Switch；
3. button/card/chip/FAB 共享 shape 与 interaction 模型；
4. Slider、Progress、Tabs、Navigation 等复合状态组件；
5. TextField、Search、List、Typography-heavy 组件；
6. Overlay、Picker、adaptive layout 与复杂 motion 组件。

不同组件族可以在代码独立时并行，但每个族必须独立恢复、独立验证，不能用一次大范围解除排除掩盖错误
归属。

### M7：删除组件遗留

随组件恢复同步删除：

- obsolete `*Defaults` 与 adapter；
- 只为旧 runtime 存在的 semantic projection；
- excluded AXAML 中的旧 Typography attached-property authoring；
- 测试与 visual harness 中的旧 theme placement helper；
- csproj 中该组件族的排除项。

Foundation 已删除的类型不得重新出现。

### M8：全库审计与验证

所有组件恢复后执行：

- `rg` 审计旧 snapshot、scheme、resolver、adapter 与 wrapper 名称；
- build、unit/integration、compiled-XAML tests；
- theme mutation、scope、variant 与 GC/lifetime stress tests；
- 全量视觉 parity；
- 性能测量，决定是否另开持久 cache 设计。

## 4. 每个组件的恢复清单

每个组件切片至少回答：

- Raw facts 来自哪些 `.comp.jsonc`、System 和 Reference identities？
- 哪些输出必须是 public `MaterialValueKey<T>`，哪些仅为 library AXAML glue？
- 是否需要 leaf key，还是 immutable profile 更合适？
- Variant/Size/state 由哪个 selector 或组件 policy 选择？
- Bounds、RTL、pointer 和 animation 是否仍留在 presenter/control？
- 所有 Material 默认槽是否只由 DynamicResource 路径写入？
- ordinary Avalonia Style/local value 的优先级是否保持？
- C#、AutomationPeer、AXAML、tests 是否作为完整族恢复？
- 是否删除了该族所有 obsolete Defaults 与旧 transport 引用？

## 5. 验证顺序

用户允许后，每个切片按以下顺序验证：

1. 静态旧符号审计与 XML/Markdown 检查；
2. production build；
3. focused runtime/component tests；
4. 最小 visual parity family；
5. 必要时扩大到相邻组件和全量视觉测试。

视觉测试失败需区分明显功能/可用性错误与轻微 parity 瑕疵。前者在交付检查前修复，后者可以记录后交由
验收决定处理优先级。
