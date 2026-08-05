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
        BuffMiningPower,
        BuffMaxHP,
        BuffInventory,
        BuffMonsterSpawnRateDecrease,
        BuffHazardSpawnRateDecrease,

        // Curses
        CurseAttackDamage,
        CurseMiningPower,
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

        // Relic modifier: monster attack bonus (FireCultMask 등)
        public int RelicMonsterAttackBonus { get; set; } = 0;
        // Relic modifier: mining power bonus
        public int RelicMiningModifier { get; set; } = 0;
        // Relic modifier: inventory slot bonus (SturdyBackpack 등)
        public int RelicInventoryBonus { get; set; } = 0;
        // Relic modifier: healing multiplier (GreedyCoin 등, 기본값 1.0)
        public float RelicHealMultiplier { get; set; } = 1.0f;

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
            RelicMonsterAttackBonus = 0;
            RelicMiningModifier = 0;
            RelicInventoryBonus = 0;
            RelicHealMultiplier = 1.0f;

            // Apply GlobalUpgrade and Character base stats
            var meta = MetaProgressionManager.Instance;
            var selectedChar = CharacterManager.Instance.SelectedCharacterID;
            var staticData = CharacterDatabase.Get(selectedChar);

            BaseMaxHP = 10 + (meta.MaxHPLevel - 1) * 2;
            BaseAttackDamage = 1 + (meta.AttackLevel - 1);
            int pickaxeMiningPower = PickaxeManager.Instance?.GetEquippedMiningPower() ?? 1;
            BaseMiningPower = pickaxeMiningPower + (meta.MiningPowerLevel - 1);
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

        public void RemoveEffect(EffectType type, int count = 1)
        {
            if (!_effectStacks.ContainsKey(type) || _effectStacks[type] <= 0) return;

            _effectStacks[type] = Mathf.Max(0, _effectStacks[type] - count);

            if (_effectStacks[type] == 0)
                EffectManager.Instance?.RemoveEffect(type.ToString());
            else
                RegisterEffectToManager(type, _effectStacks[type]);

            OnStatsUpdated?.Invoke();
            OnHPChanged?.Invoke();
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
                    display = $"⚔+{value}";
                    iconKey = "Effect_Buff_Attack";
                    break;
                case EffectType.BuffMiningPower:
                    nameKey = "effect_buff_mining_name";
                    descKey = "effect_buff_mining_desc";
                    value = stack * 1;
                    display = $"⛏+{value}";
                    iconKey = "Effect_Buff_Mining";
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
                    display = $"⚔-{value}";
                    iconKey = "Effect_Debuff_Attack";
                    break;
                case EffectType.CurseMiningPower:
                    nameKey = "effect_curse_mining_name";
                    descKey = "effect_curse_mining_desc";
                    value = stack * 1;
                    display = $"⛏-{value}";
                    iconKey = "Effect_Debuff_Mining";
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
            var selectedChar  = CharacterManager.Instance.SelectedCharacterID;
            int passiveBonus  = CharacterManager.Instance.GetPassiveAttackBonus(selectedChar);
            int buffModifier  = GetEffectStack(EffectType.BuffAttackDamage) * 1;
            int curseModifier = GetEffectStack(EffectType.CurseAttackDamage) * 1;
            // 시작 유물: 녹슨 검 (Mercenary) — 공격력 고정 가산
            int relicFixedAtk = StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetFixedAttackBonus() : 0;
            int baseResult    = Mathf.Max(1, BaseAttackDamage + passiveBonus + buffModifier - curseModifier + BossAttackModifier + relicFixedAtk);
            float statusMod   = StatusEffectManager.Instance?.GetTotalAttackModifier() ?? 0f;
            int statusAdjust  = Mathf.RoundToInt(baseResult * statusMod);
            // 유물: 전체 몬스터 피해 배율 (DragonFang 등)
            float relicDmgMult  = RelicManager.Instance?.GetDamageMultiplierBonus() ?? 0f;
            int relicMultAdjust = Mathf.RoundToInt(baseResult * relicDmgMult);

            // 신규 패시브: Berserker — 체력이 낮을수록 공격력 증가(100%→0% 선형, 최대치는 레벨로 조절)
            // + 시작 유물(핏빛 펜던트): 체력 30% 이하일 때 추가 보너스
            float maxBonus = CharacterManager.Instance.GetPassiveLowHpAttackBonusMax(selectedChar);
            int maxHp = GetMaxHP();
            float hpRatio = maxHp > 0 ? Mathf.Clamp01((float)CurrentHP / maxHp) : 1f;
            float lowHpBonus = maxBonus * (1f - hpRatio);
            float relicLowHpBonus = StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetLowHpAttackBonus() : 0f;
            if (relicLowHpBonus > 0f && hpRatio <= 0.3f) lowHpBonus += relicLowHpBonus;
            int berserkerAdjust = Mathf.RoundToInt(baseResult * lowHpBonus);

            return Mathf.Max(1, baseResult + statusAdjust + relicMultAdjust + berserkerAdjust);
        }

        public int GetMiningPower()
        {
            var selectedChar  = CharacterManager.Instance.SelectedCharacterID;
            int passiveBonus  = CharacterManager.Instance.GetPassiveMiningBonus(selectedChar);
            int buffModifier  = GetEffectStack(EffectType.BuffMiningPower) * 1;
            int curseModifier = GetEffectStack(EffectType.CurseMiningPower) * 1;
            int baseResult    = BaseMiningPower + passiveBonus + buffModifier - curseModifier + BossMiningModifier + RelicMiningModifier;
            float statusMod   = StatusEffectManager.Instance?.GetTotalMiningModifier() ?? 0f;
            int statusAdjust  = Mathf.RoundToInt(baseResult * statusMod);
            // 유물: 내구도 조건부 채굴력 보너스 (WorkGlove 등)
            int conditionalBonus = RelicManager.Instance?.GetConditionalMiningBonus() ?? 0;

            // 시작 유물: 광부 장갑 (Miner) — 광물 획득량 배율 가산
            float miningGainBonus = StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetMiningGainBonus() : 0f;
            int miningGainAdjust = Mathf.RoundToInt(baseResult * miningGainBonus);

            return Mathf.Max(1, baseResult + statusAdjust + conditionalBonus + miningGainAdjust);
        }

        public int GetInventorySize()
        {
            int upgradeBonus = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.InventorySizeLevel * 4 : 0;
            return BaseInventorySize + upgradeBonus + RelicInventoryBonus;
        }

        public float GetMonsterSpawnRateMultiplier()
        {
            float baseRate = 1.0f;
            float buffModifier = GetEffectStack(EffectType.BuffMonsterSpawnRateDecrease) * 0.15f; // -15% per stack
            float curseModifier = GetEffectStack(EffectType.CurseMonsterSpawnRateIncrease) * 0.25f; // +25% per stack
            float relicModifier = RelicManager.Instance?.GetRelicMonsterSpawnRateBonus() ?? 0f;
            return Mathf.Max(0.1f, (baseRate - buffModifier + curseModifier + relicModifier) * BossMonsterSpawnMultiplier);
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

            if (CurrentHP <= 0 && InventoryManager.Instance.GetItemCount(AddressableKeys.ItemImmortalityPotion) > 0)
            {
                InventoryManager.Instance.RemoveItem(AddressableKeys.ItemImmortalityPotion, 1);
                var itemData = InventoryManager.Instance.GetTemplate(AddressableKeys.ItemImmortalityPotion);
                int reviveHP = itemData != null ? itemData.reviveHP : 0;
                Heal(reviveHP);

                // 연출: 화면 암전 후 복귀, 황금빛 부활 이펙트, 카메라 강조 (실제 전용 이펙트/사운드 에셋은 후속 작업)
                EffectSystem.Instance.FlashScreen(new Color(1f, 0.85f, 0.2f, 0.6f), 0.6f);
                EffectSystem.Instance.ShakeCamera(0.4f, 0.2f);

                Vector3 revivePos = Camera.main != null
                    ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                    : transform.position + Vector3.up;
                EffectSystem.Instance.SpawnDamageText(revivePos, "REVIVED!", new Color(1f, 0.85f, 0.2f));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Item]\nImmortality Potion Activated\nHP Restored : {reviveHP}");
                Debug.Log("[Item]\nRevive Completed");
#endif
                return; // BossReviveCount 체크로 내려가지 않음 — 중복 부활 방지
            }

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
            if (amount <= 0) return;
            // 유물: 회복량 배율 적용 (GreedyCoin -50% 등)
            amount = Mathf.RoundToInt(amount * RelicHealMultiplier);
            if (amount <= 0) return;
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
