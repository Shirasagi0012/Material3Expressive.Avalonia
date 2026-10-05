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

## 仓库结构

- `src/Material3Expressive`：控件库；`src/Material3Expressive.Catalog`：展示应用。
- `modules/MaterialShapes`：形状库、Gallery、测试和文档。
- `modules/ComposeAnimation`：Runtime、Avalonia、TestSupport、Catalog、测试和文档。
- `modules/MaterialSkiaShadow`、`modules/MaterialTypography.Roboto`：阴影与字体模块。
- `tests`：控件测试与 VisualParity。

以上模块均由本仓库直接管理。MaterialColorUtilities、MaterialSymbols、SmoothScroll.Avalonia
保留独立仓库，本项目只消费 NuGet 包；具体版本见 `Directory.Packages.props`。

## 本地开发

需要 .NET 10 SDK 和 PowerShell。（暂时）目前一部分依赖包位于我的 GitHub Packages，你需要配置你的 GitHub 账号 token 才能从 GitHub Packages restore。

```powershell
dotnet restore Material3Expressive.slnx
dotnet build Material3Expressive.slnx --no-restore
dotnet run --project src/Material3Expressive.Catalog
```

VisualParity 的独立运行方式见 [测试文档](tests/VisualParity/README.md)。
