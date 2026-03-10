using System.IO;
using Newtonsoft.Json;
using Technyx.Sdk.Config;
using UnityEditor;
using UnityEngine;

namespace Technyx.Sdk.Editor
{
    public static class SdkSettingsProvider
    {
        private static SdkConfig _config;
        private static string _configPath;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Technyx SDK", SettingsScope.Project)
            {
                label = "Technyx SDK",
                guiHandler = OnGUI,
                keywords = new[] { "technyx", "sdk", "api", "auth", "token" },
            };
        }

        private static void EnsureLoaded()
        {
            if (_config != null)
                return;

            _configPath = FindConfigPath();
            if (_configPath != null)
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonConvert.DeserializeObject<SdkConfig>(json) ?? new SdkConfig();
            }
            else
            {
                _config = new SdkConfig();
            }
        }

        private static void OnGUI(string searchContext)
        {
            EnsureLoaded();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Technyx SDK Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _config.apiBaseUrl = EditorGUILayout.TextField("API Base URL", _config.apiBaseUrl);
            _config.tokenRefreshMarginSeconds = EditorGUILayout.IntField("Token Refresh Margin (s)", _config.tokenRefreshMarginSeconds);
            _config.encryptionSalt = EditorGUILayout.TextField("Encryption Salt", _config.encryptionSalt);
            _config.requestTimeoutSeconds = EditorGUILayout.IntField("Request Timeout (s)", _config.requestTimeoutSeconds);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Save"))
            {
                SaveConfig();
            }
        }

        private static void SaveConfig()
        {
            if (_configPath == null)
            {
                var dir = "Assets/Resources";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                _configPath = Path.Combine(dir, "TechnyxConfig.json");
            }

            var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
            AssetDatabase.Refresh();
            Debug.Log("[Technyx.Sdk] Configuration saved to " + _configPath);
        }

        private static string FindConfigPath()
        {
            var guids = AssetDatabase.FindAssets("TechnyxConfig t:TextAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("TechnyxConfig.json"))
                    return path;
            }

            return null;
        }
    }
}
