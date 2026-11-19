# Unity CLI MCP 工具验证报告

## 1. 环境

| 项目 | 值 |
| --- | --- |
| 测试日期 | 2026-09-12（北京时间 00:30 – 05:10） |
| Unity Editor | 6000.3.10f1，工程 `F:\Test\DGame\GameUnity`，Pipeline 服务 `127.0.0.1:7800`，PID 19256 |
| `unity` CLI | 1.0.0-beta.9 |
| `com.unity.pipeline` | 0.6.0-exp.1（`Library/PackageCache/com.unity.pipeline@9bb4172c603c`） |
| MCP 服务器 | `.mcp.json` → `unity mcp --project-path GameUnity`（`UNITY_NO_BANNER=1`、`UNITY_NON_INTERACTIVE=1`） |
| 工具数量 | `unity list --project-path GameUnity --json` → `count: 149`，全部 `group: built-in`，与 MCP 客户端 `mcp__unity-cli__*` 一一对应 |
| 执行档位 | 深度档：只读 / 隔离目录内写入 / Play / 编译 / 热重载 / 烘焙 / 项目设置改后恢复 全部真实执行；`build`、`switch_build_target`、`package_add` / `package_remove`、`bake_lighting` / `clear_baked_lighting`、`set_graphics_settings` 只做 `dry_run` + 拒绝门；`set_player_settings` 只改 `bundleVersion`，`scriptingBackend` / `apiCompatibilityLevel` 不触碰 |
| 活动构建目标 | Android（已安装：Android、StandaloneWindows、StandaloneWindows64） |
| 资源运行模式 | `EditorPrefs["EditorPlayMode"] = 0`（EditorSimulateMode，Play 不联网） |
| 未安装包 | `com.unity.timeline`、AI Navigation、Project Auditor |

**GameStart 写盘记录（D7）**：测试前 `GameStart.unity` 在 git 中已有 4 行改动（`UIRoot` 实例 `UICamera` 的 `m_ClearFlags: 4` 覆盖，文件 mtime 2026-09-11 23:50:32，早于本次测试），`save_all` 时编辑器已无脏场景（返回 `scenes: []`）。文件 SHA-256 在测试前、`save_all` 后与测试结束时一致：

```
61dfc63a6805fa591701ee4a427a43d21014d618b1c545f163625623ad23cfff  GameUnity/Assets/Scenes/GameStart/GameStart.unity
git diff --stat: GameUnity/Assets/Scenes/GameStart/GameStart.unity | 4 ++++
```

**隔离**：临时资产 `Assets/PipelineMcpTest/`（含 additive 临时场景 `McpTestScene.unity`、`McpHotReloadScene.unity`）、`GameUnity/AgentScripts/`、截图与工作日志 `GameUnity/Temp/mcp-test/`、临时 Tag `McpTestTag`、临时 EditMode 程序集 `PipelineMcpTest.Editor`。测试结束后前两者删除，Tag 移除，authoring root 复原为 `Assets`。

## 2. 汇总

| 结果 | 数量 |
| --- | --- |
| 通过 | 140 |
| 失败 | 1（`clear_baked_lighting`：`dry_run` 无法预览，见问题 1） |
| 预期不可用 | 8（Timeline 4 个、`audit` / `audit_status`、`bake_navmesh_surfaces`、`reload_file_player_interpreter`） |
| 跳过 | 0 |
| 合计 | 149 |

执行方式分布：真实执行 134（其中 `set_player_settings` 只以 `bundleVersion` 真实执行，`scriptingBackend` / `apiCompatibilityLevel` 未触碰）、`dry_run` + 拒绝门 7（`bake_lighting`、`clear_baked_lighting`、`build`、`switch_build_target`、`set_graphics_settings`、`package_add`、`package_remove`）、预期不可用 8。

## 3. 分类结果

结果列取值：`通过` / `失败` / `预期不可用` / `跳过`。执行方式：真实执行、dry_run + 拒绝门、预期不可用。

