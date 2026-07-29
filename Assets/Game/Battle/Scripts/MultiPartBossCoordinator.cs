using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.Battle
{
    // 전금속 거인: 광석 코어가 하나라도 살아있는 동안 본체를 무적/선택불가로 유지한다.
    // 파트 파괴 시 광석 드랍 + 연출을 담당(기존 BossPresenter.HandleCoreDestroyed 로직 이식).
    public class MultiPartBossCoordinator
    {
        private int _alivePartCount;
        private Transform _bodyTransform;

        public bool MainBodyIsInvincible => _alivePartCount > 0;

        public void RegisterPart() => _alivePartCount++;
        public void RegisterBody(Transform bodyTransform) => _bodyTransform = bodyTransform;

        public void HandlePartDestroyed(BlockType oreType, Vector3 worldPos)
        {
            _alivePartCount = Mathf.Max(0, _alivePartCount - 1);

            string itemId = oreType switch
            {
                BlockType.Iron    => "Item_Iron",
                BlockType.Silver  => "Item_Silver",
                BlockType.Gold    => "Item_Gold",
                BlockType.Diamond => "Item_Diamond",
                _                 => ""
            };

            if (!string.IsNullOrEmpty(itemId))
            {
                InventoryManager.Instance.AddItem(itemId, 1);
                EffectSystem.Instance.SpawnDamageText(worldPos + Vector3.up * 0.5f, $"+{oreType}", Color.yellow);
            }

            EffectSystem.Instance.FlashScreen(new Color(1f, 0.8f, 0f, 0.3f), 0.2f);
            EffectSystem.Instance.ShakeCamera(0.3f, 0.1f);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Boss]\nCore Destroyed\nOre : {oreType}\nRemaining Cores : {_alivePartCount}");
#endif

            if (_alivePartCount == 0 && _bodyTransform != null)
            {
                EffectSystem.Instance.SpawnDamageText(_bodyTransform.position + Vector3.up * 1.0f, "COLOSSUS EXPOSED!", Color.red);
                EffectSystem.Instance.ShakeCamera(0.5f, 0.2f);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Boss]\nAll Cores Destroyed\nColossus Now Vulnerable");
#endif
            }
        }
    }
}
