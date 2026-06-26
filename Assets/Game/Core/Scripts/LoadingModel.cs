using System;
using UnityEngine;

namespace DeepEarth.Core
{
    public class LoadingModel
    {
        public float  Progress   { get; private set; }
        public string StatusText { get; private set; } = "";
        public bool   IsComplete { get; private set; }

        public event Action OnChanged;

        public void SetProgress(float progress, string text)
        {
            Progress   = Mathf.Clamp01(progress);
            StatusText = text;
            OnChanged?.Invoke();
        }

        public void SetComplete()
        {
            IsComplete = true;
            SetProgress(1f, LocalizationManager.Instance?.GetTranslation("loading_ready") ?? "Ready!");
        }
    }
}
