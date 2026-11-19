# 工程 AI 配置统一实施计划

> **For agentic workers:** 使用 subagent-driven-development 执行独立的 skill 合并和脚本验证，主代理负责目录迁移与最终验收。

**Goal:** 将工程 skills 和 MCP 服务定义集中维护在 `.codex`，Claude、Cursor 和 Codex 使用相同内容。

**Architecture:** `.codex/skills` 保存 45 个去重后的技能；`.claude/skills`、`.cursor/skills`、`.agents/skills` 链接到该目录。`.codex/mcp.json` 保存可移植的 MCP 定义，根 `.mcp.json` 和 `.cursor/mcp.json` 链接到它；同步脚本更新 `.codex/config.toml` 中的 MCP 表，保留其他配置和 Codex 专用选项。

**Tech Stack:** Python、PowerShell、JSON、TOML、Windows 目录/文件符号链接。

**Spec:** 当前对话中用户批准的方案（“来搞吧”）。

## 全局约束

- 在当前工程中实施本地配置变更，保留已有 `.codex/hooks.json` 和启动场景修改。
- 同名 skills 优先新版：OpenSpec 1.12.0；合并 HGame 完整模块参考；保留 Codex 的架构技能增强和元数据。
- 保留 Unity CLI 和 Rider 两个服务；不改框架或游戏运行时代码。
- 软链初始化必须幂等，不覆盖非空的真实目录或不同内容的配置文件。
- Windows 链接不依赖 Git 克隆自动恢复；提供初始化命令和检查模式。
- 移走旧目录前，确认绝对路径属于本工程并保存可恢复的本地备份。

## 任务 1：合并技能

- [x] 从 `.agents/skills` 合入 Comet、OpenSpec 和通用流程 skills；合并 `.claude` 中更新的 HGame 文档。
- [x] 保留 `.codex` 中独有技能、Codex 元数据和架构增强；只调整迁移必要的宿主语法/路径。
- [x] 验证共 45 个技能，名称唯一，OpenSpec 生成版本为 1.12.0；检查相对文档与执行路径。

## 任务 2：实现同步与初始化

- [x] 先写行为测试：JSON/TOML 等价、保留 hooks 和专用选项、重复运行不改文件、移除服务同步生效、非法配置不写文件。
- [x] 实现 `.codex/scripts/sync_mcp.py`，提供同步与只读 `--check`。
- [x] 实现 `.codex/scripts/setup-agent-links.ps1`，默认创建链接并同步，`-Check` 只验证；拒绝覆盖未知真实内容。
- [x] 合并 Unity CLI 与 Rider 到 `.codex/mcp.json`；记录依赖与初始化命令。

## 任务 3：迁移与验收

- [x] 校验并备份旧 skill 目录，移走三处旧实体目录；建立所有链接。
- [x] 将根 `.mcp.json` 改为链接并新增 Cursor MCP 链接，同步 Codex TOML。
- [x] 运行行为测试、链接检查、MCP 同步检查；验证重复初始化幂等。
- [x] 检查 Git 差异和原有改动；能运行的客户端用只读命令检查配置，其余说明重载要求。

## 执行记录

- 决策：直接在当前工程安装本地链接，避免新 worktree 中完成后当前工程仍未生效。
- 决策：MCP 使用 JSON 作为共同服务定义源，TOML 保留 Codex 专用选项；hooks 等其他表不参与同步。

- 验收：17 项行为测试通过；五个相对软链与四处 45 个技能入口一致，MCP JSON/TOML 服务匹配。
- 验收：Codex CLI 读取到两个启用的项目 MCP；Claude 识别共享项目配置，首次连接仍需客户端原生确认；Cursor 完成文件入口验证。
- 验收：非 MCP TOML 设置和 Rider 专用选项保留，原 hooks.json 与启动场景修改哈希不变，Git 差异空白检查通过。
- 恢复：原目录及配置完整保存在 `.codex/backups/20261118-234234/`。
