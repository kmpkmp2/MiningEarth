using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        private readonly DurabilityBarView _durabilityBar;
        private bool _barEverShown;
        private CancellationTokenSource _autoHideCts;

        public BlockPresenter(BlockModel model, BlockView view)
        {
            Model = model;
            View = view;

            _durabilityBar = DurabilityBarView.Create(view.transform);
            View.OnTouched += HandleTouched;
        }

        public void Dispose()
        {
            CancelAutoHide();

            if (View != null)
                View.OnTouched -= HandleTouched;

            _durabilityBar?.Hide();
        }

        private void HandleTouched()
        {
            if (Model.IsDestroyed) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                return;

            if (StatManager.Instance.CheckMiningFailure())
            {
                EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up * 0.5f, LocalizationManager.Instance.GetTranslation("combat_miss"), Color.gray);
                EffectSystem.Instance.ShakeCamera(0.1f, 0.03f);
                return;
            }

            string oreName = Model.Type.ToString();

            // First touch: show bar with current durability (before this hit is applied)
            if (!_barEverShown)
            {
                _barEverShown = true;
                Debug.Log($"[Mining]\nShow Durability Bar\nOre : {oreName}\nDurability : {Model.CurrentHits}/{Model.MaxHits}");
                _durabilityBar?.Show(Model.CurrentHits, Model.MaxHits);
            }

            int damage = StatManager.Instance.GetMiningPower();
            Model.TakeHit(damage);

            EffectSystem.Instance.ShakeCamera(0.15f, 0.06f);
            EffectSystem.Instance.SpawnHitParticles(View.transform.position, View.GetBlockColor());
            EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up * 0.5f, damage.ToString(), Color.yellow);
            View.PlayHitFeedback();

            if (Model.IsDestroyed)
            {
                Debug.Log($"[Mining]\nOre Destroyed\nOre : {oreName}");
                CancelAutoHide();
                _durabilityBar?.Hide();
                OnBlockDestroyed?.Invoke(this);
            }
            else
            {
                Debug.Log($"[Mining]\nHit Ore\nOre : {oreName}\nDurability : {Model.CurrentHits}/{Model.MaxHits}");
                _durabilityBar?.UpdateDurability(Model.CurrentHits, Model.MaxHits);
                ResetAutoHide();
            }
        }

        private void ResetAutoHide()
        {
            CancelAutoHide();
            _autoHideCts = new CancellationTokenSource();
            RunAutoHideAsync(_autoHideCts.Token).Forget();
        }

        private async UniTaskVoid RunAutoHideAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(3000, cancellationToken: token);
                _durabilityBar?.Hide();
                _barEverShown = false; // next touch shows the bar fresh
            }
            catch (OperationCanceledException) { }
        }

        private void CancelAutoHide()
        {
            _autoHideCts?.Cancel();
            _autoHideCts?.Dispose();
            _autoHideCts = null;
        }
    }
}