### 3.1 编辑器生命周期与可观测性（16）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `editor_status` | 真实执行 | 测试前 / Play 中 / 结束后 | 通过 | `status=ready` `playMode=stopped`；Play 中 `playing`，暂停时 `paused` |
| `editor_focus` | 真实执行 | — | 通过 | "Editor focused via DockArea" |
| `editor_play` | 真实执行 | 两次（GameStart + 临时场景集合） | 通过 | "Entered play mode"；8 s 后 `playing`，控制台 0 错误 |
| `editor_pause` | 真实执行 | Play 中两次 | 通过 | "Play mode paused" → `playMode=paused`；再次 → "unpaused" |
| `editor_stop` | 真实执行 | 两次 | 通过 | "Exited play mode"；5 s 后 `ready` / `stopped`，场景均 `isDirty=false` |
| `menu` | 真实执行 | 无 path；`Assets/Refresh` | 通过 | 列出 849 项；执行成功 |
| `set_autotick` | 真实执行 | `enable=true interval_ms=16 persist=false` | 通过 | "Auto-tick already enabled (interval updated to 16ms)" |
| `get_console_logs` | 真实执行 | `severity=error/warning/all limit=20` | 通过 | 返回 5 条历史错误（2026-09-10）；清空后 0 |
| `console` | 真实执行 | `tail=5`；`since=<cursor>` | 通过 | seq 2–6 `cursor=6`；跟随读取 0 条 |
| `clear_console` | 真实执行 | — | 通过 | `cleared:true`；`get_console_logs` 变 0，但 `console` 仍返回旧条目（问题 3） |
| `get_performance_stats` | 真实执行 | — | 通过 | drawCalls 1、mono 1.29 GB、cpuFrame 0.87 ms |
| `audit` | 预期不可用 | 默认 | 预期不可用 | `status=unavailable`："Project Auditor is not installed" |
| `audit_status` | 预期不可用 | — | 预期不可用 | 同上 |
| `report_evals` | 真实执行 | `top=10` | 通过 | totalCount 4、distinctFingerprints 10、coverageSuggestions |
| `get_authoring_root` | 真实执行 | 测试前后 | 通过 | `Assets` |
| `set_authoring_root` | 真实执行 | `Assets/PipelineMcpTest` → `Assets` | 通过 | 设定后 `read_text_file Assets/Scenes/...` 被拒 "outside the authoring root"，`..` 被拒，相对路径 `notes.txt` 成功；复原成功 |

### 3.2 资产与文件（12）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `create_folder` | 真实执行 | `PipelineMcpTest/Sub` 及 batch 7 个子目录 | 通过 | 返回 DefaultAsset 句柄，中间目录自动创建 |
| `create_asset` | 真实执行 | `UnityEngine.Material`（默认 URP/Lit；`shader=Standard`）、`UnityEngine.Rendering.VolumeProfile` | 通过 | 三个资产创建成功；已存在路径时无 `confirm` → 400；`dry_run=true` 时同样返回"已存在"而非预览（问题 15） |
| `write_text_file` | 真实执行 | 新建 / 覆盖无 confirm / `confirm=true` 含中文 / asmdef / 测试脚本 | 通过 | 覆盖无 confirm → 400 "Pass confirm=true"；confirm 后 21 B 中文内容正确 |
| `read_text_file` | 真实执行 | `PipelineMcpTest/notes.txt` | 通过 | 内容与写入一致 |
| `find_assets` | 真实执行 | `name=McpTest search_in=PipelineMcpTest`；`type=Material` | 通过 | 2 条 / 1 条，含 guid 与 type |
| `copy_asset` | 真实执行 | `dry_run` 后真实复制 | 通过 | 预览返回目标路径；复制后 GUID 不同（`39ea8a4a…`） |
| `move_asset` | 真实执行 | `dry_run` 后移入 `Sub/` | 通过 | GUID 保持 |
| `rename_asset` | 真实执行 | `notes_copy` → `notes_renamed` | 通过 | 扩展名保留，GUID 保持 |
| `import_asset` | 真实执行 | `dry_run` 后导入 `Temp/mcp-test/mcp_test.png`（`eval` 生成 4×4） | 通过 | `type=Texture2D` |
| `get_import_settings` | 真实执行 | `platform=Default` / `Android` | 通过 | TextureImporter 字段 + platformOverride 块 |
| `set_import_settings` | 真实执行 | `dry_run`（含未知字段）后设置 `isReadable/mipmapEnabled` | 通过 | `unknown:[bogusField]`；真实设置后回读 `isReadable=true mipmapEnabled=false` |
| `delete_asset` | 真实执行 | 无 confirm / `dry_run` / `confirm=true` | 通过 | 无 confirm → 400；预览返回资产描述；confirm 删除 `Assets/Temp`、`Assets/PipelineMcpTest`（含 .meta） |

### 3.3 场景（9）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `create_scene` | 真实执行 | `additive=true template=default` / `empty` | 通过 | 新场景自动成为活动场景（`isActive=true`），`rootCount=2` |
| `open_scene` | 真实执行 | 非 additive 打开临时场景 → 非 additive 打开 GameStart → additive 打开临时场景 | 通过 | 三次均成功；非 additive 会关闭其他场景 |
| `save_scene` | 真实执行 | 只传临时场景路径 | 通过 | `isDirty` → false |
| `save_all` | 真实执行 | 测试前（D7）与临时场景 dirty 时 | 通过 | 第一次 `scenes: []`（无脏场景）；第二次保存 `McpTestScene.unity` |
| `list_open_scenes` | 真实执行 | 多次 | 通过 | 返回 name/path/isLoaded/isDirty/isActive/rootCount |
| `set_active_scene` | 真实执行 | 临时场景 ↔ GameStart | 通过 | 路径支持省略 `Assets/` 与 `.unity` |
| `get_scene_hierarchy` | 真实执行 | `path=PipelineMcpTest/McpTestScene` | 通过 | 树含 instanceId / hierarchyPath / components / children |
| `add_scene_to_build` | 真实执行 | `enabled=false` | 通过 | `count=2`，`get_build_settings` 可见 |
| `remove_scene_from_build` | 真实执行 | — | 通过 | `removed:true count=1`，与测试前一致 |

