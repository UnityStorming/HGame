using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HGame
{
    /// <summary>
    /// 一次性迁移工具：创建 URP 资产、接管 Graphics/Quality 设置，并把主相机与 UI 相机改为 URP 相机堆叠。
    /// 可重复执行，已存在的资产与设置会被复用。
    /// </summary>
    public static class URPMigrationTool
    {
        public const string SettingsFolder = "Assets/Settings/URP";
        public const string PipelineAssetPath = SettingsFolder + "/HGame_URPAsset.asset";
        public const string RendererAssetPath = SettingsFolder + "/HGame_URPRenderer.asset";
        public const string GlobalSettingsPath = SettingsFolder + "/UniversalRenderPipelineGlobalSettings.asset";
        public const string GameStartScenePath = "Assets/Scenes/GameStart/GameStart.unity";
        public const string SceneUIPath = "Assets/Scenes/SceneUI.unity";
        public const string UIRootPrefabPath = "Assets/HGame/Runtime/Setting/Prefabs/UIRoot.prefab";
        public const string UICameraName = "UICamera";
        public const string UICanvasName = "UICanvas";

        [MenuItem("HGame Tools/Render Pipeline/Migrate To URP", priority = 100)]
        public static void Migrate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            MigrateInternal();
        }

        /// <summary>批处理入口：-executeMethod HGame.URPMigrationTool.RunBatch</summary>
        public static void RunBatch()
        {
            try
            {
                MigrateInternal();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void MigrateInternal()
        {
            var pipelineAsset = EnsurePipelineAssets();
            AssignPipeline(pipelineAsset);
            SetupUIRootPrefab();
            SetupGameStartScene();
            SetupSceneUI();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[URPMigrationTool] 迁移完成。");
        }

        private static UniversalRenderPipelineAsset EnsurePipelineAssets()
        {
            CoreUtils.EnsureFolderTreeInAssetFilePath(PipelineAssetPath);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
                ResourceReloader.ReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);
            }

            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
            }

            var so = new SerializedObject(pipelineAsset);
            so.FindProperty("m_SupportsHDR").boolValue = false;
            so.FindProperty("m_MSAA").intValue = (int)MsaaQuality.Disabled;
            so.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            so.FindProperty("m_RequireDepthTexture").boolValue = false;
            so.FindProperty("m_RequireOpaqueTexture").boolValue = false;
            so.FindProperty("m_RenderScale").floatValue = 1f;
            so.FindProperty("m_UseSRPBatcher").boolValue = true;
            so.FindProperty("m_SupportsDynamicBatching").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipelineAsset);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            return pipelineAsset;
        }

        private static void AssignPipeline(UniversalRenderPipelineAsset pipelineAsset)
        {
            EnsureGlobalSettings();

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            int currentLevel = QualitySettings.GetQualityLevel();
            int levelCount = QualitySettings.names.Length;
            for (int i = 0; i < levelCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            QualitySettings.SetQualityLevel(currentLevel, false);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// UniversalRenderPipelineGlobalSettings 是 internal 类型，只能通过 Type 反射创建；
        /// 不提前创建的话 URP 会在默认路径 Assets/ 下自动生成。
        /// </summary>
        private static void EnsureGlobalSettings()
        {
            var settingsType = typeof(UniversalRenderPipeline).Assembly.GetType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings");
            if (settingsType == null)
            {
                throw new System.InvalidOperationException("找不到 UniversalRenderPipelineGlobalSettings 类型");
            }

            var settings = AssetDatabase.LoadAssetAtPath(GlobalSettingsPath, settingsType) as RenderPipelineGlobalSettings;
            if (settings == null)
            {
                settings = RenderPipelineGlobalSettingsUtils.Create(settingsType, GlobalSettingsPath);
            }

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(settings);
        }

        private static void SetupUIRootPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(UIRootPrefabPath);
            try
            {
                var uiCamera = FindChildComponent<Camera>(root.transform, UICameraName);
                var canvas = FindChildComponent<Canvas>(root.transform, UICanvasName);
                if (uiCamera == null || canvas == null)
                {
                    throw new System.InvalidOperationException($"UIRoot.prefab 缺少 {UICameraName} 或 {UICanvasName}");
                }

                uiCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                if (uiCamera.GetComponent<UIOverlayCameraBinder>() == null)
                {
                    uiCamera.gameObject.AddComponent<UIOverlayCameraBinder>();
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                PrefabUtility.SaveAsPrefabAsset(root, UIRootPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupGameStartScene()
        {
            var scene = EditorSceneManager.OpenScene(GameStartScenePath, OpenSceneMode.Single);
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                throw new System.InvalidOperationException("GameStart.unity 中找不到 MainCamera");
            }

            var uiCamera = FindSceneCamera(scene, UICameraName);
            if (uiCamera == null)
            {
                throw new System.InvalidOperationException($"GameStart.unity 中找不到 {UICameraName}");
            }

            var baseData = mainCamera.GetUniversalAdditionalCameraData();
            baseData.renderType = CameraRenderType.Base;
            if (!baseData.cameraStack.Contains(uiCamera))
            {
                baseData.cameraStack.Add(uiCamera);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupSceneUI()
        {
            var scene = EditorSceneManager.OpenScene(SceneUIPath, OpenSceneMode.Single);
            var uiCamera = FindSceneCamera(scene, UICameraName);
            if (uiCamera == null)
            {
                throw new System.InvalidOperationException($"SceneUI.unity 中找不到 {UICameraName}");
            }

            var uiCameraData = uiCamera.GetUniversalAdditionalCameraData();
            uiCameraData.renderType = CameraRenderType.Base;
            // 代码修改 prefab 实例字段必须显式记录，否则不会写入场景的 m_Modifications
            PrefabUtility.RecordPrefabInstancePropertyModifications(uiCameraData);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static Camera FindSceneCamera(Scene scene, string name)
        {
            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            foreach (var root in roots)
            {
                var camera = FindChildComponent<Camera>(root.transform, name);
                if (camera != null)
                {
                    return camera;
                }
            }

            return null;
        }

        public static T FindChildComponent<T>(Transform root, string name) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.name == name)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
