using UnityEngine;
using UnityEngine.SceneManagement;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class BootstrapSceneController : MonoBehaviour
    {
        private void Start()
        {
            SceneManager.LoadScene(SceneNames.StartMenu);
        }
    }
}
