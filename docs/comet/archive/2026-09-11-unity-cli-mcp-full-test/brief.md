# 目标

对 `.mcp.json` 中 `unity-cli` MCP 服务器（Unity 官方 `com.unity.pipeline` 0.6.0-exp.1，经 `unity` CLI 1.0.0-beta.9 的 `unity mcp --project-path GameUnity` 暴露）提供给 MCP 客户端的全部 149 个工具，逐一在当前已打开的 `GameUnity` 工程（Unity 6000.3.10f1，Pipeline 服务端口 7800）上实测，产出一份按分类整理的测试报告，记录每个工具的调用方式、实际返回与发现的问题；测试结束后工程与编辑器恢复到测试前状态，除报告与 Comet 产物外无残留改动。

# 范围

- 测试对象：`unity list --project-path GameUnity --json` 返回的 149 个 built-in 工具，与 MCP 客户端枚举到的 `mcp__unity-cli__*` 工具一一对应。按包文档分为 15 类（每个工具只归一类，合计 149）：编辑器生命周期与可观测性（16）、资产与文件（12）、场景（9）、GameObject 与组件（14）、Prefab（7）、脚本与编译/热重载（13，含 `recompile` / `recompile_status`）、动画与 Timeline（14）、材质与 Shader（4）、烘焙（17）、导航与选择（3）、截图（3，`capture_game_view`、`capture_scene_view`、`screenshot`）、构建与测试（12）、项目设置（18）、包管理（6）、批处理（1）。
- 每个工具至少执行一次真实调用；执行方式按 Spec「执行策略表」分为四种：真实执行、`dry_run` 预览 + 无 `confirm` 拒绝门、预期不可用（依赖未安装包或未连接 Player）、跳过（只允许由本 brief 决策明确列出的原因）。执行档位为深度档（见 D6）。
- 测试隔离：所有创建类操作只落在临时目录 `GameUnity/Assets/PipelineMcpTest/` 与 additive 打开的临时场景 `Assets/PipelineMcpTest/McpTestScene.unity`；`run_script`、`eval_file` 使用 `GameUnity/AgentScripts/` 下的临时 `.cs`（位于 `Assets/` 之外，不触发导入）；截图与 PNG 输出写到 `GameUnity/Temp/` 或临时目录；临时 Tag 名 `McpTestTag`；临时 EditMode 测试程序集 `PipelineMcpTest.Editor`（用于让 `list_tests`/`run_tests`/`test_status` 有真实测试可跑）。
- 端到端链路：`create_script` → `recompile` → `recompile_status` → `attach_script`；`write_text_file`（asmdef + 测试脚本）→ `recompile` → `list_tests` → `run_tests` → `test_status`；`batch` 的 `dry_run`、事务成功、事务失败回滚三种情形；`editor_play` → `editor_pause` → `editor_stop` 往返（约 10 秒，记录控制台错误数）。
- 场景：测试开始前先 `save_all` 把 `GameStart` 当前内存修改写盘（D7），之后临时场景以 additive 为主；`open_scene` 额外做一次非 additive 打开临时场景再非 additive 打开回 `GameStart` 的往返；`save_all` 在临时场景 dirty 时再真实执行一次。
- 交付物：测试报告 `docs/unity-cli-mcp-test-report.md`；基于实测在 hgame-dev skill 的两份镜像新增参考文档 `mcp-unity-cli.md` 并登记路由（见 D8）；报告附录追加 `unity` CLI 命令行冒烟结果（见 D9）。
- 清理：删除 `Assets/PipelineMcpTest/`（含 `.meta`）与 `GameUnity/AgentScripts/`，关闭临时场景，移除临时 Tag，`set_authoring_root` 复原为 `Assets`，被修改过的设置恢复原值。

# 非目标

