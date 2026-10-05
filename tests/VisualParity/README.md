# Visual parity

采集 Compose 与 Avalonia 的真实事实，按身份和语义精确比较。
公开结果采用 **用例概览 → 单个采样点详情** 两层结构：运行时输出进度和概览路径，
Agent 通过文件系统读取 Markdown 与 YAML，无需调用查询脚本。
系统不推断原因，不进行配准或文字遮罩；结构化事实精确比较，Raster 另有明确的降采样容差判定，原始差异保留。

## 阅读入口

| 要做什么 | 阅读位置 |
| --- | --- |
| 运行测试、查看结果、了解格式版本 | 本文 |
| 选择覆盖组合、测量和采样时机，编写双端用例 | [AUTHORING.md](AUTHORING.md) |
| 理解测量定义、YAML、像素与差异状态 | [MEASUREMENTS.md](MEASUREMENTS.md) |
| 查阅框架取舍、适用范围和维护者确认依据 | [DECISIONS.md](DECISIONS.md) |

## 源码结构

```text
Material3Expressive.Parity/
  Program.cs                # CLI、筛选与运行流程
  Cases/                    # 独立组件测试类、数据源和专用读数器
  Execution/                # 自动发现、数据展开和实例执行
  Protocol/                 # 元数据、采集事实、JSON 与 PNG 解码
  Capture/                  # 宿主、快照、通用读数、环境和参考进程
  Comparison/               # 差分模型和比较算法
  Results/                  # YAML、Markdown 概览与 run 导航索引
reference/src/main/kotlin/
  Main.kt                   # Compose 进程入口
  cases/                    # 与 C# 对应的组件场景
  execution/                # 注解、场景发现与 suspend 调用
  protocol/                 # 采集传输契约
  capture/                  # Compose 宿主、快照与通用读数
Material3Expressive.Parity.Tests/
  Cases/ Protocol/ Capture/ Comparison/ Results/
report/                     # 独立 Vue 3 + TypeScript + Vite 应用
  server/artifacts.ts       # Vite 插件：run 列表和只读产物访问
  src/data/                # YAML 契约与按需读取
  src/pages/               # run 列表、run、case 页面与可分享路由
  src/components/          # 采样概览、测量、趋势、图像检查
  src/imaging/             # Worker、PNG 解码与像素计算
  tests/                   # 数据、像素与 Vite 接口测试
```

