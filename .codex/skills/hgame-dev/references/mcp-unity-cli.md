# MCP Unity CLI（com.unity.pipeline）工具指南

> **适用场景**：通过 `.mcp.json` 中的 `unity-cli` 服务器（Unity 官方 `com.unity.pipeline` 包 + `unity` CLI）驱动已打开的 HGame `GameUnity` 工程：资产 / 场景 / GameObject / Prefab / 脚本 / 动画 / 材质 / 烘焙 / 构建测试 / 项目设置 / 包管理 / 热重载 | **关联文档**：[mcp-visual.md](mcp-visual.md)（材质 / Shader / 纹理 / 动画 / VFX）、[naming-rules.md](naming-rules.md)（UI 节点前缀）、[architecture.md](architecture.md)（分层落位）、[troubleshooting.md](troubleshooting.md)

## 已核实来源

- 包：`GameUnity/Packages/manifest.json` 的 `"com.unity.pipeline": "0.6.0-exp.1"`，包缓存 `GameUnity/Library/PackageCache/com.unity.pipeline@9bb4172c603c`，随包文档在 `Documentation~/`（`index.md`、`safety-and-mutations.md`、`hot-reload.md`、`commands/*.md`）。
- 客户端：`unity` CLI 1.0.0-beta.9；`.mcp.json` 配置 `"command": "unity", "args": ["mcp", "--project-path", "GameUnity"]`，环境变量 `UNITY_NO_BANNER=1`、`UNITY_NON_INTERACTIVE=1`。
- 连接：编辑器内 `Window/Pipeline/Settings...`（`Assets/Settings/Pipeline/EditorPipelineManager.asset`，`m_AutoStart: 1`）启动本地 HTTP 服务（本机为 `127.0.0.1:7800`）；`unity status --json` 列出在线实例，`unity list --project-path GameUnity --json` 返回 **149** 个 `built-in` 工具，与 MCP 客户端的 `mcp__unity-cli__*` 一一对应。Bearer token 在 `GameUnity/Library/Pipeline/.unity-pipeline-port` 的 `evalToken`。
- 唯一的 MCP：`com.coplaydev.unity-mcp`（CoplayDev unity-mcp）已于提交 `7600c17e` 从工程移除，原先描述它的 `mcp-tools.md` 已删除；本文档与 [mcp-visual.md](mcp-visual.md) 是 HGame 唯一的 MCP 指南。不要再使用 `manage_*` 系列聚合工具名，工具名一律以 `unity list` 输出为准。
- 本文档结论来自 2026-09-12 在 Unity 6000.3.10f1 上对全部 149 个工具的实测，详见 `docs/unity-cli-mcp-test-report.md`。

## 安全门

- **`confirm` / `dry_run` 约定**（`safety-and-mutations.md`）：破坏性或覆盖性命令先 `dry_run=true` 取预览，再 `confirm=true` 执行；不带 `confirm` 会被拒绝（HTTP 400 或 `status: rejected` / `Refused`）。资产、包、项目设置写入 **不在 Unity Undo 内**；场景 / 对象写入在 `AuthoringUndoScope` 中合并为一步 Undo。
- **实测例外**：
  - `clear_baked_lighting` / `clear_navmesh` / `clear_occlusion_culling` 的 `confirm` 检查先于 `dry_run`，单独 `dry_run=true` 也返回 400；要预览必须 `confirm=true` + `dry_run=true`。
  - `bake_lighting` / `bake_navmesh` / `bake_occlusion_culling` 的 `confirm` "accepted for parity; not required"：**不带 `dry_run` 就会真实烘焙**，务必显式传 `dry_run=true` 做校验。
  - `create_asset` / `write_text_file` 在目标已存在时无 `confirm` → 400；`create_asset` 的 `dry_run` 此时不返回预览。
  - `set_runtime_pipeline_settings` 在 Play 模式被拒绝（设计如此）。
