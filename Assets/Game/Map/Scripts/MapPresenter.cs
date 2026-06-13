using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DeepEarth.Map
{
    public class MapPresenter
    {
        private static MapPresenter _instance;
        public static MapPresenter Instance => _instance;

        private readonly DepthData _model;
        private readonly MapView _view;
        private readonly MapGenerator _generator;

        public DepthData Model => _model;

        public MapPresenter(DepthData model, MapView view, MapGenerator generator)
        {
            _model = model;
            _view = view;
            _generator = generator;
            _instance = this;
        }

        public async UniTask HandleBlockMinedAsync(int newDepth)
        {
            _model.CurrentDepth = newDepth;

            // Trigger MapView to move MapRoot back by 1 unit
            var moveTcs = new UniTaskCompletionSource();
            _view.MoveMapBack(1f, 0.18f, () => moveTcs.TrySetResult());

            // Update procedural wall segments for the new depth
            _generator.UpdateMapSegments(newDepth);

            // Wait for map slide animation to complete
            await moveTcs.Task;
        }
    }
}
