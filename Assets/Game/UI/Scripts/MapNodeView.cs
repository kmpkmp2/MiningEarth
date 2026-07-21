using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DeepEarth.Map;

namespace DeepEarth.UI
{
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] private Button     button;
        [SerializeField] private Image      backgroundImage;
        [SerializeField] private Image      iconImage;
        [SerializeField] private GameObject completedMark;
        [SerializeField] private GameObject lockedOverlay;

        [SerializeField] private Color accessibleColor = Color.white;
        [SerializeField] private Color completedColor  = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color lockedColor     = new Color(0.25f, 0.25f, 0.25f, 1f);

        [Header("Glow / Pulse")]
        [SerializeField] private GameObject glowObject;
        [SerializeField] private GameObject currentPositionIndicator; // 현재 위치 링 — Prefab에서 직접 참조
        [SerializeField] private float      pulseSpeed  = 1.2f;
        [SerializeField] private float      pulseAmount = 0.08f;

        public event Action<string> OnClicked;
        public string NodeKey { get; private set; }

        private CancellationTokenSource _pulseCts;

        private void Awake()
        {
            button?.onClick.AddListener(HandleClick);
        }

        // ─── 일반 노드 Setup ────────────────────────────────────────────────

        public void Setup(MapNode data, Sprite icon, bool isAccessible, bool isCompleted)
        {
            NodeKey = $"{data.Floor}_{data.Column}";

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null && data.RoomType != RoomType.Mine);
            }

            if (currentPositionIndicator != null) currentPositionIndicator.SetActive(false);
            UpdateVisuals(isAccessible, isCompleted);
        }

        public void Refresh(MapNode data, bool isAccessible, bool isCompleted)
        {
            UpdateVisuals(isAccessible, isCompleted);
        }

        /// <summary>현재 위치 링 표시 여부. Setup() 이후 별도로 호출.</summary>
        public void SetCurrentPosition(bool isCurrent)
        {
            if (currentPositionIndicator != null) currentPositionIndicator.SetActive(isCurrent);
        }

        // ─── Mine Entrance Setup ────────────────────────────────────────────

        /// <summary>Mine Entrance 전용 초기화. 클릭 불가 / Glow + Pulse 활성화.</summary>
        public void SetupAsEntrance(MapNode data, Sprite icon)
        {
            NodeKey = $"{data.Floor}_{data.Column}";

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }

            if (button          != null) button.interactable = false;
            if (lockedOverlay   != null) lockedOverlay.SetActive(false);
            if (completedMark   != null) completedMark.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = accessibleColor;
            if (glowObject      != null) glowObject.SetActive(true);

            if (currentPositionIndicator != null) currentPositionIndicator.SetActive(false);
            StartPulse();
        }

        // ─── Internals ──────────────────────────────────────────────────────

        private void UpdateVisuals(bool isAccessible, bool isCompleted)
        {
            bool locked = !isAccessible && !isCompleted;

            if (completedMark   != null) completedMark.SetActive(isCompleted);
            if (lockedOverlay   != null) lockedOverlay.SetActive(locked);

            if (button != null)
                button.interactable = isAccessible && !isCompleted;

            if (backgroundImage != null)
            {
                backgroundImage.color = isCompleted ? completedColor
                                      : locked      ? lockedColor
                                      :               accessibleColor;
            }

            // Glow + Pulse: 선택 가능 노드만 활성화
            if (isAccessible && !isCompleted)
            {
                if (glowObject != null) glowObject.SetActive(true);
                StartPulse();
            }
            else
            {
                if (glowObject != null) glowObject.SetActive(false);
                StopPulse();
            }
        }

        private void HandleClick() => PlaySelectAnimationAsync().Forget();

        private async UniTaskVoid PlaySelectAnimationAsync()
        {
            if (button != null) button.interactable = false;

            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                const float duration = 0.15f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float scale = Mathf.Lerp(1f, 0.85f, t);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                rt.localScale = Vector3.one;
            }

            OnClicked?.Invoke(NodeKey);
        }

        private void StartPulse()
        {
            StopPulse();
            _pulseCts = new CancellationTokenSource();
            PulseAsync(_pulseCts.Token).Forget();
        }

        private void StopPulse()
        {
            _pulseCts?.Cancel();
            _pulseCts?.Dispose();
            _pulseCts = null;

            var rt = GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;
        }

        private async UniTaskVoid PulseAsync(CancellationToken token)
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            float elapsed = 0f;
            while (!token.IsCancellationRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = 1f + Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
                rt.localScale = new Vector3(scale, scale, 1f);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }
        }

        private void OnDestroy()
        {
            button?.onClick.RemoveListener(HandleClick);
            StopPulse();
        }
    }
}
