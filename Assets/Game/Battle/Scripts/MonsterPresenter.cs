using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Combat;

namespace DeepEarth.Battle
{
    // 몬스터 1마리의 턴 행동(플레이어 공격 수신 / 자신의 패턴 스텝 실행) 담당.
    // 기존 Combat.MonsterPresenter(피해적용+연출+처치판정)를 그대로 감싸서 재사용한다.
    public class MonsterPresenter
    {
        private readonly Combat.MonsterPresenter _combatPresenter;
        private readonly MonsterPatternModel _pattern;
        private readonly TurnModel _turn;

        public Combat.MonsterPresenter CombatPresenter => _combatPresenter;
        public PatternStepData CurrentStep => _pattern.CurrentStep;

        public MonsterPresenter(Combat.MonsterPresenter combatPresenter, MonsterPatternModel pattern, TurnModel turn)
        {
            _combatPresenter = combatPresenter;
            _pattern = pattern;
            _turn = turn;
        }

        // 플레이어가 공격을 선택했을 때 이 몬스터가 받는 피해 처리.
        public void ReceivePlayerAttack()
        {
            int dmg = StatManager.Instance.GetAttackDamage();
            if (_combatPresenter.Model.IsDefending)
                dmg = Mathf.RoundToInt(dmg * (1f - BattleBalanceData.Instance.defenseRate));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Battle]\nPlayer Attack\nDamage : {dmg}");
#endif
            _combatPresenter.ApplyExternalDamage(dmg);
        }

        // 이 몬스터의 Monster Turn 행동 실행. 반환값은 실행된(방금 소모된) 스텝.
        public PatternStepData ExecuteTurn()
        {
            var step = _pattern.CurrentStep;
            if (step == null || _combatPresenter.Model.IsDead) return step;

            _combatPresenter.Model.IsDefending = false;

            switch (step.intentType)
            {
                case IntentType.Attack:
                case IntentType.HeavyAttack:
                {
                    int dmg = step.value > 0 ? step.value : _combatPresenter.Model.Damage;
                    if (_turn.PlayerIsDefending)
                        dmg = Mathf.RoundToInt(dmg * (1f - BattleBalanceData.Instance.defenseRate));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"[Battle]\nMonster Attack\nDamage : {dmg}");
#endif
                    _combatPresenter.View.PlayAttackAnimation();
                    StatManager.Instance.TakeDamage(dmg);
                    break;
                }
                case IntentType.Defend:
                    _combatPresenter.Model.IsDefending = true;
                    break;
                case IntentType.Debuff:
                    StatusEffectManager.Instance?.ApplyMiningPowerDown();
                    break;
                case IntentType.Heal:
                {
                    // 1단계 일반 몬스터 로스터에는 미사용 — 추후 확장 지점.
                    break;
                }
                case IntentType.Buff:
                case IntentType.Summon:
                case IntentType.Special:
                    // 1단계(일반 몬스터)에서는 사용하지 않음 — 확장 지점.
                    break;
            }

            return _pattern.Advance();
        }

        public void Dispose()
        {
            _combatPresenter.Dispose();
        }
    }
}