- **在 HGame 工程中永不真实执行**（只 `dry_run` / 无 `confirm` 验证拒绝门）：
  - `switch_build_target`：触发全量重导入 + 域重载，无 `dry_run` 参数。
  - `package_add` / `package_remove`：改动 `manifest.json` / `packages-lock.json`，影响 HybridCLR / YooAsset 依赖链；改包走人工评审。
  - `bake_lighting` / `clear_baked_lighting`：纯 UGUI 工程无烘焙需求，且清除不可撤销。
  - `set_graphics_settings`：会把渲染管线从 `Assets/Settings/URP/HGame_URPAsset.asset` 切走（见 `architecture.md` 的 URP 说明）。
  - `set_player_settings` 的 `scriptingBackend` / `apiCompatibilityLevel`：触发域重载且影响 IL2CPP / HybridCLR 配置；`bundleVersion` / `companyName` / `productName` 可改。
  - `build`：HGame 打包走 `HGame Tools/Build/*` 与 `build-pipeline.md` 的 ReleaseTools 链路，不用通用 `BuildPipeline`。
- **副作用提示**：`editor_focus` 会把 Unity 拉到前台；`clear_console` 只清 `get_console_logs` 的缓冲，`console`（seq/cursor）缓冲不受影响；`recompile`、`editor_play`、`package_resolve` 可能触发域重载，之后轮询 `editor_status.status == "ready"`；域重载后 `instanceId` 全部失效，跨域重载定位用 `hierarchyPath` 或 `globalId`。

## 路径与 authoring root

- 资产类参数接受 **authoring root 相对路径**，默认 root 为 `Assets`，`Assets/` 前缀可省略（`PipelineMcpTest/Sub` ≡ `Assets/PipelineMcpTest/Sub`）；场景路径的 `.unity`、Prefab 的 `.prefab` 可省略。`..` 与越出 root 的路径被拒绝。`set_authoring_root` 可临时收窄到子目录（用完必须 `set_authoring_root Assets` 复原）。
- **例外**：`screenshot.output`、`run_script.file`、`eval_file.file`、`import_asset.source`、`build.outputPath`、`batch` 内同名参数按 **项目根**（`GameUnity/`）或绝对路径解析；`capture_game_view` / `capture_scene_view` 的 `save_path` 实测按 **authoring root** 解析（传 `Temp/x.png` 会写到 `Assets/Temp/x.png` 并产生 `.meta`），不要用它写到 `Assets` 之外。
- `run_script` / `eval_file` 的源文件放在 `Assets/` 之外（约定 `GameUnity/AgentScripts/`，用完删除），不触发导入与域重载；`run_script` 编译到内存程序集并执行 `Namespace.Type.Method` 静态入口，`args` 支持基元、枚举名、`string[]` 与 GameObject 句柄（`"/McpCube"`）。
- HGame 落位不变：热更资源 `Assets/BundleAssets/`、UI Prefab `Assets/BundleAssets/UI/`、热更脚本 `Assets/Scripts/HotFix/GameLogic/`（见 `architecture.md`）。`create_script` 生成的是全局命名空间 MonoBehaviour 模板（进入 `Assembly-CSharp`），HGame 业务脚本应改用 `write_text_file` 写入符合 `naming-rules.md` 的内容并放到正确程序集目录。
- 临时试验资源统一放 `Assets/PipelineMcpTest/`（或同类临时目录），场景以 `additive=true` 创建 / 打开，结束后 `delete_asset confirm=true` 整目录删除；不要在 `GameStart.unity` 上做实验（非 additive `open_scene` 会静默丢弃未保存修改，`save_all` 会把它写盘）。

## 按分类的工具速查（149）

`A` = 加法 / 只读，直接调用；`C` = 需要 `confirm`；`D` = 建议先 `dry_run`；`P` = 只在 Play 或需前置条件；`X` = 本工程不真实执行；`N` = 需要未安装的包 / 未连接 Player。

### 编辑器生命周期与可观测性（16）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `editor_status` | A | `status` / `playMode` / `compiling` / `domainReloadInProgress`；域重载后轮询它 |
| `editor_focus` | A | 把 Unity 窗口拉到前台 |
| `editor_play` / `editor_stop` | A | 进出 Play；进入后等 5–8 s 再操作；EditorSimulateMode 下不联网 |
| `editor_pause` | A | 切换暂停（toggle） |
| `menu` | A | 无 `path` 列出 849 个菜单项（含 `HGame Tools/*`、`HybridCLR/*`、`YooAsset/*`）；传 `path` 执行 |
| `screenshot` | A | `view=game|scene`，`output` 项目根相对 |
| `set_autotick` | A | 后台保持编辑器 tick（默认已开） |
| `get_console_logs` | A | `severity=error|warning|log|all`，最新在前 |
| `console` | A | seq / `cursor` 增量跟随（`since=<cursor>`） |
| `clear_console` | A | 只清 `get_console_logs` 缓冲 |
| `get_performance_stats` | A | drawCalls / 内存 / 帧时间 |
| `audit` / `audit_status` | N | Project Auditor 未安装 → `unavailable` |
| `report_evals` | A | `eval` 用量指纹排名 |
| `get_authoring_root` / `set_authoring_root` | A | 读 / 改路径沙箱根，改完必须复原 |

