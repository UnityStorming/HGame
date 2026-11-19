# MCP 材质与视觉操作（unity-cli）

> **适用场景**：通过 `.mcp.json` 的 `unity-cli` MCP（Unity 官方 `com.unity.pipeline`）管理材质、Shader、纹理与 UI 图片、动画、摄像机、图形设置、粒子 / 线条、模型与图片导入 | **关联文档**：[mcp-unity-cli.md](mcp-unity-cli.md)（工具总览、安全门、路径规则、149 工具速查）、[resource-api.md](resource-api.md)（运行时资源加载）、[naming-rules.md](naming-rules.md)（资源与 UI 命名）、[architecture.md](architecture.md)（URP 与相机堆叠）

## 工具速查

只使用 `unity list --project-path GameUnity --json` 返回的工具名；下表按视觉工作流分组，参数名以 `unity list` 的 `parameters[].name` 为准。

| 工具 | 用途 |
|------|------|
| `create_asset` | 创建材质（`type=UnityEngine.Material` + `shader`）或其它 `UnityEngine.Object` 资产 |
| `get_material_properties` | 读材质的 shader、`rawRenderQueue`、已启用关键字与全部属性值 |
| `set_material_properties` | 设置材质属性 / 贴图 / 关键字 / renderQueue，可换 shader，支持 `dry_run` |
| `list_shaders` | 按名称子串列出可用 Shader |
| `get_shader_properties` | 查看 Shader 声明的属性（名称、类型、范围、标记） |
| `write_text_file` | 写 `.shader` / `.cginc` / `.hlsl` 文件（无专用 Shader 工具） |
| `import_asset` | 从宿主机绝对路径导入图片、模型、音频到 `Assets/` |
| `get_import_settings` / `set_import_settings` | 读写 TextureImporter / ModelImporter / AudioImporter 字段与平台覆盖 |
| `create_animation_clip` / `set_animation_curve` / `get_animation_clip` / `remove_animation_curve` | AnimationClip 与曲线 |
| `create_animator_controller` / `add_animator_parameter` / `add_animator_layer` / `add_animator_state` / `add_animator_transition` / `get_animator_controller` | AnimatorController 结构 |
| `add_component` / `set_component_properties` / `set_serialized_field` / `get_serialized_fields` | 给对象挂 Animator / ParticleSystem / LineRenderer / Camera 等组件并改字段 |
| `get_graphics_settings` | 读当前渲染管线资产（HGame 禁止用 `set_graphics_settings` 切换） |
| `find_assets` / `search` / `delete_asset` | 核对、查找与清理资产 |
| `batch` | 多步视觉操作打包执行（事务默认开启） |

未安装、因此不可用的能力：Timeline（`create_timeline` 等返回 `package_not_found`）、VFX Graph（工程未安装 `com.unity.visualeffectgraph`，只能用内置 ParticleSystem）、AI 生成图片 / 模型（unity-cli 没有生成类工具）。

## HGame 资源目录

| 资源类型 | 推荐目录 |
|----------|----------|
| 材质 | `Assets/BundleAssets/Materials/` |
| Shader | `Assets/BundleAssets/Shaders/` |
| 特效 Prefab / 粒子 | `Assets/BundleAssets/Effects/` |
| UI Prefab | `Assets/BundleAssets/UI/` |
| UI 图集 / 图标源图 | `Assets/BundleAssets/UIRaw/` |
| 音频 | `Assets/BundleAssets/Audios/` |
| 角色 / 模型 Prefab | `Assets/BundleAssets/Actor/` 或 `Assets/BundleAssets/Prefabs/` |
| 场景 | `Assets/BundleAssets/Scenes/` |

视觉资产进入运行时前必须确认 YooAsset 收集规则和最终资源地址。不要使用 `Assets/AssetRaw/`。路径写 `Assets/` 相对形式（前缀可省），不要写宿主机绝对路径（`import_asset` 的 `source` 除外）。

## 材质与 Shader

### 创建与设置材质

```text
create_asset path=BundleAssets/Materials/HeroPreviewMat.mat type=UnityEngine.Material shader="Universal Render Pipeline/Lit"
set_material_properties material=BundleAssets/Materials/HeroPreviewMat.mat dry_run=true
  properties={"_BaseColor": [0.25, 0.55, 0.9, 1], "_Metallic": 0.2, "_BaseMap": {"path": "Assets/BundleAssets/UIRaw/Atlas/HeroAlbedo.png"}}
set_material_properties material=... properties={...}          # 去掉 dry_run 真正写入
get_material_properties material=BundleAssets/Materials/HeroPreviewMat.mat   # 回读核对
```

