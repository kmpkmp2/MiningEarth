using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DeepEarth.UI
{
    public class ResourceEarnedRowView : MonoBehaviour
    {
        [SerializeField] private ResourceItemView ironItem;
        [SerializeField] private ResourceItemView silverItem;
        [SerializeField] private ResourceItemView goldItem;
        [SerializeField] private ResourceItemView diamondItem;

        public event Action OnItemShown;

        public void ResetAll()
        {
            ironItem?.ResetState();
            silverItem?.ResetState();
            goldItem?.ResetState();
            diamondItem?.ResetState();
        }

        public async UniTask AnimateAsync(int iron, int silver, int gold, int diamond, CancellationToken ct = default)
        {
            // 철 0.0s, 은 0.15s, 금 0.30s, 다이아 0.45s
            const float interval = 0.15f;

            var items = new (ResourceItemView view, int count)[]
            {
                (ironItem,    iron),
                (silverItem,  silver),
                (goldItem,    gold),
                (diamondItem, diamond),
            };

            var tasks = new UniTask[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                tasks[i] = ShowItemWithDelayAsync(items[i].view, items[i].count, i * interval, ct);
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask ShowItemWithDelayAsync(ResourceItemView item, int count, float delay, CancellationToken ct)
        {
            if (item == null) return;
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: true, cancellationToken: ct);
            if (ct.IsCancellationRequested) return;
            OnItemShown?.Invoke();
            await item.ShowAsync(count, ct);
        }
    }
}
