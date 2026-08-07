using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.UI;

namespace DeepEarth.Core
{
    [Serializable]
    public class RelicEffectData
    {
        public RelicEffectType effectType;
        public float value;

        // ── 트리거 시스템(그룹 A/C/D/E/I) — triggerEvent가 None이면 기존 방식(즉시적용/패시브조회) 그대로 ──
        public RelicTriggerEvent triggerEvent = RelicTriggerEvent.None;
        public DeepEarth.Map.RoomType triggerNodeType;      // triggerEvent가 NodeArrival/NodeCompletion일 때만 사용
        public List<ItemData> targetItems;                  // NodeItemGrant 전용 — 트리거 시 이 중 랜덤 1개 지급

        // ── 임시 버프 전용(그룹 D) ──────────────────────────────────────────
        public int buffDurationTurns;    // 0=즉시 1회성 효과, 1 이상=해당 턴수만큼 지속되는 임시 버프
        public int buffMaxStacks = 1;    // 임시 버프 최대 중첩 횟수

        // ── 조건부 발동 전용(그룹 C/G) ──────────────────────────────────────
        public float conditionHpRatioMax = 1f;   // 1=조건없음, 0.3="HP 30% 이하일 때만"

        // ── 실시간 파생 스탯 계산 전용(그룹 F/K) ──────────────────────────────
        public RelicScalingSource scalingSource = RelicScalingSource.None;
        public float scalingDivisor = 1f;        // "N당" 값 — 보너스 = floor(소스값 ÷ scalingDivisor) × value
    }

    [Serializable]
    public class RelicFallbackReward
    {
        public string itemID;
        public int amount;
    }

    [CreateAssetMenu(fileName = "RelicRewardConfig", menuName = "DeepEarth/Relic/RelicRewardConfig")]
    public class RelicRewardConfig : ScriptableObject
    {
        // 2026-08-07 4단계 등급 개편 — 목표 모집단 비율(전체 111종 기준 일반45%/레어28%/유니크20%/전설7%)과
        // 동일한 값을 기본 확률로 사용한다. Doc/Relic_Rule.md "등급 판정 기준" 절 참고.
        [Header("Default Rarity Chances (sum must equal 1)")]
        [Range(0f, 1f)] public float commonChance    = 0.45f;
        [Range(0f, 1f)] public float rareChance      = 0.28f;
        [Range(0f, 1f)] public float uniqueChance    = 0.20f;
        [Range(0f, 1f)] public float legendaryChance = 0.07f;

        [Header("Context Modifiers")]
        [Tooltip("Boss: legendary chance bonus (added, removes from common)")]
        [Range(0f, 0.5f)] public float bossLegendaryBonus = 0.10f;

        [Tooltip("Elite: minimum rare chance")]
        [Range(0f, 1f)] public float eliteRareMinimum = 0.50f;

        [Tooltip("CrystalShrine / Tombstone: rare is the lowest available rarity")]
        public bool rareMinimumEnabled = false;

        [Header("Fallback Rewards (all relics acquired)")]
        public List<RelicFallbackReward> fallbackRewards = new List<RelicFallbackReward>();

        // Returns (common, rare, unique, legendary) chances for the given context
        public (float c, float r, float u, float l) GetChances(RelicRewardContext ctx)
        {
            float c = commonChance;
            float r = rareChance;
            float u = uniqueChance;
            float l = legendaryChance;

            switch (ctx)
            {
                case RelicRewardContext.Boss:
                    l += bossLegendaryBonus;
                    c -= bossLegendaryBonus;
                    c = Mathf.Max(0f, c);
                    break;
                case RelicRewardContext.Elite:
                    float bonus = Mathf.Max(0f, eliteRareMinimum - r);
                    r += bonus;
                    c -= bonus;
                    c = Mathf.Max(0f, c);
                    break;
                case RelicRewardContext.CrystalShrine:
                case RelicRewardContext.Tombstone:
                    // Rare is the minimum — no common (Unique/Legendary 비율은 그대로 유지)
                    r += c;
                    c = 0f;
                    break;
                case RelicRewardContext.Merchant:
                    // No legendary in shops — Legendary 몫은 한 단계 아래인 Unique로 흡수
                    u += l;
                    l = 0f;
                    break;
                case RelicRewardContext.Treasure:
                    // 보물상자 유물 풀 자체가 rarity <= Unique라 Legendary 없음(Merchant와 동일 이유)
                    u += l;
                    l = 0f;
                    break;
            }

            // Normalise to sum = 1
            float total = c + r + u + l;
            if (total > 0) { c /= total; r /= total; u /= total; l /= total; }
            return (c, r, u, l);
        }
    }
}
