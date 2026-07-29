using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;
using DeepEarth.UI;

namespace DeepEarth.Battle
{
    // Target Select 상태 동안 몬스터별 선택 인디케이터 표시 + 클릭(Combat.MonsterView.OnTouched 재사용) 감지 담당.
    public class TargetSelectPresenter
    {
        private readonly GameObject _indicatorPrefab;
        private readonly Transform _layer;
        private readonly Dictionary<MonsterPresenter, TargetIndicatorView> _views = new Dictionary<MonsterPresenter, TargetIndicatorView>();
        private readonly Dictionary<MonsterPresenter, Action> _touchHandlers = new Dictionary<MonsterPresenter, Action>();

        private Sprite _ringSprite;
        private bool _ringLoadAttempted;

        public TargetSelectPresenter(GameObject indicatorPrefab, Transform layer)
        {
            _indicatorPrefab = indicatorPrefab;
            _layer = layer;
        }

        public async UniTask<MonsterPresenter> SelectTargetAsync(IReadOnlyList<MonsterPresenter> targets, BattleView view)
        {
            await EnsureRingSpriteLoadedAsync();

            var tcs = new UniTaskCompletionSource<MonsterPresenter>();

            void HandleCancel() => tcs.TrySetResult(null);

            foreach (var m in targets)
                ShowIndicator(m, tcs);

            if (view != null)
            {
                view.OnCancelClicked += HandleCancel;
                view.OnAttackClicked += HandleCancel; // 공격 버튼 다시 선택 = 취소
                view.SetTargetSelectUIVisible(true);
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Battle]\nTarget Select");
#endif
            try
            {
                return await tcs.Task;
            }
            finally
            {
                if (view != null)
                {
                    view.OnCancelClicked -= HandleCancel;
                    view.OnAttackClicked -= HandleCancel;
                    view.SetTargetSelectUIVisible(false);
                }
                ClearIndicators();
            }
        }

        private async UniTask EnsureRingSpriteLoadedAsync()
        {
            if (_ringLoadAttempted) return;
            _ringLoadAttempted = true;
            _ringSprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(AddressableKeys.TargetRingIcon);
        }

        private void ShowIndicator(MonsterPresenter monster, UniTaskCompletionSource<MonsterPresenter> tcs)
        {
            if (_indicatorPrefab == null || _layer == null) return;

            var go = UnityEngine.Object.Instantiate(_indicatorPrefab, _layer);
            var indicator = go.GetComponent<TargetIndicatorView>();
            if (indicator == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }

            indicator.SetRingSprite(_ringSprite);
            indicator.SetFollowTarget(monster.CombatPresenter.View.transform);
            indicator.SetVisible(true);
            indicator.PlayScaleInAsync().Forget();
            _views[monster] = indicator;

            Action handler = () => HandleSelected(monster, indicator, tcs).Forget();
            _touchHandlers[monster] = handler;
            monster.CombatPresenter.View.OnTouched += handler;
        }

        private async UniTaskVoid HandleSelected(MonsterPresenter monster, TargetIndicatorView selectedIndicator, UniTaskCompletionSource<MonsterPresenter> tcs)
        {
            if (tcs.Task.Status != UniTaskStatus.Pending) return; // 이미 다른 클릭으로 처리됨(중복 클릭 방지)

            // 선택되지 않은 나머지 인디케이터는 즉시 정리하고, 선택된 것만 확정 연출을 재생한다.
            foreach (var kv in _views)
                if (kv.Key != monster && kv.Value != null)
                    UnityEngine.Object.Destroy(kv.Value.gameObject);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Battle]\nTarget\n{monster.CombatPresenter.Model.Type}");
#endif
            await selectedIndicator.PlayConfirmAsync();
            tcs.TrySetResult(monster);
        }

        private void ClearIndicators()
        {
            foreach (var kv in _touchHandlers)
                if (kv.Key?.CombatPresenter?.View != null)
                    kv.Key.CombatPresenter.View.OnTouched -= kv.Value;
            _touchHandlers.Clear();

            foreach (var kv in _views)
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value.gameObject);
            _views.Clear();
        }
    }
}
