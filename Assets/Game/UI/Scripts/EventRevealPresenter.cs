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

        public async UniTask ShowAsync(string eventName, string subtitle = null)
        {
            _view.SetEventName(eventName);
            _view.SetSubtitle(subtitle);
            await _view.PlayShowAsync();
        }

        public async UniTask HideAsync()
        {
            await _view.PlayHideAsync();
        }

        public void Dispose() { }
    }
}