- `create_asset` 不传 `shader` 时：URP 工程默认 `Universal Render Pipeline/Lit`。目标已存在需 `confirm=true`。
- `properties` 取值：Float / Range / Int 传数字；Color 传 `[r,g,b,a]` 或 `"#RRGGBBAA"`；Vector 传 `[x,y,z,w]`；Texture 传 `{"path": ...}` / `{"guid": ...}`，传 `null` 清空。未声明的属性名会在返回的 `unknown[]` 中列出，`dry_run=true` 可先校验。
- 属性名先用 `get_shader_properties shader="<Shader 名>"` 或 `get_material_properties` 查实，不要凭经验猜。
- **URP 材质的 `renderQueue` 与关键字（如 `_EMISSION`）实测会被 URP 的 `MaterialPostprocessor` 按 `_Surface` / `_QueueOffset` / GI 标志重新计算**：命令返回 `applied` 且磁盘写入正确，但内存对象与 `get_material_properties` 立即读回默认值。要改渲染队列或发光，改用语义属性（`_Surface`、`_QueueOffset`、`_EmissionColor`）并在设置后回读；Built-in Shader（如 `Standard`）不受影响。
- 材质赋给 Renderer：`set_serialized_field target=/HeroPreview component=MeshRenderer field=m_Materials.Array.data[0] value={"path": "Assets/BundleAssets/Materials/HeroPreviewMat.mat"}`；多材质槽先 `m_Materials.Array.size` 调整数量。

### 常见 Shader 属性

| Shader / 管线 | 主颜色 | 主贴图 | 备注 |
|---------------|--------|--------|------|
| `Universal Render Pipeline/Lit` / `Unlit` | `_BaseColor` | `_BaseMap` | 当前工程为 URP 17.3，视觉资产优先按 URP 属性名 |
| `Standard`（Built-in） | `_Color` | `_MainTex` | URP 项目下慎用 |
| `Sprites/Default`、`UI/Default` | `_Color` | `_MainTex` | UI / 字体材质先用 `get_material_properties` 查实 |
| 自定义 Shader | 以 `get_shader_properties` 返回为准 | 同左 | 不要凭名称推断 |

`list_shaders filter="Universal Render Pipeline/"` 可列出 URP 下全部 Shader 名（含 `Hidden/` 内部 Shader，按 `isSupported` 过滤）。

### Shader 文件

unity-cli 没有专用 Shader 工具，`.shader` 文件用 `write_text_file` 写入（覆盖需 `confirm=true`），写入后自动导入编译；编译错误用 `get_console_logs severity=error` 查看。自定义 Shader 放 `Assets/BundleAssets/Shaders/`，并确认与 URP 17.3 兼容。现有自定义 UI Shader（`UI/GuideMask`、`HGame/UI/Sprites Shader`、`UGUIPro/UITextOutLine`）实际位于 `Assets/Scripts/HotFix/GameLogic/Module/UIModule/Expansion/**`，由 YooAsset `AutoCollectShaders` 随材质收集。

## 纹理、UI 图片与模型导入

```text
import_asset source="D:/art/IconSword.png" path=BundleAssets/UIRaw/Atlas/IconSword.png dry_run=true
import_asset source="D:/art/IconSword.png" path=BundleAssets/UIRaw/Atlas/IconSword.png
get_import_settings asset=BundleAssets/UIRaw/Atlas/IconSword.png
set_import_settings asset=BundleAssets/UIRaw/Atlas/IconSword.png settings={"textureType": "Sprite", "spriteImportMode": "Single", "mipmapEnabled": false, "maxTextureSize": 512}
set_import_settings asset=... platform=Android settings={"overridden": true, "maxTextureSize": 512, "format": "ASTC_6x6"}
```

- `get_import_settings` 按 importer 类型返回字段（TextureImporter：`textureType`、`sRGBTexture`、`alphaIsTransparency`、`isReadable`、`mipmapEnabled`、`wrapMode`、`filterMode`、`spriteImportMode`、`spritePixelsPerUnit`、`maxTextureSize` 等；`platform` 可取 `Standalone` / `iOS` / `Android` / `WebGL` 读平台覆盖块）。
- `set_import_settings` 先 `dry_run=true` 看 `applied[]` / `unknown[]`，再真实写入并触发重导入；模型（FBX）走 ModelImporter 字段，音频走 AudioImporter 字段。
- UI 图标 / 图集源图放 `Assets/BundleAssets/UIRaw/`，图集生成走 `HGame Tools/图集工具/*` 菜单（`menu`）。
- 导入后用 `find_assets search_in=BundleAssets/UIRaw name=IconSword` 或 `search query="t:Texture2D IconSword"` 核对，并确认贴图、材质、Prefab 依赖都在可收集目录。

