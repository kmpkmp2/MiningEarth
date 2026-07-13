using DeepEarth.Audio;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class SettingsUIPresenter
    {
        private readonly SettingsUIView _view;
        private readonly GameManager _gameManager;
        private ExitRunConfirmPopupView _confirmPopup;

        public SettingsUIPresenter(SettingsUIView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            _view.OnCloseClicked += HandleClose;
            _view.OnLanguageKoClicked += HandleLanguageKo;
            _view.OnLanguageEnClicked += HandleLanguageEn;
            _view.OnMasterVolumeChanged += HandleMasterVolume;
            _view.OnBGMVolumeChanged    += HandleBGMVolume;
            _view.OnSFXVolumeChanged    += HandleSFXVolume;
            _view.OnExitRunClicked      += HandleExitRunClicked;

            _confirmPopup = _view.GetConfirmPopup();
            if (_confirmPopup != null)
            {
                _confirmPopup.OnCancelClicked  += HandleExitRunCancel;
                _confirmPopup.OnConfirmClicked += HandleExitRunConfirm;
                _confirmPopup.Hide();
            }

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += RefreshUI;

            var audioSettings = SaveManager.CurrentData.AudioSettings;
            _view.InitSliders(audioSettings.MasterVolume, audioSettings.BGMVolume, audioSettings.SFXVolume);

            // ExitRunRow는 런이 진행 중일 때만 표시
            _view.SetExitRunVisible(RunDataModel.Current != null);

            RefreshUI();
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnCloseClicked -= HandleClose;
                _view.OnLanguageKoClicked -= HandleLanguageKo;
                _view.OnLanguageEnClicked -= HandleLanguageEn;
                _view.OnMasterVolumeChanged -= HandleMasterVolume;
                _view.OnBGMVolumeChanged    -= HandleBGMVolume;
                _view.OnSFXVolumeChanged    -= HandleSFXVolume;
                _view.OnExitRunClicked      -= HandleExitRunClicked;
            }

            if (_confirmPopup != null)
            {
                _confirmPopup.OnCancelClicked  -= HandleExitRunCancel;
                _confirmPopup.OnConfirmClicked -= HandleExitRunConfirm;
            }

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= RefreshUI;
        }

        private void HandleClose()
        {
            _confirmPopup?.Hide();
            _gameManager.CloseSettings();
        }

        private void HandleLanguageKo()
        {
            LocalizationManager.Instance?.SetLanguage("ko");
        }

        private void HandleLanguageEn()
        {
            LocalizationManager.Instance?.SetLanguage("en");
        }

        private void RefreshUI()
        {
            _view.Localize();
            if (LocalizationManager.Instance != null)
                _view.UpdateVisuals(LocalizationManager.Instance.CurrentLanguageCode);
        }

        private void HandleExitRunClicked()
        {
            _confirmPopup?.Show();
        }

        private void HandleExitRunCancel()
        {
            _confirmPopup?.Hide();
        }

        private void HandleExitRunConfirm()
        {
            _confirmPopup?.Hide();
            _gameManager.AbandonRun();
        }

        private void HandleMasterVolume(float v) => AudioManager.Instance?.SetMasterVolume(v);
        private void HandleBGMVolume(float v)    => AudioManager.Instance?.SetBGMVolume(v);
        private void HandleSFXVolume(float v)    => AudioManager.Instance?.SetSFXVolume(v);
    }
}
