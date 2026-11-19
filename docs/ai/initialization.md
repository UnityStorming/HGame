# AI 初始化操作

[返回文档索引](../index.html) · [AI 工作流](workflows.md) · [MCP 介绍](mcp.md) · [Skill 介绍](skills.md)

本页面向首次 Clone HGame 的开发者，以 Windows PowerShell 为例，并提供 macOS 的软链恢复与修复命令。所有工程命令都在仓库根目录运行，也就是同时包含 `GameUnity`、`GameConfig` 和 `.codex` 的目录。

## 1. 准备环境

| 工具 | 用途 | 何时需要 |
| --- | --- | --- |
| AI 客户端 | 在工程内阅读、修改和验证代码 | 必需；可选择 Codex 桌面端、CLI 或当前支持项目技能的客户端 |
| Node.js 与 npm | 安装 Codex CLI、Comet、OpenSpec | 使用对应 CLI 时需要 |
| Python | 运行共享配置初始化与 MCP 同步脚本 | 使用工程初始化脚本时需要 |
| Unity Editor 与 Unity CLI | 连接并操作运行中的编辑器 | 使用 Unity MCP 时需要 |
| Rider 与 MCP Server | 提供 IDE 上下文和工具 | 使用 Rider MCP 时需要 |

如果使用 Codex CLI，可通过 npm 安装并登录：

```powershell
npm install -g @openai/codex
codex --version
codex login
```

使用桌面客户端时，在客户端登录并打开本地 HGame 仓库即可；桌面端登录与工程配置初始化是两个步骤。官方安装及账号说明见 [Codex CLI](https://learn.chatgpt.com/docs/codex/cli) 和 [Codex 认证](https://developers.openai.com/codex/auth)。

## 2. 恢复工程共享配置

工程将 Skill 统一维护在 `.codex/skills/`，将 MCP 服务统一维护在 `.codex/mcp.json`。各客户端入口通过链接和同步脚本复用它们。详细维护规则见 [工程 AI 配置](../../.codex/README.md)。

在仓库根目录执行：

```powershell
python --version

# 安装初始化脚本的依赖；Python 3.11+ 不需要额外 TOML 读取依赖。
python -m pip install -r .codex/scripts/requirements.txt

# 创建共享配置链接，并同步 Codex 的 MCP 服务表。
.\.codex\scripts\setup-agent-links.ps1
```

Windows 创建符号链接需要启用开发者模式，或在具备创建符号链接权限的终端中运行。Git 将软链检出为目标路径文本时，脚本会识别并恢复这些占位文件。

初始化脚本会建立以下入口：

| 客户端入口 | 共享目标 |
| --- | --- |
| `.agents/skills`、`.claude/skills`、`.cursor/skills` | `.codex/skills/` |
| 根目录 `.mcp.json`、`.cursor/mcp.json` | `.codex/mcp.json` |
| `.codex/config.toml` 中的 MCP 服务表 | 由 `.codex/mcp.json` 同步生成 |

如果脚本报告非空真实目录、错误链接或不同内容的配置文件冲突，先核对并备份旧内容，再按提示处理。不要直接删除现有配置。正确的链接和已同步配置可以重复检查、恢复。

### macOS：恢复和修复软链

工程提交的是标准 Git 符号链接（文件模式 `120000`），目标使用相对路径，因此可以在 macOS 上生效，正常 Clone 会自动恢复。现有 `.ps1` 初始化脚本主要供 Windows 使用；macOS 可以直接使用 Git 恢复链接。

如果链接变成普通文本文件或损坏，在 macOS 的仓库根目录执行：

```bash
git config --local core.symlinks true

git restore --source=HEAD --worktree -- \
  .claude/skills .cursor/skills .agents/skills \
  .mcp.json .cursor/mcp.json
```

这会从当前提交恢复五个链接入口，覆盖这些入口的本地变化，不修改 `.codex/skills` 和 `.codex/mcp.json` 中的共享内容。如果入口位置已有包含内容的真实目录或需要保留的配置文件，先移走备份，再执行恢复命令。

检查链接：

```bash
ls -ld .claude/skills .cursor/skills .agents/skills \
  .mcp.json .cursor/mcp.json
```

三个 Skill 入口应显示 `-> ../.codex/skills`；根 `.mcp.json` 应显示 `-> .codex/mcp.json`；`.cursor/mcp.json` 应显示 `-> ../.codex/mcp.json`。共享目标目录和文件也必须存在，否则仍是失效链接。

macOS 上使用 `python3` 同步 Codex MCP 配置：

```bash
python3 .codex/scripts/sync_mcp.py
```

Python 3.11+ 不需要额外 TOML 读取依赖；旧版 Python 先执行 `python3 -m pip install -r .codex/scripts/requirements.txt`。软链跨平台可用，Unity CLI、Python 和 `mcp_proxy` 等 MCP 依赖仍需在 Mac 上安装，并确保共享配置中的启动命令在本机可用。

## 3. 验证配置

```powershell
.\.codex\scripts\setup-agent-links.ps1 -Check
python .codex/scripts/sync_mcp.py --check
```

两项都是只读检查。成功时退出码为 0；第一项会显示 `Agent links and MCP configuration are consistent.`。失败时按提示修复对应配置或链接。

macOS 的链接检查使用上面的 `ls -ld` 命令，MCP 同步状态使用以下只读检查：

```bash
python3 .codex/scripts/sync_mcp.py --check
```

## 4. 按需要准备工作流工具

希望使用 Comet 或 OpenSpec 时，先安装对应 CLI：

```powershell
npm install -g @rpamis/comet
npm install -g @fission-ai/openspec
comet --version
openspec --version
```

HGame 已包含 `.comet/config.yaml`、工作流技能和 `docs/openspec/config.yaml`。Clone 后优先恢复工程配置并检查工具可用性；初始化、升级 Comet/OpenSpec 时可能更新共享技能，操作前先检查本地改动和团队版本要求。

## 5. 连接编辑器与 IDE

1. 用 Unity 打开 `GameUnity` 工程，等待导入和编译完成。
2. 确认 Unity CLI 位于 PATH，执行 `unity --version`。未安装时可让 AI 按当前 `unity-cli` 技能安装，或使用 [Unity 官方 CLI 安装脚本](https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1)。
3. 在 Unity 中检查 `Window/Pipeline/Settings...` 的服务状态，然后执行：

   ```powershell
   unity status --json
   unity list --project-path GameUnity --json
   ```

4. 使用 Rider MCP 时，打开 Rider 的 MCP Server 设置并确认实际服务地址。工程配置中的地址需要与本机 Rider 一致，详见 [MCP 介绍与连接](mcp.md)。

共享配置初始化成功表示入口一致；CLI、Unity Editor 和 Rider 都正常运行后，才能确认对应 MCP 服务可用。

## 6. 启动 AI，做一次只读验证

在 AI 客户端打开仓库根目录；Codex CLI 可在根目录运行 `codex`。配置变更后重载客户端或开始新会话，让它重新读取 Skill 和 MCP 清单。

可以发送以下请求：

```text
先读取 AGENTS.md，使用 hgame-dev 查询架构和模块规范。
列出本会话实际可用的 MCP 服务，检查 Unity 连接状态。
只做检查，不修改工程；报告检查结果和缺少的环境。
```

完成后按 [AI 工作流](workflows.md) 选择任务方式。浏览器阅读这些 MD 时，可按 [Markdown 阅读扩展安装指南](../markdown-viewer-setup.html) 配置排版。
