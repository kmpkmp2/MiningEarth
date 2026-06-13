using UnityEngine;

namespace DeepEarth.Map
{
    public class ThemeView : MonoBehaviour
    {
        [SerializeField] private Light directionalLight;
        [SerializeField] private Camera mainCamera;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (directionalLight == null)
            {
                // Find directional light in scene
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        directionalLight = l;
                        break;
                    }
                }
            }
        }

        public void ApplyThemeSettings(ThemeData data)
        {
            if (directionalLight != null)
            {
                directionalLight.color = data.LightColor;
                directionalLight.intensity = data.LightIntensity;
            }

            if (mainCamera != null)
            {
                mainCamera.backgroundColor = data.CameraBackgroundColor;
            }

            RenderSettings.ambientLight = data.AmbientColor;
            RenderSettings.fog = data.EnableFog;
            RenderSettings.fogColor = data.FogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.04f;
        }
    }
}