## 动画

动画控制器和 Clip 放在对应角色或特效资源目录下，例如 `Assets/BundleAssets/Actor/<Role>/Animations/`。已有 FBX 动画时跳过 `create_animation_clip`，直接把导入后的 `.anim` 或模型子资源 Clip 作为 `motion`。

### 动画 workflow：角色 Idle / Run 控制器

```text
create_animation_clip path=BundleAssets/Actor/Hero/Animations/HeroIdle.anim frameRate=30 loop=true
create_animation_clip path=BundleAssets/Actor/Hero/Animations/HeroRun.anim  frameRate=30 loop=true
set_animation_curve clip=BundleAssets/Actor/Hero/Animations/HeroRun.anim type=Transform property=m_LocalPosition.z
  keys=[{"time": 0, "value": 0}, {"time": 0.8, "value": 2}]                   # path 省略 = 根节点；子节点传 path=Body/Head
create_animator_controller path=BundleAssets/Actor/Hero/Animations/Hero.controller
add_animator_parameter controller=BundleAssets/Actor/Hero/Animations/Hero.controller name=Speed type=Float defaultValue=0
add_animator_state controller=... name=Idle motion=BundleAssets/Actor/Hero/Animations/HeroIdle.anim isDefault=true
add_animator_state controller=... name=Run  motion=BundleAssets/Actor/Hero/Animations/HeroRun.anim
add_animator_transition controller=... fromState=Idle toState=Run hasExitTime=false duration=0.12 conditions=[{"parameter": "Speed", "mode": "Greater", "threshold": 0.1}]
add_animator_transition controller=... fromState=Run  toState=Idle hasExitTime=false duration=0.12 conditions=[{"parameter": "Speed", "mode": "Less", "threshold": 0.1}]
get_animator_controller controller=BundleAssets/Actor/Hero/Animations/Hero.controller     # 核对参数 / 层 / 状态 / 过渡
add_component target=/HeroPreview type=Animator
set_serialized_field target=/HeroPreview component=Animator field=m_Controller value={"path": "Assets/BundleAssets/Actor/Hero/Animations/Hero.controller"}
```

- 错误码：重复参数名返回 `duplicate_parameter`；层名不存在返回 `layer_not_found`；条件 `mode` 与参数类型不匹配（Float / Int 只能用 `Greater` / `Less` / `Equals` / `NotEqual`，Bool / Trigger 用 `If` / `IfNot`）返回 400。
- `add_animator_transition` 的 `fromState` 支持 `AnyState` / `Entry`，`toState` 支持 `Exit`；`hasExitTime` 默认 `false`，即时响应的移动 / 战斗过渡保持 `false`。
- `set_animation_curve` 省略切线时为 0（非 Unity 的 Auto 模式）；`remove_animation_curve` 需 `confirm=true`。
- Timeline 工具因 `com.unity.timeline` 未安装全部返回 `package_not_found`。
- 验证：`get_animator_controller` 结构完整、`get_serialized_fields target=/HeroPreview component=Animator field=m_Controller` 非空，并确认 `Assets/BundleAssets/Actor/Hero/Animations/` 被 YooAsset 收集。

## 摄像机与图形设置

- 场景摄像机用 `set_component_properties target=/Main Camera type=Camera properties={...}` 或 `set_serialized_field` 改序列化字段（`m_ClearFlags`、`m_BackGroundColor`、`orthographic`、`field of view`、`near clip plane`、`far clip plane` 等，先 `get_component_properties` 读出实际字段名）。
- 工程使用 URP 相机堆叠：`GameStart.unity` 的 `Main Camera` 为 Base，`UIRoot.prefab` 的 `UICamera` 为 Overlay 并由 `UIOverlayCameraBinder` 自动加入堆叠（见 [architecture.md](architecture.md)）；新增相机时不要破坏这一结构，Overlay 相机的 `m_ClearFlags` 会被 URP 强制为 `4`（Nothing）。
- `get_graphics_settings` 读取当前管线资产（`Assets/Settings/URP/HGame_URPAsset.asset`）；`set_graphics_settings` 会切换渲染管线，在 HGame 只允许 `dry_run`。质量等级 / vSync 用 `get_quality_settings` / `set_quality_settings`（`confirm=true` 写入 `ProjectSettings/QualitySettings.asset`，改动需评审）。

## 粒子、线条、拖尾

unity-cli 没有粒子专用工具，用组件命令组合：

