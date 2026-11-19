# 目标

把 `GameUnity` 工程的渲染管线从 Built-in 切换为 URP（Universal Render Pipeline，Unity 6000.3.10f1 内置的 17.3.0 版本），切换后现有纯 UGUI 界面的显示效果与切换前一致，工程可正常编译，UI 通过 URP 相机堆叠正确渲染，并提供一个可在批处理模式运行的 URP 配置校验工具。

# 范围

- 包：`GameUnity/Packages/manifest.json` 加入 `com.unity.render-pipelines.universal`（17.3.0，编辑器内置；`core`、`shadergraph`、`burst`、`mathematics`、`collections`、`searcher` 等依赖由 UPM 自动解析）。
- URP 资产（放在 `GameUnity/Assets/Settings/URP/`，不在 YooAsset 收集目录内，随 Player 内置）：
  - `HGame_URPAsset.asset`（UniversalRenderPipelineAsset）+ `HGame_URPRenderer.asset`（Universal Renderer，Forward）；
  - 配置以保持当前视觉与性能为准：HDR 关、MSAA 关、主光/附加光阴影关、Depth Texture 关、Opaque Texture 关、Render Scale 1、SRP Batcher 开、Dynamic Batching 关、后处理关；
  - `UniversalRenderPipelineGlobalSettings.asset` 与其引用的 `DefaultVolumeProfile.asset` 同样放在该目录；ShaderGraph 包自动生成的 `ProjectSettings/ShaderGraphSettings.asset` 一并提交。
- 项目设置：`ProjectSettings/GraphicsSettings.asset` 的 `m_CustomRenderPipeline` 与 `ProjectSettings/QualitySettings.asset` 六个质量等级的 `customRenderPipeline` 全部指向同一个 URP Asset。
- 相机：`GameStart.unity` 的 `Main Camera` 作为 Base 相机；`UIRoot.prefab` 中的 `UICamera` 作为 Overlay 相机并加入 Main Camera 的相机堆叠；`UIRoot.prefab` 的 `UICanvas.m_Camera` 绑定到 `UICamera`（保持 Screen Space - Camera）；新增框架组件 `UIOverlayCameraBinder`（`Assets/HGame/Runtime/...`，挂在 `UICamera` 上）在场景加载后自动把自身加入当前 `Camera.main` 的堆叠，覆盖 `SingletonSystem.Restart()` 重载场景与 `SceneModule` 切换场景后主相机更换的情况；`SceneUI.unity`（编辑器工作场景，无主相机）中的 `UICamera` 实例覆盖为 Base 类型以便继续预览。
- Shader：3 个自定义 UI Shader（`UI/GuideMask`、`HGame/UI/Sprites Shader`、`UGUIPro/UITextOutLine`）与 TextMeshPro Shader 均为无光照、无 `LightMode` 的 CG Shader，在 URP 下按 `SRPDefaultUnlit` 渲染，保持不改；由校验工具确认其在 URP 下无编译错误。
- 校验工具：新增 `Assets/HGame/Editor/RenderPipeline/URPSetupValidator.cs`（HGame.Editor），菜单 `HGame Tools/Render Pipeline/Validate URP Setup`，并提供批处理入口 `HGame.URPSetupValidator.RunBatch`（通过 `-executeMethod` 调用，用 `-reportPath` 指定 JSON 报告输出路径）。检查项：URP 包已解析、管线资产已赋值到 GraphicsSettings 与全部质量等级、URP Asset 关键配置、`GameStart.unity` 相机堆叠与 `UIRoot.prefab` Canvas 绑定、项目内全部 `.shader` 无编译错误、渲染烟雾测试（在编辑模式把 `MainWindow.prefab` 与 `GuideUI.prefab` 实例化到 UICanvas 下，临时把 `UICamera` 作为 Base 相机通过 `RenderPipeline.SubmitRenderRequest` 渲染到 RenderTexture，断言画面非单一颜色且不含 URP 错误色洋红 (1,0,1)；可用 `-smokeDump <png>` 导出画面）。另保留迁移工具 `URPMigrationTool.cs`（菜单 `HGame Tools/Render Pipeline/Migrate To URP`，批处理入口 `HGame.URPMigrationTool.RunBatch`），可重复执行以创建/修复 URP 资产、项目设置与相机堆叠。
- 文档：`.claude/skills/hgame-dev/references/architecture.md` 与 `.codex/skills/hgame-dev/references/architecture.md` 增加“渲染管线：URP 17.3，资产位于 `Assets/Settings/URP/`，UI 相机为 Overlay 堆叠”说明；`mcp-visual.md` 中“Shader 放 `Assets/BundleAssets/Shaders/`”一行补充“当前自定义 UI Shader 实际位于 `GameLogic/Module/UIModule/Expansion/**`”。