### 资产与文件（12）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `create_folder` | A | 递归创建 |
| `create_asset` | C(覆盖) | `type` 为可创建的 `UnityEngine.Object`（`UnityEngine.Material` + `shader`、`UnityEngine.Rendering.VolumeProfile`、自定义 ScriptableObject 全名） |
| `write_text_file` / `read_text_file` | C(覆盖) / A | UTF-8；可写 `.cs` / `.asmdef` / `.json`，写入后自动导入 |
| `find_assets` | A | `type` 用短名或全名（Prefab 用 `GameObject`），`search_in` 限定目录 |
| `copy_asset` / `move_asset` / `rename_asset` | D | copy 新 GUID；move / rename 保留 GUID |
| `import_asset` | D, C(覆盖) | `source` 绝对路径 → root 相对 `path` |
| `get_import_settings` / `set_import_settings` | A / D | 按 importer 类型返回字段；`platform` 可取 Android 等；未知键在 `unknown[]` |
| `delete_asset` | D, C | 可整目录删除（含 `.meta`） |

### 场景（9）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `create_scene` | A | `additive=true` 时自动成为活动场景；`template=empty|default` |
| `open_scene` | A | 非 additive 会关闭其他场景并丢弃未保存修改 |
| `save_scene` / `save_all` | A | `save_all` 只保存 dirty 场景，返回 `scenes[]` |
| `list_open_scenes` | A | name / path / isDirty / isActive / rootCount |
| `set_active_scene` | A | 新对象、烘焙、LightingSettings 都作用于活动场景 |
| `get_scene_hierarchy` | A | 树含 instanceId / hierarchyPath / components |
| `add_scene_to_build` / `remove_scene_from_build` | A | 幂等，`enabled` 可 false |

### GameObject 与组件（14）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `create_gameobject` / `create_gameobjects` | A | `primitive=cube|sphere|capsule|cylinder|plane|quad`；批量支持 positions / rotations / scales |
| `find_gameobjects` | A | name / tag / type / hierarchy_path 可组合；`include_inactive` |
| `set_transform` / `set_parent` / `set_active` / `set_tag` / `set_layer` / `rename_gameobject` / `delete_gameobject` | A | 目标用 `hierarchyPath`（`/Root/Child`）最稳；Tag 必须已存在 |
| `add_component` / `remove_component` | A | 短名或全名 |
| `get_component_properties` / `set_component_properties` | A | 序列化字段名（`m_Mass`）；枚举传名字；Quaternion / LayerMask 读出为 `<unsupported>`，用 `get_serialized_fields` 代替 |

### Prefab（7）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `create_prefab` | A | 场景对象 → 资产，源对象变实例 |
| `instantiate_prefab` | A | 进入活动场景（或 `scene_path`） |
| `create_prefab_variant` | A | 基于已有 prefab |
| `apply_prefab_overrides` / `revert_prefab_overrides` | A | 实例 ↔ 资产 |
| `unpack_prefab` | A | `completely` 控制层级 |
| `save_prefab_contents` | A | 隔离 stage 内 `rename_child` / `set_active_child`，嵌套安全 |

### 脚本、编译与热重载（13）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `create_script` | A | 模板到 `Assembly-CSharp`；HGame 业务脚本用 `write_text_file` |
| `recompile` / `recompile_status` | A | 写 `.cs` 后导入已触发编译时返回 `up_to_date`；域重载后轮询 `editor_status` |
| `attach_script` | A | `type=` 或 `script=` 二选一 |
| `get_serialized_fields` / `set_serialized_field` | A | 支持 `format=value`、数组 `x.Array.data[i]`、ObjectRef |
| `run_script` | A | 内存编译 + 静态入口，无域重载；`dry_run` 只编译 |
| `eval` / `eval_file` | A | Roslyn 即时求值，编译错误返回 `diagnostics` |
| `reload_file` / `reload_file_editor_interpreter` | P | in-place 热重载 `[HotReload] public void` 方法；**前提**：Play 模式且 Project Settings → Pipeline → Runtime `enableInBuilds=true`（否则 `RuntimePipelineDriver` 不启动、方法不注册，报 "not registered" / "no woven dispatch target"） |
| `reload_file_override` | A | `[HotReloadOverrideMethod("Type.Method")]` 静态方法，编辑模式即可；`HotReloadHelper.ExecuteWithHotReload(Func<object>)` 命中 override 时返回 null，取返回值用 `HotReloadRegistry.TryInvokeHotReloadResult` |
| `reload_file_player_interpreter` | N | 需要已连接的 Development Player |

