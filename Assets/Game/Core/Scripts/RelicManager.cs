using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class RelicManager : MonoBehaviour
    {
        private static RelicManager _instance;
        public static RelicManager Instance => _instance;

        private const string LabelRelic         = "Relic";
        private const string RewardConfigKey     = "Relic_RewardConfig";

        private List<RelicData>       _allRelics    = new List<RelicData>();
        private RelicRewardConfig     _rewardConfig;

        private readonly List<RelicData>   _activeRelics  = new List<RelicData>();
        private readonly HashSet<string>   _acquiredIDs   = new HashSet<string>();

        // ReviveOnce: 런 중 1회만 사용 가능한 부활 플래그
        private bool _reviveConsumed = false;
        // RestoreRelicsFromSave 중 중간 세이브 억제 플래그
        private bool _restoring = false;

        private void Awake()
        {
            if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
            else Destroy(gameObject);
        }

        // ── Init ────────────────────────────────────────────────────────────

        public async UniTask InitializeAsync()
        {
            var relics = await ResourceManager.Instance.LoadAllByLabelAsync<RelicData>(LabelRelic);
            _allRelics.Clear();
            if (relics != null) _allRelics.AddRange(relics.Where(r => r != null));

            _rewardConfig = await ResourceManager.Instance.LoadAssetAsync<RelicRewardConfig>(RewardConfigKey);
            if (_rewardConfig == null)
            {
                _rewardConfig = ScriptableObject.CreateInstance<RelicRewardConfig>();
                Debug.LogWarning("[Relic]\nRelicRewardConfig not found — using defaults.");
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Relic]\nLoaded\nCount : {_allRelics.Count}");
            foreach (var r in _allRelics)
                Debug.Log($"[Relic]\nLoaded\nID : {r.relicID}\nRarity : {r.rarity}");
#endif
        }

        // ── Pool Queries ─────────────────────────────────────────────────────

        public List<RelicData> GetAvailable(RelicRewardContext ctx = RelicRewardContext.Standard)
        {
            switch (ctx)
            {
                case RelicRewardContext.CrystalShrine:
                case RelicRewardContext.Tombstone:
                    return _allRelics.Where(r =>
                        !_acquiredIDs.Contains(r.relicID) &&
                        r.rarity >= RelicRarity.Rare).ToList();

                case RelicRewardContext.Merchant:
                    return _allRelics.Where(r =>
                        !_acquiredIDs.Contains(r.relicID) &&
                        r.rarity <= RelicRarity.Rare).ToList();

                default:
                    return _allRelics.Where(r => !_acquiredIDs.Contains(r.relicID)).ToList();
            }
        }

        // Backward-compat wrappers used by EventManager (Treasure/Tombstone events)
        public List<RelicData> GetAvailableTreasureRelics() =>
            _allRelics.Where(r =>
                !_acquiredIDs.Contains(r.relicID) &&
                r.rarity <= RelicRarity.Rare).ToList();

        public List<RelicData> GetAvailableTombstoneRelics() =>
            _allRelics.Where(r =>
                !_acquiredIDs.Contains(r.relicID) &&
                r.rarity >= RelicRarity.Rare).ToList();

        // ── Rarity-weighted random selection ─────────────────────────────────

        // Returns up to `count` distinct relics, respecting rarity weighting.
        public List<RelicData> GetRandomRelicChoices(int count,
            RelicRewardContext ctx = RelicRewardContext.Standard)
        {
            var pool   = GetAvailable(ctx).ToList();
            var result = new List<RelicData>();
            var used   = new HashSet<string>();

            while (result.Count < count && pool.Count > 0)
            {
                var target = RollRarity(ctx);
                var sub = pool.Where(x => x.rarity == target && !used.Contains(x.relicID)).ToList();
                if (sub.Count == 0) sub = pool.Where(x => !used.Contains(x.relicID)).ToList();
                if (sub.Count == 0) break;

                var pick = sub[Random.Range(0, sub.Count)];
                result.Add(pick);
                used.Add(pick.relicID);
                pool.Remove(pick);
            }
            return result;
        }

        private RelicRarity RollRarity(RelicRewardContext ctx)
        {
            var (c, r, l) = _rewardConfig != null ? _rewardConfig.GetChances(ctx) : (0.65f, 0.28f, 0.07f);
            float roll = Random.value;
            if (roll < l)     return RelicRarity.Legendary;
            if (roll < l + r) return RelicRarity.Rare;
            return RelicRarity.Common;
        }

        // ── Acquire ──────────────────────────────────────────────────────────

        public void AddRelic(RelicData relic)
        {
            if (relic == null || _acquiredIDs.Contains(relic.relicID)) return;

            _activeRelics.Add(relic);
            _acquiredIDs.Add(relic.relicID);

            // Apply effect list (new system)
            if (relic.effects != null && relic.effects.Count > 0)
            {
                foreach (var effect in relic.effects)
                    ApplyEffect(effect);
            }
            else
            {
                // Legacy fallback for pre-migration assets
                ApplyLegacyFields(relic);
            }

            EffectSystemType displayType = RarityToEffectSystemType(relic.rarity);
            string rarityLabel = RarityLocKey(relic.rarity);

            EffectManager.Instance?.RegisterEffect(
                relic.relicID,
                relic.nameLocKey,
                relic.descLocKey,
                displayType,
                0f,
                BuildDisplayString(relic),
                rarityLabel,
                relic.iconKey
            );

            SaveActiveRelicIDs();

            StatManager.Instance.TriggerStatsUpdated();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Relic]\nObtained\nName : {relic.relicID}\nRarity : {relic.rarity}");
#endif
            GameEvents.FireRelicCollected();
        }

        private void ApplyEffect(RelicEffectData effect)
        {
            int intVal = Mathf.RoundToInt(effect.value);
            switch (effect.effectType)
            {
                // ── 기존 직접 적용 ──────────────────────────────────────────────
                case RelicEffectType.AttackBonus:
                    StatManager.Instance.BossAttackModifier += intVal;
                    break;
                case RelicEffectType.MiningPowerBonus:
                    StatManager.Instance.RelicMiningModifier += intVal;
                    break;
                case RelicEffectType.MaxHPBonus:
                    StatManager.Instance.BossMaxHPModifier += intVal;
                    if (intVal > 0) StatManager.Instance.Heal(intVal);
                    break;
                case RelicEffectType.ResourceMultiplierBonus:
                    StatManager.Instance.BossResourceModifier += effect.value;
                    break;
                case RelicEffectType.MonsterAttackBonus:
                    StatManager.Instance.RelicMonsterAttackBonus += intVal;
                    break;
                case RelicEffectType.PickaxeDurabilityModifier:
                    PickaxeDurabilityManager.Instance?.ApplyRelicDurabilityModifier(intVal);
                    break;

                // ── 신규 직접 적용 ──────────────────────────────────────────────
                case RelicEffectType.InventorySlotBonus:
                    StatManager.Instance.RelicInventoryBonus += intVal;
                    break;
                case RelicEffectType.PickaxeMaxDurabilityBonus:
                    PickaxeDurabilityManager.Instance?.AddMaxDurabilityBonus(intVal);
                    break;
                case RelicEffectType.HealingMultiplierModifier:
                    StatManager.Instance.RelicHealMultiplier *= effect.value;
                    break;

                // ── 패시브 쿼리 타입은 _activeRelics 에서 런타임 조회 ─────────────
                // BurnDuration/Damage/Immunity, MonsterSpawnRate, IronGainBonus~,
                // PostCombatHealBonus, PostBossHealBonus, BossKillFullHeal,
                // PickaxeDurabilityRateModifier, PickaxeNoDurabilityLoss, PickaxeRepairOnKill,
                // TrapDamageReduction, EliteDamageBonus, DamageMultiplierBonus,
                // LuckyMineChance, MineHealChance, KillIronChance,
                // FloodImmunity, PoisonImmunity,
                // EliteKillRelicReward, EliteRewardMultiplier,
                // ReviveOnce, ConditionalMiningBonus
            }
        }

        private void ApplyLegacyFields(RelicData relic)
        {
            if (relic.attackBonus != 0)
                StatManager.Instance.BossAttackModifier += relic.attackBonus;
            if (relic.miningPowerBonus != 0)
                StatManager.Instance.RelicMiningModifier += relic.miningPowerBonus;
            if (relic.maxHPBonus != 0)
            {
                StatManager.Instance.BossMaxHPModifier += relic.maxHPBonus;
                if (relic.maxHPBonus > 0) StatManager.Instance.Heal(relic.maxHPBonus);
            }
            if (relic.resourceMultiplierBonus != 0)
                StatManager.Instance.BossResourceModifier += relic.resourceMultiplierBonus;
            if (relic.monsterAttackBonus != 0)
                StatManager.Instance.RelicMonsterAttackBonus += relic.monsterAttackBonus;
            if (relic.pickaxeDurabilityModifier != 0)
                PickaxeDurabilityManager.Instance?.ApplyRelicDurabilityModifier(relic.pickaxeDurabilityModifier);
        }

        // ── Passive Queries ───────────────────────────────────────────────────

        private float SumEffectValues(RelicEffectType type)
        {
            float total = 0f;
            foreach (var r in _activeRelics) total += r.GetEffectValue(type);
            return total;
        }

        // ── 기존 ──────────────────────────────────────────────────────────────
        public int GetBurnDurationModifier()   => Mathf.RoundToInt(SumEffectValues(RelicEffectType.BurnDurationModifier));
        public int GetBurnDamageModifier()     => Mathf.RoundToInt(SumEffectValues(RelicEffectType.BurnDamageModifier));
        public int GetMonsterAttackBonus()     => Mathf.RoundToInt(SumEffectValues(RelicEffectType.MonsterAttackBonus));
        public float GetRelicMonsterSpawnRateBonus() => SumEffectValues(RelicEffectType.MonsterSpawnRateBonus);

        public bool CheckBurnImmunity()
        {
            foreach (var r in _activeRelics)
            {
                float chance = r.GetEffectValue(RelicEffectType.BurnImmunityChance);
                if (chance > 0f && Random.value < chance) return true;
            }
            return false;
        }

        // ── 광물 타입별 획득 보너스 ───────────────────────────────────────────
        public float GetIronGainBonus()    => SumEffectValues(RelicEffectType.IronGainBonus);
        public float GetSilverGainBonus()  => SumEffectValues(RelicEffectType.SilverGainBonus);
        public float GetGoldGainBonus()    => SumEffectValues(RelicEffectType.GoldGainBonus);
        public float GetDiamondGainBonus() => SumEffectValues(RelicEffectType.DiamondGainBonus);

        public float GetOreTypeGainBonus(BlockType type)
        {
            switch (type)
            {
                case BlockType.Iron:    return GetIronGainBonus();
                case BlockType.Silver:  return GetSilverGainBonus();
                case BlockType.Gold:    return GetGoldGainBonus();
                case BlockType.Diamond: return GetDiamondGainBonus();
                default: return 0f;
            }
        }

        // ── 전투·보스 후 회복 ─────────────────────────────────────────────────
        public int GetPostCombatHealBonus() => Mathf.RoundToInt(SumEffectValues(RelicEffectType.PostCombatHealBonus));
        public int GetPostBossHealBonus()   => Mathf.RoundToInt(SumEffectValues(RelicEffectType.PostBossHealBonus));
        public bool HasBossKillFullHeal()   => SumEffectValues(RelicEffectType.BossKillFullHeal) > 0f;

        // ── 곡괭이 ───────────────────────────────────────────────────────────
        public bool HasPickaxeNoDurabilityLoss() => SumEffectValues(RelicEffectType.PickaxeNoDurabilityLoss) > 0f;
        public int GetPickaxeRepairOnKill()      => Mathf.RoundToInt(SumEffectValues(RelicEffectType.PickaxeRepairOnKill));

        public float GetPickaxeDurabilityRateModifier()
        {
            float mult = 1.0f;
            foreach (var r in _activeRelics)
            {
                float v = r.GetEffectValue(RelicEffectType.PickaxeDurabilityRateModifier);
                if (v > 0f) mult *= v; // 0.8 * 0.9 = 복수 유물 곱산
            }
            return mult;
        }

        // ── 피해 보너스 ───────────────────────────────────────────────────────
        public int GetTrapDamageReduction()    => Mathf.RoundToInt(SumEffectValues(RelicEffectType.TrapDamageReduction));
        public float GetEliteDamageBonus()     => SumEffectValues(RelicEffectType.EliteDamageBonus);
        public float GetDamageMultiplierBonus() => SumEffectValues(RelicEffectType.DamageMultiplierBonus);

        // ── 채굴/처치 시 확률 ────────────────────────────────────────────────
        public bool CheckLuckyMineChance()
        {
            float total = SumEffectValues(RelicEffectType.LuckyMineChance);
            return total > 0f && Random.value < total;
        }

        public bool CheckMineHealChance()
        {
            float total = SumEffectValues(RelicEffectType.MineHealChance);
            return total > 0f && Random.value < total;
        }

        public bool CheckKillIronChance()
        {
            float total = SumEffectValues(RelicEffectType.KillIronChance);
            return total > 0f && Random.value < total;
        }

        // ── 상태이상 면역 ────────────────────────────────────────────────────
        public bool HasFloodImmunity()  => SumEffectValues(RelicEffectType.FloodImmunity)  > 0f;
        public bool HasPoisonImmunity() => SumEffectValues(RelicEffectType.PoisonImmunity) > 0f;

        // ── 엘리트 ──────────────────────────────────────────────────────────
        public bool HasEliteKillRelicReward()   => SumEffectValues(RelicEffectType.EliteKillRelicReward)  > 0f;
        public float GetEliteRewardMultiplier() => Mathf.Max(1f, SumEffectValues(RelicEffectType.EliteRewardMultiplier));

        // ── 조건부 채굴 ─────────────────────────────────────────────────────
        public int GetConditionalMiningBonus()
        {
            float total = SumEffectValues(RelicEffectType.ConditionalMiningBonus);
            if (total == 0f) return 0;
            var dm = PickaxeDurabilityManager.Instance;
            if (dm == null || dm.MaxDurability == 0) return 0;
            float ratio = (float)dm.CurrentDurability / dm.MaxDurability;
            return ratio >= 0.5f ? Mathf.RoundToInt(total) : 0;
        }

        // ── 부활 (런 중 1회) ─────────────────────────────────────────────────
        public bool CheckAndConsumeRevive()
        {
            if (_reviveConsumed) return false;
            float fraction = SumEffectValues(RelicEffectType.ReviveOnce);
            if (fraction <= 0f) return false;
            _reviveConsumed = true;
            int reviveHP = Mathf.Max(1, Mathf.RoundToInt(StatManager.Instance.GetMaxHP() * fraction));
            StatManager.Instance.Heal(reviveHP);
            EffectSystem.Instance?.FlashScreen(new Color(0.2f, 1f, 0.4f, 0.4f), 0.5f);
            EffectSystem.Instance?.ShakeCamera(0.3f, 0.12f);
            EffectSystem.Instance?.SpawnDamageText(
                Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f : Vector3.up,
                $"부활! HP {Mathf.RoundToInt(fraction * 100)}%", Color.green);
            Debug.Log($"[Relic]\nReviveOnce Used\nHP : {reviveHP}");
            return true;
        }

        public IReadOnlyList<RelicData> GetActiveRelics() => _activeRelics;

        // ── Fallback Rewards ─────────────────────────────────────────────────

        // Called when all relics are acquired and a relic reward would be given.
        public void GiveFallbackReward()
        {
            if (_rewardConfig == null || _rewardConfig.fallbackRewards.Count == 0)
            {
                InventoryManager.Instance?.AddItem("Item_Gold", 3);
                return;
            }
            foreach (var fb in _rewardConfig.fallbackRewards)
                InventoryManager.Instance?.AddItem(fb.itemID, fb.amount);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Relic]\nDuplicate Removed\nFallback reward given.");
