using Cysharp.Threading.Tasks;

namespace DeepEarth.UI
{
    public class EventRevealPresenter
    {
        private readonly EventRevealView _view;

        public EventRevealPresenter(EventRevealView view)
        {
            _view = view;
            _view.gameObject.SetActive(false);
        }

        public async UniTask ShowAsync(string eventName)
        {
            _view.SetEventName(eventName);
            await _view.PlayShowAsync();
        }

        public async UniTask HideAsync()
        {
            await _view.PlayHideAsync();
        }

        public void Dispose() { }
    }
}
