# Material 3 Expressive

基于 Avalonia 实现 Material 3 Expressive，以固定版本的 Compose Material3 为依据，忠实移植其逻辑、视觉效果和交互行为。

## 项目结构

- `src/Material3Expressive/`：控件库；`Components/` 按组件组织，`Foundation/` 提供共享能力，`Tokens/` 和 `Baseline/` 提供 Token 定义与默认值。
- `src/Material3Expressive.Catalog/`：组件展示应用。
- `tests/Material3Expressive.Tests/`：控件库测试。
- `tests/VisualParity/`：Compose 参考端、Avalonia 捕获端、比较器及系统自身测试。
- `modules/`：颜色、图标、阴影、字体等配套模块，部分为 Git 子模块。

## 开始工作

- 环境、依赖和运行命令见 [README.md](README.md)。
- 移植或修改组件前，按下方“上游源码”直接定位参考，阅读 [移植指南](docs/PORTING.md)和[架构说明](docs/ARCHITECTURE.md)中相关能力；组件目录有 README 时一并阅读；已有 INTERNALS.md 时同时阅读其维护说明。
- 文档只按需更新，不要求每个任务创建计划、对应表或决策记录。
- [视觉一致性测试](tests/VisualParity/README.md)：运行命令与结果阅读；新增用例见[用例编写](tests/VisualParity/AUTHORING.md)。
- 修改基础能力前，阅读架构说明并检查实际调用方；检查能力说明及 `docs/DECISIONS.md` 中相关共享决定的适用范围。组件用法放在 `Components/<组件>/README.md`；采用拆分结构的组件，其内部机制、移植差异和已确认决定放在同目录 `INTERNALS.md`，按需补充。通用能力与接入规则留在架构说明，跨组件的共享能力决定留在 `docs/DECISIONS.md`。VisualParity 的设计决定见其[测试目录文档](tests/VisualParity/DECISIONS.md)。
- 已确认的组件设计就近记录在对应 INTERNALS.md（尚未拆分的组件暂存 README）；跨组件的共享能力决定记录于 [docs/DECISIONS.md](docs/DECISIONS.md)。
- 提交使用惯例式提交风格的提交信息。

