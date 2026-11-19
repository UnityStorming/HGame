# MCP 介绍与连接

[返回文档索引](../index.html) · [AI 初始化操作](initialization.md) · [AI 工作流](workflows.md) · [Skill 介绍](skills.md)

MCP（Model Context Protocol）是 AI 客户端连接外部工具和数据的协议。在 HGame 中，它让 AI 获取正在运行的 Unity Editor、Rider 等应用的上下文，并调用它们提供的工具。

## MCP、Skill 和项目规则各做什么

| 内容 | 职责 | HGame 示例 |
| --- | --- | --- |
| `AGENTS.md` | 定义工程约定、修改范围和开发红线 | 热更业务落位、资源释放和事件规范 |
| Skill | 提供特定任务的知识与操作方法 | `hgame-dev` 告诉 AI 怎样使用项目 UI 和资源 API |
| MCP | 提供应用的实际工具和实时上下文 | Unity 的场景层级查询、组件检查和编辑操作 |

例如，制作 UI 时先通过 `hgame-dev` 了解生命周期和命名，再用 Unity MCP 检查并操作 Prefab。Skill 不会自动启动 MCP 服务，MCP 连通也不代表 AI 已理解项目规范。

## 工程的配置入口

共享定义在 `.codex/mcp.json`，各客户端入口的恢复和同步方式见 [工程 AI 配置](../../.codex/README.md)。

当前配置包含：

| 服务 | 启动形式 | 环境前提 |
| --- | --- | --- |
| `unity-cli` | `unity mcp --project-path GameUnity` | Unity CLI 在 PATH，`GameUnity` 编辑器已打开，Pipeline 服务可用 |
| `rider` | `python -m mcp_proxy <Rider 的 SSE 地址>` | Python 可用，安装 `mcp-proxy`，Rider MCP Server 已启动 |

JSON 中的共享服务字段同步到 `.codex/config.toml`；Codex 专用选项仍在 TOML 中保留。修改共享配置后执行：

```powershell
python .codex/scripts/sync_mcp.py
python .codex/scripts/sync_mcp.py --check
```

然后重载 AI 客户端，检查本会话实际显示的工具清单。配置文件中有服务条目，只表示配置了它，不代表服务已经连接。

## Unity MCP 的使用

HGame 使用 Unity 官方 `unity-cli` 与 `com.unity.pipeline`。先用 Unity 打开 `GameUnity`，等待编译完成，在 `Window/Pipeline/Settings...` 中检查服务状态。

根目录中的只读检查：

```powershell
unity --version
unity status --json
unity list --project-path GameUnity --json
```

`status` 用于查看在线编辑器实例，`list` 用于获取目标工程实际可用的工具。工具名称和数量以本机输出为准。

常见用途包括场景层级查询、组件属性检查、Prefab 和资源操作、脚本编译、控制台诊断及画面采集。写操作可能影响场景、资产或项目设置，执行方式必须遵循 [Unity CLI MCP 工具指南](../../.codex/skills/hgame-dev/references/mcp-unity-cli.md) 的路径、预览和确认规则。

HGame 已移除 CoplayDev `unity-mcp`，不要沿用其 `manage_*` 工具示例。材质、纹理、动画等操作参阅 [MCP 材质与视觉操作](../../.codex/skills/hgame-dev/references/mcp-visual.md)，历史实测参阅 [工具验证报告](../unity-cli-mcp-test-report.md)。

## Rider MCP 的使用

工程通过 `mcp_proxy` 将 Rider 的 SSE 服务转换为 STDIO，依赖安装命令为：

```powershell
python -m pip install mcp-proxy
```

在 Rider 设置中搜索 **MCP Server**，启用当前版本提供的 MCP 服务，并复制它显示的地址。把 `.codex/mcp.json` 中 `rider` 服务的代理地址与它对齐，再运行同步脚本并重载客户端。

当前仓库配置使用 `http://localhost:64342/sse`；这个端口是配置值，不保证适用于每台电脑。以本机 Rider 显示的实际地址为准。Rider 可提供的具体工具取决于 IDE 版本和当前连接状态。

## 只读检查示例

向 AI 发送：

```text
使用 hgame-dev 的 MCP 指南检查当前 Unity 工程连接。
列出当前会话实际可用的 Unity 和 Rider MCP 工具；
读取编辑器状态和主启动场景信息，不修改资产或场景。
说明服务已连接、未启动还是缺少依赖。
```

## 常见连接问题

| 表现 | 优先检查 |
| --- | --- |
| 找不到 `unity` | CLI 安装、PATH；安装后重新打开终端和 AI 客户端 |
| Unity 服务不可用 | 编辑器是否打开目标工程、编译是否完成、Pipeline 是否启动；Safe Mode 中先解决编译问题 |
| 连接到了其他 Unity 工程 | 工作目录和 `--project-path GameUnity` 是否正确 |
| 找不到 `mcp_proxy` | 当前 `python` 对应的环境是否安装 `mcp-proxy` |
| Rider 连接失败 | Rider MCP Server 是否启动，端口和 SSE 地址是否与配置一致 |
| 配置已改但工具清单没变 | 运行同步检查，重载客户端或开始新会话 |

有关协议和 Codex 集成的官方说明见 [MCP](https://learn.chatgpt.com/docs/extend/mcp)。
