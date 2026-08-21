using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.UI;

namespace DeepEarth.Battle
{
    // 몬스터별 남은 체력을 머리 위에 표시. IntentPresenter와 동일한 인스턴스 관리 패턴이되,
    // 매 턴 갱신이 아니라 Combat.MonsterModel.OnHPChanged 이벤트 구독으로 즉시 갱신한다.
    public class HPBarPresenter
    {
        private readonly GameObject _viewPrefab;
        private readonly Transform _layer;
        private readonly Dictionary<MonsterPresenter, MonsterHPBarView> _views = new();
        private readonly Dictionary<MonsterPresenter, Action<int, int>> _handlers = new();
        private readonly Dictionary<MonsterPresenter, Action<int, int>> _shieldHandlers = new();

        public HPBarPresenter(GameObject viewPrefab, Transform layer)
        {
            _viewPrefab = viewPrefab;
            _layer = layer;
        }

        public void Register(MonsterPresenter monster)
        {
            if (_viewPrefab == null || _layer == null) return;

            var go = UnityEngine.Object.Instantiate(_viewPrefab, _layer);
            var view = go.GetComponent<MonsterHPBarView>();
            if (view == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }

            view.SetFollowTarget(monster.CombatPresenter.View.transform);

            var model = monster.CombatPresenter.Model;
            view.SetHP(model.CurrentHP, model.MaxHP);
            view.SetShield(model.Shield, model.MaxHP);
            view.SetVisible(true);

            Action<int, int> handler = (cur, max) => view.SetHP(cur, max);
            model.OnHPChanged += handler;

            Action<int, int> shieldHandler = (cur, max) => view.SetShield(cur, max);
            model.OnShieldChanged += shieldHandler;

            _views[monster] = view;
            _handlers[monster] = handler;
            _shieldHandlers[monster] = shieldHandler;
        }

        public void Remove(MonsterPresenter monster)
        {
            if (_handlers.TryGetValue(monster, out var handler))
            {
                if (monster?.CombatPresenter?.Model != null)
                    monster.CombatPresenter.Model.OnHPChanged -= handler;
                _handlers.Remove(monster);
            }

            if (_shieldHandlers.TryGetValue(monster, out var shieldHandler))
            {
                if (monster?.CombatPresenter?.Model != null)
                    monster.CombatPresenter.Model.OnShieldChanged -= shieldHandler;
                _shieldHandlers.Remove(monster);
            }

            if (_views.TryGetValue(monster, out var view))
            {
                if (view != null) UnityEngine.Object.Destroy(view.gameObject);
                _views.Remove(monster);
            }
        }

        public void Clear()
        {
            foreach (var kv in _handlers)
                if (kv.Key?.CombatPresenter?.Model != null)
                    kv.Key.CombatPresenter.Model.OnHPChanged -= kv.Value;
            _handlers.Clear();

            foreach (var kv in _shieldHandlers)
                if (kv.Key?.CombatPresenter?.Model != null)
                    kv.Key.CombatPresenter.Model.OnShieldChanged -= kv.Value;
            _shieldHandlers.Clear();

            foreach (var kv in _views)
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value.gameObject);
            _views.Clear();
        }
    }
}
