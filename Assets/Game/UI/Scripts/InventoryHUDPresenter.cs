using System;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class InventoryHUDPresenter
    {
        private readonly GameUIView _view;
        private readonly InventoryCollection _collection;

        public InventoryHUDPresenter(GameUIView view, InventoryCollection collection)
        {
            _view = view;
            _collection = collection;

            _collection.OnInventoryChanged += RefreshHUD;
            if (StatManager.Instance != null)
            {
                StatManager.Instance.OnStatsUpdated += RefreshHUD;
            }
            RefreshHUD();
        }

        public void Dispose()
        {
            if (_collection != null)
            {
                _collection.OnInventoryChanged -= RefreshHUD;
            }
            if (StatManager.Instance != null)
            {
                StatManager.Instance.OnStatsUpdated -= RefreshHUD;
            }
        }

        private void RefreshHUD()
        {
            if (_view != null && InventoryManager.Instance != null)
            {
                int usedCount = _collection.Slots.Count;
                int capacity = InventoryManager.Instance.InventoryCapacity;
                Debug.Log($"[Inventory]\nHUD Refresh\n{usedCount} / {capacity}");
                _view.SetInventorySize(usedCount, capacity);
            }
        }
    }
}
