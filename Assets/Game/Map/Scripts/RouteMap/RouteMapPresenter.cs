using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.UI;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.Map
{
    /// <summary>
    /// Manages the route-map run loop:
    ///   Map 표시 → 노드 선택 → GameManager 위임 → 노드 완료 → Map 복귀 (반복)
    ///   Boss 완료 → 다음 50층 Map 생성 → Map 표시
    ///
    /// MVP 역할: Presenter — View(MapPopupView)와 Model(MapData/MapSaveData) 사이 중재.
    /// 게임 로직은 GameManager로, 저장은 SaveManager로 위임.
    /// </summary>
    public class RouteMapPresenter : IDisposable
    {
        private static RouteMapPresenter _instance;
        public static RouteMapPresenter Instance => _instance;

        private readonly MapPopupView         _view;
        private readonly DefaultGridTemplate  _gridTemplate;
        private readonly RoomGenerationConfig _roomConfig;

        private MapData         _currentMap;
        private HashSet<string> _completedNodes  = new();
        private HashSet<string> _accessibleNodes = new();
        private MapNode         _activeNode;
        private int             _currentSeed;
        private string          _currentPositionKey = "";
        private int             _currentFloor       = -1; // 마지막으로 완료된 노드의 Floor (-1 = 아직 없음)
        private int             _autoTraverseCount  = 0;  // 현재 Auto Traverse 체인 카운트

        public MapNode ActiveNode => _activeNode;
        public MapData CurrentMap => _currentMap;

        public RouteMapPresenter(MapPopupView view, DefaultGridTemplate template, RoomGenerationConfig roomConfig)
        {
            _view         = view;
            _gridTemplate = template;
            _roomConfig   = roomConfig;
            _instance     = this;

            if (_view != null)
                _view.OnNodeSelected += OnNodeSelected;
        }

        // ─── Public API ─────────────────────────────────────────────────────

        public void InitializeRun()
        {
            _currentSeed        = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _completedNodes     = new HashSet<string>();
            _accessibleNodes    = new HashSet<string>();
            _activeNode         = null;
            _currentPositionKey = "";
            _currentFloor       = -1;
            _autoTraverseCount  = 0;

            GenerateMap(_currentSeed);
            OpenFloor0();
            SaveState();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Run]\nNext Map Generated\nSeed : {_currentSeed}");
#endif
        }

        public void RestoreFromSave(MapSaveData save)
        {
            _currentSeed = save.Seed;
            GenerateMap(_currentSeed);

            _completedNodes  = new HashSet<string>(save.CompletedNodeKeys  ?? new List<string>());
            _accessibleNodes = new HashSet<string>(save.AccessibleNodeKeys ?? new List<string>());

            if (_accessibleNodes.Count == 0)
                OpenFloor0();

            // 앱 종료 시 노드 진행 중이었다면 해당 노드를 선택 가능 상태로 복원
            if (!string.IsNullOrEmpty(save.CurrentNodeKey) && !_completedNodes.Contains(save.CurrentNodeKey))
                _accessibleNodes.Add(save.CurrentNodeKey);

            _currentPositionKey = save.CurrentPositionKey ?? "";
            _currentFloor       = save.CurrentFloor;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Run]\nMap Restored\nSeed : {_currentSeed}  Completed : {_completedNodes.Count}  Floor : {_currentFloor}");
#endif
        }

        public void Reset()
        {
            _activeNode        = null;
            _currentMap        = null;
            _completedNodes    = new HashSet<string>();
            _accessibleNodes   = new HashSet<string>();
            _currentFloor      = -1;
            _autoTraverseCount = 0;
        }

        public void ShowMap()
        {
            if (_currentMap == null || _view == null) return;
            _view.Show();
            int scrollTarget = Mathf.Max(0, _currentFloor + 1);
            _view.RenderAsync(_currentMap, _completedNodes, GetSelectableNodes(), _currentPositionKey, scrollTarget).Forget();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Run]\nMap Opened");
#endif
        }

        public void HideMap() => _view?.Hide();

        public async UniTask OnMineNodeCompleted()
        {
            if (_activeNode == null) return;
            CompleteCurrentNode();
            await UniTask.Yield();
            await ProcessNodeCompletionAsync();
        }

        public async UniTask OnNonMineNodeCompleted()
        {
            if (_activeNode == null) return;
            CompleteCurrentNode();
            await UniTask.Yield();
            await ProcessNodeCompletionAsync();
        }

        public async UniTask OnBossNodeCompleted()
        {
            await UniTask.Yield();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Run]\nBoss Cleared");
