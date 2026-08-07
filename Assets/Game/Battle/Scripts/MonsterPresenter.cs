using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;
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

        private static readonly Color ShieldDamageColor = new Color(0.3f, 0.6f, 1f);

        public Combat.MonsterPresenter CombatPresenter => _combatPresenter;
        public PatternStepData CurrentStep => _pattern.CurrentStep;

        // Target Select 후보 목록에 포함될지 여부. 기본은 항상 true — 부위 파괴형 보스 본체처럼
        // 특정 조건에서 선택 불가능해야 하는 경우 서브클래스가 오버라이드한다.
        public virtual bool IsTargetable => true;

        // Intent 미리보기용 수치. 패턴 에셋의 value가 0(미입력)이면 ExecuteTurnAsync와 동일하게
        // 몬스터의 실제 공격력(Model.Damage)으로 대체한다 — 표시값과 실제 데미지가 어긋나지 않도록
        // 소스를 하나로 유지한다.
        public int GetDisplayValue(PatternStepData step)
        {
            if (step == null) return 0;
            switch (step.intentType)
            {
                case IntentType.Attack:
                case IntentType.HeavyAttack:
                    return ModifyOutgoingDamage(step.value > 0 ? step.value : _combatPresenter.Model.Damage);
                default:
                    return step.value;
            }
        }

        // 엘리트/보스 서브클래스가 공격 데미지에 자신의 스킬 보정(예: 격노 스택)을 얹는 확장 지점.
        // GetDisplayValue/ExecuteTurnAsync가 공통으로 거쳐가므로 인텐트 표시값과 실제 데미지가 항상 일치한다.
        protected virtual int ModifyOutgoingDamage(int baseDamage) => baseDamage;

        public MonsterPresenter(Combat.MonsterPresenter combatPresenter, MonsterPatternModel pattern, TurnModel turn)
        {
            _combatPresenter = combatPresenter;
            _pattern = pattern;
            _turn = turn;
        }

        // 플레이어가 공격을 선택했을 때 이 몬스터가 받는 피해 처리.
        // Fast Turn Battle: PlayerAttackAnimationTime(윈드업) → 피해 적용 → PlayerHitEffectTime(피격 연출) 순으로
        // 아주 짧게 진행한다. Player Turn 재입력을 막지 않도록 이 시퀀스는 다음 Player Turn 시작 전에만 완료되면 된다.
        public virtual async UniTask ReceivePlayerAttackAsync(int aliveMonsterCount = 1)
        {
            int dmg = StatManager.Instance.GetAttackDamage();
            dmg += RelicManager.Instance?.GetPerMonsterAttackBonus(aliveMonsterCount) ?? 0;

            // 그룹 D: 전투 도끼 — 전투당 1회, 첫 공격에 배율 적용
            float firstAttackMult = RelicManager.Instance?.GetFirstAttackDamageMultiplier() ?? 0f;
            if (firstAttackMult > 0f)
            {
                dmg = Mathf.RoundToInt(dmg * firstAttackMult);
                StatManager.Instance.MarkFirstAttackDone();
            }

            // 그룹 N: 최후의 일격 — 상대 HP비율 < 내 HP비율일 때, 전투당 1회 배율 적용
            if (!StatManager.Instance.CombatFinishingBlowUsed)
            {
                int monsterMaxHp = _combatPresenter.Model.MaxHP;
                int playerMaxHp  = StatManager.Instance.GetMaxHP();
                float monsterHpRatio = monsterMaxHp > 0 ? (float)_combatPresenter.Model.CurrentHP / monsterMaxHp : 0f;
                float playerHpRatio  = playerMaxHp > 0 ? (float)StatManager.Instance.CurrentHP / playerMaxHp : 0f;

                if (monsterHpRatio < playerHpRatio)
                {
                    float finishMult = RelicManager.Instance?.GetFinishingBlowMultiplier() ?? 0f;
                    if (finishMult > 0f)
                    {
                        dmg = Mathf.RoundToInt(dmg * finishMult);
                        StatManager.Instance.MarkCombatFinishingBlowUsed();
                    }
                }
            }

            if (_combatPresenter.Model.IsDefending)
                dmg = Mathf.RoundToInt(dmg * (1f - BattleBalanceData.Instance.defenseRate));

            await UniTask.Delay(TimeSpan.FromSeconds(BattleBalanceData.Instance.playerAttackAnimationTime));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Battle]\nPlayer Attack\nDamage : {dmg}");
