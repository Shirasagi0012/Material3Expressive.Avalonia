# 编写视觉一致性用例

本文说明用例选择、测量和采样规范，以及当前框架的接入方式与执行边界。
作者负责场景和采样时机的正确性；框架负责忠实执行、记录事实、比较和展示结果。
下文规范约束场景构造、覆盖选择和采样；示例不代表组件覆盖完整，也不是必须照搬的模板。
运行命令见 [README](README.md)，事实定义与输出格式见 [MEASUREMENTS](MEASUREMENTS.md)。
涉及组件时，先按[移植指南](../../docs/PORTING.md)读取固定上游；测试场景不能替代上游依据。

宿主固定使用 4× 渲染，用例的尺寸和输入坐标保持 DIP。Compose 的 `Probe` 自动使用
`ComposeHost.RenderScale`；组件专用读数器同样将像素位置、半径和 elevation 除以
该倍率后报告 DIP，保留原生像素时标为 px。字号、字距和 UTF-16 索引不随倍率相乘。

ComposeHost 同时向默认平台 WindowInfo 提供实际窗口像素尺寸；仅设置 scene.size 不会更新
WindowInfo，Core 的桌面阴影光照会单独读取后者。显式非零窗口环境由用例负责，宿主保留它。

## 用例分类与稳定输入

用例分为静态和动态两类，组件用例 ID 分别采用 `<component>.static.*` 和
`<component>.dynamic.*`。分类依据是是否验证随时间发生的变化，不是是否存在用户交互。

- 静态用例：给定配置下的稳定外观和布局，例如正常/禁用外观、尺寸、RTL、受限空间。
- 动态用例：输入、属性变化或时钟推进引起的行为和视觉过程，例如按压、取消、主题切换、
  进度变化和循环动画。静态禁用外观与交互过程中禁用分别建用例。
- 确定进度条的固定进度可做静态用例；不确定进度条和环形进度条的持续动画做动态用例，
  不要求进入稳态，也不为归类而人为添加交互。

当前组件视觉一致性用例不包含文字，也不保留必然产生文字差异的辅助用例。
文字排版、字体配置和栅格化属于当前未覆盖范围，不视为已验证一致或组件已接受的差异。
必须依赖文字才能成立的场景暂不纳入；已有文字读数能力及其框架自测可以保留。
日期与时间选择器例外：数字、月份名和时间读数是组件内容。维护者于 2026-10-11 允许
`date-picker` 与 `time-picker` 套件用真实文字做双端捕获，不隐藏文字、不放宽容差。
文字差异与几何、颜色和状态分开阅读；匹配失败不记为已接受的视觉差异。其他组件仍遵守本段。

场景使用确定尺寸的非文字内容，如矩形、简单矢量图形；不使用字体图标、空字符串、
透明或隐藏文字作为替代。内容通过组件正常入口参与布局；验证前景色时继承组件实际提供的颜色，
不得在子元素上写死预期颜色。双端使用相同的逻辑尺寸和几何定义。
空内容仅用于容器、最小尺寸等适合的场景，不作为全部用例的统一夹具。

固定尺寸用于排除与目标无关的测量依赖，不得固定正在验证的结果：

| 验证目标 | 构造要求 |
| --- | --- |
| 容器绘制、状态变化、按压动画 | 固定必要的外部尺寸，内容尺寸确定 |
| 默认高度、最小尺寸 | 保留待验证方向的自动尺寸 |
| Padding、固有尺寸、内容对齐 | 使用确定尺寸内容，通过组件正常布局获得结果 |
| 受限空间、裁剪 | 显式设置待验证的外部约束，观察实际测量、排列与绘制 |

