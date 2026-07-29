using System;
using UnityEngine;

namespace DeepEarth.Combat
{
    public class MonsterView : MonoBehaviour
    {
        public event Action OnTouched;

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 _originalScale;
        private Vector3 _spawnWorldPosition;
        private Color _originalColor;
        private Material _instancedMaterial;

        private bool UseSprite => spriteRenderer != null;

        public int SpawnIndex { get; private set; } = -1;

        private void Awake()
        {
            _originalScale = transform.localScale;

            if (UseSprite)
            {
                _originalColor = spriteRenderer.color;
            }
            else
            {
                if (meshRenderer == null)
                    meshRenderer = GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                {
                    _instancedMaterial = meshRenderer.material;
                    _originalColor = _instancedMaterial.color;
                }
            }
        }

        private void OnEnable()
        {
            // 풀 재사용 시 코루틴 중단으로 잔류한 플래시 색상 초기화
            if (UseSprite)
            {
                if (spriteRenderer != null) spriteRenderer.color = _originalColor;
            }
            else
            {
                if (_instancedMaterial != null) _instancedMaterial.color = _originalColor;
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
            if (UseSprite)
            {
                spriteRenderer.color = flashColor;
                yield return new WaitForSeconds(duration);
                spriteRenderer.color = _originalColor;
            }
            else
            {
                if (_instancedMaterial == null) yield break;
                _instancedMaterial.color = flashColor;
                yield return new WaitForSeconds(duration);
                _instancedMaterial.color = _originalColor;
            }
        }

        // duration: 기본값 0.25f는 기존 실시간 전투(Elite 등, 인자 없이 호출)의 룬지 속도를 그대로 유지하기 위함.
        // 턴제 전투(Battle 네임스페이스)는 BattleBalanceData.monsterAttackAnimationTime을 명시적으로 전달한다.
        public void PlayAttackAnimation(float duration = 0.25f)
        {
            StartCoroutine(CoAttackLunge(duration));
        }

        private System.Collections.IEnumerator CoAttackLunge(float duration)
        {
            Vector3 spawnPos = _spawnWorldPosition;
            Vector3 forwardPos = spawnPos + new Vector3(0f, 0f, -1.5f);

            Debug.Log($"[Battle]\nAttack Start\nPosition : {spawnPos.x:F2},{spawnPos.y:F2},{spawnPos.z:F2}");

            float elapsed = 0f;

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
            if (!UseSprite && _instancedMaterial != null)
            {
                Destroy(_instancedMaterial);
            }
        }
    }
}
