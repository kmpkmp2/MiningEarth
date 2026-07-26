using System.Collections.Generic;
using DeepEarth.UI;

namespace DeepEarth.Core
{
    public enum MerchantSlotKind { Relic, Consumable, Potion, Featured }

    public class MerchantSlotData
    {
        public MerchantSlotKind kind;
        public RelicData relic;   // kind == Relic || Featured
        public ItemData item;     // kind == Consumable || Potion
        public int purchaseQuantity = 1; // Potion 슬롯은 1개 단위 구매

        public string priceCurrencyItemId;
        public int basePrice;
        public int finalPrice;
        public bool isDiscounted;
        public bool soldOut;

        public string NameLocKey => relic != null ? relic.nameLocKey : item?.nameLocKey;
        public string DescLocKey => relic != null ? relic.descLocKey : item?.descLocKey;
        public string IconKey => relic != null ? relic.iconKey : item?.iconKey;
        public RelicRarity RelicRarityValue => relic != null ? relic.rarity : RelicRarity.Common;
        public ItemRarity ItemRarityValue => item != null ? item.rarity : ItemRarity.Common;
        public bool IsLegendary => relic != null ? relic.rarity == RelicRarity.Legendary : item != null && item.rarity == ItemRarity.Legendary;
    }

    // 한 번의 Merchant 노드 방문 동안만 유지되는 휘발성 상점 데이터.
    // SaveData에 저장되지 않는다 — 노드 완료와 함께 소멸(Event/Grave/Treasure와 동일한 패턴).
    public class MerchantInventoryModel
    {
        public List<MerchantSlotData> RelicSlots = new List<MerchantSlotData>();
        public List<MerchantSlotData> ConsumableSlots = new List<MerchantSlotData>();
        public List<MerchantSlotData> PotionSlots = new List<MerchantSlotData>();
        public MerchantSlotData FeaturedSlot;

        public string MerchantQuoteKey;
        public bool HasDiscountEvent;
        public bool IsAllDiscount;   // true = 전체 할인, false = Rare 유물 할인

        public IEnumerable<MerchantSlotData> AllSlots()
        {
            foreach (var s in RelicSlots) yield return s;
            foreach (var s in ConsumableSlots) yield return s;
            foreach (var s in PotionSlots) yield return s;
            if (FeaturedSlot != null) yield return FeaturedSlot;
        }

        public void MarkSoldOut(MerchantSlotData slot)
        {
            slot.soldOut = true;
        }
    }
}