# 非目标

- 不改变颜色空间（保持 Gamma），不启用 HDR、后处理 Volume、2D 光照、Shader Graph。
- 不改写 3 个自定义 UI Shader 与 TextMeshPro Shader；`TMP_SDF-Surface.shader`、`TMP_SDF-Surface-Mobile.shader` 两个未被任何材质引用的表面着色器保留原样。
- 不调整 Built-in 材质：工程内 5 个材质均使用项目自有 Shader，没有 Standard / Legacy / Mobile / Particles 材质，Render Pipeline Converter 无需运行。
- 不修改 YooAsset 收集规则、HybridCLR `AOTMetaAssemblies`、Obfuz 配置：热更代码不引用 URP 程序集，URP 相关新增代码全部放在 AOT 侧的 `HGame.Runtime` / `HGame.Editor`。
- 不修改 `Assets/Scenes/GameStart/GameStart.unity` 与 `UIRoot.prefab` 之外的场景/预制体结构，不改变 UI 排序、分辨率适配与 Canvas 层级。
- 不修改 Debugger 面板 `QualityInformationWindow`（其读取的 `QualitySettings` 字段在 URP 下仍是合法 API，仅部分字段不再生效）。
- 不做真机测试与 Player 打包；不重命名或迁移 `SceneUI.unity`。

# 验收示例

