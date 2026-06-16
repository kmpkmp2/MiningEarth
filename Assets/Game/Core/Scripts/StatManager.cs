using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public enum EffectType
    {
        // Buffs
        BuffAttackDamage,
        BuffMaxHP,
        BuffInventory,
        BuffMonsterSpawnRateDecrease,
        BuffHazardSpawnRateDecrease,

        // Curses
        CurseAttackDamage,
        CurseMaxHP,
        CurseMonsterSpawnRateIncrease,
        CurseHazardSpawnRateIncrease,
        CurseInstantDamageOnEncounter,
        CurseMiningFailChance
    }

    public class StatManager : MonoBehaviour
    {
        private static StatManager _instance;
        public static StatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("StatManager");
                    _instance = go.AddComponent<StatManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Base Stats (Default run stats)
        public int BaseMaxHP { get; private set; } = 10;
        public int CurrentHP { get; private set; }
        public int BaseAttackDamage { get; private set; } = 1;
        public int BaseMiningPower { get; private set; } = 1;
        public int BaseInventorySize { get; private set; } = 24;

        // Boss run-local modifiers (not saved)
        public int BossAttackModifier { get; set; } = 0;
        public int BossMaxHPModifier { get; set; } = 0;
        public int BossMiningModifier { get; set; } = 0;
        public float BossResourceModifier { get; set; } = 0f;
        public float BossMonsterSpawnMultiplier { get; set; } = 1f;
        public float BossHealDropChanceModifier { get; set; } = 0f;
        public int BossReviveCount { get; set; } = 0;
        public float BossDamageToBossMultiplier { get; set; } = 1f;
        public bool BossRareEventDouble { get; set; } = false;

        // Buff / Curse stacks (Limited to max 3 per effect type)
        private readonly Dictionary<EffectType, int> _effectStacks = new Dictionary<EffectType, int>();

        public event Action OnHPChanged;
        public event Action OnStatsUpdated;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ResetStatsForRun()
        {
            _effectStacks.Clear();

            // Reset Boss modifiers
            BossAttackModifier = 0;
            BossMaxHPModifier = 0;
            BossMiningModifier = 0;
            BossResourceModifier = 0f;
            BossMonsterSpawnMultiplier = 1f;
            BossHealDropChanceModifier = 0f;
            BossReviveCount = 0;
            BossDamageToBossMultiplier = 1f;
            BossRareEventDouble = false;

            // Apply Meta Upgrades and Character stats
            var meta = MetaProgressionManager.Instance;
            var selectedChar = CharacterManager.Instance.SelectedCharacterID;
            
            BaseMaxHP = 10 + (meta.MaxHPLevel - 1) * 2;
            BaseAttackDamage = 1 + (meta.AttackLevel - 1) + CharacterManager.Instance.GetStartingAttackBonus(selectedChar);
            BaseMiningPower = 1 + (meta.MiningPowerLevel - 1) + CharacterManager.Instance.GetStartingMiningBonus(selectedChar);
            BaseInventorySize = 24; // Base Capacity is 24

            int upgradeBonus = meta.InventorySizeLevel * 4;
            int finalCapacity = BaseInventorySize + upgradeBonus;

            Debug.Log($"[Inventory]\nBase Capacity : {BaseInventorySize}");
            Debug.Log($"[Inventory]\nUpgrade Bonus : +{upgradeBonus}");
            Debug.Log($"[Inventory]\nFinal Capacity : {finalCapacity}");

            CurrentHP = BaseMaxHP;
            
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.ClearRunEffects();
                EffectManager.Instance.InitializeCharacterPassive(selectedChar);
            }

            OnHPChanged?.Invoke();
            OnStatsUpdated?.Invoke();
        }

        public void AddEffect(EffectType type)
        {
            if (!_effectStacks.ContainsKey(type))
            {
                _effectStacks[type] = 0;
            }

            if (_effectStacks[type] < GameSettings.MaxBuffDebuffStack)
            {
                _effectStacks[type]++;
                
                // If Max HP increases, give immediate HP boost
                if (type == EffectType.BuffMaxHP)
                {
                    int prevMax = GetMaxHP();
                    CurrentHP += 2; // Increase current HP along with max HP
                    int newMax = GetMaxHP();
                    CurrentHP = Mathf.Clamp(CurrentHP, 0, newMax);
                }
                else if (type == EffectType.CurseMaxHP)
                {
                    int newMax = GetMaxHP();
                    CurrentHP = Mathf.Clamp(CurrentHP, 0, newMax);
                }

                if (EffectManager.Instance != null)
                {
                    RegisterEffectToManager(type, _effectStacks[type]);
                }

                OnStatsUpdated?.Invoke();
                OnHPChanged?.Invoke();
                Debug.Log($"Applied effect: {type}, current stack: {_effectStacks[type]}");
            }
        }

        private void RegisterEffectToManager(EffectType type, int stack)
        {
            string id = type.ToString();
            string nameKey = "";
            string descKey = "";
            EffectSystemType systemType = (type.ToString().StartsWith("Buff")) ? EffectSystemType.Buff : EffectSystemType.Debuff;
            float value = 0;
            string display = "";
            string iconKey = "";
            string source = "Chest/Event";

            switch (type)
            {
                case EffectType.BuffAttackDamage:
                    nameKey = "effect_buff_attack_name";
                    descKey = "effect_buff_attack_desc";
                    value = stack * 1;
                    display = $"⚔{value}";
                    iconKey = "Effect_Buff_Attack";
                    break;
                case EffectType.BuffMaxHP:
                    nameKey = "effect_buff_maxhp_name";
                    descKey = "effect_buff_maxhp_desc";
                    value = stack * 2;
                    display = $"❤{value}";
                    iconKey = "Effect_Buff_MaxHP";
                    break;
                case EffectType.BuffInventory:
                    nameKey = "effect_buff_inventory_name";
                    descKey = "effect_buff_inventory_desc";
                    value = stack * 5;
                    display = $"📦{value}";
                    iconKey = "Effect_Buff_Inventory";
                    break;
                case EffectType.BuffMonsterSpawnRateDecrease:
                    nameKey = "effect_buff_monsterspawn_name";
                    descKey = "effect_buff_monsterspawn_desc";
                    value = stack * 15;
                    display = $"-{value}%";
                    iconKey = "Effect_Buff_MonsterDecrease";
                    break;
                case EffectType.BuffHazardSpawnRateDecrease:
                    nameKey = "effect_buff_hazardspawn_name";
                    descKey = "effect_buff_hazardspawn_desc";
                    value = stack * 15;
                    display = $"-{value}%";
                    iconKey = "Effect_Buff_HazardDecrease";
                    break;
                case EffectType.CurseAttackDamage:
                    nameKey = "effect_curse_attack_name";
                    descKey = "effect_curse_attack_desc";
                    value = stack * 1;
                    display = $"-{value}";
                    iconKey = "Effect_Debuff_Attack";
                    break;
                case EffectType.CurseMaxHP:
                    nameKey = "effect_curse_maxhp_name";
                    descKey = "effect_curse_maxhp_desc";
                    value = stack * 2;
                    display = $"-{value}";
                    iconKey = "Effect_Debuff_MaxHP";
                    break;
                case EffectType.CurseMonsterSpawnRateIncrease:
                    nameKey = "effect_curse_monsterspawn_name";
                    descKey = "effect_curse_monsterspawn_desc";
                    value = stack * 25;
                    display = $"☠{value}%";
                    iconKey = "Effect_Debuff_MonsterEncounter";
                    break;
                case EffectType.CurseHazardSpawnRateIncrease:
                    nameKey = "effect_curse_hazardspawn_name";
                    descKey = "effect_curse_hazardspawn_desc";
                    value = stack * 25;
                    display = $"🔥{value}%";
                    iconKey = "Effect_Debuff_HazardEncounter";
                    break;
                case EffectType.CurseInstantDamageOnEncounter:
                    nameKey = "effect_curse_instantdamage_name";
                    descKey = "effect_curse_instantdamage_desc";
                    value = stack * 1;
                    display = $"-{value}";
                    iconKey = "Effect_Debuff_InstantDamage";
                    break;
                case EffectType.CurseMiningFailChance:
                    nameKey = "effect_curse_miningfail_name";
                    descKey = "effect_curse_miningfail_desc";
                    value = stack * 15;
                    display = $"{value}%";
                    iconKey = "Effect_Debuff_MiningFail";
                    break;
            }

            EffectManager.Instance.RegisterEffect(id, nameKey, descKey, systemType, value, display, source, iconKey);
        }

        public int GetEffectStack(EffectType type)
        {
            return _effectStacks.TryGetValue(type, out int stack) ? stack : 0;
        }

        public int GetMaxHP()
        {
            int buffModifier = GetEffectStack(EffectType.BuffMaxHP) * 2;
            int curseModifier = GetEffectStack(EffectType.CurseMaxHP) * 2;
            return Mathf.Max(1, BaseMaxHP + buffModifier - curseModifier + BossMaxHPModifier);
        }

        public int GetAttackDamage()
        {
            var selectedChar = CharacterManager.Instance.SelectedCharacterID;
            int passiveBonus = CharacterManager.Instance.GetPassiveAttackBonus(selectedChar);
            int buffModifier = GetEffectStack(EffectType.BuffAttackDamage) * 1;
            int curseModifier = GetEffectStack(EffectType.CurseAttackDamage) * 1;
            return Mathf.Max(1, BaseAttackDamage + passiveBonus + buffModifier - curseModifier + BossAttackModifier);
        }

        public int GetMiningPower()
        {
            var selectedChar = CharacterManager.Instance.SelectedCharacterID;
            int passiveBonus = CharacterManager.Instance.GetPassiveMiningBonus(selectedChar);
            int buffModifier = GetEffectStack(EffectType.BuffAttackDamage) * 1;
            int curseModifier = GetEffectStack(EffectType.CurseAttackDamage) * 1;
            return Mathf.Max(1, BaseMiningPower + passiveBonus + buffModifier - curseModifier + BossMiningModifier);
        }

        public int GetInventorySize()
        {
            int upgradeBonus = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.InventorySizeLevel * 4 : 0;
            return BaseInventorySize + upgradeBonus;
        }

        public float GetMonsterSpawnRateMultiplier()
        {
            float baseRate = 1.0f;
            float buffModifier = GetEffectStack(EffectType.BuffMonsterSpawnRateDecrease) * 0.15f; // -15% per stack
            float curseModifier = GetEffectStack(EffectType.CurseMonsterSpawnRateIncrease) * 0.25f; // +25% per stack
            return Mathf.Max(0.1f, (baseRate - buffModifier + curseModifier) * BossMonsterSpawnMultiplier);
        }

        public float GetHazardSpawnRateMultiplier()
        {
            float baseRate = 1.0f;
            float buffModifier = GetEffectStack(EffectType.BuffHazardSpawnRateDecrease) * 0.15f;
            float curseModifier = GetEffectStack(EffectType.CurseHazardSpawnRateIncrease) * 0.25f;
            return Mathf.Max(0.1f, baseRate - buffModifier + curseModifier);
        }

        public bool CheckMiningFailure()
        {
            int curseStack = GetEffectStack(EffectType.CurseMiningFailChance);
            if (curseStack == 0) return false;

            float failProbability = curseStack * 0.15f; // 15% per stack
            return UnityEngine.Random.value < failProbability;
        }

        public int GetEncounterInstantDamage()
        {
            int curseStack = GetEffectStack(EffectType.CurseInstantDamageOnEncounter);
            return curseStack * 1; // 1 damage per stack
        }

        public void TakeDamage(int amount)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnHPChanged?.Invoke();

            if (CurrentHP <= 0 && BossReviveCount > 0)
            {
                BossReviveCount--;
                Heal(GetMaxHP());

                // Premium visual feedback on resurrection
                EffectSystem.Instance.FlashScreen(new Color(0.2f, 0.9f, 0.3f, 0.4f), 0.5f);
                EffectSystem.Instance.ShakeCamera(0.3f, 0.15f);
                
                Vector3 textWorldPos = Camera.main != null 
                    ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                    : transform.position + Vector3.up;

                EffectSystem.Instance.SpawnDamageText(textWorldPos, "REVIVED! +100% HP", Color.green);
            }
        }

        public void Heal(int amount)
        {
            int max = GetMaxHP();
            CurrentHP = Mathf.Min(max, CurrentHP + amount);
            OnHPChanged?.Invoke();
        }

        public void TriggerStatsUpdated()
        {
            OnStatsUpdated?.Invoke();
            OnHPChanged?.Invoke();
        }

        public void ApplyBurn(int ticks, int dmg)
        {
            StartCoroutine(CoBurn(ticks, dmg));
        }

        private System.Collections.IEnumerator CoBurn(int ticks, int dmg)
        {
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(1.0f);
                if (CurrentHP <= 0) yield break;

                TakeDamage(dmg);

                // Fiery orange flash and BURN text feedback
                EffectSystem.Instance.FlashScreen(new Color(1f, 0.5f, 0f, 0.25f), 0.2f);
                EffectSystem.Instance.ShakeCamera(0.12f, 0.06f);
                
                Vector3 textWorldPos = Camera.main != null 
                    ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Camera.main.transform.right * 0.5f
                    : transform.position + Vector3.up;

                EffectSystem.Instance.SpawnDamageText(textWorldPos, "BURN! -1 HP", new Color(1f, 0.4f, 0f));
            }
        }
    }
}
