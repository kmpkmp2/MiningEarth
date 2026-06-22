using System;
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
                Debug.LogWarning("StatusEffectManager: StatusEffect_Burn not found in Addressables. Using defaults.");
                _burnData = ScriptableObject.CreateInstance<StatusEffectData>();
            }
        }

        public void ApplyBurn()
        {
            EnsureBurnData();

            // PurificationRing 등 면역 유물 체크
            if (RelicManager.Instance != null && RelicManager.Instance.CheckBurnImmunity())
            {
                Debug.Log("[Burn]\nImmunity Triggered\nBurn Blocked");
                return;
            }

            // Remove existing burn — no stacking from the same source
            var existing = _activeEffects.Find(e => e.EffectID == _burnData.effectID);
            if (existing != null)
            {
                _activeEffects.Remove(existing);
                EffectManager.Instance?.RemoveEffect(_burnData.effectID);
            }

            // 유물 수치 계산
            int baseDuration = _burnData.baseDuration;
            int baseDamage = _burnData.damagePerTurn;
            int durationMod = RelicManager.Instance?.GetBurnDurationModifier() ?? 0;
            int damageMod = RelicManager.Instance?.GetBurnDamageModifier() ?? 0;
            int finalDuration = Mathf.Max(1, baseDuration + durationMod);
            int finalDamage = Mathf.Max(0, baseDamage + damageMod);

            Debug.Log($"[Burn]\nBase Duration : {baseDuration}\nBase Damage : {baseDamage}");
            RelicManager.Instance?.LogBurnContributions();
            Debug.Log($"[Burn]\nFinal Duration : {finalDuration}\nFinal Damage : {finalDamage}");

            var model = new StatusEffectModel(_burnData, finalDuration, finalDamage);
            _activeEffects.Add(model);

            RegisterInEffectManager(model);

            Debug.Log($"[Status]\nBurn Applied\nDuration : {model.RemainingTurns}\nDamage : {model.DamagePerTurn}");
        }

        // Call once per action turn (block destroyed / monster killed / event chosen).
        public void ProcessActionTurn()
        {
            if (_activeEffects.Count == 0) return;

            var toRemove = new List<StatusEffectModel>();

            foreach (var effect in _activeEffects)
            {
                int dmg = effect.Tick();

                if (dmg > 0)
                {
                    StatManager.Instance.TakeDamage(dmg);

                    if (StatManager.Instance.CurrentHP > 0)
                    {
                        EffectSystem.Instance.FlashScreen(new Color(1f, 0.5f, 0f, 0.25f), 0.2f);
                        EffectSystem.Instance.ShakeCamera(0.12f, 0.05f);

                        Vector3 pos = Camera.main != null
                            ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Camera.main.transform.right * 0.5f
                            : Vector3.up;
                        EffectSystem.Instance.SpawnDamageText(pos, $"-{dmg} 화상", new Color(1f, 0.4f, 0f));
                    }

                    Debug.Log($"[Status]\nBurn Tick\nRemaining Turn : {effect.RemainingTurns}\nDamage : {dmg}\nCurrent HP : {StatManager.Instance.CurrentHP}");
                }

                if (effect.IsExpired)
                    toRemove.Add(effect);
                else
                {
                    string tickDisplay = effect.DamagePerTurn > 0
                        ? $"{effect.RemainingTurns}턴 (-{effect.DamagePerTurn})"
                        : $"{effect.RemainingTurns}턴 (무피해)";
                    EffectManager.Instance?.UpdateEffectDisplay(effect.EffectID, tickDisplay, effect.RemainingTurns);
                }
            }

            foreach (var effect in toRemove)
            {
                _activeEffects.Remove(effect);
                EffectManager.Instance?.RemoveEffect(effect.EffectID);
                Debug.Log("[Status]\nBurn End");
            }
        }

        public void ClearAll()
        {
            foreach (var effect in _activeEffects)
                EffectManager.Instance?.RemoveEffect(effect.EffectID);

            _activeEffects.Clear();
        }

        private void RegisterInEffectManager(StatusEffectModel model)
        {
            string display = model.DamagePerTurn > 0
                ? $"{model.RemainingTurns}턴 (-{model.DamagePerTurn})"
                : $"{model.RemainingTurns}턴 (무피해)";

            EffectManager.Instance?.RegisterEffect(
                model.EffectID,
                model.Data.nameLocKey,
                model.Data.descLocKey,
                EffectSystemType.StatusEffect,
                model.RemainingTurns,
                display,
                "Lava Event",
                model.Data.iconKey
            );
        }

        private void EnsureBurnData()
        {
            if (_burnData == null)
                _burnData = ScriptableObject.CreateInstance<StatusEffectData>();
        }
    }
}
