using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepEarth.Common
{
    // 기준 해상도/종횡비를 유지하는 고정 렌더링 영역(SafeBox) 관리자.
    // 책임은 Viewport(Camera.rect) / UI Safe 영역(RectTransform anchor) 계산·적용까지이며,
    // Camera Position/Rotation/Gameplay 로직(EffectSystem의 흔들림 등)은 건드리지 않는다 —
    // 두 시스템은 서로 다른 Camera 프로퍼티(rect vs localPosition)를 다루므로 충돌하지 않는다.
    public class SafeBoxManager : MonoBehaviour
    {
        private static SafeBoxManager _instance;
        public static SafeBoxManager Instance => _instance;

        [Header("Reference Resolution (코드/인스펙터에서 쉽게 변경 가능)")]
        [SerializeField] private Vector2Int referenceResolution = new Vector2Int(1080, 1920);

        [Header("Debug (Read-Only, Refresh 시 자동 갱신)")]
        [SerializeField] private Vector2Int _debugCurrentScreenResolution;
        [SerializeField] private Rect _debugSafeBoxViewportRect;
        [SerializeField] private Rect _debugSafeBoxScreenRect;

        private Camera _gameplayCamera;
        private Camera _backgroundCamera;
        private RectTransform _uiSafeBoxRoot;

        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        public Vector2Int ReferenceResolution => referenceResolution;
        public float TargetAspect => (float)referenceResolution.x / referenceResolution.y;
        public Rect SafeBoxViewportRect { get; private set; } = new Rect(0f, 0f, 1f, 1f);
        public Rect SafeBoxScreenRect { get; private set; }
        public Vector2Int SafeBoxPixelSize => new Vector2Int(Mathf.RoundToInt(SafeBoxScreenRect.width), Mathf.RoundToInt(SafeBoxScreenRect.height));
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // 씬 전환 시 이전 씬의 Camera/UI 참조가 파괴된 채로 남지 않도록 초기화.
        // 새 씬의 부트스트랩(예: GameBootstrap)이 곧이어 Initialize()를 다시 호출한다.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _gameplayCamera = null;
            _backgroundCamera = null;
            _uiSafeBoxRoot = null;
            IsInitialized = false;
        }

        // 씬 부트스트랩이 자신의 Camera/UI Root를 등록할 때 호출한다. backgroundCamera는 선택 사항(letterbox 배경용).
        public void Initialize(Camera gameplayCamera, Camera backgroundCamera, RectTransform uiSafeBoxRoot)
        {
            _gameplayCamera = gameplayCamera;
            _backgroundCamera = backgroundCamera;
            _uiSafeBoxRoot = uiSafeBoxRoot;
            IsInitialized = _gameplayCamera != null;

            _lastScreenWidth = -1;
            _lastScreenHeight = -1;
            Refresh();
        }

        private void Update()
        {
            if (!IsInitialized) return;

            // 매 프레임 재계산 금지 — 실제로 Screen 해상도가 바뀐 경우에만 Refresh (해상도/Orientation 변경 대응 겸용).
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                Refresh();
        }

        public void Refresh()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            float screenAspect = (float)Screen.width / Screen.height;
            float targetAspect = TargetAspect;

            Rect viewportRect;
            if (screenAspect > targetAspect)
            {
                // 화면이 기준보다 넓다 → 높이를 꽉 채우고 좌우를 SafeBox 밖으로 둔다(Pillarbox).
                float widthNormalized = targetAspect / screenAspect;
                float xOffset = (1f - widthNormalized) * 0.5f;
                viewportRect = new Rect(xOffset, 0f, widthNormalized, 1f);
            }
            else if (screenAspect < targetAspect)
            {
                // 화면이 기준보다 좁다 → 너비를 꽉 채우고 위아래를 SafeBox 밖으로 둔다(Letterbox).
                float heightNormalized = screenAspect / targetAspect;
                float yOffset = (1f - heightNormalized) * 0.5f;
                viewportRect = new Rect(0f, yOffset, 1f, heightNormalized);
            }
            else
            {
                viewportRect = new Rect(0f, 0f, 1f, 1f);
            }

            SafeBoxViewportRect = viewportRect;
            SafeBoxScreenRect = new Rect(
                viewportRect.x * Screen.width,
                viewportRect.y * Screen.height,
                viewportRect.width * Screen.width,
                viewportRect.height * Screen.height);

            // Camera: rect만 적용 — position/rotation/FOV/orthographicSize는 절대 건드리지 않는다.
            // Perspective 카메라는 Camera.aspect가 pixelRect(=rect x Screen)에서 자동 파생되므로
            // FOV를 그대로 둬도 SafeBox 영역 안에서는 항상 동일한 논리적 시야를 유지한다.
            if (_gameplayCamera != null)
                _gameplayCamera.rect = viewportRect;

            // 배경 카메라는 항상 전체 화면을 SolidColor로만 클리어 — SafeBox 밖 여백 색상 담당.
            if (_backgroundCamera != null)
                _backgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);

            if (_uiSafeBoxRoot != null)
            {
                _uiSafeBoxRoot.anchorMin = new Vector2(viewportRect.x, viewportRect.y);
                _uiSafeBoxRoot.anchorMax = new Vector2(viewportRect.x + viewportRect.width, viewportRect.y + viewportRect.height);
                _uiSafeBoxRoot.offsetMin = Vector2.zero;
                _uiSafeBoxRoot.offsetMax = Vector2.zero;
            }

            _debugCurrentScreenResolution = new Vector2Int(Screen.width, Screen.height);
            _debugSafeBoxViewportRect = SafeBoxViewportRect;
            _debugSafeBoxScreenRect = SafeBoxScreenRect;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[SafeBox]\n" +
                $"Screen: {Screen.width}x{Screen.height}\n" +
                $"Reference: {referenceResolution.x}x{referenceResolution.y}\n" +
                $"Aspect: {targetAspect:F4} (Screen: {screenAspect:F4})\n" +
                $"Viewport: {viewportRect}\n" +
                $"Pixel Rect: {SafeBoxScreenRect}");
#endif
        }
    }
}