- A1: `GameUnity/Packages/manifest.json` 含 `"com.unity.render-pipelines.universal": "17.3.0"`；`GameUnity/Packages/packages-lock.json` 中 `com.unity.render-pipelines.universal`、`com.unity.render-pipelines.core`、`com.unity.shadergraph` 版本均为 `17.3.0`。
- A2: `GameUnity/Assets/Settings/URP/HGame_URPAsset.asset`、`HGame_URPRenderer.asset`、`UniversalRenderPipelineGlobalSettings.asset`、`DefaultVolumeProfile.asset` 及各自 `.meta` 存在，`GameUnity/Assets/` 根目录下没有 URP 自动生成的 `UniversalRenderPipelineGlobalSettings.asset` / `DefaultVolumeProfile.asset`；`HGame_URPAsset.asset` 序列化字段为 `m_SupportsHDR: 0`、`m_MSAA: 1`、`m_MainLightShadowsSupported: 0`、`m_AdditionalLightShadowsSupported: 0`、`m_RequireDepthTexture: 0`、`m_RequireOpaqueTexture: 0`、`m_RenderScale: 1`、`m_UseSRPBatcher: 1`、`m_SupportsDynamicBatching: 0`，且 `m_RendererDataList` 引用 `HGame_URPRenderer.asset`（`m_RenderingMode: 0` Forward）。
- A3: `ProjectSettings/GraphicsSettings.asset` 的 `m_CustomRenderPipeline` 与 `ProjectSettings/QualitySettings.asset` 六个质量等级的 `customRenderPipeline` 都引用 `HGame_URPAsset.asset.meta` 中的 GUID；`GraphicsSettings.asset` 的 `m_RenderPipelineGlobalSettingsMap` 含 `UnityEngine.Rendering.Universal.UniversalRenderPipeline` 条目；`ProjectSettings.asset` 的 `m_ActiveColorSpace` 仍为 0。
- A4: `GameStart.unity` 中 Main Camera 挂有 `UniversalAdditionalCameraData`（`m_CameraType: 0`），其 `m_Cameras` 包含 UIRoot 实例的 UICamera；`UIRoot.prefab` 中 UICamera 挂有 `UniversalAdditionalCameraData`（`m_CameraType: 1`）与 `UIOverlayCameraBinder`，`UICanvas` 的 `m_RenderMode: 1` 且 `m_Camera` 引用 UICamera；`SceneUI.unity` 中 UICamera 实例的 `m_CameraType` 覆盖为 0。
- A5: `Assets/HGame/Runtime/Module/RenderPipeline/UIOverlayCameraBinder.cs` 存在于 `namespace HGame`，在 `OnEnable` 与 `SceneManager.sceneLoaded` 中把自身相机加入 `Camera.main` 的 `cameraStack`（已存在不重复），`Camera.main` 为空时不抛异常，`OnDisable` 注销回调；`HGame.Runtime.asmdef` 的 `references` 含 `Unity.RenderPipelines.Universal.Runtime`；`GameLogic.asmdef`、`GameProto.asmdef`、`GameBattle.asmdef` 不含任何 `Unity.RenderPipelines` 引用。
- A6: 以 Unity 6000.3.10f1 用 `-batchmode -quit -nographics -logFile` 打开 worktree 的 `GameUnity` 工程：日志 0 个 `error CS`，无 `Shader error`，无包解析失败（`Package resolution failed`/`Cannot resolve`），正常退出。
- A7: `Unity.exe -batchmode -quit -projectPath <worktree>/GameUnity -executeMethod HGame.URPSetupValidator.RunBatch -reportPath <report.json> -logFile ...`（不带 `-nographics`）退出码 0；报告 JSON `passed: true`，`packageResolved`、`pipelineAssigned`、`qualityLevelsAssigned`、`pipelineAssetConfig`、`cameraStack`、`canvasBinding`、`shaderErrors`、`renderSmoke` 全部 `passed`（`shaderErrors.detail` 记录两个 TMP Surface Shader 为 skipped），`renderSmoke.detail` 记录不同颜色像素数 > 1 且洋红像素数 = 0（该测试以 UICamera 临时作为 Base 相机渲染 UI 画布；相机堆叠配置由 `cameraStack`/`canvasBinding` 保证）。
- A8: `.claude/skills/hgame-dev/references/architecture.md` 与 `.codex/skills/hgame-dev/references/architecture.md` 均含 `URP 17.3`、`Assets/Settings/URP/`、`UIOverlayCameraBinder`；两份 `mcp-visual.md` 含 `GameLogic/Module/UIModule/Expansion`。
- A9: 相对目标分支 `Unity6000.3`，`git diff` 显示以下文件无改动：3 个自定义 Shader、`Assets/Plugins/TextMesh Pro/Shaders/**`、`Assets/BundleAssets/Materials/*.mat`、`Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`、`ProjectSettings/HybridCLRSettings.asset`、`ProjectSettings/Obfuz.asset`、`Assets/HGame/Runtime/Setting/UpdateSettings.asset`。

# 约束与不变量

- Build 期间目标 worktree `F:\Test\DGame\.worktrees\migrate-builtin-to-urp\GameUnity` 不能被 Unity 编辑器打开（批处理导入、包解析与校验工具都需要独占该工程）；当前打开在 `F:\Test\DGame\GameUnity` 的编辑器不受影响，但同一时间只能有一个 Unity 实例打开同一个工程目录。
- 首次以批处理模式打开 worktree 工程会完整导入 Library 并从 Unity registry 下载 URP 依赖包（本机缓存缺少 burst 1.8.14、collections 2.4.3、searcher 4.9.3；registry 已确认可访问）。
- 所有新增 `.asset`/`.cs`/`.prefab` 改动连带 `.meta` 提交；现有资产 GUID 不变。
- 不顺手重构与本次无关的代码。

# 决策

