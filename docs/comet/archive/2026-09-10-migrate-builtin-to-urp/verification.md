---
generated_from_state_version: 10
---

# 验证

## 当前结果

- 结果: **已归档**
- 验证情况: **已完成检查，验证结果已确认**
- 目标周期: 2
- 迭代: 1
- 验证器尝试次数: 1
- 完成时间: 2026-09-10T15:24:45.274Z
- 摘要: A1–A9 全部通过：URP 17.3.0 包、Assets/Settings/URP 资产与配置、项目设置、相机堆叠/画布绑定、AOT 侧 UIOverlayCameraBinder 与 asmdef 边界、编译与校验工具报告、文档更新、受保护文件零改动均经独立取证确认。

## 验收

| 编号 | 结果 | 来源 | 验收项 | 原因 |
| --- | --- | --- | --- | --- |
| A1 | passed | brief.md | A1: `GameUnity/Packages/manifest.json` 含 `"com.unity.render-pipelines.universal": "17.3.0"`；`GameUnity/Packages/packages-lock.json` 中 `com.unity.render-pipelines.universal`、`com.unity.render-pipelines.core`、`com.unity.shadergraph` 版本均为 `17.3.0`。 | manifest.json:14 声明 universal 17.3.0；packages-lock.json 中 universal/core/shadergraph 均为 17.3.0（行 146、160、195） |
| A2 | passed | brief.md | A2: `GameUnity/Assets/Settings/URP/HGame_URPAsset.asset`、`HGame_URPRenderer.asset`、`UniversalRenderPipelineGlobalSettings.asset`、`DefaultVolumeProfile.asset` 及各自 `.meta` 存在，`GameUnity/Assets/` 根目录下没有 URP 自动生成的 `UniversalRenderPipelineGlobalSettings.asset` / `DefaultVolumeProfile.asset`；`HGame_URPAsset.asset` 序列化字段为 `m_SupportsHDR: 0`、`m_MSAA: 1`、`m_MainLightShadowsSupported: 0`、`m_AdditionalLightShadowsSupported: 0`、`m_RequireDepthTexture: 0`、`m_RequireOpaqueTexture: 0`、`m_RenderScale: 1`、`m_UseSRPBatcher: 1`、`m_SupportsDynamicBatching: 0`，且 `m_RendererDataList` 引用 `HGame_URPRenderer.asset`（`m_RenderingMode: 0` Forward）。 | Assets/Settings/URP 下 4 个 .asset + .meta 齐全，Assets 根无重复全局设置/VolumeProfile；HGame_URPAsset 九个字段值全部符合，m_RendererDataList GUID 6ad0393e… 对应 HGame_URPRenderer.asset.meta，Renderer m_RenderingMode 0 |
| A3 | passed | brief.md | A3: `ProjectSettings/GraphicsSettings.asset` 的 `m_CustomRenderPipeline` 与 `ProjectSettings/QualitySettings.asset` 六个质量等级的 `customRenderPipeline` 都引用 `HGame_URPAsset.asset.meta` 中的 GUID；`GraphicsSettings.asset` 的 `m_RenderPipelineGlobalSettingsMap` 含 `UnityEngine.Rendering.Universal.UniversalRenderPipeline` 条目；`ProjectSettings.asset` 的 `m_ActiveColorSpace` 仍为 0。 | GraphicsSettings m_CustomRenderPipeline 与六个质量等级 customRenderPipeline 均为 GUID a5affba9354f0b04cb9e565d61ba13cd；m_RenderPipelineGlobalSettingsMap 含 UniversalRenderPipeline → 44abb5e6…；ProjectSettings.asset m_ActiveColorSpace 0 且无 diff |
| A4 | passed | brief.md | A4: `GameStart.unity` 中 Main Camera 挂有 `UniversalAdditionalCameraData`（`m_CameraType: 0`），其 `m_Cameras` 包含 UIRoot 实例的 UICamera；`UIRoot.prefab` 中 UICamera 挂有 `UniversalAdditionalCameraData`（`m_CameraType: 1`）与 `UIOverlayCameraBinder`，`UICanvas` 的 `m_RenderMode: 1` 且 `m_Camera` 引用 UICamera；`SceneUI.unity` 中 UICamera 实例的 `m_CameraType` 覆盖为 0。 | GameStart.unity Main Camera UACD m_CameraType 0，m_Cameras 含 stripped Camera 3662584843660349294（源 UIRoot.prefab 3662584843330752890）；UIRoot.prefab UICamera UACD m_CameraType 1 + UIOverlayCameraBinder，UICanvas m_RenderMode 1、m_Camera 指向 UICamera；SceneUI.unity 覆盖 m_CameraType 0 |
| A5 | passed | brief.md | A5: `Assets/HGame/Runtime/Module/RenderPipeline/UIOverlayCameraBinder.cs` 存在于 `namespace HGame`，在 `OnEnable` 与 `SceneManager.sceneLoaded` 中把自身相机加入 `Camera.main` 的 `cameraStack`（已存在不重复），`Camera.main` 为空时不抛异常，`OnDisable` 注销回调；`HGame.Runtime.asmdef` 的 `references` 含 `Unity.RenderPipelines.Universal.Runtime`；`GameLogic.asmdef`、`GameProto.asmdef`、`GameBattle.asmdef` 不含任何 `Unity.RenderPipelines` 引用。 | UIOverlayCameraBinder.cs 位于 namespace HGame，OnEnable 订阅 sceneLoaded 并 Bind，Camera.main 为空/非 Base 直接 return，Contains 去重，OnDisable 注销；HGame.Runtime.asmdef 含 GUID 15fc0a57…（Unity.RenderPipelines.Universal.Runtime）；GameLogic/GameProto/GameBattle asmdef 与 Assets/Scripts 无任何 URP 引用 |
| A6 | passed | brief.md | A6: 以 Unity 6000.3.10f1 用 `-batchmode -quit -nographics -logFile` 打开 worktree 的 `GameUnity` 工程：日志 0 个 `error CS`，无 `Shader error`，无包解析失败（`Package resolution failed`/`Cannot resolve`），正常退出。 | unity_verify_compile.log（6000.3.10f1）error CS 0、Shader error 0、无 Package resolution failed/Cannot resolve，末尾 Exiting batchmode successfully / return code 0 |
| A7 | passed | brief.md | A7: `Unity.exe -batchmode -quit -projectPath <worktree>/GameUnity -executeMethod HGame.URPSetupValidator.RunBatch -reportPath <report.json> -logFile ...`（不带 `-nographics`）退出码 0；报告 JSON `passed: true`，`packageResolved`、`pipelineAssigned`、`qualityLevelsAssigned`、`pipelineAssetConfig`、`cameraStack`、`canvasBinding`、`shaderErrors`、`renderSmoke` 全部 `passed`（`shaderErrors.detail` 记录两个 TMP Surface Shader 为 skipped），`renderSmoke.detail` 记录不同颜色像素数 > 1 且洋红像素数 = 0（该测试以 UICamera 临时作为 Base 相机渲染 UI 画布；相机堆叠配置由 `cameraStack`/`canvasBinding` 保证）。 | urp-verify-report.json passed=true，8 项 passed；shaderErrors checked=14 skipped=2，renderSmoke distinctColors>=64、magentaPixels=0；smoke.png 目视含 UI 文字与下拉控件、无洋红；RunBatch 经 EditorApplication.Exit(0) 退出，Runtime 记录退出码 0 |
| A8 | passed | brief.md | A8: `.claude/skills/hgame-dev/references/architecture.md` 与 `.codex/skills/hgame-dev/references/architecture.md` 均含 `URP 17.3`、`Assets/Settings/URP/`、`UIOverlayCameraBinder`；两份 `mcp-visual.md` 含 `GameLogic/Module/UIModule/Expansion`。 | .claude 与 .codex 两份 architecture.md:104-106 含 URP 17.3 / Assets/Settings/URP/ / UIOverlayCameraBinder；两份 mcp-visual.md:98 含 GameLogic/Module/UIModule/Expansion |
| A9 | passed | brief.md | A9: 相对目标分支 `Unity6000.3`，`git diff` 显示以下文件无改动：3 个自定义 Shader、`Assets/Plugins/TextMesh Pro/Shaders/**`、`Assets/BundleAssets/Materials/*.mat`、`Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`、`ProjectSettings/HybridCLRSettings.asset`、`ProjectSettings/Obfuz.asset`、`Assets/HGame/Runtime/Setting/UpdateSettings.asset`。 | git diff --stat Unity6000.3 对 3 个自定义 Shader、TMP Shaders、BundleAssets/Materials、AssetBundleCollectorSetting.asset、HybridCLRSettings.asset、Obfuz.asset、UpdateSettings.asset 输出为空，且无未跟踪文件 |