### 3.4 GameObject 与组件（14）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `create_gameobject` | 真实执行 | 空对象；`primitive=cube/sphere/plane`；带 `parent` | 通过 | 返回 instanceId 与 hierarchyPath |
| `create_gameobjects` | 真实执行 | `count=3 primitive=capsule positions scales parent` | 通过 | `McpBatch1..3`，位置 / 缩放生效 |
| `find_gameobjects` | 真实执行 | name / type / hierarchy_path / tag / `include_inactive=false` | 通过 | 各过滤器结果正确；inactive 对象被排除 |
| `set_transform` | 真实执行 | 位置 / 旋转 / 缩放 | 通过 | `get_component_properties` 回读一致 |
| `set_parent` | 真实执行 | 挂到 `/McpRoot` 再 detach | 通过 | hierarchyPath 变化正确 |
| `set_active` | 真实执行 | false → true | 通过 | 停用后 `find_gameobjects(include_inactive=false)` 找不到 |
| `set_tag` | 真实执行 | `McpTestTag` | 通过 | `find_gameobjects tag=McpTestTag` 命中 |
| `set_layer` | 真实执行 | 按名 `UI`；按索引 `2` | 通过 | `eval` 回读 layer=UI |
| `rename_gameobject` | 真实执行 | `McpBatch1` → `McpBatchRenamed` | 通过 | 层级中名称更新 |
| `delete_gameobject` | 真实执行 | `/McpRoot/McpBatch2` | 通过 | 层级中消失 |
| `add_component` | 真实执行 | `Rigidbody`；`UnityEngine.BoxCollider` | 通过 | 短名与全名均可 |
| `remove_component` | 真实执行 | `Rigidbody` | 通过 | `find_gameobjects type=Rigidbody` = 0 |
| `get_component_properties` | 真实执行 | Transform / Rigidbody | 通过 | 数值正确；Quaternion / LayerMask 显示 `<unsupported:…>`（问题 5） |
| `set_component_properties` | 真实执行 | `m_Mass=7.5 m_UseGravity=false m_Interpolate="Interpolate"` | 通过 | 返回体即为更新后的属性 |

### 3.5 Prefab（7）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `create_prefab` | 真实执行 | `/McpCube` → `Prefabs/McpCube` | 通过 | `.prefab` 自动补全，源对象变为实例 |
| `instantiate_prefab` | 真实执行 | `name=McpCubeInstance` | 通过 | 实例进入活动场景 |
| `create_prefab_variant` | 真实执行 | `McpCubeVariant` | 通过 | 变体资产创建 |
| `apply_prefab_overrides` | 真实执行 | 改实例 position / scale 后 apply | 通过 | 资产 `m_LocalPosition=(1.5,2.5,-3)`、`m_LocalScale=3` 更新（`get_serialized_fields` 读资产核实） |
| `revert_prefab_overrides` | 真实执行 | 实例 scale 改 9 后 revert | 通过 | 实例回到资产值 3 |
| `unpack_prefab` | 真实执行 | `completely=false` | 通过 | `PrefabUtility.GetPrefabInstanceStatus` = NotAPrefab |
| `save_prefab_contents` | 真实执行 | `rename_child Child→ChildRenamed`；`set_active_child=false` | 通过 | prefab 文件 `m_Name: ChildRenamed`、`m_IsActive: 0`；场景实例不受影响 |

### 3.6 脚本、编译与热重载（13）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `create_script` | 真实执行 | `McpTestBehaviour` 到 `PipelineMcpTest/Scripts` | 通过 | 生成 206 B MonoBehaviour 模板 |
| `recompile` | 真实执行 | 写脚本后；写 asmdef 后 | 通过 | `up_to_date`（`write_text_file` 已触发导入编译）；`completed failed:false` |
| `recompile_status` | 真实执行 | — | 通过 | `idle` / `up_to_date` / `completed`，`errors:[]` |
| `attach_script` | 真实执行 | `type=McpTestBehaviour`；`script=Assets/.../McpTestBehaviour.cs` | 通过 | 两种定位方式均成功 |
| `get_serialized_fields` | 真实执行 | 组件 `counter`；`format=value`；prefab 资产 Transform | 通过 | 描述符 / 单值 / Quaternion 以 xyzw 输出 |
| `set_serialized_field` | 真实执行 | `counter=42` | 通过 | 回读 42 |
| `run_script` | 真实执行 | `dry_run`；`entry=McpAgent.McpRunScript.Main`；`args=["ab",3,true]`；`args=["/McpCubeInstance"]`（ObjectRef） | 通过 | `compileMs≈50–133`；返回值、参数绑定、GameObject 参数解析均正确 |
| `eval` | 真实执行 | 表达式 / 语句 / 故意编译错误 | 通过 | 编译错误返回 `diagnostics`（CS0029 行列） |
| `eval_file` | 真实执行 | `AgentScripts/McpEval.cs` | 通过 | 返回字符串 |
| `reload_file` | 真实执行 | 编辑模式；Play（默认设置）；Play（`enableInBuilds=true`） | 通过 | 前两者失败 "not registered as [HotReloadWithOverrides]"；启用 Runtime Pipeline 后 Play 中成功，`Tick()` 由 +1 变 +7（问题 6） |
| `reload_file_editor_interpreter` | 真实执行 | 同上 | 通过 | 默认设置下报 "no woven dispatch target"（误导）；注册后成功，`Tick()` +1000 |
| `reload_file_override` | 真实执行 | `AgentScripts/McpOverrides.cs`（`[HotReloadOverrideMethod("McpTestBehaviour.Compute")]`） | 通过 | 编辑模式即成功注册；`HotReloadRegistry.TryInvokeHotReloadResult` 返回 300；但 `HotReloadHelper.ExecuteWithHotReload(Func<object>)` 返回 null（问题 7） |
| `reload_file_player_interpreter` | 预期不可用 | 同一文件 | 预期不可用 | "no player connected (run a Development Build with Autoconnect Profiler)" |

