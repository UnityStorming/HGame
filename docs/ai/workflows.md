# AI 工作流

[返回文档索引](../index.html) · [AI 初始化操作](initialization.md) · [MCP 介绍](mcp.md) · [Skill 介绍](skills.md)

HGame 支持普通 AI 任务，以及由 Comet 或 OpenSpec 保存需求和进度的工作流。先选择与你的任务规模和交付方式匹配的入口。

## 选择入口

| 方式 | 适用场景 | 发给 AI 的示例 |
| --- | --- | --- |
| 普通任务 | 文档、局部修改、只读调查、问题解释 | `使用 hgame-dev 解释资源加载与释放方式。` |
| Comet 默认入口 | 希望工作流按项目配置组织需求、实现和验收 | `使用 comet，为背包界面增加筛选功能。` |
| Comet Native | 希望围绕验收目标持续实现、验证，并保存恢复状态 | `使用 comet-native 实现背包筛选功能。` |
| Comet Classic | 希望经过提案、技术设计、任务计划和 OpenSpec 归档 | `使用 comet-classic 设计并实现背包筛选功能。` |
| OpenSpec | 希望直接创建或执行规格变更 | `使用 openspec-propose，为背包筛选功能创建提案。` |

客户端支持斜杠技能入口时，也可输入 `/comet`、`/comet-native` 或 `/comet-classic`。这些是发给 AI 的技能入口，不是在 PowerShell 中运行的命令。具体语法以客户端提供的技能列表为准。

## 普通任务怎么推进

1. **说明目标和范围**：提供问题表现、相关模块、期望行为、限制和验收方式。
2. **查询规范和现有实现**：AI 按 `AGENTS.md` 判断任务等级，使用 `hgame-dev`；编辑游戏配置时优先使用 `luban-dev`。
3. **实施必要修改**：按项目分层落位，复用现有模块，保留用户已有改动。
4. **验证并报告**：运行适用检查，说明修改内容、结果和无法验证的限制。

请求示例：

```text
使用 hgame-dev，为背包界面增加按品质筛选。
沿用现有 UI 和模块结构，仅修改背包相关文件。
验收：切换品质后列表正确刷新；关闭并重开界面后行为符合现有设计。
先核对现有实现和 API，再给出方案并完成修改、验证。
```

如果仓库中已有活跃 Comet change，环境恢复探针会判断当前请求是否属于它。无关任务不会仅因为存在活跃 change 就被并入；流程中对方案的回复会继续当前 change。

## Comet Native

当前 [项目配置](../../.comet/config.yaml) 的 `default_workflow` 为 `native`，同时启用 Native 和 Classic，产物语言为中文。`/comet` 按项目配置选择入口；显式请求 Classic 时使用 Classic。

Native 的核心阶段：

| 阶段 | 主要工作 | 用户关注点 |
| --- | --- | --- |
| Shape | 澄清需求、定义目标规格与验收场景 | 范围、行为和验收标准是否正确 |
| Build | 实现候选、完成开发检查和代码复核 | 实现是否覆盖确认的目标 |
| Verify | 按验收场景检查；需要修复时回到 Build | 哪些场景通过、失败或暂时无法验证 |
| Archive | 使用已接受的验收结果归档并收尾 | 工作区、分支和交付方式是否符合授权 |

Native 由 Skill 和 Runtime 推进，AI 读取 Runtime 给出的下一步。用户通常只需说明需求、回答澄清问题、审阅方案和验收结果；不需要手工编辑工作流状态或执行内部阶段命令。

详细规则见 [Comet Native](../../.codex/skills/comet-native/SKILL.md)。实现检查和验收判断分开；缺少独立验收能力或外部信息时，AI 会说明限制并按流程处理。

## Comet Classic

Classic 将 OpenSpec 的规格管理与设计、计划、执行流程结合起来：

| 阶段 | 内容 |
| --- | --- |
| Open | 建立 change、提案和需求产物 |
| Design | 技术设计、方案比较和实现计划 |
| Build | 按计划实施并更新任务进度 |
| Verify | 核对实现与规格、记录验证证据，必要时修复 |
| Archive | 合并增量规格、归档 change 并完成分支收尾 |

当前工程采用 `docs` 产物布局。Classic 中的 OpenSpec 命令由 Comet adapter 解析目录，AI 不应套用其他工程的固定路径或手工改状态文件。详细规则见 [Comet Classic](../../.codex/skills/comet-classic/SKILL.md)。

## 直接使用 OpenSpec

常用技能对应以下目的：

| 技能 | 目的 |
| --- | --- |
| [openspec-explore](../../.codex/skills/openspec-explore/SKILL.md) | 调查问题、讨论方案、澄清需求 |
| [openspec-propose](../../.codex/skills/openspec-propose/SKILL.md) | 创建提案、设计、规格和任务等实施产物 |
| [openspec-apply-change](../../.codex/skills/openspec-apply-change/SKILL.md) | 实施已有 change 的任务 |
| [openspec-archive-change](../../.codex/skills/openspec-archive-change/SKILL.md) | 归档已完成的 change |

工程技能会根据项目布局选择产物目录；已有 Comet Classic change 应继续经 Classic 入口推进，避免混用独立 OpenSpec 状态操作。

## 继续任务与交付

继续已有变更时，明确名称和意图，例如“继续 `bag-filter`，按已确认方案实施”。需求发生变化时说明新的行为和验收标准，让 AI 更新相应产物。

工作流归档、Git 提交、Push、合并和创建 PR 是不同操作。需要远程交付时明确要求，例如“验证后提交并 Push 当前任务改动”。AI 完成报告应列出实际检查结果和限制，不能把未运行的检查写成通过。
