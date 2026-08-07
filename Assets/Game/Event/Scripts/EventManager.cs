using System;
using System.Collections.Generic;
using System.Linq;
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

            if (!string.IsNullOrEmpty(chosenOption.ItemRewardKey))
            {
                InventoryManager.Instance?.AddItem(chosenOption.ItemRewardKey, 1);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Event]\nItem Reward\nKey : {chosenOption.ItemRewardKey}");
#endif
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

        /// Shows an event popup for NodeEventManager-driven events.
        /// Returns the chosen option index. Effects are applied by the caller.
        public async UniTask<int> ShowEventChoiceAsync(EventData data)
        {
            _choiceTcs = new UniTaskCompletionSource<int>();

            GameManager.Instance.PauseForEvent();
            OnEventTriggered?.Invoke(data);

            int choice = await _choiceTcs.Task;

            StatusEffectManager.Instance?.ProcessActionTurn();
            OnEventCompleted?.Invoke();

            if (StatManager.Instance.CurrentHP > 0)
                GameManager.Instance.SetGameState(GameState.Playing);

            return choice;
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
                EventRevealType.Treasure      => "reveal_treasure",
                EventRevealType.Tombstone     => "reveal_tombstone",
                EventRevealType.MonsterRat    => "reveal_monster_rat",
                EventRevealType.MonsterSpider => "reveal_monster_spider",
                EventRevealType.MonsterSlime  => "reveal_monster_slime",
                EventRevealType.Water         => "reveal_water",
                EventRevealType.Lava          => "reveal_lava",
                EventRevealType.Boss          => "reveal_boss",
                _                             => "reveal_unknown"
            };

            string translated = lm != null ? lm.GetTranslation(key) : key;

            if (string.IsNullOrEmpty(translated) || translated == key)
            {
                return type switch
                {
                    EventRevealType.Treasure      => "보물상자 발견!",
                    EventRevealType.Tombstone     => "수상한 무덤 발견!",
                    EventRevealType.MonsterRat    => "동굴쥐 발견!",
                    EventRevealType.MonsterSpider => "동굴거미 무리 발견!",
                    EventRevealType.MonsterSlime  => "슬라임 발견!",
                    EventRevealType.Water         => "지하수 발견!",
                    EventRevealType.Lava          => "용암 지대 발견!",
                    EventRevealType.Boss          => "보스 출현!",
                    _                             => "???",
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
                new EventOption("item_burn_cure_name", "item_burn_cure_desc", AddressableKeys.ItemBurnCure)
            };

            // 신규 패시브: TreasureHunter — 유물 등장 확률 증가 + 시작 유물(행운의 동전) 추가 가산
            var charID = CharacterManager.Instance.SelectedCharacterID;
            bool bonusSlot = CharacterManager.Instance.HasTreasureRewardBonus(charID)
                            || (RelicManager.Instance?.HasTreasureRewardBonusRelic() ?? false); // 그룹 M: 도굴꾼 장갑
            float relicBonusChance = CharacterManager.Instance.GetTreasureRelicChanceBonus(charID)
                                    + (StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetTreasureRewardBonus() : 0f);

            if (RelicManager.Instance != null)
            {
                var treasureRelics = RelicManager.Instance.GetAvailableTreasureRelics();
                foreach (var relic in treasureRelics)
                    optionsPool.Add(new EventOption(relic));

                if (relicBonusChance > 0f && treasureRelics.Count > 0 && UnityEngine.Random.value < relicBonusChance)
                    optionsPool.Add(new EventOption(treasureRelics[UnityEngine.Random.Range(0, treasureRelics.Count)]));
            }

            WeightedShuffleTreasureOptions(optionsPool);
            // 신규 패시브: TreasureHunter — 보상 개수 +1 (3→4)
            int count = Mathf.Min(bonusSlot ? 4 : 3, optionsPool.Count);
            var selectedOptions = new List<EventOption>();
            var usedRelicIDs = new HashSet<string>();
            foreach (var opt in optionsPool)
            {
                if (selectedOptions.Count >= count) break;
                if (opt.RelicReward != null)
                {
                    if (usedRelicIDs.Contains(opt.RelicReward.relicID)) continue;
                    usedRelicIDs.Add(opt.RelicReward.relicID);
                }
                selectedOptions.Add(opt);
            }

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
                    new List<EffectType> { EffectType.BuffMaxHP, EffectType.BuffMaxHP, EffectType.CurseInstantDamageOnEncounter }
                ),
                new EventOption(
                    "event_opt_reckless_title",
                    "event_opt_reckless_desc",
                    new List<EffectType> { EffectType.BuffAttackDamage, EffectType.BuffAttackDamage, EffectType.CurseMiningFailChance }
                ),
                new EventOption(
                    "event_opt_vampiric_title",
                    "event_opt_vampiric_desc",
                    new List<EffectType> { EffectType.BuffAttackDamage, EffectType.BuffAttackDamage, EffectType.CurseInstantDamageOnEncounter }
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

        // 그룹 J(B-2): Efraimidis-Spirakis 가중 셔플 — 유물 등급별 가중치로 보물상자 옵션 순서를 편향시키되
        // 나머지 비-유물 옵션(채굴 버프/HP 버프/화상 치료)과의 상대적 다양성은 유지한다.
        private void WeightedShuffleTreasureOptions(List<EventOption> list)
        {
            var (commonWeight, rareWeight, uniqueWeight) = RelicManager.Instance?.GetTreasureRarityWeights() ?? (1f, 1f, 1f);

            float GetWeight(EventOption opt)
            {
                if (opt.RelicReward == null) return 1f;
                return opt.RelicReward.rarity switch
                {
                    RelicRarity.Unique => Mathf.Max(0.0001f, uniqueWeight),
                    RelicRarity.Rare   => Mathf.Max(0.0001f, rareWeight),
                    _                  => Mathf.Max(0.0001f, commonWeight),
                };
            }

            var keyed = list.Select(opt => (opt, key: Mathf.Pow(UnityEngine.Random.value, 1f / GetWeight(opt)))).ToList();
            keyed.Sort((a, b) => b.key.CompareTo(a.key));

            list.Clear();
            foreach (var entry in keyed) list.Add(entry.opt);
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
