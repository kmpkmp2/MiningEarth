using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.Event
{
    /// <summary>
    /// Orchestrates the 16 new Event Node events.
    /// Handles weighted random selection, 5-floor gap history,
    /// choice display via EventManager.ShowEventChoiceAsync, and all per-event effects.
    /// </summary>
    public class NodeEventManager : MonoBehaviour
    {
        private static NodeEventManager _instance;
        public static NodeEventManager Instance => _instance;

        private readonly Dictionary<NodeEventID, NodeEventData> _events = new();
        private readonly Dictionary<NodeEventID, int>           _lastTriggerFloor = new();
        private bool _dataLoaded;

        // Collapsed Tunnel: skip the player's next node selection
        public bool SkipNextNode { get; set; } = false;

        // Miner's Journal floor-based buff
        private int _minerJournalFloors;
        private const string MinerJournalEffectID = "BuffOreGain";

        private const int EventGapFloors = 5;

        private static readonly string[] MineralSpendOrder =
            { "Item_Diamond", "Item_Gold", "Item_Silver", "Item_Iron" };

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
        }

        // ── Init ────────────────────────────────────────────────────────────

        public async UniTask EnsureDataLoadedAsync()
        {
            if (_dataLoaded) return;
            _dataLoaded = true;

            var list = await ResourceManager.Instance.LoadAllByLabelAsync<NodeEventData>(
                AddressableKeys.LabelNodeEvent);
            if (list != null)
                foreach (var d in list)
                    if (d != null) _events[d.eventID] = d;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Event]\nNodeEvent Data Loaded\nCount : {_events.Count}");
#endif
        }

        // ── Floor Counters ──────────────────────────────────────────────────

        /// Called by GameManager after every node visit to tick floor-based buffs.
        public void DecrementFloorCounters()
        {
            if (_minerJournalFloors <= 0) return;
            _minerJournalFloors--;

            if (_minerJournalFloors == 0)
            {
                EffectManager.Instance?.RemoveEffect(MinerJournalEffectID);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Event]\nMiner's Journal\nExpired");
#endif
            }
            else
            {
                EffectManager.Instance?.UpdateEffectDisplay(
                    MinerJournalEffectID,
                    $"+20% {_minerJournalFloors}층",
                    _minerJournalFloors);
            }
        }

        public float GetOreGainMultiplier() => _minerJournalFloors > 0 ? 1.2f : 1.0f;

        // ── Public Entry Points ─────────────────────────────────────────────

        /// For RoomType.Event → pick from all 16 events.
        public async UniTask TriggerEventAsync(int depth)
        {
            await EnsureDataLoadedAsync();
            var pool = GetEligibleEvents(null, depth);
            await ExecutePickedEventAsync(pool, depth);
        }

        /// For RoomType.Rest → pick from Positive events only.
        public async UniTask TriggerRestAsync(int depth)
        {
            await EnsureDataLoadedAsync();
            var pool = GetEligibleEvents(NodeEventCategory.Positive, depth);
            if (pool.Count == 0)
                pool = _events.Values.Where(e => e.category == NodeEventCategory.Positive).ToList();
            await ExecutePickedEventAsync(pool, depth);
        }

        // ── Selection ───────────────────────────────────────────────────────

        private List<NodeEventData> GetEligibleEvents(NodeEventCategory? cat, int depth)
        {
            IEnumerable<NodeEventData> src = _events.Values;
            if (cat.HasValue) src = src.Where(e => e.category == cat.Value);

            return src.Where(e =>
                !_lastTriggerFloor.TryGetValue(e.eventID, out int last) ||
                depth - last >= EventGapFloors
            ).ToList();
        }

        private async UniTask ExecutePickedEventAsync(List<NodeEventData> pool, int depth)
        {
            if (pool.Count == 0)
            {
                // Fallback: safe healing event
                await FallbackHealAsync(depth);
                return;
            }

            var picked = WeightedPick(pool);
            _lastTriggerFloor[picked.eventID] = depth;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Event]\nTriggered\nName : {picked.eventID}\nDepth : {depth}");