### 3.7 动画与 Timeline（14）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `create_animation_clip` | 真实执行 | `frameRate=30 loop=true` | 通过 | AnimationClip 创建 |
| `set_animation_curve` | 真实执行 | `dry_run`；root `m_LocalPosition.x` 两键（含切线）；`path=ChildRenamed m_LocalScale.y` | 通过 | `keyCount=2`，预览不写盘 |
| `get_animation_clip` | 真实执行 | `includeKeys=true` / false | 通过 | frameRate 30、length 1、loop true、绑定与关键帧一致 |
| `remove_animation_curve` | 真实执行 | 无 confirm / `dry_run` / `confirm=true` | 通过 | 400 → 预览 → 移除后剩 1 绑定 |
| `create_animator_controller` | 真实执行 | `Anim/McpController.controller` | 通过 | 含默认 Base Layer |
| `add_animator_parameter` | 真实执行 | Float(0.5) / Trigger / 重复名 | 通过 | 重复名返回 `code: duplicate_parameter` |
| `add_animator_layer` | 真实执行 | `Upper Additive weight=0.75` | 通过 | `index=1` |
| `add_animator_state` | 真实执行 | `Idle`(default) / `Run`(motion=McpClip) / 不存在的层 | 通过 | `layer_not_found` 错误码正确 |
| `add_animator_transition` | 真实执行 | `dry_run`；`Idle→Run Speed Greater 0.1`；`AnyState→Run Jump If`；Float 用 `If` | 通过 | Float+If → 400 "not valid for Float parameter" |
| `get_animator_controller` | 真实执行 | — | 通过 | 参数 / 层 / 状态 / 过渡 / 条件完整 |
| `create_timeline` | 预期不可用 | — | 预期不可用 | `code: package_not_found` "com.unity.timeline is not installed" |
| `add_timeline_track` | 预期不可用 | — | 预期不可用 | 同上 |
| `add_timeline_clip` | 预期不可用 | — | 预期不可用 | 同上 |
| `get_timeline` | 预期不可用 | — | 预期不可用 | 同上 |

### 3.8 材质与 Shader（4）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `list_shaders` | 真实执行 | `filter="Universal Render Pipeline/" limit=8` | 通过 | 返回 name / assetPath / isBuiltin / isSupported |
| `get_shader_properties` | 真实执行 | `shader=Universal Render Pipeline/Lit` | 通过 | 50 个属性，含 type / range / flags |
| `get_material_properties` | 真实执行 | `McpTestMat.mat` | 通过 | shader / rawRenderQueue / enabledKeywords / 全部属性 |
| `set_material_properties` | 真实执行 | `dry_run`（含未知属性）；`_BaseColor` hex、`_Metallic`、`_EmissionColor` 数组、`_BaseMap` ObjectRef、`renderQueue`、`enableKeywords/disableKeywords` | 通过 | 属性与贴图正确；URP 材质上 renderQueue / `_EMISSION` 被 URP 后处理重置（问题 9）；Built-in `Standard` 材质完全正确 |