## 检查

| 检查 | 命令 | 工作目录 | 状态 | 退出码 | 耗时 |
| --- | --- | --- | --- | ---: | ---: |
| A6 Unity 6000.3.10f1 batchmode -nographics compile (log: .comet/unity_verify_compile.log) | -batchmode -quit -nographics -projectPath F:/Test/DGame/.worktrees/migrate-builtin-to-urp/GameUnity -logFile F:/Test/DGame/.worktrees/migrate-builtin-to-urp/.comet/unity_verify_compile.log | . | passed | 0 | 18031 ms |
| A7 HGame.URPSetupValidator.RunBatch (report: .comet/urp-verify-report.json, dump: .comet/urp-verify-smoke.png) | -batchmode -quit -projectPath F:/Test/DGame/.worktrees/migrate-builtin-to-urp/GameUnity -executeMethod HGame.URPSetupValidator.RunBatch -reportPath F:/Test/DGame/.worktrees/migrate-builtin-to-urp/.comet/urp-verify-report.json -smokeDump F:/Test/DGame/.worktrees/migrate-builtin-to-urp/.comet/urp-verify-smoke.png -logFile F:/Test/DGame/.worktrees/migrate-builtin-to-urp/.comet/unity_verify_validate.log | . | passed | 0 | 20410 ms |

