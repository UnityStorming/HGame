# 渲染管线（URP）

## 概述

`GameUnity` 工程使用 URP（Universal Render Pipeline）17.3.0（Unity 6000.3.10f1 编辑器内置版本）作为唯一渲染管线；Built-in 管线不再使用。工程为纯 UGUI 项目，切换后 UI 显示效果与切换前一致，颜色空间保持 Gamma。

## 包与资产

- `GameUnity/Packages/manifest.json` 声明 `com.unity.render-pipelines.universal: 17.3.0`；`core`、`shadergraph`、`universal-config` 及 `burst`、`mathematics`、`collections`、`searcher` 由 UPM 自动解析并记录在 `packages-lock.json`。
- URP 资产统一位于 `GameUnity/Assets/Settings/URP/`（不在 YooAsset 任何收集目录内，随 Player 内置）：
  - `HGame_URPAsset.asset`：UniversalRenderPipelineAsset。HDR 关、MSAA 关（Disabled）、主光与附加光阴影关、Depth Texture 关、Opaque Texture 关、Render Scale 1、SRP Batcher 开、Dynamic Batching 关、后处理关；`m_RendererDataList[0]` 指向 `HGame_URPRenderer.asset`。
  - `HGame_URPRenderer.asset`：UniversalRendererData，渲染模式 Forward，不附加 Renderer Feature。
  - `UniversalRenderPipelineGlobalSettings.asset`：URP 全局设置（通过 `RenderPipelineGlobalSettingsUtils.Create` 创建在该目录）；`DefaultVolumeProfile.asset`：全局设置引用的默认 Volume Profile。`GameUnity/Assets/` 根目录不保留 URP 自动生成的重复文件。
- ShaderGraph 包自动生成的 `ProjectSettings/ShaderGraphSettings.asset` 随工程提交。
- 六个质量等级（Very Low / Low / Medium / High / Very High / Ultra）共用同一个 `HGame_URPAsset.asset`。

## 项目设置

- `ProjectSettings/GraphicsSettings.asset`：`m_CustomRenderPipeline` 引用 `HGame_URPAsset.asset`；`m_RenderPipelineGlobalSettingsMap` 含 `UnityEngine.Rendering.Universal.UniversalRenderPipeline` 条目并指向 `UniversalRenderPipelineGlobalSettings.asset`。
- `ProjectSettings/QualitySettings.asset`：六个等级的 `customRenderPipeline` 均引用 `HGame_URPAsset.asset`；其余质量字段保持原值。
- `ProjectSettings/ProjectSettings.asset` 的 `m_ActiveColorSpace` 保持 0（Gamma）。

## 相机

- `Assets/Scenes/GameStart/GameStart.unity` 的 `Main Camera` 为 URP Base 相机（`UniversalAdditionalCameraData.m_CameraType = 0`），其 `m_Cameras`（相机堆叠）包含场景内 `UIRoot` 实例的 `UICamera`。
- `Assets/HGame/Runtime/Setting/Prefabs/UIRoot.prefab`：
  - `UICamera` 为 URP Overlay 相机（`m_CameraType = 1`），并挂载 `UIOverlayCameraBinder` 组件；
  - `UICanvas` 保持 `Screen Space - Camera`（`m_RenderMode = 1`），`m_Camera` 引用 `UICamera`。
- `UIOverlayCameraBinder`（`Assets/HGame/Runtime/Module/RenderPipeline/UIOverlayCameraBinder.cs`，命名空间 `HGame`，程序集 `HGame.Runtime`，需要 `Unity.RenderPipelines.Universal.Runtime` 引用）：在 `OnEnable` 与 `SceneManager.sceneLoaded` 回调中，把自身相机加入当前 `Camera.main` 的 `UniversalAdditionalCameraData.cameraStack`（已存在则不重复添加）；`Camera.main` 为空或不是 Base 相机时不做任何操作也不报错；`OnDisable` 时注销回调。它覆盖 `SingletonSystem.Restart()` 重载场景以及 `SceneModule` 切换场景后主相机更换的情况。
- `Assets/Scenes/SceneUI.unity`（编辑器工作场景，无主相机）中的 `UICamera` 实例把 `UniversalAdditionalCameraData.m_CameraType` 覆盖为 0（Base），使该场景仍可在编辑器中预览 UI。

## Shader 与材质

