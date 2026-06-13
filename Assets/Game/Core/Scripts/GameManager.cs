using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Mining;
using DeepEarth.Combat;
using DeepEarth.Event;
using DeepEarth.UI;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        [Header("Game States")]
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        [Header("Run Data")]
        public int CurrentDepth { get; private set; } = 0;
        public int IronCount { get; private set; } = 0;
        public int SilverCount { get; private set; } = 0;
        public int GoldCount { get; private set; } = 0;
        public int DiamondCount { get; private set; } = 0;
        public int WillEarnedThisRun { get; private set; } = 0;

        // UI references
        private GameObject _hudObject;
        private GameObject _gameOverObject;
        private GameObject _eventObject;
        private GameObject _settingsObject;
        private GameObject _relicPopupObject;
        private GameObject _inventoryPopupObject;

        private GameUIPresenter _hudPresenter;
        private GameOverUIPresenter _gameOverPresenter;
        private EventUIPresenter _eventPresenter;
        private SettingsUIPresenter _settingsPresenter;
        private RelicPopupPresenter _relicPopupPresenter;
        private InventoryPresenter _inventoryPopupPresenter;

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

        private void Start()
        {
            // Register resource reward
            if (MiningSystem.Instance != null)
            {
                MiningSystem.Instance.OnResourceMined += HandleResourceMined;
            }

            // Register HP death hook
            StatManager.Instance.OnHPChanged += CheckPlayerDeath;
        }

        private void OnDestroy()
        {
            if (MiningSystem.Instance != null)
            {
                MiningSystem.Instance.OnResourceMined -= HandleResourceMined;
            }

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
        }

        public async UniTask InitializeUIAsync(Canvas canvas, Image flashOverlay, GameObject particlePrefab)
        {
            try
            {
                // Instantiate UI views via Addressables
                _hudObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelHUD, canvas.transform);
                _gameOverObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelGameOver, canvas.transform);
                _eventObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelEvent, canvas.transform);
                _settingsObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelSettings, canvas.transform);
                
                _relicPopupObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelRelicPopup, canvas.transform);
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

                if (_hudObject == null || _gameOverObject == null || _eventObject == null || _settingsObject == null)
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

                // Setup EffectSystem
                EffectSystem.Instance.Initialize(Camera.main, canvas, flashOverlay, particlePrefab);


                // Setup Presenters
                _hudPresenter = new GameUIPresenter(hudView, this);
                _gameOverPresenter = new GameOverUIPresenter(gameOverView, this);
                _eventPresenter = new EventUIPresenter(eventView);
                _settingsPresenter = new SettingsUIPresenter(settingsView, this);
                _relicPopupPresenter = new RelicPopupPresenter(relicPopupView, this);
                _inventoryPopupPresenter = new InventoryPresenter(inventoryPopupView, this);

                // Initially hide panels and show Main HUD
                _hudObject.SetActive(true);
                _gameOverObject.SetActive(false);
                _eventObject.SetActive(false);
                _settingsObject.SetActive(false);
                _relicPopupObject.SetActive(false);
                _inventoryPopupObject.SetActive(false);

                StartGame();
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameManager: Critical exception during InitializeUIAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void StartGame()
        {
            CurrentDepth = 0;
            IronCount = 0;
            SilverCount = 0;
            GoldCount = 0;
            DiamondCount = 0;
            WillEarnedThisRun = 0;

            StatManager.Instance.ResetStatsForRun();
            CurrentState = GameState.Playing;

            _hudObject.SetActive(true);
            _gameOverObject.SetActive(false);

            // Reset Map system
            if (DeepEarth.Map.MapGenerator.Instance != null)
            {
                DeepEarth.Map.MapGenerator.Instance.ResetGenerator();
            }
            if (DeepEarth.Map.MapPresenter.Instance != null && DeepEarth.Map.MapPresenter.Instance.Model != null)
            {
                DeepEarth.Map.MapPresenter.Instance.Model.CurrentDepth = 0;
            }

            OnGameDataChanged?.Invoke();

            // Spawn first block
            MiningSystem.Instance.SpawnNextBlockAsync().Forget();
        }

        public void PauseForEvent()
        {
            CurrentState = GameState.EventPause;
        }

        public void ResumeAfterEvent()
        {
            CurrentState = GameState.Playing;
            OnGameDataChanged?.Invoke();
            // Continue mining
            MiningSystem.Instance.SpawnNextBlockAsync().Forget();
        }

        private void HandleResourceMined(BlockType type, int amount)
        {
            // Apply Boss run-local resource multiplier (e.g. +20% or +50%)
            float bossMult = 1.0f + StatManager.Instance.BossResourceModifier;
            amount = Mathf.RoundToInt(amount * bossMult);
            if (amount < 1) amount = 1;

            // Apply Grave Robber passive: 10% chance for +10% extra yield (minimum +1)
            var selectedChar = CharacterManager.Instance.SelectedCharacterID;
            if (CharacterManager.Instance.HasGraveRobberPassive(selectedChar))
            {
                if (UnityEngine.Random.value < 0.1f)
                {
                    int extra = Mathf.Max(1, Mathf.RoundToInt(amount * 0.1f));
                    amount += extra;
                    Debug.Log($"Grave Robber Passive Triggered: +{extra} {type} added!");
                }
            }

            // Check Inventory Capacity limit
            int currentTotal = IronCount + SilverCount + GoldCount + DiamondCount;
            int maxCap = StatManager.Instance.GetInventorySize();

            if (currentTotal + amount > maxCap)
            {
                EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, LocalizationManager.Instance.GetTranslation("hud_inv_full"), Color.red);
                return;
            }

            switch (type)
            {
                case BlockType.Iron: IronCount += amount; break;
                case BlockType.Silver: SilverCount += amount; break;
                case BlockType.Gold: GoldCount += amount; break;
                case BlockType.Diamond: DiamondCount += amount; break;
            }

            OnGameDataChanged?.Invoke();
        }

        public async UniTaskVoid OnBlockMined()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentDepth++;
            OnGameDataChanged?.Invoke();

            // Trigger MapView slide transition and MapGenerator wall updates
            if (DeepEarth.Map.MapPresenter.Instance != null)
            {
                await DeepEarth.Map.MapPresenter.Instance.HandleBlockMinedAsync(CurrentDepth);
            }

            // Check Boss trigger at Depth 50, 100, 150, 200, 250, and every 50 Depth thereafter
            if (CurrentDepth > 0 && CurrentDepth % 50 == 0)
            {
                BossManager.Instance.StartBossSequenceAsync(CurrentDepth).Forget();
                return;
            }

            // 1. Check Combat trigger
            float monsterChance = GetMonsterSpawnChance(CurrentDepth) * StatManager.Instance.GetMonsterSpawnRateMultiplier();
            if (UnityEngine.Random.value < monsterChance)
            {
                // Trigger combat
                MonsterType mType = (UnityEngine.Random.value < 0.6f) ? MonsterType.CaveRat : MonsterType.CaveSpider;
                
                EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.2f), 0.2f);
                EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, LocalizationManager.Instance.GetTranslation("combat_monster_encounter"), Color.red);
                
                await CombatSystem.Instance.StartCombatAsync(mType, CurrentDepth);

                if (StatManager.Instance.CurrentHP <= 0) return; // Player died in combat
            }
            // 2. Check Hazard (water/lava) trigger
            else
            {
                float hazardChance = GetHazardSpawnChance(CurrentDepth) * StatManager.Instance.GetHazardSpawnRateMultiplier();
                if (UnityEngine.Random.value < hazardChance)
                {
                    int damage = 1 + DifficultyLevel;
                    bool isLava = UnityEngine.Random.value < 0.5f;
                    Color flashColor = isLava ? new Color(1f, 0.4f, 0f, 0.35f) : new Color(0f, 0.4f, 1f, 0.35f);

                    StatManager.Instance.TakeDamage(damage);
                    
                    EffectSystem.Instance.FlashScreen(flashColor, 0.25f);
                    EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);
                    string msg = LocalizationManager.Instance.GetFormatted(isLava ? "combat_lava" : "combat_water", damage);
                    EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, msg, Color.red);

                    if (StatManager.Instance.CurrentHP <= 0) return; // Player died to hazard
                }
                // 3. Check Event trigger (Chest / Tombstone)
                else if (UnityEngine.Random.value < 0.08f)
                {
                    bool isTombstone = UnityEngine.Random.value < 0.3f; // 30% tombstone, 70% treasure box
                    await EventManager.Instance.TriggerRandomEventAsync(isTombstone);
                    return; // EventManager triggers next block after completion, so exit here
                }
            }

            // Spawn next block if still alive
            if (StatManager.Instance.CurrentHP > 0 && CurrentState == GameState.Playing)
            {
                await MiningSystem.Instance.SpawnNextBlockAsync();
            }
        }

        private void CheckPlayerDeath()
        {
            if (StatManager.Instance.CurrentHP <= 0 && CurrentState == GameState.Playing)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            CurrentState = GameState.GameOver;

            // Will reward formula: Depth/5 + Resources value/2
            int resourceValue = (IronCount * 1) + (SilverCount * 2) + (GoldCount * 3) + (DiamondCount * 5);
            WillEarnedThisRun = (CurrentDepth / 5) + (resourceValue / 2);

            // Accumulate collected resources in persistent save data
            SaveManager.CurrentData.PersistentIron += IronCount;
            SaveManager.CurrentData.PersistentSilver += SilverCount;
            SaveManager.CurrentData.PersistentGold += GoldCount;
            SaveManager.CurrentData.PersistentDiamond += DiamondCount;

            // Update personal best depth
            if (CurrentDepth > SaveManager.CurrentData.BestDepth)
            {
                SaveManager.CurrentData.BestDepth = CurrentDepth;
            }

            MetaProgressionManager.Instance.AddWill(WillEarnedThisRun);

            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.ClearRunEffects();
            }

            _hudObject.SetActive(false);
            _gameOverObject.SetActive(true);

            OnGameDataChanged?.Invoke();
            Debug.Log("Game Over!");
        }

        public void RestartGame()
        {
            DisposePresenters();
            
            // Re-instantiate/Re-bind UI Presenters
            if (_hudObject != null && _gameOverObject != null && _eventObject != null && _settingsObject != null && _relicPopupObject != null && _inventoryPopupObject != null)
            {
                var hudView = _hudObject.GetComponent<GameUIView>();
                var gameOverView = _gameOverObject.GetComponent<GameOverUIView>();
                var eventView = _eventObject.GetComponent<EventUIView>();
                var settingsView = _settingsObject.GetComponent<SettingsUIView>();
                var relicPopupView = _relicPopupObject.GetComponent<RelicPopupView>();
                var inventoryPopupView = _inventoryPopupObject.GetComponent<InventoryPopupView>();

                _hudPresenter = new GameUIPresenter(hudView, this);
                _gameOverPresenter = new GameOverUIPresenter(gameOverView, this);
                _eventPresenter = new EventUIPresenter(eventView);
                _settingsPresenter = new SettingsUIPresenter(settingsView, this);
                _relicPopupPresenter = new RelicPopupPresenter(relicPopupView, this);
                _inventoryPopupPresenter = new InventoryPresenter(inventoryPopupView, this);
            }
            
            StartGame();
        }

        private float GetMonsterSpawnChance(int depth)
        {
            if (depth < 50) return 0.10f;
            if (depth < 100) return 0.20f;
            if (depth < 200) return 0.35f;
            return 0.50f;
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
