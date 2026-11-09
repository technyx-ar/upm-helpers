using System;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using Technyx.Sdk.Devices;
using Technyx.Sdk.Http;
using Technyx.Sdk.Teams;
using UnityEngine;

namespace Technyx.Sdk
{
    public class TechnyxSdk : MonoBehaviour
    {
        public static TechnyxSdk Instance { get; private set; }
        public static AuthService Auth { get; private set; }
        public static DeviceService Devices { get; private set; }
        public static TeamService Teams { get; private set; }
        public static OrganizationService Organizations { get; private set; }
        public static ApiClient Http { get; private set; }
        public static SdkConfig Config { get; private set; }
        public static TokenStorage TokenStorage { get; private set; }

        public static bool IsInitialized => Instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null)
                return;

            var config = SdkConfigLoader.Load();

            var go = new GameObject("[Technyx.Sdk]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<TechnyxSdk>();

            Build(config);
            Auth.RestoreSession();

            Debug.Log($"[Technyx.Sdk] Initialized. API: {Config.apiBaseUrl}");
        }

        // Replaces the configuration of the running SDK. The services are rebuilt, so references to
        // TechnyxSdk.Auth, Devices and the other services taken earlier go stale and OnAuthStateChanged
        // has to be subscribed again. A different encryptionSalt makes the stored session unreadable.
        public static void Configure(SdkConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (Instance == null)
                throw new InvalidOperationException("The SDK is not initialized yet.");

            config.Validate();

            Auth?.Shutdown();
            SdkConfigLoader.Replace(config);
            Build(config);
            Auth.RestoreSession();

            Debug.Log($"[Technyx.Sdk] Reconfigured. API: {Config.apiBaseUrl}");
        }

        private static void Build(SdkConfig config)
        {
            Config = config;
            TokenStorage = new TokenStorage(config.encryptionSalt);
            Http = new ApiClient(config, TokenStorage);
            Auth = new AuthService(Http, TokenStorage, config);
            Devices = new DeviceService(Http);
            Teams = new TeamService(Http);
            Organizations = new OrganizationService(Http);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Auth?.Shutdown();
            Instance = null;
        }
    }
}
