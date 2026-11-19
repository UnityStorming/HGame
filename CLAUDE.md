# CLAUDE.md

请使用中文写提案和回答。
这个文件为 Claude Code (claude.ai/code) 提供指导，用于处理此代码库中的代码。

主启动场景位于 `GameUnity/Assets/Scenes/GameStart/GameStart.unity`。

---

## ⚡ 强制工作流

### 第零步：判断任务等级

| 等级 | 判断标准 | 知识查询策略 |
|------|----------|--------------|
| **L1 简单** | typo、注释、日志文案、单行变量改名；前提是不涉及框架 API、UI 节点前缀、事件定义、资源路径、Luban 配置 | 可跳过 skill，直接处理 |
| **L2 调用** | 调用已知 API、单模块局部修改 | 使用 `hgame-dev` skill 查询对应主题 |
| **L3 功能** | 新功能、跨文件修改、新增 UI / 资源 / 事件 / 模块逻辑 | 使用 `hgame-dev` 查询全量相关主题；涉及配置先用 `luban-dev` |
| **L4 架构** | 模块设计、系统重构、多模块协作、架构决策 | 使用 `hgame-dev` 并按需并行查询多个 reference；配置链路同时使用 `luban-dev` |

不确定时上调一级。凡涉及 HGame API 名称、程序集边界、资源地址、UI 节点前缀、事件接口、热更流程、Luban 表结构，都不要按 L1 处理。

### 第一步：按主题获取规范

同一会话中已查过的主题可复用摘要；只有涉及新主题或发现文档与源码冲突时才重新查询。配置表相关任务优先 `luban-dev`，业务落位和运行时代码再补 `hgame-dev`。

| 场景 | 必须查询主题 |
|------|--------------|
| 文件落位 / 程序集边界 | `architecture.md` |
| UI 生命周期 / 窗口 / Widget / IUIController | `ui-lifecycle.md`、`ui-patterns.md` |
| 资源加载 / 释放 / 场景切换资源整理 | `resource-api.md`、`resource-patterns.md` |
| 热更资源包 / YooAsset 下载链路 | `hotpatch-workflow.md` |
| 整包 / AB 打包 / Jenkins 自动化 / ReleaseTools | `build-pipeline.md` |
| 热更代码 / HybridCLR / AOT 泛型 | `hotfix-workflow.md` |
| 模块 API / DataCenterModule / MemoryPool / Audio / Fsm | `modules.md` |
| 新增模块 / ModuleSystem / SingletonSystem / 模块热插拔 | `module-system.md` |
| 事件系统 / EventCenter / 事件反模式 | `event-system.md`、`event-antipatterns.md` |
| Luban 配置消费 | `luban-config.md`；编辑配置表时使用 `luban-dev` |
| 红点系统 | `reddot-system.md` |
| 命名 / UI 节点前缀 / 代码规范 | `naming-rules.md` |
| MCP 工具链 | `mcp-unity-cli.md`（Unity 官方 `unity-cli` / com.unity.pipeline：场景 / GameObject / Prefab / 脚本 / 编辑器 / 烘焙 / 设置 / batch）、`mcp-visual.md`（材质 / Shader / 纹理 / 动画 / VFX）。CoplayDev unity-mcp 已于提交 7600c17e 移除，不要使用 `manage_*` 系列工具，MCP 操作统一走 `.mcp.json` 的 `unity-cli` |
| 排障 | `troubleshooting.md` |

### 第二步：输出代码或方案

基于已查询规范实现。若 reference 与源码冲突：

1. 用 `rg` 搜索实际签名和调用点。
2. 优先信任当前源码。
3. 在回复中标注冲突点；如果任务本身是维护 skill，直接修正 `.claude/skills/hgame-dev/references/` 对应文档。

---

## HGame 开发指导

处理 HGame 代码时优先使用 `hgame-dev` skill。它参考 TEngine 的 `tengine-dev` 组织方式，但内容按 HGame 当前项目结构、API 和封装编写。

**知识源**：`.claude/skills/hgame-dev/references/`

### 使用方式

```
使用 skill = "hgame-dev"
描述需要查询的技术问题或功能点
```

### 专用 skill 优先级

- Luban 配置表、Excel、`__tables__.xlsx` / `__beans__.xlsx` / `__enums__.xlsx`、导表脚本、`GameConfig/` 数据：优先使用 `luban-dev`。
- 代码落位、程序集边界、HotFix/Runtime/AOT 分层、资源模块生命周期、UI/事件/模块架构：使用 `hgame-dev`。
- 同时涉及配置和业务代码时，先用 `luban-dev` 处理配置链路，再用 `hgame-dev` 处理业务落位与调用方式。

---

## 核心红线

