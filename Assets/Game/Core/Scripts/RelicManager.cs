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
                case RelicRewardContext.Treasure:
                    // 4단계 개편(2026-08-07): Unique는 Rare급 판매 대상에 포함, Legendary만 제외.
                    return _allRelics.Where(r =>
                        !_acquiredIDs.Contains(r.relicID) &&
                        r.rarity <= RelicRarity.Unique).ToList();

                default:
                    return _allRelics.Where(r => !_acquiredIDs.Contains(r.relicID)).ToList();
            }
        }

        // Backward-compat wrappers used by EventManager (Treasure/Tombstone events)
        public List<RelicData> GetAvailableTreasureRelics() =>
            _allRelics.Where(r =>
                !_acquiredIDs.Contains(r.relicID) &&
                r.rarity <= RelicRarity.Unique).ToList();

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
            var (c, r, u, l) = _rewardConfig != null ? _rewardConfig.GetChances(ctx) : (0.45f, 0.28f, 0.20f, 0.07f);

            // 그룹 J — 행운의 반지(전역) + 신비한 열쇠(Treasure 한정). Rare 확률을 가산(기존 동작 유지).
            float rareBonus = 0f;
            foreach (var relic in _activeRelics)
                foreach (var e in relic.effects)
                {
                    if (e.effectType == RelicEffectType.RareChanceBonus) rareBonus += e.value;
                    if (e.effectType == RelicEffectType.TreasureRareChanceBonus && ctx == RelicRewardContext.Treasure)
                        rareBonus += e.value;
                }
            if (rareBonus > 0f)
            {
                r += rareBonus;
                c = Mathf.Max(0f, c - rareBonus);
                float total = c + r + u + l;
                if (total > 0) { c /= total; r /= total; u /= total; l /= total; }
            }

            float roll = Random.value;
            if (roll < l)         return RelicRarity.Legendary;
            if (roll < l + u)     return RelicRarity.Unique;
            if (roll < l + u + r) return RelicRarity.Rare;
            return RelicRarity.Common;
        }

        // 그룹 J(B-2) — 보물상자 가중 셔플에서 사용할 (Common, Rare, Unique) 가중치. Legendary는 보물상자 풀에 없어 제외.
        public (float commonWeight, float rareWeight, float uniqueWeight) GetTreasureRarityWeights()
        {
            var (c, r, u, _) = _rewardConfig != null ? _rewardConfig.GetChances(RelicRewardContext.Treasure) : (0.45f, 0.35f, 0.20f, 0f);

            float keyBonus = 0f;
            foreach (var relic in _activeRelics)
                foreach (var e in relic.effects)
                    if (e.effectType == RelicEffectType.TreasureRareChanceBonus)
                        keyBonus += e.value;

            r += keyBonus;
            c = Mathf.Max(0f, c - keyBonus);
            return (c, r, u);
        }

        // ── Acquire ──────────────────────────────────────────────────────────

        private const string CollectorsBagID = "relic_collectors_bag";

        public void AddRelic(RelicData relic)
        {
            if (relic == null || _acquiredIDs.Contains(relic.relicID)) return;

            // 그룹 L — 수집가의 가방은 런타임 전용 복제본을 사용(에셋 원본 오염 방지)
            RelicData instanceToUse = relic;
            if (relic.relicID == CollectorsBagID)
            {
                instanceToUse = ScriptableObject.Instantiate(relic);
                instanceToUse.effects = new List<RelicEffectData>(relic.effects);
            }

            _activeRelics.Add(instanceToUse);
            _acquiredIDs.Add(relic.relicID);

            // Apply effect list (new system)
            if (instanceToUse.effects != null && instanceToUse.effects.Count > 0)
            {
                foreach (var effect in instanceToUse.effects)
                    ApplyEffect(effect);
            }
            else
            {
                // Legacy fallback for pre-migration assets
                ApplyLegacyFields(instanceToUse);
            }

            EffectSystemType displayType = RarityToEffectSystemType(relic.rarity);
            string rarityLabel = RarityLocKey(relic.rarity);

            EffectManager.Instance?.RegisterEffect(
                relic.relicID,
                relic.nameLocKey,
                relic.descLocKey,
                displayType,
                0f,
                BuildDisplayString(instanceToUse),
                rarityLabel,
                relic.iconKey
            );

            SaveActiveRelicIDs();

            StatManager.Instance.TriggerStatsUpdated();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Relic]\nObtained\nName : {relic.relicID}\nRarity : {relic.rarity}");
