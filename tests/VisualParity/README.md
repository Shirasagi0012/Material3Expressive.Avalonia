# Visual parity

采集 Compose 与 Avalonia 的真实事实，按身份和语义精确比较。
公开结果采用 **用例概览 → 单个采样点详情** 两层结构：运行时直接输出概览，
Agent 通过文件系统读取 Markdown 与 YAML，无需调用查询脚本。
系统不推断原因，不用容差、配准或文字遮罩消除差异。

## 阅读入口

| 要做什么 | 阅读位置 |
| --- | --- |
| 运行测试、查看结果、了解格式版本 | 本文 |
| 选择覆盖组合、测量和采样时机，编写双端用例 | [AUTHORING.md](AUTHORING.md) |
| 理解测量定义、YAML、像素与差异状态 | [MEASUREMENTS.md](MEASUREMENTS.md) |

## 源码结构

```text
Material3Expressive.Parity/
  Program.cs                # CLI、筛选与运行流程
  Cases/                    # 独立组件测试类、数据源和专用读数器
  Execution/                # 自动发现、数据展开和实例执行
  Protocol/                 # 元数据、采集事实、JSON 与规范像素
  Capture/                  # 宿主、快照、通用读数、环境和参考进程
  Comparison/               # 差分模型和比较算法
  Results/                  # YAML、概览和 HTML 报告
    Templates/              # HTML / CSS
reference/src/main/kotlin/
  Main.kt                   # Compose 进程入口
  cases/                    # 与 C# 对应的组件场景
  execution/                # 注解、场景发现与 suspend 调用
  protocol/                 # 采集传输契约
  capture/                  # Compose 宿主、快照与通用读数
Material3Expressive.Parity.Tests/
  Cases/ Protocol/ Capture/ Comparison/ Results/
```

