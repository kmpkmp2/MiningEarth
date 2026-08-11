using UnityEngine;

namespace DeepEarth.Core
{
    // StartMenuScene 전용 애플리케이션 완전 종료 서비스.
    // 런 종료/정산 시스템과 완전히 분리되어 있으며, 이 클래스는 플랫폼 종료 요청만 담당한다.
    public static class GameExitService
    {
        public static void QuitApplication()
        {
            Application.Quit();
        }
    }
}