#endif

            // MapIndex 증가: 다음 50층 구간 진입
            var save = SaveManager.CurrentData;
            if (save.MapSaveData != null)
                save.MapSaveData.MapIndex++;

            InitializeRun();
            ShowMap();
        }

        // ─── Internals ──────────────────────────────────────────────────────

        private void GenerateMap(int seed)
        {
            var rng = new SeededRandomProvider(seed);

            // 시작 유물: 낡은 나침반 (Adventurer) — Event 노드 등장확률 소폭 가산
            float eventWeightMultiplier = 1f + (StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetEventChoiceRollBonus() : 0f);

            var generator = new MapGenerator(_gridTemplate, _roomConfig, rng, eventWeightMultiplier);
            _currentMap   = generator.Generate(seed);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_currentMap.StartNode != null)
                Debug.Log("[Map]\nMine Entrance Added To Presenter");
#endif
        }

        private void OpenFloor0()
        {
            if (_currentMap == null) return;
            for (int col = 0; col < _currentMap.Columns; col++)
            {
                MapNode node = _currentMap.Grid[0, col];
                if (node.IsActive)
                    _accessibleNodes.Add($"0_{col}");
            }
        }

        private void CompleteCurrentNode()
        {
            if (_activeNode == null) return;
            string key = $"{_activeNode.Floor}_{_activeNode.Column}";
            _completedNodes.Add(key);
            _accessibleNodes.Remove(key);
            _currentPositionKey = key;
            _currentFloor       = _activeNode.Floor;
            UnlockNextNodes(key);
            SaveState();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Run]\nNode Completed\n{_activeNode.RoomType} [{key}]\n[Run]\nCurrent Node Updated\nPosition : {_currentPositionKey}  Floor : {_currentFloor}");
#endif
        }

        private void UnlockNextNodes(string completedKey)
        {
            if (_currentMap == null) return;

            string[] parts = completedKey.Split('_');
            if (parts.Length < 2) return;
            if (!int.TryParse(parts[0], out int floor) || !int.TryParse(parts[1], out int col)) return;

            MapNode node = _currentMap.GetNodeIncludingBoss(floor, col);
            if (node == null) return;

            foreach (var conn in node.OutgoingConnections)
            {
                MapNode next = _currentMap.GetNodeIncludingBoss(conn.ToFloor, conn.ToColumn);
                if (next == null || !next.IsActive) continue;

                string nextKey = $"{next.Floor}_{next.Column}";
                _accessibleNodes.Add(nextKey);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Map]\nNext Node Activated\n  Floor {next.Floor} Col {next.Column} : {next.RoomType}");
#endif
            }
        }

        private void OnNodeSelected(string nodeKey)
        {
            if (!_accessibleNodes.Contains(nodeKey)) return;

            string[] parts = nodeKey.Split('_');
            if (parts.Length < 2) return;
            if (!int.TryParse(parts[0], out int floor) || !int.TryParse(parts[1], out int col)) return;

            // Floor 제한: CurrentFloor 이하 노드는 선택 불가
            if (floor <= _currentFloor) return;

            MapNode node = _currentMap?.GetNodeIncludingBoss(floor, col);
            if (node == null) return;

            _activeNode = node;

            var nodeData = new NodeData(
                nodeKey:     nodeKey,
                nodeType:    node.RoomType,
                floor:       node.Floor,
                column:      node.Column,
                contentSeed: _currentSeed ^ (node.Floor * 31 + node.Column)
            );

            // 앱 종료 대비: 선택된 노드를 저장 (완료 시 클리어)
            SaveCurrentNode(nodeKey);

            HideMap();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Run]\nNode Selected\nNodeType : {node.RoomType}");
