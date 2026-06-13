using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Core;
using Cysharp.Threading.Tasks;

namespace DeepEarth.UI
{
    public class EffectHUDPresenter
    {
        private readonly EffectHUDView _view;
        private readonly List<Action> _unbindActions = new List<Action>();

        public EffectHUDPresenter(EffectHUDView view)
        {
            _view = view;

            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.OnEffectsChanged += RefreshHUD;
            }

            RefreshHUD();
        }

        public void Dispose()
        {
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.OnEffectsChanged -= RefreshHUD;
            }

            UnbindTooltips();
            _view.ClearAll();
        }

        private void UnbindTooltips()
        {
            foreach (var unbind in _unbindActions)
            {
                unbind?.Invoke();
            }
            _unbindActions.Clear();
        }

        private void RefreshHUD()
        {
            UnbindTooltips();
            _view.ClearAll();

            if (EffectManager.Instance == null || _view == null) return;

            var active = EffectManager.Instance.GetActiveEffects();
            int displayCount = Mathf.Min(active.Count, 10);

            for (int i = 0; i < displayCount; i++)
            {
                var effect = active[i];
                var iconView = _view.GetIconFromPool();
                if (iconView == null) continue;

                iconView.SetStack(effect.ValueDisplayString);

                LoadIconSpriteAsync(iconView, effect.IconKey).Forget();

                string title = effect.GetTranslatedName();
                string description = effect.GetFormattedDescription();

                Action showTooltip = () =>
                {
                    _view.ShowTooltip(iconView.transform.position, title, description);
                };
                Action hideTooltip = () =>
                {
                    _view.HideTooltip();
                };

                iconView.Trigger.OnShowTooltip += showTooltip;
                iconView.Trigger.OnHideTooltip += hideTooltip;

                _unbindActions.Add(() =>
                {
                    if (iconView != null && iconView.Trigger != null)
                    {
                        iconView.Trigger.OnShowTooltip -= showTooltip;
                        iconView.Trigger.OnHideTooltip -= hideTooltip;
                    }
                });
            }
        }

        private async UniTaskVoid LoadIconSpriteAsync(EffectIconView iconView, string key)
        {
            if (iconView == null) return;

            Sprite sprite = null;
            if (ResourceManager.Instance != null)
            {
                sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(key);
                if (sprite == null)
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>("Effect_Placeholder");
                }
            }

            if (iconView != null && sprite != null)
            {
                iconView.SetIcon(sprite);
            }
        }
    }
}