## 阻塞项

_无。_

## 风险与跳过的工作

- 运行时相机堆叠效果（Play 模式下 Overlay 相机经 Main Camera 堆叠显示、场景切换后 UIOverlayCameraBinder 重绑）未做目视验证，建议用户打开 GameStart 场景 Play 确认。
- URP 自动写入的副作用：GraphicsSettings m_LightsUseColorTemperature 0→1、Ultra 等级 antiAliasing 2→0、QualitySettings/场景 serializedVersion 升级；对纯 UGUI 工程无实际影响。
- 批处理日志开头出现 '.comet is not a valid directory name' 提示，源于命令行传入 .comet 目录路径，未影响输出。
- 验证日志因 EditorApplication.Exit 退出而不含 'Exiting batchmode successfully' 字样，退出码 0 依赖 Runtime 进程记录。

## 之前的迭代

| 目标周期 | 迭代 | 尝试 | 结果 | 未解决项 | 摘要 | 完成时间 |
| ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 1 | 0 | recovery | — | Native confirmed acceptance criteria changed | 2026-09-10T15:08:01.925Z |
| 2 | 1 | 1 | pass | — | A1–A9 全部通过：URP 17.3.0 包、Assets/Settings/URP 资产与配置、项目设置、相机堆叠/画布绑定、AOT 侧 UIOverlayCameraBinder 与 asmdef 边界、编译与校验工具报告、文档更新、受保护文件零改动均经独立取证确认。 | 2026-09-10T15:24:45.274Z |



## 结论

A1–A9 全部通过：URP 17.3.0 包、Assets/Settings/URP 资产与配置、项目设置、相机堆叠/画布绑定、AOT 侧 UIOverlayCameraBinder 与 asmdef 边界、编译与校验工具报告、文档更新、受保护文件零改动均经独立取证确认。
