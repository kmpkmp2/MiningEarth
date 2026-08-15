using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Mining;
using DeepEarth.Combat;
using DeepEarth.Event;
using DeepEarth.UI;
using DeepEarth.Map;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        [Header("Route Map Config")]
        [SerializeField] private DeepEarth.Map.DefaultGridTemplate  gridTemplate;
        [SerializeField] private DeepEarth.Map.RoomGenerationConfig roomConfig;

        public void SetMapConfig(DeepEarth.Map.DefaultGridTemplate template, DeepEarth.Map.RoomGenerationConfig config)
        {
            gridTemplate = template;
            roomConfig   = config;
        }

        [Header("Game States")]
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        [Header("Run Data")]
        public int CurrentDepth { get; private set; } = 0;
        public int IronCount => InventoryManager.Instance.GetItemCount("Item_Iron");
        public int SilverCount => InventoryManager.Instance.GetItemCount("Item_Silver");
        public int GoldCount => InventoryManager.Instance.GetItemCount("Item_Gold");
        public int DiamondCount => InventoryManager.Instance.GetItemCount("Item_Diamond");
        public int WillEarnedThisRun { get; private set; } = 0;

        public void AddRunWill(int amount)
        {
            WillEarnedThisRun += amount;
            OnGameDataChanged?.Invoke();
        }

        // UI references
        private GameObject _hudObject;
        private GameObject _gameOverObject;
        private GameObject _eventObject;
        private GameObject _settingsObject;
        private GameObject _relicPopupObject;
        private GameObject _inventoryPopupObject;
        private GameObject _eventRevealObject;
        private GameObject _mapPopupObject;
        private GameObject _merchantObject;
        private GameObject _relicCopyPopupObject;
        private GameObject _achievementNotifObject;
        private DeepEarth.UI.AchievementNotificationView _achievementNotifView;

        private GameUIPresenter _hudPresenter;
        private GameOverUIPresenter _gameOverPresenter;
        private EventUIPresenter _eventPresenter;
        private SettingsUIPresenter _settingsPresenter;
        private RelicPopupPresenter _relicPopupPresenter;
        private InventoryPresenter _inventoryPopupPresenter;
        private EventRevealPresenter _eventRevealPresenter;
        private MerchantPresenter _merchantPresenter;
        private DeepEarth.Map.RouteMapPresenter _routeMapPresenter;

        // 그룹 L(수집가의 가방) 전용 — RelicManager가 참조하는 강제선택 팝업. 신규 Singleton이 아니라 GameManager가 소유.
        public RelicCopyPopupPresenter RelicCopyPopupPresenter { get; private set; }

        private GameState _previousState;

        public event Action OnGameDataChanged;

        public string DifficultyName
        {
            get
            {
                if (CurrentDepth < 30) return "diff_very_easy";
                if (CurrentDepth < 80) return "diff_easy";
                if (CurrentDepth < 150) return "diff_medium";
                if (CurrentDepth < 250) return "diff_hard";
                return "diff_very_hard";
            }
        }

        private int DifficultyLevel
        {
            get
            {
                if (CurrentDepth < 50) return 1;
                if (CurrentDepth < 100) return 2;
                if (CurrentDepth < 200) return 3;
                return 4;
            }
        }

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

        private DepthRewardTable _depthRewardTable;
        public DepthRewardTable DepthRewardTable => _depthRewardTable;

        public RectTransform GetInventoryButtonRect() => _hudPresenter?.GetInventoryButtonRect();

        private void Start()
        {
            // Register HP death hook
            StatManager.Instance.OnHPChanged += CheckPlayerDeath;

            // Load DepthRewardTable via Addressables
            LoadDepthRewardTableAsync().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid LoadDepthRewardTableAsync()
        {
            _depthRewardTable = await ResourceManager.Instance.LoadAssetAsync<DepthRewardTable>(AddressableKeys.DepthRewardTable);
            if (_depthRewardTable == null)
                Debug.LogWarning("[GameManager] DepthRewardTable not found. Depth bonuses will be 0.");
        }

        private void OnDestroy()
        {
            if (StatManager.Instance != null)
            {
                StatManager.Instance.OnHPChanged -= CheckPlayerDeath;
            }

            DisposePresenters();
        }

        private void DisposePresenters()
        {
            _hudPresenter?.Dispose();
            _gameOverPresenter?.Dispose();
            _eventPresenter?.Dispose();
            _settingsPresenter?.Dispose();
            _relicPopupPresenter?.Dispose();
            _relicPopupPresenter = null;
            _inventoryPopupPresenter?.Dispose();
            _inventoryPopupPresenter = null;
            _eventRevealPresenter?.Dispose();
            _eventRevealPresenter = null;
            _routeMapPresenter?.Dispose();
            _routeMapPresenter = null;
            _merchantPresenter?.Dispose();
            _merchantPresenter = null;
        }

        public void SetGameState(GameState state)
        {
            CurrentState = state;
            OnGameDataChanged?.Invoke();
        }

        public void AdvanceDepth()
        {
            CurrentDepth++;
            OnGameDataChanged?.Invoke();
            DeepEarth.Common.GameEvents.FireDepthReached(CurrentDepth);
        }

        public async UniTask InitializeUIAsync(Camera mainCamera, Canvas canvas, Image flashOverlay, GameObject particlePrefab)
        {
            try
            {
                // Instantiate UI views via Addressables
                // 각 패널은 로드 직후 즉시 비활성화한다 — 이후 남은 패널들의 비동기 로드가
                // 진행되는 동안 화면에 노출되어 깜빡이는 것을 방지하기 위함(맨 끝에서 일괄 처리하지 않음).
                _hudObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelHUD, canvas.transform);
                // HUD는 항상 낮은 sibling index에 고정한다 — CombatSystem.SetupBattleUIAsync가 GameBootstrap.Awake()에서
                // 별도로 먼저 UI_Panel_Battle을 생성하는 것과 경쟁(race)하지 않도록, HUD 스스로 위치를 고정시킨다.
                // (그렇지 않으면 로드 완료 순서에 따라 HUD가 Battle 위에 그려질 수 있음.)
                _hudObject?.transform.SetSiblingIndex(1);
                _gameOverObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelGameOver, canvas.transform);
                _gameOverObject?.SetActive(false);
                _eventObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelEvent, canvas.transform);
                _eventObject?.SetActive(false);
                _settingsObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelSettings, canvas.transform);
                _settingsObject?.SetActive(false);

                _relicPopupObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelRelicPopup, canvas.transform);
                _relicPopupObject?.SetActive(false);
                if (_relicPopupObject == null)
                {
                    Debug.LogWarning("UIPanelRelicPopup failed to load. Creating fallback placeholder...");
                    _relicPopupObject = new GameObject("RelicPopup_Fallback", typeof(RectTransform));
                    _relicPopupObject.transform.SetParent(canvas.transform, false);
                    var fallbackView = _relicPopupObject.AddComponent<RelicPopupView>();
                    
                    var bg = new GameObject("Bg", typeof(RectTransform));
                    bg.transform.SetParent(_relicPopupObject.transform, false);
                    bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                    
                    var title = new GameObject("Title", typeof(RectTransform));
                    title.transform.SetParent(bg.transform, false);
                    var titleText = title.AddComponent<TextMeshProUGUI>();
                    titleText.text = "Relic Popup Fallback";
                    
                    var close = new GameObject("Close", typeof(RectTransform));
                    close.transform.SetParent(bg.transform, false);
                    var closeBtn = close.AddComponent<Button>();
                    
                    var content = new GameObject("Content", typeof(RectTransform));
                    content.transform.SetParent(bg.transform, false);
                    
                    SetRef(fallbackView, "popupRoot", bg);
                    SetRef(fallbackView, "closeButton", closeBtn);
                    SetRef(fallbackView, "titleText", titleText);
                    SetRef(fallbackView, "contentParent", content.transform);
                }

                _inventoryPopupObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelInventoryPopup, canvas.transform);
                _inventoryPopupObject?.SetActive(false);
                if (_inventoryPopupObject == null)
                {
                    Debug.LogWarning("UIPanelInventoryPopup failed to load. Creating fallback placeholder...");
                    _inventoryPopupObject = new GameObject("InventoryPopup_Fallback", typeof(RectTransform));
                    _inventoryPopupObject.transform.SetParent(canvas.transform, false);
                    var fallbackView = _inventoryPopupObject.AddComponent<InventoryPopupView>();
                    
                    var bg = new GameObject("Bg", typeof(RectTransform));
                    bg.transform.SetParent(_inventoryPopupObject.transform, false);
                    bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                    
                    var title = new GameObject("Title", typeof(RectTransform));
                    title.transform.SetParent(bg.transform, false);
                    var titleText = title.AddComponent<TextMeshProUGUI>();
                    titleText.text = "Inventory Popup Fallback";
                    
                    var stats = new GameObject("Stats", typeof(RectTransform));
                    stats.transform.SetParent(bg.transform, false);
                    var statsText = stats.AddComponent<TextMeshProUGUI>();
                    
                    var close = new GameObject("Close", typeof(RectTransform));
                    close.transform.SetParent(bg.transform, false);
                    var closeBtn = close.AddComponent<Button>();
                    
                    SetRef(fallbackView, "popupRoot", bg);
                    SetRef(fallbackView, "closeButton", closeBtn);
                    SetRef(fallbackView, "titleText", titleText);
                    SetRef(fallbackView, "inventoryStatsText", statsText);
                }

                // Load Merchant panel — UI_Panel_Event와 완전히 분리된 전용 상점 팝업
                _merchantObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelMerchant, canvas.transform);
                _merchantObject?.SetActive(false);

                // Load Event Reveal panel
                _eventRevealObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelEventReveal, canvas.transform);
                _eventRevealObject?.SetActive(false);

                if (_hudObject == null || _gameOverObject == null || _eventObject == null || _settingsObject == null || _eventRevealObject == null)
                {
                    Debug.LogError("GameManager: One or more UI panels failed to instantiate via Addressables!");
                    return;
                }

                var hudView = _hudObject.GetComponent<GameUIView>();
                var gameOverView = _gameOverObject.GetComponent<GameOverUIView>();
                var eventView = _eventObject.GetComponent<EventUIView>();
                var settingsView = _settingsObject.GetComponent<SettingsUIView>();
                var relicPopupView = _relicPopupObject.GetComponent<RelicPopupView>();
                var inventoryPopupView = _inventoryPopupObject.GetComponent<InventoryPopupView>();
                var revealView = _eventRevealObject.GetComponent<EventRevealView>();

                // Setup EffectSystem
                EffectSystem.Instance.Initialize(mainCamera, canvas, flashOverlay, particlePrefab);

                // Setup Presenters
                _hudPresenter = new GameUIPresenter(hudView, this);

                // Setup Pickaxe Durability Presenter (auto-add component if not wired in prefab)
                var pickaxeView = _hudObject.GetComponent<DeepEarth.UI.PickaxeDurabilityView>();
                if (pickaxeView == null)
                    pickaxeView = _hudObject.AddComponent<DeepEarth.UI.PickaxeDurabilityView>();
                PickaxeDurabilityManager.Instance?.SetupPresenter(pickaxeView);

                // Achievement in-game notification
                if (AchievementManager.Instance != null)
                {
                    // 이전 런(이전 씬 로드)에서 만들어진 알림 View가 있다면 먼저 구독 해제한다.
                    // ReferenceEquals를 쓰는 이유: 씬 전환으로 이미 파괴된 UnityEngine.Object는
                    // '!= null' 비교(오버로드된 연산자)가 false를 반환해 구독 해제 자체를 건너뛰게 되고,
                    // 그 결과 파괴된 구독자가 계속 남아 이후 알림 델리게이트 호출을 통째로 막는 문제가 있었다.
                    if (!ReferenceEquals(_achievementNotifView, null))
                        AchievementManager.Instance.OnAchievementCompleted -= _achievementNotifView.ShowNotification;

                    _achievementNotifObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelAchievementNotification, canvas.transform);
                    _achievementNotifView = _achievementNotifObject != null
                        ? _achievementNotifObject.GetComponent<DeepEarth.UI.AchievementNotificationView>()
                        : null;
                    if (_achievementNotifView != null)
                        AchievementManager.Instance.OnAchievementCompleted += _achievementNotifView.ShowNotification;
                    else
                        Debug.LogWarning("[GameManager] UI_Panel_AchievementNotification not found — Addressables에 등록 필요");
                }

                _gameOverPresenter = new GameOverUIPresenter(gameOverView, this);
                _eventPresenter = new EventUIPresenter(eventView);
                _settingsPresenter = new SettingsUIPresenter(settingsView, this);
                _relicPopupPresenter = new RelicPopupPresenter(relicPopupView, this);
                _inventoryPopupPresenter = new InventoryPresenter(inventoryPopupView, this);
                _eventRevealPresenter = new EventRevealPresenter(revealView);
                EventManager.Instance.SetRevealPresenter(_eventRevealPresenter);

                // Route Map UI
                _mapPopupObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelMapPopup, canvas.transform);
                if (_mapPopupObject != null)
                {
                    var mapPopupView = _mapPopupObject.GetComponent<DeepEarth.UI.MapPopupView>();
                    if (mapPopupView != null)
                    {
                        _routeMapPresenter = new DeepEarth.Map.RouteMapPresenter(mapPopupView, gridTemplate, roomConfig);

                        var saveData = SaveManager.CurrentData;
                        if (saveData.MapSaveData != null && saveData.MapSaveData.HasActiveMap)
                            _routeMapPresenter.RestoreFromSave(saveData.MapSaveData);
                    }
                    mapPopupView?.Hide();
                }
                else
                {
                    Debug.LogWarning("[GameManager] UI_Panel_MapPopup not found — Addressables에 등록 필요");
                }

                // Merchant (떠돌이 상점) UI — UI_Panel_Event와 완전히 분리된 전용 팝업
                if (_merchantObject != null)
                {
                    var merchantPopupView = _merchantObject.GetComponent<MerchantPopupView>();
                    if (merchantPopupView != null)
                    {
                        _merchantPresenter = new MerchantPresenter(merchantPopupView);
                        merchantPopupView.SetVisible(false);
                    }
                }
                else
                {
                    Debug.LogWarning("[GameManager] UI_Panel_Merchant not found — Addressables에 등록 필요");
                }

                // 그룹 L(수집가의 가방) 전용 팝업 — UI_Panel_RelicPopup 구조를 재활용해 신규 제작
                _relicCopyPopupObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelRelicCopyPopup, canvas.transform);
                if (_relicCopyPopupObject != null)
                {
                    var relicCopyPopupView = _relicCopyPopupObject.GetComponent<DeepEarth.UI.RelicCopyPopupView>();
                    if (relicCopyPopupView != null)
                        RelicCopyPopupPresenter = new DeepEarth.UI.RelicCopyPopupPresenter(relicCopyPopupView);
                    _relicCopyPopupObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[GameManager] UI_Panel_RelicCopyPopup not found — Addressables에 등록 필요");
                }

                // Show Main HUD (다른 패널들은 각자 로드 직후 이미 비활성화됨)
                _hudObject.SetActive(true);

                // StartGame()은 더 이상 여기서 호출하지 않는다 — Battle UI/ThemeManager 등
                // 나머지 초기화가 모두 끝난 뒤 GameBootstrap.BootSequenceAsync가 마지막에 호출한다.
                // (CurrentState가 그 시점에야 Playing/MapSelecting으로 바뀌어야 기존의
                // CurrentState 기반 입력 게이트들이 "Ready 이후에만 입력 허용"으로 자동 동작한다.)
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameManager: Critical exception during InitializeUIAsync: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public void RunStart()
        {
            Debug.Log("[Run]\nNew Run Started");

            // 1. Run Inventory Clear
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ClearRunInventory();
                
                int runItemCount = InventoryManager.Instance.GetRunInventory().GetTotalItemCount();
                Debug.Log($"[Run]\nRun Inventory Count : {runItemCount}");
            }

            // 2. EffectManager Clear
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.ClearRunEffects();
            }

            // 2b. StatusEffect Clear (Burn etc.)
            StatusEffectManager.Instance?.ClearAll();

            // 2c. Relic Clear
            RelicManager.Instance?.ClearAll();
            StartingRelicManager.Instance?.ClearAll();

            // 2d. Pickaxe Durability Clear
            PickaxeDurabilityManager.Instance?.ClearForRun();

            // 3. Player Runtime Stat Reset
            StatManager.Instance.ResetStatsForRun();

            // 3a. 시작 아이템 + 전용 시작 유물 지급 — 반드시 위의 InventoryManager.ClearRunInventory()(1번)와
            // StatManager.ResetStatsForRun()(EffectManager.InitializeCharacterPassive 포함, 3번) 이후여야
            // 그 클리어 로직에 의해 지급 즉시 유실되지 않는다.
            var charData = CharacterDatabase.Get(CharacterManager.Instance.SelectedCharacterID);
            if (charData != null)
            {
                foreach (var entry in charData.StartingItems)
                {
                    if (entry.Item != null && entry.Quantity > 0)
                        InventoryManager.Instance.AddItem(entry.Item.itemID, entry.Quantity);
                }
                StartingRelicManager.Instance?.ApplyForCharacter(charData);
            }

            // 3b. Pickaxe Durability Init (after stat reset so upgrade level is current)
            PickaxeDurabilityManager.Instance?.InitializeForRun();

            // 4. Depth Reset
            CurrentDepth = 0;

            // 5. Event State Reset
            if (EventManager.Instance != null)
            {
                var choiceField = typeof(EventManager).GetField("_choiceTcs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                choiceField?.SetValue(EventManager.Instance, null);
            }

            // 6. Boss State Reset
            if (BossManager.Instance != null)
            {
                var spawnedBossField = typeof(BossManager).GetField("_spawnedBossObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var spawnedBoss = spawnedBossField?.GetValue(BossManager.Instance) as GameObject;
                if (spawnedBoss != null)
                {
                    if (PoolSystem.Instance != null) PoolSystem.Instance.Return(spawnedBoss);
                    spawnedBossField.SetValue(BossManager.Instance, null);
                }
                
                var bossPresField = typeof(BossManager).GetField("_bossPresenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var bossPresenter = bossPresField?.GetValue(BossManager.Instance) as IDisposable;
                bossPresenter?.Dispose();
                bossPresField?.SetValue(BossManager.Instance, null);

                var rewardPresField = typeof(BossManager).GetField("_bossRewardPresenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rewardPresenter = rewardPresField?.GetValue(BossManager.Instance) as IDisposable;
                rewardPresenter?.Dispose();
                rewardPresField?.SetValue(BossManager.Instance, null);
            }

            // 3D 터널 맵 리셋
            if (DeepEarth.Map.TunnelGenerator.Instance != null)
                DeepEarth.Map.TunnelGenerator.Instance.ResetGenerator();

            if (DeepEarth.Map.MapPresenter.Instance != null && DeepEarth.Map.MapPresenter.Instance.Model != null)
                DeepEarth.Map.MapPresenter.Instance.Model.CurrentDepth = 0;

            // Route Map 리셋
            _routeMapPresenter?.Reset();

            // Route Map 세이브 데이터 클리어 (새 런이므로 이전 맵 무효화)
            var mapSave = SaveManager.CurrentData;
            if (mapSave.MapSaveData != null)
                mapSave.MapSaveData.HasActiveMap = false;
        }

        public void StartGame()
        {
            RunStart();

            WillEarnedThisRun = 0;
            DeepEarth.Common.GameEvents.FireRunStarted();

            _hudObject.SetActive(true);
            _gameOverObject.SetActive(false);

            OnGameDataChanged?.Invoke();

            if (_routeMapPresenter != null)
            {
                // Route Map 모드: 맵 생성 후 노드 선택 화면 표시
                _routeMapPresenter.InitializeRun();
                CurrentState = GameState.MapSelecting;
                _routeMapPresenter.ShowMap();
            }
            else
            {
                // 폴백: RouteMapPresenter 없을 경우 기존 선형 채굴 모드
                CurrentState = GameState.Playing;
                MiningSystem.Instance.SpawnNextBlockAsync().Forget();
            }
        }

        public void PauseForEvent()
        {
            CurrentState = GameState.EventPause;
        }

        public void ResumeAfterEvent()
        {
            CurrentState = GameState.Playing;
            OnGameDataChanged?.Invoke();

            // Route Map 모드에서는 노드 핸들러가 완료를 대기 중이므로 여기서 블록을 스폰하지 않는다.
            if (_routeMapPresenter != null) return;

            // 기존 선형 채굴 모드
            MiningSystem.Instance.SpawnNextBlockAsync().Forget();
        }

        public void TriggerStatsOrResourcesChanged()
        {
            OnGameDataChanged?.Invoke();
        }

        public async UniTaskVoid OnBlockMined()
        {
            if (CurrentState != GameState.Playing) return;

            // Route Map 모드: AdvanceDepth()가 HandleRouteNodeAsync()에서 이미 호출됨
            if (_routeMapPresenter == null)
            {
                CurrentDepth++;
                OnGameDataChanged?.Invoke();
                DeepEarth.Common.GameEvents.FireDepthReached(CurrentDepth);
            }

            // 3D 터널 슬라이드 갱신
            if (DeepEarth.Map.MapPresenter.Instance != null)
                await DeepEarth.Map.MapPresenter.Instance.HandleBlockMinedAsync(CurrentDepth);

            // ── Route Map 모드: Mine 노드 완료 처리 후 종료 ───────────────
            if (_routeMapPresenter != null)
            {
                if (StatManager.Instance.CurrentHP <= 0) return;
                await _routeMapPresenter.OnMineNodeCompleted();
                return;
            }

            // ── 기존 선형 모드 ─────────────────────────────────────────────

            // Boss trigger
            if (CurrentDepth > 0 && CurrentDepth % 50 == 0)
            {
                await EventManager.Instance.PlayRevealAsync(EventRevealType.Boss);
                BossManager.Instance.StartBossSequenceAsync(CurrentDepth).Forget();
                return;
            }

            // Rest checkpoint trigger: 이 시점에 도달했다는 것은 이미 보스 깊이(%50==0)가 아니라는 뜻이므로
            // 별도의 겹침 방지 조건 없이 그대로 확정 체크포인트로 처리한다.
            if (CurrentDepth > 0 && CurrentDepth % GameSettings.RestCheckpointInterval == 0)
            {
                await TriggerRestCheckpointAsync();
                if (StatManager.Instance.CurrentHP > 0 && CurrentState == GameState.Playing)
                    await MiningSystem.Instance.SpawnNextBlockAsync();
                return;
            }

            // 1. Combat trigger
            float monsterChance = GetMonsterSpawnChance(CurrentDepth) * StatManager.Instance.GetMonsterSpawnRateMultiplier();
            if (UnityEngine.Random.value < monsterChance)
            {
                MonsterType mType = CombatSystem.Instance.PickMonsterForDepth(CurrentDepth);
                EventRevealType mReveal = MonsterTypeToReveal(mType);
                await EventManager.Instance.PlayRevealAsync(mReveal, CombatSystem.Instance.GetMonsterDeathTriggerDescKey(mType));

                EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.2f), 0.2f);
                string nameLocKey   = CombatSystem.Instance.GetMonsterNameLocKey(mType);
                string monsterName  = string.IsNullOrEmpty(nameLocKey)
                    ? mType.ToString()
                    : LocalizationManager.Instance.GetTranslation(nameLocKey);
                string encounterMsg = LocalizationManager.Instance.GetFormatted("combat_monster_encounter_fmt", monsterName);
                EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, encounterMsg, Color.red);

                await CombatSystem.Instance.StartCombatAsync(mType, CurrentDepth);
                if (StatManager.Instance.CurrentHP <= 0) return;
            }
            else
            {
                // 2. Hazard trigger
                float hazardChance = GetHazardSpawnChance(CurrentDepth) * StatManager.Instance.GetHazardSpawnRateMultiplier();
                if (UnityEngine.Random.value < hazardChance)
                {
                    bool isLava = UnityEngine.Random.value < 0.5f;
                    await EventManager.Instance.PlayRevealAsync(isLava ? EventRevealType.Lava : EventRevealType.Water);

                    if (isLava)
                    {
                        StatusEffectManager.Instance.ApplyBurn();
                        DeepEarth.Common.GameEvents.FireLavaEncountered();
                        EffectSystem.Instance.FlashScreen(new Color(1f, 0.4f, 0f, 0.35f), 0.25f);
                        EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);
                        string burnMsg = LocalizationManager.Instance.GetTranslation("status_burn_applied_msg");
                        EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, burnMsg.Length > 0 ? burnMsg : "화상!", new Color(1f, 0.4f, 0f));
                    }
                    else
                    {
                        DeepEarth.Common.GameEvents.FireWaterEncountered();
                        // 유물: 방수 장화 — 수몰 피해 면역
                        if (RelicManager.Instance?.HasFloodImmunity() ?? false)
                        {
                            EffectSystem.Instance.FlashScreen(new Color(0f, 0.4f, 1f, 0.2f), 0.15f);
                            EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, "면역!", Color.cyan);
                        }
                        else
                        {
                            int damage = 1 + DifficultyLevel;
                            StatManager.Instance.TakeDamage(damage);
                            EffectSystem.Instance.FlashScreen(new Color(0f, 0.4f, 1f, 0.35f), 0.25f);
                            EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);
                            string msg = LocalizationManager.Instance.GetFormatted("combat_water", damage);
                            EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, msg, Color.red);
                            if (StatManager.Instance.CurrentHP <= 0) return;
                        }
                    }
                }
                // 3. Event trigger
                else if (UnityEngine.Random.value < 0.08f)
                {
                    bool isTombstone = UnityEngine.Random.value < 0.3f;
                    if (isTombstone)
                        DeepEarth.Common.GameEvents.FireTombstoneOpened();
                    else
                        DeepEarth.Common.GameEvents.FireTreasureOpened();
                    await EventManager.Instance.TriggerRandomEventAsync(isTombstone);
                    return;
                }
            }

            if (StatManager.Instance.CurrentHP > 0 && CurrentState == GameState.Playing)
                await MiningSystem.Instance.SpawnNextBlockAsync();
        }

        // ── Route Map 노드 선택 진입점 ─────────────────────────────────────

        public void OnRouteNodeSelected(DeepEarth.Map.NodeData nodeData)
        {
            HandleRouteNodeAsync(nodeData).Forget();
        }

        private async UniTaskVoid HandleRouteNodeAsync(DeepEarth.Map.NodeData nodeData)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Run]\nGame Started\nNode : {nodeData}");
