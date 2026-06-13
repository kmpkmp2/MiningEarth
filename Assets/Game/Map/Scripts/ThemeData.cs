using System;
using UnityEngine;

namespace DeepEarth.Map
{
    public class ThemeData
    {
        private string _themeKey;
        public string ThemeKey
        {
            get => _themeKey;
            set
            {
                if (_themeKey != value)
                {
                    _themeKey = value;
                    OnThemeChanged?.Invoke();
                }
            }
        }

        public Material WallMaterial { get; set; }
        public Color LightColor { get; set; }
        public float LightIntensity { get; set; }
        public Color AmbientColor { get; set; }
        public Color CameraBackgroundColor { get; set; }
        public bool EnableFog { get; set; }
        public Color FogColor { get; set; }

        public event Action OnThemeChanged;
    }
}
