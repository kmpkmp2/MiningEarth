using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // In-game toast shown briefly when an achievement is completed.
    // AchievementManager.OnAchievementCompleted → ShowNotification().
    public class AchievementNotificationView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;

        private const float DisplayDuration = 4f;
        private Coroutine _hideCoroutine;

        public static AchievementNotificationView Create(Transform canvasParent)
        {
            var root = new GameObject("AchievementNotification", typeof(RectTransform));
            root.transform.SetParent(canvasParent, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot     = new Vector2(0.5f, 1f);
            rootRt.sizeDelta = new Vector2(420f, 120f);
            rootRt.anchoredPosition = new Vector2(0f, -10f);

            // Panel background
            var panelImg = root.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.06f, 0.08f, 0.94f);

            // Header "업적 달성!"
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(root.transform, false);
            var hRt = headerGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 0.6f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.offsetMin = new Vector2(12f, 0f);
            hRt.offsetMax = new Vector2(-12f, -6f);
            var hTmp = headerGo.AddComponent<TextMeshProUGUI>();
            hTmp.fontSize = 18f;
            hTmp.fontStyle = TMPro.FontStyles.Bold;
            hTmp.color = new Color(1f, 0.85f, 0.2f);
            hTmp.alignment = TMPro.TextAlignmentOptions.Center;

            // Achievement name
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(root.transform, false);
            var nRt = nameGo.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.3f);
            nRt.anchorMax = new Vector2(1f, 0.62f);
            nRt.offsetMin = new Vector2(12f, 0f);
            nRt.offsetMax = new Vector2(-12f, 0f);
            var nTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nTmp.fontSize = 16f;
            nTmp.color = Color.white;
            nTmp.alignment = TMPro.TextAlignmentOptions.Center;

            // Achievement desc
            var descGo = new GameObject("Desc", typeof(RectTransform));
            descGo.transform.SetParent(root.transform, false);
            var dRt = descGo.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 0.32f);
            dRt.offsetMin = new Vector2(12f, 4f);
            dRt.offsetMax = new Vector2(-12f, 0f);
            var dTmp = descGo.AddComponent<TextMeshProUGUI>();
            dTmp.fontSize = 13f;
            dTmp.color = new Color(0.75f, 0.75f, 0.75f);
            dTmp.alignment = TMPro.TextAlignmentOptions.Center;

            var view = root.AddComponent<AchievementNotificationView>();
            view.panel      = root;
            view.headerText = hTmp;
            view.nameText   = nTmp;
            view.descText   = dTmp;

            root.SetActive(false);
            return view;
        }

        public void ShowNotification(AchievementModel model)
        {
            if (model == null) return;

            var loc = LocalizationManager.Instance;

            if (headerText != null)
                headerText.text = loc?.GetTranslation("achievement_unlocked_header") ?? "업적 달성!";

            if (nameText != null)
                nameText.text = loc?.GetTranslation(model.Data.nameLocKey) ?? model.Data.nameLocKey;

            if (descText != null)
                descText.text = loc?.GetTranslation(model.Data.descLocKey) ?? model.Data.descLocKey;

            if (panel != null) panel.SetActive(true);

            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(CoHide());
        }

        private IEnumerator CoHide()
        {
            yield return new WaitForSeconds(DisplayDuration);
            if (panel != null) panel.SetActive(false);
            _hideCoroutine = null;
        }
    }
}
