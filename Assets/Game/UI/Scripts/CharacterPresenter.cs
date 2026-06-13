using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    public class CharacterPresenter : IDisposable
    {
        private readonly CharacterPopupView _view;
        private CharacterID _selectedInPopupID;

        public CharacterPresenter(CharacterPopupView view)
        {
            _view = view;

            // Subscribe to view events
            _view.OnCharacterSelected += HandleCharacterSelected;
            _view.OnUnlockClicked += HandleUnlock;
            _view.OnSelectClicked += HandleSelect;
            _view.OnCloseClicked += HandleClose;

            // Subscribe to model events
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }

            // Default selection in popup is the current selected character
            _selectedInPopupID = CharacterManager.Instance.SelectedCharacterID;
            
            _view.Localize();
            Refresh();
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnCharacterSelected -= HandleCharacterSelected;
                _view.OnUnlockClicked -= HandleUnlock;
                _view.OnSelectClicked -= HandleSelect;
                _view.OnCloseClicked -= HandleClose;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        public void OpenPopup()
        {
            _selectedInPopupID = CharacterManager.Instance.SelectedCharacterID;
            _view.Open();
            Refresh();
        }

        private void HandleCharacterSelected(CharacterID id)
        {
            _selectedInPopupID = id;
            Refresh();
        }

        private void HandleUnlock()
        {
            if (CharacterManager.Instance.UnlockCharacter(_selectedInPopupID))
            {
                CharacterManager.Instance.SelectedCharacterID = _selectedInPopupID;
                Refresh();
            }
        }

        private void HandleSelect()
        {
            CharacterManager.Instance.SelectedCharacterID = _selectedInPopupID;
            Refresh();
        }

        private void HandleClose()
        {
            _view.Close();
        }

        private void HandleLanguageChanged()
        {
            _view.Localize();
            Refresh();
        }

        private void Refresh()
        {
            var staticData = CharacterDatabase.Get(_selectedInPopupID);
            if (staticData == null || LocalizationManager.Instance == null) return;

            var loc = LocalizationManager.Instance;
            string charName = loc.GetTranslation(staticData.NameKey);
            string charDesc = loc.GetTranslation(staticData.DescKey);

            bool isUnlocked = CharacterManager.Instance.IsUnlocked(_selectedInPopupID);
            bool isSelected = (CharacterManager.Instance.SelectedCharacterID == _selectedInPopupID);
            bool canUnlock = CharacterManager.Instance.CanUnlock(_selectedInPopupID);

            // Cost Text
            string costText = "";
            if (!isUnlocked && staticData.UnlockCost.Count > 0)
            {
                List<string> costItems = new List<string>();
                foreach (var kvp in staticData.UnlockCost)
                {
                    string resourceName = loc.GetTranslation("hud_" + kvp.Key.ToString().ToLower());
                    costItems.Add($"{resourceName}: {kvp.Value}");
                }
                costText = $"{loc.GetTranslation("char_cost_label")} {string.Join(" / ", costItems)}";
            }

            // Owned resources text
            var save = SaveManager.CurrentData;
            string ownedText = $"{loc.GetTranslation("char_owned_resources")} " +
                               $"{loc.GetFormatted("hud_iron", save.PersistentIron)} | " +
                               $"{loc.GetFormatted("hud_silver", save.PersistentSilver)} | " +
                               $"{loc.GetFormatted("hud_gold", save.PersistentGold)} | " +
                               $"{loc.GetFormatted("hud_diamond", save.PersistentDiamond)}";

            _view.UpdateInfo(charName, charDesc, costText, ownedText, isUnlocked, isSelected, canUnlock);
            _view.SetActiveCharacterHighlight(_selectedInPopupID);
        }
    }
}
