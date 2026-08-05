using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "GameBalanceData", menuName = "DeepEarth/GameBalanceData")]
    public class GameBalanceData : ScriptableObject
    {
        [Range(0f, 1f)] public float portableAnvilChestChance = 0.03f;
        [Range(0f, 1f)] public float immortalityPotionBossChance = 0.02f;

        public int ironToWill = 1;
        public int silverToWill = 2;
        public int goldToWill = 3;
        public int diamondToWill = 5;

        // Adventurer 패시브 — Event Node 보너스 선택지 보상
        public int AdventurerBonusStoneReward = 3;
        public int AdventurerBonusHealAmount = 1;

        public int CalculateOreWillValue(int iron, int silver, int gold, int diamond)
            => iron * ironToWill + silver * silverToWill + gold * goldToWill + diamond * diamondToWill;

        private static GameBalanceData _instance;
        public static GameBalanceData Instance => _instance != null ? _instance : (_instance = CreateInstance<GameBalanceData>());

        public static async UniTask LoadAsync()
        {
            if (_instance != null) return;

            _instance = await ResourceManager.Instance.LoadAssetAsync<GameBalanceData>(AddressableKeys.GameBalanceDataKey);
            if (_instance == null)
            {
                Debug.LogWarning("[Balance]\nGameBalanceData not found. Using runtime defaults.");
                _instance = CreateInstance<GameBalanceData>();
            }
        }
    }
}
