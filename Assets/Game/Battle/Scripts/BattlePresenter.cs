using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;
using DeepEarth.Combat;
using DeepEarth.UI;

namespace DeepEarth.Battle
{
    public enum PlayerActionType { Attack, Defend }

    // 일반 몬스터 턴제 전투 루프 총괄. CombatSystem이 필드로 소유(신규 Singleton 없음).
    public class BattlePresenter
    {
        private readonly BattleView _view;
        private readonly TurnPresenter _turnPresenter;
        private readonly IntentPresenter _intentPresenter;
        private readonly BattleModel _battleModel = new BattleModel();

        private readonly List<MonsterPresenter> _monsters = new List<MonsterPresenter>();
        private UniTaskCompletionSource<PlayerActionType> _playerActionTcs;

        public BattlePresenter(BattleView view, MonsterIntentData intentData, GameObject intentViewPrefab, Transform intentLayer)
        {
            _view = view;
            _turnPresenter = new TurnPresenter(view != null ? view.TurnView : null);
            _intentPresenter = new IntentPresenter(intentData, intentViewPrefab, intentLayer);

            if (_view != null)
            {
                _view.OnAttackClicked += HandleAttackClicked;
                _view.OnDefendClicked += HandleDefendClicked;
            }
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnAttackClicked -= HandleAttackClicked;
                _view.OnDefendClicked -= HandleDefendClicked;
            }
        }

        public async UniTask RunTurnLoopAsync(CombatSystem combatSystem, System.Func<MonsterType, MonsterPatternData> getPattern)
        {
            _monsters.Clear();
            _intentPresenter.Clear();

            foreach (var cp in combatSystem.ActivePresenters)
            {
                var pattern = getPattern(cp.Model.Type);
                var wrapper = new MonsterPresenter(cp, new MonsterPatternModel(pattern), _battleModel.Turn);
                _monsters.Add(wrapper);
                cp.OnMonsterKilled += _ => HandleMonsterKilled(wrapper);
            }

            _view?.SetVisible(true);

            while (combatSystem.HasActiveMonsters)
            {
                await PlayerTurnAsync();
                if (StatManager.Instance.CurrentHP <= 0 || !combatSystem.HasActiveMonsters) break;

                await MonsterTurnAsync();
                StatusEffectManager.Instance?.ProcessActionTurn();
                if (StatManager.Instance.CurrentHP <= 0) break;
            }

            _intentPresenter.Clear();
            _view?.SetVisible(false);
        }

        private void HandleMonsterKilled(MonsterPresenter wrapper)
        {
            _monsters.Remove(wrapper);
            _intentPresenter.RemoveIntent(wrapper);
        }

        private async UniTask PlayerTurnAsync()
        {
            _battleModel.Turn.Phase = TurnPhase.PlayerTurn;
            await _turnPresenter.ShowPlayerTurnAsync();

            RefreshAllIntents();
            _view?.SetActionButtonsInteractable(true);

            _playerActionTcs = new UniTaskCompletionSource<PlayerActionType>();
            PlayerActionType action = await _playerActionTcs.Task;

            _view?.SetActionButtonsInteractable(false);

            if (action == PlayerActionType.Attack)
            {
                var target = _monsters.FirstOrDefault(m => !m.CombatPresenter.Model.IsDead);
                target?.ReceivePlayerAttack();
                _battleModel.Turn.PlayerIsDefending = false;
            }
            else
            {
                _battleModel.Turn.PlayerIsDefending = true;
                _view?.PlayDefendEffect();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Battle]\nPlayer Defend");
#endif
            }
        }

        private async UniTask MonsterTurnAsync()
        {
            _battleModel.Turn.Phase = TurnPhase.MonsterTurn;
            await _turnPresenter.ShowMonsterTurnAsync();

            foreach (var monster in _monsters.ToArray())
            {
                if (monster.CombatPresenter.Model.IsDead) continue;
                monster.ExecuteTurn();
                if (StatManager.Instance.CurrentHP <= 0) break;
            }

            _battleModel.Turn.PlayerIsDefending = false;
            RefreshAllIntents();
        }

        private void RefreshAllIntents()
        {
            foreach (var monster in _monsters)
                if (!monster.CombatPresenter.Model.IsDead)
                    _intentPresenter.ShowIntent(monster);
        }

        private void HandleAttackClicked() => _playerActionTcs?.TrySetResult(PlayerActionType.Attack);
        private void HandleDefendClicked() => _playerActionTcs?.TrySetResult(PlayerActionType.Defend);
    }
}
