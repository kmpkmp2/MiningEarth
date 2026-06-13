using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.UI;

namespace DeepEarth.Combat
{
    public class BossRewardPresenter
    {
        private readonly BossRewardView _view;
        private readonly Action _onCompleted;
        private List<BossRewardChoice> _activeChoices;

        public BossRewardPresenter(BossRewardView view, Action onCompleted)
        {
            _view = view;
            _onCompleted = onCompleted;

            GenerateAndPopulateRewards();

            if (_view != null)
            {
                _view.OnRewardSelected += HandleRewardSelected;
                _view.SetVisible(true);
            }
            else
            {
                Debug.LogWarning("BossRewardPresenter: BossRewardView is null! Auto-selecting first reward to prevent locking the game.");
                if (_activeChoices != null && _activeChoices.Count > 0)
                {
                    ApplyReward(_activeChoices[0].Type);
                }
                _onCompleted?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnRewardSelected -= HandleRewardSelected;
            }
        }

        private void GenerateAndPopulateRewards()
        {
            _activeChoices = new List<BossRewardChoice>();
            var chosenTypes = new HashSet<BossRewardType>();

            // Pick 3 unique rewards
            while (_activeChoices.Count < 3)
            {
                // 15% chance to roll a Rare reward
                bool rollRare = UnityEngine.Random.value < 0.15f;
                BossRewardType rolledType;

                if (rollRare)
                {
                    var rareOptions = new[]
                    {
                        BossRewardType.Revive,
                        BossRewardType.BossDamage,
                        BossRewardType.Mineral50,
                        BossRewardType.DoubleEvent
                    };
                    rolledType = rareOptions[UnityEngine.Random.Range(0, rareOptions.Length)];
                }
                else
                {
                    var normalOptions = new[]
                    {
                        BossRewardType.Attack,
                        BossRewardType.MaxHP,
                        BossRewardType.Mining,
                        BossRewardType.Mineral20,
                        BossRewardType.SpawnDecrease,
                        BossRewardType.HealDrop
                    };
                    rolledType = normalOptions[UnityEngine.Random.Range(0, normalOptions.Length)];
                }

                if (!chosenTypes.Contains(rolledType))
                {
                    chosenTypes.Add(rolledType);
                    _activeChoices.Add(new BossRewardChoice(rolledType));
                }
            }

            if (_view != null)
            {
                _view.PopulateRewards(_activeChoices);
            }
        }

        private void HandleRewardSelected(int index)
        {
            if (index < 0 || index >= _activeChoices.Count) return;

            var chosen = _activeChoices[index];
            ApplyReward(chosen.Type);

            _view.SetVisible(false);
            Dispose();

            _onCompleted?.Invoke();
        }

        private void ApplyReward(BossRewardType type)
        {
            var stats = StatManager.Instance;
            switch (type)
            {
                case BossRewardType.Attack:
                    stats.BossAttackModifier += 2;
                    break;
                case BossRewardType.MaxHP:
                    stats.BossMaxHPModifier += 5;
                    stats.Heal(5);
                    break;
                case BossRewardType.Mining:
                    stats.BossMiningModifier += 2;
                    break;
                case BossRewardType.Mineral20:
                    stats.BossResourceModifier += 0.20f;
                    break;
                case BossRewardType.SpawnDecrease:
                    stats.BossMonsterSpawnMultiplier *= 0.80f; // -20%
                    break;
                case BossRewardType.HealDrop:
                    stats.BossHealDropChanceModifier += 0.15f;
                    break;
                case BossRewardType.Revive:
                    stats.BossReviveCount += 1;
                    break;
                case BossRewardType.BossDamage:
                    stats.BossDamageToBossMultiplier = 1.50f; // +50% Boss Damage
                    break;
                case BossRewardType.Mineral50:
                    stats.BossResourceModifier += 0.50f;
                    break;
                case BossRewardType.DoubleEvent:
                    stats.BossRareEventDouble = true;
                    break;
            }

            RegisterBossRewardToManager(type);
            stats.TriggerStatsUpdated();
        }

        private void RegisterBossRewardToManager(BossRewardType type)
        {
            if (EffectManager.Instance == null) return;

            string id = "BossReward_" + type.ToString();
            string nameKey = "";
            string descKey = "";
            float value = 0;
            string display = "";
            string iconKey = "";
            string source = "Boss Defeat";

            switch (type)
            {
                case BossRewardType.Attack:
                    nameKey = "effect_boss_attack_name";
                    descKey = "effect_boss_attack_desc";
                    value = 2;
                    display = "⚔2";
                    iconKey = "Effect_BossReward_AttackPower";
                    break;
                case BossRewardType.MaxHP:
                    nameKey = "effect_boss_maxhp_name";
                    descKey = "effect_boss_maxhp_desc";
                    value = 5;
                    display = "❤5";
                    iconKey = "Effect_BossReward_MaxHP";
                    break;
                case BossRewardType.Mining:
                    nameKey = "effect_boss_mining_name";
                    descKey = "effect_boss_mining_desc";
                    value = 2;
                    display = "⚒2";
                    iconKey = "Effect_BossReward_MiningPower";
                    break;
                case BossRewardType.Mineral20:
                    nameKey = "effect_boss_mineral20_name";
                    descKey = "effect_boss_mineral20_desc";
                    value = 20;
                    display = "💎20%";
                    iconKey = "Effect_BossReward_Mineral";
                    break;
                case BossRewardType.SpawnDecrease:
                    nameKey = "effect_boss_spawndecrease_name";
                    descKey = "effect_boss_spawndecrease_desc";
                    value = -20;
                    display = "-20%";
                    iconKey = "Effect_BossReward_SpawnDecrease";
                    break;
                case BossRewardType.HealDrop:
                    nameKey = "effect_boss_healdrop_name";
                    descKey = "effect_boss_healdrop_desc";
                    value = 15;
                    display = "+15%";
                    iconKey = "Effect_BossReward_HealDrop";
                    break;
                case BossRewardType.Revive:
                    nameKey = "effect_boss_revive_name";
                    descKey = "effect_boss_revive_desc";
                    value = 1;
                    display = "1";
                    iconKey = "Effect_BossReward_Revive";
                    break;
                case BossRewardType.BossDamage:
                    nameKey = "effect_boss_bossdamage_name";
                    descKey = "effect_boss_bossdamage_desc";
                    value = 50;
                    display = "+50%";
                    iconKey = "Effect_BossReward_BossDamage";
                    break;
                case BossRewardType.Mineral50:
                    nameKey = "effect_boss_mineral50_name";
                    descKey = "effect_boss_mineral50_desc";
                    value = 50;
                    display = "💎50%";
                    iconKey = "Effect_BossReward_Mineral50";
                    break;
                case BossRewardType.DoubleEvent:
                    nameKey = "effect_boss_doubleevent_name";
                    descKey = "effect_boss_doubleevent_desc";
                    value = 100;
                    display = "2x";
                    iconKey = "Effect_BossReward_DoubleEvent";
                    break;
            }

            EffectManager.Instance.RegisterEffect(id, nameKey, descKey, EffectSystemType.BossReward, value, display, source, iconKey);
        }
    }
}