- 不测试 `com.coplaydev.unity-mcp`（CoplayDev unity-mcp，hgame-dev 现有 `mcp-tools.md` 所描述的那个包）。
- 不修复 `com.unity.pipeline` 包本身的缺陷，发现的问题只记录到报告。
- 任何档位都不真实执行：`switch_build_target`、`package_add`、`package_remove`、`bake_lighting`、`clear_baked_lighting`、`set_graphics_settings`（切换渲染管线）、`set_player_settings` 的 `scriptingBackend` / `apiCompatibilityLevel`（触发域重载且不可撤销）；这些只做 `dry_run` 或无 `confirm` 拒绝门验证。
- 不连接真机 / Player：`reload_file_player_interpreter` 只验证无 Player 连接时的错误返回。
- 不修改 HGame 框架代码、热更业务代码、YooAsset 收集规则、HybridCLR / Obfuz 配置、`GameStart.unity`。
- 不安装 Timeline、AI Navigation、Project Auditor 等缺失包来"补齐"测试；对应工具按预期不可用记录。

# 验收示例

- A1: 报告文件 `docs/unity-cli-mcp-test-report.md` 存在，开头有环境段（Unity `6000.3.10f1`、`unity` CLI `1.0.0-beta.9`、`com.unity.pipeline` `0.6.0-exp.1`、测试日期、执行档位），工具结果表恰好覆盖 `unity list --project-path GameUnity --json` 返回的 149 个工具名，每个工具恰好一行，结果列取值仅限 `通过`、`失败`、`预期不可用`、`跳过` 四种。
- A2: 结果为 `失败` 或 `跳过` 的每一行都写明原因；`预期不可用` 只出现在依赖缺失包或未连接 Player 的工具上（`create_timeline`、`add_timeline_track`、`add_timeline_clip`、`get_timeline`、`bake_navmesh_surfaces`、`reload_file_player_interpreter`，以及 `audit` / `audit_status` 在返回 `unavailable` 时）；报告末尾有「发现的问题」段，逐条记录实测异常、与文档不符或可疑行为（无则写"无"）。
- A3: 报告中每个工具的执行方式与 Spec「执行策略表」一致；策略为「真实执行」的工具结果只能是 `通过` 或 `失败`，不能是 `跳过`；策略为「dry_run + 拒绝门」的工具需同时记录 `dry_run=true` 的预览返回与 `confirm=false` 时的拒绝返回。
- A4: 报告记录的端到端链路结果满足：`create_script` 后 `recompile_status` 到达 `completed`/`up_to_date` 且 `attach_script` 成功；临时测试程序集写入后 `list_tests` 数量 ≥ 1，`run_tests` 后 `test_status` 报告全部通过；`batch` 的 `dry_run` 不产生对象、事务成功批次创建的对象可被 `find_gameobjects` 找到、含故意失败操作的事务批次回滚后其对象不存在。
- A5: 测试结束后，相对测试前的 `git status --porcelain` 只包含 `docs/unity-cli-mcp-test-report.md`、`docs/comet/**`、`GameUnity/Assets/Scenes/GameStart/GameStart.unity`（仅来自测试前的 `save_all` 写盘）以及 D8 列出的 skill/路由文档；`GameUnity/Assets/PipelineMcpTest/`（及 `.meta`）与 `GameUnity/AgentScripts/` 不存在；`GameUnity/ProjectSettings/*.asset`、`GameUnity/Packages/manifest.json`、`GameUnity/Packages/packages-lock.json` 无改动；`GameStart.unity` 在测试前 `save_all` 之后的 SHA-256 与测试结束时一致（报告记录该哈希与 `git diff --stat` 结果）。
- A6: 测试结束后编辑器终态：`list_open_scenes` 只含 `Assets/Scenes/GameStart/GameStart.unity` 且 `isActive=true`、`isDirty=false`；`editor_status` 的 `playMode=stopped`、`status=ready`；`get_authoring_root` 返回 `Assets`；`get_tags_layers` 不含 `McpTestTag`；`get_build_settings` 的场景列表与测试前一致。
- A7: `.claude/skills/hgame-dev/references/mcp-unity-cli.md` 与 `.codex/skills/hgame-dev/references/mcp-unity-cli.md` 内容一致，包含「已核实来源」（包名与版本、`.mcp.json` 配置、`unity list` 数量 149）、「安全门」（`confirm` / `dry_run` 约定与永不真实执行的工具清单）、「路径与 authoring root」、「按分类的工具速查」（覆盖 15 类、149 个工具名）、「端到端链路模板」（脚本编译、测试、批处理、Play 往返）与「已知问题」（与报告「发现的问题」一致）六节；两份 `SKILL.md` 的文档路由表、根 `CLAUDE.md` 与 `AGENTS.md` 的「第一步」主题表及「Reference 路由」表都新增指向 `mcp-unity-cli.md` 的条目，且现有条目不被删除。
- A8: 报告附录记录以下六条命令的退出码与关键返回字段：`unity status --json`（含本工程条目与端口 7800）、`unity list --project-path GameUnity --json`（`count=149`）、`unity command editor_status --project-path GameUnity --json`（`status=ready`）、`unity command --help`、`unity mcp configure --list`、`unity job --help`；任一命令失败时如实记录输出。

