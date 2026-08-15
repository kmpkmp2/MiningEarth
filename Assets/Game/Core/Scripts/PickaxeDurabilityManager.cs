using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.UI;

namespace DeepEarth.Core
{
    public class PickaxeDurabilityManager : MonoBehaviour
    {
        private static PickaxeDurabilityManager _instance;
        public static PickaxeDurabilityManager Instance => _instance;

        private PickaxeData _currentPickaxeData;
        private PickaxeConfigData _configData;
        private PickaxeDurabilityModel _model;
        private PickaxeDurabilityPresenter _presenter;
        private bool _brokenAlertShown;

        public event Action OnDurabilityChanged;
        public event Action OnPickaxeBroken;
        public event Action OnPickaxeRepaired;
        public event Action OnDurabilityWarning;
        public event Action OnDurabilityWarningCleared;

        public int CurrentDurability => _model?.CurrentDurability ?? 0;
        public int MaxDurability => _model?.MaxDurability ?? 0;
        public bool IsBroken => _model?.IsBroken ?? false;
        public bool IsWarning => _model?.IsWarning ?? false;
        public bool BrokenAlertShown => _brokenAlertShown;
        public float CurrentPickaxeEfficiency => _currentPickaxeData?.repairEfficiency ?? 1f;

        // 런 스코프 응급 수리 잔여 횟수. 자원 없이도 HP를 태워 즉시 내구도를 회복할 수 있는 안전판.
        public int EmergencyRepairUsesRemaining { get; private set; }

        public enum EmergencyRepairResult { Success, CombatBlocked, AlreadyFull, NoUsesLeft, NotEnoughHp }

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
            // Prefer PickaxeManager's equipped pickaxe (already initialized before this)
            if (PickaxeManager.Instance?.EquippedPickaxeData != null)
            {
                _currentPickaxeData = PickaxeManager.Instance.EquippedPickaxeData;
            }
            else
            {
                _currentPickaxeData = await ResourceManager.Instance.LoadAssetAsync<PickaxeData>(AddressableKeys.PickaxeDefault);
            }

            if (_currentPickaxeData == null)
            {
                Debug.LogWarning("[Pickaxe] PickaxeData not found. Using runtime defaults.");
                _currentPickaxeData = ScriptableObject.CreateInstance<PickaxeData>();
                _currentPickaxeData.pickaxeID = "DefaultPickaxe";
                _currentPickaxeData.baseMaxDurability = 50;
                _currentPickaxeData.miningPower = 1;
                _currentPickaxeData.repairEfficiency = 1f;
            }

            _configData = await ResourceManager.Instance.LoadAssetAsync<PickaxeConfigData>(AddressableKeys.PickaxeConfig);
            if (_configData == null)
            {
                Debug.LogWarning("[Pickaxe] PickaxeConfigData not found. Using runtime defaults.");
                _configData = BuildDefaultConfig();
            }

            Debug.Log($"[Pickaxe]\nInitialized\nPickaxe : {_currentPickaxeData.pickaxeID}\nBase Durability : {_currentPickaxeData.baseMaxDurability}");
        }

        public void InitializeForRun()
        {
            // Sync equipped pickaxe from PickaxeManager
            if (PickaxeManager.Instance?.EquippedPickaxeData != null)
                _currentPickaxeData = PickaxeManager.Instance.EquippedPickaxeData;

            if (_currentPickaxeData == null)
            {
                Debug.LogWarning("[Pickaxe] InitializeForRun called before InitializeAsync.");
                return;
            }

            ClearModel();

            int maxDurability = PickaxeManager.Instance != null
                ? PickaxeManager.Instance.GetFinalMaxDurability(_currentPickaxeData)
                : CalculateFallbackMaxDurability(_currentPickaxeData);

            _model = new PickaxeDurabilityModel(_currentPickaxeData, maxDurability);
            _model.OnDurabilityChanged += HandleDurabilityChanged;
            _model.OnPickaxeBroken += HandlePickaxeBroken;
            _model.OnPickaxeRepaired += HandlePickaxeRepaired;
            _model.OnDurabilityWarning += HandleDurabilityWarning;
            _model.OnDurabilityWarningCleared += HandleDurabilityWarningCleared;
            _brokenAlertShown = false;
            EmergencyRepairUsesRemaining = GameSettings.EmergencyRepairMaxUsesPerRun;

            OnDurabilityChanged?.Invoke();

            string pickaxeName = LocalizationManager.Instance.GetTranslation(_currentPickaxeData.nameLocKey);
            Debug.Log($"[Pickaxe]\nRun Start\nPickaxe : {pickaxeName}\nDurability : {maxDurability}/{maxDurability}");
        }

        public void ClearForRun()
        {
            ClearModel();
            _brokenAlertShown = false;
        }

