using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using Technyx.Sdk.Devices;
using Technyx.Sdk.Http;
using UnityEngine;

namespace Technyx.Sdk
{
    public class TechnyxSdk : MonoBehaviour
    {
        public static TechnyxSdk Instance { get; private set; }
        public static AuthService Auth { get; private set; }
        public static DeviceService Devices { get; private set; }
        public static ApiClient Http { get; private set; }
        public static SdkConfig Config { get; private set; }
        public static TokenStorage TokenStorage { get; private set; }

        public static bool IsInitialized => Instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null)
                return;

            var go = new GameObject("[Technyx.Sdk]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<TechnyxSdk>();

            Config = SdkConfigLoader.Load();
            TokenStorage = new TokenStorage(Config.encryptionSalt);
            Http = new ApiClient(Config, TokenStorage);
            Auth = new AuthService(Http, TokenStorage, Config);
            Devices = new DeviceService(Http);

            Auth.RestoreSession();

            Debug.Log($"[Technyx.Sdk] Initialized. API: {Config.apiBaseUrl}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
