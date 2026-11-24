using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using Technyx.Sdk.Voice;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class VoiceServiceTests
    {
        private TestApi _api;
        private VoiceService _service;

        [SetUp]
        public void SetUp()
        {
            _api = new TestApi("_tnxtest_voice");
            _service = new VoiceService(_api.Client);
        }

        [TearDown]
        public void TearDown() => _api.Dispose();

        private static string Latin1(byte[] bytes) => Encoding.GetEncoding("ISO-8859-1").GetString(bytes);

        [Test]
        public async Task CreateSession_Realtime_ReadsTheEphemeralKeyAndSessionToken()
        {
            _api.Transport.Enqueue(201,
                "{\"success\":true,\"data\":{\"id\":\"01S1\",\"status\":\"created\",\"transport\":\"oai_rt\",\"language\":\"sk\","
                + "\"config\":{\"provider\":\"openai_realtime\",\"model\":\"gpt-realtime\",\"voice\":\"marin\"},"
                + "\"started_at\":null,\"ended_at\":null,\"created_at\":\"2026-11-24T09:00:00+00:00\","
                + "\"ephemeral_key\":\"ek_abc\",\"session_token\":\"tok123\"}}");

            var response = await _service.CreateSessionAsync(new CreateVoiceSessionRequest
            {
                Language = LanguageCodes.Slovak,
                Transport = VoiceTransports.OpenAiRealtime,
            });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("POST", _api.Transport.Requests[0].Method);
            Assert.AreEqual("voice/sessions", _api.Url(0));
            Assert.AreEqual("sk", (string)JObject.Parse(_api.Body(0))["language"]);
            Assert.AreEqual("oai_rt", (string)JObject.Parse(_api.Body(0))["transport"]);

            var session = response.Data;
            Assert.AreEqual("01S1", session.Id);
            Assert.AreEqual(VoiceSessionStatus.Created, session.Status);
            Assert.AreEqual("ek_abc", session.EphemeralKey);
            Assert.AreEqual("tok123", session.SessionToken);
            Assert.AreEqual("gpt-realtime", (string)session.Config["model"]);
            Assert.IsNull(session.StartedAt);
            Assert.IsNull(session.Token);
        }

        [Test]
        public async Task CreateSession_LiveKit_ReadsTheRoomAndParticipantToken()
        {
            _api.Transport.Enqueue(201,
                "{\"success\":true,\"data\":{\"id\":\"01S2\",\"status\":\"created\",\"transport\":\"webrtc\",\"language\":\"en\","
                + "\"config\":null,\"room_name\":\"voice-abc\",\"token\":\"jwt\",\"livekit_url\":\"wss://livekit.prod.technyx.ar\"}}");

            var response = await _service.CreateSessionAsync(new CreateVoiceSessionRequest
            {
                Language = LanguageCodes.English,
                Transport = VoiceTransports.LiveKit,
            });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("voice-abc", response.Data.RoomName);
            Assert.AreEqual("jwt", response.Data.Token);
            Assert.AreEqual("wss://livekit.prod.technyx.ar", response.Data.LiveKitUrl);
            Assert.IsNull(response.Data.Config);
            Assert.IsNull(response.Data.EphemeralKey);
        }

        [Test]
        public async Task CreateSession_ProviderFailure_ExposesTheBackendError()
        {
            _api.Transport.Enqueue(502, "{\"message\":\"OpenAI Realtime createEphemeralKey failed\"}");

            var response = await _service.CreateSessionAsync(new CreateVoiceSessionRequest
            {
                Language = "sk",
                Transport = VoiceTransports.OpenAiRealtime,
            });

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(502, response.StatusCode);
        }

        [Test]
        public void CreateSession_NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateSessionAsync(null));
        }

        [Test]
        public async Task GetSession_UsesTheEscapedId()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"id\":\"01S1\",\"status\":\"active\"}}");

            var response = await _service.GetSessionAsync("01S1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("voice/sessions/01S1", _api.Url(0));
            Assert.AreEqual(VoiceSessionStatus.Active, response.Data.Status);
        }

        [Test]
        public void GetSession_EmptyId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _service.GetSessionAsync(" "));
        }

        [Test]
        public async Task EndSession_PostsToTheEndPath()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"id\":\"01S1\",\"status\":\"ended\",\"ended_at\":\"2026-11-24T09:30:00+00:00\"}}");

            var response = await _service.EndSessionAsync("01S1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("POST", _api.Transport.Requests[0].Method);
            Assert.AreEqual("voice/sessions/01S1/end", _api.Url(0));
            Assert.AreEqual(VoiceSessionStatus.Ended, response.Data.Status);
        }

        [Test]
        public async Task SendCommand_SendsTheTextAndReadsTheAnswer()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"text\":\"Hotovo\",\"audio_url\":null,\"conversation_id\":\"01C1\",\"sources\":[]}}");

            var response = await _service.SendCommandAsync(new VoiceCommandRequest
            {
                Text = "ukaz ulohy",
                Language = LanguageCodes.Slovak,
                Context = new AiChatContext { TaskId = "01T1" },
            });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("voice/command", _api.Url(0));
            Assert.AreEqual("Hotovo", response.Data.Text);
            Assert.AreEqual("01C1", response.Data.ConversationId);
            Assert.IsNull(response.Data.AudioUrl);
            Assert.AreEqual(0, response.Data.Sources.Count);

            var body = JObject.Parse(_api.Body(0));
            Assert.AreEqual("ukaz ulohy", (string)body["text"]);
            Assert.AreEqual("01T1", (string)body["context"]["task_ulid"]);
            Assert.IsNull(body["context"]["location"]);
        }

        [Test]
        public async Task SendCommand_WithoutContext_OmitsIt()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"text\":\"ok\",\"conversation_id\":\"01C1\",\"sources\":[]}}");

            await _service.SendCommandAsync(new VoiceCommandRequest { Text = "hi", Language = "en" });

            Assert.IsNull(JObject.Parse(_api.Body(0))["context"]);
        }

        [Test]
        public async Task SendAudio_SendsTheFileLanguageAndContextFields()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"text\":\"ok\",\"audio_url\":\"https://s3.test/a.mp3\",\"conversation_id\":\"01C2\",\"sources\":[]}}");

            var response = await _service.SendAudioAsync(
                new byte[] { 1, 2, 3 },
                "cmd.wav",
                "audio/wav",
                LanguageCodes.Slovak,
                new AiChatContext { TaskId = "01T1", Location = "Hangar 2" });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("https://s3.test/a.mp3", response.Data.AudioUrl);

            var request = _api.Transport.Requests[0];
            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("voice/audio", _api.Url(0));
            StringAssert.StartsWith("multipart/form-data; boundary=", request.ContentType);

            var text = Latin1(request.Body);
            StringAssert.Contains("name=\"audio\"; filename=\"cmd.wav\"", text);
            StringAssert.Contains("name=\"language\"\r\n\r\nsk\r\n", text);
            StringAssert.Contains("name=\"context[task_ulid]\"\r\n\r\n01T1\r\n", text);
            StringAssert.Contains("name=\"context[location]\"\r\n\r\nHangar 2\r\n", text);
        }

        [Test]
        public async Task SendAudio_WithoutContext_SendsNoContextFields()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"text\":\"\",\"conversation_id\":\"\",\"sources\":[]}}");

            var response = await _service.SendAudioAsync(new byte[] { 1 }, "cmd.ogg", "audio/ogg", "en");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("", response.Data.Text);
            StringAssert.DoesNotContain("context[", Latin1(_api.Transport.Requests[0].Body));
        }

        [Test]
        public async Task SendAudio_UnsupportedFormat_FailsWithoutARequest()
        {
            var response = await _service.SendAudioAsync(new byte[] { 1 }, "cmd.m4a", "audio/mp4", "en");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("audio").Count);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task SendAudio_EmptyOrMissingLanguage_FailsWithoutARequest()
        {
            var empty = await _service.SendAudioAsync(new byte[0], "cmd.wav", "audio/wav", "en");
            var noLanguage = await _service.SendAudioAsync(new byte[] { 1 }, "cmd.wav", "audio/wav", null);

            Assert.IsFalse(empty.IsSuccess);
            Assert.IsFalse(noLanguage.IsSuccess);
            Assert.AreEqual(1, noLanguage.Error.GetFieldErrors("language").Count);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task SendAudio_TooLarge_FailsWithoutARequest()
        {
            var response = await _service.SendAudioAsync(new byte[VoiceService.MaxAudioBytes + 1], "cmd.wav", "audio/wav", "en");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task ListHistory_BuildsTheQueryAndReadsThePage()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"data\":[{\"id\":\"01H1\",\"role\":\"assistant\",\"content\":\"Ahoj\","
                + "\"audio_url\":null,\"context\":{\"location\":\"Hangar 2\"},\"created_at\":\"2026-11-24T09:00:00+00:00\"}],"
                + "\"links\":{},\"meta\":{\"current_page\":2,\"last_page\":3,\"per_page\":10,\"total\":25}}}");

            var response = await _service.ListHistoryAsync(2, "01S1", 10);

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("voice/history?page=2&session_id=01S1&per_page=10", _api.Url(0));
            Assert.IsTrue(response.Data.HasMorePages);
            Assert.AreEqual("assistant", response.Data.Data[0].Role);
            Assert.AreEqual("Hangar 2", (string)response.Data.Data[0].Context["location"]);
        }

        [Test]
        public async Task ListHistory_Defaults_OnlySendThePage()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"data\":[],\"links\":{},\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":20,\"total\":0}}}");

            await _service.ListHistoryAsync();

            Assert.AreEqual("voice/history?page=1", _api.Url(0));
        }

        [Test]
        public async Task SendOffer_ReturnsTheAnswerSdp()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"sdp\":\"v=0 answer\",\"type\":\"answer\"}}");

            var response = await _service.SendOfferAsync(new VoiceSdpOfferRequest { SessionToken = "tok123", Sdp = "v=0 offer" });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("voice/realtime/webrtc/offer", _api.Url(0));
            Assert.AreEqual("v=0 answer", response.Data.Sdp);
            Assert.AreEqual("answer", response.Data.Type);

            var body = JObject.Parse(_api.Body(0));
            Assert.AreEqual("tok123", (string)body["session_token"]);
            Assert.AreEqual("offer", (string)body["type"]);
        }

        [Test]
        public async Task SendOffer_ForALiveKitSession_ExposesTheUnprocessableError()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"Signaling is only available for oai_rt sessions.\"}");

            var response = await _service.SendOfferAsync(new VoiceSdpOfferRequest { SessionToken = "tok", Sdp = "v=0" });

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(422, response.StatusCode);
        }

        [Test]
        public async Task SendIceCandidate_OmitsUnsetFields()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":null}");

            var response = await _service.SendIceCandidateAsync(new VoiceIceCandidateRequest
            {
                SessionToken = "tok123",
                Candidate = "candidate:1 1 UDP 2122 10.0.0.1 5000 typ host",
            });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("voice/realtime/webrtc/ice", _api.Url(0));

            var body = JObject.Parse(_api.Body(0));
            Assert.IsNull(body["sdp_mid"]);
            Assert.IsNull(body["sdp_m_line_index"]);

            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":null}");
            await _service.SendIceCandidateAsync(new VoiceIceCandidateRequest
            {
                SessionToken = "tok123",
                Candidate = "c",
                SdpMid = "0",
                SdpMLineIndex = 0,
            });

            var full = JObject.Parse(_api.Body(1));
            Assert.AreEqual("0", (string)full["sdp_mid"]);
            Assert.AreEqual(0, (int)full["sdp_m_line_index"]);
        }
    }
}
