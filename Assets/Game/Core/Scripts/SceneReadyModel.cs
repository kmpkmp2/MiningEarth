using System;

namespace DeepEarth.Core
{
    // MainGameScene의 초기화 완료 상태를 추적하는 순수 모델. 새 Singleton이 아니며,
    // GameBootstrap 인스턴스가 소유하고 LoadingPresenter가 참조를 얻어 폴링/구독한다.
    public class SceneReadyModel
    {
        [Flags]
        public enum ReadyFlag
        {
            None = 0,
            SaveDataApplied = 1 << 0,
            PlayerReady = 1 << 1,
            HUDReady = 1 << 2,
            InventoryReady = 1 << 3,
            CameraReady = 1 << 4,
            AudioConnected = 1 << 5,
            MapReady = 1 << 6,
            BattleSystemReady = 1 << 7,
            AddressablesInstantiateComplete = 1 << 8,
        }

        private const ReadyFlag CoreFlags =
            ReadyFlag.SaveDataApplied | ReadyFlag.HUDReady | ReadyFlag.InventoryReady |
            ReadyFlag.BattleSystemReady | ReadyFlag.AddressablesInstantiateComplete |
            ReadyFlag.PlayerReady | ReadyFlag.MapReady;

        private const ReadyFlag AllFlags = CoreFlags | ReadyFlag.CameraReady | ReadyFlag.AudioConnected;

        public ReadyFlag Completed { get; private set; }

        // Camera/Audio 핸드오프를 제외한 7개 항목 — GameBootstrap 자체 부트 체인만으로 충족된다.
        public bool IsCoreReady => !HasFailed && (Completed & CoreFlags) == CoreFlags;

        // 9개 전 항목 — Camera/Audio 핸드오프까지 끝나야 true가 된다.
        public bool IsReady => !HasFailed && (Completed & AllFlags) == AllFlags;

        public bool HasFailed { get; private set; }
        public Exception FailureException { get; private set; }

        public event Action OnChanged;

        public void Mark(ReadyFlag flag)
        {
            if (HasFailed) return;
            Completed |= flag;
            OnChanged?.Invoke();
        }

        public void Fail(Exception ex)
        {
            HasFailed = true;
            FailureException = ex;
            OnChanged?.Invoke();
        }
    }
}
