using System;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class InventoryPresenter
    {
        private readonly InventoryPopupView _view;
        private readonly GameManager _gameManager;
        private readonly InventoryPopupPresenter _popupPresenter;

        public InventoryPresenter(InventoryPopupView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            if (_view != null)
            {
                _view.OnCloseClicked += HandleClose;
                _view.SetVisible(false);

                // Create popup presenter linking views with global InventoryManager collection
                if (InventoryManager.Instance != null)
                {
                    _popupPresenter = new InventoryPopupPresenter(_view, InventoryManager.Instance.Collection);
                }
            }
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnCloseClicked -= HandleClose;
            }
            _popupPresenter?.Dispose();
        }

        public void Open()
        {
            if (_view == null) return;

            Debug.Log("[UI] InventoryPopup Open");
            _view.SetVisible(true);
            _view.LocalizeTitle(LocalizationManager.Instance.GetTranslation("inventory_popup_title"));
            
            // Initialize popup presenter data and slots rendering
            _popupPresenter?.InitializePopup();
        }

        private void HandleClose()
        {
            _view.SetVisible(false);
            if (_gameManager != null)
            {
                _gameManager.CloseInventoryPopup();
            }
        }
    }
}
