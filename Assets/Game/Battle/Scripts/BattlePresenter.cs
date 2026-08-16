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
    public enum PlayerActionType { Attack, Defend, Retreat }

    // 일반 몬스터 턴제 전투 루프 총괄. CombatSystem이 필드로 소유(신규 Singleton 없음).
    public class BattlePresenter
    {
        private readonly BattleView _view;
        private readonly TurnPresenter _turnPresenter;
        private readonly IntentPresenter _intentPresenter;
        private readonly BattleModel _battleModel = new BattleModel();

        private readonly TargetSelectPresenter _targetSelectPresenter;
        private readonly ShieldPresenter _shieldPresenter;
        private readonly HPBarPresenter _hpBarPresenter;
        private readonly List<MonsterPresenter> _monsters = new List<MonsterPresenter>();
        private UniTaskCompletionSource<PlayerActionType> _playerActionTcs;
        private bool _retreatAllowed;

        public BattlePresenter(BattleView view, MonsterIntentData intentData, GameObject intentViewPrefab, Transform intentLayer)
        {
            _view = view;
            _turnPresenter = new TurnPresenter(view != null ? view.TurnView : null);
            _intentPresenter = new IntentPresenter(intentData, intentViewPrefab, intentLayer);
            _targetSelectPresenter = new TargetSelectPresenter(view != null ? view.TargetIndicatorPrefab : null, view != null ? view.TargetIndicatorLayer : null);
            _shieldPresenter = new ShieldPresenter(view != null ? view.ShieldPopupView : null);
            _hpBarPresenter = new HPBarPresenter(view != null ? view.HPBarViewPrefab : null, view != null ? view.HPBarLayer : null);

            if (_view != null)
            {
                _view.OnAttackClicked += HandleAttackClicked;
                _view.OnDefendClicked += HandleDefendClicked;
                _view.OnRetreatClicked += HandleRetreatClicked;
            }
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnAttackClicked -= HandleAttackClicked;
                _view.OnDefendClicked -= HandleDefendClicked;
                _view.OnRetreatClicked -= HandleRetreatClicked;
            }
            _shieldPresenter?.Dispose();
        }

        // CombatSystem.SetupBattleUIAsync가 전투 UI 구성 시점에 미리 호출 — 세션 첫 조우에서
        // 타겟 링 스프라이트 로드 중 몬스터 클릭이 씹히는 레이스 컨디션을 방지한다.
        public UniTask PreloadTargetSelectAssetsAsync() => _targetSelectPresenter.PreloadRingSpriteAsync();

        // allowRetreat: 후퇴(Retreat) 액션 제공 여부. 기본 false(opt-in) — Retreat 종료 처리가
        // CombatSystem.Instance.ForceEndCombat()를 직접 호출하기 때문에, monsterSource가 실제로
        // CombatSystem.Instance인 일반 몬스터 조우에서만 true로 넘겨야 한다. 엘리트/보스는 별도의
        // MonsterSource를 쓰고 그 인스턴스들은 조우 종료 시 몬스터 GameObject를 처치 이벤트에서만
        // 정리하므로(사망 없이 강제 종료 시 유령 몬스터가 남음), 지금은 일반 몬스터 조우에서만 허용한다.
        public async UniTask RunTurnLoopAsync(
            IMonsterSource monsterSource,
            System.Func<MonsterType, MonsterPatternData> getPattern,
            System.Func<Combat.MonsterPresenter, MonsterPatternModel, TurnModel, MonsterPresenter> wrapperFactory = null,
            bool allowRetreat = false)
        {
            _monsters.Clear();
            _intentPresenter.Clear();
            _battleModel.Turn.Phase = BattleState.Idle;
            _retreatAllowed = allowRetreat;
            StatManager.Instance.ResetShield();
            StatManager.Instance.ResetCombatCounters();

            var makeWrapper = wrapperFactory ?? ((cp, pattern, turn) => new MonsterPresenter(cp, pattern, turn));

            void RegisterMonster(Combat.MonsterPresenter cp)
            {
                var pattern = getPattern(cp.Model.Type);
                var wrapper = makeWrapper(cp, new MonsterPatternModel(pattern), _battleModel.Turn);
                _monsters.Add(wrapper);
                cp.OnMonsterKilled += _ => HandleMonsterKilled(wrapper);

                // 보스 본체(5종)는 BossView에 전용 HP 슬라이더가 이미 있으므로 제외 — 나머지
                // (일반/엘리트/소환 미니언/전금속거인 광석 코어)에만 머리 위 HP바를 붙인다.
                if (!IsBossMainBody(cp.Model.Type))
                    _hpBarPresenter.Register(wrapper);
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
                RelicManager.Instance?.ApplyCombatEndEffects();
                StatManager.Instance.ResetShield();
                StatManager.Instance.ResetCombatCounters();
                _intentPresenter.Clear();
                _hpBarPresenter.Clear();
                _view?.SetVisible(false);
            }
        }

        private void HandleMonsterKilled(MonsterPresenter wrapper)
        {
            RelicManager.Instance?.ApplyMonsterKilledEffects(wrapper.CombatPresenter.Model.Type);
            _monsters.Remove(wrapper);
            _intentPresenter.RemoveIntent(wrapper);
            _hpBarPresenter.Remove(wrapper);
        }

        // 보스 본체 6종(4보스+CaveRat) 판정 — BossView 전용 HP 슬라이더와 중복 표시를 피하기 위한 제외 목록.
        private static bool IsBossMainBody(MonsterType type) => type switch
        {
            MonsterType.StoneGolemBoss or MonsterType.MotherCaveSpiderBoss or MonsterType.SkeletonWarlordBoss
                or MonsterType.AllMetalColossusBoss or MonsterType.CaveRatBoss => true,
            _ => false
        };

        private async UniTask PlayerTurnAsync()
        {
            // 그룹 D: 턴 시작 시점 실드 보너스(강철 갑옷=첫 턴 한정, 성기사의 망토=매 턴) — 재시도 루프 밖에서 1회만 부여.
            int turnStartShield = (RelicManager.Instance?.GetFirstTurnShieldBonus() ?? 0) + (RelicManager.Instance?.GetEveryTurnShieldBonus() ?? 0);
            if (turnStartShield > 0) StatManager.Instance.AddShield(turnStartShield);

            // Cancel 시 턴을 소모하지 않고 이 루프를 재진입한다.
            while (true)
            {
                _battleModel.Turn.Phase = BattleState.PlayerTurn;
                // Fast Turn Battle: 배너는 await하지 않는다 — 입력은 즉시 받을 수 있어야 한다.
                _turnPresenter.ShowPlayerTurnAsync().Forget();

                RefreshAllIntents();
                _view?.SetActionButtonsInteractable(true);
                _view?.SetRetreatButtonVisible(_retreatAllowed);

                _playerActionTcs = new UniTaskCompletionSource<PlayerActionType>();
                PlayerActionType action = await _playerActionTcs.Task;

                if (action == PlayerActionType.Retreat)
                {
                    if (!_retreatAllowed) continue; // 방어적 가드 — 버튼은 숨겨두지만 만일을 대비.

                    _view?.SetActionButtonsInteractable(false);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log("[Battle]\nRetreat Button");
#endif
                    var aliveMonsters = _monsters.Where(m => !m.CombatPresenter.Model.IsDead).ToList();
                    int retreatDamage = aliveMonsters.Count > 0 ? aliveMonsters.Max(m => m.CombatPresenter.Model.Damage) : 0;

                    var result = StatManager.Instance.TakeGuaranteedDamage(retreatDamage);
                    PickaxeDurabilityManager.Instance?.ApplyDirectDurabilityLoss(GameSettings.RetreatPickaxeDurabilityCost);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"[Battle]\nRetreat\nGuaranteed Damage : {result.HpDamage}\nPickaxe Durability Lost : {GameSettings.RetreatPickaxeDurabilityCost}");
#endif

                    Combat.CombatSystem.Instance.ForceEndCombat();
                    StatManager.Instance.IncrementCombatTurn();
                    return;
                }

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
                    StatManager.Instance.IncrementCombatTurn();
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
                int aliveMonsterCount = _monsters.Count(m => !m.CombatPresenter.Model.IsDead);
                await target.ReceivePlayerAttackAsync(aliveMonsterCount);
                StatManager.Instance.IncrementCombatTurn();
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
        private void HandleRetreatClicked() => _playerActionTcs?.TrySetResult(PlayerActionType.Retreat);
    }
}
