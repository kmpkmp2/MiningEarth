using System;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Combat
{
    public class MonsterPresenter
    {
        public MonsterModel Model { get; private set; }
        public MonsterView View { get; private set; }

        public event Action<MonsterPresenter> OnMonsterKilled;

        public MonsterPresenter(MonsterModel model, MonsterView view)
        {
            Model = model;
            View = view;
        }

        public virtual void Dispose() { }

        // 플레이어 터치 공격 외의 경로(예: 폭탄류 소모 아이템)에서 이 몬스터에게 피해를 주기 위한 공용 진입점.
        public void ApplyExternalDamage(int damage)
        {
            if (Model.IsDead) return;

            Model.TakeDamage(damage);

            EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up * 0.5f, damage.ToString(), Color.white);
            EffectSystem.Instance.SpawnHitParticles(View.transform.position, View.GetMonsterColor());
            EffectSystem.Instance.ShakeCamera(0.12f, 0.04f);
            View.PlayHurtFeedback();

            if (Model.IsDead)
            {
                OnMonsterKilled?.Invoke(this);
            }
        }
    }
}
