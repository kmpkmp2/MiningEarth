using System;
using UnityEngine;

namespace DeepEarth.Common
{
    public static class LocalizationTrigger
    {
        public static void Trigger()
        {
            Debug.Log("LocalizationTrigger: Triggering Setup...");
            try
            {
                // Find and load Assembly-CSharp-Editor
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                System.Reflection.Assembly editorAssembly = null;
                foreach (var assembly in assemblies)
                {
                    if (assembly.GetName().Name == "Assembly-CSharp-Editor")
                    {
                        editorAssembly = assembly;
                        break;
                    }
                }

                if (editorAssembly == null)
                {
                    Debug.LogError("LocalizationTrigger: Assembly-CSharp-Editor not found in current AppDomain!");
                    return;
                }

                var type = editorAssembly.GetType("DeepEarth.Editor.LocalizationSetup");
                if (type == null)
                {
                    Debug.LogError("LocalizationTrigger: Type DeepEarth.Editor.LocalizationSetup not found in Assembly-CSharp-Editor!");
                    return;
                }

                var method = type.GetMethod("Setup", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogError("LocalizationTrigger: Method Setup not found in LocalizationSetup class!");
                    return;
                }

                method.Invoke(null, null);
                Debug.Log("LocalizationTrigger: Successfully invoked Setup()!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"LocalizationTrigger: Failed to invoke Setup due to exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