        public void OnOreHit(BlockType type, int depth)
        {
            if (_model == null || _configData == null || _model.IsBroken) return;
            // 유물: 내구도 감소 없음 (InfiniteWhetstone 등)
            if (RelicManager.Instance?.HasPickaxeNoDurabilityLoss() ?? false) return;

            int loss = _configData.GetPerHitDurabilityLoss(type, depth);
            if (loss <= 0) return;

            // 유물: 내구도 감소율 배율 (OilBottle, LeatherHandle 등)
            float rateModifier = RelicManager.Instance?.GetPickaxeDurabilityRateModifier() ?? 1.0f;

            // 신규 패시브: Blacksmith — 내구도 감소량 감소 + 시작 유물(망치) 추가 감소
            var charID = CharacterManager.Instance.SelectedCharacterID;
            float passiveReduction = CharacterManager.Instance.GetPassivePickaxeDurabilityReduction(charID)
                                    + (StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetPickaxeDurabilityReduction() : 0f);
            rateModifier *= Mathf.Max(0f, 1f - passiveReduction);

            if (rateModifier != 1.0f)
                loss = Mathf.Max(1, Mathf.RoundToInt(loss * rateModifier));

            _model.LoseDurability(loss);
            Debug.Log($"[Pickaxe]\nHit Durability Loss\nOre : {type}\nDepth : {depth}\nLoss : {loss}\nCurrent : {_model.CurrentDurability}/{_model.MaxDurability}");
        }

        public void OnOreDestroyed(BlockType type)
        {
            if (_model == null || _configData == null) return;

            if (_model.IsBroken)
            {
                int damage = _configData.GetBrokenDamage(type);
                if (damage > 0)
                {
                    StatManager.Instance.TakeDamage(damage);
                    EffectSystem.Instance?.FlashScreen(new Color(1f, 0.3f, 0f, 0.2f), 0.15f);
                    Debug.Log($"[Pickaxe]\nBroken Mining Damage\nOre : {type}\nHP Loss : {damage}\nCurrent HP : {StatManager.Instance.CurrentHP}");
                }
            }
        }

        // 채굴 화면 HUD 전용 안전판: 자원이 없어도 HP를 태워 내구도를 응급 복구한다.
        // 전투 중에는 사용 불가, 이번 런 최대 사용 횟수 제한, 이 소모로는 HP가 1 미만으로 내려가지 않는다.
        public EmergencyRepairResult TryEmergencyRepair()
        {
            if (_model == null) return EmergencyRepairResult.AlreadyFull;
            if (DeepEarth.Combat.CombatSystem.Instance != null && DeepEarth.Combat.CombatSystem.Instance.IsCombatActive)
                return EmergencyRepairResult.CombatBlocked;
            if (CurrentDurability >= MaxDurability)
                return EmergencyRepairResult.AlreadyFull;
            if (EmergencyRepairUsesRemaining <= 0)
                return EmergencyRepairResult.NoUsesLeft;
            if (StatManager.Instance.CurrentHP <= GameSettings.EmergencyRepairHpCost)
                return EmergencyRepairResult.NotEnoughHp;

            StatManager.Instance.TakeDamage(GameSettings.EmergencyRepairHpCost);
            Repair(GameSettings.EmergencyRepairDurabilityGain);
            EmergencyRepairUsesRemaining--;

            Debug.Log($"[Pickaxe]\nEmergency Repair Used\nHP Cost : {GameSettings.EmergencyRepairHpCost}\nDurability Gain : {GameSettings.EmergencyRepairDurabilityGain}\nUses Left : {EmergencyRepairUsesRemaining}");
            return EmergencyRepairResult.Success;
        }

        // 채굴(OnOreHit)이 아닌 상황(예: 전투 후퇴)에서 곡괭이 내구도를 직접 소모시킬 때 사용.
        public void ApplyDirectDurabilityLoss(int amount)
        {
            if (_model == null || amount <= 0) return;
            _model.LoseDurability(amount);
        }

        public void Repair(int gain)
        {
            if (_model == null || gain <= 0) return;
            float bonus = RelicManager.Instance?.GetRepairEfficiencyBonus() ?? 0f;
            int finalGain = Mathf.RoundToInt(gain * (1f + bonus));
            _model.Repair(finalGain);
        }

        public RepairRecipe GetRepairRecipe(string itemID) => _configData?.GetRepairRecipe(itemID);

        public bool HasRepairRecipe(string itemID) => _configData?.GetRepairRecipe(itemID) != null;

        public void ApplyRelicDurabilityModifier(int delta)
        {
            if (_model == null || delta >= 0) return;
            _model.LoseDurability(-delta);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Pickaxe]\nRelic Durability Penalty\nDelta : {delta}\nCurrent : {_model.CurrentDurability}/{_model.MaxDurability}");
#endif
        }

        // 유물: 최대 내구도 증가 (ReinforcedHandle 등)
        public void AddMaxDurabilityBonus(int amount)
        {
            if (_model == null || amount <= 0) return;
            _model.AddMaxDurability(amount);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Pickaxe]\nRelic MaxDurability Bonus\n+{amount}\nMax : {_model.MaxDurability}");
