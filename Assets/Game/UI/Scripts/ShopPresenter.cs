using TMPro;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class ShopPresenter
    {
        private readonly ShopPanelView        _panelView;
        private readonly PickaxeShopPresenter _pickaxePresenter;
        private ShopCategory _currentCategory = ShopCategory.Pickaxe;

        public ShopPresenter(ShopPanelView panelView, GameObject slotPrefab)
        {
            _panelView = panelView;
            _panelView.OnCategorySelected += HandleCategorySelected;

            _pickaxePresenter = new PickaxeShopPresenter(_panelView.ContentView, slotPrefab);
            _pickaxePresenter.OnItemSelected += HandleItemSelected;

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;

            _panelView.SetActiveTab(ShopCategory.Pickaxe);
            _panelView.InfoView.ShowPlaceholder();
        }

        // 상점이 열릴 때 호출 — 현재 탭의 아이템 리스트만 갱신
        public void Refresh()
        {
            if (_currentCategory == ShopCategory.Pickaxe)
                _pickaxePresenter.Activate();
        }

        // 언어 변경 시 탭 라벨 + 슬롯 텍스트 갱신
        public void Localize()
        {
            _panelView.LocalizeTabs();
            Refresh();
        }

        public void Dispose()
        {
            if (_panelView != null)
                _panelView.OnCategorySelected -= HandleCategorySelected;

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;

            _pickaxePresenter.OnItemSelected -= HandleItemSelected;
            _pickaxePresenter.Dispose();
        }

        private void HandleCategorySelected(ShopCategory category)
        {
            if (_currentCategory == category) return;
            _currentCategory = category;

            _panelView.ContentView.Clear();
            _panelView.InfoView.ShowPlaceholder();
            _panelView.SetActiveTab(category);

            if (category == ShopCategory.Pickaxe)
            {
                _pickaxePresenter.Activate();
            }
            else
            {
                _pickaxePresenter.Deactivate();
                ShowComingSoon();
            }
        }

        private void HandleItemSelected(ShopItemDisplayData data)
        {
            _panelView.InfoView.Show(data);
        }

        private void HandleLanguageChanged()
        {
            _panelView.LocalizeTabs();
            if (_currentCategory == ShopCategory.Pickaxe)
                _pickaxePresenter.Refresh();
        }

        private void ShowComingSoon()
        {
            var go = new GameObject("ComingSoon", typeof(RectTransform));
            go.transform.SetParent(_panelView.ContentView.Content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 80f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = 16f;
            tmp.color     = new Color(0.5f, 0.5f, 0.5f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text      = LocalizationManager.Instance?.GetTranslation("menu_shop_coming_soon") ?? "COMING SOON";
        }
    }
}
