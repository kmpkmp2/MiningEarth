using UnityEngine;
using DeepEarth.UI;

namespace DeepEarth.Core
{
    public class StartMenuBootstrapper : MonoBehaviour
    {
        [SerializeField] private StartMenuUIView view;

        private StartMenuPresenter _presenter;

        private void Start()
        {
            if (view == null)
            {
                view = FindAnyObjectByType<StartMenuUIView>();
            }

            if (view != null)
            {
                _presenter = new StartMenuPresenter(view);
            }
            else
            {
                Debug.LogError("StartMenuBootstrapper: StartMenuUIView is not assigned and could not be found in the scene!");
            }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
