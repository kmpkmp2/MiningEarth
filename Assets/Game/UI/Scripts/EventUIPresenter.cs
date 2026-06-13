using DeepEarth.Event;
using DeepEarth.Core;
using UnityEngine;

namespace DeepEarth.UI
{
    public class EventUIPresenter
    {
        private readonly EventUIView _view;

        private EventData _currentEvent;

        public EventUIPresenter(EventUIView view)
        {
            _view = view;

            // Subscribe to UI selection
            _view.OnOptionSelected += HandleOptionSelected;

            // Subscribe to manager events
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventTriggered += HandleEventTriggered;
                EventManager.Instance.OnEventCompleted += HandleEventCompleted;
            }

            // Subscribe to language change
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }

            // Initially hide the event UI
            _view.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            _view.OnOptionSelected -= HandleOptionSelected;

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventTriggered -= HandleEventTriggered;
                EventManager.Instance.OnEventCompleted -= HandleEventCompleted;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleEventTriggered(EventData data)
        {
            _currentEvent = data;
            _view.gameObject.SetActive(true);
            _view.PopulateEvent(data);
        }

        private void HandleEventCompleted()
        {
            _currentEvent = null;
            _view.gameObject.SetActive(false);
        }

        private void HandleLanguageChanged()
        {
            if (_view.gameObject.activeSelf && _currentEvent != null)
            {
                _view.PopulateEvent(_currentEvent);
            }
        }

        private void HandleOptionSelected(int index)
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.SelectOption(index);
            }
        }
    }
}