### 动画与 Timeline（14）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `create_animation_clip` / `set_animation_curve` / `get_animation_clip` / `remove_animation_curve` | A / D / A / C | 曲线绑定 `type`+`property`+`path`；省略切线为 0（非 Auto） |
| `create_animator_controller` / `add_animator_parameter` / `add_animator_layer` / `add_animator_state` / `add_animator_transition` / `get_animator_controller` | A | 错误码 `duplicate_parameter`、`layer_not_found`；条件 mode 与参数类型校验（Float/Int 用 Greater/Less/Equals/NotEqual） |
| `create_timeline` / `add_timeline_track` / `add_timeline_clip` / `get_timeline` | N | `com.unity.timeline` 未安装 → `package_not_found` |

### 材质与 Shader（4）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `list_shaders` / `get_shader_properties` | A | `filter` 子串；属性含 type / range / flags |
| `get_material_properties` | A | 返回 `rawRenderQueue`（-1 = 继承）与 `enabledKeywords` |
| `set_material_properties` | D | Color 支持 `#RRGGBBAA` / 数组，Texture 传 `{path}`；**URP 材质**的 `renderQueue` / `enableKeywords`（如 `_EMISSION`）会被 URP `MaterialPostprocessor` 按 `_Surface` / `_QueueOffset` / GI 标志重算而失效，改用语义属性并回读核对；Built-in Shader 不受影响 |

### 烘焙（17）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `get_lighting_settings` / `set_lighting_settings` | A / D | 场景无 LightingSettings 时 `set` 会在**活动场景**上新建内存对象并标脏 |
| `bake_lighting` / `clear_baked_lighting` | X | 只 `dry_run`；`clear_*` 预览需 `confirm+dry_run` |
| `lighting_bake_status` / `cancel_lighting_bake` | A | 只读 / 无烘焙时 `cancelled:false` |
| `get_navmesh_settings` / `set_navmesh_settings` | A / D | 默认 agent 参数 |
| `bake_navmesh` / `navmesh_bake_status` / `cancel_navmesh_bake` / `clear_navmesh` | D, C | 异步，轮询 status；结果 `NavMesh.asset` 写到场景同名文件夹 |
| `bake_navmesh_surfaces` | N | AI Navigation 未安装 |
| `bake_occlusion_culling` / `occlusion_bake_status` / `cancel_occlusion_bake` / `clear_occlusion_culling` | D, C | 异步；`stats.umbraDataSizeBytes` 恒 0（用 `eval` 读 `StaticOcclusionCulling.umbraDataSize`） |

### 导航与选择（3）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `get_selection` / `set_selection` | A | `paths` 选资产、`instance_ids` 选场景对象（域重载后失效） |
| `search` | A | Unity Search 语法（`t:Material`、`h:`、`p:`） |

### 截图（3）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `screenshot` | A | 写 PNG 文件，项目根相对 |
| `capture_game_view` | A | `source=camera`（漏 Overlay UI）/ `screen`（Play 中含 UI）；不传 `save_path` 返回内联 base64 |
| `capture_scene_view` | A | Scene View 渲染 |

### 构建与测试（12）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `build` | X | `dry_run` 校验目标与路径；未安装目标返回 `validationErrors` |
| `build_status` | A | `idle` / `queued` / `building` / `completed` |
| `switch_build_target` | X | 无 `dry_run` |
| `switch_build_target_status` | A | `idle` / `switching` / `completed` |
| `list_build_targets` / `list_build_profiles` | A | `isInstalled` 标记 |
| `get_build_settings` / `set_build_settings` | A / D | `developmentBuild` 等开关，不管理场景列表 |
| `list_tests` / `run_tests` / `test_status` / `cancel_tests` | A | `filter` 子串、`mode=editor|playmode|all`；短测试会同步返回 `completed` |

