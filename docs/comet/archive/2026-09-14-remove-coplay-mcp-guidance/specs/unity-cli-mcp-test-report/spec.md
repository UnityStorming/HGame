# Unity CLI MCP 工具验证报告

## 概述

`docs/unity-cli-mcp-test-report.md` 是对 `.mcp.json` 中 `unity-cli` MCP 服务器（`com.unity.pipeline` 0.6.0-exp.1，经 `unity` CLI 1.0.0-beta.9 的 `unity mcp --project-path GameUnity` 暴露）全部 149 个工具在 `GameUnity` 工程（Unity 6000.3.10f1）上的实测记录。它回答三个问题：每个工具能否按文档工作、失败或不可用的原因是什么、实测中发现了哪些问题。报告是一次性的验证产物，不是持续维护的 API 文档。

## 测试对象与分类

测试对象是 `unity list --project-path GameUnity --json` 返回的 149 个 `built-in` 工具，与 MCP 客户端枚举到的 `mcp__unity-cli__*` 工具一一对应。报告按以下 15 类组织（括号内为工具数）：

编辑器生命周期与可观测性（16）、资产与文件（12）、场景（9）、GameObject 与组件（14）、Prefab（7）、脚本与编译/热重载（13，含 `recompile` / `recompile_status`）、动画与 Timeline（14）、材质与 Shader（4）、烘焙（17）、导航与选择（3）、截图（3，含 `screenshot`）、构建与测试（12）、项目设置（18）、包管理（6）、批处理（1）。每个工具只归一类，合计 149。

## 执行方式

每个工具恰好对应一种执行方式：

| 执行方式 | 含义 | 允许的结果 |
| --- | --- | --- |
| 真实执行 | 以真实参数调用，产生实际效果，效果限定在隔离范围内并在结束时清理 | `通过` / `失败` |
| dry_run + 拒绝门 | 先以 `dry_run=true` 调用取得预览，再以 `confirm=false`（或不带 `confirm`）调用验证被拒绝；两次返回都记录 | `通过` / `失败` |
| 预期不可用 | 工具依赖的包未安装或需要已连接的 Player；记录实际错误码/消息 | `预期不可用` / `失败`（返回与预期不符时） |
| 跳过 | 仅限 brief 决策明确列出的原因；当前决策下没有任何工具预设为跳过 | `跳过`（必须写原因） |

## 执行策略表（深度档）

执行档位为深度档：标注「改后恢复」的行先读取原值、改为不同值并回读、再恢复原值；`ProjectSettings/` 事后以 `git diff` 校验无残留。标注「不真实执行」的行在任何档位都只做 `dry_run` + 拒绝门。

### 编辑器生命周期与可观测性

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `editor_status` | 真实执行 | 测试前后各一次，`status=ready`、`playMode=stopped` |
| `editor_focus` | 真实执行 | Unity 窗口到前台 |
| `editor_play` | 真实执行 | 进入 Play；随后 `editor_status.playMode=playing` |
| `editor_pause` | 真实执行 | Play 中切换暂停，再切回 |
| `editor_stop` | 真实执行 | 退出 Play；`playMode=stopped` |
| `menu` | 真实执行 | 无 path 列出菜单；执行 `Assets/Refresh` |
| `set_autotick` | 真实执行 | `enable=true, interval_ms=16, persist=false` |
| `get_console_logs` | 真实执行 | `severity=error, limit=20` |
| `console` | 真实执行 | `tail=20`，再以返回的 `cursor` 作 `since` 跟随一次 |
| `clear_console` | 真实执行 | 清空后 `console` 返回条目数下降 |
| `get_performance_stats` | 真实执行 | 返回渲染/内存/帧时间字段 |
| `audit` | 预期不可用 | Project Auditor 未安装；若实际能扫描则改记真实执行结果 |
| `audit_status` | 预期不可用 | 同上；返回 `unavailable` 或 `idle` |
| `report_evals` | 真实执行 | `top=10`，返回 eval 指纹排名 |
| `get_authoring_root` | 真实执行 | 返回 `Assets` |
| `set_authoring_root` | 真实执行 | 设为 `Assets/PipelineMcpTest` 后，越界路径调用被拒绝；随后复原为 `Assets` |

