# Visual parity

采集 Compose 与 Avalonia 的真实事实，按身份和语义精确比较。
公开结果采用 **用例概览 → 单个采样点详情** 两层结构：运行时直接输出概览，
Agent 通过文件系统读取 Markdown 与 YAML，无需调用查询脚本。
系统不推断原因，不用容差、配准或文字遮罩消除差异。

## 阅读入口

| 要做什么 | 阅读位置 |
| --- | --- |
| 运行测试、查看结果、迁移旧结果 | 本文 |
| 新增双端用例、选择现有例子、了解宿主边界 | [AUTHORING.md](AUTHORING.md) |
| 理解测量定义、YAML、像素与差异状态 | [MEASUREMENTS.md](MEASUREMENTS.md) |

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
    case.yaml                # 身份、样本索引、执行错误、清理轨迹、自检结果
    environment.yaml         # 双端环境与二进制来源，每用例保存一次
    manifest.json            # 权威数据及图像的完整文件集合与哈希
    report.html              # 从这些 YAML 和像素文件生成
```

`overview.md` 每行对应一个采样点，列出 geometry、text、properties、motion 的
**different / notCompared** 数量；mixed 单列显示两侧类别冲突的字段。图像单独计数，未请求截图明确标注。
执行错误和重复性状态单独显示。计数表示观测次数，不是独立缺陷数或完成百分比。

```powershell
$caseDirectory = 'artifacts/visual-parity/<run>/cases/button.filled.press'
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
YAML 读写使用锁定的 YamlDotNet。以下命令从仓库根目录执行；首次使用先 build，
list、render、compare 和 verify-reference 不自动构建。run、self-test 自动构建双端，capture-actual 只构建 Avalonia 端。

```powershell
./tests/VisualParity/parity.ps1 build
./tests/VisualParity/parity.ps1 list
./tests/VisualParity/parity.ps1 run --case button.filled.press
./tests/VisualParity/parity.ps1 run --family button
./tests/VisualParity/parity.ps1 run
./tests/VisualParity/parity.ps1 self-test
./tests/VisualParity/parity.ps1 capture-actual --case text.wrapped
./tests/VisualParity/parity.ps1 test
```

输出默认在 `artifacts/visual-parity/<run>/`；`--output <new-directory>` 指定新目录，不覆盖已有目录。
每个用例结束即在终端打印其完整概览及绝对路径。`run` 生成结果成功返回 **0**，即使存在差异。
`--require-equal` 显式启用严格门禁：存在差异返回 **1**，只有未比较项返回 **3**；执行错误优先返回 **2**。
`capture-actual` 的参考缺失项目为 notCompared。门禁仅覆盖提交的事实，不证明测量没有遗漏。

`self-test` 另做两端重复捕获和额外被动截图检查，结果保存到 `case.yaml` 的 repeatability，
不稳定返回 2。普通 run 不自动重复捕获。自检稳定不表示组件一致。

```powershell
# 从公开结果重新生成概览和 HTML，不依赖采集缓存，也不执行两端组件。
./tests/VisualParity/parity.ps1 render --directory artifacts/visual-parity/<run>/cases/button.filled.press

# 将旧 schema 2 用例转换到一个全新目录；旧采集包保持原样。
./tests/VisualParity/parity.ps1 compare --directory artifacts/visual-parity/<old-run>/button.filled.press --output artifacts/visual-parity/converted-button

# 校验内部 Compose 采集包及当前基线。
./tests/VisualParity/parity.ps1 verify-reference --directory <compose-capture-bundle>
```

### 版本与迁移

结果文件版本为 **schema 3**，用例元数据和双端采集传输协议仍为 **schema 2**。
旧 `inspect` 已移除；读取概览和 YAML 即可替代查询。默认结果不再生成 `agent.json`、
`comparison.json`、`summary.json` 或双端 `observations.json` 副本。schema 1 不自动升级。
`render` 只更新派生的 Markdown/HTML，先验证权威文件集合、SHA256、样本身份和 PNG/RGBA 一致性。
manifest 不包含这两个可重建的展示文件。结果目录可以独立复制和重新呈现。

## 缓存与完整性

运行前冻结用例元数据。内部原始包、自检重复包和临时差分保存在
`artifacts/visual-parity-captures/<id>/<case>/`，不混入公开结果；`case.yaml` 的 sourceBundle 保留诊断路径，
但读取或重建报告不依赖它。参考缓存仍在 `artifacts/visual-parity-reference/`，命中后校验文件集合、
SHA256、身份和像素协议。失败捕获不作为成功参考缓存。
`run --refresh-reference` 可强制重新采集 Compose 参考；这不会更改固定上游基线。

## 验证

系统测试覆盖差分、语义冲突、缺失、协议与像素完整性，以及真实控件采集和时钟重复性。
结果格式测试还验证 YAML 数值/字符串类型及精度、双端语义保留、同时间样本的原始执行次序、
单端与失败结果、文件篡改，以及脱离原始采集包后的 HTML 重建。
这些检查不表示组件已与 Compose 视觉一致。