- D1（Agent，事实）：目标 URP 版本为 6000.3.10f1 编辑器内置的 `com.unity.render-pipelines.universal@17.3.0`。
- D2（Agent，事实）：工程为纯 UGUI（14 个预制体 + 2 个场景中只有 CanvasRenderer/Canvas/Camera，无 Light、无 MeshRenderer/SpriteRenderer/粒子、无烘焙数据、无后处理），5 个材质均使用项目自有 Shader。
- D3（用户 2026-09-10）：change 使用独立 worktree（`.worktrees/migrate-builtin-to-urp`，分支 `comet/migrate-builtin-to-urp`，目标分支 `Unity6000.3`）。
- D4（Agent 实现选择）：URP 资产统一放在 `Assets/Settings/URP/`，不进入 `Assets/BundleAssets`，避免被 YooAsset 收集。
- D5（Agent 实现选择）：相机堆叠维护逻辑放在 AOT 侧 `HGame.Runtime`（新组件挂在 `UIRoot.prefab` 的 `UICamera` 上），热更程序集不新增对 URP 程序集的引用，因此无需调整 HybridCLR 补充元数据与 link.xml。
- D6（用户 2026-09-10，Q1）：使用 Universal Renderer（Forward），不使用 2D Renderer。
- D7（用户 2026-09-10，Q2）：采用相机堆叠：Main Camera 为 Base，UICamera 为 Overlay 并加入堆叠，UICanvas 绑定到 UICamera；新增 `UIOverlayCameraBinder` 在场景切换后自动重新堆叠；`SceneUI.unity` 中 UICamera 覆盖为 Base 以保留编辑器预览。
- D8（用户 2026-09-10，Q3）：只建一个 URP Asset（MSAA/HDR/阴影全关），六个质量等级共用；Very High / Ultra 原有 2x MSAA 不再保留。
- D9（用户 2026-09-10，Q4）：编辑模式的离屏 `RenderRequest` 不输出 Overlay 相机内容（RenderGraph 与兼容模式、Screen Space-Camera 与 World Space 画布均实测为纯背景色），因此 `renderSmoke` 改为临时把 `UICamera` 作为 Base 相机渲染 UI 画布；相机堆叠配置由 `cameraStack`/`canvasBinding` 静态检查保证，运行时堆叠效果由用户打开工程 Play 目视确认。
- D10（Agent 实现选择）：保留 `URPMigrationTool.cs`（可重复执行的迁移/修复工具，校验工具复用其路径常量）；`UniversalRenderPipelineGlobalSettings` 是 internal 类型，通过 `RenderPipelineGlobalSettingsUtils.Create(Type, path)` 创建到 `Assets/Settings/URP/`；URP 在 `Assets/` 根目录自动生成的重复全局设置已删除，`DefaultVolumeProfile.asset` 移入 `Assets/Settings/URP/`。

# 待解决问题

- （无）目标、范围、非目标、A1–A9 验收项与 D1–D10 决策已由用户于 2026-09-10 确认（含 Q4 对 renderSmoke 方式的决定），进入 Build。

# 验证预期

- 批处理模式：`Unity.exe -batchmode -quit -nographics -projectPath <worktree>/GameUnity -logFile ...` 编译 0 个 `error CS`，日志无 Shader 编译错误。
- 批处理模式：`Unity.exe -batchmode -quit -projectPath <worktree>/GameUnity -executeMethod HGame.URPSetupValidator.RunBatch -reportPath <report.json>`（不带 `-nographics` 以便渲染烟雾测试）退出码 0，报告中全部检查项通过；首次在 worktree 运行前需把主工程 `GameUnity/Library` 复制到 worktree（gitee 上的 HybridCLR git 包本机无法重新克隆）。
- 文本检查：manifest 含 URP 包；`GraphicsSettings.asset`/`QualitySettings.asset` 引用 URP Asset 的 GUID；`GameStart.unity` 与 `UIRoot.prefab` 含 `UniversalAdditionalCameraData` 与堆叠/绑定字段。