ComposeAnimation 与 SmoothScroll 通过 `Shirasagi0012.*` NuGet 包接入，版本由
[Directory.Packages.props](Directory.Packages.props) 管理，GitHub Packages 包源及映射由
[nuget.config](nuget.config) 管理。依赖准备与包源访问见[接入指南](docs/GETTING_STARTED.md#源码与依赖)，
不再要求相邻 ComposeAnimation 本地包目录，不改成跨仓库 ProjectReference。

## 本地开发

需要 .NET 10 SDK 和 PowerShell。框架与包版本由
[Directory.Packages.props](Directory.Packages.props) 管理，包来源由 [nuget.config](nuget.config) 管理。

先在仓库根目录初始化子模块：

```powershell
git submodule update --init --recursive
```

需要改变依赖内容时使用新的包版本并同步中央版本；不要把包源凭证提交到仓库。

准备好依赖后执行：

```powershell
dotnet restore Material3Expressive.slnx
dotnet build Material3Expressive.slnx --no-restore
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore
dotnet run --project src/Material3Expressive.Catalog
```

## 上游源码

| 参考 | 本机仓库 | 固定版本字段（`docs/upstream-baseline.json`） |
| --- | --- | --- |
| AndroidX：Material3 逻辑的默认依据 | `D:\source\androidx` | `androidx.commit` |
| Compose Multiplatform Core：桌面实现与可执行参考的源码 | `D:\source\compose-multiplatform-core` | `composeMultiplatformCore.commit` |

按 JSON 中的 commit 读取，不以本地 HEAD 或浮动版本替代。在项目根目录可这样读取 Button：

```powershell
$upstream = Get-Content docs/upstream-baseline.json -Raw | ConvertFrom-Json
git -C D:\source\androidx show "$($upstream.androidx.commit):compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Button.kt"
git -C D:\source\compose-multiplatform-core show "$($upstream.composeMultiplatformCore.commit):compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Button.kt"
```

涉及平台实现时查看 Core；发现两者行为不同则明确提出，不自行混用。其他机器可使用不同的本地路径，
仓库 URL 和固定 commit 仍以 JSON 为准。
[基线选择与核验记录](docs/UPSTREAM_BASELINE.md)仅在升级上游、调查版本差异或维护参考产物时按需阅读，日常组件移植不必通读。

## 设计取向

以下是帮助取舍的默认偏好，不是机械规则。允许为明确需求承担复杂度，但应能解释其收益。

- 优先让源码直接呈现领域概念、数据关系和执行过程。简单操作应有相称的表达成本。
- 新增类型、接口或中间层，应解决当前明确的问题。局部变化若需要多层包装、转发或相似类型，应重新审视建模。
- 用数据和组合表达适合统一的差异，同时保留有意义的语义区别。不要为每种参数组合建立专门类型，也不要为减少类型数量引入更复杂的通用框架。
- 可扩展性、可检查性和防御性机制应有实际用途；调用入口简短不足以证明内部设计合理。
- 未经任务授权，不为统一风格而扩大重构范围。

## API 与移植

- 能直接移植的逻辑直接移植，保留算法、数据关系、状态转换、执行顺序和有意义的职责划分。不因本地写法更熟悉而重新设计或省略行为。
- 先检查已有基础能力及其限制。能力不足时指出具体缺口，不在组件内静默建立重复机制，也不自行降级效果。
- 框架适配必须说明具体原因与语义影响；已有明确决定可在其适用范围内直接复用。“更符合 Avalonia”本身不足以支持改写。
- 新的非平凡 API、共享抽象、职责调整和上游语义偏离由维护者决定。先提供具体问题、推荐方案、必要的调用示例和影响，确认后实施；已有授权不重复确认，普通等价转换不增加审批步骤。确认后的重要决定按移植指南简要记录。
- 分别说明尚未实现、未经验证和已接受的差异。本地现有代码或编译成功不能独自证明上游一致性，也不能证明某个设计已获批准。不得自行修改规范来迁就实现。
- 重视经过使用场景推敲的统一 API；相似操作采用一致的命名、参数结构和返回语义。
- 实现前先了解已有基础能力和对应的上游实现。影响效果的算法、参数、状态转换与边界条件应便于与 Compose 对照。
- 框架集成和对外 API 结合 C# 与 Avalonia 的使用方式设计。发生一致性、平台约束与复杂度的冲突时，说明具体取舍，不自行以近似效果替代既定目标。

## 函数与局部组织

- 长度适中、没有复用需求的流程默认保留在一个函数中，便于连续阅读；不要仅为给步骤命名而拆分私有方法。
- 函数过长、存在实际或具体可预见的复用，或具有清晰的独立职责时，考虑提取；不采用固定行数阈值。
- 接受局部函数；仅服务当前方法的辅助逻辑不必提升为类成员。

## 集合与表达

- 非性能热点默认偏好整齐的 LINQ 链式表达；复杂转换可结合局部函数，复杂度本身不是改写为循环的充分理由。
- 不因笼统的性能担忧增加样板；存在明确性能约束或其他写法更清楚时可以例外。
- 不为链式外观改变执行语义或掩盖重要的状态变化。

## MVVM

- 使用 CommunityToolkit.Mvvm 且工具链支持时，ViewModel 的可观察属性优先采用 `[ObservableProperty]` partial property，让源码直接呈现属性声明。
- 生成器已能满足需求时，不手写等价的字段、访问器和通知样板；特殊行为可使用扩展机制或手写实现。
- 此偏好不替代控件自身的 Avalonia 属性机制。

## 失败与结果

- 预期内失败偏好显式状态返回，按需使用 `bool` 或自定义状态枚举，通过输出参数提供结果；失败原因影响调用者处理方式时再区分状态。
- 不为简单失败处理引入繁琐的通用 Result/Option 包装，也不把所有异常捕获后压缩成 `false`。
- Union 不自动引入；异步结果表达和异常边界尚无统一偏好，结合具体场景设计。
