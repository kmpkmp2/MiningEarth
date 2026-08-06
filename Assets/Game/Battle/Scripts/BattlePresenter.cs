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

        private readonly TargetSelectPresenter _targetSelectPresenter;
        private readonly ShieldPresenter _shieldPresenter;
        private readonly List<MonsterPresenter> _monsters = new List<MonsterPresenter>();
        private UniTaskCompletionSource<PlayerActionType> _playerActionTcs;

        public BattlePresenter(BattleView view, MonsterIntentData intentData, GameObject intentViewPrefab, Transform intentLayer)
        {
            _view = view;
            _turnPresenter = new TurnPresenter(view != null ? view.TurnView : null);
            _intentPresenter = new IntentPresenter(intentData, intentViewPrefab, intentLayer);
            _targetSelectPresenter = new TargetSelectPresenter(view != null ? view.TargetIndicatorPrefab : null, view != null ? view.TargetIndicatorLayer : null);
            _shieldPresenter = new ShieldPresenter(view != null ? view.ShieldPopupView : null);

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
            _shieldPresenter?.Dispose();
        }

        public async UniTask RunTurnLoopAsync(
            IMonsterSource monsterSource,
            System.Func<MonsterType, MonsterPatternData> getPattern,
            System.Func<Combat.MonsterPresenter, MonsterPatternModel, TurnModel, MonsterPresenter> wrapperFactory = null)
        {
            _monsters.Clear();
            _intentPresenter.Clear();
            _battleModel.Turn.Phase = BattleState.Idle;
            StatManager.Instance.ResetShield();

            var makeWrapper = wrapperFactory ?? ((cp, pattern, turn) => new MonsterPresenter(cp, pattern, turn));

            void RegisterMonster(Combat.MonsterPresenter cp)
            {
                var pattern = getPattern(cp.Model.Type);
                var wrapper = makeWrapper(cp, new MonsterPatternModel(pattern), _battleModel.Turn);
                _monsters.Add(wrapper);
                cp.OnMonsterKilled += _ => HandleMonsterKilled(wrapper);
            }

            foreach (var cp in monsterSource.ActivePresenters)
                RegisterMonster(cp);

            // 전투 중 새로 스폰되는 몬스터(슬라임 분열, 보스 소환 등)도 턴 시스템에 편입시킨다.
            // 반드시 아래 finally에서 해제 — BattlePresenter는 세션 내내 재사용되는 공유 인스턴스라
            // 해제하지 않으면 다음 전투부터 구독이 누적되어 몬스터가 중복 등록된다.
            monsterSource.OnMonsterSpawned += RegisterMonster;
            try
            {
                _view?.SetVisible(true);

                while (monsterSource.HasActiveMonsters)
                {
                    await PlayerTurnAsync();
                    if (StatManager.Instance.CurrentHP <= 0 || !monsterSource.HasActiveMonsters) break;

                    await MonsterTurnAsync();
                    StatusEffectManager.Instance?.ProcessActionTurn();
                    if (StatManager.Instance.CurrentHP <= 0) break;
                }
            }
            finally
            {
                monsterSource.OnMonsterSpawned -= RegisterMonster;
                _battleModel.Turn.Phase = BattleState.BattleEnd;
                StatManager.Instance.ResetShield();
                _intentPresenter.Clear();
                _view?.SetVisible(false);
            }
        }

        private void HandleMonsterKilled(MonsterPresenter wrapper)
        {
            _monsters.Remove(wrapper);
            _intentPresenter.RemoveIntent(wrapper);
        }

        private async UniTask PlayerTurnAsync()
        {
            // Cancel 시 턴을 소모하지 않고 이 루프를 재진입한다.
            while (true)
            {
                _battleModel.Turn.Phase = BattleState.PlayerTurn;
                // Fast Turn Battle: 배너는 await하지 않는다 — 입력은 즉시 받을 수 있어야 한다.
                _turnPresenter.ShowPlayerTurnAsync().Forget();

                RefreshAllIntents();
                _view?.SetActionButtonsInteractable(true);

                _playerActionTcs = new UniTaskCompletionSource<PlayerActionType>();
                PlayerActionType action = await _playerActionTcs.Task;

                if (action == PlayerActionType.Defend)
                {
                    _view?.SetActionButtonsInteractable(false);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log("[Battle]\nDefend Button");
#endif
                    _battleModel.Turn.Phase = BattleState.ShieldSelect;
                    bool applied = await _shieldPresenter.SelectAndApplyOreAsync();
                    if (!applied)
                    {
                        // 취소 → 턴 미소모, Player Turn 재진입.
                        continue;
                    }

                    _view?.PlayDefendEffect();
                    return;
                }

                // 공격 선택 → 즉시 실행하지 않고 Target Select 상태로 진입한다.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Battle]\nAttack Button");
#endif
                _battleModel.Turn.Phase = BattleState.TargetSelect;

                var candidates = _monsters.Where(m => !m.CombatPresenter.Model.IsDead && m.IsTargetable).ToList();
                var target = await _targetSelectPresenter.SelectTargetAsync(candidates, _view);

                if (target == null)
                {
                    // 취소 → Player Turn 재진입, 턴 미소모(SetTargetSelectUIVisible(false)가 Defend 버튼을 이미 복원).
                    continue;
                }

                _view?.SetActionButtonsInteractable(false);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Battle]\nAttack");
#endif
                await target.ReceivePlayerAttackAsync();
                return;
            }
        }

        private async UniTask MonsterTurnAsync()
        {
            _battleModel.Turn.Phase = BattleState.MonsterTurn;
            _turnPresenter.ShowMonsterTurnAsync().Forget();

            foreach (var monster in _monsters.ToArray())
            {
                if (monster.CombatPresenter.Model.IsDead) continue;
                await monster.ExecuteTurnAsync();
                if (StatManager.Instance.CurrentHP <= 0) break;
            }

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