新增或修改用例直接进入对应的 `*Cases` 文件，在 `Cases/` 与参考端 `cases/` 中创建相应文件；
通过 suite/scenario 标记自动发现，无中央目录和入口注册表。更具体的入口说明见 [AUTHORING](AUTHORING.md#实现入口)。
各目录仍在同一程序集内；执行模型见编写文档。

## 输出与阅读

```text
<run>/
  summary.md                 # 整次运行的用例清单
  report.html                # 开发者的交互汇总页
  cases/<case>/
    overview.md              # 全部采样点、分类差异计数、详情链接
    samples/
      0000-initial.yaml       # 自包含的双端事实与差分
      0001-pressed-16000.yaml
    images/
      0000-initial.compose.full.png
      0000-initial.compose.full.rgba
      0000-initial.avalonia.full.png
      0000-initial.avalonia.full.rgba
      0000-initial.diff.full.png
      0000-initial.diff.full.rgba
    case.yaml                # 身份与实际配置、样本索引、执行错误、清理轨迹、自检结果
    environment.yaml         # 双端环境与二进制来源，每用例保存一次
    manifest.json            # 权威数据及图像的完整文件集合与哈希
    report.html              # 从这些 YAML 和像素文件生成
```

`overview.md` 每行对应一个采样点，列出 geometry、text、properties、motion 的
**different / notCompared** 数量；mixed 单列显示两侧类别冲突的字段。图像单独计数，未请求截图明确标注。
执行错误和重复性状态单独显示。计数表示观测次数，不是独立缺陷数或完成百分比。

```powershell
$caseDirectory = 'artifacts/visual-parity/<run>/cases/<case-id>'
Get-Content "$caseDirectory/overview.md"
Get-Content "$caseDirectory/samples/0002-pressed-32000.yaml"
rg -n -C 8 'cornerRadii:|status: different' "$caseDirectory/samples"
```

可一次读取多个采样文件，或用 Python 等工具解析 YAML，提取字段历史、筛选差异、读取规范像素。
不要求依赖本项目进程；[MEASUREMENTS.md](MEASUREMENTS.md)说明文件结构、合并规则和 Python 示例。
HTML 提供时间线、字段历史、原图/叠加/差异图、像素查询、环境、轨迹和重复性面板；
每个样本链接到对应 YAML。HTML 内嵌数据与像素，支持离线查看。

## 运行

当前执行参考为 Windows x64、density=1、fontScale=1。需要 .NET 10、JDK（本机 JDK 25）、
锁定 Gradle 依赖及项目 README 中的本地 ComposeAnimation NuGet 包。
参考源码与制品版本按 [`upstream-baseline.json`](../../docs/upstream-baseline.json) 固定。
YAML 读写使用锁定的 YamlDotNet。以下命令从仓库根目录执行；`<case-id>` 和 `<suite>` 替换为 list 输出的实际身份。
run、self-test 自动构建双端；list、capture-actual 只构建 C# 项目。
render 和 verify-reference 不自动构建，首次使用前执行 build。

```powershell
./tests/VisualParity/parity.ps1 build
./tests/VisualParity/parity.ps1 list
./tests/VisualParity/parity.ps1 run --case '<case-id>'
./tests/VisualParity/parity.ps1 run --family '<suite>'
./tests/VisualParity/parity.ps1 run
./tests/VisualParity/parity.ps1 self-test
./tests/VisualParity/parity.ps1 capture-actual --case '<case-id>'
./tests/VisualParity/parity.ps1 test
```

输出默认在 `artifacts/visual-parity/<run>/`；`--output <new-directory>` 指定新目录，不覆盖已有目录。
每个用例结束即在终端打印其完整概览及绝对路径。`run` 生成结果成功返回 **0**，即使存在差异。
`--require-equal` 显式启用严格门禁：存在差异返回 **1**，只有未比较项返回 **3**；执行错误优先返回 **2**。
`capture-actual` 的参考缺失项目为 notCompared。门禁仅覆盖提交的事实，不证明测量没有遗漏。

`self-test` 另做两端重复捕获和额外被动截图检查，结果保存到 `case.yaml` 的 repeatability，
不稳定返回 2。普通 run 不自动重复捕获。自检稳定不表示组件一致。

Avalonia 宿主的 `Draw()` 会处理 Dispatcher 工作以及待提交、待渲染的合成批次，
包括渲染回调产生的后续提交；队列暂时为空不代表画面已经更新。它不发送动画帧，
也不等待动画结束；`Snapshot()` 仍只读取已绘制的位图。该完成检查针对当前固定的
Avalonia 12.1.2 和 UI 线程渲染模式，局部读取 `Compositor` 的内部批次字段；
升级 Avalonia 或改变渲染线程模式时须重新核验，字段结构不匹配会报错。

```powershell
# 从公开结果重新生成概览和 HTML，不依赖采集缓存，也不执行两端组件。
./tests/VisualParity/parity.ps1 render --directory 'artifacts/visual-parity/<run>/cases/<case-id>'

# 校验内部 Compose 采集包及当前基线。
./tests/VisualParity/parity.ps1 verify-reference --directory <compose-capture-bundle>
```

### 文件版本

公开结果为 **schema 5**，用例元数据为 **schema 4**（suite/scenario 与必需的对象 config），双端采集协议仍为 **schema 2**。
只支持当前格式，不读取旧元数据或旧结果，没有升级、回退或 `compare` 转换入口；旧产物需要重新运行生成。
`render` 只更新派生的 Markdown/HTML，先验证权威文件集合、SHA256、样本身份和 PNG/RGBA 一致性。
manifest 不包含这两个可重建的展示文件。当前结果目录可以独立复制和重新呈现。

## 缓存与完整性

用例数据源只在 C# 各组件测试类中维护，支持循环生成配置；发现、展开、校验和筛选后，
运行器在捕获前冻结全部选中用例的完整 JSON，双端都读取它。元数据包含 config，整体参与参考缓存身份。
公开的 `case.yaml` 保留实际配置。内部原始包、自检重复包和临时差分保存在
`artifacts/visual-parity-captures/<id>/<case>/`，不混入公开结果；`case.yaml` 的 sourceBundle 保留诊断路径，
但读取或重建报告不依赖它。参考缓存仍在 `artifacts/visual-parity-reference/`，命中后校验文件集合、
SHA256、身份和像素协议。失败捕获不作为成功参考缓存。
`run --refresh-reference` 可强制重新采集 Compose 参考；这不会更改固定上游基线。

## 验证

系统测试覆盖差分、语义冲突、缺失、协议与像素完整性，以及真实控件采集和时钟重复性。
结果格式测试还验证 YAML 数值/字符串类型及精度、双端语义保留、同时间样本的原始执行次序、
单端与失败结果、文件篡改，以及脱离原始采集包后的 HTML 重建。
这些检查不表示组件已与 Compose 视觉一致。