#endif
            _combatPresenter.ApplyExternalDamage(dmg);
            RelicManager.Instance?.ApplyPlayerDealtDamageEffects();

            await UniTask.Delay(TimeSpan.FromSeconds(BattleBalanceData.Instance.playerHitEffectTime));
        }

        // 이 몬스터의 Monster Turn 행동 실행. 반환값은 실행된(방금 소모된) 스텝.
        public virtual async UniTask<PatternStepData> ExecuteTurnAsync()
        {
            var step = _pattern.CurrentStep;
            if (step == null || _combatPresenter.Model.IsDead) return step;

            _combatPresenter.Model.IsDefending = false;

            switch (step.intentType)
            {
                case IntentType.Attack:
                case IntentType.HeavyAttack:
                {
                    int dmg = ModifyOutgoingDamage(step.value > 0 ? step.value : _combatPresenter.Model.Damage);

                    _combatPresenter.View.PlayAttackAnimation(BattleBalanceData.Instance.monsterAttackAnimationTime);
                    await UniTask.Delay(TimeSpan.FromSeconds(BattleBalanceData.Instance.monsterAttackAnimationTime));

                    var result = StatManager.Instance.TakeDamage(dmg);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"[Battle]\nMonster Attack\nIncoming Damage : {dmg}\nShield Damage : {result.ShieldDamage}\nHP Damage : {result.HpDamage}");
#endif

                    float hitEffectTime = BattleBalanceData.Instance.monsterHitEffectTime;
                    EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.35f), hitEffectTime);
                    EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);
                    Vector3 textWorldPos = Camera.main != null
                        ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Camera.main.transform.right * -0.5f
                        : _combatPresenter.View.transform.position + Vector3.up;
                    if (result.ShieldDamage > 0)
                        EffectSystem.Instance.SpawnDamageText(textWorldPos, $"-{result.ShieldDamage}", ShieldDamageColor);
                    if (result.HpDamage > 0)
                        EffectSystem.Instance.SpawnDamageText(textWorldPos, $"-{result.HpDamage} HP", Color.red);

                    await UniTask.Delay(TimeSpan.FromSeconds(hitEffectTime));
                    break;
                }
                case IntentType.Defend:
                    _combatPresenter.Model.IsDefending = true;
                    break;
                case IntentType.Debuff:
                    OnDebuffStep();
                    break;
                case IntentType.Heal:
                    OnHealStep();
                    break;
                case IntentType.Buff:
                    OnBuffStep();
                    break;
                case IntentType.Summon:
                    OnSummonStep();
                    break;
                case IntentType.Special:
                    OnSpecialStep();
                    break;
            }

            return _pattern.Advance();
        }

        // 일반 몬스터 기본 동작(채굴력 감소 디버프). 엘리트/보스 서브클래스가 각자의 스킬로 오버라이드한다.
        protected virtual void OnDebuffStep() => StatusEffectManager.Instance?.ApplyMiningPowerDown();

        // 아래 세 스텝은 일반 몬스터 로스터에서는 미사용 — 엘리트/보스 확장 지점.
        protected virtual void OnHealStep() { }
        protected virtual void OnBuffStep() { }
        protected virtual void OnSummonStep() { }
        protected virtual void OnSpecialStep() { }

        public virtual void Dispose()
        {
            _combatPresenter.Dispose();
        }
    }
}
