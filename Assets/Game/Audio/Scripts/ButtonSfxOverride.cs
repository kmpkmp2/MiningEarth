using UnityEngine;
using UnityEngine.UI;

namespace DeepEarth.Audio
{
    // 특정 버튼에 기본 클릭음(AddressableKeys.UISFXButtonClick)과 다른 사운드가 필요할 때만 붙인다.
    // 붙이지 않으면 ButtonClickSfxManager가 자동으로 기본 클릭음을 사용한다.
    [RequireComponent(typeof(Button))]
    public class ButtonSfxOverride : MonoBehaviour
    {
        [SerializeField] private string overrideSfxId;

        public string OverrideSfxId => overrideSfxId;
    }
}