#endif
            await ExecuteEventAsync(picked, depth);
        }

        private NodeEventData WeightedPick(List<NodeEventData> pool)
        {
            int total = pool.Sum(e => e.weight);
            int roll  = UnityEngine.Random.Range(0, total);
            int acc   = 0;
            foreach (var e in pool)
            {
                acc += e.weight;
                if (roll < acc) return e;
            }
            return pool[pool.Count - 1];
        }

        // ── Event Dispatch ──────────────────────────────────────────────────

        private async UniTask ExecuteEventAsync(NodeEventData data, int depth)
        {
            // Events with fully dynamic choices bypass the SO choices list
            switch (data.eventID)
            {
                case NodeEventID.CrystalShrine:
                    await ExecuteCrystalShrineAsync(depth);
                    return;
                case NodeEventID.AncientForge:
                    await ExecuteAncientForgeAsync(depth);
                    return;
                case NodeEventID.RichOreVein:
                    await ExecuteRichOreVeinAsync(data, depth);
                    return;
            }

            if (data.isAutomatic)
            {
                var autoEvent = BuildAutoEventData(data);
                await EventManager.Instance.ShowEventChoiceAsync(autoEvent);
                if (StatManager.Instance.CurrentHP <= 0) return;
                await ApplyEffectAsync(data.eventID, 0, depth);
                return;
            }

            var eventData = BuildEventDataWithBonusChoice(data, out bool bonusChoiceShown);
            int choice = await EventManager.Instance.ShowEventChoiceAsync(eventData);
            if (StatManager.Instance.CurrentHP <= 0) return;

            // 신규 패시브: Adventurer — Event Node 보너스 선택지(범용 안전 보상), 저작된 선택지 범위를 벗어나면 개별 이벤트 로직을 타지 않는다.
            if (bonusChoiceShown && choice == data.choices.Count)
            {
                ApplyAdventurerBonusChoice();
                return;
            }

            await ApplyEffectAsync(data.eventID, choice, depth);
        }

        private EventData BuildAutoEventData(NodeEventData data)
        {
            var opts = new List<EventOption>
            {
                new EventOption("event_btn_continue", "event_btn_continue_desc", new List<EffectType>())
            };
            return new EventData(data.titleLocKey, data.descLocKey, false, opts);
        }

        private EventData BuildEventData(NodeEventData data)
        {
            var opts = data.choices
                .Select(c => new EventOption(c.titleLocKey, c.descLocKey, new List<EffectType>()))
                .ToList();
            return new EventData(data.titleLocKey, data.descLocKey, false, opts);
        }

        // 신규 패시브: Adventurer — 저작된 선택지 뒤에 범용 "보너스 선택지" 1개를 확률적으로 추가.
        // 16종 이벤트 전부에 실제 3번째 선택지를 개별 저작하는 대신, 콘텐츠와 무관한 안전 보상으로 대체(플랜의 가정 사항 참고).
        // ExecuteRichOreVeinAsync 등 choice 인덱스를 직접 분기하는 특수 이벤트는 이 메서드를 쓰지 않아 영향받지 않는다.
        private EventData BuildEventDataWithBonusChoice(NodeEventData data, out bool bonusChoiceShown)
        {
            var opts = data.choices
                .Select(c => new EventOption(c.titleLocKey, c.descLocKey, new List<EffectType>()))
                .ToList();

            bonusChoiceShown = false;
            var charID = CharacterManager.Instance.SelectedCharacterID;
            float roll = CharacterManager.Instance.GetPassiveEventChoiceRoll(charID);
            if (roll > 0f && UnityEngine.Random.value < roll)
            {
                opts.Add(new EventOption("event_bonus_choice_title", "event_bonus_choice_desc", new List<EffectType>()));
                bonusChoiceShown = true;
            }

            return new EventData(data.titleLocKey, data.descLocKey, false, opts);
        }

        private void ApplyAdventurerBonusChoice()
        {
            var balance = GameBalanceData.Instance;
            InventoryManager.Instance.AddItem("Item_Stone", balance != null ? balance.AdventurerBonusStoneReward : 3);
            int healAmount = balance != null ? balance.AdventurerBonusHealAmount : 1;
            if (healAmount > 0) StatManager.Instance.Heal(healAmount);
            ShowFeedback(LocalizationManager.Instance.GetTranslation("event_bonus_choice_result"), Color.cyan);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Event]\nAdventurer Bonus Choice\nStone : +{(balance != null ? balance.AdventurerBonusStoneReward : 3)}\nHeal : +{healAmount}");
