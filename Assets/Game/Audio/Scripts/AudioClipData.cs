using UnityEngine;

namespace DeepEarth.Audio
{
    public enum AudioCategory { BGM, SFX, UI, Voice, Ambient }

    [CreateAssetMenu(fileName = "AudioClipData", menuName = "DeepEarth/Audio/AudioClipData")]
    public class AudioClipData : ScriptableObject
    {
        public string        ID;
        public AudioCategory Category;
        public string        AddressableKey;
        [Range(0f, 1f)]
        public float         Volume  = 1f;
        public bool          Loop;
        public float         FadeIn;
        public float         FadeOut;
    }
}
