# Material 3 Expressive for Avalonia

 Avalonia 的 Material 3 Expressive 主题/控件库，以尽可能在视觉效果和动效上还原 Compose material3 为目标。

## 注意事项

> [!WARNING]
> 项目大量借助 Codex 来移植 Compose material3 的组件，并且目前还有很多组件的代码我没有仔细审查。

> [!WARNING]
> 使用 Avalonia 的不稳定 API `ISkiaSharpApiLeaseFeature` 以绘制和 Compose 相同的阴影。
> 
> 还使用 SkiaSharp V4 以提供对 MaterialSymbols VF 的支持。

## 组件 

到目前为止已经实现了多数基础组件，较全面的基础能力，不过很多依赖复杂功能的组件还没有实现。

## 安装与使用

当前源码的依赖准备、最小桌面应用、主题/字体/动画初始化见[接入指南](docs/GETTING_STARTED.md)。

- [全部组件](docs/COMPONENTS.md)：按用途浏览组件用法与维护说明
- [Button](src/Material3Expressive/Components/Button/README.md)：主题、尺寸、命令与内容
- [TextField](src/Material3Expressive/Components/TextField/README.md)：文本绑定、标签、内容槽与输入边界
- 维护组件时先看[移植指南](docs/PORTING.md)；文档分工见[组件文档约定](docs/COMPONENT_DOCS.md)

## 本地开发

环境及依赖准备见[接入指南](docs/GETTING_STARTED.md#源码与依赖)。准备完成后，从仓库根目录运行：

```powershell
dotnet restore Material3Expressive.slnx
dotnet build Material3Expressive.slnx --no-restore
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore
dotnet run --project src/Material3Expressive.Catalog --no-restore
```

视觉对比另见 [VisualParity](tests/VisualParity/README.md)，其环境与通过含义和控件库测试不同。