### 3.9 烘焙（17）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `get_lighting_settings` | 真实执行 | 前 / 后 | 通过 | `available:false` → 创建后 `available:true bakedGI:false bounces:1` |
| `set_lighting_settings` | 真实执行 | `dry_run`（含未知键）；`bakedGI=false bounces=1` | 通过 | `settingsSource: none → created`（在活动场景上新建内存 LightingSettings，问题 11） |
| `bake_lighting` | dry_run + 拒绝门 | `dry_run=true` | 通过 | `wouldBake:true` + 设置快照；`confirm` 按描述"不强制"（问题 2） |
| `lighting_bake_status` | 真实执行 | — | 通过 | `idle` |
| `cancel_lighting_bake` | 真实执行 | — | 通过 | `cancelled:false` |
| `clear_baked_lighting` | dry_run + 拒绝门 | 无 confirm；`dry_run=true` | 失败 | 两者都返回 400 "Pass confirm=true"：`dry_run` 无法预览（问题 1） |
| `get_navmesh_settings` | 真实执行 | — | 通过 | agentRadius 0.5 等 |
| `set_navmesh_settings` | 真实执行 | `dry_run`；`agentRadius 0.5→0.6→0.5` | 通过 | `unknown:[bogus]`；回读一致；已恢复 |
| `bake_navmesh` | 真实执行 | `dry_run`；`confirm=true`（临时场景含静态 Plane） | 通过 | `baking bakeId` → 192 ms 完成，NavMesh 42 顶点 / 20 三角，`McpTestScene/NavMesh.asset` 生成 |
| `navmesh_bake_status` | 真实执行 | — | 通过 | `completed result=succeeded bakeTimeMs=192` |
| `cancel_navmesh_bake` | 真实执行 | — | 通过 | `cancelled:false` |
| `clear_navmesh` | 真实执行 | 无 confirm；`confirm+dry_run`；`confirm` | 通过 | 400 → `dry_run` 预览 → `cleared:true`，顶点 0，文件删除（`dry_run` 需同时 confirm，问题 1） |
| `bake_navmesh_surfaces` | 预期不可用 | — | 预期不可用 | `package_not_found`（com.unity.ai.navigation） |
| `bake_occlusion_culling` | 真实执行 | `dry_run`；`confirm smallest_occluder=1` | 通过 | 806 ms 完成，`umbraDataSize=14912`，`OcclusionCullingData.asset` 生成 |
| `occlusion_bake_status` | 真实执行 | — | 通过 | `completed succeeded`；`stats.umbraDataSizeBytes=0` 与实际 14912 不符（问题 10） |
| `cancel_occlusion_bake` | 真实执行 | — | 通过 | `cancelled:false` |
| `clear_occlusion_culling` | 真实执行 | 无 confirm；`confirm` | 通过 | 400 → `cleared:true`，umbra 0，文件删除 |

### 3.10 导航与选择（3）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `get_selection` | 真实执行 | — | 通过 | `count 0` |
| `set_selection` | 真实执行 | `paths=[prefab]`；`instance_ids`（Play 前 / Play 后） | 通过 | 资产选中；Play 前的 instanceId 返回 `unresolved`，域重载后 id 变化（问题 13） |
| `search` | 真实执行 | `t:Material limit=5` | 通过 | 5 条含 GlobalObjectId / path |

### 3.11 截图（3）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `screenshot` | 真实执行 | `view=game` / `scene`，`output=Temp/mcp-test/*.png` | 通过 | 566×886 PNG 写到项目根相对路径 |
| `capture_game_view` | 真实执行 | `save_path=Temp/...`（编辑模式，`source=camera`）；Play 中 `source=screen` 内联 | 通过 | 3096 B；`save_path` 被解析到 `Assets/Temp/...`（问题 4）；Play 内联图含 HotReload overlay |
| `capture_scene_view` | 真实执行 | `save_path=Temp/...` | 通过 | 35673 B，同样落到 `Assets/` |

### 3.12 构建与测试（12）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `build` | dry_run + 拒绝门 | `dry_run`（Android）；无 confirm；`dry_run target=WebGL` | 通过 | `valid:true`；"Refused: this triggers a player build"；WebGL → `validationErrors: not installed` |
| `build_status` | 真实执行 | — | 通过 | `idle` |
| `switch_build_target` | dry_run + 拒绝门 | `StandaloneWindows64` 无 confirm | 通过 | "Refused: switching the build target triggers a full reimport"（无 `dry_run` 参数） |
| `switch_build_target_status` | 真实执行 | — | 通过 | `idle` |
| `list_build_targets` | 真实执行 | — | 通过 | 19 个目标，`isInstalled` 标记 |
| `get_build_settings` | 真实执行 | 前 / 后 | 通过 | 场景列表与开关与测试前一致 |
| `set_build_settings` | 真实执行 | `dry_run`；`developmentBuild false→true→false` | 通过 | 无 confirm → Refused；applied 后回读；已恢复 |
| `list_build_profiles` | 真实执行 | — | 通过 | `[]` |
| `list_tests` | 真实执行 | `mode=all`（测试前）；`mode=editor`（写入临时程序集后） | 通过 | 0 → 2（`PipelineMcpTest.PipelineMcpSmokeTests.*`） |
| `run_tests` | 真实执行 | `mode=editor filter=PipelineMcpTest async_tests=true` | 通过 | 直接返回 `completed`，2/2 passed，0.24 s（问题 16） |
| `test_status` | 真实执行 | 前 / 后 | 通过 | `no_tests` → `completed` 含逐条结果 |
| `cancel_tests` | 真实执行 | — | 通过 | `no_tests` |

