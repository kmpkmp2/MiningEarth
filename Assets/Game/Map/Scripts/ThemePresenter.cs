using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.Map
{
    public class ThemePresenter
    {
        private static ThemePresenter _instance;
        public static ThemePresenter Instance => _instance;

        private readonly ThemeData _model;
        private readonly ThemeView _view;
        private readonly DepthData _depthModel;

        public ThemeData Model => _model;

        public ThemePresenter(ThemeData model, ThemeView view, DepthData depthModel)
        {
            _model = model;
            _view = view;
            _depthModel = depthModel;
            _instance = this;

            _depthModel.OnDepthChanged += HandleDepthChanged;
            _model.OnThemeChanged += HandleThemeChanged;

            // Initial theme setup
            UpdateThemeForDepth(_depthModel.CurrentDepth).Forget();
        }

        public void Dispose()
        {
            if (_depthModel != null)
            {
                _depthModel.OnDepthChanged -= HandleDepthChanged;
            }
            if (_model != null)
            {
                _model.OnThemeChanged -= HandleThemeChanged;
            }
        }

        private void HandleDepthChanged(int newDepth)
        {
            UpdateThemeForDepth(newDepth).Forget();
        }

        private void HandleThemeChanged()
        {
            _view.ApplyThemeSettings(_model);
        }

        private async UniTaskVoid UpdateThemeForDepth(int depth)
        {
            string themeKey = DetermineThemeKey(depth);
            if (_model.ThemeKey == themeKey && _model.WallMaterial != null)
            {
                return; // already on this theme
            }

            // Load theme material via ResourceManager
            Material mat = await ResourceManager.Instance.LoadAssetAsync<Material>(themeKey);
            if (mat == null)
            {
                Debug.LogError($"ThemePresenter: Failed to load theme material for key '{themeKey}'");
                return;
            }

            _model.WallMaterial = mat;
            ConfigureThemeDataForTheme(themeKey, _model);
            _model.ThemeKey = themeKey; // Triggers HandleThemeChanged
        }

        private string DetermineThemeKey(int depth)
        {
            if (depth < 50) return AddressableKeys.ThemeDirt;
            if (depth < 150) return AddressableKeys.ThemeStone;
            if (depth < 300) return AddressableKeys.ThemeIron;
            if (depth < 500) return AddressableKeys.ThemeGold;
            return AddressableKeys.ThemeCrystal;
        }

        private void ConfigureThemeDataForTheme(string themeKey, ThemeData data)
        {
            switch (themeKey)
            {
                case AddressableKeys.ThemeDirt:
                    data.LightColor = new Color(1f, 0.98f, 0.9f);
                    data.LightIntensity = 1.2f;
                    data.AmbientColor = new Color(0.35f, 0.35f, 0.35f);
                    data.CameraBackgroundColor = new Color(0.12f, 0.1f, 0.1f);
                    data.EnableFog = false;
                    data.FogColor = Color.black;
                    break;
                case AddressableKeys.ThemeStone:
                    data.LightColor = new Color(0.8f, 0.85f, 0.9f);
                    data.LightIntensity = 0.8f;
                    data.AmbientColor = new Color(0.18f, 0.18f, 0.22f);
                    data.CameraBackgroundColor = new Color(0.08f, 0.08f, 0.1f);
                    data.EnableFog = true;
                    data.FogColor = new Color(0.08f, 0.08f, 0.1f);
                    break;
                case AddressableKeys.ThemeIron:
                    data.LightColor = new Color(0.6f, 0.5f, 0.4f);
                    data.LightIntensity = 0.45f;
                    data.AmbientColor = new Color(0.08f, 0.08f, 0.08f);
                    data.CameraBackgroundColor = new Color(0.03f, 0.03f, 0.04f);
                    data.EnableFog = true;
                    data.FogColor = new Color(0.03f, 0.03f, 0.04f);
                    break;
                case AddressableKeys.ThemeGold:
                    data.LightColor = new Color(1f, 0.82f, 0.35f);
                    data.LightIntensity = 0.95f;
                    data.AmbientColor = new Color(0.25f, 0.2f, 0.12f);
                    data.CameraBackgroundColor = new Color(0.1f, 0.08f, 0.05f);
                    data.EnableFog = true;
                    data.FogColor = new Color(0.1f, 0.08f, 0.05f);
                    break;
                case AddressableKeys.ThemeCrystal:
                    data.LightColor = new Color(0.4f, 0.75f, 1f);
                    data.LightIntensity = 0.75f;
                    data.AmbientColor = new Color(0.12f, 0.15f, 0.3f);
                    data.CameraBackgroundColor = new Color(0.05f, 0.05f, 0.12f);
                    data.EnableFog = true;
                    data.FogColor = new Color(0.05f, 0.05f, 0.12f);
                    break;
            }
        }
    }
}
