using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;
using DeepEarth.UI;

namespace DeepEarth.Battle
{
    // 방어 버튼 클릭 시 광물 선택 팝업 대기/취소 처리 + 선택 시 소비/Shield 부여. BattlePresenter가 소유(신규 Singleton 아님).
    public class ShieldPresenter
    {
        private readonly ShieldPopupView _view;
        private UniTaskCompletionSource<BlockType?> _selectionTcs;

        public ShieldPresenter(ShieldPopupView view)
        {
            _view = view;
            if (_view != null)
            {
                _view.OnOreSelected += HandleOreSelected;
                _view.OnCancelClicked += HandleCancel;
            }
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnOreSelected -= HandleOreSelected;
                _view.OnCancelClicked -= HandleCancel;
            }
        }

        // true = 광물 소비+Shield 부여 완료(턴 소모). false = 취소(턴 미소모).
        public async UniTask<bool> SelectAndApplyOreAsync()
        {
            if (_view == null) return false;

            RefreshCounts();
            _selectionTcs = new UniTaskCompletionSource<BlockType?>();
            _view.SetVisible(true);

            BlockType? selected;
            try { selected = await _selectionTcs.Task; }
            finally { _view.SetVisible(false); }

            if (selected == null) return false;

            ApplyOre(selected.Value);
            return true;
        }

        private void RefreshCounts()
        {
            var data = ShieldData.Instance;
            _view.SetOreButton(BlockType.Iron,    InventoryManager.Instance.GetItemCount(AddressableKeys.ItemIron),    data.GetShieldValue(BlockType.Iron));
            _view.SetOreButton(BlockType.Silver,  InventoryManager.Instance.GetItemCount(AddressableKeys.ItemSilver),  data.GetShieldValue(BlockType.Silver));
            _view.SetOreButton(BlockType.Gold,    InventoryManager.Instance.GetItemCount(AddressableKeys.ItemGold),    data.GetShieldValue(BlockType.Gold));
            _view.SetOreButton(BlockType.Diamond, InventoryManager.Instance.GetItemCount(AddressableKeys.ItemDiamond), data.GetShieldValue(BlockType.Diamond));
        }

        private void HandleOreSelected(BlockType type) => _selectionTcs?.TrySetResult(type);
        private void HandleCancel() => _selectionTcs?.TrySetResult(null);

        private void ApplyOre(BlockType type)
        {
            string itemId = type switch
            {
                BlockType.Iron    => AddressableKeys.ItemIron,
                BlockType.Silver  => AddressableKeys.ItemSilver,
                BlockType.Gold    => AddressableKeys.ItemGold,
                BlockType.Diamond => AddressableKeys.ItemDiamond,
                _ => null,
            };

            int shieldAmount = ShieldData.Instance.GetShieldValue(type);
            if (!InventoryManager.Instance.RemoveItem(itemId, 1)) return; // 방어적 체크(버튼이 이미 0개면 비활성화되어 있어야 함)

            StatManager.Instance.AddShield(shieldAmount);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Battle]\nGuard Selected\nMaterial : {type}\nConsumed : 1\nShield +{shieldAmount}\nCurrent Shield : {StatManager.Instance.CurrentShield}\n----------------");
#endif
        }
    }
}
