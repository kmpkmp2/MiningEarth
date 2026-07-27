using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.Combat
{
    public class MonsterPresenter
    {
        public MonsterModel Model { get; private set; }
        public MonsterView View { get; private set; }

        public event Action<MonsterPresenter> OnMonsterKilled;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // startRealTimeLoop: false — 턴제 전투(일반 몬스터)에서는 실시간 터치공격/자동공격 루프를 켜지 않는다.
        // 엘리트(EliteMonsterPresenter)는 이 파라미터를 넘기지 않아 기존 실시간 동작을 그대로 유지한다.
        public MonsterPresenter(MonsterModel model, MonsterView view, bool startRealTimeLoop = true)
        {
            Model = model;
            View = view;

            if (startRealTimeLoop)
            {
                View.OnTouched += HandleTouched;
                StartAttackLoop().Forget();
            }
        }

        public virtual void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            if (View != null)
            {
                View.OnTouched -= HandleTouched;
            }
        }

        protected virtual void HandleTouched()
        {
            if (Model.IsDead) return;

            var state = GameManager.Instance?.CurrentState;
            if (state != GameState.Playing && state != GameState.BossCombat)
            {
                return;
            }

            int damage = StatManager.Instance.GetAttackDamage();
            ApplyExternalDamage(damage);
        }

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

        protected virtual async UniTaskVoid StartAttackLoop()
        {
            try
            {
                if (Model.InitialAttackDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(Model.InitialAttackDelay), cancellationToken: _cts.Token);

                while (!Model.IsDead && !_cts.IsCancellationRequested)
                {
                    // Delay before attack
                    await UniTask.Delay(TimeSpan.FromSeconds(Model.AttackInterval), cancellationToken: _cts.Token);

                    var s = GameManager.Instance?.CurrentState;
                    if (Model.IsDead || (s != GameState.Playing && s != GameState.BossCombat))
                    {
                        continue;
                    }

                    // Play lunge animation
                    View.PlayAttackAnimation();

                    // Wait for the peak of the lunge
                    await UniTask.Delay(100, cancellationToken: _cts.Token);

                    s = GameManager.Instance?.CurrentState;
                    if (Model.IsDead || (s != GameState.Playing && s != GameState.BossCombat))
                    {
                        continue;
                    }

                    // Deal damage to player
                    StatManager.Instance.TakeDamage(Model.Damage);

                    // Feedback on player damage
                    EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.35f), 0.15f);
                    EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);

                    Vector3 textWorldPos = Camera.main != null 
                        ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Camera.main.transform.right * -0.5f
                        : View.transform.position + Vector3.up;

                    EffectSystem.Instance.SpawnDamageText(textWorldPos, $"-{Model.Damage} HP", Color.red);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean exit on destroy/cancellation
            }
        }
    }
}
