using System;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class CharacterShopPresenter
    {
        private readonly ShopContentView _contentView;
        private readonly CharacterShopView _view;
        private bool _isActive;

        public event Action<ShopItemDisplayData> OnItemSelected;

        public CharacterShopPresenter(ShopContentView contentView, UnityEngine.GameObject slotPrefab)
        {
            _contentView = contentView;
            _view = new CharacterShopView(contentView.Content, slotPrefab);

            _view.OnItemSelected += data => OnItemSelected?.Invoke(data);
            _view.OnItemAction += HandleItemAction;
        }

        public void Activate()
        {
            _isActive = true;
            Refresh();
        }

        public void Deactivate()
        {
            _isActive = false;
        }

        public void ClearView()
        {
            _view.ClearSlotRefs();
        }

        public void Refresh()
        {
            if (!_isActive || CharacterManager.Instance == null) return;

            _view.Refresh(CharacterDatabase.Characters);

            _contentView.ScrollToTop();
        }

        public void Dispose()
        {
            _view.ClearSlotRefs();
        }

        private void HandleItemAction(ShopItemDisplayData data)
        {
            if (data.tag is not CharacterStaticData characterData) return;

            if (CharacterManager.Instance.UnlockCharacter(characterData.ID))
            {
                Refresh();

                if (_view.TryGetDisplayData(characterData.ID, out var refreshedData))
                    OnItemSelected?.Invoke(refreshedData);
            }
        }
    }
}