### 项目设置（18）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `get_audio_settings` / `set_audio_settings` | A / D, C | volume / rolloffScale / dopplerFactor |
| `get_graphics_settings` / `set_graphics_settings` | A / X | 读当前 URP 资产；不切换 |
| `get_input_settings` / `set_input_settings` | A / D, C | 旧 Input Manager 轴 |
| `get_physics_settings` / `set_physics_settings` | A / D, C | 重力 / 求解迭代 / 弹跳阈值 |
| `get_player_settings` / `set_player_settings` | A / D, C | 只改 bundleVersion / companyName / productName |
| `get_quality_settings` / `set_quality_settings` | A / D, C | level / vSync / aa |
| `get_tags_layers` / `set_tags_layers` | A / D, C | `addTags` / `removeTags` / `setLayers`（8–31） |
| `get_time_settings` / `set_time_settings` | A / D, C | fixedDeltaTime / timeScale |
| `get_runtime_pipeline_settings` / `set_runtime_pipeline_settings` | A / D, C | `enableInBuilds` 决定 Play 时是否启动 `RuntimePipelineDriver`（热重载前提）；首次写入新建 `ProjectSettings/Packages/com.unity.pipeline/RuntimePipelineConfig.json` |

`set_*` 以 `confirm=true` 写入后会用当前 Unity 版本格式重写对应 `ProjectSettings/*.asset`（serializedVersion 升级等），即使恢复原值也会出现 git diff；只做试验时事后用 `git checkout -- GameUnity/ProjectSettings/<file>` 复原。

### 包管理（6）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `package_list` / `package_search` / `package_status` | A | `scope=installed|available|all`；search 访问 registry |
| `package_add` / `package_remove` | X | 只 `dry_run` 看 plan |
| `package_resolve` | A | 重解析 manifest，可能域重载 |

### 批处理（1）

| 工具 | 标记 | 用途 / 实测备注 |
| --- | --- | --- |
| `batch` | A | 最多 200 个 op；`transactional=true`（默认）失败整体回滚且合并为一步 Undo；后续 op 用 `"$<id>.<field>"` 引用前序结果（如 `"$c.hierarchyPath"`）；含资产 / 设置写入的批次必须 `transactional=false`；不能嵌套 batch，也不能包含 `recompile` / `editor_play` / `eval` / `open_scene` 等 |

## HGame 落位与 uGUI Prefab 流程

MCP 工具接收的是 Unity 资产路径（`Assets/` 相对，前缀可省），不要写宿主机绝对路径。HGame 的落位与 `architecture.md` 一致：

| 类型 | 路径 |
|------|------|
| UI 窗口 / Widget 业务脚本 | `Assets/Scripts/HotFix/GameLogic/UI/<模块名>/` |
| UIModule 与 UI 基础模块 | `Assets/Scripts/HotFix/GameLogic/Module/UIModule/` |
| 业务模块 | `Assets/Scripts/HotFix/GameLogic/Module/<ModuleName>/` |
| 事件接口 | `Assets/Scripts/HotFix/GameLogic/IEvent/` |
| Luban 生成代码 | `Assets/Scripts/HotFix/GameProto/`，自动生成，勿手改 |
| 热更资源根目录 | `Assets/BundleAssets/` |
| UI Prefab | `Assets/BundleAssets/UI/` |
| UI 图集 / 原始图 | `Assets/BundleAssets/UIRaw/` |
| 场景资源 | `Assets/BundleAssets/Scenes/` |
| 材质 / Shader / 特效 | `Assets/BundleAssets/Materials/`、`Assets/BundleAssets/Shaders/`、`Assets/BundleAssets/Effects/` |

HGame 运行时 UI 走 `UIWindow` / `UIWidget` / `UIModule` 和 uGUI Prefab（见 `ui-lifecycle.md`）。用 unity-cli 拼装 uGUI Prefab 的流程：

