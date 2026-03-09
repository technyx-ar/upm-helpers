using System.IO;
using Newtonsoft.Json;
using Technyx.One.Config;
using UnityEditor;
using UnityEngine;

namespace Technyx.One.Editor
{
    public static class OneSettingsProvider
    {
        private static OneConfig _config;
        private static string _configPath;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Technyx One", SettingsScope.Project)
            {
                label = "Technyx One",
                guiHandler = OnGUI,
                keywords = new[] { "technyx", "one", "api", "auth", "token" },
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
                _config = JsonConvert.DeserializeObject<OneConfig>(json) ?? new OneConfig();
            }
            else
            {
                _config = new OneConfig();
            }
        }

        private static void OnGUI(string searchContext)
        {
            EnsureLoaded();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Technyx One SDK Configuration", EditorStyles.boldLabel);
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

                _configPath = Path.Combine(dir, "OneConfig.json");
            }

            var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
            AssetDatabase.Refresh();
            Debug.Log("[Technyx.One] Configuration saved to " + _configPath);
        }

        private static string FindConfigPath()
        {
            var guids = AssetDatabase.FindAssets("OneConfig t:TextAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("OneConfig.json"))
                    return path;
            }

            return null;
        }
    }
}
