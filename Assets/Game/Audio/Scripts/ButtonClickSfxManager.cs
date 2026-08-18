using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeepEarth.Common;

namespace DeepEarth.Audio
{
    // 씬/프리팹/동적 인스턴스 어디서 만들어졌는지와 무관하게, 활성화된 모든 Button에
    // 기본 클릭음(AddressableKeys.UISFXButtonClick)을 자동으로 연결한다.
    // 개별 버튼에 다른 사운드가 필요하면 같은 GameObject에 ButtonSfxOverride를 추가해 재정의한다.
    // 기존 AudioManager(PlayUISound)를 그대로 재사용하며, 별도 오디오 재생 경로를 만들지 않는다.
    public class ButtonClickSfxManager : MonoBehaviour
    {
        private static ButtonClickSfxManager _instance;

        private const float ScanIntervalSeconds = 0.25f;
        private readonly HashSet<int> _hookedButtonIds = new HashSet<int>();

        // 어떤 씬 부트스트랩도 거치지 않고 앱 시작 시 1회 자동 생성된다 — 기존 진입 스크립트 수정 불필요.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("ButtonClickSfxManager");
            _instance = go.AddComponent<ButtonClickSfxManager>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable()
        {
            StartCoroutine(CoScanForButtons());
        }

        // 매 프레임이 아니라 일정 주기로만 검사 — 버튼 클릭음 연결은 지연되어도 체감상 문제없다.
        private IEnumerator CoScanForButtons()
        {
            var wait = new WaitForSecondsRealtime(ScanIntervalSeconds);
            while (true)
            {
                HookNewButtons();
                yield return wait;
            }
        }

        private void HookNewButtons()
        {
            var selectables = Selectable.allSelectablesArray;
            for (int i = 0; i < selectables.Length; i++)
            {
                if (!(selectables[i] is Button button)) continue;

                int id = button.GetInstanceID();
                if (_hookedButtonIds.Contains(id)) continue;
                _hookedButtonIds.Add(id);

                button.onClick.AddListener(() => PlayClickSound(button));
            }
        }

        private static void PlayClickSound(Button button)
        {
            if (button == null) return;

            string sfxId = AddressableKeys.UISFXButtonClick;
            if (button.TryGetComponent(out ButtonSfxOverride ov) && !string.IsNullOrEmpty(ov.OverrideSfxId))
                sfxId = ov.OverrideSfxId;

            AudioManager.Instance?.PlayUISound(sfxId);
        }
    }
}
