using Cysharp.Threading.Tasks;
using DeepEarth.Audio;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.UI;

namespace DeepEarth.Battle
{
    // Player/Monster Turn 배너 표시 + 전환 SFX 담당.
    public class TurnPresenter
    {
        private readonly TurnView _view;

        public TurnPresenter(TurnView view)
        {
            _view = view;
        }

        public async UniTask ShowPlayerTurnAsync()
        {
            AudioManager.Instance?.PlaySFX(AddressableKeys.BattleSFXPlayerTurn);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnityEngine.Debug.Log("[Battle]\nPlayer Turn");
#endif
            if (_view == null) return;
            await _view.PlayTurnBannerAsync(LocalizationManager.Instance.GetTranslation("battle_player_turn"));
        }

        public async UniTask ShowMonsterTurnAsync()
        {
            AudioManager.Instance?.PlaySFX(AddressableKeys.BattleSFXMonsterTurn);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnityEngine.Debug.Log("[Battle]\nMonster Turn");
#endif
            if (_view == null) return;
            await _view.PlayTurnBannerAsync(LocalizationManager.Instance.GetTranslation("battle_monster_turn"));
        }
    }
}
