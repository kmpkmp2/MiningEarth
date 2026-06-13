using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.Map
{
    public class ThemeManager : MonoBehaviour
    {
        private static ThemeManager _instance;
        public static ThemeManager Instance => _instance;

        private ThemePresenter _presenter;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(DepthData depthModel)
        {
            var view = GetComponent<ThemeView>();
            if (view == null)
            {
                view = gameObject.AddComponent<ThemeView>();
            }

            var model = new ThemeData();
            _presenter = new ThemePresenter(model, view, depthModel);
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
