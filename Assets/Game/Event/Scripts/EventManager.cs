using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.UI;

namespace DeepEarth.Event
{
    public class EventManager : MonoBehaviour
    {
        private static EventManager _instance;
        public static EventManager Instance => _instance;

        public event Action<EventData> OnEventTriggered;
        public event Action OnEventCompleted;

        private UniTaskCompletionSource<int> _choiceTcs;
        private EventRevealPresenter _revealPresenter;
        private bool _isRevealPlaying;

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

        public void SetRevealPresenter(EventRevealPresenter presenter)
        {
            _revealPresenter = presenter;
        }

        public async UniTask PlayRevealAsync(EventRevealType type)
        {
            if (_isRevealPlaying) return;
            _isRevealPlaying = true;

            Debug.Log($"[Event]\nGenerated : {type}");
            Debug.Log("[Event]\nReveal Start");

            string displayName = GetRevealDisplayName(type);
            if (_revealPresenter != null)
            {
                await _revealPresenter.ShowAsync(displayName);
            }

            EffectSystem.Instance.ShakeCamera(0.1f, 0.05f);
            await UniTask.Delay(500);

            if (_revealPresenter != null)
            {
                await _revealPresenter.HideAsync();
            }

            Debug.Log("[Event]\nReveal Complete");
            _isRevealPlaying = false;
        }

        public async UniTask TriggerRandomEventAsync(bool isTombstone)
        {
            EventRevealType revealType = isTombstone ? EventRevealType.Tombstone : EventRevealType.Treasure;
            await PlayRevealAsync(revealType);

            _choiceTcs = new UniTaskCompletionSource<int>();

            EventData eventData = isTombstone ? GenerateTombstoneEvent() : GenerateTreasureEvent();

            GameManager.Instance.PauseForEvent();

            Debug.Log("[Event]\nOpen Event");

            OnEventTriggered?.Invoke(eventData);

            int selectedIndex = await _choiceTcs.Task;

            var chosenOption = eventData.Options[selectedIndex];
            foreach (var effect in chosenOption.Effects)
            {
                StatManager.Instance.AddEffect(effect);

                if (StatManager.Instance.BossRareEventDouble && effect.ToString().StartsWith("Buff"))
                {
                    StatManager.Instance.AddEffect(effect);
                }
            }

            if (chosenOption.RelicReward != null)
            {
                RelicManager.Instance?.AddRelic(chosenOption.RelicReward);
            }

            // Action turn: event choice completed = 1 turn
            StatusEffectManager.Instance?.ProcessActionTurn();

            OnEventCompleted?.Invoke();

            // Guard: burn tick may have killed the player — don't resume if dead
            if (StatManager.Instance.CurrentHP > 0)
            {
                GameManager.Instance.ResumeAfterEvent();
            }
        }

        public void SelectOption(int index)
        {
            _choiceTcs?.TrySetResult(index);
        }

        private static string GetRevealDisplayName(EventRevealType type)
        {
            var lm = LocalizationManager.Instance;
            string key = type switch
            {
                EventRevealType.Treasure     => "reveal_treasure",
                EventRevealType.Tombstone    => "reveal_tombstone",
                EventRevealType.MonsterRat   => "reveal_monster_rat",
                EventRevealType.MonsterSpider => "reveal_monster_spider",
                EventRevealType.Water        => "reveal_water",
                EventRevealType.Lava         => "reveal_lava",
                EventRevealType.Boss         => "reveal_boss",
                _                            => "reveal_unknown"
            };

            string translated = lm != null ? lm.GetTranslation(key) : key;

            if (string.IsNullOrEmpty(translated) || translated == key)
            {
                return type switch
                {
                    EventRevealType.Treasure      => "보물상자 발견!",
                    EventRevealType.Tombstone      => "수상한 무덤 발견!",
                    EventRevealType.MonsterRat     => "동굴쥐 발견!",
                    EventRevealType.MonsterSpider  => "동굴거미 무리 발견!",
                    EventRevealType.Water          => "지하수 발견!",
                    EventRevealType.Lava           => "용암 지대 발견!",
                    EventRevealType.Boss           => "보스 출현!",
                    _                              => "???",
                };
            }
            return translated;
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

            if (RelicManager.Instance != null)
            {
                foreach (var relic in RelicManager.Instance.GetAvailableTreasureRelics())
                    optionsPool.Add(new EventOption(relic));
            }

            ShuffleList(optionsPool);
            int count = Mathf.Min(3, optionsPool.Count);
            var selectedOptions = new List<EventOption>();
            for (int i = 0; i < count; i++) selectedOptions.Add(optionsPool[i]);

            return new EventData("event_chest_title", "event_chest_desc", false, selectedOptions);
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

            if (RelicManager.Instance != null)
            {
                foreach (var relic in RelicManager.Instance.GetAvailableTombstoneRelics())
                    optionsPool.Add(new EventOption(relic));
            }

            ShuffleList(optionsPool);
            int count = Mathf.Min(2, optionsPool.Count);
            var selectedOptions = new List<EventOption>();
            for (int i = 0; i < count; i++) selectedOptions.Add(optionsPool[i]);

            return new EventData("event_tomb_title", "event_tomb_desc", true, selectedOptions);
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
