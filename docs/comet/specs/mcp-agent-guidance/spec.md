# AI 指引中的 MCP 工具链（mcp-agent-guidance）

## 概述

本仓库面向 AI 的指引文件（根 `CLAUDE.md`、`AGENTS.md`，hgame-dev skill 的 `.claude/skills/hgame-dev/` 与 `.codex/skills/hgame-dev/` 两份镜像）中，所有关于 Unity MCP 的内容只以 Unity 官方 `unity-cli` MCP（`com.unity.pipeline`，`.mcp.json` 中唯一的 MCP 服务器）为准。`com.coplaydev.unity-mcp` 已于提交 `7600c17e` 从工程移除，指引中不再出现以它为前提的工具名、示例或 eval 用例；唯一保留的提法是各入口处一句"已移除、不要使用 `manage_*` 工具"的说明。

## 文件与内容

### 根入口：`CLAUDE.md` 与 `AGENTS.md`

- 「第一步：按主题获取规范」主题表的「MCP 工具链」行引用 `mcp-unity-cli.md`（场景 / GameObject / Prefab / 脚本 / 编辑器 / 烘焙 / 设置 / batch）与 `mcp-visual.md`（材质 / Shader / 纹理 / 动画 / VFX），并在同一行说明"CoplayDev unity-mcp 已于 7600c17e 移除，不要使用 `manage_*` 系列工具，MCP 操作统一走 `.mcp.json` 的 `unity-cli`"。
- 「Reference 路由」表含「MCP Unity CLI 工具」→ `mcp-unity-cli.md` 与「MCP 材质 / Shader / 动画 / VFX」→ `mcp-visual.md` 两行，不含 `mcp-tools.md`。
- 两份文件的上述两处内容一致。

### hgame-dev `SKILL.md`（两份镜像）

- frontmatter `description` 触发词含 `unity-cli`、`com.unity.pipeline`、`MCP`，不含 `CoplayDev unity-mcp`。
- 文档路由表含「MCP Unity CLI 工具」→ `mcp-unity-cli.md`（P1）与「MCP 材质/Shader/动画/VFX」→ `mcp-visual.md`（P2）两行，不含 `mcp-tools.md` 行。两份镜像的这两行一致；`.claude` 版既有的 `module-system.md` 行不受影响。

### `references/mcp-unity-cli.md`（两份镜像，内容一致）

在既有六节（已核实来源、安全门、路径与 authoring root、按分类的工具速查、端到端链路模板、已知问题）基础上：

- 「已核实来源」写明：`com.coplaydev.unity-mcp` 已于 `7600c17e` 移除，`mcp-tools.md` 已删除，本文档与 `mcp-visual.md` 是 HGame 唯一的 MCP 指南；关联文档行不再链接 `mcp-tools.md`。
- 新增「HGame 落位与 uGUI Prefab 流程」节：HGame 路径表（UI 窗口 / Widget 脚本 `Assets/Scripts/HotFix/GameLogic/UI/<模块名>/`、UIModule `Assets/Scripts/HotFix/GameLogic/Module/UIModule/`、业务模块 `Assets/Scripts/HotFix/GameLogic/Module/<ModuleName>/`、事件接口 `Assets/Scripts/HotFix/GameLogic/IEvent/`、Luban 生成代码 `Assets/Scripts/HotFix/GameProto/`（勿手改）、热更资源根 `Assets/BundleAssets/`、UI Prefab `Assets/BundleAssets/UI/`、图集与原始图 `Assets/BundleAssets/UIRaw/`、场景 `Assets/BundleAssets/Scenes/`、材质 / Shader / 特效 `Assets/BundleAssets/Materials|Shaders|Effects/`）；"工具接收 `Assets/` 相对路径、不写宿主机绝对路径"；uGUI Prefab 流程（`create_gameobject` 临时根 → `add_component` Canvas / CanvasScaler / GraphicRaycaster 与子节点 uGUI 组件 → 节点按 `naming-rules.md` 前缀命名 → `set_component_properties` 设置 CanvasScaler：`uiScaleMode=ScaleWithScreenSize`、`referenceResolution=1920×1080`、`screenMatchMode=MatchWidthOrHeight`、`matchWidthOrHeight=0.5` → `create_prefab` 到 `Assets/BundleAssets/UI/<Name>.prefab` → `delete_gameobject` 临时对象 → 绑定脚本走 HGame `ScriptGenerator` 链路）；Prefab 根节点结构；锚点规则（全屏拉伸四角、固定尺寸控件单点锚点）；YooAsset 地址与收集规则提醒（资源地址通常是文件名不含路径与扩展名，改动前核对收集设置与加载点）。
- 新增「常用菜单项」节：`menu` 执行前先无 `path` 列出确认；UI 脚本生成（`GameObject/ScriptGenerator/*`）、Luban 转表（`HGame Tools/Luban/转表`，优先 `luban-dev`）、HybridCLR 生成（`HybridCLR/Generate/*`，先确认平台）、保存项目（`File/Save Project`）。
- 新增「常见错误」节：`Assets/GameScripts/...` → `Assets/Scripts/HotFix/GameLogic/...`；`Assets/AssetRaw/...` → `Assets/BundleAssets/...`；直接改 `GameProto` 生成代码 → 改配置源并导表；重名对象直接修改 → 先 `find_gameobjects` 取 `hierarchyPath` / `globalId`；用 `create_script` 模板写业务脚本 → 用 `write_text_file` 写符合规范的内容；沿用 CoplayDev 的 `manage_*` 工具名 → 该包已移除。

