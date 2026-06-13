using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class CharacterPopupView : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] public GameObject popupPanel;

        [Header("Title")]
        [SerializeField] public TextMeshProUGUI titleText;

        [Header("Character Selection Buttons")]
        [SerializeField] public Button prisonerButton;
        [SerializeField] public Button mercenaryButton;
        [SerializeField] public Button minerButton;
        [SerializeField] public Button graveRobberButton;

        [Header("Character Info Panel")]
        [SerializeField] public TextMeshProUGUI charNameText;
        [SerializeField] public TextMeshProUGUI charDescText;
        [SerializeField] public TextMeshProUGUI charCostText;
        [SerializeField] public TextMeshProUGUI ownedResourcesText;

        [Header("Action Buttons")]
        [SerializeField] public Button unlockButton;
        [SerializeField] public TextMeshProUGUI unlockButtonText;
        [SerializeField] public Button selectButton;
        [SerializeField] public TextMeshProUGUI selectButtonText;
        [SerializeField] public Button closeButton;
        [SerializeField] public TextMeshProUGUI closeButtonText;

        // Events
        public event Action<CharacterID> OnCharacterSelected;
        public event Action OnUnlockClicked;
        public event Action OnSelectClicked;
        public event Action OnCloseClicked;

        private void Start()
        {
            if (prisonerButton != null) prisonerButton.onClick.AddListener(() => OnCharacterSelected?.Invoke(CharacterID.Prisoner));
            if (mercenaryButton != null) mercenaryButton.onClick.AddListener(() => OnCharacterSelected?.Invoke(CharacterID.Mercenary));
            if (minerButton != null) minerButton.onClick.AddListener(() => OnCharacterSelected?.Invoke(CharacterID.Miner));
            if (graveRobberButton != null) graveRobberButton.onClick.AddListener(() => OnCharacterSelected?.Invoke(CharacterID.GraveRobber));

            if (unlockButton != null) unlockButton.onClick.AddListener(() => OnUnlockClicked?.Invoke());
            if (selectButton != null) selectButton.onClick.AddListener(() => OnSelectClicked?.Invoke());
            if (closeButton != null) closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
        }

        public void Open()
        {
            if (popupPanel != null) popupPanel.SetActive(true);
        }

        public void Close()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        public void SetActiveCharacterHighlight(CharacterID id)
        {
            SetButtonColor(prisonerButton, id == CharacterID.Prisoner);
            SetButtonColor(mercenaryButton, id == CharacterID.Mercenary);
            SetButtonColor(minerButton, id == CharacterID.Miner);
            SetButtonColor(graveRobberButton, id == CharacterID.GraveRobber);
        }

        private void SetButtonColor(Button btn, bool isSelected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? new Color(0.18f, 0.77f, 0.31f) : new Color(0.2f, 0.22f, 0.25f);
            }
        }

        public void UpdateInfo(
            string charName, 
            string charDesc, 
            string costText, 
            string ownedText, 
            bool isUnlocked, 
            bool isSelected, 
            bool canUnlock)
        {
            if (charNameText != null) charNameText.text = charName;
            if (charDescText != null) charDescText.text = charDesc;
            if (charCostText != null) charCostText.text = costText;
            if (ownedResourcesText != null) ownedResourcesText.text = ownedText;

            // Handle Unlock button visibility/interactability
            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(!isUnlocked);
                unlockButton.interactable = canUnlock;
            }

            // Handle Select button visibility/interactability
            if (selectButton != null)
            {
                selectButton.gameObject.SetActive(isUnlocked);
                selectButton.interactable = !isSelected;
                if (selectButtonText != null && LocalizationManager.Instance != null)
                {
                    selectButtonText.text = LocalizationManager.Instance.GetTranslation(isSelected ? "char_selected" : "char_select");
                }
            }
        }

        public void Localize()
        {
            if (LocalizationManager.Instance == null) return;
            var loc = LocalizationManager.Instance;

            if (titleText != null) titleText.text = loc.GetTranslation("char_popup_title");
            if (unlockButtonText != null) unlockButtonText.text = loc.GetTranslation("char_unlock");
            if (closeButtonText != null) closeButtonText.text = loc.GetTranslation("settings_close");

            SetButtonLabel(prisonerButton, loc.GetTranslation("char_prisoner_name"));
            SetButtonLabel(mercenaryButton, loc.GetTranslation("char_mercenary_name"));
            SetButtonLabel(minerButton, loc.GetTranslation("char_miner_name"));
            SetButtonLabel(graveRobberButton, loc.GetTranslation("char_graverobber_name"));
        }

        private void SetButtonLabel(Button btn, string text)
        {
            if (btn == null) return;
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
            }
        }
    }
}
