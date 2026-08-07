using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepEarth.UI;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    // 떠돌이 상점 상품 생성 로직 — 노드 진입마다 새 MerchantInventoryModel을 만든다.
    // SaveData에 저장되지 않는 휘발성 데이터(Context: Event/Grave/Treasure와 동일 패턴).
    public class MerchantModel
    {
        private readonly MerchantBalanceData _balance;
        private readonly MerchantQuoteData _quotes;

        public MerchantModel(MerchantBalanceData balance, MerchantQuoteData quotes)
        {
            _balance = balance;
            _quotes = quotes;
        }

        public MerchantInventoryModel GenerateShop(int depth)
        {
            var result = new MerchantInventoryModel();

            RollDiscountEvent(result);
            GenerateRelicSlots(result, depth);
            GenerateConsumableSlots(result, depth);
            GeneratePotionSlots(result);
            GenerateFeaturedSlot(result, depth);
            result.MerchantQuoteKey = _quotes != null ? _quotes.GetRandomQuoteKey() : string.Empty;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            LogGeneratedShop(result);
#endif
            return result;
        }

        private void RollDiscountEvent(MerchantInventoryModel result)
        {
            result.HasDiscountEvent = Random.value < _balance.discountEventChance;
            result.IsAllDiscount = result.HasDiscountEvent && Random.value < 0.5f;
        }

        private void GenerateRelicSlots(MerchantInventoryModel result, int depth)
        {
            var pool = RelicManager.Instance?.GetAvailable() ?? new List<RelicData>();
            var used = new HashSet<string>();
            var rarityTable = _balance.GetRelicRarityForDepth(depth);

            for (int i = 0; i < _balance.relicSlotCount; i++)
            {
                var relic = PickRelic(pool, used, rarityTable);
                if (relic == null) break;
                used.Add(relic.relicID);

                var slot = new MerchantSlotData { kind = MerchantSlotKind.Relic, relic = relic };
                ApplyRelicPrice(slot, result);
                result.RelicSlots.Add(slot);
            }
        }

        private void GenerateConsumableSlots(MerchantInventoryModel result, int depth)
        {
            var pool = (InventoryManager.Instance?.GetTemplatesByType(ItemType.Consumable) ?? System.Array.Empty<ItemData>())
                .Where(it => it.rarity != ItemRarity.Epic)
                .Where(it => !(it.autoUseOnDeath && (InventoryManager.Instance?.GetItemCount(it.itemID) ?? 0) > 0))
                .ToList();

            var used = new HashSet<string>();
            var rarityTable = _balance.GetConsumableRarityForDepth(depth);

            for (int i = 0; i < _balance.consumableSlotCount; i++)
            {
                var item = PickConsumable(pool, used, rarityTable);
                if (item == null) break;
                used.Add(item.itemID);

                var slot = new MerchantSlotData { kind = MerchantSlotKind.Consumable, item = item };
                ApplyConsumablePrice(slot, result);
                result.ConsumableSlots.Add(slot);
            }
        }

        private void GeneratePotionSlots(MerchantInventoryModel result)
        {
            AddPotionSlot(result, AddressableKeys.ItemPotion, _balance.potionSmallRootPrice);
            AddPotionSlot(result, AddressableKeys.ItemPotionMedium, _balance.potionMediumRootPrice);
            AddPotionSlot(result, AddressableKeys.ItemPotionLarge, _balance.potionLargeRootPrice);
        }

        private void AddPotionSlot(MerchantInventoryModel result, string itemId, int rootPrice)
        {
            var item = InventoryManager.Instance?.GetTemplate(itemId);
            if (item == null) return;

            // 그룹 H: 포션 가격 할인 유물
            float priceReduction = RelicManager.Instance?.GetPotionPriceReduction() ?? 0f;
            int finalPrice = Mathf.Max(1, Mathf.RoundToInt(rootPrice * (1f - Mathf.Clamp01(priceReduction))));

            result.PotionSlots.Add(new MerchantSlotData
            {
                kind = MerchantSlotKind.Potion,
                item = item,
                priceCurrencyItemId = AddressableKeys.ItemWood,
                basePrice = rootPrice,
                finalPrice = finalPrice,
            });
        }

        private void GenerateFeaturedSlot(MerchantInventoryModel result, int depth)
        {
            var used = new HashSet<string>(result.RelicSlots.Select(s => s.relic.relicID));
            var pool = RelicManager.Instance?.GetAvailable() ?? new List<RelicData>();

            RelicRarity target = Random.value < _balance.featuredLegendaryChance
                ? RelicRarity.Legendary
                : PickRarityForDepth(_balance.GetRelicRarityForDepth(depth));

            var candidates = pool.Where(r => !used.Contains(r.relicID) && r.rarity == target).ToList();
            if (candidates.Count == 0)
                candidates = pool.Where(r => !used.Contains(r.relicID)).ToList();
            if (candidates.Count == 0) return;

            var relic = candidates[Random.Range(0, candidates.Count)];
            var slot = new MerchantSlotData { kind = MerchantSlotKind.Featured, relic = relic };

            ApplyRelicPrice(slot, result, forceNoStandardDiscount: true);
            float featuredDiscount = Random.Range(_balance.featuredSlotDiscountMin, _balance.featuredSlotDiscountMax);
            slot.basePrice = slot.finalPrice;
            slot.finalPrice = Mathf.Max(1, Mathf.RoundToInt(slot.finalPrice * (1f - featuredDiscount)));
            slot.isDiscounted = true;

            result.FeaturedSlot = slot;
        }

        private RelicData PickRelic(List<RelicData> pool, HashSet<string> used, DepthRarityEntry rarityTable)
        {
            var available = pool.Where(r => !used.Contains(r.relicID)).ToList();
            if (available.Count == 0) return null;

            var target = PickRarityForDepth(rarityTable);
            var tierPool = available.Where(r => r.rarity == target).ToList();
            if (tierPool.Count == 0) tierPool = available;

            return tierPool[Random.Range(0, tierPool.Count)];
        }

        private ItemData PickConsumable(List<ItemData> pool, HashSet<string> used, DepthRarityEntry rarityTable)
        {
            var available = pool.Where(it => !used.Contains(it.itemID)).ToList();
            if (available.Count == 0) return null;

            var target = PickItemRarityForDepth(rarityTable);
            var tierPool = available.Where(it => it.rarity == target).ToList();
            if (tierPool.Count == 0) tierPool = available;

            return tierPool[Random.Range(0, tierPool.Count)];
        }

        private static RelicRarity PickRarityForDepth(DepthRarityEntry entry)
        {
            float roll = Random.value;
            if (roll < entry.commonChance) return RelicRarity.Common;
            if (roll < entry.commonChance + entry.rareChance) return RelicRarity.Rare;
            if (roll < entry.commonChance + entry.rareChance + entry.uniqueChance) return RelicRarity.Unique;
            return RelicRarity.Legendary;
        }

        private static ItemRarity PickItemRarityForDepth(DepthRarityEntry entry)
        {
            float roll = Random.value;
            if (roll < entry.commonChance) return ItemRarity.Common;
            if (roll < entry.commonChance + entry.rareChance) return ItemRarity.Rare;
            return ItemRarity.Legendary;
        }

        private void ApplyRelicPrice(MerchantSlotData slot, MerchantInventoryModel result, bool forceNoStandardDiscount = false)
        {
            int basePrice = slot.relic.rarity switch
            {
                RelicRarity.Common => _balance.relicPriceCommon,
                RelicRarity.Rare => _balance.relicPriceRare,
                RelicRarity.Unique => _balance.relicPriceUnique,
                _ => _balance.relicPriceLegendary,
            };
            slot.priceCurrencyItemId = slot.relic.rarity switch
            {
                RelicRarity.Common => AddressableKeys.ItemSilver,
                RelicRarity.Rare => AddressableKeys.ItemGold,
                RelicRarity.Unique => AddressableKeys.ItemGold,
                _ => AddressableKeys.ItemDiamond,
            };

            slot.basePrice = basePrice;
            slot.finalPrice = basePrice;

            float discountPercent = 0f;
            if (!forceNoStandardDiscount && result.HasDiscountEvent)
            {
                if (result.IsAllDiscount) discountPercent = _balance.discountAllPercent;
                else if (slot.relic.rarity == RelicRarity.Rare) discountPercent = _balance.discountRareRelicPercent;
            }

            // 그룹 H: 상점 할인 유물 — 이벤트 유무와 무관하게 상시 적용
            discountPercent += RelicManager.Instance?.GetShopDiscountBonus() ?? 0f;

            if (discountPercent > 0f)
            {
                slot.finalPrice = Mathf.Max(1, Mathf.RoundToInt(basePrice * (1f - Mathf.Clamp01(discountPercent))));
                slot.isDiscounted = true;
            }
        }

        private void ApplyConsumablePrice(MerchantSlotData slot, MerchantInventoryModel result)
        {
            int basePrice = slot.item.rarity switch
            {
                ItemRarity.Common => _balance.consumablePriceCommon,
                ItemRarity.Rare => _balance.consumablePriceRare,
                _ => _balance.consumablePriceLegendary,
            };
            slot.priceCurrencyItemId = slot.item.rarity switch
            {
                ItemRarity.Common => AddressableKeys.ItemSilver,
                ItemRarity.Rare => AddressableKeys.ItemGold,
                _ => AddressableKeys.ItemDiamond,
            };

            slot.basePrice = basePrice;
            slot.finalPrice = basePrice;

            float discountPercent = (result.HasDiscountEvent && result.IsAllDiscount) ? _balance.discountAllPercent : 0f;

            // 그룹 H: 상점 할인 유물 — 이벤트 유무와 무관하게 상시 적용
            discountPercent += RelicManager.Instance?.GetShopDiscountBonus() ?? 0f;

            if (discountPercent > 0f)
            {
                slot.finalPrice = Mathf.Max(1, Mathf.RoundToInt(basePrice * (1f - Mathf.Clamp01(discountPercent))));
                slot.isDiscounted = true;
            }
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void LogGeneratedShop(MerchantInventoryModel result)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[Merchant]\nGenerate Shop\n----------------\nRelic\n");
            foreach (var s in result.RelicSlots) sb.Append(s.relic.rarity).Append('\n');
            sb.Append("----------------\nConsumable\n");
            foreach (var s in result.ConsumableSlots) sb.Append(s.item.rarity).Append('\n');
            if (result.HasDiscountEvent)
            {
                sb.Append("----------------\nDiscount\n");
                sb.Append(result.IsAllDiscount ? $"{_balance.discountAllPercent * 100f}% All" : $"{_balance.discountRareRelicPercent * 100f}% Rare Relic").Append('\n');
            }
            Debug.Log(sb.ToString());
        }
#endif
    }
}
