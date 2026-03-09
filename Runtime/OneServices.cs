using Technyx.One.Auth;
using Technyx.One.Config;
using Technyx.One.Http;
using UnityEngine;

namespace Technyx.One
{
    public class OneServices : MonoBehaviour
    {
        public static OneServices Instance { get; private set; }
        public static AuthService Auth { get; private set; }
        public static ApiClient Http { get; private set; }
        public static OneConfig Config { get; private set; }
        public static TokenStorage TokenStorage { get; private set; }

        public static bool IsInitialized => Instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null)
                return;

            var go = new GameObject("[Technyx.One]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<OneServices>();

            Config = OneConfigLoader.Load();
            TokenStorage = new TokenStorage(Config.encryptionSalt);
            Http = new ApiClient(Config, TokenStorage);
            Auth = new AuthService(Http, TokenStorage, Config);

            Auth.RestoreSession();

            Debug.Log($"[Technyx.One] Initialized. API: {Config.apiBaseUrl}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
