using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.Battle
{
    // 3단계 로스터 확장(2026-08) — 정규 몬스터 중 고유 기믹을 가진 4종(광석벌레/광산균사체/
    // 심연의광부벌/황금광맥정령)의 턴 훅. 엘리트의 EliteMonsterPresenter와 동일하게, 몬스터 타입
    // 하나당 새 서브클래스를 만드는 대신 이 라우터 클래스 하나가 Model.Type으로 분기한다.
    public class GimmickMonsterPresenter : MonsterPresenter
    {
        private MonsterType Type => CombatPresenter.Model.Type;

        // 광석벌레: 광석 떨어뜨리기 사용 횟수(최대 2) — 사용할 때마다 자기 공격력 -1.
        private int _oreBurrowerShedCount;

        // 심연의광부벌: 벌집강화 사용 여부(1회 사용 후 지속) — 피해 감소 + 확률 추가타.
        private bool _abyssBeeGuardActive;

        // 황금광맥정령: 황금흡수 사용 후 다음 피격 1회에 적용.
        private bool _goldVeinAbsorbActive;

        public GimmickMonsterPresenter(Combat.MonsterPresenter combatPresenter, MonsterPatternModel pattern, TurnModel turn)
            : base(combatPresenter, pattern, turn) { }

        protected override int ModifyOutgoingDamage(int baseDamage)
        {
            switch (Type)
            {
                case MonsterType.OreBurrower:
                    return Mathf.Max(1, baseDamage - _oreBurrowerShedCount);

                case MonsterType.GoldVeinSpirit:
                {
                    int goldBonus = Mathf.Min(GetGoldCount(), 10);
                    return baseDamage + goldBonus;
                }

                case MonsterType.AbyssMinerBee:
                    if (_abyssBeeGuardActive && UnityEngine.Random.value < 0.3f)
                        return baseDamage * 2;
                    return baseDamage;

                default:
                    return base.ModifyOutgoingDamage(baseDamage);
            }
        }

        protected override void OnDebuffStep()
        {
            if (Type != MonsterType.MineMycelium) { base.OnDebuffStep(); return; }

            // 포자 살포: 채굴력 감소 / 공격력 저하 중 하나를 50% 확률로 부여.
            if (UnityEngine.Random.value < 0.5f)
            {
                base.OnDebuffStep(); // 채굴력 감소(기본 구현 재사용)
            }
            else
            {
                int current = StatManager.Instance.GetEffectStack(EffectType.CurseAttackDamage);
                if (current < GameSettings.MaxBuffDebuffStack)
                {
                    StatManager.Instance.AddEffect(EffectType.CurseAttackDamage);
                    SpawnFlavorText("공격력 저하!", new Color(0.5f, 0.2f, 0.8f));
                }
            }
        }

        protected override void OnHealStep()
        {
            if (Type != MonsterType.MineMycelium) { base.OnHealStep(); return; }

            int amount = CurrentStep?.value ?? 0;
            if (amount <= 0) return;
            CombatPresenter.Model.Heal(amount);
            SpawnFlavorText($"+{amount} 회복", Color.green);
        }

        protected override void OnSpecialStep()
        {
            switch (Type)
            {
                case MonsterType.OreBurrower:
                    _oreBurrowerShedCount = Mathf.Min(_oreBurrowerShedCount + 1, 2);
                    SpawnFlavorText("광석 떨어뜨림!", new Color(0.6f, 0.5f, 0.4f));
                    break;

                case MonsterType.GoldVeinSpirit:
                    _goldVeinAbsorbActive = true;
                    SpawnFlavorText("황금 흡수!", new Color(1f, 0.84f, 0f));
                    break;

                default:
                    base.OnSpecialStep();
                    break;
            }
        }

        protected override void OnBuffStep()
        {
            switch (Type)
            {
                case MonsterType.AbyssMinerBee:
                    _abyssBeeGuardActive = true;
                    SpawnFlavorText("벌집 강화!", new Color(1f, 0.8f, 0.2f));
                    break;

                case MonsterType.GoldVeinSpirit:
                    SpawnFlavorText("광맥 강화!", new Color(1f, 0.84f, 0f));
                    break;

                default:
                    base.OnBuffStep();
                    break;
            }
        }

        protected override void OnAttackLanded()
        {
            if (Type == MonsterType.AbyssMinerBee)
                StatusEffectManager.Instance?.ApplyPoison(3);
            else
                base.OnAttackLanded();
        }

        protected override float GetIncomingDamageMultiplier()
        {
            switch (Type)
            {
                case MonsterType.AbyssMinerBee:
                    return _abyssBeeGuardActive ? 0.7f : 1f;

                case MonsterType.GoldVeinSpirit:
                    if (!_goldVeinAbsorbActive) return 1f;
                    _goldVeinAbsorbActive = false; // 다음 피격 1회 소모
                    int reduction = Mathf.Min(1 + GetGoldCount(), 80);
                    return 1f - reduction / 100f;

                default:
                    return base.GetIncomingDamageMultiplier();
            }
        }

        private static int GetGoldCount() => InventoryManager.Instance?.GetItemCount("Item_Gold") ?? 0;

        private static void SpawnFlavorText(string text, Color color)
        {
            if (Camera.main == null || EffectSystem.Instance == null) return;
            Vector3 pos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Vector3.up * 0.3f;
            EffectSystem.Instance.SpawnDamageText(pos, text, color);
        }
    }
}
