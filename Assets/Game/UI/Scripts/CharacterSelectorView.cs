using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class CharacterSelectorView : MonoBehaviour
    {
        [Header("Dynamic Buttons")]
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject buttonSlotPrefab;

        public event Action<CharacterID> OnCharacterSelected;

        private readonly List<CharacterButtonSlotView> _slots = new List<CharacterButtonSlotView>();

        public void Refresh(CharacterID selectedID)
        {
            if (buttonContainer == null || buttonSlotPrefab == null) return;

            var characters = CharacterDatabase.Characters;

            while (_slots.Count > characters.Count)
            {
                var last = _slots[_slots.Count - 1];
                _slots.RemoveAt(_slots.Count - 1);
                if (last != null) Destroy(last.gameObject);
            }

            while (_slots.Count < characters.Count)
            {
                var go = Instantiate(buttonSlotPrefab, buttonContainer);
                _slots.Add(go.GetComponent<CharacterButtonSlotView>());
            }

            for (int i = 0; i < characters.Count; i++)
            {
                var data = characters[i];
                var id = data.ID;
                var slot = _slots[i];
                if (slot == null) continue;

                bool isUnlocked = CharacterManager.Instance != null && CharacterManager.Instance.IsUnlocked(id);
                bool isSelected = id == selectedID;
                string name = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetTranslation(data.NameKey)
                    : id.ToString();

                slot.SetData(name, isUnlocked, isSelected);
                slot.OnClick = () => OnCharacterSelected?.Invoke(id);
            }
        }
    }
}
