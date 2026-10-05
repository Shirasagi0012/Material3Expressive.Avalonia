# Material 3 Expressive for Avalonia

 Avalonia 的 Material 3 Expressive 主题/控件库，以尽可能在视觉效果和动效上还原 Compose material3 为目标。

## 注意事项

> [!WARNING]
> 项目大量借助 Codex 来移植 Compose material3 的组件，并且目前还有很多组件的代码我没有仔细审查。

> [!WARNING]
> 使用 SkiaSharp V4 以提供对 MaterialSymbols VF 的支持。

## 组件 

到目前为止已经实现了多数基础组件，较全面的基础能力，不过很多依赖复杂功能的组件还没有实现。

## 安装与使用

当前源码的依赖准备、最小桌面应用、主题/字体/动画初始化见[接入指南](docs/GETTING_STARTED.md)。

- [全部组件](docs/COMPONENTS.md)：按用途浏览组件用法与维护说明
- [Button](src/Material3Expressive/Components/Button/README.md)：主题、尺寸、命令与内容
- [TextField](src/Material3Expressive/Components/TextField/README.md)：文本绑定、标签、内容槽与输入边界
- 维护组件时先看[移植指南](docs/PORTING.md)；文档分工见[组件文档约定](docs/COMPONENT_DOCS.md)

## 仓库结构

- `src/Material3Expressive`：控件库；`src/Material3Expressive.Catalog`：展示应用。
- `modules/MaterialShapes`：形状库、Gallery、测试和文档。
- `modules/ComposeAnimation`：Runtime、Avalonia、TestSupport、Catalog、测试和文档。
- `modules/MaterialSkiaShadow`、`modules/MaterialTypography.Roboto`：阴影与字体模块。
- `tests`：控件测试与 VisualParity。

以上模块均由本仓库直接管理。MaterialColorUtilities、MaterialSymbols、SmoothScroll.Avalonia
保留独立仓库，本项目只消费 NuGet 包；具体版本见 `Directory.Packages.props`。

## 本地开发

需要 .NET 10 SDK；VisualParity 脚本另需 PowerShell。部分依赖从 GitHub Packages 恢复，
环境与身份验证准备见[接入指南](docs/GETTING_STARTED.md#源码与依赖)。不需要初始化 Git 子模块。

```powershell
dotnet restore Material3Expressive.slnx
dotnet build Material3Expressive.slnx --no-restore
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore
dotnet run --project src/Material3Expressive.Catalog --no-restore
```

VisualParity 的独立运行方式见 [测试文档](tests/VisualParity/README.md)。

## 版本与发布

各可发布项目在自己的 csproj 中声明包版本；外部依赖版本由相应的中央包配置管理。
[手动发布工作流](.github/workflows/publish-nuget.yml)构建所选 ref，按各项目版本打包
ComposeAnimation Runtime/Avalonia/TestSupport、MaterialShapes、Shadow、Roboto 与主库，
再按依赖顺序推送至 GitHub Packages。工作流不会从 tag 名推导或统一覆盖这些版本。
发布包的使用与版本核对见[接入指南](docs/GETTING_STARTED.md#源码接入与发布包)。