### 资产与文件

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `create_folder` | 真实执行 | `PipelineMcpTest/Sub` |
| `create_asset` | 真实执行 | `UnityEngine.Material`（`Materials/McpTestMat.mat`）与 `UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset` 或其他可创建 ScriptableObject（`Data/McpTestSO.asset`） |
| `write_text_file` | 真实执行 | 写 `PipelineMcpTest/notes.txt`；覆盖时不带 `confirm` 被拒绝，带 `confirm=true` 成功 |
| `read_text_file` | 真实执行 | 读回内容一致 |
| `find_assets` | 真实执行 | `name=McpTest`、`type=Material`、`search_in=PipelineMcpTest` |
| `copy_asset` | 真实执行 | `dry_run=true` 预览后真实复制，GUID 不同 |
| `move_asset` | 真实执行 | 移入 `Sub/`，GUID 不变 |
| `rename_asset` | 真实执行 | 重命名，GUID 不变 |
| `import_asset` | 真实执行 | 导入 `Temp/mcp_test.png`（由 `eval` 生成）到 `PipelineMcpTest/Tex/mcp_test.png` |
| `get_import_settings` | 真实执行 | 读取该 PNG 的 TextureImporter 字段 |
| `set_import_settings` | 真实执行 | `dry_run=true` 后真实设置 `isReadable=true` |
| `delete_asset` | 真实执行 | 无 `confirm` 被拒绝；`dry_run` 预览；`confirm=true` 删除临时资产（清理阶段整目录删除） |

### 场景

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `create_scene` | 真实执行 | `PipelineMcpTest/McpTestScene`，`additive=true`，`template=default` |
| `open_scene` | 真实执行 | 临时场景保存后：非 additive 打开临时场景 → 非 additive 打开回 `GameStart` → `additive=true` 重新打开临时场景 |
| `save_scene` | 真实执行 | 只传临时场景路径 |
| `save_all` | 真实执行 | 测试开始前一次（把 `GameStart` 内存修改写盘并记录哈希）；临时场景 dirty 时再一次 |
| `list_open_scenes` | 真实执行 | 含 GameStart 与临时场景 |
| `set_active_scene` | 真实执行 | 设为临时场景，结束前设回 GameStart |
| `get_scene_hierarchy` | 真实执行 | 临时场景树含创建的对象 |
| `add_scene_to_build` | 真实执行 | 加入临时场景 `enabled=false` |
| `remove_scene_from_build` | 真实执行 | 移除后 `get_build_settings` 场景列表与测试前一致 |

### GameObject 与组件

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `create_gameobject` | 真实执行 | 空对象 `McpRoot` 与 `primitive=cube` |
| `create_gameobjects` | 真实执行 | `count=3` 带 positions |
| `find_gameobjects` | 真实执行 | 按 name / type / hierarchy_path |
| `set_transform` | 真实执行 | 位置/旋转/缩放回读一致 |
| `set_parent` | 真实执行 | 挂到 `McpRoot` 下，再 detach |
| `set_active` | 真实执行 | false → true |
| `set_tag` | 真实执行 | 使用 `McpTestTag`（先由 `set_tags_layers` 添加） |
| `set_layer` | 真实执行 | 按名 `UI` 与按索引 `0` |
| `rename_gameobject` | 真实执行 | 回读名称 |
| `delete_gameobject` | 真实执行 | 删除后 `find_gameobjects` 找不到 |
| `add_component` | 真实执行 | `Rigidbody`、`UnityEngine.BoxCollider` |
| `remove_component` | 真实执行 | 移除 `Rigidbody` |
| `get_component_properties` | 真实执行 | 读取 `Transform` / `Rigidbody` |
| `set_component_properties` | 真实执行 | 设置 `Rigidbody.m_Mass` 后回读 |

### Prefab

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `create_prefab` | 真实执行 | `PipelineMcpTest/Prefabs/McpCube` |
| `instantiate_prefab` | 真实执行 | 实例化到临时场景 |
| `create_prefab_variant` | 真实执行 | `McpCubeVariant` |
| `apply_prefab_overrides` | 真实执行 | 修改实例位置后 apply，prefab 资产更新 |
| `revert_prefab_overrides` | 真实执行 | 再改后 revert，实例回到资产值 |
| `unpack_prefab` | 真实执行 | `completely=false` |
| `save_prefab_contents` | 真实执行 | `rename_child` / `set_active_child` |

