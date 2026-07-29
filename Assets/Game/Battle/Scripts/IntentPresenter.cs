using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.UI;

namespace DeepEarth.Battle
{
    // 몬스터별 Intent(다음 턴 예고 행동) 표시 갱신 담당. 몬스터 머리 위 월드스페이스 UI를 관리한다.
    public class IntentPresenter
    {
        private readonly MonsterIntentData _intentData;
        private readonly GameObject _intentViewPrefab;
        private readonly Transform _layer;
        private readonly Dictionary<MonsterPresenter, IntentView> _views = new();

        public IntentPresenter(MonsterIntentData intentData, GameObject intentViewPrefab, Transform layer)
        {
            _intentData = intentData;
            _intentViewPrefab = intentViewPrefab;
            _layer = layer;
        }

        public void ShowIntent(MonsterPresenter monster)
        {
            if (_intentViewPrefab == null || _layer == null) return;

            if (!_views.TryGetValue(monster, out var view) || view == null)
            {
                var go = Object.Instantiate(_intentViewPrefab, _layer);
                view = go.GetComponent<IntentView>();
                _views[monster] = view;
            }

            RefreshIntent(monster, view);
        }

        private void RefreshIntent(MonsterPresenter monster, IntentView view)
        {
            var step = monster.CurrentStep;
            if (step == null || view == null)
            {
                view?.SetVisible(false);
                return;
            }

            var entry = _intentData != null ? _intentData.GetEntry(step.intentType) : null;
            string iconKey = entry != null ? entry.iconKey : string.Empty;
            string glyph = entry != null ? entry.iconGlyph : string.Empty;
            string desc = entry != null ? LocalizationManager.Instance.GetTranslation(entry.descLocKey) : string.Empty;

            view.SetVisible(true);
            view.SetIntent(iconKey, glyph, monster.GetDisplayValue(step), desc);
            view.SetFollowTarget(monster.CombatPresenter.View.transform);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Battle]\nMonster Intent\n{step.intentType}");
#endif
        }

        public void RemoveIntent(MonsterPresenter monster)
        {
            if (_views.TryGetValue(monster, out var view))
            {
                if (view != null) Object.Destroy(view.gameObject);
                _views.Remove(monster);
            }
        }

        public void Clear()
        {
            foreach (var kv in _views)
                if (kv.Value != null) Object.Destroy(kv.Value.gameObject);
            _views.Clear();
        }
    }
}
