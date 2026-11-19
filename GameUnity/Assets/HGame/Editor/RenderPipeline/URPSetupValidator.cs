using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HGame
{
    /// <summary>
    /// URP 配置校验工具：检查包、管线资产、项目设置、相机堆叠、Shader 编译与一次 UI 渲染烟雾测试，输出 JSON 报告。
    /// </summary>
    public static class URPSetupValidator
    {
        [Serializable]
        private class CheckResult
        {
            public string id;
            public string status;
            public string detail;
        }

        [Serializable]
        private class Report
        {
            public bool passed;
            public List<CheckResult> checks = new List<CheckResult>();
        }

        private const string StatusPassed = "passed";
        private const string StatusFailed = "failed";
        private const string StatusSkipped = "skipped";
        private const string DefaultReportPath = "Logs/urp-setup-report.json";
        private const int SmokeWidth = 750;
        private const int SmokeHeight = 1334;

        private static readonly string[] s_smokePrefabPaths =
        {
            "Assets/BundleAssets/UI/MainWindow.prefab",
            "Assets/BundleAssets/UI/GuideUI.prefab",
        };

        /// <summary>未被任何材质引用的 Built-in 表面着色器，不参与编译错误检查。</summary>
        private static readonly string[] s_skippedShaderPaths =
        {
            "Assets/Plugins/TextMesh Pro/Shaders/TMP_SDF-Surface.shader",
            "Assets/Plugins/TextMesh Pro/Shaders/TMP_SDF-Surface-Mobile.shader",
        };

        [MenuItem("HGame Tools/Render Pipeline/Validate URP Setup", priority = 101)]
        public static void Validate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var report = Run();
            string json = JsonUtility.ToJson(report, true);
            Debug.Log($"[URPSetupValidator] passed={report.passed}\n{json}");
        }

        /// <summary>批处理入口：-executeMethod HGame.URPSetupValidator.RunBatch -reportPath &lt;json&gt;</summary>
        public static void RunBatch()
        {
            int exitCode = 1;
            try
            {
                var report = Run();
                string reportPath = GetCommandLineArg("-reportPath") ?? DefaultReportPath;
                string directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
                Debug.Log($"[URPSetupValidator] passed={report.passed}, report={reportPath}");
                exitCode = report.passed ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            EditorApplication.Exit(exitCode);
        }

        private static Report Run()
        {
            var report = new Report();
            RunCheck(report, "packageResolved", CheckPackageResolved);
            RunCheck(report, "pipelineAssigned", CheckPipelineAssigned);
            RunCheck(report, "qualityLevelsAssigned", CheckQualityLevelsAssigned);
            RunCheck(report, "pipelineAssetConfig", CheckPipelineAssetConfig);
            RunCheck(report, "cameraStack", CheckCameraStack);
            RunCheck(report, "canvasBinding", CheckCanvasBinding);
            RunCheck(report, "shaderErrors", CheckShaderErrors);
            RunCheck(report, "renderSmoke", CheckRenderSmoke);

            report.passed = true;
            foreach (var check in report.checks)
            {
                if (check.status == StatusFailed)
                {
                    report.passed = false;
                }
            }

            return report;
        }

        private static void RunCheck(Report report, string id, Func<string> check)
        {
            var result = new CheckResult { id = id };
            try
            {
                result.detail = check();
                result.status = StatusPassed;
            }
            catch (Exception e)
            {
                result.detail = e.Message;
                result.status = StatusFailed;
            }

            report.checks.Add(result);
        }

        private static string CheckPackageResolved()
        {
            const string lockPath = "Packages/packages-lock.json";
            const string packageName = "com.unity.render-pipelines.universal";
            if (!File.Exists(lockPath))
            {
                throw new Exception($"{lockPath} 不存在");
            }

            if (!File.ReadAllText(lockPath).Contains($"\"{packageName}\""))
            {
                throw new Exception($"{lockPath} 未包含 {packageName}");
            }

            return $"{packageName} 已解析";
        }

        private static UniversalRenderPipelineAsset LoadPipelineAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPMigrationTool.PipelineAssetPath);
            if (asset == null)
            {
                throw new Exception($"找不到 {URPMigrationTool.PipelineAssetPath}");
            }

            return asset;
        }

        private static string CheckPipelineAssigned()
        {
            var expected = LoadPipelineAsset();
            if (GraphicsSettings.defaultRenderPipeline != expected)
            {
                throw new Exception($"GraphicsSettings.defaultRenderPipeline 不是 {URPMigrationTool.PipelineAssetPath}");
            }

            return URPMigrationTool.PipelineAssetPath;
        }

        private static string CheckQualityLevelsAssigned()
        {
            var expected = LoadPipelineAsset();
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) != expected)
                {
                    throw new Exception($"质量等级 [{names[i]}] 未指向 {URPMigrationTool.PipelineAssetPath}");
                }
            }

            return $"{names.Length} 个质量等级均已指向 URP Asset";
        }

        private static string CheckPipelineAssetConfig()
        {
            var asset = LoadPipelineAsset();
            var problems = new List<string>();
            if (asset.supportsHDR) problems.Add("HDR 应关闭");
            if (asset.msaaSampleCount != (int)MsaaQuality.Disabled) problems.Add("MSAA 应关闭");
            if (asset.supportsMainLightShadows) problems.Add("主光阴影应关闭");
            if (asset.supportsAdditionalLightShadows) problems.Add("附加光阴影应关闭");
            if (asset.supportsCameraDepthTexture) problems.Add("Depth Texture 应关闭");
            if (asset.supportsCameraOpaqueTexture) problems.Add("Opaque Texture 应关闭");
            if (!asset.useSRPBatcher) problems.Add("SRP Batcher 应开启");
            if (Mathf.Abs(asset.renderScale - 1f) > 0.0001f) problems.Add("Render Scale 应为 1");
            if (problems.Count > 0)
            {
                throw new Exception(string.Join("；", problems));
            }

            return "HDR/MSAA/阴影/Depth/Opaque 关闭，SRP Batcher 开启，Render Scale 1";
        }

        private static string CheckCameraStack()
        {
            var scene = EditorSceneManager.OpenScene(URPMigrationTool.GameStartScenePath, OpenSceneMode.Single);
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                throw new Exception("GameStart.unity 中找不到 MainCamera");
            }

            var uiCamera = URPMigrationTool.FindSceneCamera(scene, URPMigrationTool.UICameraName);
            if (uiCamera == null)
            {
                throw new Exception($"GameStart.unity 中找不到 {URPMigrationTool.UICameraName}");
            }

            var baseData = mainCamera.GetUniversalAdditionalCameraData();
            if (baseData.renderType != CameraRenderType.Base)
            {
                throw new Exception("Main Camera 不是 Base 相机");
            }

            if (!baseData.cameraStack.Contains(uiCamera))
            {
                throw new Exception("Main Camera 的相机堆叠不包含 UICamera");
            }

            if (uiCamera.GetUniversalAdditionalCameraData().renderType != CameraRenderType.Overlay)
            {
                throw new Exception("UICamera 不是 Overlay 相机");
            }

            if (uiCamera.GetComponent<UIOverlayCameraBinder>() == null)
            {
                throw new Exception("UICamera 缺少 UIOverlayCameraBinder");
            }

            return "Main Camera(Base) 堆叠包含 UICamera(Overlay, UIOverlayCameraBinder)";
        }

        private static string CheckCanvasBinding()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(URPMigrationTool.UIRootPrefabPath);
            if (prefab == null)
            {
                throw new Exception($"找不到 {URPMigrationTool.UIRootPrefabPath}");
            }

            var canvas = URPMigrationTool.FindChildComponent<Canvas>(prefab.transform, URPMigrationTool.UICanvasName);
            if (canvas == null)
            {
                throw new Exception($"UIRoot.prefab 中找不到 {URPMigrationTool.UICanvasName}");
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                throw new Exception("UICanvas 不是 Screen Space - Camera");
            }

            if (canvas.worldCamera == null || canvas.worldCamera.gameObject.name != URPMigrationTool.UICameraName)
            {
                throw new Exception("UICanvas.worldCamera 未绑定到 UICamera");
            }

            return "UICanvas(ScreenSpaceCamera) 绑定 UICamera";
        }

        private static string CheckShaderErrors()
        {
            var skipped = new HashSet<string>(s_skippedShaderPaths);
            var errors = new List<string>();
            int checkedCount = 0;
            int skippedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Shader"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (skipped.Contains(path))
                {
                    skippedCount++;
                    continue;
                }

                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null)
                {
                    continue;
                }

                checkedCount++;
                if (ShaderUtil.ShaderHasError(shader))
                {
                    errors.Add(path);
                }
            }

            if (errors.Count > 0)
            {
                throw new Exception("Shader 编译错误: " + string.Join(", ", errors));
            }

            return $"checked={checkedCount}, skipped={skippedCount} ({string.Join(", ", s_skippedShaderPaths)})";
        }

        private static string CheckRenderSmoke()
        {
            var scene = EditorSceneManager.OpenScene(URPMigrationTool.GameStartScenePath, OpenSceneMode.Single);
            var uiCamera = URPMigrationTool.FindSceneCamera(scene, URPMigrationTool.UICameraName);
            if (uiCamera == null)
            {
                throw new Exception("GameStart.unity 缺少 UICamera");
            }

            var canvas = uiCamera.transform.parent.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                throw new Exception("GameStart.unity 中找不到 UICanvas");
            }

            // 编辑模式的离屏 RenderRequest 不会输出 Overlay 相机的内容，因此临时把 UICamera 当作 Base 相机单独渲染 UI；
            // 相机堆叠配置本身由 cameraStack / canvasBinding 两项检查保证。
            var uiCameraData = uiCamera.GetUniversalAdditionalCameraData();
            var previousRenderType = uiCameraData.renderType;
            var previousTarget = uiCamera.targetTexture;
            var instances = new List<GameObject>();
            RenderTexture rt = null;
            Texture2D readback = null;
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var prefabPath in s_smokePrefabPaths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        throw new Exception($"找不到 {prefabPath}");
                    }

                    var instance = (GameObject)UnityEngine.Object.Instantiate(prefab, canvas.transform, false);
                    instance.SetActive(true);
                    instances.Add(instance);
                }

                Canvas.ForceUpdateCanvases();
                rt = new RenderTexture(SmokeWidth, SmokeHeight, 24, RenderTextureFormat.ARGB32);
                // URP 不支持 Camera.Render()，需通过 RenderRequest 提交
                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (!RenderPipeline.SupportsRenderRequest(uiCamera, request))
                {
                    throw new Exception("当前渲染管线不支持 StandardRequest");
                }

                uiCameraData.renderType = CameraRenderType.Base;
                RenderPipeline.SubmitRenderRequest(uiCamera, request);

                readback = new Texture2D(SmokeWidth, SmokeHeight, TextureFormat.RGBA32, false);
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, SmokeWidth, SmokeHeight), 0, 0);
                readback.Apply();

                var pixels = readback.GetPixels32();
                var distinct = new HashSet<uint>();
                int magenta = 0;
                foreach (var pixel in pixels)
                {
                    if (pixel.r >= 250 && pixel.g <= 5 && pixel.b >= 250)
                    {
                        magenta++;
                    }

                    if (distinct.Count < 64)
                    {
                        distinct.Add((uint)(pixel.r << 16 | pixel.g << 8 | pixel.b));
                    }
                }

                string detail = $"distinctColors={(distinct.Count >= 64 ? ">=64" : distinct.Count.ToString())}, magentaPixels={magenta}";
                string dumpPath = GetCommandLineArg("-smokeDump");
                if (!string.IsNullOrEmpty(dumpPath))
                {
                    string dumpDirectory = Path.GetDirectoryName(dumpPath);
                    if (!string.IsNullOrEmpty(dumpDirectory))
                    {
                        Directory.CreateDirectory(dumpDirectory);
                    }

                    File.WriteAllBytes(dumpPath, readback.EncodeToPNG());
                    detail += $", dump={dumpPath}";
                }

                if (distinct.Count <= 1)
                {
                    throw new Exception("渲染结果为单一颜色，UI 未绘制: " + detail);
                }

                if (magenta > 0)
                {
                    throw new Exception("渲染结果含 URP 错误色洋红: " + detail);
                }

                return detail;
            }
            finally
            {
                RenderTexture.active = previousActive;
                uiCameraData.renderType = previousRenderType;
                uiCamera.targetTexture = previousTarget;
                foreach (var instance in instances)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }

                if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
                EditorSceneManager.OpenScene(URPMigrationTool.GameStartScenePath, OpenSceneMode.Single);
            }
        }

        private static string GetCommandLineArg(string argName)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == argName && i + 1 < args.Length)
                {
                    return args[i + 1];
                }

                if (args[i].StartsWith(argName + "=", StringComparison.Ordinal))
                {
                    return args[i].Substring(argName.Length + 1);
                }
            }

            return null;
        }
    }
}
