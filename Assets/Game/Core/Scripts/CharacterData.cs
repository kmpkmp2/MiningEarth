using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.UI;

namespace DeepEarth.Core
{
    public enum CharacterID
    {
        Prisoner,
        Mercenary,
        Miner,
        GraveRobber,
        Adventurer,
        Blacksmith,
        Alchemist,
        TreasureHunter,
        Priest,
        Berserker
    }

    public enum PassiveType
    {
        None,
        AttackBonus,
        MiningBonus,
        GraveRobberPassive,
        EventChoiceBonus,
        PickaxeDurabilityReduction,
        PotionHealBonus,
        TreasureRewardBonus,
        CurseDurationReduction,
        LowHpAttackBonus
    }

    [Serializable]
    public class PassiveLevelEffect
    {
        public int Level;
        public string DescKey;
        public float Value;
        public int WillCost;
    }

    [Serializable]
    public class CharacterSaveEntry
    {
        public CharacterID ID;
        public bool IsUnlocked;
    }

    [Serializable]
    public class StartingItemEntry
    {
        public ItemData Item;
        public int Quantity;
    }

    [CreateAssetMenu(fileName = "Character_New", menuName = "DeepEarth/Character/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public CharacterID ID;
        public string NameKey;
        public string DescKey;
        public string FlavorKey;
        public string PlaystyleKey;

        [Header("Passive")]
        public PassiveType Passive;
        public string PassiveNameKey;
        public string PassiveDescKey;
        public List<PassiveLevelEffect> PassiveLevels = new List<PassiveLevelEffect>();

        [Header("Passive Display (표시 전용 — EffectManager/RunSetupPresenter가 switch 대신 참조)")]
        public bool PassiveValueIsPercent;
        public string PassiveValueFormatKey;
        public string PassiveHudIcon;
        public string PassiveHudFormat = "{0:0}";

        [Header("Unlock")]
        public List<PickaxeCostEntry> UnlockCost = new List<PickaxeCostEntry>();

        [Header("Starting Loadout")]
        public List<StartingItemEntry> StartingItems = new List<StartingItemEntry>();
        public StartingRelicData StartingRelic;
    }

    public static class CharacterDatabase
    {
        private static readonly List<CharacterData> _characters = new List<CharacterData>();
        private static bool _loaded;

        public static IReadOnlyList<CharacterData> Characters => _characters;

        public static async UniTask LoadAsync()
        {
            if (_loaded) return;
            var list = await ResourceManager.Instance.LoadAllByLabelAsync<CharacterData>(AddressableKeys.LabelCharacterData);
            _characters.Clear();
            if (list != null) _characters.AddRange(list.Where(c => c != null));
            _loaded = true;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Character]\nDatabase Loaded\nCount : {_characters.Count}");
#endif
        }

        public static CharacterData Get(CharacterID id)
        {
            return _characters.Find(c => c.ID == id);
        }
    }
}
