using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "BattleBalanceData", menuName = "DeepEarth/BattleBalanceData")]
    public class BattleBalanceData : ScriptableObject
    {
        [Range(0f, 1f)] public float defenseRate = 0.5f;
        [Min(0f)] public float turnDelay = 0.5f;
        [Min(0f)] public float intentAnimationTime = 0.2f;

        private static BattleBalanceData _instance;
        public static BattleBalanceData Instance => _instance != null ? _instance : (_instance = CreateInstance<BattleBalanceData>());

        public static async UniTask LoadAsync()
        {
            if (_instance != null) return;

            _instance = await ResourceManager.Instance.LoadAssetAsync<BattleBalanceData>(AddressableKeys.BattleBalanceDataKey);
            if (_instance == null)
            {
                Debug.LogWarning("[Battle]\nBattleBalanceData not found. Using runtime defaults.");
                _instance = CreateInstance<BattleBalanceData>();
            }
        }
    }
}
