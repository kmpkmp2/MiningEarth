using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class ShopPanelView : MonoBehaviour
    {
        [SerializeField] private ShopContentView   contentView;
        [SerializeField] private ShopInfoView      infoView;

        // 탭 순서: [0]=Pickaxe, [1]=Character, [2]=Consumable, [3]=Special
        [SerializeField] private Button[]          tabButtons;
        [SerializeField] private Image[]           tabBackgrounds;
        [SerializeField] private TextMeshProUGUI[] tabLabels;

        private readonly List<(ShopCategory category, Image bg, TextMeshProUGUI label)> _tabs = new();

        public ShopContentView ContentView => contentView;
        public ShopInfoView    InfoView    => infoView;

        public event Action<ShopCategory> OnCategorySelected;

        private void Awake() => EnsureTabs();

        public void SetActiveTab(ShopCategory category)
        {
            EnsureTabs();
            foreach (var (cat, bg, _) in _tabs)
            {
                if (bg) bg.color = cat == category
                    ? new Color(0.15f, 0.35f, 0.55f, 1f)
                    : new Color(0.12f, 0.14f, 0.18f, 1f);
            }
        }

        public void LocalizeTabs()
        {
            EnsureTabs();
            var loc = LocalizationManager.Instance;
            foreach (var (cat, _, label) in _tabs)
            {
                if (!label) continue;
                string key = cat switch
                {
                    ShopCategory.Pickaxe    => "shop_tab_pickaxe",
                    ShopCategory.Character  => "shop_tab_character",
                    ShopCategory.Consumable => "shop_tab_consumable",
                    ShopCategory.Special    => "shop_tab_special",
                    _                       => cat.ToString()
                };
                label.text = loc?.GetTranslation(key) ?? cat.ToString();
            }
        }

        // ShopPanel이 비활성 상태에서 Presenter 생성자가 접근할 수 있도록 lazy 초기화
        private void EnsureTabs()
        {
            if (_tabs.Count > 0) return;

            var catOrder = new ShopCategory[]
            {
                ShopCategory.Pickaxe,
                ShopCategory.Character,
                ShopCategory.Consumable,
                ShopCategory.Special,
            };

            for (int i = 0; i < catOrder.Length; i++)
            {
                var capturedCat = catOrder[i];
                if (tabButtons != null && i < tabButtons.Length && tabButtons[i] != null)
                    tabButtons[i].onClick.AddListener(() => OnCategorySelected?.Invoke(capturedCat));

                var bg  = tabBackgrounds != null && i < tabBackgrounds.Length ? tabBackgrounds[i] : null;
                var lbl = tabLabels     != null && i < tabLabels.Length      ? tabLabels[i]      : null;
                _tabs.Add((catOrder[i], bg, lbl));
            }
        }
    }
}
