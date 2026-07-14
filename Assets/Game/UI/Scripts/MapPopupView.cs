using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Map;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class MapPopupView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect    scrollRect;
        [SerializeField] private RectTransform nodesContainer;
        [SerializeField] private TMP_Text      titleText;
        [SerializeField] private NodeIconData  iconData;

        [Header("Line Colors")]
        [SerializeField] private Color lineActiveColor   = new Color(1f, 1f, 0.5f, 1f);
        [SerializeField] private Color lineInactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("Layout")]
        [SerializeField] private float colSpacing   = 160f;
        [SerializeField] private float floorSpacing = 120f;
        [SerializeField] private float bottomPadding = 60f;

        public event Action<string> OnNodeSelected;

        private readonly List<MapNodeView> _nodeViews = new();
        private MapData _currentMap;

        // ─── Show / Hide ────────────────────────────────────────────────────

        public void Show()
        {
            transform.SetAsLastSibling(); // 다른 팝업/HUD 위에 표시
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ─── Render ─────────────────────────────────────────────────────────

        public async UniTask RenderAsync(MapData mapData, HashSet<string> completedKeys, HashSet<string> accessibleKeys)
        {
            ClearAll();
            _currentMap = mapData;

            if (titleText != null)
                titleText.text = "MAP";

            float contentHeight = mapData.Floors * floorSpacing + bottomPadding;
            if (nodesContainer != null)
                nodesContainer.sizeDelta = new Vector2(nodesContainer.sizeDelta.x, contentHeight);

            // ── Lines first (SiblingIndex 낮음 → 노드 뒤에 렌더링) ──
            for (int floor = 0; floor < mapData.Floors; floor++)
            {
                for (int col = 0; col < mapData.Columns; col++)
                {
                    MapNode fromNode = mapData.Grid[floor, col];
                    if (!fromNode.IsActive) continue;

                    Vector2 fromPos  = NodePosition(floor, col, mapData.Columns);
                    string  fromKey  = $"{fromNode.Floor}_{fromNode.Column}";
                    bool    fromDone = completedKeys.Contains(fromKey);

                    foreach (var conn in fromNode.OutgoingConnections)
                    {
                        if (conn.ToFloor >= mapData.Floors) continue; // Boss 연결 제외

                        Vector2 toPos = NodePosition(conn.ToFloor, conn.ToColumn, mapData.Columns);
                        bool active   = fromDone || accessibleKeys.Contains($"{conn.ToFloor}_{conn.ToColumn}");
                        Color color   = active ? lineActiveColor : lineInactiveColor;

                        var lineGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.MapLinePrefab, nodesContainer);
                        if (lineGo == null) continue;

                        lineGo.GetComponent<MapLineView>()?.Connect(fromPos, toPos, color);
                    }
                }
            }

            // ── Nodes ──
            for (int floor = 0; floor < mapData.Floors; floor++)
            {
                for (int col = 0; col < mapData.Columns; col++)
                {
                    MapNode node = mapData.Grid[floor, col];
                    if (!node.IsActive) continue;

                    string key         = $"{node.Floor}_{node.Column}";
                    bool   isCompleted = completedKeys.Contains(key);
                    bool   accessible  = accessibleKeys.Contains(key);

                    var nodeGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.MapNodePrefab, nodesContainer);
                    if (nodeGo == null) continue;

                    var rt = nodeGo.GetComponent<RectTransform>();
                    if (rt != null)
                        rt.anchoredPosition = NodePosition(floor, col, mapData.Columns);

                    var nodeView = nodeGo.GetComponent<MapNodeView>();
                    if (nodeView == null) continue;

                    Sprite icon = iconData != null ? iconData.GetIcon(node.RoomType) : null;
                    nodeView.Setup(node, icon, accessible, isCompleted);
                    nodeView.OnClicked += RaiseNodeSelected;
                    _nodeViews.Add(nodeView);
                }
            }

            // 현재 접근 가능한 가장 높은 층으로 스크롤
            ScrollToFloor(GetHighestAccessibleFloor(accessibleKeys));
        }

        public void RefreshNodeStates(HashSet<string> completedKeys, HashSet<string> accessibleKeys)
        {
            if (_currentMap == null) return;

            int idx = 0;
            for (int floor = 0; floor < _currentMap.Floors && idx < _nodeViews.Count; floor++)
            {
                for (int col = 0; col < _currentMap.Columns && idx < _nodeViews.Count; col++)
                {
                    MapNode node = _currentMap.Grid[floor, col];
                    if (!node.IsActive) continue;

                    string key         = $"{node.Floor}_{node.Column}";
                    bool   isCompleted = completedKeys.Contains(key);
                    bool   accessible  = accessibleKeys.Contains(key);

                    _nodeViews[idx].Refresh(node, accessible, isCompleted);
                    idx++;
                }
            }
        }

        // ─── Internals ──────────────────────────────────────────────────────

        private void ClearAll()
        {
            foreach (var nv in _nodeViews)
            {
                if (nv != null) nv.OnClicked -= RaiseNodeSelected;
            }
            _nodeViews.Clear();

            if (nodesContainer != null)
            {
                for (int i = nodesContainer.childCount - 1; i >= 0; i--)
                    Destroy(nodesContainer.GetChild(i).gameObject);
            }
        }

        private void ScrollToFloor(int floorIndex)
        {
            if (scrollRect == null || nodesContainer == null) return;
            Canvas.ForceUpdateCanvases();
            float contentH  = nodesContainer.sizeDelta.y;
            float targetY   = floorIndex * floorSpacing;
            float normalised = contentH > 0f ? Mathf.Clamp01(targetY / contentH) : 0f;
            scrollRect.verticalNormalizedPosition = normalised;
        }

        private Vector2 NodePosition(int floor, int col, int columns)
        {
            float halfW = (columns - 1) * colSpacing * 0.5f;
            return new Vector2(col * colSpacing - halfW, floor * floorSpacing + bottomPadding);
        }

        private static int GetHighestAccessibleFloor(HashSet<string> accessibleKeys)
        {
            int highest = 0;
            foreach (string key in accessibleKeys)
            {
                string[] parts = key.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int floor) && floor > highest)
                    highest = floor;
            }
            return highest;
        }

        protected void RaiseNodeSelected(string nodeKey) => OnNodeSelected?.Invoke(nodeKey);
    }
}
