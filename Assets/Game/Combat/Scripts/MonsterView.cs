using System;
using UnityEngine;

namespace DeepEarth.Combat
{
    public class MonsterView : MonoBehaviour
    {
        public event Action OnTouched;

        [SerializeField] private MeshRenderer meshRenderer;

        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private Color _originalColor;
        private Material _instancedMaterial;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _originalPosition = transform.localPosition;
            
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (meshRenderer != null)
            {
                _instancedMaterial = meshRenderer.material;
                _originalColor = _instancedMaterial.color;
            }
        }

        private void OnMouseDown()
        {
            OnTouched?.Invoke();
        }

        public void PlayHurtFeedback()
        {
            StartCoroutine(CoFlashColor(Color.red, 0.12f));
        }

        private System.Collections.IEnumerator CoFlashColor(Color flashColor, float duration)
        {
            if (_instancedMaterial == null) yield break;

            _instancedMaterial.color = flashColor;
            yield return new WaitForSeconds(duration);
            _instancedMaterial.color = _originalColor;
        }

        public void PlayAttackAnimation()
        {
            StartCoroutine(CoAttackLunge());
        }

        private System.Collections.IEnumerator CoAttackLunge()
        {
            float elapsed = 0f;
            float duration = 0.25f;
            Vector3 forwardPos = _originalPosition + new Vector3(0, 0, -1.5f); // Lunge towards camera

            while (elapsed < duration * 0.4f)
            {
                float t = elapsed / (duration * 0.4f);
                transform.localPosition = Vector3.Lerp(_originalPosition, forwardPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration * 0.6f)
            {
                float t = elapsed / (duration * 0.6f);
                transform.localPosition = Vector3.Lerp(forwardPos, _originalPosition, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = _originalPosition;
        }

        public Color GetMonsterColor()
        {
            return _originalColor;
        }

        private void OnDestroy()
        {
            if (_instancedMaterial != null)
            {
                Destroy(_instancedMaterial);
            }
        }
    }
}
