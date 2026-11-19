#if !UNITY_6000_3_OR_NEWER

using UnityEditor;

namespace HGame
{
    [InitializeOnLoad]
    public partial class UnityToolbarExtendLeft
    {
        static UnityToolbarExtendLeft()
        {
            UnityToolbarExtend.LEFT_TOOLBAR_GUI.Add(OnToolbarGUI_ScreenLauncher);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuit;
        }
    }
}

#endif