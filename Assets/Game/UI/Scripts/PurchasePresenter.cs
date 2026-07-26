using System;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // 상인 상품 구매 처리 전담 — 재화 확인 → 차감 → 지급 → SOLD OUT 표시.
    // Potion 슬롯은 상시 판매 상품이라 SOLD OUT 처리하지 않는다(요청서: "포션은 항상 판매한다").
    public class PurchasePresenter
    {
        public event Action<MerchantSlotData> OnPurchased;
        public event Action<string> OnPurchaseFailed; // 실패 사유를 나타내는 로컬라이즈 키

        public bool IsHpFull() => StatManager.Instance != null && StatManager.Instance.CurrentHP >= StatManager.Instance.GetMaxHP();

        public bool CanAfford(MerchantSlotData slot)
        {
            if (slot == null || slot.soldOut) return false;
            if (slot.kind == MerchantSlotKind.Potion && IsHpFull()) return false;

            int owned = InventoryManager.Instance?.GetItemCount(slot.priceCurrencyItemId) ?? 0;
            return owned >= slot.finalPrice;
        }

        public bool TryPurchase(MerchantSlotData slot)
        {
            if (slot == null || slot.soldOut) return false;

            if (slot.kind == MerchantSlotKind.Potion && IsHpFull())
            {
                OnPurchaseFailed?.Invoke("merchant_hp_full");
                return false;
            }

            int owned = InventoryManager.Instance?.GetItemCount(slot.priceCurrencyItemId) ?? 0;
            if (owned < slot.finalPrice)
            {
                OnPurchaseFailed?.Invoke("merchant_quote_no_funds");
                return false;
            }

            InventoryManager.Instance.RemoveItem(slot.priceCurrencyItemId, slot.finalPrice);

            if (slot.kind == MerchantSlotKind.Relic || slot.kind == MerchantSlotKind.Featured)
            {
                RelicManager.Instance?.AddRelic(slot.relic);
            }
            else
            {
                InventoryManager.Instance?.AddItem(slot.item.itemID, slot.purchaseQuantity);
            }

            if (slot.kind != MerchantSlotKind.Potion)
                slot.soldOut = true;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            LogPurchase(slot);
#endif
            OnPurchased?.Invoke(slot);
            return true;
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void LogPurchase(MerchantSlotData slot)
        {
            string itemLabel = slot.relic != null
                ? $"{slot.relic.rarity} Relic ({slot.relic.relicID})"
                : $"{slot.item.rarity} {slot.kind} ({slot.item.itemID})";
            Debug.Log($"[Merchant]\nPurchase\n{itemLabel}\n{slot.priceCurrencyItemId} -{slot.finalPrice}");
        }
#endif
    }
}
