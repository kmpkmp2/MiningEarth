using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class StatusEffectManager : MonoBehaviour
    {
        private static StatusEffectManager _instance;
        public static StatusEffectManager Instance => _instance;

        private readonly List<StatusEffectModel> _activeEffects = new List<StatusEffectModel>();
        private StatusEffectData _burnData;
        private StatusEffectData _miningPowerDownData;
        private StatusEffectData _miningPowerUpData;
        private const string     PoisonEffectID = "Poison";

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

        public async UniTask InitializeAsync()
        {
            _burnData = await ResourceManager.Instance.LoadAssetAsync<StatusEffectData>(AddressableKeys.StatusEffectBurn);
            if (_burnData == null)
            {
                Debug.LogWarning("StatusEffectManager: StatusEffect_Burn not found. Using defaults.");
                _burnData = ScriptableObject.CreateInstance<StatusEffectData>();
            }

            _miningPowerDownData = await ResourceManager.Instance.LoadAssetAsync<StatusEffectData>(AddressableKeys.StatusEffectMiningDown);
            if (_miningPowerDownData == null)
            {
                Debug.LogWarning("StatusEffectManager: StatusEffect_MiningPowerDown not found. Using defaults.");
                _miningPowerDownData = CreateDefaultMiningData(StatusEffectID.MiningPowerDown, "MiningPowerDown", -0.15f, 10);
            }

            _miningPowerUpData = await ResourceManager.Instance.LoadAssetAsync<StatusEffectData>(AddressableKeys.StatusEffectMiningUp);
            if (_miningPowerUpData == null)
            {
                Debug.LogWarning("StatusEffectManager: StatusEffect_MiningPowerUp not found. Using defaults.");
                _miningPowerUpData = CreateDefaultMiningData(StatusEffectID.MiningPowerUp, "MiningPowerUp", 0.20f, 10);
            }
        }

        // ── Burn ────────────────────────────────────────────────────────

        public void ApplyBurn()
        {
            EnsureBurnData();

            if (RelicManager.Instance != null && RelicManager.Instance.CheckBurnImmunity())
            {
                Debug.Log("[Burn]\nImmunity Triggered\nBurn Blocked");
                return;
            }

            var existing = _activeEffects.Find(e => e.EffectID == _burnData.effectID);
            if (existing != null)
            {
                _activeEffects.Remove(existing);
                EffectManager.Instance?.RemoveEffect(_burnData.effectID);
            }

            int baseDuration = _burnData.baseDuration;
            int baseDamage   = _burnData.damagePerTurn;
            int durationMod  = RelicManager.Instance?.GetBurnDurationModifier() ?? 0;
            int damageMod    = RelicManager.Instance?.GetBurnDamageModifier() ?? 0;
            int finalDuration = Mathf.Max(1, Mathf.RoundToInt((baseDuration + durationMod) * GetPriestDurationMultiplier()));
            int finalDamage   = Mathf.Max(0, baseDamage + damageMod);

            Debug.Log($"[Burn]\nBase Duration : {baseDuration}\nBase Damage : {baseDamage}");
            RelicManager.Instance?.LogBurnContributions();
            Debug.Log($"[Burn]\nFinal Duration : {finalDuration}\nFinal Damage : {finalDamage}");

            var model = new StatusEffectModel(_burnData, finalDuration, finalDamage);
            _activeEffects.Add(model);
            RegisterInEffectManager(model);

            Debug.Log($"[Status]\nBurn Applied\nDuration : {model.RemainingTurns}\nDamage : {model.DamagePerTurn}");
        }

        public bool HasBurn()
        {
            EnsureBurnData();
            return _activeEffects.Exists(e => e.EffectID == _burnData.effectID);
        }

        public bool CureBurn()
        {
            EnsureBurnData();
            var existing = _activeEffects.Find(e => e.EffectID == _burnData.effectID);
            if (existing == null) return false;
            _activeEffects.Remove(existing);
            EffectManager.Instance?.RemoveEffect(_burnData.effectID);
            Debug.Log("[Status]\nBurn Cured");
            return true;
        }

        // ── Poison ──────────────────────────────────────────────────────────

        public void ApplyPoison(int turns = 6)
        {
            // 유물: 독 해독제 — 중독 면역
            if (RelicManager.Instance?.HasPoisonImmunity() ?? false)
            {
                Debug.Log("[Status]\nPoison Blocked by Relic Immunity");
                return;
            }

            int durationMod = RelicManager.Instance?.GetPoisonDurationModifier() ?? 0;
            int addedTurns  = Mathf.Max(1, Mathf.RoundToInt((turns + durationMod) * GetPriestDurationMultiplier()));

            // 그룹 I: 재적용 시 교체 대신 남은 턴수에 가산 (데미지는 매 틱마다 "그 시점의 남은 턴수")
            var existing = _activeEffects.Find(e => e.EffectID == PoisonEffectID);
            if (existing != null)
            {
                existing.ExtendDuration(addedTurns);
                EffectManager.Instance?.UpdateEffectDisplay(existing.EffectID, BuildDisplayString(existing), existing.RemainingTurns);
                Debug.Log($"[Status]\nPoison Extended\nAdded Turns : {addedTurns}\nTotal Remaining : {existing.RemainingTurns}");
                return;
            }

            var data  = CreatePoisonData();
            var model = new StatusEffectModel(data, addedTurns, 0, damageScalesWithRemainingTurns: true);
            _activeEffects.Add(model);
            RegisterInEffectManager(model);
            Debug.Log($"[Status]\nPoison Applied\nDuration : {addedTurns}\nAttack Mod : -10%");
        }

        public bool HasPoison() => _activeEffects.Exists(e => e.EffectID == PoisonEffectID);

        public bool CurePoison()
        {
            var existing = _activeEffects.Find(e => e.EffectID == PoisonEffectID);
            if (existing == null) return false;
            _activeEffects.Remove(existing);
            EffectManager.Instance?.RemoveEffect(PoisonEffectID);
            Debug.Log("[Status]\nPoison Cured");
            return true;
        }

        public float GetTotalAttackModifier()
        {
            float total = 0f;
            foreach (var effect in _activeEffects)
                if (effect.Data.effectType == StatusEffectID.Poison || effect.Data.effectType == StatusEffectID.PlayerAttackBuff)
                    total += effect.AttackModifier;
            return total;
        }

        // ── Player Attack Buff (그룹 C) ────────────────────────────────────
        // 화상/독과 달리 동일 effectID라도 교체하지 않고 maxStacks까지 별도 인스턴스로 중첩된다.
        public void ApplyPlayerAttackBuff(string effectID, int duration, float attackBonus, int maxStacks)
        {
            int currentStacks = _activeEffects.FindAll(e => e.EffectID == effectID).Count;
            if (currentStacks >= maxStacks)
            {
                Debug.Log($"[Status]\n{effectID} Stack Limit Reached\nMax Stacks : {maxStacks}");
                return;
            }

            var data = ScriptableObject.CreateInstance<StatusEffectData>();
            data.effectType        = StatusEffectID.PlayerAttackBuff;
            data.effectID          = effectID;
            data.nameLocKey        = "status_player_attack_buff_name";
            data.descLocKey        = "status_player_attack_buff_desc";
            data.damagePerTurn     = 0;
            data.attackModifier    = attackBonus;
            data.miningPowerModifier = 0f;
            data.baseDuration      = duration;
            data.systemType        = EffectSystemType.StatusEffect;
            data.iconKey           = "Effect_Buff_Attack";
            data.source            = "Relic";

            var model = new StatusEffectModel(data, duration, 0);
            _activeEffects.Add(model);
            RegisterInEffectManager(model);
            Debug.Log($"[Status]\n{effectID} Applied\nStack : {currentStacks + 1}/{maxStacks}\nDuration : {duration}\nAttack Mod : +{attackBonus * 100}%");
        }

        // 그룹 C: 소비 즉시 디버프 해제 — 화상/독만 대상(채굴력/공격력 하락 상태이상은 미포함, 사용자 확정 사항)
        public void ClearAllDebuffs()
        {
            CureBurn();
            CurePoison();
        }

        private StatusEffectData CreatePoisonData()
        {
            var d = ScriptableObject.CreateInstance<StatusEffectData>();
            d.effectType        = StatusEffectID.Poison;
            d.effectID          = PoisonEffectID;
            d.nameLocKey        = "status_poison_name";
            d.descLocKey        = "status_poison_desc";
            d.damagePerTurn     = 0;
            d.attackModifier    = -0.10f;
            d.miningPowerModifier = 0f;
            d.baseDuration      = 6;
            d.systemType        = EffectSystemType.StatusEffect;
            d.iconKey           = "Effect_Debuff_Poison";
            d.source            = "Event";
            return d;
        }

        // ── Mining Power Effects ─────────────────────────────────────────

        public void ApplyMiningPowerDown(int durationOverride = -1)
        {
            if (_miningPowerDownData == null) return;

            var existing = _activeEffects.Find(e => e.EffectID == _miningPowerDownData.effectID);
            if (existing != null)
            {
                _activeEffects.Remove(existing);
                EffectManager.Instance?.RemoveEffect(_miningPowerDownData.effectID);
            }

            int baseDuration = durationOverride > 0 ? durationOverride : _miningPowerDownData.baseDuration;
            int duration = Mathf.Max(1, Mathf.RoundToInt(baseDuration * GetPriestDurationMultiplier()));
            var model = new StatusEffectModel(_miningPowerDownData, duration, 0);
            _activeEffects.Add(model);
            RegisterInEffectManager(model);

            Debug.Log($"[Status]\nMiningPowerDown Applied\nDuration : {duration}\nModifier : {_miningPowerDownData.miningPowerModifier}");
        }

        // 신규 패시브: Priest — "저주형"(지속시간 기반) 디버프 지속시간 감소 + 시작 유물(축복의 성수) 추가 감소
        // 코드베이스에 duration 기반 "Curse" 카테고리가 별도로 없어 Burn/Poison/MiningPowerDown 전부에 넓게 적용한다.
        private float GetPriestDurationMultiplier()
        {
            var charID = CharacterManager.Instance.SelectedCharacterID;
            float reduction = CharacterManager.Instance.GetPassiveCurseDurationReduction(charID)
                             + (StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetCurseDurationReduction() : 0f);
            return Mathf.Max(0f, 1f - Mathf.Clamp01(reduction));
        }

        public void ApplyMiningPowerUp(int durationOverride = -1)
        {
            if (_miningPowerUpData == null) return;

            var existing = _activeEffects.Find(e => e.EffectID == _miningPowerUpData.effectID);
            if (existing != null)
            {
                _activeEffects.Remove(existing);
                EffectManager.Instance?.RemoveEffect(_miningPowerUpData.effectID);
            }

            int duration = durationOverride > 0 ? durationOverride : _miningPowerUpData.baseDuration;
            var model = new StatusEffectModel(_miningPowerUpData, duration, 0);
            _activeEffects.Add(model);
            RegisterInEffectManager(model);

            Debug.Log($"[Status]\nMiningPowerUp Applied\nDuration : {duration}\nModifier : {_miningPowerUpData.miningPowerModifier}");
        }

        public float GetTotalMiningModifier()
        {
            float total = 0f;
            foreach (var effect in _activeEffects)
            {
                if (effect.Data.effectType == StatusEffectID.MiningPowerDown ||
                    effect.Data.effectType == StatusEffectID.MiningPowerUp)
                    total += effect.MiningPowerModifier;
            }
            return total;
        }

        // ── Action Turn ──────────────────────────────────────────────────

        public void ProcessActionTurn()
        {
            if (_activeEffects.Count == 0) return;

            var toRemove = new List<StatusEffectModel>();

            foreach (var effect in _activeEffects)
            {
                int dmg = effect.Tick();

                if (dmg > 0 && (effect.Data.effectType == StatusEffectID.Burn || effect.Data.effectType == StatusEffectID.Poison))
                {
                    float reducePercent = RelicManager.Instance?.GetStatusDamagePercentModifier() ?? 0f;
                    dmg = Mathf.Max(0, Mathf.RoundToInt(dmg * (1f - Mathf.Clamp01(reducePercent))));
                }

                if (dmg > 0)
                {
                    StatManager.Instance.TakeDamage(dmg);
                    StatManager.Instance.AddCombatStatusDamage(dmg);

                    if (StatManager.Instance.CurrentHP > 0)
                    {
                        bool isPoison = effect.Data.effectType == StatusEffectID.Poison;
                        Color flashColor = isPoison ? new Color(0.5f, 0.9f, 0.2f, 0.25f) : new Color(1f, 0.5f, 0f, 0.25f);
                        EffectSystem.Instance.FlashScreen(flashColor, 0.2f);
                        EffectSystem.Instance.ShakeCamera(0.12f, 0.05f);

                        Vector3 pos = Camera.main != null
                            ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Camera.main.transform.right * 0.5f
                            : Vector3.up;
                        string label = isPoison ? $"-{dmg} 중독" : $"-{dmg} 화상";
                        Color textColor = isPoison ? new Color(0.5f, 0.9f, 0.2f) : new Color(1f, 0.4f, 0f);
                        EffectSystem.Instance.SpawnDamageText(pos, label, textColor);
                    }

                    Debug.Log($"[Status]\n{effect.Data.effectID} Tick\nRemaining Turn : {effect.RemainingTurns}\nDamage : {dmg}\nCurrent HP : {StatManager.Instance.CurrentHP}");
                }

                if (effect.IsExpired)
                    toRemove.Add(effect);
                else
                    EffectManager.Instance?.UpdateEffectDisplay(effect.EffectID, BuildDisplayString(effect), effect.RemainingTurns);
            }

            foreach (var effect in toRemove)
            {
                _activeEffects.Remove(effect);
                EffectManager.Instance?.RemoveEffect(effect.EffectID);
                Debug.Log($"[Status]\n{effect.Data.effectID} End");
            }
        }

        public void ClearAll()
        {
            foreach (var effect in _activeEffects)
                EffectManager.Instance?.RemoveEffect(effect.EffectID);
            _activeEffects.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void RegisterInEffectManager(StatusEffectModel model)
        {
            string display  = BuildDisplayString(model);
            string src      = string.IsNullOrEmpty(model.Data.source) ? "Status Effect" : model.Data.source;

            EffectManager.Instance?.RegisterEffect(
                model.EffectID,
                model.Data.nameLocKey,
                model.Data.descLocKey,
                model.Data.systemType,
                model.RemainingTurns,
                display,
                src,
                model.Data.iconKey
            );
        }

        private string BuildDisplayString(StatusEffectModel model)
        {
            if (model.DamagePerTurn > 0)
                return $"{model.RemainingTurns}턴 (-{model.DamagePerTurn})";
            if (model.AttackModifier < 0)
                return $"{model.RemainingTurns}턴 (공격{(int)(model.AttackModifier * 100)}%)";
            if (model.AttackModifier > 0)
                return $"{model.RemainingTurns}턴 (공격+{(int)(model.AttackModifier * 100)}%)";
            if (model.MiningPowerModifier < 0)
                return $"{model.RemainingTurns}턴 ({(int)(model.MiningPowerModifier * 100)}%)";
            if (model.MiningPowerModifier > 0)
                return $"{model.RemainingTurns}턴 (+{(int)(model.MiningPowerModifier * 100)}%)";
            return $"{model.RemainingTurns}턴";
        }

        private void EnsureBurnData()
        {
            if (_burnData == null)
                _burnData = ScriptableObject.CreateInstance<StatusEffectData>();
        }

        private StatusEffectData CreateDefaultMiningData(StatusEffectID id, string effectID, float modifier, int duration)
        {
            var data = ScriptableObject.CreateInstance<StatusEffectData>();
            data.effectType          = id;
            data.effectID            = effectID;
            data.nameLocKey          = id == StatusEffectID.MiningPowerDown ? "status_mining_down_name" : "status_mining_up_name";
            data.descLocKey          = id == StatusEffectID.MiningPowerDown ? "status_mining_down_desc" : "status_mining_up_desc";
            data.damagePerTurn       = 0;
            data.miningPowerModifier = modifier;
            data.baseDuration        = duration;
            data.systemType          = EffectSystemType.StatusEffect;
            data.source              = id == StatusEffectID.MiningPowerDown ? "Skeleton" : "Mimic";
            return data;
        }
    }
}
