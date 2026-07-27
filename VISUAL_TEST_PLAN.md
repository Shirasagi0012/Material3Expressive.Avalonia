可以把它作为项目下一阶段的正式 Roadmap。总体原则是：先建立确定性的 Avalonia 回归基线，再扩展 Compose 对照，最后处理动画时间轴。

以下规划默认首个标准环境是 Windows x64、Skia、1.0 DPI；以后需要跨平台时，再增加独立的平台基线，不让不同平台共享像素基线。

## 一、目标与边界

视觉测试需要回答三类问题：

1. 本次代码修改是否意外改变了现有视觉？
2. 当前实现与固定版本 Compose 的差异在哪里？
3. 动画在关键时间点是否符合预期？

暂时不包含：

- 在线 CI 或自动上传。
- 对整个 Gallery 窗口直接截图。
- Windows、Linux、Android 之间的像素完全相等。
- 自动批准和覆盖基线。
- 首阶段的动画逐帧比较。

## 二、项目结构

建议拆成三个项目，避免测试运行、基线写入和比较逻辑相互耦合：

```text
tests/
├── Material3Expressive.VisualTesting/
│   ├── Cases/
│   ├── Rendering/
│   ├── Comparison/
│   ├── Reporting/
│   └── Baselines/
│
└── Material3Expressive.VisualTests/
    └── xUnit 只读验证入口

tools/
└── Material3Expressive.VisualBaseline/
    └── record / verify / list / report 命令

visual-references/
└── compose/
    └── 1.5.0-alpha25/
```

必须创建独立 VisualTests 程序集。Avalonia Headless 是进程级单例，而现有行为测试使用默认 Headless Drawing；视觉测试需要真实 Skia，两者放在同一测试进程中容易发生配置冲突。

## 三、视觉测试模型

每个场景使用稳定 ID 和显式配置：

```csharp
public sealed record VisualCase(
    string Id,
    PixelSize Viewport,
    double RenderScaling,
    VisualTheme Theme,
    Func<Control> CreateContent,
    Func<VisualContext, Task>? Prepare,
    ComparisonProfile Comparison,
    IReadOnlyList<string> Tags);
```

场景需要记录：

- 稳定的 `CaseId`
- 视口尺寸
- Light/Dark
- Seed color
- DPI
- Culture 和 FlowDirection
- 控件创建方法
- 截图前需要建立的交互状态
- 比较阈值
- 已知 parity exception
- 所属 Smoke/Full/Parity/Motion 测试集

例如：

```text
button/filled/medium/enabled/light
button/filled/medium/disabled/light
button/all-variants/medium/dark
icon-button/all-widths/small/light
textfield/outlined/error/dark
navigation/bar/selected/light
```

## 四、确定性渲染环境

VisualTests 使用真实 Skia：

```csharp
AppBuilder.Configure<VisualTestApplication>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions
    {
        UseHeadlessDrawing = false,
        ShouldRenderOnUIThread = true,
        Fps = 60
    });
```

Avalonia 12.1 已提供 `CaptureRenderedFrame`、强制渲染帧、DPI 设置和输入模拟，可以完全在无窗口环境运行。[Avalonia Headless 文档](https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform)

每次截图前必须统一执行：

1. 创建固定尺寸 TopLevel。
2. 设置 `RenderScaling`。
3. 设置主题、Culture、FlowDirection。
4. 显示窗口。
5. Pump Dispatcher。
6. 完成 Measure/Arrange。
7. 强制至少两个渲染帧。
8. 检查没有未完成的资源或布局任务。
9. 调用 `CaptureRenderedFrame`。
10. 保存渲染环境指纹。

环境指纹包括：

```json
{
  "os": "windows",
  "architecture": "x64",
  "avalonia": "12.1.0",
  "renderer": "skia",
  "renderScaling": 1.0,
  "culture": "en-US",
  "fontHashes": {},
  "pixelFormat": "BGRA8888-Premul"
}
```

指纹不匹配时应拒绝比较，不能用宽松阈值掩盖环境变化。

### 字体策略

字体是最大的不确定因素。

- 回归测试使用测试项目内固定携带的字体文件。
- 字体文件参与环境指纹。
- 不依赖操作系统字体 fallback。
- Compose parity 使用相同的 Roboto/Roboto Flex 静态实例。
- Typography 专门测试字体 fallback 和 emphasized slot。

核心库仍然不依赖字体包；这些字体只属于视觉测试资产。

## 五、比较引擎

### 1. Avalonia 自身回归

默认使用严格模式：

- 图片尺寸必须完全一致。
- 比较预乘 Alpha 后的 RGBA。
- 记录最大通道差。
- 记录变化像素比例。
- 计算 RMSE。
- 计算最大连续差异区域。
- 输出热力图和半透明叠加图。

建议提供四种 profile：

| Profile | 用途 |
|---|---|
| `Exact` | 纯色、形状、Token 色板 |
| `AntiAlias` | 文本、曲线、圆角 |
| `Shadow` | Elevation、Dialog、FAB |
| `ComposeParity` | 跨渲染器比较 |

