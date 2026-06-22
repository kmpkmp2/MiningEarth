using System;
using UnityEngine;

namespace DeepEarth.Combat
{
    public class MonsterView : MonoBehaviour
    {
        public event Action OnTouched;

        [SerializeField] private MeshRenderer meshRenderer;

        private Vector3 _originalScale;
        private Vector3 _spawnWorldPosition;
        private Color _originalColor;
        private Material _instancedMaterial;

        public int SpawnIndex { get; private set; } = -1;

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

        // Must be called after transform.position is set at spawn time.
        // Stores world position so attack animation is independent of parent changes.
        public void InitializeSpawn(int spawnIndex)
        {
            SpawnIndex = spawnIndex;
            _spawnWorldPosition = transform.position;
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
            Vector3 spawnPos = _spawnWorldPosition;
            Vector3 forwardPos = spawnPos + new Vector3(0f, 0f, -1.5f);

            Debug.Log($"[Battle]\nAttack Start\nPosition : {spawnPos.x:F2},{spawnPos.y:F2},{spawnPos.z:F2}");

            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration * 0.4f)
            {
                float t = elapsed / (duration * 0.4f);
                transform.position = Vector3.Lerp(spawnPos, forwardPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration * 0.6f)
            {
                float t = elapsed / (duration * 0.6f);
                transform.position = Vector3.Lerp(forwardPos, spawnPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = spawnPos;
            Debug.Log($"[Battle]\nReturn To Spawn Position\nPosition : {spawnPos.x:F2},{spawnPos.y:F2},{spawnPos.z:F2}");
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