### 3.13 项目设置（18）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `get_audio_settings` | 真实执行 | — | 通过 | volume 1 / rolloff 1 / doppler 1 |
| `set_audio_settings` | 真实执行 | `dry_run`；无 confirm；`volume 1→0.5→1` | 通过 | "Intended effect: volume 1 -> 0.5"；Refused；applied；已恢复 |
| `get_graphics_settings` | 真实执行 | — | 通过 | `HGame_URPAsset`，`usingScriptableRenderPipeline:true` |
| `set_graphics_settings` | dry_run + 拒绝门 | `dry_run` 当前资产；`dry_run {}`；无 confirm | 通过 | "No changes specified"；"Intended effect: HGame_URPAsset -> <built-in>"；Refused |
| `get_input_settings` | 真实执行 | — | 通过 | 18 个轴 |
| `set_input_settings` | 真实执行 | `dry_run`；`Horizontal sensitivity 3→4→3` | 通过 | Refused / applied / 已恢复 |
| `get_physics_settings` | 真实执行 | — | 通过 | gravity (0,-9.81,0) solver 6 bounce 2 |
| `set_physics_settings` | 真实执行 | `dry_run`；`bounceThreshold 2→3→2` | 通过 | Refused / applied / 已恢复 |
| `get_player_settings` | 真实执行 | — | 通过 | IL2CPP / NET_Standard_2_0 / bundleVersion 1.0 |
| `set_player_settings` | 真实执行（仅 `bundleVersion`） | `dry_run`；`1.0→1.0.99→1.0` | 通过 | Refused / applied / 已恢复；未触碰 scriptingBackend / apiCompatibilityLevel |
| `get_quality_settings` | 真实执行 | — | 通过 | level 5 Ultra vSync 1 aa 0 |
| `set_quality_settings` | 真实执行 | `dry_run`；`vSyncCount 1→0→1` | 通过 | Refused / applied / 已恢复 |
| `get_tags_layers` | 真实执行 | 前 / 后 | 通过 | 7 tags / 5 layers，结束时无 `McpTestTag` |
| `set_tags_layers` | 真实执行 | `dry_run addTags`；无 confirm；`confirm addTags`；`confirm removeTags` | 通过 | 往返成功；`TagManager.asset` 被以新格式重写（问题 12） |
| `get_time_settings` | 真实执行 | — | 通过 | fixed 0.02 max 0.333 scale 1 |
| `set_time_settings` | 真实执行 | `dry_run`；`timeScale 1→2→1` | 通过 | Refused / applied / 已恢复 |
| `get_runtime_pipeline_settings` | 真实执行 | 前 / 后 | 通过 | enableInBuilds false、timeout 30000 等 |
| `set_runtime_pipeline_settings` | 真实执行 | Play 中 confirm；`dry_run`；`requestTimeoutMs 30000→45000→30000`；`enableInBuilds false→true→false` | 通过 | Play 中被拒（符合文档）；首次 confirm 新建 `ProjectSettings/Packages/com.unity.pipeline/RuntimePipelineConfig.json`（问题 12） |

### 3.14 包管理（6）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `package_list` | 真实执行 | `scope=installed include_indirect=false` | 通过 | 47 个直接依赖，含 manifest 映射 |
| `package_search` | 真实执行 | `query=com.unity.timeline` | 通过 | 1.8.13，`isInstalled:false` |
| `package_add` | dry_run + 拒绝门 | `com.unity.timeline@1.8.13` | 通过 | `plan: Add package…`；无 confirm → `status: rejected` |
| `package_remove` | dry_run + 拒绝门 | `com.unity.ide.rider` | 通过 | `plan: Remove package…`；无 confirm → rejected |
| `package_resolve` | 真实执行 | — | 通过 | `completed requiresRecompile:true`；随后 `editor_status=ready`，未发生域重载 |
| `package_status` | 真实执行 | 前 / 后 | 通过 | `idle` → `completed operation=resolve` |

### 3.15 批处理（1）

| 工具 | 执行方式 | 输入摘要 | 结果 | 返回摘要 / 原因 |
| --- | --- | --- | --- | --- |
| `batch` | 真实执行 | ① `dry_run`；② 事务 `create_gameobject → set_transform($c.hierarchyPath) → rename_gameobject`；③ 事务含 `add_component NoSuchComponentType`；④ 非事务 7 个 `create_folder` | 通过 | ① `applied:0 valid:true`；② `applied:3`，`McpTxDone` 存在；③ `applied:1 reverted:true`，`McpTxFail` 不存在；④ `applied:7` |

## 4. 端到端链路

