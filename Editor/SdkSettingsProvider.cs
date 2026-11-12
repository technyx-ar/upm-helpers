using System;
using System.IO;
using Newtonsoft.Json;
using Technyx.Sdk.Config;
using UnityEditor;
using UnityEngine;

namespace Technyx.Sdk.Editor
{
    public static class SdkSettingsProvider
    {
        private const string ConfigDirectory = "Assets/Resources";
        private const string ConfigFileName = "TechnyxConfig.json";

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
            _config = _configPath != null
                ? JsonConvert.DeserializeObject<SdkConfig>(File.ReadAllText(_configPath)) ?? new SdkConfig()
                : new SdkConfig();
        }

        private static void OnGUI(string searchContext)
        {
            EnsureLoaded();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Technyx SDK Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_configPath == null)
            {
                EditorGUILayout.HelpBox(
                    $"No {ConfigDirectory}/{ConfigFileName} in this project, so the built-in defaults apply. Save creates the file.",
                    MessageType.Info);
            }

            _config.apiBaseUrl = EditorGUILayout.TextField("API Base URL", _config.apiBaseUrl);
            _config.tokenRefreshMarginSeconds = EditorGUILayout.IntField("Token Refresh Margin (s)", _config.tokenRefreshMarginSeconds);
            _config.encryptionSalt = EditorGUILayout.TextField("Encryption Salt", _config.encryptionSalt);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Requests", EditorStyles.boldLabel);
            _config.requestTimeoutSeconds = EditorGUILayout.IntField("Request Timeout (s)", _config.requestTimeoutSeconds);
            _config.streamTimeoutSeconds = EditorGUILayout.IntField("Stream Timeout (s)", _config.streamTimeoutSeconds);
            _config.uploadTimeoutSeconds = EditorGUILayout.IntField("Upload Timeout (s)", _config.uploadTimeoutSeconds);
            _config.maxRetries = EditorGUILayout.IntField("Max Retries", _config.maxRetries);
            _config.retryBaseDelaySeconds = EditorGUILayout.FloatField("Retry Base Delay (s)", _config.retryBaseDelaySeconds);
            _config.maxRetryDelaySeconds = EditorGUILayout.IntField("Max Retry Delay (s)", _config.maxRetryDelaySeconds);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Token refresh retry", EditorStyles.boldLabel);
            _config.refreshRetryBaseSeconds = EditorGUILayout.FloatField("Retry Base Delay (s)", _config.refreshRetryBaseSeconds);
            _config.refreshRetryMaxSeconds = EditorGUILayout.IntField("Retry Max Delay (s)", _config.refreshRetryMaxSeconds);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Save"))
                SaveConfig();
        }

        private static void SaveConfig()
        {
            try
            {
                _config.Validate();
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Technyx SDK", e.Message, "OK");
                return;
            }

            if (_configPath == null)
            {
                Directory.CreateDirectory(ConfigDirectory);
                _configPath = ConfigDirectory + "/" + ConfigFileName;
            }

            File.WriteAllText(_configPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
            AssetDatabase.Refresh();
            Debug.Log("[Technyx.Sdk] Configuration saved to " + _configPath);
        }

        // Only the game project's own Assets folder is writable; a package copy is read-only.
        private static string FindConfigPath()
        {
            foreach (var guid in AssetDatabase.FindAssets("TechnyxConfig t:TextAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/", StringComparison.Ordinal) && path.EndsWith("/" + ConfigFileName, StringComparison.Ordinal))
                    return path;
            }

            return null;
        }
    }
}
