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
        [SerializeField] private float colSpacing          = 160f;
        [SerializeField] private float floorSpacing        = 120f;
        [SerializeField] private float bottomPadding       = 360f;
        [SerializeField] private float topPadding          = 300f;
        [SerializeField] private float startNodeSpacing    = 120f;
        [SerializeField] private float nodeVerticalOffset  = -3000f; // 모든 노드/라인 y 오프셋

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

        public async UniTask RenderAsync(MapData mapData, HashSet<string> completedKeys, HashSet<string> accessibleKeys, string currentPositionKey = "", int scrollTargetFloor = -1)
        {
            ClearAll();
            _currentMap = mapData;

            if (titleText != null)
                titleText.text = "MAP";

            // Content 범위: Mine Entrance(하단) → Floor 0..N-1 → Boss(상단)
            float contentHeight = (mapData.Floors + 1) * floorSpacing + startNodeSpacing + bottomPadding + topPadding;
            if (nodesContainer != null)
                nodesContainer.sizeDelta = new Vector2(nodesContainer.sizeDelta.x, contentHeight);

            // ── Mine Entrance → Floor 0 Lines (최하위 SiblingIndex) ──
            if (mapData.StartNode != null)
            {
                Vector2 startPos = StartNodePosition();
                foreach (var conn in mapData.StartNode.OutgoingConnections)
                {
                    Vector2 toPos  = NodePosition(conn.ToFloor, conn.ToColumn, mapData.Columns);
                    string  toKey  = $"{conn.ToFloor}_{conn.ToColumn}";
                    bool    active = accessibleKeys.Contains(toKey) || completedKeys.Contains(toKey);
                    Color   color  = active ? lineActiveColor : lineInactiveColor;

                    var lineGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.MapLinePrefab, nodesContainer);
                    if (lineGo == null) continue;

                    lineGo.GetComponent<MapLineView>()?.Connect(startPos, toPos, color);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log("[Map]\nMine Entrance Line Created");
#endif
                }
            }

            // ── Grid Lines (노드 뒤에 렌더링) ──
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
                        bool isBossConn = mapData.BossNode != null && conn.ToFloor == mapData.BossNode.Floor;
                        Vector2 toPos   = isBossConn
                            ? BossNodePosition(mapData.BossNode.Floor)
                            : NodePosition(conn.ToFloor, conn.ToColumn, mapData.Columns);

                        bool active = fromDone || accessibleKeys.Contains($"{conn.ToFloor}_{conn.ToColumn}");
                        Color color = active ? lineActiveColor : lineInactiveColor;

                        var lineGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.MapLinePrefab, nodesContainer);
                        if (lineGo == null) continue;

                        lineGo.GetComponent<MapLineView>()?.Connect(fromPos, toPos, color);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        if (isBossConn) Debug.Log("[Map]\nBoss Line Created");
#endif
                    }
                }
            }

            // ── Mine Entrance Node ──
            if (mapData.StartNode != null)
            {
                var nodeGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.MapNodePrefab, nodesContainer);
                if (nodeGo != null)
                {
                    var rt = nodeGo.GetComponent<RectTransform>();
                    if (rt != null)
                        rt.anchoredPosition = StartNodePosition();

                    var nodeView = nodeGo.GetComponent<MapNodeView>();
                    if (nodeView != null)
                    {
                        Sprite icon = iconData != null ? iconData.GetIcon(RoomType.Start) : null;
                        nodeView.SetupAsEntrance(mapData.StartNode, icon);
                        // Start 노드는 클릭 불가 — _nodeViews에 등록하지 않음
                    }
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Map]\nMine Entrance View Created");
#endif
            }

            // ── Grid Nodes ──
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
                    if (!string.IsNullOrEmpty(currentPositionKey) && key == currentPositionKey)
                        nodeView.SetCurrentPosition(true);
                    nodeView.OnClicked += RaiseNodeSelected;
                    _nodeViews.Add(nodeView);
                }
            }

            // ── Boss Node ──
            if (mapData.BossNode != null)
            {
                string bossKey    = $"{mapData.BossNode.Floor}_{mapData.BossNode.Column}";
                bool   isCompleted = completedKeys.Contains(bossKey);
                bool   accessible  = accessibleKeys.Contains(bossKey);

                var nodeGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.MapNodePrefab, nodesContainer);
                if (nodeGo != null)
                {
                    var rt = nodeGo.GetComponent<RectTransform>();
                    if (rt != null)
                        rt.anchoredPosition = BossNodePosition(mapData.BossNode.Floor);

                    var nodeView = nodeGo.GetComponent<MapNodeView>();
                    if (nodeView != null)
                    {
                        Sprite icon = iconData != null ? iconData.GetIcon(RoomType.Boss) : null;
                        nodeView.Setup(mapData.BossNode, icon, accessible, isCompleted);
                        nodeView.OnClicked += RaiseNodeSelected;
                        _nodeViews.Add(nodeView);
                    }
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Map]\nBoss Node View Created");
#endif
            }

            // 완료 Floor+1 위치로 스크롤 (미전달 시 접근 가능한 가장 높은 층으로 폴백)
            int targetFloor = scrollTargetFloor >= 0 ? scrollTargetFloor : GetHighestAccessibleFloor(accessibleKeys);
            ScrollToFloor(targetFloor);
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

            // Boss node (마지막 _nodeViews 엔트리)
            if (_currentMap.BossNode != null && idx < _nodeViews.Count)
            {
                string bossKey    = $"{_currentMap.BossNode.Floor}_{_currentMap.BossNode.Column}";
                bool   isCompleted = completedKeys.Contains(bossKey);
                bool   accessible  = accessibleKeys.Contains(bossKey);
                _nodeViews[idx].Refresh(_currentMap.BossNode, accessible, isCompleted);
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

            // floor 0 이하(게임 시작) → Mine Entrance가 화면 하단에 보이도록 완전한 Bottom
            if (floorIndex <= 0)
            {
                scrollRect.verticalNormalizedPosition = 0f;
                return;
            }

            float contentH    = nodesContainer.sizeDelta.y;
            float viewportH   = scrollRect.viewport != null ? scrollRect.viewport.rect.height : 0f;
            float scrollableH = Mathf.Max(0f, contentH - viewportH);
            // nodeVerticalOffset은 순수 시각 오프셋 — contentH 기준 상대 위치에서 제외
            float nodeRelY = floorIndex * floorSpacing + startNodeSpacing + bottomPadding;
            float centerY  = nodeRelY - viewportH * 0.5f;
            scrollRect.verticalNormalizedPosition = scrollableH > 0f ? Mathf.Clamp01(centerY / scrollableH) : 0f;
        }

        private Vector2 NodePosition(int floor, int col, int columns)
        {
            float halfW = (columns - 1) * colSpacing * 0.5f;
            return new Vector2(col * colSpacing - halfW,
                               floor * floorSpacing + startNodeSpacing + bottomPadding + nodeVerticalOffset);
        }

        private Vector2 StartNodePosition()
        {
            return new Vector2(0f, bottomPadding + nodeVerticalOffset);
        }

        private Vector2 BossNodePosition(int bossFloor)
        {
            return new Vector2(0f, bossFloor * floorSpacing + startNodeSpacing + bottomPadding + nodeVerticalOffset);
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