# 约束与不变量

- 编辑器中 `GameStart` 场景当前处于 dirty（未保存）状态，且 `git status` 干净，说明该修改只存在于内存；非 additive 的 `open_scene` 会静默丢弃它，`save_all` 会把它写盘。按 D7，测试开始前先 `save_all` 写盘并记录 `git diff --stat` 与文件哈希，此后测试不得再改动 `GameStart.unity`。
- 场景类工具以 additive 方式操作临时场景为主；`save_scene` 只传临时场景路径；非 additive `open_scene` 只在 `GameStart` 已写盘、临时场景已保存的前提下做一次往返。
- `recompile`、`editor_play`、`package_resolve` 会触发域重载，后续调用前需轮询 `editor_status.status == ready` / `recompile_status`。
- 编辑器资源运行模式为 `EditorSimulateMode`（`EditorPrefs["EditorPlayMode"]=0`），Play 不需要联网热更。
- 活动构建目标为 Android；`build` 的 `dry_run` 以该目标校验。
- `editor_focus` 会把 Unity 窗口拉到前台，`clear_console` 会清空用户控制台，两者为一次性影响，可接受。
- 只改与任务直接相关的文件，不顺手重构无关代码。

# 决策

- D1（Agent，事实）：测试对象是 Unity 官方 `com.unity.pipeline@0.6.0-exp.1` 通过 `unity mcp` 暴露的 149 个工具（`unity list --json` 的 `count`），与 MCP 客户端工具列表逐一对应；`com.coplaydev.unity-mcp` 是并存的另一个 MCP 包，不在本次范围。
- D2（Agent，事实）：`com.unity.timeline`、AI Navigation、Project Auditor 未安装；工程内 `list_tests` 返回 0 个测试；`get_authoring_root` 为 `Assets`；活动构建目标 Android，已安装目标为 Android / StandaloneWindows / StandaloneWindows64。
- D3（Agent 实现选择）：临时资源统一放在 `Assets/PipelineMcpTest/`，临时场景 additive 打开；`run_script` / `eval_file` 的源文件放在 `GameUnity/AgentScripts/`；临时 Tag 为 `McpTestTag`；临时 EditMode 测试程序集为 `PipelineMcpTest.Editor`（不进入 HybridCLR 热更列表）。
- D4（Agent 实现选择）：`menu` 先列出菜单项，再执行无副作用的 `Assets/Refresh`；`set_authoring_root` 切到 `Assets/PipelineMcpTest` 后验证路径限制再复原为 `Assets`；`set_tags_layers` 以 `addTags` → `removeTags` 往返验证。
- D5（Agent 实现选择）：`import_asset` 的外部文件由 `eval` 在 `GameUnity/Temp/` 生成一张 4×4 PNG；`get_import_settings` / `set_import_settings` 针对该 PNG。
- D6（用户 2026-09-11，Q1）：执行档位为 **B 深度档**：在安全档基础上，真实执行"改后恢复原值"的项目设置 `set_audio_settings` / `set_physics_settings` / `set_quality_settings` / `set_time_settings` / `set_input_settings` / `set_build_settings` / `set_lighting_settings` / `set_navmesh_settings` / `set_runtime_pipeline_settings` / `set_player_settings`（仅 `bundleVersion`），每项先读原值、改为不同值并回读、再恢复原值，最后以 `git diff` 校验 `ProjectSettings/` 无残留；真实执行 `package_resolve`（触发域重载，之后轮询 `editor_status`）；以临时场景为活动场景真实执行 `bake_navmesh` → `navmesh_bake_status` → `clear_navmesh`、`bake_occlusion_culling` → `occlusion_bake_status` → `clear_occlusion_culling`。仍不真实执行 `build`、`switch_build_target`、`package_add` / `package_remove`、`bake_lighting` / `clear_baked_lighting`、`set_graphics_settings`、`set_player_settings` 的 `scriptingBackend` / `apiCompatibilityLevel`。

