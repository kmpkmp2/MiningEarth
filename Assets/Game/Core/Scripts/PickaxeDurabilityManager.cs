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

        public int CurrentDurability => _model?.CurrentDurability ?? 0;
        public int MaxDurability => _model?.MaxDurability ?? 0;
        public bool IsBroken => _model?.IsBroken ?? false;
        public bool BrokenAlertShown => _brokenAlertShown;
        public float CurrentPickaxeEfficiency => _currentPickaxeData?.repairEfficiency ?? 1f;

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
            _currentPickaxeData = await ResourceManager.Instance.LoadAssetAsync<PickaxeData>(AddressableKeys.PickaxeDefault);
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

        public void InitializeForRun(int upgradeBonus = 0, int relicBonus = 0, int eventBonus = 0)
        {
            if (_currentPickaxeData == null)
            {
                Debug.LogWarning("[Pickaxe] InitializeForRun called before InitializeAsync.");
                return;
            }

            ClearModel();

            int maxDurability = _currentPickaxeData.baseMaxDurability + upgradeBonus + relicBonus + eventBonus;
            _model = new PickaxeDurabilityModel(_currentPickaxeData, maxDurability);
            _model.OnDurabilityChanged += HandleDurabilityChanged;
            _model.OnPickaxeBroken += HandlePickaxeBroken;
            _model.OnPickaxeRepaired += HandlePickaxeRepaired;
            _brokenAlertShown = false;

            OnDurabilityChanged?.Invoke();

            Debug.Log($"[Pickaxe]\nRun Initialized\nMax Durability : {maxDurability}\nUpgrade Bonus : {upgradeBonus}");
        }

        public void ClearForRun()
        {
            ClearModel();
            _brokenAlertShown = false;
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
            else
            {
                int loss = _configData.GetDurabilityLoss(type);
                if (loss > 0)
                {
                    _model.LoseDurability(loss);
                    Debug.Log($"[Pickaxe]\nDurability Loss\nOre : {type}\nLoss : {loss}\nCurrent : {_model.CurrentDurability} / {_model.MaxDurability}");
                }
            }
        }

        public void Repair(int gain)
        {
            if (_model == null || gain <= 0) return;
            _model.Repair(gain);
        }

        public RepairRecipe GetRepairRecipe(string itemID) => _configData?.GetRepairRecipe(itemID);

        public bool HasRepairRecipe(string itemID) => _configData?.GetRepairRecipe(itemID) != null;

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
        }

        private void ClearModel()
        {
            if (_model != null)
            {
                _model.OnDurabilityChanged -= HandleDurabilityChanged;
                _model.OnPickaxeBroken -= HandlePickaxeBroken;
                _model.OnPickaxeRepaired -= HandlePickaxeRepaired;
                _model = null;
            }
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