### 脚本与编译 / 热重载

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `create_script` | 真实执行 | `McpTestBehaviour`（含 `[HotReload] public void Tick()` 与 `public int counter`） |
| `recompile` | 真实执行 | 触发后轮询 |
| `recompile_status` | 真实执行 | 到达 `completed` / `up_to_date` |
| `attach_script` | 真实执行 | 分别以 `type` 与 `script` 路径各附加一次 |
| `get_serialized_fields` | 真实执行 | 读 `counter`，`format=value` |
| `set_serialized_field` | 真实执行 | 设 `counter=42` 后回读 |
| `run_script` | 真实执行 | `AgentScripts/McpRunScript.cs`：`dry_run=true` 只编译；真实运行返回值；`args` 传参 |
| `eval` | 真实执行 | 表达式与语句各一次；故意的编译错误返回 `diagnostics` |
| `eval_file` | 真实执行 | `AgentScripts/McpEval.cs` |
| `reload_file` | 真实执行 | 修改 `Tick()` 体后应用；记录是否需要 Play 模式 |
| `reload_file_editor_interpreter` | 真实执行 | 同上，解释器后端 |
| `reload_file_override` | 真实执行 | 以 override 文件应用；不满足前置时记录实际错误 |
| `reload_file_player_interpreter` | 预期不可用 | 无已连接 Player |

### 动画与 Timeline

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `create_animation_clip` | 真实执行 | `Anim/McpClip.anim`，`loop=true` |
| `set_animation_curve` | 真实执行 | `Transform.m_LocalPosition.x` 两个关键帧 |
| `get_animation_clip` | 真实执行 | `includeKeys=true` 回读曲线 |
| `remove_animation_curve` | 真实执行 | 无 `confirm` 拒绝；`confirm=true` 移除 |
| `create_animator_controller` | 真实执行 | `Anim/McpController.controller` |
| `add_animator_parameter` | 真实执行 | Float / Trigger；重复名返回 `duplicate_parameter` |
| `add_animator_layer` | 真实执行 | `Upper`，Additive |
| `add_animator_state` | 真实执行 | `Idle`（默认）与 `Run`，`motion=McpClip` |
| `add_animator_transition` | 真实执行 | `Idle→Run` 带条件；不存在的层返回 `layer_not_found` |
| `get_animator_controller` | 真实执行 | 结构含以上参数/层/状态/过渡 |
| `create_timeline` | 预期不可用 | `com.unity.timeline` 未安装 |
| `add_timeline_track` | 预期不可用 | 同上 |
| `add_timeline_clip` | 预期不可用 | 同上 |
| `get_timeline` | 预期不可用 | 同上 |

### 材质与 Shader

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `list_shaders` | 真实执行 | `filter=Universal`，`limit=20` |
| `get_shader_properties` | 真实执行 | `shader=Universal Render Pipeline/Lit` |
| `get_material_properties` | 真实执行 | 读 `McpTestMat.mat` |
| `set_material_properties` | 真实执行 | `dry_run` 后设置 `_BaseColor`、启用 `_EMISSION`，回读 |

### 烘焙

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `get_lighting_settings` | 真实执行 | 读取当前设置 |
| `set_lighting_settings` | 真实执行（改后恢复） | `dry_run=true` 预览后真实设置一个字段并恢复 |
| `bake_lighting` | dry_run + 拒绝门（不真实执行） | `dry_run=true` |
| `lighting_bake_status` | 真实执行 | `idle` |
| `cancel_lighting_bake` | 真实执行 | 无烘焙时返回 |
| `clear_baked_lighting` | dry_run + 拒绝门（不真实执行） | 无 `confirm` 拒绝；`dry_run` 预览 |
| `get_navmesh_settings` | 真实执行 | 读取默认 agent |
| `set_navmesh_settings` | 真实执行（改后恢复） | `dry_run` 预览后真实设置 `agentRadius` 并恢复 |
| `bake_navmesh` | 真实执行 | `dry_run` 预览；临时场景为活动场景且含静态 Plane 时真实烘焙 |
| `navmesh_bake_status` | 真实执行 | `idle` |
| `cancel_navmesh_bake` | 真实执行 | 无烘焙时返回 |
| `clear_navmesh` | 真实执行 | 无 `confirm` 拒绝；`confirm=true` 清除临时场景烘焙结果 |
| `bake_navmesh_surfaces` | 预期不可用 | AI Navigation 未安装，`package_not_found` |
| `bake_occlusion_culling` | 真实执行 | `dry_run` 预览；临时场景为活动场景时真实烘焙 |
| `occlusion_bake_status` | 真实执行 | `idle` |
| `cancel_occlusion_bake` | 真实执行 | 无烘焙时返回 |
| `clear_occlusion_culling` | 真实执行 | 无 `confirm` 拒绝；`confirm=true` 清除 |

