using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DeepEarth.UI
{
    public class EffectHUDView : MonoBehaviour
    {
        [Header("Hierarchy References")]
        [SerializeField] private Transform containerParent;
        [SerializeField] private GameObject iconPrefab;

        [Header("Tooltip Panel")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipTitle;
        [SerializeField] private TextMeshProUGUI tooltipDesc;

        private readonly List<EffectIconView> _activeIcons = new List<EffectIconView>();
        private readonly List<EffectIconView> _pool = new List<EffectIconView>();

        public IReadOnlyList<EffectIconView> ActiveIcons => _activeIcons;

        public EffectIconView GetIconFromPool()
        {
            EffectIconView view = null;
            if (_pool.Count > 0)
            {
                int lastIdx = _pool.Count - 1;
                view = _pool[lastIdx];
                _pool.RemoveAt(lastIdx);
                view.gameObject.SetActive(true);
            }
            else
            {
                if (iconPrefab != null && containerParent != null)
                {
                    var go = Instantiate(iconPrefab, containerParent);
                    view = go.GetComponent<EffectIconView>();
                }
            }

            if (view != null)
            {
                _activeIcons.Add(view);
            }
            return view;
        }

        public void ClearAll()
        {
            foreach (var icon in _activeIcons)
            {
                if (icon != null)
                {
                    icon.SetStack("");
                    icon.gameObject.SetActive(false);
                    _pool.Add(icon);
                }
            }
            _activeIcons.Clear();
            HideTooltip();
        }

        public void ShowTooltip(Vector3 iconPosition, string title, string description)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(true);
                if (tooltipTitle != null) tooltipTitle.text = title;
                if (tooltipDesc != null) tooltipDesc.text = description;

                var rt = tooltipPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.transform.position = iconPosition + new Vector3(0f, 90f, 0f);
                }
            }
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
    }
}
