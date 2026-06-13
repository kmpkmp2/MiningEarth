using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        }

        public void SetCustomFont(TMP_FontAsset font)
        {
            _customFont = font;
        }

        public void ShakeCamera(float duration = 0.2f, float magnitude = 0.1f)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                StartCoroutine(CoShakeCamera(duration, magnitude));
            }
        }

        private IEnumerator CoShakeCamera(float duration, float magnitude)
        {
            Vector3 originalPos = _mainCamera.transform.localPosition;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                _mainCamera.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _mainCamera.transform.localPosition = originalPos;
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
            screenPos += new Vector2(Random.Range(-30f, 30f), Random.Range(-10f, 10f));
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
    }
}
