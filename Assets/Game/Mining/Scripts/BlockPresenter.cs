using System;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.Mining
{
    public class BlockPresenter
    {
        public BlockModel Model { get; private set; }
        public BlockView View { get; private set; }

        public event Action<BlockPresenter> OnBlockDestroyed;

        public BlockPresenter(BlockModel model, BlockView view)
        {
            Model = model;
            View = view;

            View.OnTouched += HandleTouched;
        }

        public void Dispose()
        {
            if (View != null)
            {
                View.OnTouched -= HandleTouched;
            }
        }

        private void HandleTouched()
        {
            if (Model.IsDestroyed) return;

            // Check for game state before allowing interaction
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // Check if mining fails due to CurseMiningFailChance
            if (StatManager.Instance.CheckMiningFailure())
            {
                EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up * 0.5f, LocalizationManager.Instance.GetTranslation("combat_miss"), Color.gray);
                EffectSystem.Instance.ShakeCamera(0.1f, 0.03f);
                return;
            }

            int damage = StatManager.Instance.GetMiningPower();
            Model.TakeHit(damage);

            // Visual feedback
            EffectSystem.Instance.ShakeCamera(0.15f, 0.06f);
            EffectSystem.Instance.SpawnHitParticles(View.transform.position, View.GetBlockColor());
            EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up * 0.5f, damage.ToString(), Color.yellow);
            View.PlayHitFeedback();

            if (Model.IsDestroyed)
            {
                OnBlockDestroyed?.Invoke(this);
            }
        }
    }
}