| 链路 | 步骤与结果 |
| --- | --- |
| 脚本编译 → 附加 | `create_script` → `write_text_file confirm`（替换为含 `[HotReload]` 的类）→ `recompile`（`up_to_date`，导入已触发编译）→ `recompile_status up_to_date failed:false` → `attach_script type=` 与 `script=` 各成功一次 → `get_serialized_fields counter=0` → `set_serialized_field 42` → 回读 42 |
| 测试程序集 | `write_text_file` asmdef（`PipelineMcpTest.Editor`，`UNITY_INCLUDE_TESTS`）+ 测试脚本 → `recompile completed` → `list_tests mode=editor Count=2` → `run_tests filter=PipelineMcpTest` → `completed total 2 passed 2` → `test_status` 同结果 |
| 批处理三情形 | `dry_run` 不创建对象；事务成功批次的对象可被 `find_gameobjects` 找到；含故意失败操作的事务批次回滚后其对象不存在（见 3.15） |
| Play 往返 | `editor_play` → 8 s → `editor_status playing` → `get_console_logs severity=error` 0 条 → `editor_pause` ×2 → `editor_stop` → 5 s → `ready/stopped`，GameStart 与临时场景 `isDirty=false` |
| 热重载 | 默认设置：`reload_file` / `reload_file_editor_interpreter` 在编辑模式与 Play 均失败；`eval` 手动 `HotReloadRegistry.RegisterReloadableMethod(Tick)` 后成功（+100 / +1000）；`set_runtime_pipeline_settings enableInBuilds=true` 后再次 Play：`RuntimePipelineBootstrap.Instance` 存在、注册表含 `McpTestBehaviour.Tick`，`reload_file` 直接成功（+7） |

## 5. 发现的问题

1. **`clear_baked_lighting` / `clear_navmesh` / `clear_occlusion_culling` 的 `dry_run` 被 `confirm` 门先拦截。** 源码 `Editor/Commands/Baking/*.cs` 中 `if (!confirm) throw` 位于 `if (dryRun)` 之前（LightingBakeCommands.cs:141–145、NavMeshBakeCommands.cs:142–145、OcclusionBakeCommands.cs:154–157），与 `safety-and-mutations.md`"`dry_run` 优先于 `confirm`"的约定相反；单独 `dry_run=true` 返回 400，只有 `confirm=true` + `dry_run=true` 才能预览。
2. **`bake_lighting` / `bake_navmesh` / `bake_occlusion_culling` 的 `confirm` 不强制。** 参数描述明确为 "Accepted for parity; not required"，即不带 `confirm` 也不带 `dry_run` 的调用会真实烘焙（本次未故意验证，按描述记录）。与其它破坏性命令的约定不一致，Agent 必须显式传 `dry_run=true` 才安全。
3. **`clear_console` 只清空 `get_console_logs` 的缓冲。** 调用后 `get_console_logs total=0`，但 `console`（带 seq / cursor 的运行时命令）仍返回同样 6 条历史条目；两套缓冲彼此独立。
4. **`capture_game_view` / `capture_scene_view` 的 `save_path` 按 authoring root 解析。** 传 `Temp/mcp-test/x.png` 实际写到 `Assets/Temp/mcp-test/x.png`（`savedPath` 字段可见），并生成 `.meta`、进入 git；而 `screenshot.output` 是项目根相对。文档描述均为 "project-relative"。
5. **`get_component_properties` 对 Quaternion / LayerMask 输出 `<unsupported:…>`。** `Transform.m_LocalRotation`、`Rigidbody.m_InertiaRotation/m_IncludeLayers/m_ExcludeLayers` 不可读；`get_serialized_fields` 对同一字段可正常输出 xyzw。
6. **默认设置下编辑器 Play 模式的 in-place 热重载不可用。** `[HotReload]` 方法的注册由 `RuntimePipelineDriver.Awake` 完成（`Runtime/PlayerSupport/RuntimePipelineDriver.cs:104`），而该驱动只在 Project Settings → Pipeline → Runtime 的 `enableInBuilds=true` 时由 `RuntimePipelineBootstrap` 创建（`RuntimePipelineBootstrap.cs:39`）。HGame 当前为 `false`，因此 `reload_file` 报 "not registered as [HotReloadWithOverrides]"，`reload_file_editor_interpreter` 报 "no woven dispatch target"（实际 IL 已编织，诊断误导）。实测把 `enableInBuilds` 改为 `true` 后 Play，`reload_file` 直接成功。`hot-reload.md` 未提及此前提。
7. **`HotReloadHelper.ExecuteWithHotReload<T>(…, Func<object>, …)` 命中 override 后固定返回 `null`。** `Runtime/HotReload/HotReloadHelper.cs:30` 注释 "Hot reload methods currently don't return values"，与文档"支持 value-returning 重载"矛盾；`(int)` 拆箱直接 NRE。`HotReloadRegistry.TryInvokeHotReloadResult` 能正确取得 override 返回值（300）。
8. **`hotreload_status` / `cleanup_hotreload` / `runtime_status` / `quit` 等 `RuntimeOnly=true` 命令不在编辑器目录（149）中，但文档 `index.md` 列出了它们。** `unity command hotreload_status` 仍可对编辑器执行（返回 Reloadable 1 / Overrides 1）；`cleanup_hotreload` 要求 `assemblyDir` 必填。
9. **URP 材质上 `set_material_properties` 的 `renderQueue` / `enableKeywords` 被 URP 后处理重置。** 命令返回 `applied` 且磁盘写入正确（`m_CustomRenderQueue: 2500`、`m_ValidKeywords: - _EMISSION`），但 URP `MaterialPostprocessor.OnPostprocessAllAssets → InitializeLatest → ShaderUtils.UpdateMaterial` 按 `_Surface` / `_QueueOffset` / `globalIlluminationFlags`（`m_LightmapFlags: 4` = EmissiveIsBlack）重新计算，内存对象与 `get_material_properties` 立即读回 `renderQueue 2000`、`enabledKeywords []`，强制重导入后队列亦回到 2000。对照 Built-in `Standard` 材质：关键字与队列写入并在重导入后保持。对 URP 材质应改用语义属性（`_Surface`、`_QueueOffset`、`_EmissionColor` + 关闭 EmissiveIsBlack）并在设置后回读核对。
10. **`occlusion_bake_status.stats.umbraDataSizeBytes` 恒为 0。** 烘焙完成后 `StaticOcclusionCulling.umbraDataSize = 14912`。
11. **`set_lighting_settings` 在无 LightingSettings 时于活动场景上新建内存对象。** 返回 `settingsSource: created`，对象名 `PipelineLightingSettings`、`assetPath` 为空、不落盘，并使活动场景 dirty；若活动场景是 `GameStart` 会把它标脏。使用前应先把临时场景设为活动场景。
12. **`set_*` 项目设置以 `confirm=true` 写入后，即使恢复原值，`ProjectSettings/*.asset` 也会被 Unity 6000.3 以新序列化格式重写。** `AudioManager.asset`（新增 4 字段）、`DynamicsManager.asset`（serializedVersion 14→23）、`TimeManager.asset`（`Fixed Timestep` 变为 `m_Count/m_Rate`）、`TagManager.asset`（serializedVersion 2→3，新增 `m_RenderingLayers`）出现 git diff；语义值一致。`set_runtime_pipeline_settings` 首次 confirm 会新建 `ProjectSettings/Packages/com.unity.pipeline/RuntimePipelineConfig.json`。本次以 `git checkout` 恢复字节原样并删除该 JSON。
13. **`instanceId` 在域重载（Play / 编译）后失效。** Play 前 `/McpCube` 的 `-32816` 在 Play 后变为 `63900`，`set_selection instance_ids` 返回 `unresolved`；跨域重载定位应使用 `hierarchyPath` 或 `globalId`。
14. **历史控制台错误（2026-09-10，非本次产生）**：Git Bash 下 `unity command set_transform /__CLI_Test_Cube` 的参数被 MSYS 转成 `C:/Program Files/Git/__CLI_Test_Cube`（以 `/` 开头的层级路径需用 `MSYS_NO_PATHCONV=1` 或双斜杠）；`find_assets type=Prefab` 不支持（应为 `GameObject`）；Pipeline 服务端 `GET /api/commands` 在客户端提前断开时抛 "Cannot be changed after headers are sent"（`BasePipelineServer.cs:992`）。
15. **`create_asset` 的 `dry_run` 在目标已存在时不返回预览。** 直接返回 400 "An asset already exists… Pass confirm=true"。
16. **`run_tests async_tests=true` 同步返回了完整结果**（`status: completed`），未出现 `running` / job 状态；短测试下行为合理，但与参数描述"return immediately, poll /test-status"不符。

