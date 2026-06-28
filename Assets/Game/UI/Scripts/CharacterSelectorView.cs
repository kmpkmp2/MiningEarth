using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class CharacterSelectorView : MonoBehaviour
    {
        [Header("Character Buttons")]
        [SerializeField] private Button prisonerButton;
        [SerializeField] private Button mercenaryButton;
        [SerializeField] private Button minerButton;
        [SerializeField] private Button graveRobberButton;

        public event Action<CharacterID> OnCharacterSelected;

        private static readonly Color ColorSelected = new Color(0.18f, 0.77f, 0.31f);
        private static readonly Color ColorNormal   = new Color(0.2f, 0.22f, 0.25f);
        private static readonly Color ColorLocked   = new Color(0.15f, 0.15f, 0.17f);

        private void Start()
        {
            if (prisonerButton   != null) prisonerButton.onClick.AddListener(()   => OnCharacterSelected?.Invoke(CharacterID.Prisoner));
            if (mercenaryButton  != null) mercenaryButton.onClick.AddListener(()  => OnCharacterSelected?.Invoke(CharacterID.Mercenary));
            if (minerButton      != null) minerButton.onClick.AddListener(()      => OnCharacterSelected?.Invoke(CharacterID.Miner));
            if (graveRobberButton!= null) graveRobberButton.onClick.AddListener(()=> OnCharacterSelected?.Invoke(CharacterID.GraveRobber));
        }

        public void Refresh(CharacterID selectedID)
        {
            RefreshButton(prisonerButton,    CharacterID.Prisoner,   selectedID);
            RefreshButton(mercenaryButton,   CharacterID.Mercenary,  selectedID);
            RefreshButton(minerButton,       CharacterID.Miner,      selectedID);
            RefreshButton(graveRobberButton, CharacterID.GraveRobber,selectedID);
        }

        private void RefreshButton(Button btn, CharacterID id, CharacterID selectedID)
        {
            if (btn == null) return;

            bool isUnlocked = CharacterManager.Instance != null && CharacterManager.Instance.IsUnlocked(id);
            bool isSelected = id == selectedID;

            btn.interactable = isUnlocked;

            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = isSelected ? ColorSelected : (isUnlocked ? ColorNormal : ColorLocked);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && LocalizationManager.Instance != null)
            {
                var staticData = CharacterDatabase.Get(id);
                string name = staticData != null
                    ? LocalizationManager.Instance.GetTranslation(staticData.NameKey)
                    : id.ToString();
                label.text = isUnlocked ? name : $"🔒 {name}";
            }
        }
    }
}