新增或修改用例直接进入对应的 `*Cases` 文件，在 `Cases/` 与参考端 `cases/` 中创建相应文件；
通过 suite/scenario 标记自动发现，无中央目录和入口注册表。更具体的入口说明见 [AUTHORING](AUTHORING.md#实现入口)。
各目录仍在同一程序集内；执行模型见编写文档。

`Capture/ReferenceCatalog.cs` 管理参考清单与失效判断，`ReferenceProducer.cs` 管理单个常驻 JVM；
`Execution/BuildSnapshot.cs` 固定执行二进制。比较器仅消费捕获证据，不控制场景执行。
`ResultFiles.Write` 返回已发布路径对应的事实，`Read` 用于独立回读；`RunResults` 只保留累计计数和计时。

## 输出与阅读

```text
<run>/
  summary.md                 # 整次运行的用例清单
  run.yaml                   # 运行状态、每个用例的结果摘要；不包含样本详情
  cases/<case>/
    overview.md              # 全部采样点、分类差异计数、详情链接
    samples/
      0000-initial.yaml       # 自包含的双端事实与差分
      0001-pressed-16000.yaml
    images/
      0000-initial.compose.full.png
      0000-initial.avalonia.full.png
      0000-initial.diff.full.png
    case.yaml                # 身份与实际配置、全部样本分类计数和索引、执行错误、清理轨迹、自检结果
    environment.yaml         # 双端环境与二进制来源，每用例保存一次
    manifest.json            # 权威数据及图像的完整文件集合与哈希
```

`overview.md` 每行对应一个采样点，列出 geometry、text、properties、motion 的
**different / notCompared** 数量；mixed 单列显示两侧类别冲突的字段。图像单独计数，未请求截图明确标注。
执行错误和重复性状态单独显示。计数表示观测次数，不是独立缺陷数或完成百分比。

```powershell
$caseDirectory = 'artifacts/visual-parity/runs/<run>/cases/<case-id>'
Get-Content "$caseDirectory/overview.md"
Get-Content "$caseDirectory/samples/0002-pressed-32000.yaml"
rg -n -C 8 'cornerRadii:|status: different' "$caseDirectory/samples"
```

可一次读取多个采样文件，或用 Python 等工具解析 YAML，提取字段历史、筛选差异、读取规范像素。
不要求依赖本项目进程；[MEASUREMENTS.md](MEASUREMENTS.md)说明文件结构、合并规则和 Python 示例。
开发者使用独立报告应用浏览这些产物：一个 Vite 实例展示所有 run，可选择 run、用例和采样点。
采样概览来自 `case.yaml` 的 `sampleSummaries`，选中样本才读取详情；趋势和轨迹面板按需读取该用例的全部样本。
原图直接引用现有 PNG，Worker 只解码当前图像对，完成叠加、差异阈值、精确取色和区域统计。
切换样本会取消旧请求并丢弃旧 Worker 响应。筛选、字段和采样选择使用路由/query 保存，可刷新或复制地址。
采集过程不再生成 HTML，不保存浏览器所需的像素副本。Agent 的文件读取方式不受报告应用影响。

## 运行

当前执行参考为 Windows x64，固定 density=4、fontScale=1。需要 .NET 10、JDK（本机 JDK 25）、
锁定 Gradle 依赖及项目 README 中的 NuGet 包源凭据。
Avalonia 捕获端通过 ProjectReference 使用仓内 ComposeAnimation.TestSupport，修改动画源码后直接重新构建。
`Material3Expressive.Parity/packages.lock.json` 记录外部包与仓内项目依赖；变更依赖后用
`dotnet restore Material3Expressive.slnx --force-evaluate` 更新，不再准备本地 ComposeAnimation 包。
参考源码与制品版本按 [`upstream-baseline.json`](../../docs/upstream-baseline.json) 固定。
脚本默认以 Release 构建和运行，避免 Debug 的逐像素计算开销；需要调试时可直接用 dotnet 的 Debug 配置。
YAML 读写使用锁定的 YamlDotNet。以下命令从仓库根目录执行；`<case-id>` 和 `<suite>` 替换为 list 输出的实际身份。
prepare-reference、self-test 自动构建双端；run、recompare、list、capture-actual 只构建 C# 项目。
普通 run 不启动 JVM，不隐式补采 Compose；参考缺失、配置或参考实现改变时，在 Avalonia 采集前列出问题。
verify-reference 不自动构建，首次使用前执行 build。
报告需要 Node.js 22.12+ 或 24+；仅查看已有结果无需启动 .NET 或 JVM。npm 依赖由 package-lock.json 锁定。

```powershell
./tests/VisualParity/parity.ps1 build
./tests/VisualParity/parity.ps1 list
# 首次或修改参考场景/配置后准备；未变化的用例直接复用。
./tests/VisualParity/parity.ps1 prepare-reference
./tests/VisualParity/parity.ps1 run --case '<case-id>'
./tests/VisualParity/parity.ps1 run --family '<suite>'
./tests/VisualParity/parity.ps1 run
./tests/VisualParity/parity.ps1 self-test
./tests/VisualParity/parity.ps1 capture-actual --case '<case-id>'
./tests/VisualParity/parity.ps1 test
# 显式固定某次参考快照；省略时在运行开始读取 references/current.json。
./tests/VisualParity/parity.ps1 run --reference '<snapshot.json>'
# 强制重新采集参考并发布新快照，旧快照和捕获不覆盖。
./tests/VisualParity/parity.ps1 prepare-reference --family '<suite>' --refresh-reference
# 只重算已有双端证据；源 run 需要 plan.json，不重新采集，不沿用旧重复性结论。
./tests/VisualParity/parity.ps1 recompare --directory '<run-directory>'

# 只读浏览已有结果，不重新采集；打开终端输出的 localhost 地址，Ctrl+C 停止。
./tests/VisualParity/parity.ps1 report
# 默认 http://localhost:5173，页面内选择 run；--port 5174 可调整 Vite 端口。
```

输出默认在 `artifacts/visual-parity/runs/<run>/`；`--output <new-directory>` 指定新目录，不覆盖已有目录。
每个用例结束即在终端打印紧凑进度、耗时和概览绝对路径。完整概览仍在 overview.md。
`run` 生成结果成功返回 **0**，即使存在差异。
`--require-match` 接受 equal 与 withinTolerance；存在 different 返回 **1**，只有未比较项返回 **3**。
`--require-equal` 要求原始事实完全相等，withinTolerance 也返回 **1**；两种门禁均以执行错误 **2** 优先。
`capture-actual` 的参考缺失项目为 notCompared。门禁仅覆盖提交的事实，不证明测量没有遗漏。

`self-test` 不读取参考缓存，在同一常驻 JVM 中实际执行两端重复捕获和额外被动截图检查，结果保存到 `case.yaml` 的 repeatability，
不稳定返回 2。普通 run 不自动重复捕获。自检稳定不表示组件一致。

`run`、`self-test`、`capture-actual` 均固定使用双端真实 4× DPI 渲染，无倍率选项：
320×240 DIP 的场景生成 1280×960 PNG，保留原始图和精确差分。Raster 另外用固定 4×4 块平均得到
1× 比较数据：整图及每个非空点区域均须满足最大通道差 ≤20、平均逐像素最大通道差 ≤1（0～255）。
原图不同但满足容差时标为 withinTolerance，不称为 equal；结构化事实与重复性自检仍精确比较。
算法、区域边界与透明通道约定见 [MEASUREMENTS](MEASUREMENTS.md#raster-容差判定)。
布局与输入仍使用 DIP，文字字号仍使用 sp；Compose 的物理像素测量换算回 DIP，
图像取色与 ROI 使用实际像素。倍率记入 environment.yaml 的 density 并参与参考缓存身份；
不同 density/fontScale 的图像不进行像素比较。提高 DPI 可能改变布局取整及字体度量。

Avalonia 宿主的 `Draw()` 会处理 Dispatcher 工作以及待提交、待渲染的合成批次，
包括渲染回调产生的后续提交；队列暂时为空不代表画面已经更新。它不发送动画帧，
也不等待动画结束；`Snapshot()` 仍只读取已绘制的位图。该完成检查针对当前固定的
Avalonia 12.1.2 和 UI 线程渲染模式，局部读取 `Compositor` 的内部批次字段；
升级 Avalonia 或改变渲染线程模式时须重新核验，字段结构不匹配会报错。

Compose 宿主的 `draw()` 在绘制后提交 snapshot 通知，再检查公开的
`hasPendingMeasureOrLayout` / `hasPendingDraw`；存在失效工作时继续布局和绘制。
例如 Popup 的 `onPlaced` 在布局期间写入锚点状态，会请求提示层重测。
最多处理 20 轮，未完成则报错；不推进动画帧，也不在 Snapshot 中补绘制。

```powershell
# 校验内部 Compose 采集包及当前基线。
./tests/VisualParity/parity.ps1 verify-reference --directory <compose-capture-bundle>
```

### 文件版本

公开结果与 run 索引为 **schema 8**（新增 withinTolerance 与 Raster 容差证据），用例元数据为 **schema 4**（suite/scenario 与必需的对象 config），双端采集协议为 **schema 4**。
只支持当前格式，不读取旧元数据或旧结果，没有升级、回退或 `compare` 转换入口；旧产物需要重新运行生成。
旧 `serve` / `render` 命令、HTML 模板及 .NET 报告服务器已删除，不提供兼容入口。
case manifest 包含所有权威数据和图像，排除 manifest 本身和派生的 overview.md。
报告插件校验被请求产物的 schema 和 SHA256；前端校验样本身份，Worker 校验 PNG 尺寸及其像素统计与记录的一致性。
`run.yaml` 是原子更新的导航索引。运行中只保存计数、阶段累计时间和空 cases；
每个已提交用例的摘要与阶段时间追加到 `progress.jsonl`，`summary.md` 也只追加一行。
报告读取运行中索引时合并已提交的 journal 条目；结束时一次性生成包含完整 cases 的 run.yaml。
中断的 run 保留 running 状态和已完成的用例文件，未发布的用例保存在 .work。
当前 run 目录可独立复制，用下文配置加入浏览列表，不依赖采集缓存。

## 缓存与完整性

用例数据源只在 C# 各组件测试类中维护，支持循环生成配置；发现、展开、校验和筛选后，
运行器在捕获前冻结全部选中用例的完整 JSON，双端都读取它。元数据包含 config，整体参与参考缓存身份。
公开的 `case.yaml` 保留实际配置。目录组织为：

```text
artifacts/visual-parity/
  builds/<producer>/<hash>/ # 按内容冻结的可执行构建，避免运行中重建覆盖二进制
  references/
    current.json            # 最近成功准备的快照，run 启动时固定
    snapshots/<id>.json     # 不可变参考清单，记录定义哈希、捕获位置和 manifest 哈希
    captures/<id>/<case>/   # 成功的 Compose 捕获；多个快照可以引用同一捕获
    .work/<id>/             # 准备阶段临时输入，失败时保留诊断
  runs/<run>/               # 自包含的正式结果
    plan.json               # 本轮用例、配置、runner 环境和固定参考清单
    run.yaml / progress.jsonl / summary.md / cases/
    .work/<case>/           # 本轮临时采集；异常或不稳定时保留
```

`--output` 只改变测试运行的位置；参考库和构建快照位置固定。准备阶段用一个 JVM 顺序执行，
只复用进程、场景发现和环境信息，每例仍创建独立 fixture、Host 和时钟。场景失败后终止该 JVM，
不发布不完整参考清单。普通 run 使用单进程、单 UI 线程顺序采集 Avalonia，不引入进程池或数据库。
参考身份包含 Kotlin 源码、已构建 JAR、Gradle 配置、固定基线、字体及平台；准备时也核对实际 JVM 环境。
C# 控件库的实现和 DLL 不参与 Compose 参考身份；配置按冻结的完整元数据 JSON 哈希判断。
部分准备会保留同一参考实现下其他已准备用例的引用；参考实现变化后，不沿用旧实现的条目。
直接读取固定参考，不为本轮复制内部捕获包；公开结果仍复制必要图片，保持 run 自包含。
用例结果先在本轮 .work 写出并校验，再原子发布到 cases。没有执行错误或自检不稳定时，
自动删除**本轮创建**的临时用例目录；视觉差异本身保留在正式结果中，不阻止临时清理。
失败或不稳定的用例保留临时文件，`case.yaml` 的 `diagnostics` 给出相对路径；进程中断也可能留下 `.work`。
公开结果的读取和报告浏览不依赖 `.work` 或参考缓存；报告服务不暴露 `.work`。
捕获读取检查文件集合、SHA256、身份及 PNG 头；比较时按样本解码图片并验证像素数据，不累计整轮像素。
导出使用内存中的比较事实，不反复解析刚生成的 YAML；格式 round-trip 由测试覆盖。
`verify-reference` 和公开结果独立回读仍完整解码校验。PNG 是唯一持久化像素来源。
旧 cache/reference 不再作为隐式参考来源；执行 prepare-reference 建立新快照，不自动迁移或删除旧产物。
构建快照、参考快照、捕获及历史运行由使用者管理。删除参考捕获前应检查保留快照的引用；公开 run 不依赖它们。

阶段耗时以毫秒记录在 progress.jsonl 和最终 run.yaml：referenceRead/referenceCapture、actualCapture、
comparison、export、selfTest（适用时）、total；run 另记录 startup 和执行 wall。
外层脚本构建及首次冻结构建的开销不包含在执行 wall 内。当前没有自动重试、断点续跑、分布式调度或共享结果库。

## 报告应用

默认只枚举 `artifacts/visual-parity/runs/` 的直接子目录，且必须有当前版本的 `run.yaml`。
不递归搜索缓存或旧调试目录。`--output` 指定到其他位置时，将该 run 或其父目录加入环境变量：

```powershell
$env:PARITY_REPORT_ROOTS = '["D:/parity-results", "D:/another-run"]'
./tests/VisualParity/parity.ps1 report
```

额外目录与默认目录同时显示；同名 run 由位置区分。根目录没有对应格式文件时不会作为 run 展示，
损坏索引会独立报告，其他 run 仍可访问。服务只提供 GET，不暴露 `.work`，不运行用例或处理图像。

前端开发和构建从仓库根目录执行：

```powershell
npm --prefix tests/VisualParity/report ci --no-audit --no-fund
npm --prefix tests/VisualParity/report run test
npm --prefix tests/VisualParity/report run build
npm --prefix tests/VisualParity/report run preview
```

`preview` 也启用产物插件，浏览构建后的同一个应用。`dist/` 是应用构建结果，不写入每个 run。
PNG 固定为 sRGB 8-bit straight alpha，取色通过 fast-png 直接解码，Canvas 只负责展示。
阈值和区域分析是诊断视图，不修改运行时判断；整数超出 JavaScript 安全范围时保留 BigInt，原始值不舍入。


## 验证

系统测试覆盖差分、语义冲突、缺失、协议与像素完整性，以及真实控件采集和时钟重复性。
结果格式测试还验证 YAML 数值/字符串类型及精度、双端语义保留、同时间样本的原始执行次序、
单端与失败结果、文件篡改，以及脱离原始采集包后的结果读取。
这些检查不表示组件已与 Compose 视觉一致。