#endif
        }

        // ── Effect Dispatch ─────────────────────────────────────────────────

        private async UniTask ApplyEffectAsync(NodeEventID id, int choice, int depth)
        {
            switch (id)
            {
                case NodeEventID.Rockfall:            ApplyRockfall(choice);           break;
                case NodeEventID.PoisonMist:          ApplyPoisonMist(choice);         break;
                case NodeEventID.SpikeTrap:           ApplySpikeTrap(choice);          break;
                case NodeEventID.CollapsedTunnel:     ApplyCollapsedTunnel();          break;
                case NodeEventID.DarkAltar:           ApplyDarkAltar(choice);          break;
                case NodeEventID.LostSpirit:          ApplyLostSpirit(choice);         break;
                case NodeEventID.AbandonedCamp:       ApplyAbandonedCamp();            break;
                case NodeEventID.HotSpring:           ApplyHotSpring();                break;
                case NodeEventID.AbandonedEquipment:  ApplyAbandonedEquipment();       break;
                case NodeEventID.MinersJournal:       ApplyMinersJournal();            break;
                case NodeEventID.DevilContract:       ApplyDevilContract(choice);      break;
                case NodeEventID.MineGodBlessing:     ApplyMineGodBlessing(choice);    break;
                default: break;
            }
            await UniTask.CompletedTask;
        }

        // ── 1. Rockfall ─────────────────────────────────────────────────────
        // ① 대피 (곡괭이 내구도 -10%) ② 맞기 (HP-2, Iron×2)
        private void ApplyRockfall(int choice)
        {
            if (choice == 0)
            {
                var pdm = PickaxeDurabilityManager.Instance;
                if (pdm != null)
                {
                    int loss = Mathf.Max(1, Mathf.RoundToInt(pdm.MaxDurability * 0.1f));
                    pdm.ApplyRelicDurabilityModifier(-loss);
                }
                ShowFeedback(LocalizationManager.Instance.GetTranslation("event_rockfall_dodge"), Color.yellow);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Event]\nRockfall\nChoice : Dodge\nDurability : -10%");
#endif
            }
            else
            {
                int trapRed  = RelicManager.Instance?.GetTrapDamageReduction() ?? 0;
                int rockDmg  = Mathf.Max(0, 2 - trapRed);
                if (rockDmg > 0) StatManager.Instance.TakeDamage(rockDmg);
                InventoryManager.Instance.AddItem("Item_Iron", 2);
                ShowFeedback(rockDmg > 0 ? $"-{rockDmg} HP  +Iron×2" : "+Iron×2", new Color(0.8f, 0.4f, 0f));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Event]\nRockfall\nChoice : Hit\nHP : -{rockDmg}\nIron : +2");
