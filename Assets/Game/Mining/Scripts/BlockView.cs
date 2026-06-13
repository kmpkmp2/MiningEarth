using System;
using UnityEngine;

namespace DeepEarth.Mining
{
    public class BlockView : MonoBehaviour
    {
        public event Action OnTouched;

        [SerializeField] private MeshRenderer meshRenderer;

        private Vector3 _originalScale;
        private Color _originalColor;
        private Material _instancedMaterial;

        private void Awake()
        {
            _originalScale = transform.localScale;
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

        public void PlayHitFeedback()
        {
            StopAllCoroutines();
            StartCoroutine(CoPunchScale());
        }

        private System.Collections.IEnumerator CoPunchScale()
        {
            float elapsed = 0f;
            float duration = 0.12f;
            Vector3 targetScale = _originalScale * 1.12f;

            if (_instancedMaterial != null)
            {
                _instancedMaterial.color = Color.white;
            }

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, Mathf.Sin(t * Mathf.PI));
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = _originalScale;

            if (_instancedMaterial != null)
            {
                _instancedMaterial.color = _originalColor;
            }
        }

        public Color GetBlockColor()
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
