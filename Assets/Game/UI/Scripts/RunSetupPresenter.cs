using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class RunSetupPresenter
    {
        private readonly RunSetupPanelView _view;

        private readonly List<CharacterID> _unlockedChars   = new();
        private readonly List<string>      _unlockedPickaxes = new();

        private int _charIndex   = 0;
        private int _pickaxeIndex = 0;

        public RunSetupPresenter(RunSetupPanelView view)
        {
            _view = view;

            _view.OnStartRunClicked    += HandleStartRun;
            _view.OnBackClicked        += HandleBack;
            _view.OnCharPrevClicked    += () => NavigateChar(-1);
            _view.OnCharNextClicked    += () => NavigateChar(+1);
            _view.OnPickaxePrevClicked += () => NavigatePickaxe(-1);
            _view.OnPickaxeNextClicked += () => NavigatePickaxe(+1);

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += Refresh;
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnStartRunClicked    -= HandleStartRun;
                _view.OnBackClicked        -= HandleBack;
                _view.OnCharPrevClicked    -= () => NavigateChar(-1);
                _view.OnCharNextClicked    -= () => NavigateChar(+1);
                _view.OnPickaxePrevClicked -= () => NavigatePickaxe(-1);
                _view.OnPickaxeNextClicked -= () => NavigatePickaxe(+1);
            }
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= Refresh;
        }

        public void Open()
        {
            BuildLists();
            SyncToSaveData();
            Refresh();
        }

        private void BuildLists()
        {
            _unlockedChars.Clear();
            var saveData = SaveManager.CurrentData;
            if (saveData?.CharacterProgress != null)
            {
                foreach (var entry in saveData.CharacterProgress)
                    if (entry.IsUnlocked)
                        _unlockedChars.Add(entry.ID);
            }
            if (_unlockedChars.Count == 0)
                _unlockedChars.Add(CharacterID.Prisoner);

            _unlockedPickaxes.Clear();
            if (saveData?.UnlockedPickaxeIDs != null)
                _unlockedPickaxes.AddRange(saveData.UnlockedPickaxeIDs);
            if (_unlockedPickaxes.Count == 0)
                _unlockedPickaxes.Add("pickaxe_wood");
        }

        private void SyncToSaveData()
        {
            var saveData = SaveManager.CurrentData;

            _charIndex = _unlockedChars.IndexOf(saveData?.SelectedCharacterID ?? CharacterID.Prisoner);
            if (_charIndex < 0) _charIndex = 0;

            _pickaxeIndex = _unlockedPickaxes.IndexOf(saveData?.EquippedPickaxeID ?? "pickaxe_wood");
            if (_pickaxeIndex < 0) _pickaxeIndex = 0;
        }

        private void Refresh()
        {
            var loc = LocalizationManager.Instance;

            _view.SetTitle(loc?.GetTranslation("run_setup_title") ?? "Run Setup");
            _view.SetStartButtonLabel(loc?.GetTranslation("run_setup_start") ?? "Start Run!");

            RefreshCharDisplay();
            RefreshPickaxeDisplay();
        }

        private void RefreshCharDisplay()
        {
            if (_unlockedChars.Count == 0) return;

            var charID  = _unlockedChars[_charIndex];
            var loc     = LocalizationManager.Instance;
            string name = loc?.GetTranslation($"char_{charID.ToString().ToLower()}_name") ?? charID.ToString();
            string desc = loc?.GetTranslation($"char_{charID.ToString().ToLower()}_desc") ?? "";

            string label = _unlockedChars.Count > 1
                ? $"{name}  [{_charIndex + 1}/{_unlockedChars.Count}]"
                : name;
            _view.SetCharacter(label, desc);
        }

        private void RefreshPickaxeDisplay()
        {
            if (_unlockedPickaxes.Count == 0) return;

            var pkxID = _unlockedPickaxes[_pickaxeIndex];
            int power = PickaxeManager.Instance != null
                ? (PickaxeManager.Instance.GetPickaxeByID(pkxID)?.miningPower ?? 1)
                : 1;

            string label = _unlockedPickaxes.Count > 1
                ? $"{pkxID}  [{_pickaxeIndex + 1}/{_unlockedPickaxes.Count}]"
                : pkxID;
            _view.SetPickaxe(label, $"Mining Power: {power}");
        }

        private void NavigateChar(int delta)
        {
            if (_unlockedChars.Count == 0) return;
            _charIndex = (_charIndex + delta + _unlockedChars.Count) % _unlockedChars.Count;
            RefreshCharDisplay();
        }

        private void NavigatePickaxe(int delta)
        {
            if (_unlockedPickaxes.Count == 0) return;
            _pickaxeIndex = (_pickaxeIndex + delta + _unlockedPickaxes.Count) % _unlockedPickaxes.Count;
            RefreshPickaxeDisplay();
        }

        private void HandleStartRun()
        {
            var charID  = _unlockedChars.Count > 0 ? _unlockedChars[_charIndex] : CharacterID.Prisoner;
            var pkxID   = _unlockedPickaxes.Count > 0 ? _unlockedPickaxes[_pickaxeIndex] : "pickaxe_wood";
            RunSetupContext.MarkComplete(charID, pkxID);
            SceneManager.LoadScene(SceneNames.Loading);
        }

        private void HandleBack()
        {
            // 호출 측(StartMenuPresenter)에서 ShowMainMenu()로 처리
        }
    }
}