#endif
            }
        }

        // ── 2. Poison Mist ──────────────────────────────────────────────────
        // ① 6턴 공격력 -10% (Poison)  ② 광물 5개 소비
        private void ApplyPoisonMist(int choice)
        {
            if (choice == 0)
            {
                StatusEffectManager.Instance?.ApplyPoison(6);
                ShowFeedback(LocalizationManager.Instance.GetTranslation("status_poison_name"), new Color(0.4f, 0.8f, 0.2f));
            }
            else
            {
                int spent = SpendMinerals(5);
                ShowFeedback($"-{spent} 광물", Color.gray);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Event]\nPoison Mist\nChoice : Dash\nMinerals Spent : {spent}");
#endif
            }
        }

        // ── 3. Spike Trap ───────────────────────────────────────────────────
        // ① 우회 (곡괭이 내구도 -15%)  ② 돌파 (70% safe, 30% HP-3)
        private void ApplySpikeTrap(int choice)
        {
            if (choice == 0)
            {
                var pdm = PickaxeDurabilityManager.Instance;
                if (pdm != null)
                {
                    int loss = Mathf.Max(1, Mathf.RoundToInt(pdm.MaxDurability * 0.15f));
                    pdm.ApplyRelicDurabilityModifier(-loss);
                }
                ShowFeedback(LocalizationManager.Instance.GetTranslation("event_trap_detour"), Color.yellow);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Event]\nSpikeTrap\nChoice : Detour\nDurability : -15%");
#endif
            }
            else
            {
                if (UnityEngine.Random.value < 0.30f)
                {
                    int trapRed2  = RelicManager.Instance?.GetTrapDamageReduction() ?? 0;
                    int spikeDmg  = Mathf.Max(0, 3 - trapRed2);
                    if (spikeDmg > 0)
                    {
                        StatManager.Instance.TakeDamage(spikeDmg);
                        EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.2f);
                        ShowFeedback($"-{spikeDmg} HP", Color.red);
                    }
                    else
                    {
                        ShowFeedback(LocalizationManager.Instance.GetTranslation("event_trap_safe"), Color.green);
                    }
                }
                else
                {
                    ShowFeedback(LocalizationManager.Instance.GetTranslation("event_trap_safe"), Color.green);
                }
            }
        }

        // ── 4. Collapsed Tunnel ─────────────────────────────────────────────
        // 자동: 곡괭이 내구도 -20%, 다음 노드 스킵
        private void ApplyCollapsedTunnel()
        {
            var pdm = PickaxeDurabilityManager.Instance;
            if (pdm != null)
            {
                int loss = Mathf.Max(1, Mathf.RoundToInt(pdm.MaxDurability * 0.2f));
                pdm.ApplyRelicDurabilityModifier(-loss);
                ShowFeedback($"내구도 -{loss}", new Color(0.7f, 0.5f, 0.2f));
            }
            SkipNextNode = true;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Event]\nCollapsed Tunnel\nDurability : -20%\nNextNode : Skip");
#endif
        }

        // ── 5. Dark Altar ───────────────────────────────────────────────────
        // ① HP-5 + 랜덤 유물  ② 스킵
        private void ApplyDarkAltar(int choice)
        {
            if (choice == 0)
            {
                StatManager.Instance.TakeDamage(5);
                if (StatManager.Instance.CurrentHP <= 0) return;

                var relics = RelicManager.Instance?.GetRandomRelicChoices(1);
                if (relics != null && relics.Count > 0)
                {
                    RelicManager.Instance.AddRelic(relics[0]);
                    string name = LocalizationManager.Instance.GetTranslation(relics[0].nameLocKey);
                    ShowFeedback($"-5 HP  +{name}", new Color(0.8f, 0.3f, 1f));
                }
                else
                {
                    ShowFeedback("-5 HP", Color.red);
                }
            }
        }

        // ── 6. Lost Spirit ──────────────────────────────────────────────────
        // ① 광물 5개 소비 + 랜덤 버프  ② 무시 → CurseInstantDamageOnEncounter
        private void ApplyLostSpirit(int choice)
        {
            if (choice == 0)
            {
                SpendMinerals(5);
                GiveRandomBuff();
                ShowFeedback(LocalizationManager.Instance.GetTranslation("event_spirit_appeased"), new Color(0.6f, 0.8f, 1f));
            }
            else
            {
                StatManager.Instance.AddEffect(EffectType.CurseInstantDamageOnEncounter);
                ShowFeedback(LocalizationManager.Instance.GetTranslation("event_spirit_angered"), Color.red);
            }
        }

        // ── 7. Abandoned Camp ───────────────────────────────────────────────
        // 자동: HP 회복 30%, 곡괭이 내구도 +20%
        private void ApplyAbandonedCamp()
        {
            int heal = Mathf.Max(2, Mathf.RoundToInt(StatManager.Instance.GetMaxHP() * 0.3f));
            StatManager.Instance.Heal(heal);

            int repair = Mathf.Max(1, Mathf.RoundToInt((PickaxeDurabilityManager.Instance?.MaxDurability ?? 10) * 0.2f));
            PickaxeDurabilityManager.Instance?.Repair(repair);

            ShowFeedback($"+{heal} HP  내구도 회복", Color.green);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Event]\nAbandoned Camp\nHP : +{heal}\nRepair : +{repair}");