#endif

            GameManager.Instance.OnRouteNodeSelected(nodeData);
        }

        private void SaveCurrentNode(string nodeKey)
        {
            var save = SaveManager.CurrentData;
            if (save.MapSaveData == null) save.MapSaveData = new MapSaveData();
            save.MapSaveData.CurrentNodeKey = nodeKey;
            SaveManager.Save();
        }

        private void SaveState()
        {
            var save = SaveManager.CurrentData;
            if (save.MapSaveData == null)
                save.MapSaveData = new MapSaveData();

            save.MapSaveData.HasActiveMap        = true;
            save.MapSaveData.Seed                = _currentSeed;
            save.MapSaveData.CompletedNodeKeys   = new List<string>(_completedNodes);
            save.MapSaveData.AccessibleNodeKeys  = new List<string>(_accessibleNodes);
            save.MapSaveData.CurrentNodeKey      = "";
            save.MapSaveData.CurrentPositionKey  = _currentPositionKey;
            save.MapSaveData.CurrentFloor        = _currentFloor;
            SaveManager.Save();
        }

        // ─── Auto Traverse ──────────────────────────────────────────────────

        private async UniTask ProcessNodeCompletionAsync()
        {
            bool willAutoTraverse = ShouldAutoTraverse();

            if (willAutoTraverse) _autoTraverseCount++;
            else                  _autoTraverseCount = 0;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            LogMapProgress(willAutoTraverse);
#endif

            if (willAutoTraverse)
            {
                var nextNode = GetAutoTraverseTarget();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Run]\nAuto Traverse\nCount : {_autoTraverseCount}  → {nextNode.RoomType} [{nextNode.Floor}_{nextNode.Column}]");
#endif
                NavigateToNode(nextNode);
            }
            else
            {
                // 다음 노드가 채광 노드가 아니라서 맵 팝업으로 돌아가는 경우 — 방금 채굴한 광물/아이템이
                // 인벤토리로 날아가는 연출(EffectSystem.SpawnMiningRewardPickup)이 아직 진행 중이라면
                // 그 연출이 끝날 때까지 기다렸다가 팝업을 띄운다. 연출이 없으면 즉시 통과.
                if (EffectSystem.Instance != null)
                    await EffectSystem.Instance.WaitForMiningRewardPickupAsync();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Run]\nReturn To Map");
#endif
                ShowMap();
            }
        }

        private bool ShouldAutoTraverse()
        {
            if (_activeNode == null || _currentMap == null) return false;
            if (_activeNode.OutgoingConnections.Count != 1) return false;

            var conn     = _activeNode.OutgoingConnections[0];
            var nextNode = _currentMap.GetNodeIncludingBoss(conn.ToFloor, conn.ToColumn);

            if (nextNode == null || !nextNode.IsActive)           return false;
            if (nextNode.RoomType != RoomType.Mine)               return false;

            return true;
        }

        private MapNode GetAutoTraverseTarget()
        {
            var conn = _activeNode.OutgoingConnections[0];
            return _currentMap.GetNodeIncludingBoss(conn.ToFloor, conn.ToColumn);
        }

        private void NavigateToNode(MapNode node)
        {
            _activeNode = node;
            string nodeKey = $"{node.Floor}_{node.Column}";

            var nodeData = new NodeData(
                nodeKey:     nodeKey,
                nodeType:    node.RoomType,
                floor:       node.Floor,
                column:      node.Column,
                contentSeed: _currentSeed ^ (node.Floor * 31 + node.Column)
            );

            SaveCurrentNode(nodeKey);
            GameManager.Instance.OnRouteNodeSelected(nodeData);
        }

        // ─── Floor Filter ────────────────────────────────────────────────────

        private HashSet<string> GetSelectableNodes()
        {
            var selectable = new HashSet<string>();
            foreach (var key in _accessibleNodes)
            {
                var parts = key.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int floor) && floor > _currentFloor)
                    selectable.Add(key);
            }
            return selectable;
        }

        // ─── Debug ───────────────────────────────────────────────────────────

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void LogMapProgress(bool willAutoTraverse)
        {
            if (_activeNode == null) return;

            var selectable     = GetSelectableNodes();
            var disabledFloors = new System.Collections.Generic.HashSet<int>();
            foreach (var key in _accessibleNodes)
            {
                var p = key.Split('_');
                if (p.Length >= 2 && int.TryParse(p[0], out int f) && f <= _currentFloor)
                    disabledFloors.Add(f);
            }

            string disabledStr = disabledFloors.Count > 0 ? string.Join(", ", disabledFloors) : "-";

            Debug.Log(
                $"[Map]\n===== MAP PROGRESS =====\n" +
                $"Current Node\n" +
                $"  Floor : {_activeNode.Floor}\n" +
                $"  RoomType : {_activeNode.RoomType}\n" +
                $"  OutConnection Count : {_activeNode.OutgoingConnections.Count}\n" +
                $"Auto Traverse : {willAutoTraverse}\n" +
                $"Auto Traverse Count : {_autoTraverseCount}\n" +
                $"Current Floor : {_currentFloor}\n" +
                $"Selectable Node Count : {selectable.Count}\n" +
                $"Disabled Floors : {disabledStr}"
            );
        }
#endif

        public void Dispose()
        {
            if (_view != null)
                _view.OnNodeSelected -= OnNodeSelected;
            _instance = null;
        }
    }
}
