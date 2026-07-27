using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    public enum IntentType
    {
        Attack,
        HeavyAttack,
        Defend,
        Buff,
        Debuff,
        Heal,
        Summon,
        Special
    }

    [Serializable]
    public class IntentEntry
    {
        public IntentType type;
        public string iconKey;   // 실제 아이콘 스프라이트용 Addressables 키(향후 아트 준비 시 사용, 현재 미사용)
        public string iconGlyph; // TMP로 즉시 표시 가능한 이모지 글리프(예: "⚔") — 아트 없이도 시각 구분 제공
        public string descLocKey;
    }

    // IntentType → 아이콘/설명 조회 테이블. NodeIconData와 동일한 단일 asset 패턴.
    [CreateAssetMenu(fileName = "MonsterIntentData", menuName = "DeepEarth/MonsterIntentData")]
    public class MonsterIntentData : ScriptableObject
    {
        [SerializeField] private List<IntentEntry> entries = new List<IntentEntry>();

        public IntentEntry GetEntry(IntentType type) => entries.Find(e => e.type == type);
    }
}
