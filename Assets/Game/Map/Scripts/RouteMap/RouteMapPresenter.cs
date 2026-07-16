using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.UI;
using DeepEarth.Core;

namespace DeepEarth.Map
{
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
            _currentSeed     = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _completedNodes  = new HashSet<string>();
            _accessibleNodes = new HashSet<string>();
            _activeNode      = null;

            GenerateMap(_currentSeed);
            OpenFloor0();
            SaveState();

            Debug.Log($"[Map]\nRouteMapPresenter.InitializeRun  seed={_currentSeed}");
        }

        public void RestoreFromSave(MapSaveData save)
        {
            _currentSeed = save.Seed;
            GenerateMap(_currentSeed);

            _completedNodes  = new HashSet<string>(save.CompletedNodeKeys  ?? new List<string>());
            _accessibleNodes = new HashSet<string>(save.AccessibleNodeKeys ?? new List<string>());

            if (_accessibleNodes.Count == 0)
                OpenFloor0();

            Debug.Log($"[Map]\nRouteMapPresenter.RestoreFromSave  seed={_currentSeed}  completed={_completedNodes.Count}");
        }

        public void Reset()
        {
            _activeNode      = null;
            _currentMap      = null;
            _completedNodes  = new HashSet<string>();
            _accessibleNodes = new HashSet<string>();
        }

        public void ShowMap()
        {
            if (_currentMap == null || _view == null) return;
            _view.Show();
            _view.RenderAsync(_currentMap, _completedNodes, _accessibleNodes).Forget();
        }

        public void HideMap() => _view?.Hide();

        public async UniTask OnMineNodeCompleted()
        {
            if (_activeNode == null) return;
            CompleteNode($"{_activeNode.Floor}_{_activeNode.Column}");
            await UniTask.Yield();
            ShowMap();
        }

        public async UniTask OnNonMineNodeCompleted()
        {
            if (_activeNode == null) return;
            CompleteNode($"{_activeNode.Floor}_{_activeNode.Column}");
            await UniTask.Yield();
            ShowMap();
        }

        public async UniTask OnBossNodeCompleted()
        {
            await UniTask.Yield();
            InitializeRun();
            ShowMap();
        }

        // ─── Internals ──────────────────────────────────────────────────────

        private void GenerateMap(int seed)
        {
            var rng       = new SeededRandomProvider(seed);
            var generator = new MapGenerator(_gridTemplate, _roomConfig, rng);
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

        private void CompleteNode(string nodeKey)
        {
            _completedNodes.Add(nodeKey);
            _accessibleNodes.Remove(nodeKey);
            UnlockNextNodes(nodeKey);
            SaveState();
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
                if (next != null && next.IsActive)
                    _accessibleNodes.Add($"{next.Floor}_{next.Column}");
            }
        }

        private void OnNodeSelected(string nodeKey)
        {
            if (!_accessibleNodes.Contains(nodeKey)) return;

            string[] parts = nodeKey.Split('_');
            if (parts.Length < 2) return;
            if (!int.TryParse(parts[0], out int floor) || !int.TryParse(parts[1], out int col)) return;

            MapNode node = _currentMap?.GetNodeIncludingBoss(floor, col);
            if (node == null) return;

            _activeNode = node;
            HideMap();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (node.RoomType == RoomType.Boss)
                Debug.Log("[Map]\nBoss Node Clicked");
#endif
            GameManager.Instance.OnRouteNodeSelected(_activeNode);
        }

        private void SaveState()
        {
            var save = SaveManager.CurrentData;
            if (save.MapSaveData == null)
                save.MapSaveData = new MapSaveData();

            save.MapSaveData.HasActiveMap      = true;
            save.MapSaveData.Seed              = _currentSeed;
            save.MapSaveData.CompletedNodeKeys  = new List<string>(_completedNodes);
            save.MapSaveData.AccessibleNodeKeys = new List<string>(_accessibleNodes);
            SaveManager.Save();
        }

        public void Dispose()
        {
            if (_view != null)
                _view.OnNodeSelected -= OnNodeSelected;
            _instance = null;
        }
    }
}