不得在夹具中补偿组件应完成的 Padding、对齐或状态转换，不得通过单端偏移、事后图像对齐、
任意取整或修改预期尺寸消除差异。真实交互使用输入事件，不通过直接设置内部 pressed 等状态代替。
稳定输入不保证双端相等；圆角抗锯齿等原始差异仍如实呈现，Raster 使用统一的 1× 降采样容差判定，
不按用例单独放宽阈值。具体算法与区域约定见 [MEASUREMENTS](MEASUREMENTS.md#raster-容差判定)。

## 用例粒度与覆盖选择

独立验证的配置和行为组织为用例，同一次执行中的观察时刻组织为采样点。
不同静态配置通过数据源展开为独立用例，共用场景方法；一次动态过程包含多个采样点。
需要验证控件之间的布局或行为关系时，可以在一个场景中构造多个控件。

```text
button.static.appearance.filled.small.round   → initial
button.static.appearance.filled.large.round   → initial
button.static.appearance.outlined.small.round → initial
button.dynamic.press.filled.small.round       → initial → press-start → press-progress → released
```

以上 ID 是组织方式示意，不是当前用例清单。每个配置应能独立筛选、重跑和定位，
不将无关的静态配置串成一个长用例的采样点，也不为每个组合复制场景代码。

静态测试默认覆盖核心外观维度的全部有效组合，例如 Button 的样式 × 大小、
Navigation Rail 的形态 × 对齐方式。根据固定上游确认实际支持范围，排除无效组合。
其他维度按实现关系和边界问题补充，不默认将所有维度相乘，也不统一使用两两组合替代分析。

下面是数据源的伪代码；配置类型、集合和辅助函数由作者按组件定义，不是新增框架 API：

```csharp
// 基本外观：全部有效的样式 × 大小。
foreach (var config in ValidAppearanceConfigurations())
    yield return StaticCase(config);

// 补充配置：按实现中的依赖关系选择，并排除与前面重复的配置。
foreach (var config in AdditionalConfigurations())
    yield return StaticCase(config);
```

例如，禁用表现若只依赖样式，可以对每种样式选择代表大小；如果也依赖大小，就覆盖相关组合。
图标布局、非文字内容尺寸、受限空间、RTL 和主题等同样根据上游及本地实现判断，不能只凭维度名称假定独立。
省略组合的依据用简短代码注释说明即可，不要求另建覆盖元数据。已发现的非文字问题应保留为回归用例；
原场景含文字时迁移到稳定的非文字夹具，纯文字问题明确留在当前未覆盖范围。
不同数据组的交集由作者明确排除，重复 ID 仍由框架报错。

动态用例按行为分支选择，例如按压、取消、中断、反向和禁用输入；不必逐项重跑整个静态矩阵。
不同配置走不同交互或动画路径时分别覆盖，共享路径时可选择代表配置。
禁用外观与禁用输入响应是不同验证目标；中断、取消等独立行为宜分别建场景，避免一条时间线承担过多目标。

不设每组件用例数或采样点数的统一上限，但每组用例和采样点都应有明确的验证目标。
通过代码数据源、共享场景和按需读取结果控制重复代码与阅读成本；执行成本另行衡量。
场景代码、数据组及必要的简短注释应清楚表达验证目标、构造配置、操作和采样、所选比较证据。
不要求作者另写故障假设、证明错误如何影响观测，或维护统一的用例说明元数据。
固定尺寸外观矩阵之外，应按组件能力安排自动尺寸和约束布局用例。

## 测量按需选择

视觉效果和行为是判断的核心。结构化测量将截图中难以辨认的细微差异变成可搜索、可比较、
可关联到源码的证据，辅助调试者排查原因；报告不需要直接指出哪处实现有问题。

组件可以维护可复用的测量定义，静态和动态场景按需组合，不要求每个场景提交完整 probe 清单。
已有证据足以验证目标时，截图配合少量读数即可，不追求属性覆盖率。

```csharp
// 伪代码：复用读数能力，场景直接选择需要提交的事实。
Sample("initial", screenshot: true, probes: [
    MeasureContainer(button)
]);

// 验证内容布局时，再加入 MeasureContent(button)。
// 不需要为所有场景统一开启一套“完整测量模式”。
```

- 相同事实复用稳定的身份、定义和单位；按 container、content、icon 等语义配对，不要求双端内部树相同。
- 读取真实状态和绘制结果，输入配置不能冒充执行结果；相同名称也不能证明双端读数语义相同。
- 允许按需添加内部动画值、中间量等诊断属性，有持续价值的保留为回归证据，临时排查读数可以移除。
- 内部表示差异本身不等于视觉或行为不一致。诊断读数应如实说明定义与来源，不为配对伪造相同语义。
- 双端都未选择采集的属性不计为缺失，也不据此计算完成率；已经选择采集却无法取得的数据应明确记录不可用原因。
  仅一端缺失字段时仍按现有协议呈现 absent / notCompared，不能将其解释为相等。
- 比较证据必须关联场景目标。交互场景按需记录实际回调次数和状态，布局场景读取最终几何，
  视觉场景结合截图与实际绘制属性；不统一提交无关的文字、内部状态或全部诊断字段。
- 新增或重写的场景先用 self-test 检查各端自身的重复性，再分析双端差异。
  重复性通过不代表双端一致；真实的不一致仍应保留。

当前比较器对结构化事实执行精确比较，Raster 单独应用统一容差；没有自动识别“仅供诊断”并排除门禁的机制。
调试者需要结合源码解释诊断差异；字段状态、不可用原因和严格门禁的含义分别见
[MEASUREMENTS](MEASUREMENTS.md)与[README](README.md#运行)。

## 初始状态与采样时机

静态场景直接构造目标配置，由作者保证采集时组件处于稳态；状态转换过程另用动态场景验证。
需要完成哪些布局、绘制、初始化操作和帧推进，由熟悉组件原理的作者决定。
框架不推断稳态，也不规定统一的初始化流程或“等待若干帧即可稳定”。

持续动画组件由作者安排从第 0 帧开始采集，使用已有手动时钟控制后续推进。
挂载、动画启动和首次采样的顺序由作者明确实现，不先运行到任意较晚相位再开始记录。
循环动画采集周期内关键时刻和跨周期边界；双端按同一时间线推进，不能分别寻找最相似的相位。

动态场景默认围绕行为阶段选择观察时刻，不要求逐帧记录，也没有固定数量的采样模板：

```text
初始状态 → 输入 → 开始变化 → 变化途中 → 释放/切换 → 转换后 → 完成之后
```

没有过渡的行为可能只需前后两个采样点，复杂动画可以增加观察时刻。
验证过渡过程必须包含途中证据，只有起点和终点不足以验证动画。
双端执行相同的输入时间线，在相同观察时刻采样；不能各自等到“进度 50%”再比较，
以免掩盖启动时间或时长差异。时间相同之外，还需核对事件、帧和绘制顺序，具体语义见下方执行边界。

发现问题时允许加密相关时间段，修复后保留有回归价值的采样点，纯排查用途的可以移除。
completed / settled 等名称需要明确的完成依据；仅在固定较晚时刻观察时，使用 late-300000 等描述。
截图按验证和排查需要选择，不要求每个动态采样点都截图。

## 组件文件

每个组件维护独立的 `XxxCases.cs` / `XxxCases.kt`，包含配置类型、数据源（仅 C#）、场景过程与专用测量。
共享的主题、字体和布局辅助函数放在 `CaseDefaults`；没有中央场景类或手工入口注册表。
框架自测的最小夹具位于独立测试项目，不依赖组件用例，也不参与生产用例发现。

## 定义测试类和数据源

下面是中性的接入示例；场景体省略，只说明声明和配置传输，不提供组件实现模板：

```csharp
public sealed record ExampleConfig(double Value);

[ParitySuite("example")]
public sealed class ExampleCases
{
    public static IEnumerable<CaseData<ExampleConfig>> Configurations()
    {
        yield return new("low", "Low input", new ExampleConfig(1));
        yield return new("high", "High input", new ExampleConfig(2));
    }

    [ParityScenario("static", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ExampleConfig>();
        await using var host = new AvaloniaHost(writer);
        // 构建场景、初始化、读取事实并 writer.Submit(host.Snapshot(...))。
    }
}
```

- 测试类标记 `ParitySuite`，是具有公开无参构造函数的公开、非抽象、非泛型类。
- 场景是类中直接声明的公开实例方法，签名为 `Task Method(CaptureWriter)`，标记 `ParityScenario`。
- Data 指向同一类中的公开静态无参方法，返回 `IEnumerable<CaseData<T>>`。单独用例也返回一项数据。
- 完整用例 ID 为 `suite + "." + CaseData.Id`，例如 `example.low`；不同场景的数据 ID 也不能重名。
  suite、scenario、数据 ID 使用小写字母、数字、点和短横线，以字母或数字开头，按语义命名，不依赖枚举下标。
- suite/scenario 是双端执行身份，方法名和类名不参与传输。重命名 C# 方法不需要同步重命名 Kotlin 方法。

配置组合使用普通循环或 LINQ，不为每个组合建立新方法。数据源只生成数据，不创建控件、启动宿主或写文件；
构造函数也不负责建立场景。框架每次捕获都创建新的测试类实例，自检的重复捕获同样如此。
资源由场景内的 `await using` / `use` 管理；框架目前不提供构造注入、Setup/Teardown 或继承式场景发现。

## Compose 端配对

Compose 只实现场景，不重复维护用例矩阵：

```kotlin
@Serializable
data class ExampleConfig(val value: Double)

@ParitySuite("example")
class ExampleCases {
    @ParityScenario("static")
    suspend fun capture(writer: CaptureWriter) {
        val config = writer.readConfig<ExampleConfig>()
        ComposeHost(writer).use { host ->
            // 使用真实 Compose API 构建场景，并提交对应样本。
        }
    }
}
```

参考端的测试类为 `parity` 包内的公开顶层类，有公开无参构造函数；场景为公开的 suspend 实例方法，
接收一个 CaptureWriter，返回 Unit。标记使用与 C# 相同的 suite/scenario，即可自动配对，无需手写注册。
当前发现器只扫描 C# 测试执行程序集和 Compose producer 自己的 jar，不遍历依赖程序集或依赖 jar。

配置字段使用 camelCase，缺少必填参数、未知字段、类型不符或不允许的 null 会失败。
无参数场景使用 EmptyConfig，生成 `{}`。输入各自映射到真实组件 API，不能将一端的运行结果作为另一端的预期值。
所有事件、帧和采样过程仍写在场景代码中，不引入 JSON 执行语言。

## 发现、冻结与执行

```text
发现测试类和场景 → 校验声明 → 展开 C# 数据源 → 校验并排序用例 ID
    → --case / --family 筛选 → 冻结全部选中用例的 JSON
    → prepare-reference：核对 Compose 入口 → 独立实例采集 → 发布参考快照
    → run：核对并固定参考快照 → 独立实例采集 Avalonia → 比较和生成结果
```

`list` 只编译和发现 C#，不创建测试类实例、图形宿主或 Java 进程；它会调用静态数据源。
`capture-actual` 和普通 `run` 不启动参考端。`prepare-reference` / `self-test` 在捕获前取得
参考端场景索引，缺失入口直接报错。`run` 检查参考实现、配置和捕获身份，参考不适用时要求重新准备。
`--family` 按 suite ID 筛选。

以下占位符替换为实际 suite 和用例 ID：

```powershell
./tests/VisualParity/parity.ps1 list --family '<suite>'
./tests/VisualParity/parity.ps1 prepare-reference --family '<suite>'
./tests/VisualParity/parity.ps1 run --case '<case-id>'
./tests/VisualParity/parity.ps1 self-test --case '<case-id>'
./tests/VisualParity/parity.ps1 test
```

重复 suite/scenario、错误方法或数据源签名在发现时失败；重复用例 ID 在展开后失败。
数据源枚举后配置序列化为独立对象，捕获前生成内部 `case.json`。双端读取同一份冻结的配置，
完整元数据参与参考缓存身份，公开 `case.yaml` 的 metadata 保存 suite、scenario 和 config。
参考端在一轮准备或自检中复用同一 JVM；每次捕获创建新的测试类实例、Host 和时钟，
用例不能依赖其他用例执行过或依赖进程重启来清除自身静态状态。配置或参考源码改变后重新准备参考，
仅修改 Avalonia 控件实现无需重新采集 Compose。

用例成功入口至少提交一个 Sample。样本 ID 在捕获内唯一，时间不能倒退；同一时间可以有不同 ID 的样本。
Point ID 在样本内唯一，Field 名在 Point 内唯一，双端按身份配对。领域对象关系不要求内部视图树一致。
具体测量定义见 [MEASUREMENTS](MEASUREMENTS.md)。作者只提交事实，不手写 YAML、manifest 或差分。

## 执行与采集边界

- Frame 只投递一次动画帧，然后 Draw。AdvanceTo 只推进事件逻辑时间，不投递帧。
- Compose 的 AdvanceTo 同时推进 TestCoroutineScheduler，其 delay 精度为整毫秒；Avalonia 的任意 Task.Delay 和框架计时器未虚拟化。
- Draw 处理布局/渲染而不投递 ComposeAnimation 帧；Snapshot 被动读取已有结果，不 Draw、不 Frame。
- Sample 同时保存 timeUs 和 frameTimeUs。时间相同不保证输入、注册与动画开始顺序相同，trace 保留操作次序。
- Avalonia 用 Skia headless、ManualAnimationFrameClock 和 GetLastRenderedFrame；绘制最多处理 20 轮工作，不是全局异步静止证明。
- ExtraImages 只在作者已定义的非截图采样点增加被动截图，不增加样本、帧或绘制。
- 关闭窗口后等待动画根清理并检查残留任务；用例/动画/清理异常保留为执行错误。

共享读数器提供几何和文字布局等通用事实；组件专用的视觉与行为读数由用例作者实现。
现有通用读数的定义和限制见 [MEASUREMENTS](MEASUREMENTS.md)。
框架能力或自测通过不代表所有控件变体、键盘/焦点、Popup、原生桌面或跨平台已经覆盖。

每次 Frame 只发送一个帧；跳到较晚时间不会补齐中间帧。
送帧节奏与采样时刻应分别安排：需要观察正常动画时，连续推进固定节奏的帧，
再在选定时刻读取样本；只在少量观察时刻送帧测到的是稀疏渲染，不等于连续动画。
当前 Button 动态场景使用从 0 开始、每 16,000µs 一帧的绝对时间网格，输入前补齐不晚于输入时刻的帧。
非网格时刻只推进事件时间、绘制和采样，不增加动画帧；例如 time-75000 的 timeUs=75000、frameTimeUs=64000。
加密采样不能改变送帧历史；若输入恰在网格时刻，当前 Button 场景先送帧再执行输入。
两端有相同的时间戳仍需核对输入顺序、动画注册、调度器与最后绘制的关系。
若观察输入后但绘制前的状态，当前属性与截图可能属于不同阶段，不能默认截图已反映该输入。
Compose probe 使用普通 holder，不能通过采集回调触发重组。
提交器复制集合，后续报告不会再读取活的控件或布局对象。

## 实现入口

主项目根目录只保留 CLI 的 `Program.cs` 和项目文件；各目录按职责组织：

| C# 目录 | 职责 / 主要入口 | Kotlin 对应目录 |
| --- | --- | --- |
| [Cases/](Material3Expressive.Parity/Cases) | 独立组件测试类、配置数据源与专用测量 | `cases/` |
| [Execution/](Material3Expressive.Parity/Execution) | 标记、CaseData、自动发现、展开、双端场景核对与实例执行 | `execution/` |
| [Protocol/](Material3Expressive.Parity/Protocol) | 元数据、采集事实、JSON 包校验、规范像素 | `protocol/` |
| [Capture/](Material3Expressive.Parity/Capture) | 宿主、快照、通用读数、环境、Compose 进程与缓存 | `capture/` |
| [Comparison/](Material3Expressive.Parity/Comparison) | 比较结果模型、事实配对与精确差分 | 参考端不负责比较 |
| [Results/](Material3Expressive.Parity/Results) | YAML、Markdown 概览和 run 索引；交互前端在 report/ | 参考端不负责报告 |

C# Program.cs 和 Kotlin Main.kt 保留为运行入口。框架测试按 Cases、Protocol、Capture、Comparison、Results 归类，
测试项目根目录 ParityTests.cs 保存共享夹具，TestAssembly.cs 配置测试宿主。

场景、输入、取样时间及测量内容均由作者决定。框架不自动发现控件属性，也不声明覆盖完整。
专用读数器也应明确来源与语义限制，不能用推测值替代无法采集的事实。
