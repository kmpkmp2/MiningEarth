using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.Event
{
    public class EventManager : MonoBehaviour
    {
        private static EventManager _instance;
        public static EventManager Instance => _instance;

        public event Action<EventData> OnEventTriggered;
        public event Action OnEventCompleted;

        private UniTaskCompletionSource<int> _choiceTcs;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async UniTask TriggerRandomEventAsync(bool isTombstone)
        {
            _choiceTcs = new UniTaskCompletionSource<int>();

            EventData eventData;
            if (isTombstone)
            {
                eventData = GenerateTombstoneEvent();
            }
            else
            {
                eventData = GenerateTreasureEvent();
            }

            // Pause Game via GameManager
            GameManager.Instance.PauseForEvent();

            // Invoke UI event
            OnEventTriggered?.Invoke(eventData);

            // Wait for player to choose
            int selectedIndex = await _choiceTcs.Task;

            // Apply effects
            var chosenOption = eventData.Options[selectedIndex];
            foreach (var effect in chosenOption.Effects)
            {
                StatManager.Instance.AddEffect(effect);
                
                // Double event rewards (buffs only) if the player has the rare Double Blessing buff
                if (StatManager.Instance.BossRareEventDouble && effect.ToString().StartsWith("Buff"))
                {
                    StatManager.Instance.AddEffect(effect);
                }
            }

            // Invoke complete
            OnEventCompleted?.Invoke();

            // Resume Game
            GameManager.Instance.ResumeAfterEvent();
        }

        public void SelectOption(int index)
        {
            _choiceTcs?.TrySetResult(index);
        }

        private EventData GenerateTreasureEvent()
        {
            var optionsPool = new List<EventOption>
            {
                new EventOption("event_opt_mining_title", "event_opt_mining_desc", new List<EffectType> { EffectType.BuffAttackDamage }),
                new EventOption("event_opt_hp_title", "event_opt_hp_desc", new List<EffectType> { EffectType.BuffMaxHP }),
                new EventOption("event_opt_stealth_title", "event_opt_stealth_desc", new List<EffectType> { EffectType.BuffMonsterSpawnRateDecrease }),
                new EventOption("event_opt_hat_title", "event_opt_hat_desc", new List<EffectType> { EffectType.BuffHazardSpawnRateDecrease })
            };

            // Shuffle and pick 3 unique options
            ShuffleList(optionsPool);
            var selectedOptions = new List<EventOption> { optionsPool[0], optionsPool[1], optionsPool[2] };

            return new EventData(
                "event_chest_title",
                "event_chest_desc",
                false,
                selectedOptions
            );
        }

        private EventData GenerateTombstoneEvent()
        {
            var optionsPool = new List<EventOption>
            {
                new EventOption(
                    "event_opt_demonic_title",
                    "event_opt_demonic_desc",
                    new List<EffectType> { EffectType.BuffAttackDamage, EffectType.BuffAttackDamage, EffectType.CurseMaxHP }
                ),
                new EventOption(
                    "event_opt_fortitude_title",
                    "event_opt_fortitude_desc",
                    new List<EffectType> { EffectType.BuffMaxHP, EffectType.BuffMaxHP, EffectType.CurseHazardSpawnRateIncrease }
                ),
                new EventOption(
                    "event_opt_reckless_title",
                    "event_opt_reckless_desc",
                    new List<EffectType> { EffectType.BuffAttackDamage, EffectType.BuffAttackDamage, EffectType.CurseMiningFailChance }
                ),
                new EventOption(
                    "event_opt_vampiric_title",
                    "event_opt_vampiric_desc",
                    new List<EffectType> { EffectType.BuffMonsterSpawnRateDecrease, EffectType.CurseInstantDamageOnEncounter }
                )
            };

            ShuffleList(optionsPool);
            var selectedOptions = new List<EventOption> { optionsPool[0], optionsPool[1] };

            return new EventData(
                "event_tomb_title",
                "event_tomb_desc",
                true,
                selectedOptions
            );
        }

        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
