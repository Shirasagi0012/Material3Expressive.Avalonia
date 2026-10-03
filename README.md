# Material 3 Expressive for Avalonia

基于 Avalonia 实现 Material 3 Expressive，以尽可能在视觉效果和动效上还原Compose material3为目标

当前包含 Button、IconButton、Surface、[NavigationRail / NavigationRailItem](src/Material3Expressive/Components/NavigationRail/README.md)、
[WideNavigationRail / ModalWideNavigationRail / WideNavigationRailItem](src/Material3Expressive/Components/WideNavigationRail/README.md)、
[NavigationBar / NavigationBarItem](src/Material3Expressive/Components/NavigationBar/README.md)、
[Slider / VerticalSlider / RangeSlider](src/Material3Expressive/Components/Slider/README.md)、
[原生 CheckBox 的两态/三态 Material 模板](src/Material3Expressive/Components/CheckBox/README.md)，以及 Token、颜色、图标、阴影和交互动画等基础能力。
这些是现有实现范围，不代表所有组件已经对齐当前上游基线。
已接入 [Compose 视觉一致性测试](tests/VisualParity/README.md)，提供真实双端捕获、确定性动画回放及差异报告。

## 文档

- [AGENTS.md](AGENTS.md)：开发约束与 Agent 的阅读入口。
- [架构说明](docs/ARCHITECTURE.md)：项目结构、已有能力、职责边界和接入方式。
- [移植指南](docs/PORTING.md)：调查上游、实施移植、处理适配与记录决定。
- [上游源码入口](AGENTS.md#上游源码)：日常移植所需的仓库位置与固定版本读取方式。
- [基线选择与核验记录](docs/UPSTREAM_BASELINE.md)：升级上游、调查版本差异或维护参考产物时按需查阅；版本与校验值见 [JSON](docs/upstream-baseline.json)。
- [视觉一致性测试](tests/VisualParity/README.md)：运行命令与结果阅读；新增用例见[用例编写](tests/VisualParity/AUTHORING.md)。

组件专属说明随组件放在 `Components/<组件>/README.md`，按需补充。
重要本地设计在维护者确认后简要记录于 `docs/DECISIONS.md`，首次需要时再创建。

## 本地开发

需要 .NET 10 SDK 和 PowerShell。框架与包版本由
[Directory.Packages.props](Directory.Packages.props) 管理，包来源由 [nuget.config](nuget.config) 管理。

先在仓库根目录初始化子模块：

```powershell
git submodule update --init --recursive
```

ComposeAnimation 通过 NuGet 包接入；当前本地包源为相邻仓库的 `../ComposeAnimation/packages`。
准备与中央版本一致的 `ComposeAnimation.Runtime`、`ComposeAnimation.Avalonia`、
`ComposeAnimation.TestSupport` 三个包。如果尚未生成，在相邻 ComposeAnimation 仓库可用的前提下执行：

```powershell
[xml]$packageVersions = Get-Content ./Directory.Packages.props
$composeVersion = ($packageVersions.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'ComposeAnimation.Avalonia').Version
& ../ComposeAnimation/pack.ps1 -Version $composeVersion
```

打包脚本拒绝覆盖同版本产物；已有匹配包时直接复用。需要改变依赖内容时使用新的包版本并同步中央版本，
不要改成跨仓库的 ComposeAnimation `ProjectReference`。更换本地包源位置时同步调整 NuGet 配置。

准备好依赖后执行：

```powershell
dotnet restore Material3Expressive.slnx
dotnet build Material3Expressive.slnx --no-restore
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore
dotnet run --project src/Material3Expressive.Catalog
```

当前仓库保留着一项全新克隆限制的记录：部分子模块固定提交在原本地检出中存在，
但无法从配置的远端获取。该问题修复并验证前，上述命令不构成全新环境可复现的保证；
子模块可获取性与 CI 恢复仍需处理。

## 仓库布局

- `src/Material3Expressive/`：控件库与主题入口。
- `src/Material3Expressive.Catalog/`：测试与展示用 demo。
- `tests/Material3Expressive.Tests/`：当前控件库测试。
- `tests/VisualParity/`：Compose 参考端、Avalonia 捕获端、比较器及系统自身测试。
- `modules/`：颜色、图标、阴影、字体等配套模块，部分为 Git 子模块。
- `docs/`：通用架构、移植方法与上游依据。

旧实现与旧设计保留在 Git 历史中；其中的设计不自动成为当前实现的要求。