#endif
        }

        // ── 8. Rich Ore Vein ────────────────────────────────────────────────
        // ① 일반 광석 보상  ② 3배 광석 + 20% 엘리트 소환
        private async UniTask ExecuteRichOreVeinAsync(NodeEventData data, int depth)
        {
            _lastTriggerFloor[NodeEventID.RichOreVein] = depth;

            var eventData = BuildEventData(data);
            int choice = await EventManager.Instance.ShowEventChoiceAsync(eventData);
            if (StatManager.Instance.CurrentHP <= 0) return;

            if (choice == 0)
            {
                InventoryManager.Instance.AddItem("Item_Iron",   3);
                InventoryManager.Instance.AddItem("Item_Silver", 2);
                InventoryManager.Instance.AddItem("Item_Gold",   1);
                ShowFeedback("Iron×3  Silver×2  Gold×1", new Color(0.9f, 0.8f, 0.2f));
            }
            else
            {
                InventoryManager.Instance.AddItem("Item_Iron",   9);
                InventoryManager.Instance.AddItem("Item_Silver", 6);
                InventoryManager.Instance.AddItem("Item_Gold",   3);
                ShowFeedback("Iron×9  Silver×6  Gold×3!", new Color(1f, 0.8f, 0f));

                if (UnityEngine.Random.value < 0.20f)
                {
                    ShowFeedback(LocalizationManager.Instance.GetTranslation("event_rich_ore_elite"), new Color(1f, 0.5f, 0f));
                    await UniTask.Delay(800);
                    await EliteCombatSystem.Instance.StartEliteCombatAsync(depth);
                }
            }
        }

        // ── 10. Hot Spring ──────────────────────────────────────────────────
        // 자동: HP 50% 회복, 화상/중독 해제
        private void ApplyHotSpring()
        {
            int heal = Mathf.Max(1, StatManager.Instance.GetMaxHP() / 2);
            StatManager.Instance.Heal(heal);
            StatusEffectManager.Instance?.CureBurn();
            StatusEffectManager.Instance?.CurePoison();
            ShowFeedback($"+{heal} HP  화상/중독 해제", Color.cyan);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Event]\nHot Spring\nHeal : {heal}");
#endif
        }

        // ── 11. Abandoned Equipment ─────────────────────────────────────────
        // 자동: Attack+1, Mining+1, 수리+20% 중 랜덤
        private void ApplyAbandonedEquipment()
        {
            int roll = UnityEngine.Random.Range(0, 3);
            switch (roll)
            {
                case 0:
                    StatManager.Instance.AddEffect(EffectType.BuffAttackDamage);
                    ShowFeedback("공격력 +1", new Color(1f, 0.7f, 0.2f));
                    break;
                case 1:
                    StatManager.Instance.AddEffect(EffectType.BuffMiningPower);
                    ShowFeedback("채굴력 +1", new Color(0.5f, 0.9f, 0.5f));
                    break;
                case 2:
                    int repair = Mathf.Max(1, Mathf.RoundToInt((PickaxeDurabilityManager.Instance?.MaxDurability ?? 10) * 0.2f));
                    PickaxeDurabilityManager.Instance?.Repair(repair);
                    ShowFeedback("곡괭이 내구도 +20%", new Color(0.8f, 0.6f, 0.3f));
                    break;
            }
        }

        // ── 12. Miner's Journal ─────────────────────────────────────────────
        // 자동: 20층 동안 광석 +20%, HUD 버프 아이콘
        private void ApplyMinersJournal()
        {
            _minerJournalFloors = 20;
            EffectManager.Instance?.RegisterEffect(
                MinerJournalEffectID,
                "effect_ore_gain_name",
                "effect_ore_gain_desc",
                EffectSystemType.Buff,
                20,
                $"+20% 20층",
                "Event",
                "Effect_Buff_OreGain"
            );
            ShowFeedback("+20% 광석 획득 (20층)", new Color(0.9f, 0.7f, 0.2f));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Event]\nMiner's Journal\nOre Gain : +20%\nFloors : 20");
