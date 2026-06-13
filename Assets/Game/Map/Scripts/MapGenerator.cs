using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Map
{
    public class MapGenerator : MonoBehaviour
    {
        private static MapGenerator _instance;
        public static MapGenerator Instance => _instance;

        [SerializeField] private Transform mapRoot;
        [SerializeField] private Transform floorParent;
        [SerializeField] private Transform leftWallParent;
        [SerializeField] private Transform rightWallParent;
        [SerializeField] private Transform ceilingParent;

        private readonly Dictionary<int, WallSegmentView> _activeSegments = new Dictionary<int, WallSegmentView>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        public void Initialize(Transform root, Transform floor, Transform leftWall, Transform rightWall, Transform ceiling)
        {
            mapRoot = root;
            floorParent = floor;
            leftWallParent = leftWall;
            rightWallParent = rightWall;
            ceilingParent = ceiling;
        }

        public void ResetGenerator()
        {
            ClearMap();
            if (mapRoot != null)
            {
                mapRoot.position = Vector3.zero;
            }
            UpdateMapSegments(0);
        }

        public void ClearMap()
        {
            foreach (var segment in _activeSegments.Values)
            {
                if (segment != null)
                {
                    PoolSystem.Instance.Return(segment.gameObject);
                }
            }
            _activeSegments.Clear();
        }

        public void UpdateMapSegments(int depth)
        {
            if (mapRoot == null) return;

            // Retrieve active wall material from ThemePresenter
            Material wallMat = null;
            if (ThemePresenter.Instance != null && ThemePresenter.Instance.Model != null)
            {
                wallMat = ThemePresenter.Instance.Model.WallMaterial;
            }

            float segmentLength = 2f;
            // Visible range of local Z is [depth - 2, depth + 14]
            int startIndex = Mathf.FloorToInt((depth - 2) / segmentLength);
            int endIndex = Mathf.CeilToInt((depth + 14) / segmentLength);

            // Spawn segments in range
            for (int i = startIndex; i <= endIndex; i++)
            {
                float localZ = i * segmentLength;
                if (!_activeSegments.ContainsKey(i))
                {
                    SpawnSegmentAsync(i, localZ, wallMat).Forget();
                }
                else if (_activeSegments[i] != null && wallMat != null)
                {
                    _activeSegments[i].SetMaterial(wallMat);
                }
            }

            // Recycle off-screen segments (world Z < -4)
            List<int> keysToRemove = new List<int>();
            foreach (var kvp in _activeSegments)
            {
                if (kvp.Value == null) continue;

                float localZ = kvp.Key * segmentLength;
                float worldZ = localZ + mapRoot.position.z;
                if (worldZ < -4f)
                {
                    PoolSystem.Instance.Return(kvp.Value.gameObject);
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _activeSegments.Remove(key);
            }
        }

        private async UniTaskVoid SpawnSegmentAsync(int index, float localZ, Material wallMat)
        {
            // Register placeholder to prevent double spawning
            _activeSegments[index] = null;

            GameObject segmentGo = await PoolSystem.Instance.GetAsync(AddressableKeys.MapWallSegment, mapRoot);
            if (segmentGo == null)
            {
                _activeSegments.Remove(index);
                return;
            }

            // Parent segment to MapRoot
            segmentGo.transform.SetParent(mapRoot, false);
            segmentGo.transform.localPosition = new Vector3(0, 0, localZ);
            segmentGo.transform.localRotation = Quaternion.identity;

            var view = segmentGo.GetComponent<WallSegmentView>();
            if (view != null)
            {
                if (wallMat != null)
                {
                    view.SetMaterial(wallMat);
                }
                _activeSegments[index] = view;
            }
            else
            {
                PoolSystem.Instance.Return(segmentGo);
                _activeSegments.Remove(index);
            }
        }
    }
}
