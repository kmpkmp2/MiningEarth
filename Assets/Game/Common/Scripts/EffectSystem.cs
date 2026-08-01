using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Core;
using DeepEarth.UI;
using DeepEarth.Audio;

namespace DeepEarth.Common
{
    public class EffectSystem : MonoBehaviour
    {
        private static EffectSystem _instance;
        public static EffectSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("EffectSystem");
                    _instance = go.AddComponent<EffectSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Camera _mainCamera;
        private Image _flashOverlay;
        private Canvas _uiCanvas;
        private GameObject _particlePrefab;
        private TMP_FontAsset _customFont;

        private Vector3 _originalLocalPosition;
        private Vector3 _shakeOffset = Vector3.zero;
        private Coroutine _shakeCoroutine;
        private bool _isOriginalPositionSet = false;

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            ResetCameraReference();
        }

        private void ResetCameraReference()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _originalLocalPosition = _mainCamera.transform.localPosition;
                _isOriginalPositionSet = true;
                #if UNITY_EDITOR
                Debug.Log($"[Camera] Original Position: {_originalLocalPosition}");
                #endif
            }
            else
            {
                _isOriginalPositionSet = false;
            }
            _shakeOffset = Vector3.zero;
            _shakeCoroutine = null;
        }

        private void EnsureCameraReference()
        {
            Camera current = Camera.main;
            if (current != null && (_mainCamera != current || !_isOriginalPositionSet))
            {
                _mainCamera = current;
                _originalLocalPosition = _mainCamera.transform.localPosition;
                _isOriginalPositionSet = true;
                #if UNITY_EDITOR
                Debug.Log($"[Camera] Original Position (Auto-Detected): {_originalLocalPosition}");
                #endif
            }
        }

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
            }
        }

        public void Initialize(Camera mainCamera, Canvas uiCanvas, Image flashOverlay, GameObject particlePrefab)
        {
            _mainCamera = mainCamera;
            _uiCanvas = uiCanvas;
            _flashOverlay = flashOverlay;
            _particlePrefab = particlePrefab;

            if (_mainCamera != null)
            {
                _originalLocalPosition = _mainCamera.transform.localPosition;
                _isOriginalPositionSet = true;
                #if UNITY_EDITOR
                Debug.Log($"[Camera] Original Position (Init): {_originalLocalPosition}");
                #endif
            }
        }

        public void SetCustomFont(TMP_FontAsset font)
        {
            _customFont = font;
        }

        public void ShakeCamera(float duration = 0.2f, float magnitude = 0.1f)
        {
            EnsureCameraReference();
            if (_mainCamera != null)
            {
                if (_shakeCoroutine != null)
                {
                    StopCoroutine(_shakeCoroutine);
                    _shakeCoroutine = null;
                    _shakeOffset = Vector3.zero;
                    ApplyCameraPosition();
                    #if UNITY_EDITOR
                    Debug.Log("[Camera] Reset Position due to overlap / restart");
                    #endif
                }
                _shakeCoroutine = StartCoroutine(CoShakeCamera(duration, magnitude));
            }
        }

        private IEnumerator CoShakeCamera(float duration, float magnitude)
        {
            float elapsed = 0.0f;
            #if UNITY_EDITOR
            if (_mainCamera != null)
            {
                Debug.Log($"[Camera] Original Position: {_originalLocalPosition}");
                Debug.Log($"[Camera] Current Position: {_mainCamera.transform.localPosition}");
            }
            #endif

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

                _shakeOffset = new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            ApplyCameraPosition();
            _shakeCoroutine = null;

            #if UNITY_EDITOR
            Debug.Log($"[Camera] Shake End");
            Debug.Log($"[Camera] Position Reset");
            if (_mainCamera != null)
            {
                Debug.Log($"[Camera] Current Position: {_mainCamera.transform.localPosition}");
            }
            #endif
        }

        private void LateUpdate()
        {
            ApplyCameraPosition();
        }

        private void ApplyCameraPosition()
        {
            EnsureCameraReference();
            if (_mainCamera != null && _isOriginalPositionSet)
            {
                _mainCamera.transform.localPosition = _originalLocalPosition + _shakeOffset;
            }
        }

        public void FlashScreen(Color color, float duration = 0.15f)
        {
            if (_flashOverlay != null)
            {
                StartCoroutine(CoFlashScreen(color, duration));
            }
        }

        private IEnumerator CoFlashScreen(Color color, float duration)
        {
            _flashOverlay.color = color;
            _flashOverlay.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
                _flashOverlay.color = new Color(color.r, color.g, color.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _flashOverlay.gameObject.SetActive(false);
        }

        public void SpawnDamageText(Vector3 worldPosition, string text, Color color)
        {
            if (_uiCanvas == null || _mainCamera == null)
            {
                _uiCanvas = FindFirstObjectByType<Canvas>();
                _mainCamera = Camera.main;
            }

            if (_uiCanvas == null || _mainCamera == null) return;

            // Spawn floating text on canvas
            GameObject textGo = new GameObject("DamageText");
            textGo.transform.SetParent(_uiCanvas.transform, false);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            
            if (_customFont != null)
            {
                tmp.font = _customFont;
            }
            else
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            // Positioning
            Vector2 screenPos = _mainCamera.WorldToScreenPoint(worldPosition);
            
            // Adjust position slightly randomly
            screenPos += new Vector2(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-10f, 10f));
            textGo.transform.position = screenPos;

            StartCoroutine(CoAnimateDamageText(textGo, tmp));
        }

        private IEnumerator CoAnimateDamageText(GameObject go, TextMeshProUGUI tmp)
        {
            float duration = 1.0f;
            float elapsed = 0.0f;
            Vector3 startPos = go.transform.position;
            Vector3 targetPos = startPos + new Vector3(0, 100f, 0); // Float upwards

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                go.transform.position = Vector3.Lerp(startPos, targetPos, t);
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, Mathf.Lerp(1.0f, 0.0f, t));
                go.transform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.5f, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(go);
        }

        public void SpawnHitParticles(Vector3 worldPosition, Color color)
        {
            if (_particlePrefab == null) return;

            GameObject particles = Instantiate(_particlePrefab, worldPosition, Quaternion.identity);
            var ps = particles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
                ps.Play();
            }
            Destroy(particles, 1.5f);
        }

        // 채굴 보상 아이콘이 HUD 인벤토리 버튼으로 날아가는 연출 + 획득 SFX.
        // ResourceItemView.AbsorbToAsync(GameOver 재화 흡수 연출)와 동일한 이징 방식을 일반화한 것.
        public void SpawnMiningRewardPickup(Vector3 worldStartPosition, string iconKey, int amount, RectTransform target)
        {
            SpawnMiningRewardPickupAsync(worldStartPosition, iconKey, amount, target).Forget();
        }

        private async UniTaskVoid SpawnMiningRewardPickupAsync(Vector3 worldStartPosition, string iconKey, int amount, RectTransform target)
        {
            if (_uiCanvas == null) _uiCanvas = FindFirstObjectByType<Canvas>();
            EnsureCameraReference();
            if (_uiCanvas == null || _mainCamera == null || target == null) return;

            GameObject iconGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPrefabMiningRewardIcon, _uiCanvas.transform);
            if (iconGo == null) return;

            var view = iconGo.GetComponent<MiningRewardIconView>();
            if (view == null)
            {
                Destroy(iconGo);
                return;
            }

            var rt = iconGo.GetComponent<RectTransform>();
            if (rt != null)
                rt.position = _mainCamera.WorldToScreenPoint(worldStartPosition);

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(iconKey))
            {
                try { sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey); }
                catch (Exception) { sprite = null; }
            }
            if (sprite == null)
            {
                try { sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>("Empty_item_Icon"); }
                catch (Exception) { sprite = null; }
            }

            view.SetIcon(sprite);
            view.SetAmount(amount);

            AudioManager.Instance?.PlaySFX(AddressableKeys.MiningSFXPickup);

            await view.FlyToAsync(target);
            Destroy(iconGo);
        }
    }
}
