# 目标

仓库已在提交 `7600c17e 暂时移除CoPlay MCP` 移除 `com.coplaydev.unity-mcp`。把所有面向 AI 的指引文件——根 `CLAUDE.md`、`AGENTS.md`，以及 hgame-dev skill 的 `.claude` / `.codex` 两份镜像（`SKILL.md`、`references/`、`evals/evals.json`）——更新为只以 Unity 官方 `unity-cli` MCP（`com.unity.pipeline`，`.mcp.json` 唯一服务器）为准：删除或改写 CoplayDev 专属内容，把其中与具体 MCP 无关的 HGame 约定保留到 `mcp-unity-cli.md`；同时删除用户级全局 skill `~/.claude/skills/unity-mcp-skill`。

# 范围

- **路由与触发词**：`CLAUDE.md` 与 `AGENTS.md` 的「第一步」主题表「MCP 工具链」行和「Reference 路由」表不再引用 `mcp-tools.md`，改为 `mcp-unity-cli.md`（场景 / GameObject / Prefab / 脚本 / 编辑器 / 烘焙 / 设置 / batch）与 `mcp-visual.md`（材质 / Shader / 纹理 / 动画 / VFX）；两份 hgame-dev `SKILL.md` 的文档路由表同步，frontmatter `description` 触发词去掉 `CoplayDev unity-mcp`，加入 `unity-cli`、`com.unity.pipeline`。
- **移除说明**：`CLAUDE.md` 与 `AGENTS.md` 的「MCP 工具链」行内加一句"CoplayDev unity-mcp 已于 7600c17e 移除，不要使用 `manage_*` 系列工具，MCP 操作统一走 `.mcp.json` 的 `unity-cli`"。
- **`mcp-tools.md`（两份镜像）**：先把与具体 MCP 无关的 HGame 约定迁入 `mcp-unity-cli.md`，再删除文件。迁移内容：HGame 路径表（UI 脚本 / 模块 / 事件接口 / Luban 生成代码 / 热更资源根 / UI Prefab / 图集 / 场景 / 材质-Shader-特效目录）、"工具接收 `Assets/` 相对路径、热更资源根是 `Assets/BundleAssets/`"、uGUI Prefab 流程（改为 `create_gameobject` + `add_component` + `set_component_properties` + `create_prefab` 到 `Assets/BundleAssets/UI/<Name>.prefab`，节点命名遵守 `naming-rules.md`，根节点 Canvas / CanvasScaler / GraphicRaycaster 结构，CanvasScaler 推荐值 ScaleWithScreenSize / 1920×1080 / MatchWidthOrHeight / 0.5，锚点规则）、YooAsset 地址与收集规则提醒、`menu` 常用菜单项（UI 脚本生成、Luban 转表、HybridCLR 生成、保存项目）、常见错误表（`Assets/GameScripts`、`Assets/AssetRaw`、直接改 `GameProto`、重名对象直接修改）。
- **`mcp-visual.md`（两份镜像）**：整篇改写为 unity-cli 版本，保留"材质 / Shader / 纹理 / 动画 / 摄像机 / 图形 / 粒子-VFX / 模型与图片导入"的定位与 HGame 目录约定：材质用 `create_asset`（`UnityEngine.Material` + `shader`）、`set_material_properties`、`get_material_properties`、`list_shaders`、`get_shader_properties`，并写明 URP 材质 `renderQueue` / 关键字被 URP 后处理重置的实测结论；Shader 文件用 `write_text_file` 写 `.shader`；纹理 / Sprite / 模型用 `import_asset` + `get_import_settings` / `set_import_settings`；动画用 `create_animation_clip` / `set_animation_curve` / `get_animation_clip` / `remove_animation_curve` / `create_animator_controller` / `add_animator_parameter` / `add_animator_layer` / `add_animator_state` / `add_animator_transition` / `get_animator_controller`，Animator 组件用 `add_component` + `set_serialized_field`；摄像机用 `set_component_properties`（URP 相机堆叠见 `architecture.md`）；图形设置只读（`get_graphics_settings`，`set_graphics_settings` 在 HGame 禁用）；粒子 / LineRenderer / TrailRenderer 用 `add_component` + `set_component_properties`，VFX Graph 与 Timeline 未安装照实标注；批量用 `batch`；常见错误表改为 unity-cli 参数名。
- **`evals/evals.json`（两份镜像）**：id 11 `mcp-create-ui-script`、12 `mcp-create-material`、13 `mcp-animation-workflow`、18 `mcp-ui-vs-ugui-boundary` 改写为 unity-cli 等价用例（prompt 不再提 CoplayDev；`expected_patterns` 只引用 unity-cli 工具名与 HGame 路径；`forbidden_patterns` 加入 `manage_`），其余 15 个用例不动。
- **`mcp-unity-cli.md`（两份镜像）**：「已核实来源」改为"CoplayDev unity-mcp 已于 7600c17e 移除，`mcp-tools.md` 已删除，本文档与 `mcp-visual.md` 是 HGame 唯一的 MCP 指南"；关联文档行去掉 `mcp-tools.md`；新增「HGame 落位与 uGUI Prefab 流程」「常用菜单项」「常见错误」三节承接迁移内容。
- **归档规格**：本 change 以 replace 更新 `unity-cli-mcp-test-report` 规格，「hgame-dev 参考文档」第 1 条不再描述与 CoplayDev 的对比，改为记录已移除。
- **全局 skill**：删除 `C:\Users\BingLau\.claude\skills\unity-mcp-skill\`（仓库外，用户决定 D5）。

# 非目标

- 不修改历史测试报告 `docs/unity-cli-mcp-test-report.md` 与归档目录 `docs/comet/archive/**`。
- 不修改 `.mcp.json`（已只含 `unity-cli`）、`.claude/settings*.json`、`~/.claude.json`（均无 CoplayDev 残留）。
- 不修改生成文件 `.understand-anything/knowledge-graph.json`。
- 不修改 Unity 工程内容（`manifest.json` / `packages-lock.json` 已由用户提交）。
- 不对 `unity-cli` 工具做新的实测；改写内容只依据已归档报告与 `mcp-unity-cli.md` 的既有实测结论，不发明未列入 149 个工具的命令。
- 不同步两份 `SKILL.md` 既有的非 MCP 差异行（`.claude` 版多出的 `module-system.md` 行保持原样）。

# 验收示例

- A1: 在 worktree 中执行 `rg -n -i --hidden "coplay|unity-mcp|McpForUnity|manage_(gameobject|script|asset|scene|components|prefabs|editor|ui|material|shader|texture|animation|camera|graphics|vfx|scriptable_object)|batch_execute|execute_menu_item|unity_reflect|get_test_job|refresh_unity|execute_code"`（排除 `.git/`、`.comet/`、`.codegraph/`、`.understand-anything/`、`.worktrees/`、`GameUnity/Library|Temp/`、`docs/comet/archive/`、`docs/comet/changes/`、`docs/unity-cli-mcp-test-report.md`），命中只允许是：`CLAUDE.md`、`AGENTS.md` 各 1 行「MCP 工具链」移除说明；两份 `mcp-unity-cli.md` 各 1 行「已核实来源」移除说明；本 change 的 replace 规格 `docs/comet/specs/unity-cli-mcp-test-report/spec.md`（归档后）或其它文件 0 命中；两份 `evals.json` 中只在 `forbidden_patterns` 出现 `manage_`。
- A2: `.claude/skills/hgame-dev/references/mcp-tools.md` 与 `.codex/skills/hgame-dev/references/mcp-tools.md` 不存在；两份 `mcp-visual.md` 内容一致，其「工具速查」表首列的每个反引号工具名都属于 `unity list --project-path GameUnity --json` 返回的 149 个工具，且文中含 URP 材质 `renderQueue` / 关键字被重置的说明与 "VFX Graph / Timeline 未安装" 的标注。
- A3: 两份 `mcp-unity-cli.md` 内容一致，含二级标题「HGame 落位与 uGUI Prefab 流程」「常用菜单项」「常见错误」；「HGame 落位与 uGUI Prefab 流程」含路径表（至少 `Assets/Scripts/HotFix/GameLogic/UI/`、`Assets/Scripts/HotFix/GameProto/`、`Assets/BundleAssets/UI/`、`Assets/BundleAssets/UIRaw/`）、CanvasScaler 推荐值（`ScaleWithScreenSize`、`1920`、`1080`、`MatchWidthOrHeight`、`0.5`）、`create_prefab` 到 `Assets/BundleAssets/UI/` 的步骤与对 `naming-rules.md` 的引用；「已核实来源」含 `7600c17e` 与 "`mcp-tools.md` 已删除"。
- A4: 两份 `SKILL.md` 的文档路由表都不含 `mcp-tools.md`，都含指向 `mcp-unity-cli.md` 与 `mcp-visual.md` 的行；两份 frontmatter `description` 都不含 `CoplayDev`，都含 `unity-cli` 与 `com.unity.pipeline`。
- A5: `CLAUDE.md` 与 `AGENTS.md`：「MCP 工具链」行与「Reference 路由」表不含 `mcp-tools.md`，含 `mcp-unity-cli.md` 与 `mcp-visual.md`；「MCP 工具链」行含 `7600c17e` 与 `manage_*`（移除说明）。
- A6: 两份 `evals.json` 内容一致、可被 `json.load` 解析、`evals` 仍为 19 项且 `skill_name` 不变；id 11/12/13/18 的 `prompt` 与 `description` 不含 `CoplayDev`，`expected_patterns` 中出现的工具名均属于 149 个 unity-cli 工具（id 11 含 `write_text_file` 或 `create_script` 与 `Assets/Scripts/HotFix/GameLogic/UI`；id 12 含 `create_asset`、`set_material_properties`、`Assets/BundleAssets/Materials`、`Universal Render Pipeline/Lit`；id 13 含 `create_animator_controller`、`add_animator_parameter`、`add_animator_state`、`add_animator_transition`；id 18 含 `Assets/BundleAssets/UI` 与 `UIWindow`），四个用例的 `forbidden_patterns` 都含 `manage_`；其余 15 个用例与 `7600c17e` 版本逐字相同。
- A7: 本 change 目录 `specs/unity-cli-mcp-test-report/spec.md` 存在（replace），其「hgame-dev 参考文档」第 1 条不含 `com.coplaydev.unity-mcp` 对比描述而含"已移除"；其余内容与 `docs/comet/specs/unity-cli-mcp-test-report/spec.md` 逐字相同。
- A8: `C:\Users\BingLau\.claude\skills\unity-mcp-skill` 目录不存在。
- A9: worktree 中 `git status --porcelain --untracked-files=all` 只包含：`CLAUDE.md`、`AGENTS.md`、两份 `SKILL.md`、两份 `mcp-unity-cli.md`、两份 `mcp-visual.md`、两份被删除的 `mcp-tools.md`、两份 `evals.json`、`docs/comet/changes/remove-coplay-mcp-guidance/**`；`.mcp.json`、`docs/unity-cli-mcp-test-report.md`、`docs/comet/archive/**`、`docs/comet/specs/**`、`GameUnity/**` 无改动；所有改动文本文件 UTF-8 且不含 U+FFFD。

# 约束与不变量

- hgame-dev 的每个被改文件在 `.claude` 与 `.codex` 镜像中逐字一致（`SKILL.md` 只要求 MCP 相关行一致）。
- 改写只使用 149 个 unity-cli 工具名与已归档报告 / `mcp-unity-cli.md` 的实测结论；参数名以 `unity list` 的 `parameters[].name` 为准。
- 文档为中文、UTF-8、LF；不引入 U+FFFD。
- 只改与任务直接相关的文件，不顺手重构无关文档。

# 决策

- D1（Agent，事实）：`com.coplaydev.unity-mcp` 已由用户在 `7600c17e` 从 `GameUnity/Packages/manifest.json` 与 `packages-lock.json` 移除；`.mcp.json` 只含 `unity-cli`；`.claude/settings.local.json`、`~/.claude.json` 无 CoplayDev 服务器或权限残留；`GameUnity/` 内无残留目录。
- D2（Agent，事实）：CoplayDev 专属内容分布：`mcp-tools.md`（整篇，62 处）、`mcp-visual.md`（整篇，8 处工具名 + 全部示例）、`evals.json` 4 个用例（id 11/12/13/18）、`SKILL.md` 触发词 1 处、`CLAUDE.md` / `AGENTS.md` 各 2 行、`mcp-unity-cli.md` 2 处、归档规格 `unity-cli-mcp-test-report` 1 处；`docs/unity-cli-mcp-test-report.md` 无提及。`unity-mcp-skill` 位于用户目录 `~/.claude/skills/unity-mcp-skill`（仓库外）。
- D3（用户 2026-09-14，Q1）：worktree 隔离，分支 `comet/remove-coplay-mcp-guidance`，目标分支 `Unity6000.3`，目录 `.worktrees/remove-coplay-mcp-guidance`。
- D4（用户 2026-09-14，Q2）：改写策略——`mcp-tools.md` 的 HGame 通用内容迁入 `mcp-unity-cli.md` 后删除；`mcp-visual.md` 与 4 个 eval 用例改写为 unity-cli 版本。
- D5（用户 2026-09-14，Q3）：删除全局 skill `~/.claude/skills/unity-mcp-skill`。
- D6（Agent 实现选择）：CoplayDev 移除说明放在 `CLAUDE.md` / `AGENTS.md`「第一步」主题表「MCP 工具链」行内，一句话，不新增章节。
- D7（Agent 实现选择）：`mcp-visual.md` 保留文件名与定位；VFX Graph、Timeline、AI Navigation、Project Auditor 未安装的事实照实标注，不安装包。
- D8（Agent 实现选择）：归档规格 `unity-cli-mcp-test-report` 以 replace 方式在本 change 更新，只改「hgame-dev 参考文档」第 1 条。

# 待解决问题

- （无）目标、范围、非目标、A1–A9 验收项与 D1–D8 决策已由用户于 2026-09-14 确认，进入 Build。

# 验证预期

- 残留扫描：A1 的 `rg` 命令，命中集合与允许集合逐行比对。
- 文件与镜像：`ls` 不存在性、`diff` 两份镜像、`rg` 标题与关键字。
- 工具名核对：脚本提取 `mcp-visual.md` 工具表首列与 `evals.json` `expected_patterns` 中的工具名，与 `unity list --project-path GameUnity --json` 的 149 个名字集合比对（在主工程目录运行 `unity list`，Unity 编辑器需在线）。
- JSON：`json.load` 两份 `evals.json`，比对未改用例与 `git show 7600c17e:...` 版本。
- 全局 skill：`test ! -d ~/.claude/skills/unity-mcp-skill`。
- 范围：worktree `git status --porcelain --untracked-files=all` 与 U+FFFD 扫描。
