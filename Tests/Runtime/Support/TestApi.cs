using System;
using System.Text;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Tests
{
    internal sealed class TestApi : IDisposable
    {
        private readonly TokenStorage _storage;

        public TestApi(string storagePrefix)
        {
            Config = new SdkConfig
            {
                apiBaseUrl = "https://api.test/api/v1",
                encryptionSalt = "test-salt",
                maxRetries = 0,
            };

            _storage = new TokenStorage(Config.encryptionSalt, storagePrefix);
            _storage.Clear();
            _storage.Save("test-token", DateTime.UtcNow.AddHours(1));

            Transport = new FakeHttpTransport();
            Client = new ApiClient(Config, _storage, Transport);
        }

        public SdkConfig Config { get; }

        public FakeHttpTransport Transport { get; }

        public ApiClient Client { get; }

        public string Url(int request) => Transport.Requests[request].Url.Substring(Config.apiBaseUrl.Length + 1);

        public string Body(int request) => Encoding.UTF8.GetString(Transport.Requests[request].Body);

        public void Dispose() => _storage.Clear();
    }
}
