using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [Serializable]
    public class DepthRarityEntry
    {
        public int minDepth;
        [Range(0f, 1f)] public float commonChance;
        [Range(0f, 1f)] public float rareChance;
        [Range(0f, 1f)] public float legendaryChance;
    }

    [CreateAssetMenu(fileName = "MerchantBalanceData", menuName = "DeepEarth/MerchantBalanceData")]
    public class MerchantBalanceData : ScriptableObject
    {
        [Header("Slot Counts")]
        public int relicSlotCount = 4;
        public int consumableSlotCount = 2;

        [Header("Relic Rarity By Depth")]
        public List<DepthRarityEntry> relicRarityByDepth = new List<DepthRarityEntry>
        {
            new DepthRarityEntry { minDepth = 0,   commonChance = 0.75f, rareChance = 0.24f, legendaryChance = 0.01f },
            new DepthRarityEntry { minDepth = 50,  commonChance = 0.65f, rareChance = 0.30f, legendaryChance = 0.05f },
            new DepthRarityEntry { minDepth = 100, commonChance = 0.55f, rareChance = 0.38f, legendaryChance = 0.07f },
            new DepthRarityEntry { minDepth = 150, commonChance = 0.45f, rareChance = 0.45f, legendaryChance = 0.10f },
        };

        [Header("Consumable Rarity By Depth")]
        public List<DepthRarityEntry> consumableRarityByDepth = new List<DepthRarityEntry>
        {
            new DepthRarityEntry { minDepth = 0,   commonChance = 0.95f, rareChance = 0.05f, legendaryChance = 0.00f },
            new DepthRarityEntry { minDepth = 50,  commonChance = 0.90f, rareChance = 0.10f, legendaryChance = 0.00f },
            new DepthRarityEntry { minDepth = 100, commonChance = 0.88f, rareChance = 0.11f, legendaryChance = 0.01f },
            new DepthRarityEntry { minDepth = 150, commonChance = 0.80f, rareChance = 0.18f, legendaryChance = 0.02f },
        };

        [Header("Relic Prices (Run Currency 개수)")]
        public int relicPriceCommon = 3;    // Silver
        public int relicPriceRare = 3;      // Gold
        public int relicPriceLegendary = 3; // Diamond

        [Header("Consumable Prices (Run Currency 개수)")]
        public int consumablePriceCommon = 2;    // Silver
        public int consumablePriceRare = 2;      // Gold
        public int consumablePriceLegendary = 2; // Diamond

        [Header("Potion Prices (Root / Item_Wood 개수)")]
        public int potionSmallRootPrice = 8;
        public int potionMediumRootPrice = 15;
        public int potionLargeRootPrice = 30;

        [Header("Discount Event")]
        [Range(0f, 1f)] public float discountEventChance = 0.05f;
        [Range(0f, 1f)] public float discountAllPercent = 0.20f;
        [Range(0f, 1f)] public float discountRareRelicPercent = 0.50f;

        [Header("Featured Slot")]
        [Range(0f, 1f)] public float featuredSlotDiscountMin = 0.20f;
        [Range(0f, 1f)] public float featuredSlotDiscountMax = 0.30f;
        [Range(0f, 1f)] public float featuredLegendaryChance = 0.03f;

        public DepthRarityEntry GetRelicRarityForDepth(int depth) => GetEntryForDepth(relicRarityByDepth, depth);
        public DepthRarityEntry GetConsumableRarityForDepth(int depth) => GetEntryForDepth(consumableRarityByDepth, depth);

        private static DepthRarityEntry GetEntryForDepth(List<DepthRarityEntry> entries, int depth)
        {
            DepthRarityEntry best = null;
            foreach (var e in entries)
            {
                if (depth >= e.minDepth && (best == null || e.minDepth > best.minDepth))
                    best = e;
            }
            return best;
        }

        private static MerchantBalanceData _instance;
        public static MerchantBalanceData Instance => _instance != null ? _instance : (_instance = CreateInstance<MerchantBalanceData>());

        public static async UniTask LoadAsync()
        {
            if (_instance != null) return;

            _instance = await ResourceManager.Instance.LoadAssetAsync<MerchantBalanceData>(AddressableKeys.MerchantBalanceDataKey);
            if (_instance == null)
            {
                Debug.LogWarning("[Merchant]\nMerchantBalanceData not found. Using runtime defaults.");
                _instance = CreateInstance<MerchantBalanceData>();
            }
        }
    }
}