### 导航与选择

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `get_selection` | 真实执行 | 读取当前选择 |
| `set_selection` | 真实执行 | 选中临时 prefab 资产与临时对象，回读一致 |
| `search` | 真实执行 | `t:Material McpTest` |

### 截图

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `capture_game_view` | 真实执行 | `save_path=Temp/mcp_game.png`，`source=camera`；Play 中再以 `source=screen` 一次 |
| `capture_scene_view` | 真实执行 | `save_path=Temp/mcp_scene.png` |
| `screenshot` | 真实执行 | `view=game` 与 `view=scene` 各一张，输出到 `Temp/`（项目根相对） |

### 构建与测试

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `build` | dry_run + 拒绝门（不真实执行） | `dry_run=true` 校验 Android 目标；无 `confirm` 拒绝 |
| `build_status` | 真实执行 | `idle` |
| `switch_build_target` | dry_run + 拒绝门（不真实执行，无 `dry_run` 参数） | 无 `confirm` 被拒绝 |
| `switch_build_target_status` | 真实执行 | `idle` |
| `list_build_targets` | 真实执行 | Android / Windows 已安装 |
| `get_build_settings` | 真实执行 | 测试前后快照一致 |
| `set_build_settings` | 真实执行（改后恢复） | `dry_run` 预览；`developmentBuild` 改后恢复 |
| `list_build_profiles` | 真实执行 | 返回列表（可为空） |
| `list_tests` | 真实执行 | 临时测试程序集写入并编译后 ≥ 1 |
| `run_tests` | 真实执行 | `mode=editor`，`filter=PipelineMcpTest` |
| `test_status` | 真实执行 | 全部通过 |
| `cancel_tests` | 真实执行 | 无运行时返回 |

### 项目设置

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `get_audio_settings` / `get_graphics_settings` / `get_input_settings` / `get_physics_settings` / `get_player_settings` / `get_quality_settings` / `get_tags_layers` / `get_time_settings` / `get_runtime_pipeline_settings` | 真实执行 | 读取并记录快照 |
| `set_audio_settings` / `set_physics_settings` / `set_quality_settings` / `set_time_settings` / `set_input_settings` / `set_runtime_pipeline_settings` | 真实执行（改后恢复） | `dry_run=true` 预览、无 `confirm` 拒绝、`confirm=true` 改一个字段并回读、再恢复原值 |
| `set_graphics_settings` | dry_run + 拒绝门（不真实执行） | `dry_run` 预览当前 URP 资产；无 `confirm` 拒绝 |
| `set_player_settings` | 真实执行（改后恢复，仅 `bundleVersion`） | 不改 `scriptingBackend` / `apiCompatibilityLevel` |
| `set_tags_layers` | 真实执行 | `addTags=[McpTestTag]` → `removeTags=[McpTestTag]`；`dry_run` 与拒绝门也记录 |

### 包管理

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `package_list` | 真实执行 | `scope=installed` 含 `com.unity.pipeline` |
| `package_search` | 真实执行 | `query=com.unity.timeline` |
| `package_add` | dry_run + 拒绝门（不真实执行） | `identifier=com.unity.timeline`，`dry_run=true`；无 `confirm` 拒绝 |
| `package_remove` | dry_run + 拒绝门（不真实执行） | `name=com.unity.timeline`，`dry_run=true`；无 `confirm` 拒绝 |
| `package_resolve` | 真实执行 | 触发重解析/域重载后轮询 `editor_status` 与 `package_status` |
| `package_status` | 真实执行 | `idle` |

### 批处理