#endif
        }

        // ── Burn Logging (for StatusEffectManager) ──────────────────────────

        public void LogBurnContributions()
        {
            foreach (var r in _activeRelics)
            {
                int dur = Mathf.RoundToInt(r.GetEffectValue(RelicEffectType.BurnDurationModifier));
                int dmg = Mathf.RoundToInt(r.GetEffectValue(RelicEffectType.BurnDamageModifier));
                if (dur == 0 && dmg == 0) continue;

                string log = $"[Burn]\nRelic Applied\n{r.relicID}";
                if (dur != 0) log += $"\nDuration {(dur > 0 ? "+" : "")}{dur}";
                if (dmg != 0) log += $"\nDamage {(dmg > 0 ? "+" : "")}{dmg}";
                Debug.Log(log);
            }
        }

        // ── Clear ────────────────────────────────────────────────────────────

        public void ClearAll()
        {
            foreach (var r in _activeRelics)
                EffectManager.Instance?.RemoveEffect(r.relicID);

            _activeRelics.Clear();
            _acquiredIDs.Clear();
            _reviveConsumed = false;
            ClearActiveRelicSave();
        }

        // ── Save / Restore ───────────────────────────────────────────────────

        private void SaveActiveRelicIDs()
        {
            if (_restoring) return;
            var save = SaveManager.CurrentData;
            if (save == null) return;
            save.ActiveRelicIDs = _acquiredIDs.ToList();
            SaveManager.Save();
        }

        private void ClearActiveRelicSave()
        {
            var save = SaveManager.CurrentData;
            if (save == null) return;
            save.ActiveRelicIDs.Clear();
            SaveManager.Save();
        }

        public void RestoreRelicsFromSave(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return;
            _restoring = true;
            foreach (var id in ids)
            {
                var relic = _allRelics.Find(r => r.relicID == id);
                if (relic != null) AddRelic(relic);
            }
            _restoring = false;
            SaveActiveRelicIDs();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Relic]\nRestored from save\nCount : {ids.Count}");