1. `create_gameobject name=<XxxUI>` 建临时根节点（在 additive 临时场景或当前场景均可）。
2. `add_component` 依次加 `Canvas`、`CanvasScaler`、`GraphicRaycaster`；子节点用 `create_gameobject parent=/<XxxUI>` + `add_component`（`RectTransform` 随 uGUI 组件自动添加；常用 `Image`、`Button`、`RawImage`、`Text`、`TMP_Text`、`ScrollRect`、`GridLayoutGroup`、`CanvasGroup`）。
3. 节点命名严格遵守 [naming-rules.md](naming-rules.md#ui-节点前缀)，例如 `m_btnClose`、`m_tfContent`、`m_dropDownQuality`；脚本生成器按前缀绑定字段。
4. `set_component_properties target=/<XxxUI> type=CanvasScaler` 设置适配：`m_UiScaleMode=ScaleWithScreenSize`、`m_ReferenceResolution=[1920,1080]`（按项目实际为准）、`m_ScreenMatchMode=MatchWidthOrHeight`、`m_MatchWidthOrHeight=0.5`。
5. 锚点规则：全屏背景锚点拉伸到四角（`m_AnchorMin=[0,0]`、`m_AnchorMax=[1,1]`）；按钮、图标等固定尺寸控件用单点锚点并靠近所在屏幕区域，避免不同分辨率下偏移。
6. `create_prefab source=/<XxxUI> path=BundleAssets/UI/<XxxUI>` 保存到 `Assets/BundleAssets/UI/<XxxUI>.prefab`，然后 `delete_gameobject` 删除临时对象（如果用了临时场景，一并关闭并删除）。
7. 绑定脚本走 HGame 现有生成链路（`menu` 执行 `GameObject/ScriptGenerator/*`，或按 `ui-lifecycle.md` 手写），脚本放 `Assets/Scripts/HotFix/GameLogic/UI/<模块名>/`。

Prefab 根节点应至少具备：

```text
XxxUI.prefab
├── Canvas
├── CanvasScaler
├── GraphicRaycaster
└── m_tfContent / m_btnClose / ...
```

资源改动后要确认 YooAsset 收集规则和运行时地址：HGame 资源地址通常等于 `Assets/BundleAssets` 下的文件名（不含路径和扩展名），但不要假设文件名一定就是最终 Address，改动前用 `find_assets` / `search` 搜索现有加载点并核对 `AssetBundleCollectorSetting.asset`。

## 常用菜单项

`menu` 不传 `path` 时列出全部菜单（本工程约 849 项），执行前先确认路径存在：

| 操作 | 菜单路径 | 说明 |
|------|----------|------|
| UI 脚本生成 | `GameObject/ScriptGenerator/UIProperty*`、`.../UIPropertyAndListener*` | 先选中 Prefab 根节点（`set_selection`），再执行 |
| Luban 转表 | `HGame Tools/Luban/转表` | 配置表结构与数据优先走 `luban-dev` skill，不凭菜单名猜测 |
| HybridCLR 生成 | `HybridCLR/Generate/All` 及 `HybridCLR/CompileDll/*` | 先确认当前平台（`get_build_settings`）；耗时长 |
| 保存项目 | `File/Save Project` | 场景另用 `save_scene` / `save_all` |
| 刷新资源库 | `Assets/Refresh` | 无副作用 |

## 常见错误

| 错误 | 正确做法 |
|------|----------|
| 使用 `Assets/GameScripts/...` | HGame 使用 `Assets/Scripts/HotFix/GameLogic/...` |
| 使用 `Assets/AssetRaw/...` | HGame 热更资源使用 `Assets/BundleAssets/...` |
| 直接改 `GameProto` 生成代码 | 修改配置源 / 模板并导表（`luban-dev`） |
| 重名对象直接按 name 修改 | 先 `find_gameobjects` 取 `hierarchyPath` 或 `globalId`；域重载后 `instanceId` 失效 |
| 用 `create_script` 模板直接写业务脚本 | 模板进入 `Assembly-CSharp`；HGame 业务脚本用 `write_text_file` 写符合 `naming-rules.md` 的内容到正确程序集目录 |
| 沿用已移除的旧 MCP 包的 `manage_*` 聚合工具名 | 该包已从工程移除（见「已核实来源」）；对应操作见本文档「按分类的工具速查」 |
| `capture_*` 的 `save_path` 写到 `Temp/` | 会落到 `Assets/Temp/` 并产生 `.meta`；文件截图用 `screenshot` |

## 端到端链路模板

```text
# 1. 新脚本 → 编译 → 挂载
write_text_file path=Scripts/HotFix/GameLogic/<Module>/<Name>.cs contents=<符合 naming-rules 的代码>
recompile → 轮询 recompile_status 到 completed|up_to_date（域重载后 editor_status.status==ready）
attach_script target=/<Path> type=<Namespace.Type>
get_serialized_fields target=/<Path> component=<Type> → set_serialized_field … → 回读

# 2. 测试程序集 → 跑测试
write_text_file <Tests>/<Asm>.asmdef（references UnityEngine.TestRunner/UnityEditor.TestRunner，defineConstraints UNITY_INCLUDE_TESTS）
write_text_file <Tests>/<Test>.cs → recompile → list_tests mode=editor → run_tests mode=editor filter=<Asm 或类名> → test_status

# 3. 批量搭建层级（事务）
batch transactional=true operations=[
  {id:"root", command:"create_gameobject", params:{name:"TempRoot"}},
  {id:"c",    command:"add_component",     params:{target:"$root.hierarchyPath", type:"Canvas"}},
  {id:"t",    command:"set_transform",     params:{target:"$root.hierarchyPath", position:[0,0,0]}}]
→ 任一失败整体回滚；对象再用 create_prefab 落到 Assets/BundleAssets/UI/<Name>.prefab，删除临时对象

# 4. Play 往返检查
get_console_logs severity=error → editor_play → 等 5–8 s → editor_status.playMode==playing
→ get_console_logs severity=error（抓运行期报错）→ editor_stop → 轮询 editor_status.status==ready && playMode==stopped

# 5. 热重载（需 enableInBuilds=true）
set_runtime_pipeline_settings confirm=true settings={enableInBuilds:true}（编辑模式下改）→ editor_play
→ 编辑 [HotReload] public void 方法体 → reload_file filename=Assets/.../X.cs → 验证 → editor_stop → 复原 enableInBuilds

# 6. 域重载后的定位
任何 recompile / editor_play / package_resolve 之后：editor_status 轮询到 ready；重新 find_gameobjects 取新 instanceId，或一直用 hierarchyPath / globalId
```

## 已知问题

来自 `docs/unity-cli-mcp-test-report.md` 第 5 节（2026-09-12，com.unity.pipeline 0.6.0-exp.1）：

1. `clear_baked_lighting` / `clear_navmesh` / `clear_occlusion_culling`：`confirm` 检查先于 `dry_run`，单独 `dry_run` 被拒。
2. `bake_lighting` / `bake_navmesh` / `bake_occlusion_culling`：`confirm` 不强制，不带 `dry_run` 即真实烘焙。
3. `clear_console` 不清 `console` 命令的 seq 缓冲。
4. `capture_game_view` / `capture_scene_view` 的 `save_path` 落到 `Assets/` 下（authoring root），与 `screenshot.output` 不一致。
5. `get_component_properties` 对 Quaternion / LayerMask 输出 `<unsupported:…>`。
6. 默认 `enableInBuilds=false` 时 Play 模式 in-place 热重载不可用（`RuntimePipelineDriver` 未启动）；`reload_file_editor_interpreter` 的 "no woven dispatch target" 诊断误导。
7. `HotReloadHelper.ExecuteWithHotReload(Func<object>)` 命中 override 后返回 null（`HotReloadHelper.cs:30`）。
8. `hotreload_status` / `cleanup_hotreload` / `runtime_status` / `quit` 为 RuntimeOnly，不在 149 工具目录中但文档列出；`cleanup_hotreload` 要求 `assemblyDir`。
9. URP 材质的 `renderQueue` / 关键字被 URP 后处理重置（Built-in Shader 正常）。
10. `occlusion_bake_status.stats.umbraDataSizeBytes` 恒 0。
11. `set_lighting_settings` 在无设置时于活动场景新建内存 LightingSettings 并标脏。
12. `set_*` 项目设置重写 `ProjectSettings/*.asset` 为新序列化格式；`set_runtime_pipeline_settings` 新建 `RuntimePipelineConfig.json`。
13. `instanceId` 在域重载后失效。
14. Git Bash 下 `unity command` 的 `/Root/Child` 参数会被 MSYS 转成 Windows 路径（用 `MSYS_NO_PATHCONV=1`）；`find_assets type=Prefab` 应写 `GameObject`。
15. `create_asset` 在目标已存在时 `dry_run` 不返回预览。
16. `run_tests async_tests=true` 对短测试同步返回 `completed`。
