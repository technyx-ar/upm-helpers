using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Technyx.Sdk.Config
{
    public static class SdkConfigLoader
    {
        private const string ResourceName = "TechnyxConfig";

        private static SdkConfig _cached;

        public static SdkConfig Load()
        {
            if (_cached != null)
                return _cached;

            var textAsset = Resources.Load<TextAsset>(ResourceName);
            var config = textAsset == null
                ? new SdkConfig()
                : JsonConvert.DeserializeObject<SdkConfig>(textAsset.text) ?? new SdkConfig();

            config.Validate();
            _cached = config;
            return _cached;
        }

        public static void Override(SdkConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (TechnyxSdk.IsInitialized)
                throw new InvalidOperationException("The SDK is already initialized. Use TechnyxSdk.Configure to replace its configuration.");

            config.Validate();
            _cached = config;
        }

        public static void Reset()
        {
            _cached = null;
        }

        internal static void Replace(SdkConfig config)
        {
            config.Validate();
            _cached = config;
        }
    }
}
