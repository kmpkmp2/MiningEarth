using System;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class InventoryPresenter
    {
        private readonly InventoryPopupView _view;
        private readonly GameManager _gameManager;

        public InventoryPresenter(InventoryPopupView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            if (_view != null)
            {
                _view.OnCloseClicked += HandleClose;
                _view.SetVisible(false);
            }
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnCloseClicked -= HandleClose;
            }
        }

        public void Open()
        {
            if (_view == null) return;

            Debug.Log("[UI] InventoryPopup Open");
            _view.SetVisible(true);
            _view.LocalizeTitle(LocalizationManager.Instance.GetTranslation("inventory_popup_title"));
            
            // Format current resource stats
            if (_gameManager != null)
            {
                int currentItems = _gameManager.IronCount + _gameManager.SilverCount + _gameManager.GoldCount + _gameManager.DiamondCount;
                int maxCap = StatManager.Instance.GetInventorySize();
                
                string stats = $"{LocalizationManager.Instance.GetFormatted("hud_iron", _gameManager.IronCount)}\n" +
                               $"{LocalizationManager.Instance.GetFormatted("hud_silver", _gameManager.SilverCount)}\n" +
                               $"{LocalizationManager.Instance.GetFormatted("hud_gold", _gameManager.GoldCount)}\n" +
                               $"{LocalizationManager.Instance.GetFormatted("hud_diamond", _gameManager.DiamondCount)}\n\n" +
                               $"{LocalizationManager.Instance.GetFormatted("hud_bag", currentItems, maxCap)}";
                
                _view.UpdateInventoryStats(stats);
            }
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
