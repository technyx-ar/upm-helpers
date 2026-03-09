using Newtonsoft.Json;
using UnityEngine;

namespace Technyx.One.Config
{
    public static class OneConfigLoader
    {
        private static OneConfig _cached;

        public static OneConfig Load()
        {
            if (_cached != null)
                return _cached;

            var textAsset = Resources.Load<TextAsset>("OneConfig");
            if (textAsset == null)
            {
                Debug.LogWarning("[Technyx.One] OneConfig.json not found in Resources. Using defaults.");
                _cached = new OneConfig();
                return _cached;
            }

            _cached = JsonConvert.DeserializeObject<OneConfig>(textAsset.text) ?? new OneConfig();
            return _cached;
        }

        public static void Override(OneConfig config)
        {
            _cached = config;
        }

        public static void Reset()
        {
            _cached = null;
        }
    }
}
