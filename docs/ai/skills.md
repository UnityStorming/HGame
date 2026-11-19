# Skill 介绍与使用

[返回文档索引](../index.html) · [AI 初始化操作](initialization.md) · [AI 工作流](workflows.md) · [MCP 介绍](mcp.md)

Skill 是可复用的任务说明包，通常以 `SKILL.md` 为入口，按需要包含参考文档、脚本和模板。它告诉 AI 什么时候使用这套方法、查询哪些知识，以及怎样完成和验证任务。

## HGame 如何组织 Skill

工程统一维护位置是 `.codex/skills/`。其中：

- `SKILL.md` 提供技能说明和文档路由。
- `references/` 或 `reference/` 存放专题规范，按任务需要读取。
- `scripts/` 存放技能需要的工具脚本。
- `agents/` 等目录提供特定客户端使用的元数据；实际结构以各技能为准。

`.agents/skills`、`.claude/skills` 和 `.cursor/skills` 通过相对软链复用统一内容。Clone 后按 [AI 初始化操作](initialization.md) 恢复链接，各客户端才能在对应入口发现共享技能。

项目技能与个人安装的全局技能可能同时出现在客户端中。可用清单以当前会话为准，不能把某台开发机的全局技能当作工程已经包含的能力。

## 常用技能

| 技能 | 适用任务 | 入口 |
| --- | --- | --- |
| `hgame-dev` | HGame 架构、模块、UI、资源、事件、热更等开发规范 | [项目开发指导](../../.codex/skills/hgame-dev/SKILL.md) |
| `luban-dev` | 游戏配置数据、表结构、校验、导表和消费链路 | [Luban 配置开发](../../.codex/skills/luban-dev/SKILL.md) |
| `comet` | 按项目配置选择工作流 | [Comet 入口](../../.codex/skills/comet/SKILL.md) |
| `comet-native` / `comet-classic` | 按指定流程组织需求、实现、验收和归档 | [AI 工作流](workflows.md) |
| `systematic-debugging` | 排查 bug、失败检查和异常表现 | [系统排障](../../.codex/skills/systematic-debugging/SKILL.md) |
| `improve-codebase-architecture` | 评估 HGame 架构及重构机会 | [架构改善](../../.codex/skills/improve-codebase-architecture/SKILL.md) |
| `grill-with-docs` | 结合项目架构文档深度审查代码变更 | [代码审查](../../.codex/skills/grill-with-docs/SKILL.md) |
| `verification-before-completion` | 在完成报告前核对实际验证证据 | [完成前验证](../../.codex/skills/verification-before-completion/SKILL.md) |

## 怎么调用

最通用的方式是在请求中明确技能名称和目标：

```text
使用 hgame-dev，核对 UIWindow 生命周期后新增一个背包弹窗。
```

```text
使用 luban-dev，检查道具表的新字段、引用关系和导表结果。
```

```text
使用 systematic-debugging 和 hgame-dev，调查界面关闭后仍触发事件回调的问题。
先定位原因，再做最小修复并验证。
```

Codex 支持技能选择入口；客户端提供对应能力时，可通过 `$hgame-dev` 等名称显式选择。斜杠命令、菜单和自动匹配行为因客户端而异，以实际技能清单为准。自然语言明确要求使用某个技能，也能表达同样的任务意图。

仅写“使用 Skill”不足以定义目标。说明需要修改的功能、问题表现、范围、期望行为和验证标准，技能才能围绕任务发挥作用。

## 与项目规则配合

`AGENTS.md` 规定 HGame 的任务等级和知识查询要求：

| 等级 | 典型任务 | 查询方式 |
| --- | --- | --- |
| L1 | 不涉及框架 API 等边界的文字和注释修改 | 可直接处理 |
| L2 | 已知 API 调用、单模块局部修改 | 用 `hgame-dev` 查询对应主题 |
| L3 | 新功能、跨文件、UI、事件或模块逻辑 | 查询全部相关主题；配置编辑先用 `luban-dev` |
| L4 | 架构、系统重构和多模块协作 | 查询架构及相关模块规范，配置链路同时查询 |

例如资源任务应查询加载 API 和持有、释放模式；UI 任务应查询生命周期和使用模式。文档与源码冲突时核实当前签名、优先信任源码，并报告冲突，不能为了套用技能示例而改坏现有接口。

## 维护与更新

团队新增和修改技能时只操作 `.codex/skills/`，不要为每个客户端复制一份。初始化与同步规则见 [工程 AI 配置](../../.codex/README.md)。

更新 Comet/OpenSpec 等生成技能前，检查安装版本、共享链接、项目定制和当前未提交改动。更新后重载客户端，确认清单与调用方式；不要只凭文件存在就判断技能已在会话中可用。

需要新技能时，先说明稳定、可重复的任务及其输入、输出和验证方式，再使用客户端可用的 `skill-creator` 等创作工具。官方介绍见 [构建 Skills](https://learn.chatgpt.com/docs/build-skills)。