- 自定义 UI Shader `UI/GuideMask`、`HGame/UI/Sprites Shader`、`UGUIPro/UITextOutLine` 以及 `Assets/Plugins/TextMesh Pro/Shaders/` 下的 TextMeshPro Shader 内容保持不变；它们没有 `LightMode` Tag，由 URP 按 `SRPDefaultUnlit` 渲染。
- 工程内材质（`Assets/BundleAssets/Materials/*.mat`、TextMeshPro 字体材质）保持不变。

## 迁移与校验工具

- `Assets/HGame/Editor/RenderPipeline/URPMigrationTool.cs`（命名空间 `HGame`，程序集 `HGame.Editor`）提供菜单 `HGame Tools/Render Pipeline/Migrate To URP` 与批处理入口 `HGame.URPMigrationTool.RunBatch`：创建/复用 URP 资产与全局设置、把管线资产赋给 GraphicsSettings 与全部质量等级、配置 `UIRoot.prefab`、`GameStart.unity`、`SceneUI.unity` 的相机堆叠与画布绑定；可重复执行。
- `Assets/HGame/Editor/RenderPipeline/URPSetupValidator.cs`（命名空间 `HGame`，程序集 `HGame.Editor`）提供菜单 `HGame Tools/Render Pipeline/Validate URP Setup` 与批处理入口 `HGame.URPSetupValidator.RunBatch`（命令行 `-executeMethod HGame.URPSetupValidator.RunBatch -reportPath <json>`；未传 `-reportPath` 时写到 `Logs/urp-setup-report.json`）。
- 检查项与判定：
  - `packageResolved`：`Packages/packages-lock.json` 含 `com.unity.render-pipelines.universal`；
  - `pipelineAssigned`：`GraphicsSettings.defaultRenderPipeline` 为 `HGame_URPAsset.asset`；
  - `qualityLevelsAssigned`：六个质量等级的 `QualitySettings.GetRenderPipelineAssetAt(i)` 均为该资产；
  - `pipelineAssetConfig`：HDR 关、MSAA 关、主光/附加光阴影关、Depth/Opaque Texture 关、SRP Batcher 开；
  - `cameraStack`：打开 `GameStart.unity` 后 Main Camera 为 Base 且堆叠包含 UICamera，UICamera 为 Overlay 并挂有 `UIOverlayCameraBinder`；
  - `canvasBinding`：`UIRoot.prefab` 的 `UICanvas.worldCamera` 为 `UICamera`；
  - `shaderErrors`：`Assets/**/*.shader` 中除 `TMP_SDF-Surface.shader`、`TMP_SDF-Surface-Mobile.shader`（未被引用的表面着色器，记为 `skipped`）外，`ShaderUtil.ShaderHasError` 全部为 false；
  - `renderSmoke`：在编辑模式打开 `GameStart.unity`，把 `Assets/BundleAssets/UI/MainWindow.prefab` 与 `GuideUI.prefab` 实例化到 `UICanvas` 下，临时把 `UICamera` 的 URP 相机类型改为 Base，用 `RenderPipeline.SubmitRenderRequest`（`StandardRequest`）渲染到 750×1334 的 RenderTexture 并读回像素，要求不同颜色像素数大于 1 且不存在 URP 错误色洋红 (R=1,G=0,B=1) 像素；命令行 `-smokeDump <png>` 可导出画面；完成后恢复相机类型为 Overlay、销毁实例、不保存场景。（编辑模式的离屏 RenderRequest 不输出 Overlay 相机内容，堆叠配置由 `cameraStack`/`canvasBinding` 保证。）
- 报告为 JSON：`{ "passed": bool, "checks": [ { "id", "status": "passed|failed|skipped", "detail" } ] }`；批处理入口在全部非 `skipped` 检查通过时以退出码 0 结束，否则以退出码 1 结束。

## 文档

- `.claude/skills/hgame-dev/references/architecture.md` 与 `.codex/skills/hgame-dev/references/architecture.md` 说明：渲染管线为 URP 17.3，资产位于 `Assets/Settings/URP/`，UI 相机为 Overlay 并由 `UIOverlayCameraBinder` 维护堆叠，校验菜单为 `HGame Tools/Render Pipeline/Validate URP Setup`。
- 两份 `mcp-visual.md` 中关于自定义 Shader 位置的说明补充当前自定义 UI Shader 实际位于 `Assets/Scripts/HotFix/GameLogic/Module/UIModule/Expansion/**`。

## 不受影响的部分

- YooAsset 收集规则、HybridCLR `AOTMetaAssemblies`/`patchAOTAssemblies`、Obfuz 配置、热更程序集（GameLogic / GameProto / GameBattle）的 asmdef 引用均不变；热更代码不引用 URP 程序集。
