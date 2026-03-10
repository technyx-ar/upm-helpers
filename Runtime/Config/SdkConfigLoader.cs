using Newtonsoft.Json;
using UnityEngine;

namespace Technyx.Sdk.Config
{
    public static class SdkConfigLoader
    {
        private static SdkConfig _cached;

        public static SdkConfig Load()
        {
            if (_cached != null)
                return _cached;

            var textAsset = Resources.Load<TextAsset>("TechnyxConfig");
            if (textAsset == null)
            {
                Debug.LogWarning("[Technyx.Sdk] TechnyxConfig.json not found in Resources. Using defaults.");
                _cached = new SdkConfig();
                return _cached;
            }

            _cached = JsonConvert.DeserializeObject<SdkConfig>(textAsset.text) ?? new SdkConfig();
            return _cached;
        }

        public static void Override(SdkConfig config)
        {
            _cached = config;
        }

        public static void Reset()
        {
            _cached = null;
        }
    }
}
