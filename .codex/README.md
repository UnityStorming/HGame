# 工程 AI 配置

## 唯一维护位置

- Skills：`.codex/skills/`，当前共 45 个。`.claude/skills`、`.cursor/skills`、`.agents/skills` 都是指向它的相对目录软链。
- MCP 服务：`.codex/mcp.json`，包含 `unity-cli` 和 `rider`。根 `.mcp.json` 和 `.cursor/mcp.json` 是指向它的相对文件软链。
- Codex 入口：`.codex/config.toml` 的 MCP 表由同步脚本维护。其他设置及各服务的 Codex 专用选项（例如超时、启用状态）仍在 TOML 中保留。
- 各客户端的 hooks、规则和 agents 保留各自配置位置。

## 新克隆或链接恢复

Python 3.11+ 可直接使用；旧版 Python 先安装 TOML 读取依赖：

```powershell
python -m pip install -r .codex/scripts/requirements.txt
```

在工程根目录运行：

```powershell
.\.codex\scripts\setup-agent-links.ps1
```

脚本建立五个链接并同步 Codex MCP。Windows 需要启用开发者模式或在有创建符号链接权限的终端运行。Git 的 `core.symlinks=false` 可能把链接检出为目标路径文本；脚本会识别并恢复这些占位文件，不依赖 Git 自动恢复链接。

重复运行不会改写正确的链接或已同步配置。脚本会拒绝覆盖错误目标的链接、非空真实目录和不同内容的配置文件；需要人工确认并移走旧内容后再运行。

## 修改与升级

新增或修改 skill 直接操作 `.codex/skills`。统一内容使用当前宿主可用工具的表述；Codex 的 `agents/openai.yaml` 可以保留，其他宿主不依赖该文件。OpenSpec 当前采用生成版本 `1.12.0`，统一使用技能名称和 CLI 备用步骤。`.openspec-target` 保留 `codex` 标记。

Comet/OpenSpec 的安装和升级命令可能通过链接写入同一份文件。升级后应检查版本、宿主调用语法以及当前项目的定制参考，避免逐个宿主安装后覆盖共享内容。

修改 MCP 只编辑 `.codex/mcp.json`，然后运行：

```powershell
python .codex/scripts/sync_mcp.py
```

共享 JSON 支持 STDIO 的 `command`、`args`、`env`，以及 Streamable HTTP 的 `url`、`headers`；`headers` 转换为 Codex 的 `http_headers`。传统 SSE 使用 `mcp_proxy` 转 STDIO，Rider 已采用此方式。

共享字段以 JSON 为准；Codex 专用选项在 `config.toml` 中调整。删除 JSON 中的服务会同步删除对应 TOML 服务表。同步脚本先解析和校验生成结果，再原子替换 TOML，不修改 hooks 等非 MCP 设置。

## 验证

```powershell
.\.codex\scripts\setup-agent-links.ps1 -Check
python .codex/scripts/sync_mcp.py --check
python -m unittest discover -s .codex/scripts/tests -v
```

`-Check` 和 `--check` 均只读，发现漂移时返回非零退出码。配置更新后重载相应客户端；已运行的会话可能仍持有旧的技能和 MCP 清单。

首次迁移的旧目录和原始 MCP/TOML 配置保存在 `.codex/backups/` 的时间戳子目录中；该目录已忽略，不加入 Git。
