using System;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class CharacterSelectorPresenter : IDisposable
    {
        private readonly CharacterSelectorView _view;

        public CharacterID SelectedCharacterID { get; private set; }
        public event Action<CharacterID> OnCharacterChanged;

        public CharacterSelectorPresenter(CharacterSelectorView view)
        {
            _view = view;
            _view.OnCharacterSelected += HandleCharacterSelected;

            SelectedCharacterID = CharacterManager.Instance != null
                ? CharacterManager.Instance.SelectedCharacterID
                : CharacterID.Prisoner;
        }

        public void Refresh()
        {
            _view?.Refresh(SelectedCharacterID);
        }

        public void SetCharacter(CharacterID id)
        {
            SelectedCharacterID = id;
            Refresh();
        }

        public void Dispose()
        {
            if (_view != null)
                _view.OnCharacterSelected -= HandleCharacterSelected;
        }

        private void HandleCharacterSelected(CharacterID id)
        {
            if (!CharacterManager.Instance.IsUnlocked(id)) return;
            SelectedCharacterID = id;
            Refresh();
            OnCharacterChanged?.Invoke(id);
        }
    }
}
