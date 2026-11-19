using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HGame
{
    /// <summary>
    /// 挂在 UI 相机（URP Overlay）上，把自身加入当前主相机（URP Base）的相机堆叠。
    /// 场景重载或切换后主相机可能被替换，因此在每次场景加载后重新绑定。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class UIOverlayCameraBinder : MonoBehaviour
    {
        private Camera m_camera;

        private void OnEnable()
        {
            m_camera = GetComponent<Camera>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Bind();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Bind();
        }

        private void Bind()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || mainCamera == m_camera)
            {
                return;
            }

            var baseData = mainCamera.GetUniversalAdditionalCameraData();
            if (baseData == null || baseData.renderType != CameraRenderType.Base)
            {
                return;
            }

            var stack = baseData.cameraStack;
            if (stack.Contains(m_camera))
            {
                return;
            }

            stack.Add(m_camera);
        }
    }
}