| 工具 | 执行方式 | 输入要点 / 预期 |
| --- | --- | --- |
| `batch` | 真实执行 | ① `dry_run=true`：不创建对象；② 事务批次：`create_gameobject` + `set_transform`（引用 `$0.instanceId`）成功；③ 含故意失败操作（不存在的组件类型）的事务批次：返回失败且其前序创建的对象不存在 |

## 隔离与清理

- 临时资产只在 `Assets/PipelineMcpTest/` 下；临时场景 `Assets/PipelineMcpTest/McpTestScene.unity` 以 additive 打开；`run_script` / `eval_file` 源文件放在 `GameUnity/AgentScripts/`；截图放在 `GameUnity/Temp/`。
- 结束时：临时场景设回非活动并关闭（通过 `eval` 调用 `EditorSceneManager.CloseScene`），`Assets/PipelineMcpTest/` 与 `GameUnity/AgentScripts/` 删除（含 `.meta`），`McpTestTag` 移除，authoring root 复原为 `Assets`，Build Settings 场景列表复原；档位 B/C 修改过的设置恢复原值。
- 终态要求见 brief A5 / A6。

## 报告格式

`docs/unity-cli-mcp-test-report.md` 依次包含：

1. 环境：Unity 版本、`unity` CLI 版本、`com.unity.pipeline` 版本、测试日期、执行档位（深度档）、GameStart 写盘记录（`git diff --stat` 与 SHA-256）。
2. 汇总：各结果计数（通过 / 失败 / 预期不可用 / 跳过）。
3. 分类结果表（15 类）：列为 `工具`、`执行方式`、`输入摘要`、`结果`、`返回摘要 / 原因`。每个工具恰好一行。
4. 端到端链路记录：脚本编译链路、测试链路、批处理三情形、Play 往返。
5. 发现的问题：逐条记录异常、与文档不符或可疑行为；无则写"无"。
6. 附录：`unity` CLI 命令行冒烟结果（`unity status --json`、`unity list --project-path GameUnity --json`、`unity command editor_status --project-path GameUnity --json`、`unity command --help`、`unity mcp configure --list`、`unity job --help` 的退出码与关键字段）。

## hgame-dev 参考文档

`.claude/skills/hgame-dev/references/mcp-unity-cli.md` 与 `.codex/skills/hgame-dev/references/mcp-unity-cli.md` 内容一致，面向后续 Agent 使用 `unity-cli` MCP 时查阅，包含六节：

1. 已核实来源：`com.unity.pipeline` 版本、`.mcp.json` 中的 `unity-cli` 服务器配置、`unity list --json` 的工具数量（149）；并记录 `com.coplaydev.unity-mcp` 已移除（提交 `7600c17e`）、`mcp-tools.md` 已删除，本文档与 `mcp-visual.md` 是 HGame 唯一的 MCP 指南（详见 `mcp-agent-guidance` 规格）。
2. 安全门：`confirm` / `dry_run` 约定、Undo 分组、authoring root 路径限制；在 HGame 工程中永不真实执行的工具清单（`switch_build_target`、`package_add` / `package_remove`、`bake_lighting` / `clear_baked_lighting`、`set_graphics_settings`、`set_player_settings` 的脚本后端/API 级别）及原因。
3. 路径与 authoring root：工具接受 `Assets/` 相对路径（前缀可省）、`run_script` / `eval_file` 使用 `Assets/` 之外的文件、HGame 资源根目录 `Assets/BundleAssets/` 与热更脚本目录的落位提醒。
4. 按分类的工具速查：15 类、149 个工具名，每个附一句用途与实测备注（如需要包、需要 Play、触发域重载）。
5. 端到端链路模板：脚本创建→编译→附加；测试程序集→`list_tests`→`run_tests`→`test_status`；`batch` 事务用法；Play 往返与控制台检查；域重载后的 `editor_status` 轮询。
6. 已知问题：与报告「发现的问题」一致。

路由登记：`.claude/skills/hgame-dev/SKILL.md` 与 `.codex/skills/hgame-dev/SKILL.md` 的文档路由表新增一行「MCP Unity CLI（com.unity.pipeline）」指向 `mcp-unity-cli.md`；根 `CLAUDE.md` 与 `AGENTS.md` 的「第一步」主题表「MCP 工具链」行补充 `mcp-unity-cli.md`，「Reference 路由」表新增「MCP Unity CLI 工具（com.unity.pipeline）」行；现有条目不删除。
