using UnityEngine;

namespace HGame
{
    [CreateAssetMenu(fileName = "AudioSetting", menuName = "HGame/AudioSetting")]
    public class AudioSetting : ScriptableObject
    {
        [Tooltip("确保顺序和AudioType保持一致")]
        public AudioGroupConfig[] audioGroupConfigs = null;
    }
}