#endif
        }

        // 유물: 처치 시 내구도 회복 (AutoRepairKit 등)
        public void RepairOnKill(int amount)
        {
            if (_model == null || amount <= 0) return;
            _model.Repair(amount);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Pickaxe]\nRelic Repair On Kill\n+{amount}\nCurrent : {_model.CurrentDurability}/{_model.MaxDurability}");
#endif
        }

        public void SetCurrentPickaxe(PickaxeData data)
        {
            if (data == null) return;
            _currentPickaxeData = data;
            Debug.Log($"[Pickaxe]\nPickaxe Changed\n{data.pickaxeID}");
        }

        public void SetupPresenter(PickaxeDurabilityView view)
        {
            _presenter?.Dispose();
            _presenter = new PickaxeDurabilityPresenter(view);
        }

        private void HandleDurabilityChanged()
        {
            OnDurabilityChanged?.Invoke();
        }

        private void HandlePickaxeBroken()
        {
            Debug.Log($"[Pickaxe]\nBroken\nCurrent : {_model?.CurrentDurability ?? 0}");
            if (!_brokenAlertShown)
            {
                _brokenAlertShown = true;
                OnPickaxeBroken?.Invoke();
            }
        }

        private void HandlePickaxeRepaired()
        {
            OnPickaxeRepaired?.Invoke();
            DeepEarth.Common.GameEvents.FirePickaxeRepaired();
            RelicManager.Instance?.ApplyPickaxeRepairedEffects();
        }

        private void HandleDurabilityWarning()
        {
            Debug.Log($"[Pickaxe]\nDurability Warning\nCurrent : {_model?.CurrentDurability ?? 0}/{_model?.MaxDurability ?? 0}");
            OnDurabilityWarning?.Invoke();
        }

        private void HandleDurabilityWarningCleared()
        {
            OnDurabilityWarningCleared?.Invoke();
        }

        private void ClearModel()
        {
            if (_model != null)
            {
                _model.OnDurabilityChanged -= HandleDurabilityChanged;
                _model.OnPickaxeBroken -= HandlePickaxeBroken;
                _model.OnPickaxeRepaired -= HandlePickaxeRepaired;
                _model.OnDurabilityWarning -= HandleDurabilityWarning;
                _model.OnDurabilityWarningCleared -= HandleDurabilityWarningCleared;
                _model = null;
            }
        }

        private static int CalculateFallbackMaxDurability(PickaxeData data)
        {
            if (data == null) return 50;

            int level = MetaProgressionManager.Instance?.PickaxeDurabilityLevel ?? 0;
            float multiplier = 1.0f + level * 0.1f;
            return Mathf.Max(1, Mathf.RoundToInt(data.baseMaxDurability * multiplier));
        }

        private static PickaxeConfigData BuildDefaultConfig()
        {
            var config = ScriptableObject.CreateInstance<PickaxeConfigData>();
            config.oreEntries = new System.Collections.Generic.List<OrePickaxeEntry>
            {
                new OrePickaxeEntry { blockType = BlockType.Dirt,    durabilityLoss = 0, brokenDamage = 0 },
                new OrePickaxeEntry { blockType = BlockType.Root,    durabilityLoss = 1, brokenDamage = 1 },
                new OrePickaxeEntry { blockType = BlockType.Stone,   durabilityLoss = 1, brokenDamage = 1 },
                new OrePickaxeEntry { blockType = BlockType.Iron,    durabilityLoss = 1, brokenDamage = 1 },
                new OrePickaxeEntry { blockType = BlockType.Silver,  durabilityLoss = 2, brokenDamage = 2 },
                new OrePickaxeEntry { blockType = BlockType.Gold,    durabilityLoss = 2, brokenDamage = 2 },
                new OrePickaxeEntry { blockType = BlockType.Diamond, durabilityLoss = 3, brokenDamage = 3 },
            };
            config.repairRecipes = new System.Collections.Generic.List<RepairRecipe>
            {
                new RepairRecipe { itemID = "Item_Stone",   itemNameLocKey = "item_stone_name",   itemCostPerUse = 10, durabilityGain = 5  },
                new RepairRecipe { itemID = "Item_Iron",    itemNameLocKey = "item_iron_name",    itemCostPerUse = 5,  durabilityGain = 10 },
                new RepairRecipe { itemID = "Item_Silver",  itemNameLocKey = "item_silver_name",  itemCostPerUse = 3,  durabilityGain = 15 },
                new RepairRecipe { itemID = "Item_Gold",    itemNameLocKey = "item_gold_name",    itemCostPerUse = 2,  durabilityGain = 25 },
                new RepairRecipe { itemID = "Item_Diamond", itemNameLocKey = "item_diamond_name", itemCostPerUse = 1,  durabilityGain = 40 },
            };
            return config;
        }
    }
}
