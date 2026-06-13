using System;
using System.Collections;
using UnityEngine;

namespace DeepEarth.Map
{
    public class MapView : MonoBehaviour
    {
        [SerializeField] private Transform mapRoot;

        private Coroutine _moveCoroutine;

        public void Initialize(Transform root)
        {
            mapRoot = root;
        }

        public void MoveMapBack(float distance, float duration, Action onComplete = null)
        {
            if (mapRoot == null)
            {
                Debug.LogError("MapView: mapRoot reference is missing!");
                onComplete?.Invoke();
                return;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }
            _moveCoroutine = StartCoroutine(MoveCoroutine(distance, duration, onComplete));
        }

        private IEnumerator MoveCoroutine(float distance, float duration, Action onComplete)
        {
            Vector3 startPos = mapRoot.position;
            Vector3 targetPos = startPos + Vector3.back * distance;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smoothstep interpolation
                t = t * t * (3f - 2f * t);
                mapRoot.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            mapRoot.position = targetPos;
            _moveCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