#endif
            CurrentState = GameState.Playing;
            AdvanceDepth();
            NodeEventManager.Instance?.DecrementFloorCounters();

            // Collapsed Tunnel: skip this node entirely
            if (NodeEventManager.Instance?.SkipNextNode == true)
            {
                NodeEventManager.Instance.SkipNextNode = false;
                if (StatManager.Instance.CurrentHP <= 0) return;
                if (_routeMapPresenter != null) await _routeMapPresenter.OnNonMineNodeCompleted();
                return;
            }

            // 그룹 A: 노드 입장 시점 트리거(고대열쇠 등) — 모든 노드 타입 공통
            RelicManager.Instance?.ApplyNodeArrivalEffects(nodeData.NodeType);

            switch (nodeData.NodeType)
            {
                case DeepEarth.Map.RoomType.Mine:
                    await MiningSystem.Instance.SpawnNextBlockAsync();
                    break;

                case DeepEarth.Map.RoomType.Monster:
                    MonsterType mType = CombatSystem.Instance.PickMonsterForDepth(CurrentDepth);
                    EventRevealType mReveal = MonsterTypeToReveal(mType);
                    await EventManager.Instance.PlayRevealAsync(mReveal, CombatSystem.Instance.GetMonsterDeathTriggerDescKey(mType));

                    EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.2f), 0.2f);
                    await CombatSystem.Instance.StartCombatAsync(mType, CurrentDepth);

                    if (StatManager.Instance.CurrentHP <= 0) return;
                    RelicManager.Instance?.ApplyNodeCompletionEffects(nodeData.NodeType);
                    await _routeMapPresenter.OnNonMineNodeCompleted();
                    break;

                case DeepEarth.Map.RoomType.Elite:
                    EffectSystem.Instance.FlashScreen(new Color(1f, 0.5f, 0f, 0.2f), 0.2f);
                    await EliteCombatSystem.Instance.StartEliteCombatAsync(CurrentDepth);

                    if (StatManager.Instance.CurrentHP <= 0) return;
                    RelicManager.Instance?.ApplyNodeCompletionEffects(nodeData.NodeType);
                    await _routeMapPresenter.OnNonMineNodeCompleted();
                    break;

                case DeepEarth.Map.RoomType.Treasure:
                    DeepEarth.Common.GameEvents.FireTreasureOpened();
                    await EventManager.Instance.TriggerRandomEventAsync(false);
                    RelicManager.Instance?.ApplyNodeCompletionEffects(nodeData.NodeType);
                    await _routeMapPresenter.OnNonMineNodeCompleted();
                    break;

                case DeepEarth.Map.RoomType.Grave:
                    DeepEarth.Common.GameEvents.FireTombstoneOpened();
                    await EventManager.Instance.TriggerRandomEventAsync(true);
                    RelicManager.Instance?.ApplyNodeCompletionEffects(nodeData.NodeType);
                    await _routeMapPresenter.OnNonMineNodeCompleted();
                    break;

                case DeepEarth.Map.RoomType.Event:
                    await NodeEventManager.Instance.TriggerEventAsync(CurrentDepth);
                    if (StatManager.Instance.CurrentHP <= 0) return;
                    RelicManager.Instance?.ApplyNodeCompletionEffects(nodeData.NodeType);
                    await _routeMapPresenter.OnNonMineNodeCompleted();
                    break;

                case DeepEarth.Map.RoomType.Merchant:
                    if (_merchantPresenter != null)
                        await _merchantPresenter.OpenAsync(CurrentDepth);
                    if (StatManager.Instance.CurrentHP <= 0) return;
                    RelicManager.Instance?.ApplyNodeCompletionEffects(nodeData.NodeType);
                    await _routeMapPresenter.OnNonMineNodeCompleted();
                    break;

                case DeepEarth.Map.RoomType.Rest:
                    await NodeEventManager.Instance.TriggerRestAsync(CurrentDepth);
                    if (StatManager.Instance.CurrentHP <= 0) return;
                    RelicManager.Instance?.ApplyNodeCompletionEffects(nodeData.NodeType);
                    await _routeMapPresenter.OnNonMineNodeCompleted();
                    break;

                case DeepEarth.Map.RoomType.Boss:
                    await EventManager.Instance.PlayRevealAsync(EventRevealType.Boss);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log("[Run]\nBoss Battle Started");
