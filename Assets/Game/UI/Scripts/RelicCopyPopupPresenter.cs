using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // 그룹 L(수집가의 가방) 전용 — 후보 유물 목록 중 하나를 강제 선택하게 하는 팝업.
    // 신규 Singleton이 아니라 GameManager가 인스턴스 필드로 소유(TargetSelectPresenter와 동일 패턴).
    public class RelicCopyPopupPresenter
    {
        private readonly RelicCopyPopupView _view;
        private readonly List<RelicCopyCardView> _activeCardViews = new List<RelicCopyCardView>();

        public RelicCopyPopupPresenter(RelicCopyPopupView view)
        {
            _view = view;
            _view?.SetVisible(false);
        }

        // 취소 불가 — 후보가 있는 한 반드시 하나를 반환한다.
        public async UniTask<RelicData> SelectRelicAsync(List<RelicData> candidates)
        {
            if (_view == null || candidates == null || candidates.Count == 0) return null;

            var tcs = new UniTaskCompletionSource<RelicData>();

            _view.SetVisible(true);
            _view.LocalizeTitle(LocalizationManager.Instance.GetTranslation("relic_copy_popup_title"));
            _view.ClearCards();
            _activeCardViews.Clear();

            var contentParent = _view.GetContentParent();
            var prefab = _view.GetCardPrefab();

            if (contentParent != null && prefab != null)
            {
                foreach (var relic in candidates)
                {
                    var cardGo = Object.Instantiate(prefab, contentParent);
                    _view.AddCard(cardGo);

                    var cardView = cardGo.GetComponent<RelicCopyCardView>();
                    if (cardView == null) continue;

                    string name = LocalizationManager.Instance.GetTranslation(relic.nameLocKey);
                    string desc = LocalizationManager.Instance.GetTranslation(relic.descLocKey);
                    string typeName = LocalizationManager.Instance.GetTranslation(RarityLocKey(relic.rarity));
                    cardView.Setup(name, typeName, desc, null, relic.rarity);
                    LoadIconSpriteForCardAsync(cardView, relic.iconKey).Forget();

                    var capturedRelic = relic;
                    void Handler()
                    {
                        if (tcs.Task.Status != UniTaskStatus.Pending) return;
                        tcs.TrySetResult(capturedRelic);
                    }
                    cardView.OnClicked += Handler;
                    _activeCardViews.Add(cardView);
                }
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Relic]\nCollector's Bag Selection\nCandidates : {candidates.Count}");
#endif

            var selected = await tcs.Task;
            _view.SetVisible(false);
            _view.ClearCards();
            _activeCardViews.Clear();
            return selected;
        }

        private static string RarityLocKey(RelicRarity rarity) => rarity switch
        {
            RelicRarity.Legendary => "relic_rarity_legendary",
            RelicRarity.Unique    => "relic_rarity_unique",
            RelicRarity.Rare      => "relic_rarity_rare",
            _                     => "relic_rarity_common"
        };

        private async UniTaskVoid LoadIconSpriteForCardAsync(RelicCopyCardView cardView, string key)
        {
            if (cardView == null || string.IsNullOrEmpty(key) || ResourceManager.Instance == null) return;

            var sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(key);
            if (sprite == null)
                sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>("Effect_Placeholder");

            if (cardView != null && sprite != null)
            {
                var iconField = typeof(RelicCopyCardView).GetField("iconImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (iconField != null)
                {
                    var img = (UnityEngine.UI.Image)iconField.GetValue(cardView);
                    if (img != null) img.sprite = sprite;
                }
            }
        }
    }
}