```text
create_gameobject name=HitEffect
add_component target=/HitEffect type=ParticleSystem
get_serialized_fields target=/HitEffect component=ParticleSystem            # 先读出实际序列化路径
set_serialized_field target=/HitEffect component=ParticleSystem field=looping value=false
set_serialized_field target=/HitEffect component=ParticleSystem field=lengthInSec value=1
set_serialized_field target=/HitEffect component=ParticleSystem field=moveWithTransform value=1     # 1 = World，命中 / 飞行特效必须世界空间
set_serialized_field target=/HitEffect component=ParticleSystem field=InitialModule.startLifetime.scalar value=0.5
set_serialized_field target=/HitEffect component=ParticleSystem field=EmissionModule.rateOverTime.scalar value=0
create_prefab source=/HitEffect path=BundleAssets/Effects/HitEffect
```

- ParticleSystem 的序列化结构是嵌套模块（`InitialModule.*`、`EmissionModule.*`（含 `m_Bursts`）、`ShapeModule.*`、`ColorModule.*`、`SizeModule.*`…），MinMaxCurve 字段通过 `.scalar` / `.minScalar` / `.maxCurve` 子路径写常量；以上路径未经本工程实测，务必先 `get_serialized_fields` 读出再写，写后回读。
- `LineRenderer` / `TrailRenderer` 同样用 `add_component` + `set_serialized_field`（宽度在 `m_Parameters.widthMultiplier` / `m_Parameters.widthCurve`，顶点在 `m_Positions.Array.*`；`startWidth` 只是运行时属性，不是序列化字段，先 `get_serialized_fields` 回读路径）。
- UI 内粒子使用工程已有的 `Coffee.UIExtensions/UI Particle` 组件（`add_component type=Coffee.UIExtensions.UIParticle`）。
- 特效 Prefab 保存到 `Assets/BundleAssets/Effects/`，运行时加载用 `GameModule.ResourceModule`，生命周期按 [resource-api.md](resource-api.md) 释放。

## 批量视觉操作

多个材质 / 纹理 / 组件修改用 `batch`：事务默认开启，任一失败整体回滚；后续操作可用 `"$<id>.<field>"` 引用前序结果。资产写入（`create_asset`、`set_import_settings`、`write_text_file` 等）不在 Undo 内，含这类操作的批次必须 `transactional=false`。

```text
batch transactional=false operations=[
  {"id": "mat", "command": "create_asset", "params": {"path": "BundleAssets/Materials/EnemyPreviewMat.mat", "type": "UnityEngine.Material", "shader": "Universal Render Pipeline/Lit"}},
  {"id": "col", "command": "set_material_properties", "params": {"material": "$mat.assetPath", "properties": {"_BaseColor": [0.8, 0.2, 0.2, 1]}}}
]
```

`batch` 不能包含 `recompile`、`editor_play`、`eval`、`open_scene` 等排除命令，也不能嵌套。

## 常见错误

| 错误 | 正确做法 |
|------|----------|
| 使用 `Assets/AssetRaw/...` | 使用 `Assets/BundleAssets/...` |
| 沿用已移除的旧 MCP 包的 `manage_*` 聚合工具名 | 该包已从工程移除；按本文档使用 `create_asset`、`set_material_properties`、`create_animator_controller` 等 unity-cli 工具 |
| `create_asset` 材质写 `materialPath` / `shaderName` | 参数是 `path` / `type=UnityEngine.Material` / `shader` |
| 颜色拆成 `r/g/b/a` 字段或写 `color` 参数 | `set_material_properties` 的 `properties` 里写 `"_BaseColor": [r,g,b,a]` 或 `"#RRGGBBAA"` |
| Shader 属性凭经验写 | 先 `get_shader_properties` / `get_material_properties`，再设置 `_BaseColor` / `_Color` 等 |
| 在 URP 材质上直接改 `renderQueue` 或启用 `_EMISSION` 关键字 | 会被 URP 后处理重置；改 `_Surface` / `_QueueOffset` / `_EmissionColor` 并回读 |
| 动画过渡沿用默认 `hasExitTime` | unity-cli 默认已是 `false`；需要播完再切换时显式 `hasExitTime=true exitTime=<0..1>` |
| Float 参数条件写 `If` | Float / Int 用 `Greater` / `Less` / `Equals` / `NotEqual` |
| 粒子省略模拟空间 | 命中 / 飞行特效写 `moveWithTransform=1`（World），默认 Local 会让粒子跟随物体 |
| 只创建材质不放入收集目录 | 放入 `BundleAssets` 并确认 YooAsset 收集 |
| 导入模型后不查依赖 | 用 `find_assets` / `search` 与 `get_import_settings` 检查贴图、材质、Prefab 依赖 |
| 用 `capture_*` 的 `save_path` 写到 `Temp/` 做视觉验证 | 会落到 `Assets/Temp/`；用 `screenshot`（项目根相对）或不传 `save_path` 取内联图 |