具体阈值不应凭感觉确定。第一阶段重复渲染同一场景 100 次，以实际噪声上限校准；默认仍应尽可能接近 exact。

比较器本身需要合成图片单元测试，证明它能检测：

- 单像素变化
- 1dp 位移
- 1dp 尺寸变化
- 轻微颜色变化
- Alpha 变化
- 图片尺寸变化
- 超出阈值的连续区域

### 2. 失败产物

每次失败生成：

```text
artifacts/visual-diffs/{case-id}/
├── expected.png
├── actual.png
├── diff.png
├── overlay.png
├── metrics.json
└── index.html
```

HTML 报告按组件分类，支持 Expected/Actual、滑动叠加、热力图和指标展示。

## 六、基线管理

所有普通测试都必须只读。

推荐命令：

```powershell
dotnet run --project tools/Material3Expressive.VisualBaseline -- list

dotnet run --project tools/Material3Expressive.VisualBaseline `
  -- verify --set smoke

dotnet run --project tools/Material3Expressive.VisualBaseline `
  -- record --filter "button/*"

dotnet run --project tools/Material3Expressive.VisualBaseline `
  -- report --open
```

规则：

- `verify` 永远不修改基线。
- `record` 必须显式调用。
- 新场景没有基线时失败。
- 基线没有对应场景时报告 orphan。
- 只允许按组件或 filter 更新，避免一次覆盖全部。
- 更新后必须再运行一次 `verify`。
- PNG 直接进 Git，首期不引入 Git LFS。
- `artifacts/visual-diffs` 不进入 Git。

## 七、场景覆盖规划

采用“视觉板”而不是每个属性一张图。一个视觉板在固定网格里展示同一组件的一组紧密相关状态。

预计最终约 100–120 张 Avalonia 基线图。

| 测试域 | 主要场景 | 预计基线 |
|---|---|---:|
| Foundation | Color、Typography、Shape、Elevation、Theme Override | 10 |
| Button 系列 | Variant、Size、Toggle、Icon Width、Group、Split | 22 |
| FAB 与 Card | FAB Variant/Size/Extended、FAB Menu、Card | 10 |
| Selection | Checkbox、Radio、Switch、Slider | 14 |
| Progress | Linear、Circular、Wavy、LoadingIndicator | 8 |
| Input 与 Chips | TextField、Chip、Error、Selected、Disabled | 16 |
| Navigation | Bar、Rail、Drawer、Tabs、AppBar | 14 |
| Overlay | Dialog、Sheet、Snackbar、Tooltip、Badge、Menu | 14 |
| Search/Picker/Carousel | Open/Closed、日期、时间、布局模式 | 12 |
| Cross-cutting | RTL、DPI、长文本、动态色、子树主题 | 12 |

### 基础覆盖矩阵

每个组件至少覆盖：

- Light 与 Dark
- 默认状态
- Disabled
- 主要 Variant
- 主要 Size
- Selected/Checked/Error 等持久状态
- 空内容与正常内容

不为每个组件都重复：

- 所有 Seed color
- 所有 DPI
- RTL
- 超长文本
- Token override

这些交叉场景使用代表性组件即可：

| 交叉场景 | 代表组件 |
|---|---|
| 动态 Seed | Button、Card、TextField、Navigation |
| RTL | Button、TextField、AppBar、NavigationRail |
| 1.25/2.0 DPI | Typography、IconButton、Slider、Progress |
| 长文本 | Button、Chip、Snackbar、Dialog、Navigation |
| 子树主题 | Button、Popup/Menu、Dialog |
| Token override | Button、FAB、TextField |

## 八、交互状态

利用 Headless 输入 API 建立真实状态：

- `MouseMove`：Hovered
- `MouseDown`：Pressed
- `MouseUp`：Released
- `KeyPress(Tab)`：Focused
- Click：Checked/Selected/Open
- Escape：关闭 Overlay
- Arrow keys：Slider、Tabs、Navigation

不能直接设置伪类，因为这样无法验证控件从输入到视觉状态的完整路径。

首期只捕获稳定状态：

- Enabled
- Disabled
- Selected
- Checked
- Error
- Open/Closed
- 最终 Hover/Focus/Pressed 层

对于存在过渡动画的状态，需要先完成可控时钟，再纳入严格截图。

## 九、动画视觉测试

动画测试单独作为 `Motion` 测试集。

需要先把当前基于真实 Stopwatch 的动画时钟抽象为：

```csharp
internal interface IMaterialAnimationClock
{
    TimeSpan Now { get; }
    void RequestFrame(Action<TimeSpan> callback);
}
```

生产使用真实时钟，VisualTests 使用 ManualClock：

```csharp
clock.AdvanceTo(TimeSpan.FromMilliseconds(45));
renderer.Capture();
```

建议时间轴场景：

- StateLayer：0ms、15ms、45ms、150ms
- Ripple：press、快速 release、完整 fade
- Shape Morph：初始、25%、50%、overshoot、settled
- Spring retarget：改变目标前后
- FAB Menu：每个 item reveal 时间
- LoadingIndicator：多边形切换关键点
- Wavy Progress：相位 0、¼、½、¾

动画结果以 timeline strip 输出：

```text
0ms | 45ms | 90ms | 150ms | settled
```

动画测试中禁止 `Task.Delay` 和真实等待。

## 十、Compose parity

Compose 参考图由固定的 `compose-material3 1.5.0-alpha25` 生成。Android 官方支持 Compose Preview Screenshot Testing，可作为参考图生成入口。[Android Screenshot Testing](https://developer.android.com/training/testing/ui-tests/screenshot)

两端共享稳定 Case ID：

```text
button/filled/medium/enabled/light
```

参考条件：

- mdpi
- 固定视口
- 相同 Seed color
- 相同文案
- 相同字体资产
- 相同 Light/Dark
- 固定 AndroidX commit
- 禁用系统动画和时间变化

Compose 比较不直接使用严格像素门禁，而分成：

- 容器尺寸和位置
- 圆角与轮廓
- 区域平均色及色差
- 图标尺寸和位置
- 文本边界框与 baseline
- 阴影热力图
- 整体 overlay

第一版 parity report 只生成指标和人工结论；等积累稳定数据后，才能对几何和颜色设置自动阈值。

### 已知差异管理

所有 mask 或宽松阈值必须声明原因：

```json
{
  "case": "button/small/light",
  "exception": "small-button-pointer-padding",
  "documentedIn": "docs/parity.md"
}
```

不能添加匿名忽略区域。Elevation、Focus、Switch Drag、Menu、Carousel 等已知差异都应映射到 parity 条目。

## 十一、测试集分层

| 测试集 | 内容 | 建议目标 |
|---|---|---|
| `Smoke` | Foundation + 每个组件族一个代表 | 10 秒以内 |
| `Full` | 所有静态视觉板 | 60 秒以内 |
| `Interaction` | 输入与持久交互状态 | 30 秒以内 |
| `Parity` | Avalonia 与已有 Compose 图比较 | 2 分钟以内 |
| `Motion` | 可控时钟关键帧 | 60 秒以内 |

这些是性能预算，不是预先假定的实际时间；首个原型完成后根据测量调整。

## 十二、实施阶段

### Stage 0：确定性实验

- 建立独立 Skia Headless 项目。
- 固定字体、DPI、Culture、PixelFormat。
- 重复捕获 Button 100 次。
- 验证图片 Hash 是否稳定。

退出条件：同环境重复捕获没有非预期差异。

### Stage 1：框架闭环

- `VisualCase`
- Renderer
- BaselineStore
- PixelComparer
- Diff/Overlay
- HTML Report
- record/verify 命令
- 比较器自身测试

试点：Button、Color、Typography、Shape。

退出条件：故意制造 1dp 和颜色偏差时能准确失败并生成报告。

### Stage 2：第一组件波

- Button
- IconButton
- FAB
- Card
- Toggle
- Light/Dark
- Variant/Size/Disabled

退出条件：Phase 4 组件静态视觉覆盖完成。

### Stage 3：全部静态组件

按照 roadmap Wave 2–5 扩展所有组件。

退出条件：每个公开视觉组件至少有一个 Light、Dark 和 Disabled/Selected 代表场景。

### Stage 4：交互与交叉场景

- Headless 输入驱动
- RTL
- DPI
- 长文本
- Dynamic color
- Token override
- Popup/Overlay

退出条件：主要输入状态和跨组件风险均有代表覆盖。

### Stage 5：Compose 对照

- Compose 参考项目或 Preview Screenshot 配置
- Case ID 对齐
- Reference importer
- Parity profiles
- Parity HTML report
- 已知差异映射

退出条件：Button、FAB、Card、Selection、TextField、Navigation 各至少一组可评审对照。

### Stage 6：动画关键帧

- 可注入动画时钟
- StateLayer、Ripple、Spring、Shape Morph
- Timeline strip
- 动画比较 profile

退出条件：动画测试不使用真实等待且连续运行稳定。

### Stage 7：维护工具

- Orphan baseline 检查
- 环境指纹检查
- Baseline 体积统计
- 按组件过滤
- 失败报告索引
- Visual testing 文档

## 十三、整个框架的完成标准

整套视觉测试可以认为完成，需要同时满足：

- 同一环境连续运行 20 次无随机失败。
- 普通验证不会修改任何基线。
- 所有基线都有场景，所有场景都有基线。
- 1dp 位移、颜色变化和尺寸变化均能被合成测试检出。
- 所有失败都有 Expected、Actual、Diff、Overlay 和指标。
- 所有公开组件有静态视觉覆盖。
- 关键交互状态由真实输入产生。
- 动画由可控时钟驱动。
- Compose 差异有报告，并且每个忽略项都对应 `parity.md`。
- 新增一个组件只需要添加 Case，不需要修改渲染和比较框架。

建议实际实施顺序严格按 Stage 0 → 1 → 2 推进。先把 Button、Typography、Shape 的十几张基线跑到绝对稳定，再扩展组件数量；视觉测试框架最危险的不是覆盖少，而是在基础不稳定时一次生成几百张无法信任的图片。