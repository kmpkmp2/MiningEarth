using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.Event;

namespace DeepEarth.Mining
{
    // 채굴 보상 계산 전담 정적 클래스. 새 Singleton/Manager가 아니며, 기존 Manager들의
    // 이미 동작 중인 보너스 로직(유물/버프/이벤트)을 그대로 이관해 재사용한다.
    public static class RewardCalculator
    {
        public static RewardModel Calculate(BlockType type, int depth, MineralData mineralData, DepthRewardTable depthRewardTable)
        {
            var model = new RewardModel { Type = type, ItemID = mineralData.itemID };

            model.Base = mineralData.baseRewardCount;
            int amount = model.Base;

            model.DepthBonus = depthRewardTable != null ? depthRewardTable.GetDepthBonus(type, depth) : 0;
            amount += model.DepthBonus;

            float bossMult = 1f + StatManager.Instance.BossResourceModifier;
            int afterBoss = Mathf.Max(1, Mathf.RoundToInt(amount * bossMult));
            model.BuffBonus += afterBoss - amount;
            amount = afterBoss;

            float oreMult = NodeEventManager.Instance != null ? NodeEventManager.Instance.GetOreGainMultiplier() : 1f;
            if (oreMult != 1f)
            {
                int afterEvent = Mathf.Max(1, Mathf.RoundToInt(amount * oreMult));
                model.EventBonus += afterEvent - amount;
                amount = afterEvent;
            }

            float oreTypeBonus = RelicManager.Instance != null ? RelicManager.Instance.GetOreTypeGainBonus(type) : 0f;
            if (oreTypeBonus > 0f)
            {
                int oreExtra = Mathf.Max(1, Mathf.RoundToInt(amount * oreTypeBonus));
                model.RelicBonus += oreExtra;
                amount += oreExtra;
            }

            var selectedChar = CharacterManager.Instance.SelectedCharacterID;
            if (CharacterManager.Instance.HasGraveRobberPassive(selectedChar))
            {
                float passiveChance = CharacterManager.Instance.GetCurrentPassiveValue(selectedChar);
                if (Random.value < passiveChance)
                {
                    int extra = Mathf.Max(1, Mathf.RoundToInt(amount * 0.1f));
                    model.BuffBonus += extra;
                    amount += extra;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"[Reward]\nGrave Robber Passive : +{extra} {type}");
#endif
                }
            }

            model.FinalAmount = amount;

            model.LuckyMineTriggered = RelicManager.Instance != null && RelicManager.Instance.CheckLuckyMineChance();
            model.MineHealTriggered = RelicManager.Instance != null && RelicManager.Instance.CheckMineHealChance();

            return model;
        }
    }
}