#endif
        }

        // ── 13. Crystal Shrine (dynamic) ────────────────────────────────────
        // 3개 랜덤 축복 중 1개 선택
        private async UniTask ExecuteCrystalShrineAsync(int depth)
        {
            _lastTriggerFloor[NodeEventID.CrystalShrine] = depth;

            var pool = new List<(EffectType type, string titleKey)>
            {
                (EffectType.BuffAttackDamage,           "event_shrine_atk"),
                (EffectType.BuffMiningPower,            "event_shrine_mining"),
                (EffectType.BuffMaxHP,                  "event_shrine_hp"),
                (EffectType.BuffMonsterSpawnRateDecrease,"event_shrine_spawn")
            };
            Shuffle(pool);
            var chosen = pool.Take(3).ToList();

            var opts = chosen.Select(p =>
                new EventOption(p.titleKey, p.titleKey + "_desc", new List<EffectType>())
            ).ToList();

            var eventData = new EventData("event_shrine_title", "event_shrine_desc", false, opts);
            int choice = await EventManager.Instance.ShowEventChoiceAsync(eventData);
            if (StatManager.Instance.CurrentHP <= 0) return;

            if (choice >= 0 && choice < chosen.Count)
            {
                StatManager.Instance.AddEffect(chosen[choice].type);
                string label = LocalizationManager.Instance.GetTranslation(chosen[choice].titleKey);
                ShowFeedback(label + " 획득!", new Color(0.5f, 0.9f, 1f));
            }
        }

        // ── 14. Ancient Forge (dynamic) ─────────────────────────────────────
        // 광물 소비 → Attack+1 (5 Gold), Mining+1 (5 Iron), HP+5 (3 Silver), 스킵
        private async UniTask ExecuteAncientForgeAsync(int depth)
        {
            _lastTriggerFloor[NodeEventID.AncientForge] = depth;

            var opts = new List<EventOption>
            {
                new EventOption("event_forge_atk_title",    "event_forge_atk_desc",    new List<EffectType>()),
                new EventOption("event_forge_mining_title", "event_forge_mining_desc", new List<EffectType>()),
                new EventOption("event_forge_heal_title",   "event_forge_heal_desc",   new List<EffectType>()),
                new EventOption("event_forge_skip_title",   "event_forge_skip_desc",   new List<EffectType>())
            };
            var eventData = new EventData("event_forge_title", "event_forge_desc", false, opts);
            int choice = await EventManager.Instance.ShowEventChoiceAsync(eventData);
            if (StatManager.Instance.CurrentHP <= 0) return;

            string noFunds = LocalizationManager.Instance.GetTranslation("event_merchant_no_funds");
            switch (choice)
            {
                case 0:
                    if (InventoryManager.Instance.GetItemCount("Item_Gold") >= 5)
                    {
                        InventoryManager.Instance.RemoveItem("Item_Gold", 5);
                        StatManager.Instance.AddEffect(EffectType.BuffAttackDamage);
                        ShowFeedback("-5 Gold  공격력+1", Color.yellow);
                    }
                    else ShowFeedback(noFunds, Color.red);
                    break;
                case 1:
                    if (InventoryManager.Instance.GetItemCount("Item_Iron") >= 5)
                    {
                        InventoryManager.Instance.RemoveItem("Item_Iron", 5);
                        StatManager.Instance.AddEffect(EffectType.BuffMiningPower);
                        ShowFeedback("-5 Iron  채굴력+1", new Color(0.5f, 0.9f, 0.5f));
                    }
                    else ShowFeedback(noFunds, Color.red);
                    break;
                case 2:
                    if (InventoryManager.Instance.GetItemCount("Item_Silver") >= 3)
                    {
                        InventoryManager.Instance.RemoveItem("Item_Silver", 3);
                        StatManager.Instance.Heal(5);
                        ShowFeedback("-3 Silver  +5 HP", Color.green);
                    }
                    else ShowFeedback(noFunds, Color.red);
                    break;
                // case 3: skip
            }
        }

        // ── 15. Devil Contract ──────────────────────────────────────────────
        // ① 공격력 계약: HP-20%, Attack+2
        // ② 채굴력 계약: HP-20%, Mining+2
        // ③ 거부: 아무 효과 없음
        private void ApplyDevilContract(int choice)
        {
            if (choice == 2) return;

            int loss = Mathf.Max(1, Mathf.RoundToInt(StatManager.Instance.GetMaxHP() * 0.2f));
            StatManager.Instance.TakeDamage(loss);
            if (StatManager.Instance.CurrentHP <= 0) return;

            if (choice == 0)
            {
                StatManager.Instance.AddEffect(EffectType.BuffAttackDamage);
                StatManager.Instance.AddEffect(EffectType.BuffAttackDamage);
                ShowFeedback($"-{loss} HP  공격력+2", new Color(1f, 0.3f, 0.3f));
            }
            else
            {
                StatManager.Instance.AddEffect(EffectType.BuffMiningPower);
                StatManager.Instance.AddEffect(EffectType.BuffMiningPower);
                ShowFeedback($"-{loss} HP  채굴력+2", new Color(0.5f, 0.7f, 1f));
            }
        }

        // ── 16. Mine God's Blessing ─────────────────────────────────────────
        // ① 공격력+1  ② 채굴력+1  ③ 최대HP+2
        private void ApplyMineGodBlessing(int choice)
        {
            switch (choice)
            {
                case 0:
                    StatManager.Instance.AddEffect(EffectType.BuffAttackDamage);
                    ShowFeedback("공격력 +1", new Color(1f, 0.8f, 0.2f));
                    break;
                case 1:
                    StatManager.Instance.AddEffect(EffectType.BuffMiningPower);
                    ShowFeedback("채굴력 +1", new Color(0.5f, 0.9f, 0.5f));
                    break;
                case 2:
                    StatManager.Instance.AddEffect(EffectType.BuffMaxHP);
                    ShowFeedback("최대 체력 +2", Color.green);
                    break;
            }
        }

        // ── Fallback ────────────────────────────────────────────────────────

        private async UniTask FallbackHealAsync(int depth)
        {
            int heal = Mathf.Max(1, Mathf.RoundToInt(StatManager.Instance.GetMaxHP() * 0.2f));
            StatManager.Instance.Heal(heal);
            ShowFeedback($"+{heal} HP", Color.green);
            await UniTask.CompletedTask;
        }

        // ── Utilities ───────────────────────────────────────────────────────

        private void GiveRandomBuff()
        {
            var pool = new[] { EffectType.BuffAttackDamage, EffectType.BuffMiningPower };
            StatManager.Instance.AddEffect(pool[UnityEngine.Random.Range(0, pool.Length)]);
        }

        private int SpendMinerals(int amount)
        {
            int remaining = amount;
            foreach (var id in MineralSpendOrder)
            {
                int have  = InventoryManager.Instance.GetItemCount(id);
                int spend = Mathf.Min(remaining, have);
                if (spend > 0)
                {
                    InventoryManager.Instance.RemoveItem(id, spend);
                    remaining -= spend;
                }
                if (remaining <= 0) break;
            }
            return amount - remaining;
        }

        private void ShowFeedback(string msg, Color color)
        {
            if (Camera.main == null) return;
            EffectSystem.Instance.SpawnDamageText(
                Camera.main.transform.position + Camera.main.transform.forward * 1.5f,
                msg, color);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
