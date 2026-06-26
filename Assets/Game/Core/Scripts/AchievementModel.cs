using UnityEngine;

namespace DeepEarth.Core
{
    public class AchievementModel
    {
        public AchievementData Data { get; }
        public int CurrentProgress { get; private set; }
        public bool IsCompleted { get; private set; }

        public float ProgressRatio => Data.targetValue > 0
            ? Mathf.Clamp01((float)CurrentProgress / Data.targetValue)
            : (IsCompleted ? 1f : 0f);

        public AchievementModel(AchievementData data, int savedProgress, bool savedCompleted)
        {
            Data = data;
            CurrentProgress = savedProgress;
            IsCompleted = savedCompleted;
        }

        // Returns true when the achievement is newly completed.
        public bool AddProgress(int amount)
        {
            if (IsCompleted || amount <= 0) return false;
            CurrentProgress = Mathf.Min(CurrentProgress + amount, Data.targetValue);
            if (CurrentProgress >= Data.targetValue)
            {
                IsCompleted = true;
                return true;
            }
            return false;
        }

        // For monotonically-increasing stats (e.g. ReachDepth) where we track the max.
        public bool SetProgress(int value)
        {
            if (IsCompleted || value <= CurrentProgress) return false;
            int gained = value - CurrentProgress;
            return AddProgress(gained);
        }
    }
}