#endif
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static EffectSystemType RarityToEffectSystemType(RelicRarity rarity) => rarity switch
        {
            RelicRarity.Legendary => EffectSystemType.RelicLegendary,
            RelicRarity.Rare      => EffectSystemType.RelicRare,
            _                     => EffectSystemType.RelicCommon
        };

        private static string RarityLocKey(RelicRarity rarity) => rarity switch
        {
            RelicRarity.Legendary => "relic_rarity_legendary",
            RelicRarity.Rare      => "relic_rarity_rare",
            _                     => "relic_rarity_common"
        };

        private static string BuildDisplayString(RelicData relic)
        {
            if (relic.effects != null && relic.effects.Count > 0)
                return BuildEffectListString(relic);
            return BuildLegacyString(relic);
        }

        private static string BuildEffectListString(RelicData relic)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var e in relic.effects)
            {
                string sign = e.value >= 0 ? "+" : "";
                string part = e.effectType switch
                {
                    // 기존
                    RelicEffectType.AttackBonus =>
                        $"공격력 {sign}{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.MiningPowerBonus =>
                        $"채굴력 {sign}{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.MaxHPBonus =>
                        $"HP {sign}{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.ResourceMultiplierBonus =>
                        $"광물 {sign}{e.value * 100:0}%",
                    RelicEffectType.BurnDurationModifier =>
                        $"화상 지속 {sign}{Mathf.RoundToInt(e.value)}턴",
                    RelicEffectType.BurnDamageModifier =>
                        $"화상 피해 {sign}{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.BurnImmunityChance =>
                        $"화상 면역 {e.value * 100:0}%",
                    RelicEffectType.MonsterAttackBonus =>
                        $"몬스터 공격 {sign}{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.MonsterSpawnRateBonus =>
                        $"몬스터 조우 {sign}{e.value * 100:0}%",
                    RelicEffectType.PickaxeDurabilityModifier =>
                        $"곡괭이 내구도 {sign}{Mathf.RoundToInt(e.value)}",

                    // 신규 — 광물 타입별
                    RelicEffectType.IronGainBonus    => $"철 획득 +{e.value * 100:0}%",
                    RelicEffectType.SilverGainBonus  => $"은 획득 +{e.value * 100:0}%",
                    RelicEffectType.GoldGainBonus    => $"금 획득 +{e.value * 100:0}%",
                    RelicEffectType.DiamondGainBonus => $"다이아 획득 +{e.value * 100:0}%",

                    // 신규 — 인벤토리
                    RelicEffectType.InventorySlotBonus => $"인벤토리 +{Mathf.RoundToInt(e.value)}칸",

                    // 신규 — 회복
                    RelicEffectType.PostCombatHealBonus   => $"전투 후 HP +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.PostBossHealBonus     => $"보스 후 HP +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.BossKillFullHeal      => "보스 처치 시 HP 전부 회복",
                    RelicEffectType.HealingMultiplierModifier =>
                        $"회복량 {(e.value < 1f ? "-" : "+")}{Mathf.Abs(Mathf.RoundToInt((1f - e.value) * 100))}%",

                    // 신규 — 곡괭이
                    RelicEffectType.PickaxeMaxDurabilityBonus     => $"최대 내구도 +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.PickaxeDurabilityRateModifier =>
                        $"내구도 소모 -{Mathf.RoundToInt((1f - e.value) * 100)}%",
                    RelicEffectType.PickaxeNoDurabilityLoss => "내구도 감소 없음",
                    RelicEffectType.PickaxeRepairOnKill     => $"처치 시 내구도 +{Mathf.RoundToInt(e.value)}",

                    // 신규 — 전투
                    RelicEffectType.TrapDamageReduction   => $"함정 피해 -{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.EliteDamageBonus      => $"엘리트 피해 +{e.value * 100:0}%",
                    RelicEffectType.DamageMultiplierBonus => $"몬스터 피해 +{e.value * 100:0}%",

                    // 신규 — 확률
                    RelicEffectType.LuckyMineChance  => $"채굴 시 {e.value * 100:0}% 추가 획득",
                    RelicEffectType.MineHealChance   => $"채굴 시 {e.value * 100:0}% HP +1",
                    RelicEffectType.KillIronChance   => $"처치 시 {e.value * 100:0}% 철 +1",

                    // 신규 — 면역
                    RelicEffectType.FloodImmunity  => "수몰 면역",
                    RelicEffectType.PoisonImmunity => "중독 면역",

                    // 신규 — 엘리트
                    RelicEffectType.EliteKillRelicReward  => "엘리트 처치 시 유물 추가 획득",
                    RelicEffectType.EliteRewardMultiplier => $"엘리트 보상 {Mathf.RoundToInt(e.value)}배",

                    // 신규 — 부활
                    RelicEffectType.ReviveOnce => $"1회 부활 (HP {e.value * 100:0}%)",

                    // 신규 — 조건부 채굴
                    RelicEffectType.ConditionalMiningBonus =>
                        $"내구도 50% 이상 시 채굴력 +{Mathf.RoundToInt(e.value)}",

                    _ => ""
                };
                if (!string.IsNullOrEmpty(part)) sb.Append(part).Append("  ");
            }
            return sb.ToString().TrimEnd();
        }

        private static string BuildLegacyString(RelicData relic)
        {
            var sb = new System.Text.StringBuilder();
            if (relic.attackBonus != 0)
                sb.Append($"공격력 {(relic.attackBonus > 0 ? "+" : "")}{relic.attackBonus}  ");
            if (relic.miningPowerBonus != 0)
                sb.Append($"채굴력 {(relic.miningPowerBonus > 0 ? "+" : "")}{relic.miningPowerBonus}  ");
            if (relic.maxHPBonus != 0)
                sb.Append($"HP {(relic.maxHPBonus > 0 ? "+" : "")}{relic.maxHPBonus}  ");
            if (relic.burnDurationModifier != 0)
                sb.Append($"화상 지속 {(relic.burnDurationModifier > 0 ? "+" : "")}{relic.burnDurationModifier}턴  ");
            if (relic.burnDamageModifier != 0)
                sb.Append($"화상 피해 {(relic.burnDamageModifier > 0 ? "+" : "")}{relic.burnDamageModifier}  ");
            if (relic.resourceMultiplierBonus != 0)
                sb.Append($"광물 {(relic.resourceMultiplierBonus > 0 ? "+" : "")}{relic.resourceMultiplierBonus * 100:0}%  ");
            if (relic.monsterAttackBonus != 0)
                sb.Append($"몬스터 공격 {(relic.monsterAttackBonus > 0 ? "+" : "")}{relic.monsterAttackBonus}  ");
            if (relic.monsterSpawnRateBonus != 0)
                sb.Append($"몬스터 조우 {(relic.monsterSpawnRateBonus > 0 ? "+" : "")}{relic.monsterSpawnRateBonus * 100:0}%  ");
            if (relic.pickaxeDurabilityModifier != 0)
                sb.Append($"곡괭이 내구도 {(relic.pickaxeDurabilityModifier > 0 ? "+" : "")}{relic.pickaxeDurabilityModifier}  ");
            if (relic.burnImmunityChance > 0f)
                sb.Append($"화상 무효 {relic.burnImmunityChance * 100:0}%  ");
            return sb.ToString().TrimEnd();
        }
    }
}