#endif
            GameEvents.FireRelicCollected();

            if (relic.relicID == CollectorsBagID)
                PromptRelicCopyAsync(instanceToUse).Forget();
        }

        // 그룹 L — 유물 효과 복사 대상 선택(플레이어 직접 선택, 취소 불가)
        private async UniTaskVoid PromptRelicCopyAsync(RelicData collectorInstance)
        {
            var candidates = _activeRelics
                .Where(r => r.relicID != CollectorsBagID && r != collectorInstance)
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.Log("[Relic]\nCollector's Bag\nNo Other Relics Owned — Skipped");
                return;
            }

            var presenter = GameManager.Instance?.RelicCopyPopupPresenter;
            if (presenter == null)
            {
                Debug.LogWarning("[Relic]\nCollector's Bag: RelicCopyPopupPresenter not available — Skipped");
                return;
            }

            var selected = await presenter.SelectRelicAsync(candidates);
            if (selected != null) CopyRelicEffects(collectorInstance, selected);
        }

        // 그룹 L — 대상 유물의 effects를 복제본에 추가 적용. ApplyEffect 디스패치를 그대로 재사용.
        private void CopyRelicEffects(RelicData targetInstance, RelicData sourceRelic)
        {
            foreach (var effect in sourceRelic.effects)
            {
                targetInstance.effects.Add(effect);
                ApplyEffect(effect);
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Relic]\nCollector's Bag Copied\nSource : {sourceRelic.relicID}\nEffects : {sourceRelic.effects.Count}");
#endif
        }

        private void ApplyEffect(RelicEffectData effect)
        {
            // 트리거형(그룹 A/C/D/E/I)·스케일링형(그룹 F/G/K) 효과는 획득 즉시 적용하지 않고
            // 해당 트리거/조회 시점에 별도로 처리한다(아래 47종 지원 섹션 참고).
            if (effect.triggerEvent != RelicTriggerEvent.None || effect.scalingSource != RelicScalingSource.None) return;

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
                    else StatManager.Instance.ClampCurrentHPToMax();
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
                else StatManager.Instance.ClampCurrentHPToMax();
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
        // 그룹 G: conditionHpRatioMax 조건부(피의 계약/붉은 심장) 지원 — 기본값 1f는 무조건부(기존 드래곤 송곳니 등)와 동일하게 항상 통과.
        public float GetDamageMultiplierBonus()
        {
            int maxHp = StatManager.Instance.GetMaxHP();
            float hpRatio = maxHp > 0 ? (float)StatManager.Instance.CurrentHP / maxHp : 1f;
            float total = 0f;
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.effectType == RelicEffectType.DamageMultiplierBonus && hpRatio <= e.conditionHpRatioMax)
                        total += e.value;
            return total;
        }

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

        // ── 그룹 M: 도굴꾼 장갑(TreasureHunter 패시브와 동일 개념) ───────────────
        public bool HasTreasureRewardBonusRelic() => SumEffectValues(RelicEffectType.TreasureRewardBonus) > 0f;

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

        // ══════════════════════════════════════════════════════════════════
        // 47종 신규 유물 지원 (그룹 A~N, 제안서 기준)
        // ══════════════════════════════════════════════════════════════════

        // ── 공통 헬퍼 ────────────────────────────────────────────────────────
        private float SumTriggeredEffectValues(RelicEffectType type, RelicTriggerEvent evt)
        {
            float total = 0f;
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.effectType == type && e.triggerEvent == evt) total += e.value;
            return total;
        }

        private bool HasTriggeredEffect(RelicEffectType type, RelicTriggerEvent evt)
        {
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.effectType == type && e.triggerEvent == evt) return true;
            return false;
        }

        private void ApplyMatchingTriggerEffects(RelicTriggerEvent evt)
        {
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.triggerEvent == evt) ApplyTriggeredEffect(r, e);
        }

        private void ApplyMatchingTriggerEffects(RelicTriggerEvent evt, DeepEarth.Map.RoomType nodeType)
        {
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.triggerEvent == evt && e.triggerNodeType == nodeType) ApplyTriggeredEffect(r, e);
        }

        // 트리거 발동 시 실제 적용 로직(그룹 A/C/I 공용) — HealBonus/NodeItemGrant/DebuffClearAll/
        // 스택형 AttackBonus버프/반복형 MaxHPBonus·MiningPowerBonus(오래된 지도 등)를 처리한다.
        private void ApplyTriggeredEffect(RelicData owner, RelicEffectData e)
        {
            int intVal = Mathf.RoundToInt(e.value);
            switch (e.effectType)
            {
                case RelicEffectType.HealBonus:
                    if (intVal > 0) StatManager.Instance.Heal(intVal);
                    break;
                case RelicEffectType.NodeItemGrant:
                    if (e.targetItems != null && e.targetItems.Count > 0)
                    {
                        var chosen = e.targetItems[Random.Range(0, e.targetItems.Count)];
                        if (chosen != null) InventoryManager.Instance?.AddItem(chosen.itemID, Mathf.Max(1, intVal));
                    }
                    break;
                case RelicEffectType.DebuffClearAll:
                case RelicEffectType.CombatEndDebuffClear:
                    StatusEffectManager.Instance?.ClearAllDebuffs();
                    break;
                case RelicEffectType.AttackBonus:
                    if (e.buffDurationTurns > 0)
                        StatusEffectManager.Instance?.ApplyPlayerAttackBuff(
                            "RelicBuff_" + owner.relicID, e.buffDurationTurns, e.value, Mathf.Max(1, e.buffMaxStacks));
                    else
                        StatManager.Instance.BossAttackModifier += intVal;  // 영구 가산(탐험가의 일지/보물 감지기 등, 즉시적용형 AttackBonus와 동일 처리)
                    break;
                case RelicEffectType.MaxHPBonus:
                    if (intVal > 0) { StatManager.Instance.BossMaxHPModifier += intVal; StatManager.Instance.Heal(intVal); }
                    break;
                case RelicEffectType.MiningPowerBonus:
                    StatManager.Instance.RelicMiningModifier += intVal;
                    break;
            }
        }

        // ── 그룹 A: 노드 도착/완료 트리거 ────────────────────────────────────
        public void ApplyNodeArrivalEffects(DeepEarth.Map.RoomType type)   => ApplyMatchingTriggerEffects(RelicTriggerEvent.NodeArrival, type);
        public void ApplyNodeCompletionEffects(DeepEarth.Map.RoomType type) => ApplyMatchingTriggerEffects(RelicTriggerEvent.NodeCompletion, type);

        // ── 그룹 B: 맵 생성 가중치 ────────────────────────────────────────────
        public float GetNodeWeightBonus(DeepEarth.Map.RoomType type)
        {
            float total = 0f;
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.effectType == RelicEffectType.NodeWeightBonus && e.triggerNodeType == type)
                        total += e.value;
            return total;
        }

        // ── 그룹 C: 아이템/포션 사용 트리거 ───────────────────────────────────
        public void ApplyItemUseEffects()   => ApplyMatchingTriggerEffects(RelicTriggerEvent.ItemUse);
        public void ApplyPotionUseEffects() => ApplyMatchingTriggerEffects(RelicTriggerEvent.PotionUse);

        public float GetLowHpHealMultiplier()
        {
            int maxHp = StatManager.Instance.GetMaxHP();
            float hpRatio = maxHp > 0 ? (float)StatManager.Instance.CurrentHP / maxHp : 1f;
            float mult = 1f;
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.effectType == RelicEffectType.LowHpHealMultiplier && hpRatio <= e.conditionHpRatioMax)
                        mult *= e.value;
            return mult;
        }

        // ── 그룹 D: 전투 내 동적 스케일링/트리거 ──────────────────────────────
        public int GetCombatTurnAttackBonus() =>
            Mathf.RoundToInt(SumEffectValues(RelicEffectType.CombatTurnAttackBonus)) * StatManager.Instance.CombatTurnCount;

        public int GetCombatHitTakenAttackBonus() =>
            Mathf.RoundToInt(SumEffectValues(RelicEffectType.CombatHitTakenAttackBonus)) * StatManager.Instance.CombatHitsTaken;

        public int GetPerMonsterAttackBonus(int monsterCount) =>
            Mathf.RoundToInt(SumEffectValues(RelicEffectType.PerMonsterAttackBonus)) * Mathf.Max(0, monsterCount);

        // 1이면 아직 미사용(전투 도끼) — 호출측이 StatManager.MarkFirstAttackDone()으로 소비 처리해야 함
        public float GetFirstAttackDamageMultiplier()
        {
            if (StatManager.Instance.CombatFirstAttackDone) return 0f;
            return SumTriggeredEffectValues(RelicEffectType.FirstAttackDamageBonus, RelicTriggerEvent.CombatFirstAttack);
        }

        // 강철 갑옷 — 첫 턴(CombatTurnCount==0)에만 0이 아닌 값 반환
        public int GetFirstTurnShieldBonus()
        {
            if (StatManager.Instance.CombatTurnCount != 0) return 0;
            return Mathf.RoundToInt(SumTriggeredEffectValues(RelicEffectType.CombatFirstTurnShieldBonus, RelicTriggerEvent.CombatFirstTurnOnly));
        }

        // 성기사의 망토 — 매 턴 호출
        public int GetEveryTurnShieldBonus() =>
            Mathf.RoundToInt(SumTriggeredEffectValues(RelicEffectType.EveryTurnShieldBonus, RelicTriggerEvent.CombatTurnStart));

        // ── 그룹 E: 처치/수리 트리거 ──────────────────────────────────────────
        private static readonly HashSet<MonsterType> EliteTypes = new HashSet<MonsterType> {
            MonsterType.BigSlime, MonsterType.SkeletonMiner, MonsterType.IronPlateSpider,
            MonsterType.MerchantMimic, MonsterType.CursedKnight, MonsterType.CursedPriest
        };
        private static readonly HashSet<MonsterType> BossTypes = new HashSet<MonsterType> {
            MonsterType.StoneGolemBoss, MonsterType.MotherCaveSpiderBoss, MonsterType.SkeletonWarlordBoss,
            MonsterType.AllMetalColossusBoss, MonsterType.CaveRatBoss, MonsterType.BossCore
        };
        public static bool IsEliteType(MonsterType type) => EliteTypes.Contains(type);
        public static bool IsBossType(MonsterType type)  => BossTypes.Contains(type);

        public void ApplyMonsterKilledEffects(MonsterType type)
        {
            float killHeal = SumTriggeredEffectValues(RelicEffectType.OnKillHealBonus, RelicTriggerEvent.MonsterKilled);
            if (killHeal > 0f) StatManager.Instance.Heal(Mathf.RoundToInt(killHeal));

            if (IsEliteType(type))
            {
                if (HasTriggeredEffect(RelicEffectType.EliteKillPotionDropBonus, RelicTriggerEvent.EliteKilled))
                    InventoryManager.Instance?.AddItem(AddressableKeys.ItemPotion, 1);

                if (HasTriggeredEffect(RelicEffectType.PickaxeFullRestoreOnKill, RelicTriggerEvent.EliteKilled))
                    FullRestorePickaxe();
            }

            if (IsBossType(type) && HasTriggeredEffect(RelicEffectType.PickaxeFullRestoreOnKill, RelicTriggerEvent.BossKilled))
                FullRestorePickaxe();
        }

        private void FullRestorePickaxe()
        {
            var dm = PickaxeDurabilityManager.Instance;
            if (dm != null) dm.RepairOnKill(dm.MaxDurability);
        }

        public void ApplyPlayerDealtDamageEffects()
        {
            float heal = SumTriggeredEffectValues(RelicEffectType.OnHitHealBonus, RelicTriggerEvent.PlayerDealtDamage);
            if (heal > 0f) StatManager.Instance.Heal(Mathf.RoundToInt(heal));
        }

        public void ApplyPickaxeRepairedEffects()
        {
            float heal = SumTriggeredEffectValues(RelicEffectType.OnRepairHealBonus, RelicTriggerEvent.PickaxeRepaired);
            if (heal > 0f) StatManager.Instance.Heal(Mathf.RoundToInt(heal));

            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.effectType == RelicEffectType.OnRepairMaxDurabilityChance && e.triggerEvent == RelicTriggerEvent.PickaxeRepaired)
                        if (Random.value < e.value)
                            PickaxeDurabilityManager.Instance?.AddMaxDurabilityBonus(1);
        }

        public float GetRepairEfficiencyBonus() => SumEffectValues(RelicEffectType.RepairEfficiencyBonus);

        // ── 그룹 F/K: 실시간 파생 스탯 계산 ────────────────────────────────────
        public int GetScalingBonus(RelicEffectType type)
        {
            float total = 0f;
            foreach (var r in _activeRelics)
                foreach (var e in r.effects)
                    if (e.effectType == type && e.scalingSource != RelicScalingSource.None)
                        total += Mathf.FloorToInt(GetScalingSourceValue(e.scalingSource) / Mathf.Max(0.0001f, e.scalingDivisor)) * e.value;
            return Mathf.RoundToInt(total);
        }

        private float GetScalingSourceValue(RelicScalingSource source)
        {
            switch (source)
            {
                case RelicScalingSource.InventoryOreCount:
                    if (InventoryManager.Instance == null) return 0f;
                    return InventoryManager.Instance.GetItemCount(AddressableKeys.ItemIron)
                         + InventoryManager.Instance.GetItemCount(AddressableKeys.ItemSilver)
                         + InventoryManager.Instance.GetItemCount(AddressableKeys.ItemGold)
                         + InventoryManager.Instance.GetItemCount(AddressableKeys.ItemDiamond);
                case RelicScalingSource.MiningPower:
                    return StatManager.Instance.GetMiningPower();
                case RelicScalingSource.PickaxeDurability:
                    return PickaxeDurabilityManager.Instance?.CurrentDurability ?? 0;
                case RelicScalingSource.Depth:
                    return GameManager.Instance != null ? GameManager.Instance.CurrentDepth : 0;
                case RelicScalingSource.HpLostPercent:
                {
                    int maxHp = StatManager.Instance.GetMaxHP();
                    return maxHp > 0 ? (1f - (float)StatManager.Instance.CurrentHP / maxHp) * 100f : 0f;
                }
                case RelicScalingSource.CombatStatusDamage:
                    return StatManager.Instance.CombatStatusDamageAccumulated;
                default:
                    return 0f;
            }
        }

        // ── 그룹 H: 상점 가격 ────────────────────────────────────────────────
        public float GetPotionPriceReduction() => SumEffectValues(RelicEffectType.PotionPriceReduction);
        public float GetShopDiscountBonus()    => SumEffectValues(RelicEffectType.ShopDiscountBonus);

        // ── 그룹 I: 상태이상 일반화 ───────────────────────────────────────────
        public int GetPoisonDurationModifier()        => Mathf.RoundToInt(SumEffectValues(RelicEffectType.PoisonDurationModifier));
        public float GetStatusDamagePercentModifier() => SumEffectValues(RelicEffectType.StatusDamagePercentModifier);
        public void ApplyCombatEndEffects()           => ApplyMatchingTriggerEffects(RelicTriggerEvent.CombatEnd);

        // ── 그룹 N: 최후의 일격 ───────────────────────────────────────────────
        public float GetFinishingBlowMultiplier() => SumEffectValues(RelicEffectType.FinishingBlowMultiplier);

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
            RelicRarity.Unique    => EffectSystemType.RelicUnique,
            RelicRarity.Rare      => EffectSystemType.RelicRare,
            _                     => EffectSystemType.RelicCommon
        };

        private static string RarityLocKey(RelicRarity rarity) => rarity switch
        {
            RelicRarity.Legendary => "relic_rarity_legendary",
            RelicRarity.Unique    => "relic_rarity_unique",
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

                    // 신규 — 47종 지원(그룹 A~N)
                    RelicEffectType.HealBonus              => $"HP {sign}{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.NodeItemGrant           => "아이템 지급",
                    RelicEffectType.NodeWeightBonus         => $"노드 출현 확률 +{e.value * 100:0}%",
                    RelicEffectType.DebuffClearAll          => "디버프 제거",
                    RelicEffectType.LowHpHealMultiplier     => $"저체력 시 포션 효과 {Mathf.RoundToInt(e.value)}배",
                    RelicEffectType.FirstAttackDamageBonus  => $"첫 공격 피해 +{e.value * 100:0}%",
                    RelicEffectType.CombatTurnAttackBonus   => $"매 턴 공격력 +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.CombatFirstTurnShieldBonus => $"전투 첫 턴 방어도 +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.PerMonsterAttackBonus   => $"몬스터 1마리당 공격력 +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.CombatHitTakenAttackBonus => $"피격 시 공격력 +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.EveryTurnShieldBonus    => $"매 턴 방어도 +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.OnKillHealBonus         => $"처치 시 HP +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.OnHitHealBonus          => $"적중 시 HP +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.EliteKillPotionDropBonus => "엘리트 처치 시 포션 추가 획득",
                    RelicEffectType.PickaxeFullRestoreOnKill => "처치 시 내구도 완전 회복",
                    RelicEffectType.OnRepairHealBonus       => $"수리 시 HP +{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.OnRepairMaxDurabilityChance => $"수리 시 {e.value * 100:0}% 확률 최대 내구도 +1",
                    RelicEffectType.RepairEfficiencyBonus   => $"수리 효율 +{e.value * 100:0}%",
                    RelicEffectType.PotionPriceReduction    => $"포션 가격 -{Mathf.RoundToInt(e.value)}",
                    RelicEffectType.ShopDiscountBonus       => $"상점 가격 -{e.value * 100:0}%",
                    RelicEffectType.PoisonDurationModifier  => $"중독 지속 {sign}{Mathf.RoundToInt(e.value)}턴",
                    RelicEffectType.StatusDamagePercentModifier => $"상태이상 피해 -{e.value * 100:0}%",
                    RelicEffectType.CombatEndDebuffClear    => "전투 종료 시 디버프 제거",
                    RelicEffectType.RareChanceBonus         => $"유물 Rare 이상 확률 +{e.value * 100:0}%",
                    RelicEffectType.TreasureRareChanceBonus => $"보물상자 고급 유물 확률 +{e.value * 100:0}%",
                    RelicEffectType.FinishingBlowMultiplier => $"필살 조건 충족 시 피해 {Mathf.RoundToInt(e.value)}배(전투당 1회)",
                    RelicEffectType.TreasureRewardBonus     => "보물상자 보상 선택지 +1",

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
