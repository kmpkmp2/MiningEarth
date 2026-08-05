namespace DeepEarth.Core
{
    /// <summary>
    /// 현재 런(Run)이 진행 중인지 추적하는 플래그.
    /// LoadingPresenter에서 Create()로 생성되고 RunEnd 시 Clear()로 제거된다.
    /// SettingsUIPresenter가 "Exit Run" 버튼 표시 여부를 판단하는 데 사용한다.
    /// </summary>
    public class RunDataModel
    {
        private static RunDataModel _current;
        public static RunDataModel Current => _current;

        private RunDataModel() { }

        public static RunDataModel Create()
        {
            _current = new RunDataModel();
            return _current;
        }

        public static void Clear() => _current = null;
    }
}
