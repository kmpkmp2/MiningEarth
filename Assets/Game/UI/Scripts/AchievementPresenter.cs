using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class AchievementPresenter
    {
        private readonly AchievementPopupView _view;

        private List<AchievementModel> _sortedModels = new List<AchievementModel>();
        private int  _loadedCount;
        private bool _isLoadingMore;

        private const int BatchSize = 20;

        // ── Constructor / Dispose ────────────────────────────────────────────
        public AchievementPresenter(AchievementPopupView view)
        {
            _view = view;
            _view.OnCloseClicked      += HandleClose;
            _view.OnScrollValueChanged += HandleScrollChanged;
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            if (_view == null) return;
            _view.OnCloseClicked      -= HandleClose;
            _view.OnScrollValueChanged -= HandleScrollChanged;
        }

        // ── Public API ───────────────────────────────────────────────────────
        public void Open()
        {
            _view.SetVisible(true);
            Refresh();
        }

        // ── Event handlers ───────────────────────────────────────────────────
        private void HandleClose()
        {
            _view.SetVisible(false);
        }

        private void HandleScrollChanged(Vector2 normalizedPos)
        {
            // normalizedPos.y == 0 → bottom of scroll; trigger load when near bottom
            if (!_isLoadingMore && normalizedPos.y < 0.15f)
                LoadBatch();
        }

        // ── Data / Sorting ───────────────────────────────────────────────────
        private void Refresh()
        {
            _sortedModels = BuildSortedList();

            int totalCount     = _sortedModels.Count;
            int completedCount = AchievementManager.Instance?.CompletedCount ?? 0;
            int incompleteCount = totalCount - completedCount;

            Debug.Log($"[AchievementUI]\nSort Complete\nIncomplete : {incompleteCount}\nCompleted : {completedCount}");

            // Guard scroll events during reset
            _isLoadingMore = true;
            _loadedCount   = 0;
            _view.ClearItems();
            _view.ResetScroll();
            _isLoadingMore = false;

            var loc = LocalizationManager.Instance;
            _view.SetTitle(loc?.GetTranslation("achievement_popup_title") ?? "업적");
            _view.SetCount(completedCount, totalCount);

            LoadBatch(isInitial: true);
        }

        private List<AchievementModel> BuildSortedList()
        {
            if (AchievementManager.Instance == null) return new List<AchievementModel>();

            var all = AchievementManager.Instance.GetAllAchievements();

            // 1순위: 미달성 — 진행률 내림차순, 동률 시 ID 오름차순
            var incomplete = all
                .Where(m => !m.IsCompleted)
                .OrderByDescending(m => m.ProgressRatio)
                .ThenBy(m => m.Data.achievementID);

            // 2순위: 달성 — ID 오름차순
            var completed = all
                .Where(m => m.IsCompleted)
                .OrderBy(m => m.Data.achievementID);

            return incomplete.Concat(completed).ToList();
        }

        // ── Batch loading ────────────────────────────────────────────────────
        private void LoadBatch(bool isInitial = false)
        {
            if (_isLoadingMore) return;
            if (_loadedCount >= _sortedModels.Count) return;

            _isLoadingMore = true;

            int start = _loadedCount;
            int count = Mathf.Min(BatchSize, _sortedModels.Count - start);
            var loc   = LocalizationManager.Instance;

            for (int i = 0; i < count; i++)
            {
                var model = _sortedModels[start + i];
                BuildItemData(model, loc,
                    out string name, out string desc, out string progress);
                _view.AddItem(name, desc, progress, model.IsCompleted);
            }

            _loadedCount = start + count;
            _isLoadingMore = false;

            if (isInitial)
                Debug.Log($"[AchievementUI]\nOpen Popup\nTotal Achievement : {_sortedModels.Count}\nLoaded : {_loadedCount}");
            else
                Debug.Log($"[AchievementUI]\nLoad More\nLoaded : {_loadedCount}");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static void BuildItemData(AchievementModel model, LocalizationManager loc,
            out string name, out string desc, out string progress)
        {
            bool hidden = model.Data.isHidden && !model.IsCompleted;

            name = hidden
                ? (loc?.GetTranslation("achievement_hidden_name") ?? "???")
                : (loc?.GetTranslation(model.Data.nameLocKey) ?? model.Data.nameLocKey);

            desc = hidden
                ? (loc?.GetTranslation("achievement_hidden_desc") ?? "???")
                : (loc?.GetTranslation(model.Data.descLocKey) ?? model.Data.descLocKey);

            progress = hidden
                ? ""
                : $"{model.CurrentProgress} / {model.Data.targetValue}";
        }
    }
}
