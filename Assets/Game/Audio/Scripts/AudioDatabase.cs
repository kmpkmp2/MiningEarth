using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Audio
{
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "DeepEarth/Audio/AudioDatabase")]
    public class AudioDatabase : ScriptableObject
    {
        [SerializeField] private List<AudioClipData> _clips = new();

        public AudioClipData GetClip(string id)
            => _clips.Find(c => c != null && c.ID == id);
    }
}
