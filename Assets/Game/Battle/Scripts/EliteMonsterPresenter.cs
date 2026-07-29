using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;
using DeepEarth.Combat;

namespace DeepEarth.Battle
{
    // 엘리트 몬스터 전용 턴 래퍼. Combat.EliteMonsterPresenter가 이미 갖고 있는 6종 스킬 로직(피해 흡수,
    // 중독, 격노, 도굴, 디버프)을 실시간 타이머 대신 턴 기반 훅(ReceivePlayerAttackAsync/OnDebuffStep/
    // OnSpecialStep/생성자·사망 이벤트)에서 그대로 재사용한다. 새 스킬 로직을 다시 작성하지 않는다.
    public class EliteMonsterPresenter : MonsterPresenter
    {
        private Combat.EliteMonsterPresenter EliteCombat => (Combat.EliteMonsterPresenter)CombatPresenter;
        private EliteMonsterModel EliteModel => (EliteMonsterModel)CombatPresenter.Model;

        public EliteMonsterPresenter(Combat.EliteMonsterPresenter combatPresenter, MonsterPatternModel pattern, TurnModel turn)
            : base(combatPresenter, pattern, turn)
        {
            // CursedPriest: 실시간 경로의 "생성 즉시 1스택 적용"과 동일하게 이식.
            if (EliteModel.Data.eliteSkillType == EliteSkillType.AttackDebuff)
                EliteCombat.ApplyAttackDebuffStack();

            // CursedPriest: 사망 시 이번 전투에서 건 디버프 스택 전부 제거(실시간 경로의 HandleEliteDefeated와 동일).
            combatPresenter.OnMonsterKilled += _ =>
            {
                if (EliteModel.Data.eliteSkillType == EliteSkillType.AttackDebuff)
                    EliteCombat.RemoveAllDebuffStacks();
            };
        }

        // 플레이어 공격 수신 — IronPlateSpider는 방어구가 남아있는 동안 HP 대신 방어구를 깎는다.
        public override async UniTask ReceivePlayerAttackAsync()
        {
            if (EliteModel.Data.eliteSkillType == EliteSkillType.IronArmor && !EliteModel.IsArmorBroken)
            {
                await ReceiveArmorAbsorbAsync();
                return;
            }

            await base.ReceivePlayerAttackAsync();
            if (CombatPresenter.Model.IsDead) return;

            // IronPlateSpider: 방어구 파괴 후 3타마다 중독.
            if (EliteModel.Data.eliteSkillType == EliteSkillType.IronArmor)
            {
                EliteModel.IncrementTouch();
                if (EliteModel.ShouldTriggerPoison())
                {
                    EliteModel.ResetPoisonCounter();
                    EliteCombat.ApplyPoison();
                }
            }

            // CursedKnight: 매 타격마다 격노 임계값 확인.
            if (EliteModel.Data.eliteSkillType == EliteSkillType.Enrage)
            {
                int enrageCount = EliteModel.CheckAndTriggerEnrage();
                if (enrageCount > 0) EliteCombat.OnEnrage(enrageCount);
            }
        }

        private async UniTask ReceiveArmorAbsorbAsync()
        {
            int dmg = StatManager.Instance.GetAttackDamage();
            if (CombatPresenter.Model.IsDefending)
                dmg = Mathf.RoundToInt(dmg * (1f - BattleBalanceData.Instance.defenseRate));
            float eliteBonus = RelicManager.Instance?.GetEliteDamageBonus() ?? 0f;
            if (eliteBonus > 0f) dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * (1f + eliteBonus)));

            await UniTask.Delay(TimeSpan.FromSeconds(BattleBalanceData.Instance.playerAttackAnimationTime));

            EliteModel.TakeArmorDamage(dmg);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nSkill Activated\nIronArmor Absorb : {dmg}");
#endif
            EffectSystem.Instance.SpawnDamageText(CombatPresenter.View.transform.position + Vector3.up * 0.5f, $"-{dmg} (방어)", Color.grey);
            EffectSystem.Instance.SpawnHitParticles(CombatPresenter.View.transform.position, Color.grey);
            EffectSystem.Instance.ShakeCamera(0.08f, 0.03f);
            CombatPresenter.View.PlayHurtFeedback();

            if (EliteModel.IsArmorBroken)
            {
                EffectSystem.Instance.SpawnDamageText(CombatPresenter.View.transform.position + Vector3.up, "방어 붕괴!", new Color(1f, 0.5f, 0f));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Elite]\nSkill Activated\nIronArmor : Armor Broken");
#endif
            }

            await UniTask.Delay(TimeSpan.FromSeconds(BattleBalanceData.Instance.playerHitEffectTime));
        }

        // SkeletonMiner: 광물 도둑질을 실시간 5초 루프 대신 패턴의 Special 스텝으로 이식.
        protected override void OnSpecialStep()
        {
            if (EliteModel.Data.eliteSkillType != EliteSkillType.MineralTheft) return;

            string stolen = EliteCombat.StealOneMineralFromInventory();
            if (stolen == null) return;

            Vector3 textPos = CombatPresenter.View != null ? CombatPresenter.View.transform.position + Vector3.up * 0.5f : Vector3.up;
            EffectSystem.Instance.SpawnDamageText(textPos, "광물 도둑!!", Color.yellow);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nSkill Activated\nSkeletonMiner Steal : {stolen}");
#endif
        }

        // CursedPriest: 공격력 저하 디버프를 실시간 1초 루프 대신 패턴의 Debuff 스텝으로 이식.
        protected override void OnDebuffStep()
        {
            if (EliteModel.Data.eliteSkillType == EliteSkillType.AttackDebuff)
            {
                EliteCombat.ApplyAttackDebuffStack();
                return;
            }
            base.OnDebuffStep();
        }

        // CursedKnight: 실시간 경로의 "공격 간격 -15%/스택" 대신, 턴제에는 간격 개념이 없으므로
        // 기존 데미지 공식(Model.Damage + EnrageCount)을 그대로 이식해 스택당 고정 데미지 +1로 재해석한다.
        protected override int ModifyOutgoingDamage(int baseDamage)
        {
            if (EliteModel.Data.eliteSkillType == EliteSkillType.Enrage)
                return baseDamage + EliteModel.EnrageCount;
            return base.ModifyOutgoingDamage(baseDamage);
        }
    }
}
