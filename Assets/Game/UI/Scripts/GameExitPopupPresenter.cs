using UnityEngine;
using Cysharp.Threading.Tasks;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // StartMenuScene 전용 "게임 완전 종료" 확인 팝업 Presenter.
    // 런 종료/정산 시스템과는 완전히 분리된 별개 기능이다 — RunEnd/RunResult/Will 정산 등을 일절 호출하지 않는다.
    public class GameExitPopupPresenter
    {
        private readonly GameExitPopupView _view;
        private bool _isOpen;
        private bool _quitRequested;

        public GameExitPopupPresenter(GameExitPopupView view)
        {
            _view = view;
            if (_view != null)
            {
                _view.OnCancelClicked += HandleCancel;
                _view.OnConfirmClicked += HandleConfirm;
            }
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnCancelClicked -= HandleCancel;
                _view.OnConfirmClicked -= HandleConfirm;
            }
        }

        // StartMenuBootstrapper의 Update()가 매 프레임 호출한다 — View는 입력을 직접 감지하지 않는다.
        public void HandleExitInput()
        {
            if (_view == null) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return; // PC ESC와 Android Back Button 모두 이 키코드로 들어온다.

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            bool isAndroid = Application.platform == RuntimePlatform.Android;
            Debug.Log(isAndroid ? "[GameExit]\nAndroid Back Detected" : "[GameExit]\nEscape Detected");
#endif

            if (_isOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (_isOpen || _view == null) return;
            _isOpen = true;
            _view.ShowAsync().Forget();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[GameExit]\nExit Popup Opened");
#endif
        }

        public void Close()
        {
            if (!_isOpen || _view == null) return;
            _isOpen = false;
            _view.HideAsync().Forget();
        }

        private void HandleCancel()
        {
            Close();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[GameExit]\nExit Cancelled");
#endif
        }

        private void HandleConfirm()
        {
            if (_quitRequested)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[GameExit]\nQuit Already Requested");
#endif
                return;
            }

            _quitRequested = true;
            _view?.SetButtonsInteractable(false);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[GameExit]\nApplication Quit Requested");
#endif
            GameExitService.QuitApplication();
        }
    }
}
