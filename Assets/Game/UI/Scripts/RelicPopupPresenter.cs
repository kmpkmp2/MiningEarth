using System;
using UnityEngine;
using DeepEarth.Core;
using Cysharp.Threading.Tasks;

namespace DeepEarth.UI
{
    public class RelicPopupPresenter
    {
        private readonly RelicPopupView _view;
        private readonly GameManager _gameManager;

        public RelicPopupPresenter(RelicPopupView view, GameManager gameManager)
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

            _view.SetVisible(true);
            _view.LocalizeTitle(LocalizationManager.Instance.GetTranslation("relic_popup_title"));
            _view.ClearCards();

            if (EffectManager.Instance == null) return;

            var active = EffectManager.Instance.GetActiveEffects();
            var contentParent = _view.GetContentParent();
            var prefab = _view.GetCardPrefab();

            if (contentParent == null || prefab == null) return;

            foreach (var effect in active)
            {
                var cardGo = UnityEngine.Object.Instantiate(prefab, contentParent);
                _view.AddCard(cardGo);

                var cardView = cardGo.GetComponent<EffectCardView>();
                if (cardView == null) continue;

                string typeKey = $"effect_type_{effect.EffectType}";
                string rawTypeName = LocalizationManager.Instance.GetTranslation(typeKey);
                string typeFormatted = LocalizationManager.Instance.GetFormatted("relic_popup_type_label", rawTypeName);

                string name = effect.GetTranslatedName();
                string descFormatted = LocalizationManager.Instance.GetFormatted("relic_popup_effect_label", effect.GetFormattedDescription());

                cardView.Setup(name, typeFormatted, descFormatted, null, effect.EffectType);

                LoadIconSpriteForCardAsync(cardView, effect.IconKey).Forget();
            }
        }

        private void HandleClose()
        {
            _view.SetVisible(false);
            if (_gameManager != null)
            {
                _gameManager.CloseRelicPopup();
            }
        }

        private async UniTaskVoid LoadIconSpriteForCardAsync(EffectCardView cardView, string key)
        {
            if (cardView == null) return;

            Sprite sprite = null;
            if (ResourceManager.Instance != null)
            {
                sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(key);
                if (sprite == null)
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>("Effect_Placeholder");
                }
            }

            if (cardView != null && sprite != null)
            {
                var iconField = typeof(EffectCardView).GetField("iconImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (iconField != null)
                {
                    var img = (UnityEngine.UI.Image)iconField.GetValue(cardView);
                    if (img != null) img.sprite = sprite;
                }
            }
        }
    }
}
