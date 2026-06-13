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

        public MonsterPresenter(MonsterModel model, MonsterView view)
        {
            Model = model;
            View = view;

            View.OnTouched += HandleTouched;
            StartAttackLoop().Forget();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            if (View != null)
            {
                View.OnTouched -= HandleTouched;
            }
        }

        private void HandleTouched()
        {
            if (Model.IsDead) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            int damage = StatManager.Instance.GetAttackDamage();
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

        private async UniTaskVoid StartAttackLoop()
        {
            try
            {
                while (!Model.IsDead && !_cts.IsCancellationRequested)
                {
                    // Delay before attack
                    await UniTask.Delay(TimeSpan.FromSeconds(Model.AttackInterval), cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                    {
                        continue;
                    }

                    // Play lunge animation
                    View.PlayAttackAnimation();

                    // Wait for the peak of the lunge
                    await UniTask.Delay(100, cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
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
