using System;
using DeepEarth.Core;

namespace DeepEarth.Common
{
    // Static event bus — systems fire; AchievementManager listens.
    // No direct coupling between gameplay systems and Achievement logic.
    public static class GameEvents
    {
        public static event Action OnMonsterKilled;
        public static event Action<BlockType, int> OnOreMined;          // type, amount
        public static event Action<string> OnBossKilled;                // BossID.ToString()
        public static event Action<int> OnDepthReached;                 // current depth
        public static event Action OnPickaxeRepaired;
        public static event Action<BlockType> OnRepairWithOre;          // ore type used
        public static event Action<CharacterID> OnCharacterUnlocked;
        public static event Action OnRelicCollected;
        public static event Action OnRunStarted;
        public static event Action OnPlayerDied;
        public static event Action OnTreasureOpened;
        public static event Action OnTombstoneOpened;
        public static event Action OnLavaEncountered;
        public static event Action OnWaterEncountered;

        public static void FireMonsterKilled()                      => OnMonsterKilled?.Invoke();
        public static void FireOreMined(BlockType t, int n)         => OnOreMined?.Invoke(t, n);
        public static void FireBossKilled(string bossID)            => OnBossKilled?.Invoke(bossID);
        public static void FireDepthReached(int depth)              => OnDepthReached?.Invoke(depth);
        public static void FirePickaxeRepaired()                    => OnPickaxeRepaired?.Invoke();
        public static void FireRepairWithOre(BlockType oreType)     => OnRepairWithOre?.Invoke(oreType);
        public static void FireCharacterUnlocked(CharacterID id)    => OnCharacterUnlocked?.Invoke(id);
        public static void FireRelicCollected()                     => OnRelicCollected?.Invoke();
        public static void FireRunStarted()                         => OnRunStarted?.Invoke();
        public static void FirePlayerDied()                         => OnPlayerDied?.Invoke();
        public static void FireTreasureOpened()                     => OnTreasureOpened?.Invoke();
        public static void FireTombstoneOpened()                    => OnTombstoneOpened?.Invoke();
        public static void FireLavaEncountered()                    => OnLavaEncountered?.Invoke();
        public static void FireWaterEncountered()                   => OnWaterEncountered?.Invoke();
    }
}