1. **分层落位**：框架运行时放 `GameUnity/Assets/HGame/Runtime`；编辑器工具放 `GameUnity/Assets/HGame/Editor`；热更业务放 `GameUnity/Assets/Scripts/HotFix/GameLogic`；配置生成代码放 `GameUnity/Assets/Scripts/HotFix/GameProto`。
2. **优先复用 HGame 封装**：新增服务前先确认 `GameModule`、`HGame/Runtime/Module`、`GameLogic/Module` 是否已有封装。
3. **模块访问**：业务层通过 `GameLogic.GameModule.XXX` 访问模块，不散落 `ModuleSystem.GetModule<T>()`。
4. **异步优先**：IO、资源、场景等耗时操作优先使用 `UniTask`，不要新增 Coroutine 工作流。
5. **资源必须释放**：普通资源加载与 `UnloadAsset` 成对；实例化 GameObject 由实例销毁触发资源引用回收。
6. **事件解耦**：UI 内监听用 `AddUIEvent`；跨模块事件用 `GameEvent` / `[EventInterface(EEventGroup...)]`。

---

## Reference 路由

| 场景 | 文档 |
|------|------|
| 项目结构 / 启动 / 分层 | `architecture.md` |
| 模块访问 | `modules.md` |
| 模块系统架构 / 新增模块 | `module-system.md` |
| UI 开发 | `ui-lifecycle.md`、`ui-patterns.md` |
| 事件系统 | `event-system.md`、`event-antipatterns.md` |
| 资源加载 | `resource-api.md`、`resource-patterns.md` |
| 热更代码 | `hotfix-workflow.md` |
| 热更资源包 | `hotpatch-workflow.md` |
| 构建打包 / Jenkins | `build-pipeline.md` |
| Luban 配置消费 | `luban-config.md` |
| 红点系统 | `reddot-system.md` |
| 代码规范 | `naming-rules.md` |
| 问题排查 | `troubleshooting.md` |
| MCP Unity CLI 工具（com.unity.pipeline：资产 / 场景 / Prefab / 脚本 / 热重载 / 烘焙 / 设置 / batch） | `mcp-unity-cli.md` |
| MCP 材质 / Shader / 纹理 / 动画 / VFX | `mcp-visual.md` |

---

## 冲突处理

当 reference 与代码实际 API 冲突时：

1. 使用 `rg` 搜索实际方法签名和调用点。
2. 优先信任当前源码。
3. 在回复中标注冲突点，方便后续修正文档。

---

## 自我优化机制

满足任一条件时，需要把问题反馈到本次回答；如果用户任务就是维护 skill，则直接修正对应 reference：

1. reference 文档与当前源码 API、路径、签名不一致。
2. 生成代码失败的根因来自过期文档或错误模式。
3. 用户明确指出 HGame skill、`AGENTS.md` 或 `CLAUDE.md` 描述不准确。

记录或修正文档时写清楚：

- 问题现象：错误 API、错误路径或失败表现。
- 文档位置：哪个 reference 或根文档的哪一节。
- 正确事实：经源码核实后的 API、路径、签名。
- 修正建议：文档应改成什么。

---

## HGame 补充准则

- 只改和任务直接相关的文件。
- 不顺手重构无关代码。
- 不回滚用户已有改动。
- 生成或修改 UI、资源、事件、配置消费代码前，先确认 HGame 当前 API。
- 能验证就验证；不能验证时说明限制。

---

# 通用编码准则

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

<comet-ambient-resume>
<!-- Managed by Comet. Edits inside this block may be replaced by comet init/update. -->
<!-- Contract: comet.resume_probe.v2 -->

## Comet Ambient Resume

在这个仓库中，开始处理需要改动或调查的任务前，如果可能存在活跃 Comet workflow，把当前用户请求传入只读探针：`comet resume-probe . --stdin --json`。

- 如果用户通过宿主明确调用任意 Comet Skill（例如 `@comet`、`/comet`、`@comet-native` 或 `/comet-hotfix`），显式调用优先于本恢复协议；不要运行 resume probe，直接进入被调用的 Skill。
- 如果用户通过宿主明确调用的是非 Comet 的 Skill 或斜杠命令，任务意图已由该调用明确：不要运行 resume probe，直接执行该 Skill。
- 如果你正在 Comet 流程内（包括正在等待用户回复你在流程中提出的问题），不要运行 resume probe；把这类回复（例如方案/选项选择）当作当前 change 的继续，直接按用户的选择推进。
- 只信任返回的 `workflow`、`skill` 和 `entrySource`；它们只由项目配置或无配置兼容回退决定。不得扫描或切换另一套 workflow。
- 如果 probe 返回 `auto_resume`，简短说明选中的 active change，并进入 `nextCommand` 指向的永久入口。不要把状态命令当作恢复入口直接推进。
- 如果 probe 返回 `ask_user`，只问一个简短问题并等待用户回复。
- 如果当前请求未明确调用 Comet Skill，且 probe 返回 `out_of_scope` 或 `none`，不要进入 Comet workflow。
- `out_of_scope` 或 `none` 只表示不要因为这个新请求进入 Comet workflow；它绝不表示要暂停或退出一个已在进行的 Comet 流程。
- 如果配置或状态无效且没有 `nextCommand`，停止并报告原因；不要猜测另一个 workflow。
- 不能只因为存在 active change 就把无关任务挂到该 change。Native 的未提交改动由 Native 入口检查，不由探针自动归因。
</comet-ambient-resume>
