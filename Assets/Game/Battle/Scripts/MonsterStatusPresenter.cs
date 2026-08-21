using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.UI;

namespace DeepEarth.Battle
{
    // 몬스터 하단 상태 아이콘 줄 관리. HPBarPresenter와 동일한 인스턴스 관리 패턴이되,
    // 이벤트로 커버되지 않는 진행 중인 엘리트 스킬 상태(방어구/격노 등)까지 반영하기 위해
    // 등록된 몬스터마다 주기적으로 MonsterPresenter.GetStatusEntries()를 다시 조회해 아이콘 목록을 갱신한다.
    public class MonsterStatusPresenter
    {
        private const float RefreshInterval = 0.5f;

        private readonly GameObject _viewPrefab;
        private readonly Transform _layer;
        private readonly Dictionary<MonsterPresenter, EffectHUDView> _views = new();
        private readonly Dictionary<MonsterPresenter, CancellationTokenSource> _pollTokens = new();
        private readonly Dictionary<MonsterPresenter, List<Action>> _unbindActions = new();

        public MonsterStatusPresenter(GameObject viewPrefab, Transform layer)
        {
            _viewPrefab = viewPrefab;
            _layer = layer;
        }

        public void Register(MonsterPresenter monster)
        {
            if (_viewPrefab == null || _layer == null) return;

            var go = UnityEngine.Object.Instantiate(_viewPrefab, _layer);
            var tracker = go.GetComponent<MonsterEffectHUDTracker>();
            var view = go.GetComponent<EffectHUDView>();
            if (tracker == null || view == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }

            tracker.SetFollowTarget(monster.CombatPresenter.View.transform);
            tracker.SetVisible(true);

            _views[monster] = view;
            _unbindActions[monster] = new List<Action>();

            var cts = new CancellationTokenSource();
            _pollTokens[monster] = cts;
            PollAsync(monster, view, cts.Token).Forget();
        }

        public void Remove(MonsterPresenter monster)
        {
            if (_pollTokens.TryGetValue(monster, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _pollTokens.Remove(monster);
            }

            UnbindTooltips(monster);
            _unbindActions.Remove(monster);

            if (_views.TryGetValue(monster, out var view))
            {
                if (view != null) UnityEngine.Object.Destroy(view.gameObject);
                _views.Remove(monster);
            }
        }

        public void Clear()
        {
            foreach (var cts in _pollTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _pollTokens.Clear();
            _unbindActions.Clear();

            foreach (var view in _views.Values)
                if (view != null) UnityEngine.Object.Destroy(view.gameObject);
            _views.Clear();
        }

        private void UnbindTooltips(MonsterPresenter monster)
        {
            if (!_unbindActions.TryGetValue(monster, out var actions)) return;
            foreach (var unbind in actions) unbind?.Invoke();
            actions.Clear();
        }

        private async UniTaskVoid PollAsync(MonsterPresenter monster, EffectHUDView view, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                RefreshOnce(monster, view);
                try { await UniTask.Delay(TimeSpan.FromSeconds(RefreshInterval), cancellationToken: token); }
                catch (OperationCanceledException) { return; }
            }
        }

        private void RefreshOnce(MonsterPresenter monster, EffectHUDView view)
        {
            if (view == null || monster.CombatPresenter.Model.IsDead) return;

            UnbindTooltips(monster);
            view.ClearAll();

            var entries = monster.GetStatusEntries();
            var unbindList = _unbindActions.TryGetValue(monster, out var list) ? list : null;

            foreach (var entry in entries)
            {
                var iconView = view.GetIconFromPool();
                if (iconView == null) continue;

                LoadIconSpriteAsync(iconView, entry.IconKey).Forget();

                string title = entry.Title;
                string description = entry.Description;
                Action showTooltip = () => view.ShowTooltip(iconView.transform.position, title, description);
                Action hideTooltip = () => view.HideTooltip();

                iconView.Trigger.OnShowTooltip += showTooltip;
                iconView.Trigger.OnHideTooltip += hideTooltip;

                unbindList?.Add(() =>
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
