using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [Serializable]
    public class ShieldValueEntry
    {
        public BlockType mineralType;
        public int shieldValue;
    }

    [CreateAssetMenu(fileName = "ShieldData", menuName = "DeepEarth/ShieldData")]
    public class ShieldData : ScriptableObject
    {
        public List<ShieldValueEntry> shieldValues = new List<ShieldValueEntry>();

        public int GetShieldValue(BlockType mineralType)
        {
            for (int i = 0; i < shieldValues.Count; i++)
                if (shieldValues[i].mineralType == mineralType) return shieldValues[i].shieldValue;
            return 0;
        }

        private static ShieldData _instance;
        public static ShieldData Instance => _instance != null ? _instance : (_instance = CreateInstance<ShieldData>());

        public static async UniTask LoadAsync()
        {
            if (_instance != null) return;

            _instance = await ResourceManager.Instance.LoadAssetAsync<ShieldData>(AddressableKeys.ShieldDataKey);
            if (_instance == null)
            {
                Debug.LogWarning("[Battle]\nShieldData not found. Using runtime defaults (all shield gains = 0).");
                _instance = CreateInstance<ShieldData>();
            }
        }
    }
}