另有两点非缺陷的使用提示：`batch` 的 JSON 参数中若混入 `\r` 等控制字符，命令名会被视为未知命令（客户端输入问题）；`create_scene additive=true` 会把新场景设为活动场景，需要时用 `set_active_scene` 切回。

## 6. 附录：`unity` CLI 命令行冒烟

均在 `F:\Test\DGame` 以 `UNITY_NO_BANNER=1 UNITY_NON_INTERACTIVE=1` 执行。

| 命令 | 退出码 | 关键返回 |
| --- | --- | --- |
| `unity status --json` | 0 | `success:true`，`instances[0]`：port 7800、project `F:\Test\DGame\GameUnity`、version 6000.3.10f1、pid 19256、state ready |
| `unity list --project-path GameUnity --json` | 0 | `success:true`，`data.count=149`，`target.port=7800`，每项含 name / description / group / parameters |
| `unity command editor_status --project-path GameUnity --json` | 0 | `data.result.status=ready`、`playMode=stopped`、`unityVersion=6000.3.10f1`，`target.port=7800` |
| `unity command --help` | 0 | 用法 `unity command|cmd [options] [command] [args...]`；选项含 `--project-path`、`--runtime`、`--timeout`、`--detach`、`--detail`、`--query`、`--tag`、`--group_by`、`--sort`、`--offset`、`--limit`、`--caller`、`--skill` |
| `unity mcp configure --list` | 0 | 列出 17 个受支持客户端（claude、claude-code、cursor、vscode、codex、kiro、zed …）及各自配置文件路径与状态 |
| `unity job --help` | 0 | 用法 `unity job [options] [command]`，"Manage detached Editor command jobs" |

补充：`unity list --json` 输出为 UTF-8，在 Windows 默认 GBK 控制台下需以 `encoding='utf-8'` 读取；`unity list` 与 `/api/commands`（Bearer token 来自 `GameUnity/Library/Pipeline/.unity-pipeline-port` 的 `evalToken`）返回的命令集合一致（149）。