#endif
                    // 다른 노드 타입과 달리 여기서 await하지 않는다 — 보스는 전투 종료 후 보상 선택 UI까지
                    // 이어지는 긴 흐름이라, 완료 시점은 BossManager.StartBossSequenceAsync 내부에서
                    // 보상 선택이 끝난 뒤 OnBossSequenceComplete()를 호출하는 시점으로 위임된다.
                    BossManager.Instance.StartBossSequenceAsync(CurrentDepth).Forget();
                    break;
            }
        }

        // BossManager가 보상 화면 종료 후 호출 (기존 흐름 교체 지점)
        public void OnBossSequenceComplete()
        {
            // 3단계 구조(2026-08): 최종 보스(AllMetalColossus, 깊이150)를 처치했다면 다음 맵으로 넘어가지 않고
            // 런을 승리로 종료한다. 선형/RouteMap 모드 공통 진입점.
            if (CurrentDepth >= GameSettings.FinalBossDepth)
            {
                RunEnd(isVictory: true);
                return;
            }

            if (_routeMapPresenter != null)
            {
                _routeMapPresenter.OnBossNodeCompleted()
                    .ContinueWith(() =>
                    {
                        CurrentState = GameState.MapSelecting;
                        _routeMapPresenter.ShowMap();
                    }).Forget();
            }
            else
            {
                // 기존 선형 모드
                CurrentState = GameState.Playing;
                MiningSystem.Instance.SpawnNextBlockAsync().Forget();
            }
        }

        private void CheckPlayerDeath()
        {
            if (StatManager.Instance.CurrentHP <= 0 && CurrentState != GameState.GameOver && CurrentState != GameState.MainMenu)
            {
                // 유물: 불사의 심장 — 런 중 1회 부활
                if (RelicManager.Instance?.CheckAndConsumeRevive() ?? false) return;

                Debug.Log("[Run]\nPlayer Dead");
                DeepEarth.Common.GameEvents.FirePlayerDied();
                EndGame();
            }
        }

        public void RunEnd(bool isVictory = false)
        {
            Time.timeScale = 1f;
            Debug.Log(isVictory ? "[Run]\nRunEnd Start (Victory)" : "[Run]\nRunEnd Start");

            try
            {
                CurrentState = isVictory ? GameState.Victory : GameState.GameOver;

                // Step 1: Open Result Popup / Graceful Fallback
                bool popupSuccess = false;
                if (_gameOverObject != null && _gameOverPresenter != null)
                {
                    Debug.Log("[Run]\nResult Popup Open");
                    if (_hudObject != null)
                    {
                        _hudObject.SetActive(false);
                    }
                    _gameOverObject.SetActive(true);
                    _gameOverPresenter.UpdateResultsUI(isVictory);
                    popupSuccess = true;
                }
                else
                {
                    Debug.Log("[Run]\nResult Popup load failed - Skipping popup presentation");
                }

                // Step 2: Reward Calculate
                Debug.Log("[Run]\nReward Calculate");
                // Will reward formula: Depth/3 + Ore-to-Will conversion (GameBalanceData)
                int ironForWill = IronCount, silverForWill = SilverCount, goldForWill = GoldCount, diamondForWill = DiamondCount;
                int resourceValue = GameBalanceData.Instance.CalculateOreWillValue(ironForWill, silverForWill, goldForWill, diamondForWill);
                int depthBonus = CurrentDepth / 3;
                WillEarnedThisRun = depthBonus + resourceValue;

                // Update personal best depth
                if (CurrentDepth > SaveManager.CurrentData.BestDepth)
                {
                    SaveManager.CurrentData.BestDepth = CurrentDepth;
                }

                int willBeforeGrant = MetaProgressionManager.Instance.Will;
                MetaProgressionManager.Instance.AddWill(WillEarnedThisRun);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                var balance = GameBalanceData.Instance;
                System.Text.StringBuilder resultLog = new System.Text.StringBuilder();
                resultLog.AppendLine("[Run Result]");
                resultLog.AppendLine($"Iron {ironForWill}\n  Will +{ironForWill * balance.ironToWill}");
                resultLog.AppendLine($"Silver {silverForWill}\n  Will +{silverForWill * balance.silverToWill}");
                resultLog.AppendLine($"Gold {goldForWill}\n  Will +{goldForWill * balance.goldToWill}");
                resultLog.AppendLine($"Diamond {diamondForWill}\n  Will +{diamondForWill * balance.diamondToWill}");
                resultLog.AppendLine($"Total Will\n  +{WillEarnedThisRun}");
                resultLog.Append($"Current Will\n  {willBeforeGrant} -> {willBeforeGrant + WillEarnedThisRun}");
                Debug.Log(resultLog.ToString());
#endif

                // Step 3: Transfer Currency
                int runStone = 0, runWood = 0, runDirt = 0, runIron = 0, runSilver = 0, runGold = 0, runDiamond = 0;
                if (InventoryManager.Instance != null)
                {
                    runStone = InventoryManager.Instance.GetItemCount("Item_Stone");
                    runWood = InventoryManager.Instance.GetItemCount("Item_Wood");
                    runDirt = InventoryManager.Instance.GetItemCount("Item_Dirt");
                    runIron = InventoryManager.Instance.GetItemCount("Item_Iron");
                    runSilver = InventoryManager.Instance.GetItemCount("Item_Silver");
                    runGold = InventoryManager.Instance.GetItemCount("Item_Gold");
                    runDiamond = InventoryManager.Instance.GetItemCount("Item_Diamond");
                }
                int runWill = WillEarnedThisRun;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("[Run]\nTransfer Currency");
                if (runStone > 0) sb.AppendLine($"Stone +{runStone}");
                if (runWood > 0) sb.AppendLine($"Wood +{runWood}");
                if (runDirt > 0) sb.AppendLine($"Dirt +{runDirt}");
                if (runIron > 0) sb.AppendLine($"Iron +{runIron}");
                if (runSilver > 0) sb.AppendLine($"Silver +{runSilver}");
                if (runGold > 0) sb.AppendLine($"Gold +{runGold}");
                if (runDiamond > 0) sb.AppendLine($"Diamond +{runDiamond}");
                if (runWill > 0) sb.AppendLine($"Will +{runWill}");
                Debug.Log(sb.ToString().TrimEnd());

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.TransferRunRewardToMeta();
                    InventoryManager.Instance.ClearRunInventory();
                }

                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.ClearRunEffects();
                }

                StatusEffectManager.Instance?.ClearAll();
                RelicManager.Instance?.ClearAll();
                StartingRelicManager.Instance?.ClearAll();

                StatManager.Instance.ResetStatsForRun();

                OnGameDataChanged?.Invoke();

                // Step 4: Save
                SaveManager.Save();
                Debug.Log("[Save]\nSave Complete");

                // Step 5: 런 데이터 정리
                RunSetupContext.Reset();
                RunDataModel.Clear();

                // Step 6: Transition scene if popup was skipped (Fallback)
                if (!popupSuccess)
                {
                    try
                    {
                        Debug.Log("[Scene]\nLoad StartMenuScene");
                        UnityEngine.SceneManagement.SceneManager.LoadScene(DeepEarth.Common.SceneNames.StartMenu);
                    }
                    catch (Exception sceneEx)
                    {
                        Debug.LogError($"[Scene Error] Failed to load StartMenuScene: {sceneEx.Message}\n{sceneEx.StackTrace}");
                    }
                }

                Debug.Log("[Run]\nRunEnd Complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Run Error]\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        private void EndGame()
        {
            RunEnd();
        }

        public void RestartGame()
        {
            // 새 런은 반드시 RunSetupPanel → LoadingScene 경로를 거쳐야 한다.
            RunSetupContext.Reset();
            RunDataModel.Clear();
            DisposePresenters();
            Destroy(gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene(DeepEarth.Common.SceneNames.StartMenu);
        }

        public void AbandonRun()
        {
            Debug.Log($"[Run]\nPlayer Give Up\nDepth : {CurrentDepth}");

            InventoryManager.Instance?.ClearRunInventory();
            EffectManager.Instance?.ClearRunEffects();
            StatusEffectManager.Instance?.ClearAll();
            RelicManager.Instance?.ClearAll();
            StartingRelicManager.Instance?.ClearAll();
            StatManager.Instance?.ResetStatsForRun();

            RunSetupContext.Reset();
            RunDataModel.Clear();

            Debug.Log("[Run]\nRunData Cleared");

            SaveManager.Save();

            DisposePresenters();
            Destroy(gameObject);

            Time.timeScale = 1f;

            Debug.Log("[Scene]\nMove\nMainMenuScene");
            UnityEngine.SceneManagement.SceneManager.LoadScene(DeepEarth.Common.SceneNames.StartMenu);
        }

        // 신규 유저 초반 런 밸런싱: 깊이 RestCheckpointInterval마다(보스 깊이 제외) 확정 회복 지점.
        // 자원/HP 소모 없이 최대 체력의 일정 비율 회복 + 곡괭이 내구도 일부 무료 수리.
        private async UniTask TriggerRestCheckpointAsync()
        {
            await EventManager.Instance.PlayRevealAsync(EventRevealType.Rest);

            int healAmount = Mathf.RoundToInt(StatManager.Instance.GetMaxHP() * GameSettings.RestCheckpointHealRatio);
            StatManager.Instance.Heal(healAmount);
            PickaxeDurabilityManager.Instance?.Repair(GameSettings.RestCheckpointDurabilityGain);

            Debug.Log($"[Run]\nRest Checkpoint\nDepth : {CurrentDepth}\nHeal : +{healAmount}\nPickaxe Durability : +{GameSettings.RestCheckpointDurabilityGain}");

            EffectSystem.Instance.FlashScreen(new Color(0.3f, 1f, 0.5f, 0.25f), 0.3f);
            Vector3 pos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                : Vector3.zero;
            EffectSystem.Instance.SpawnDamageText(pos, $"+{healAmount} HP", Color.green);
        }

        private float GetMonsterSpawnChance(int depth)
        {
            if (depth < 50) return 0.10f;
            if (depth < 100) return 0.20f;
            if (depth < 200) return 0.35f;
            return 0.50f;
        }

        private static EventRevealType MonsterTypeToReveal(MonsterType type)
        {
            switch (type)
            {
                case MonsterType.CaveRat:
                case MonsterType.Mimic:
                    return EventRevealType.MonsterRat;
                case MonsterType.Slime:
                case MonsterType.SmallSlime:
                    return EventRevealType.MonsterSlime;
                default:
                    return EventRevealType.MonsterSpider;
            }
        }

        private float GetHazardSpawnChance(int depth)
        {
            if (depth < 50) return 0.05f;
            if (depth < 100) return 0.10f;
            if (depth < 200) return 0.20f;
            return 0.30f;
        }

        public void OpenSettings()
        {
            if (CurrentState == GameState.SettingsPause) return;

            _previousState = CurrentState;
            CurrentState = GameState.SettingsPause;

            if (_settingsObject != null)
            {
                _settingsObject.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            if (CurrentState != GameState.SettingsPause) return;

            if (_settingsObject != null)
            {
                _settingsObject.SetActive(false);
            }

            CurrentState = _previousState;
            OnGameDataChanged?.Invoke();
        }

        public void OpenRelicPopup()
        {
            if (CurrentState == GameState.SettingsPause) return;

            Debug.Log("[UI] RelicButton Clicked");
            _previousState = CurrentState;
            CurrentState = GameState.SettingsPause;

            Debug.Log("[UI] RelicPopup Open");
            _relicPopupPresenter?.Open();
        }

        public void CloseRelicPopup()
        {
            if (CurrentState != GameState.SettingsPause) return;

            CurrentState = _previousState;
            OnGameDataChanged?.Invoke();
        }

        public void OpenInventoryPopup()
        {
            if (CurrentState == GameState.SettingsPause) return;

            Debug.Log("[UI] InventoryButton Clicked");
            _previousState = CurrentState;
            CurrentState = GameState.SettingsPause;

            _inventoryPopupPresenter?.Open();
        }

        public void CloseInventoryPopup()
        {
            if (CurrentState != GameState.SettingsPause) return;

            CurrentState = _previousState;
            OnGameDataChanged?.Invoke();
        }

        private void SetRef(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
// Trigger rebuild to pick up new connection settings
