using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "BattleBalanceData", menuName = "DeepEarth/BattleBalanceData")]
    public class BattleBalanceData : ScriptableObject
    {
        [Range(0f, 1f)] public float defenseRate = 0.5f;

        // Fast Turn Battle: 모든 전투 연출 시간값. 코드에 하드코딩하지 않고 전부 여기서 관리한다.
        [Min(0f)] public float playerAttackAnimationTime = 0.10f;
        [Min(0f)] public float playerHitEffectTime = 0.15f;
        [Min(0f)] public float monsterAttackAnimationTime = 0.25f;
        [Min(0f)] public float monsterHitEffectTime = 0.15f;
        [Min(0f)] public float turnTransitionTime = 0.10f;
        [Min(0f)] public float intentAnimationTime = 0.15f;
        [Min(0f)] public float defenseAnimationTime = 0.10f;
        [Min(0f)] public float bossSkillAnimationTime = 0.5f;

        // Target Select: 인디케이터 등장/확정 연출 시간값.
        [Min(0f)] public float targetSelectScaleInTime = 0.10f;
        [Min(0f)] public float targetSelectFlashTime = 0.10f;

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