- D7（用户 2026-09-12，Q2）：**先写盘再测**。测试开始前执行 `save_all` 把 `GameStart` 当前内存修改写入磁盘（git 将出现 `GameStart.unity` 改动，是否提交由用户之后决定），并记录 `git diff --stat` 与文件 SHA-256；此后 `save_all` 与非 additive `open_scene` 均可真实执行，但测试不得再改动 `GameStart.unity`。
- D8（用户 2026-09-12，Q3）：**报告 + skill 文档**。除 `docs/unity-cli-mcp-test-report.md` 外，在 `.claude/skills/hgame-dev/references/` 与 `.codex/skills/hgame-dev/references/` 各新增一份内容一致的 `mcp-unity-cli.md`（已核实来源、安全门、路径与 authoring root、按分类的工具速查、端到端链路模板、已知问题），并在两份 `SKILL.md` 的文档路由表、根 `CLAUDE.md` 与 `AGENTS.md` 的「第一步」主题表及「Reference 路由」表登记；不修改现有 `mcp-tools.md`（它描述的是 CoplayDev unity-mcp）以外的其他 reference。
- D9（用户 2026-09-12，Q4）：**追加 CLI 冒烟**。报告附录实测 `unity status --json`、`unity list --project-path GameUnity --json`、`unity command editor_status --project-path GameUnity --json`、`unity command --help`、`unity mcp configure --list`、`unity job --help`，记录退出码与关键字段。

# 待解决问题

- （无）目标、范围、非目标、A1–A8 验收项与 D1–D9 决策已由用户于 2026-09-12 确认，进入 Build。

# 验证预期

- 报告完整性：以 `unity list --project-path GameUnity --json` 的 149 个名字为基准，逐一核对报告结果表（脚本比对，缺失或多余均为失败）。
- 工程残留：`git status --porcelain` 与目录存在性检查（`Assets/PipelineMcpTest`、`AgentScripts`）。
- 编辑器终态：`list_open_scenes`、`editor_status`、`get_authoring_root`、`get_tags_layers`、`get_build_settings` 与测试前快照比对。
- 端到端链路：报告中对应行的返回摘要与 A4 描述一致。
- skill 文档：`diff` 两份 `mcp-unity-cli.md` 无差异；`rg mcp-unity-cli` 在两份 `SKILL.md`、`CLAUDE.md`、`AGENTS.md` 中均有命中；六个章节标题存在，工具速查覆盖 149 个名字（脚本比对）。
- CLI 冒烟：六条命令逐一执行并记录退出码，`unity list` 的 `count` 为 149。
