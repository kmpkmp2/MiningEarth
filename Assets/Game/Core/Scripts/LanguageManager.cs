using System;
using UnityEngine;

namespace DeepEarth.Core
{
    public static class LanguageManager
    {
        public static string CurrentLanguageCode => LocalizationManager.Instance.CurrentLanguageCode;
        
        public static event Action OnLanguageChanged
        {
            add => LocalizationManager.Instance.OnLanguageChanged += value;
            remove => LocalizationManager.Instance.OnLanguageChanged -= value;
        }

        public static string GetTranslation(string key)
        {
            return LocalizationManager.Instance.GetTranslation(key);
        }

        public static string GetFormatted(string key, params object[] args)
        {
            return LocalizationManager.Instance.GetFormatted(key, args);
        }

        public static void SetLanguage(string langCode)
        {
            LocalizationManager.Instance.SetLanguage(langCode);
        }

        public static void ToggleLanguage()
        {
            LocalizationManager.Instance.ToggleLanguage();
        }
    }
}