### `references/mcp-visual.md`（两份镜像，内容一致）

面向 unity-cli 的材质 / Shader / 纹理 / 动画 / 摄像机 / 图形 / 粒子-VFX / 模型与图片导入指南：

- 「适用场景」与「关联文档」只引用 `mcp-unity-cli.md`、`resource-api.md`、`naming-rules.md`、`architecture.md`。
- 「工具速查」表只列 unity-cli 工具：`create_asset`、`get_material_properties`、`set_material_properties`、`list_shaders`、`get_shader_properties`、`write_text_file`、`import_asset`、`get_import_settings`、`set_import_settings`、`create_animation_clip`、`set_animation_curve`、`get_animation_clip`、`remove_animation_curve`、`create_animator_controller`、`add_animator_parameter`、`add_animator_layer`、`add_animator_state`、`add_animator_transition`、`get_animator_controller`、`add_component`、`set_component_properties`、`set_serialized_field`、`get_graphics_settings`、`batch`、`find_assets`、`delete_asset`。
- 材质：`create_asset type=UnityEngine.Material shader=<名>` 到 `Assets/BundleAssets/Materials/`；`set_material_properties` 的 Color / Vector / Texture / 关键字 / renderQueue 用法；实测结论：URP 材质的 `renderQueue` 与 `_EMISSION` 等关键字会被 URP `MaterialPostprocessor` 按 `_Surface` / `_QueueOffset` / GI 标志重算，应改用语义属性并 `get_material_properties` 回读；Built-in Shader 不受影响；材质赋给 Renderer 用 `set_serialized_field field=m_Materials.Array.data[0]`。
- Shader：无专用工具，`write_text_file` 写 `.shader`，位置 `Assets/BundleAssets/Shaders/`（当前自定义 UI Shader 实际位于 `GameLogic/Module/UIModule/Expansion/**`）；`list_shaders` / `get_shader_properties` 核对属性名。
- 纹理 / Sprite / 模型：`import_asset` 从绝对路径导入到 `Assets/BundleAssets/UIRaw/` 等目录；`get_import_settings` / `set_import_settings`（`textureType=Sprite`、`spriteImportMode`、`isReadable`、`mipmapEnabled`、平台覆盖），模型走 ModelImporter 字段；导入后用 `find_assets` 核对。
- 动画：clip → curve → controller → parameter / layer / state / transition → `get_animator_controller` 核对；Animator 组件 `add_component Animator` + `set_serialized_field m_Controller`；错误码 `duplicate_parameter`、`layer_not_found`，条件 mode 与参数类型校验；Timeline 未安装。
- 摄像机与图形：Camera 用 `set_component_properties`；URP 相机堆叠约束引用 `architecture.md`；`get_graphics_settings` 只读，`set_graphics_settings` 在 HGame 禁用。
- 粒子 / VFX：`add_component ParticleSystem|LineRenderer|TrailRenderer` + `set_component_properties`（序列化字段名）；UI 粒子用工程已有 `Coffee.UIExtensions/UI Particle` 组件；VFX Graph 未安装。
- 批量：`batch`（事务默认、`$id.field` 引用、资产写入需 `transactional=false`）。
- 常见错误表：unity-cli 参数名（如 `type` / `shader` / `properties`）与路径纠正。

### `evals/evals.json`（两份镜像，内容一致）

- 共 19 个用例，`skill_name` 不变。id 11 `mcp-create-ui-script`、12 `mcp-create-material`、13 `mcp-animation-workflow`、18 `mcp-ui-vs-ugui-boundary` 的 `prompt` / `description` 不提 CoplayDev；`expected_patterns` 只使用 unity-cli 工具名（11：`write_text_file|create_script` + HGame UI 路径 + `UIWindow`；12：`create_asset`、`set_material_properties`、`Assets/BundleAssets/Materials`、`Universal Render Pipeline/Lit`、`_BaseColor`；13：`create_animator_controller`、`add_animator_parameter`、`add_animator_state`、`add_animator_transition`、`Assets/BundleAssets/Actor`；18：`Assets/Scripts/HotFix/GameLogic/UI`、`Assets/BundleAssets/UI`、`UIWindow`、`ScriptGenerator`）；四个用例的 `forbidden_patterns` 都含 `manage_`，并保留原有的 `GameScripts`、`Assets/AssetRaw` 等路径禁止项。其余 15 个用例逐字不变。

### 已删除文件

- `.claude/skills/hgame-dev/references/mcp-tools.md` 与 `.codex/skills/hgame-dev/references/mcp-tools.md` 不存在。

### 仓库外

- 用户级全局 skill `C:\Users\BingLau\.claude\skills\unity-mcp-skill\`（CoplayDev "MCP for Unity" 操作指南）已删除。

## 不变量

- 指引中出现的 MCP 工具名都属于 `unity list --project-path GameUnity --json` 返回的 149 个工具；参数名以其 `parameters[].name` 为准。
- hgame-dev 两份镜像逐字一致（`SKILL.md` 以 MCP 相关行为准）。
- 历史测试报告 `docs/unity-cli-mcp-test-report.md`、`docs/comet/archive/**`、`.mcp.json`、`.understand-anything/knowledge-graph.json` 不因本 capability 改变。
