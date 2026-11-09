using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Technyx.Sdk.Ai;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class AiServiceTests
    {
        private TestApi _api;
        private AiService _service;

        [SetUp]
        public void SetUp()
        {
            _api = new TestApi("_tnxtest_ai");
            _service = new AiService(_api.Client);
        }

        [TearDown]
        public void TearDown() => _api.Dispose();

        private static string Event(string name, string json) => "event: " + name + "\ndata: " + json + "\n\n";

        private const string DoneJson =
            "{\"message_ulid\":\"01M2\",\"conversation_ulid\":\"01C1\","
            + "\"grounding\":{\"grounded_ratio\":0.5,\"grounded_segments\":[{\"text\":\"Torque is 25 Nm.\",\"source_id\":\"s1\","
            + "\"document_meta\":{\"filename\":\"manual.pdf\",\"pages\":[6,7],\"file_url\":null}}],"
            + "\"generated_segments\":[{\"text\":\"Check it twice.\",\"confidence\":\"high\"}]},"
            + "\"sources\":[{\"type\":\"manual\",\"id\":\"s1\",\"title\":\"AMM\",\"relevance_score\":0.9,\"referenced\":true,"
            + "\"document_meta\":{\"filename\":\"manual.pdf\",\"pages\":[6,7]}}],"
            + "\"tokens\":{\"input\":120,\"output\":45},\"cost_cents\":2,\"provider\":\"openai\",\"model\":\"gpt-x\",\"latency_ms\":1830}";

        private sealed class Collector
        {
            public readonly List<string> Texts = new List<string>();
            public readonly List<int> Indexes = new List<int>();
            public readonly List<AiStreamSource> Sources = new List<AiStreamSource>();

            public void OnChunk(AiStreamChunk chunk)
            {
                Texts.Add(chunk.Text);
                Indexes.Add(chunk.Index);
            }

            public void OnSource(AiStreamSource source) => Sources.Add(source);
        }

        [Test]
        public async Task Chat_ReadsTheMessageWithSourcesGroundingAndTokens()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"message\":{\"id\":\"01M2\",\"role\":\"assistant\",\"content\":\"Torque is 25 Nm.\","
                + "\"sources\":[{\"type\":\"manual\",\"id\":\"s1\",\"title\":\"AMM\",\"relevance_score\":0.9,\"referenced\":true,\"document_meta\":null}],"
                + "\"grounding\":{\"grounded_ratio\":1,\"grounded_segments\":[],\"generated_segments\":[]},"
                + "\"provider\":\"openai\",\"model\":\"gpt-x\",\"tokens\":{\"input\":10,\"output\":5},\"created_at\":\"2026-03-17T10:00:00+00:00\"},"
                + "\"conversation\":{\"id\":\"01C1\",\"title\":\"Torque\",\"messages_count\":2}}}");

            var response = await _service.ChatAsync(new AiChatRequest { Message = "Torque?" });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("Torque is 25 Nm.", response.Data.Message.Content);
            Assert.AreEqual("assistant", response.Data.Message.Role);
            Assert.AreEqual("s1", response.Data.Message.Sources[0].Id);
            Assert.IsTrue(response.Data.Message.Sources[0].Referenced);
            Assert.AreEqual(0.9, response.Data.Message.Sources[0].RelevanceScore);
            Assert.AreEqual(1.0, response.Data.Message.Grounding.GroundedRatio);
            Assert.AreEqual(5, response.Data.Message.Tokens.Output);
            Assert.AreEqual("2026-03-17T10:00:00+00:00", response.Data.Message.CreatedAt);
            Assert.AreEqual("01C1", response.Data.Conversation.Id);
            Assert.AreEqual(2, response.Data.Conversation.MessagesCount);
        }

        [Test]
        public async Task Chat_SendsOnlyTheFieldsThatAreSet()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"message\":{\"id\":\"m\",\"role\":\"assistant\"},\"conversation\":{\"id\":\"c\"}}}");

            await _service.ChatAsync(new AiChatRequest { Message = "Hello" });

            Assert.AreEqual("POST", _api.Transport.Requests[0].Method);
            Assert.AreEqual("ai/chat", _api.Url(0));
            Assert.AreEqual("{\"message\":\"Hello\"}", _api.Body(0));
        }

        [Test]
        public async Task Chat_SendsLanguageConversationContextAndKnowledge()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"message\":{\"id\":\"m\",\"role\":\"assistant\"},\"conversation\":{\"id\":\"c\"}}}");

            await _service.ChatAsync(new AiChatRequest
            {
                Message = "Torque?",
                Language = LanguageCodes.English,
                ConversationId = "01C1",
                Context = new AiChatContext { TaskId = "01T1" },
                KnowledgeContext = new List<AiKnowledgeItem>
                {
                    new AiKnowledgeItem
                    {
                        SourceId = "s1",
                        SourceType = "manual",
                        Title = "AMM",
                        Content = "Torque is 25 Nm.",
                        RelevanceScore = 0.9,
                        DocumentMeta = new AiDocumentMeta { Filename = "manual.pdf", Pages = new List<int> { 6, 7 } },
                    },
                },
            });

            Assert.AreEqual(
                "{\"message\":\"Torque?\",\"language\":\"en\",\"conversation_ulid\":\"01C1\",\"context\":{\"task_ulid\":\"01T1\"},"
                + "\"kb_context\":[{\"source_id\":\"s1\",\"source_type\":\"manual\",\"title\":\"AMM\",\"content\":\"Torque is 25 Nm.\","
                + "\"relevance_score\":0.9,\"document_meta\":{\"filename\":\"manual.pdf\",\"pages\":[6,7]}}]}",
                _api.Body(0));
        }

        [Test]
        public async Task Chat_UsesTheLongTimeout()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"message\":{\"id\":\"m\",\"role\":\"assistant\"},\"conversation\":{\"id\":\"c\"}}}");

            await _service.ChatAsync(new AiChatRequest { Message = "x" });

            Assert.AreEqual(_api.Config.streamTimeoutSeconds, _api.Transport.Requests[0].TimeoutSeconds);
        }

        [Test]
        public async Task Chat_RateLimited_ExposesTheWaitTime()
        {
            _api.Transport.Enqueue(429,
                "{\"error\":{\"code\":\"AI_RATE_LIMIT\",\"message\":\"Too many requests.\",\"retry_after\":12}}", "Retry-After", "12");

            var response = await _service.ChatAsync(new AiChatRequest { Message = "x" });

            Assert.AreEqual("AI_RATE_LIMIT", response.Error.Code);
            Assert.AreEqual(12, response.Error.RetryAfterSeconds);
        }

        [Test]
        public async Task Chat_BudgetExceeded_KeepsTheDetails()
        {
            _api.Transport.Enqueue(403,
                "{\"error\":{\"code\":\"AI_BUDGET_EXCEEDED\",\"message\":\"Budget exceeded.\","
                + "\"details\":{\"budget_cents\":1000,\"used_cents\":1000,\"resets_at\":\"2026-04-01T00:00:00+00:00\"}}}");

            var response = await _service.ChatAsync(new AiChatRequest { Message = "x" });

            Assert.AreEqual("AI_BUDGET_EXCEEDED", response.Error.Code);
            Assert.AreEqual(1000, (int)response.Error.Details["budget_cents"]);
            Assert.AreEqual("2026-04-01T00:00:00+00:00", (string)response.Error.Details["resets_at"]);
        }

        [Test]
        public async Task Chat_UnknownConversation_ExposesTheFieldError()
        {
            _api.Transport.Enqueue(422,
                "{\"message\":\"x\",\"errors\":{\"conversation_ulid\":[\"Conversation not found, does not belong to this user, or is not active.\"]}}");

            var response = await _service.ChatAsync(new AiChatRequest { Message = "x", ConversationId = "nope" });

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("conversation_ulid").Count);
        }

        [Test]
        public void Chat_NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.ChatAsync(null));
        }

        [Test]
        public async Task Stream_DeliversChunksSourcesAndTheClosingEvent()
        {
            _api.Transport.EnqueueStream(200,
                Event("chunk", "{\"text\":\"Torque \",\"index\":0}"),
                Event("chunk", "{\"text\":\"is 25 Nm.\",\"index\":1}"),
                Event("source", "{\"source_id\":\"s1\",\"title\":\"AMM\",\"relevance_score\":0.9,\"document_meta\":{\"filename\":\"manual.pdf\"}}"),
                Event("done", DoneJson));
            var collected = new Collector();

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "Torque?" }, collected.OnChunk, collected.OnSource);

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("Torque is 25 Nm.", string.Concat(collected.Texts));
            CollectionAssert.AreEqual(new[] { 0, 1 }, collected.Indexes);
            Assert.AreEqual("s1", collected.Sources[0].SourceId);
            Assert.AreEqual("manual.pdf", (string)collected.Sources[0].DocumentMeta["filename"]);

            var done = response.Data;
            Assert.AreEqual("01M2", done.MessageId);
            Assert.AreEqual("01C1", done.ConversationId);
            Assert.AreEqual(0.5, done.Grounding.GroundedRatio);
            Assert.AreEqual("Torque is 25 Nm.", done.Grounding.GroundedSegments[0].Text);
            Assert.AreEqual(AiConfidence.High, done.Grounding.GeneratedSegments[0].Confidence);
            Assert.AreEqual(6, (int)done.Grounding.GroundedSegments[0].DocumentMeta["pages"][0]);
            Assert.AreEqual("s1", done.Sources[0].Id);
            Assert.AreEqual(45, done.Tokens.Output);
            Assert.AreEqual(2, done.CostCents);
            Assert.AreEqual("gpt-x", done.Model);
            Assert.AreEqual(1830, done.LatencyMs);
        }

        [Test]
        public async Task Stream_PostsToTheStreamRouteWithTheStreamTimeout()
        {
            _api.Transport.EnqueueStream(200, Event("done", DoneJson));

            await _service.ChatStreamAsync(new AiChatRequest { Message = "Hi" }, _ => { });

            Assert.AreEqual("POST", _api.Transport.Requests[0].Method);
            Assert.AreEqual("ai/chat/stream", _api.Url(0));
            Assert.AreEqual("{\"message\":\"Hi\"}", _api.Body(0));
            Assert.AreEqual(_api.Config.streamTimeoutSeconds, _api.Transport.Requests[0].TimeoutSeconds);
        }

        [Test]
        public async Task Stream_EventsSplitAtArbitraryByteBoundaries_AreReassembled()
        {
            var whole = Encoding.UTF8.GetBytes(
                Event("chunk", "{\"text\":\"Dobrý deň\",\"index\":0}") + Event("done", DoneJson));
            var chunks = new List<byte[]>();
            for (var i = 0; i < whole.Length; i += 7)
            {
                var length = Math.Min(7, whole.Length - i);
                var piece = new byte[length];
                Array.Copy(whole, i, piece, 0, length);
                chunks.Add(piece);
            }

            _api.Transport.EnqueueStreamBytes(200, chunks.ToArray());
            var collected = new Collector();

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, collected.OnChunk);

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("Dobrý deň", collected.Texts[0]);
        }

        [Test]
        public async Task Stream_LastEventWithoutBlankLine_IsStillDelivered()
        {
            _api.Transport.EnqueueStream(200, "event: done\ndata: " + DoneJson + "\n");

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, _ => { });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("01M2", response.Data.MessageId);
        }

        [Test]
        public async Task Stream_DoneEventCutOffMidLine_IsInterrupted()
        {
            _api.Transport.EnqueueStream(200, "event: done\ndata: " + DoneJson.Substring(0, 40));

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, _ => { });

            Assert.AreEqual(ApiErrorCodes.StreamInterrupted, response.Error.Code);
        }

        [Test]
        public async Task Stream_ErrorEvent_FailsWithTheBackendCodeAfterTheChunksThatArrived()
        {
            _api.Transport.EnqueueStream(200,
                Event("chunk", "{\"text\":\"Partial\",\"index\":0}"),
                Event("error", "{\"message\":\"Nastala interná chyba.\",\"code\":\"STREAM_ERROR\"}"));
            var collected = new Collector();

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, collected.OnChunk);

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.StreamError, response.Error.Code);
            Assert.AreEqual("Nastala interná chyba.", response.Error.Message);
            Assert.AreEqual("Partial", collected.Texts[0]);
        }

        [Test]
        public async Task Stream_ErrorEventWithoutCode_UsesTheStreamErrorCode()
        {
            _api.Transport.EnqueueStream(200, Event("error", "{\"message\":\"boom\"}"));

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, _ => { });

            Assert.AreEqual(ApiErrorCodes.StreamError, response.Error.Code);
            Assert.AreEqual("boom", response.Error.Message);
        }

        [Test]
        public async Task Stream_EndingWithoutTheClosingEvent_IsInterrupted()
        {
            _api.Transport.EnqueueStream(200, Event("chunk", "{\"text\":\"Half\",\"index\":0}"));
            var collected = new Collector();

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, collected.OnChunk);

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.StreamInterrupted, response.Error.Code);
            Assert.AreEqual("Half", collected.Texts[0]);
        }

        [Test]
        public async Task Stream_EmptyBody_IsInterrupted()
        {
            _api.Transport.EnqueueStream(200);

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, _ => { });

            Assert.AreEqual(ApiErrorCodes.StreamInterrupted, response.Error.Code);
        }

        [Test]
        public async Task Stream_UnknownEventsAndCommentsAreIgnored()
        {
            _api.Transport.EnqueueStream(200,
                ": keep-alive\n\n",
                Event("ping", "{}"),
                Event("chunk", "{\"text\":\"A\",\"index\":0}"),
                Event("done", DoneJson));
            var collected = new Collector();

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, collected.OnChunk);

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(1, collected.Texts.Count);
        }

        [Test]
        public async Task Stream_ThrowingCallbacks_DoNotStopTheStream()
        {
            _api.Transport.EnqueueStream(200,
                Event("chunk", "{\"text\":\"A\",\"index\":0}"),
                Event("source", "{\"source_id\":\"s1\"}"),
                Event("chunk", "{\"text\":\"B\",\"index\":1}"),
                Event("done", DoneJson));
            var seen = new List<string>();

            var response = await _service.ChatStreamAsync(
                new AiChatRequest { Message = "x" },
                chunk =>
                {
                    seen.Add(chunk.Text);
                    throw new InvalidOperationException("ui failed");
                },
                _ => throw new InvalidOperationException("ui failed"));

            Assert.IsTrue(response.IsSuccess);
            CollectionAssert.AreEqual(new[] { "A", "B" }, seen);
        }

        [Test]
        public async Task Stream_MalformedEvent_FailsAndStopsDeliveringChunks()
        {
            _api.Transport.EnqueueStream(200,
                Event("chunk", "{\"text\":\"A\",\"index\":0}"),
                Event("chunk", "this is not json"),
                Event("chunk", "{\"text\":\"C\",\"index\":2}"),
                Event("done", DoneJson));
            var collected = new Collector();

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, collected.OnChunk);

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.ParseError, response.Error.Code);
            CollectionAssert.AreEqual(new[] { "A" }, collected.Texts);
        }

        [Test]
        public async Task Stream_EventThatIsNotAnObject_FailsWithAParseError()
        {
            _api.Transport.EnqueueStream(200, Event("chunk", "[1,2]"));

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, _ => { });

            Assert.AreEqual(ApiErrorCodes.ParseError, response.Error.Code);
        }

        [Test]
        public async Task Stream_RejectedBeforeItStarts_ReturnsTheBackendError()
        {
            _api.Transport.Enqueue(429,
                "{\"error\":{\"code\":\"AI_RATE_LIMIT\",\"message\":\"Slow down.\",\"retry_after\":5}}", "Retry-After", "5");
            var collected = new Collector();

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, collected.OnChunk);

            Assert.AreEqual("AI_RATE_LIMIT", response.Error.Code);
            Assert.AreEqual(5, response.Error.RetryAfterSeconds);
            Assert.AreEqual(0, collected.Texts.Count);
        }

        [Test]
        public async Task Stream_NoOrganization_ReturnsTheBackendError()
        {
            _api.Transport.Enqueue(403, "{\"error\":{\"code\":\"AI_NO_ORGANIZATION\",\"message\":\"No organization.\"}}");

            var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, _ => { });

            Assert.AreEqual("AI_NO_ORGANIZATION", response.Error.Code);
        }

        [Test]
        public async Task Stream_CancelledByTheCaller_IsReportedAsCancelled()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                var response = await _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, _ => { }, null, cts.Token);

                Assert.IsTrue(response.IsCancelled);
            }
        }

        [Test]
        public void Stream_MissingArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => _service.ChatStreamAsync(null, _ => { }));
            Assert.Throws<ArgumentNullException>(() => _service.ChatStreamAsync(new AiChatRequest { Message = "x" }, null));
        }

        [Test]
        public async Task ListConversations_SendsFiltersAndReadsTheItems()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"data\":[{\"id\":\"01C1\",\"title\":\"Torque\",\"status\":\"active\",\"language\":\"sk\","
                + "\"messages_count\":4,\"last_message\":{\"role\":\"assistant\",\"content\":\"25 Nm\",\"created_at\":\"2026-03-17T10:00:00+00:00\"},"
                + "\"created_at\":\"2026-03-17T09:00:00+00:00\",\"updated_at\":\"2026-03-17T10:00:00+00:00\"}],\"links\":{},"
                + "\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":20,\"total\":1}}}");

            var response = await _service.ListConversationsAsync(
                AiConversationStatus.Active,
                new DateTimeOffset(2026, 3, 1, 1, 0, 0, TimeSpan.FromHours(1)),
                new DateTimeOffset(2026, 3, 31, 23, 59, 59, TimeSpan.Zero),
                1,
                20);

            Assert.AreEqual(
                "ai/conversations?page=1&per_page=20&status=active&from=2026-03-01%2000%3A00%3A00&to=2026-03-31%2023%3A59%3A59",
                _api.Url(0));
            var item = response.Data.Data[0];
            Assert.AreEqual("01C1", item.Id);
            Assert.AreEqual(AiConversationStatus.Active, item.Status);
            Assert.AreEqual(4, item.MessagesCount);
            Assert.AreEqual("25 Nm", item.LastMessage.Content);
            Assert.AreEqual("2026-03-17T10:00:00+00:00", item.UpdatedAt);
        }

        [Test]
        public async Task ListConversations_WithoutFilters_SendsOnlyThePage()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[],\"links\":{},\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":20,\"total\":0}}}");

            await _service.ListConversationsAsync();

            Assert.AreEqual("ai/conversations?page=1", _api.Url(0));
        }

        [Test]
        public async Task ListConversations_ConversationWithoutMessages_HasNoLastMessage()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"data\":[{\"id\":\"01C2\",\"title\":null,\"status\":\"closed\",\"language\":\"en\",\"messages_count\":0,"
                + "\"created_at\":\"2026-03-17T09:00:00+00:00\",\"updated_at\":\"2026-03-17T09:00:00+00:00\"}],\"links\":{},"
                + "\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":20,\"total\":1}}}");

            var response = await _service.ListConversationsAsync();

            Assert.IsNull(response.Data.Data[0].LastMessage);
            Assert.IsNull(response.Data.Data[0].Title);
        }

        [Test]
        public async Task GetConversation_ReadsTheMessagesAndTheirPaging()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"id\":\"01C1\",\"title\":\"Torque\",\"status\":\"active\",\"language\":\"sk\","
                + "\"context\":{\"task_ulid\":\"01T1\"},\"created_at\":\"2026-03-17T09:00:00+00:00\",\"updated_at\":\"2026-03-17T10:00:00+00:00\","
                + "\"messages\":[{\"id\":\"01M1\",\"role\":\"user\",\"content\":\"Torque?\",\"sources\":null,\"grounding\":null,"
                + "\"created_at\":\"2026-03-17T09:00:00+00:00\"},"
                + "{\"id\":\"01M2\",\"role\":\"assistant\",\"content\":\"25 Nm\",\"sources\":[],\"grounding\":{\"grounded_ratio\":0.0,"
                + "\"grounded_segments\":[],\"generated_segments\":[{\"text\":\"25 Nm\",\"confidence\":\"low\"}]},"
                + "\"provider\":\"openai\",\"model\":\"gpt-x\",\"tokens\":{\"input\":9,\"output\":3},\"audio_url\":\"https://cdn.test/a.mp3\","
                + "\"created_at\":\"2026-03-17T09:00:05+00:00\"}],"
                + "\"messages_meta\":{\"current_page\":1,\"per_page\":2,\"total\":5}}}");

            var response = await _service.GetConversationAsync("01C1", 1, 2);

            Assert.AreEqual("ai/conversations/01C1?page=1&per_page=2", _api.Url(0));
            var detail = response.Data;
            Assert.AreEqual("01T1", (string)detail.Context["task_ulid"]);
            Assert.AreEqual(2, detail.Messages.Count);
            Assert.AreEqual("user", detail.Messages[0].Role);
            Assert.IsNull(detail.Messages[0].Grounding);
            Assert.IsNull(detail.Messages[0].Tokens);
            Assert.IsNull(detail.Messages[0].Provider);
            Assert.AreEqual(0, detail.Messages[1].Sources.Count);
            Assert.AreEqual(AiConfidence.Low, detail.Messages[1].Grounding.GeneratedSegments[0].Confidence);
            Assert.AreEqual(3, detail.Messages[1].Tokens.Output);
            Assert.AreEqual("https://cdn.test/a.mp3", detail.Messages[1].AudioUrl);
            Assert.IsTrue(detail.MessagesMeta.HasMore);
            Assert.AreEqual(5, detail.MessagesMeta.Total);
        }

        [Test]
        public async Task GetConversation_EmptyContextEncodedAsArray_IsAnEmptyToken()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"id\":\"01C1\",\"title\":null,\"status\":\"active\",\"language\":\"sk\",\"context\":[],"
                + "\"created_at\":\"2026-03-17T09:00:00+00:00\",\"updated_at\":\"2026-03-17T10:00:00+00:00\","
                + "\"messages\":[],\"messages_meta\":{\"current_page\":1,\"per_page\":50,\"total\":0}}}");

            var response = await _service.GetConversationAsync("01C1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(JTokenType.Array, response.Data.Context.Type);
            Assert.AreEqual(0, response.Data.Messages.Count);
            Assert.IsFalse(response.Data.MessagesMeta.HasMore);
        }

        [TestCase(1, 50, 50, false)]
        [TestCase(1, 50, 51, true)]
        [TestCase(2, 50, 100, false)]
        [TestCase(2, 50, 101, true)]
        [TestCase(1, 50, 0, false)]
        public void MessagesMeta_KnowsWhetherMoreMessagesFollow(int page, int perPage, int total, bool expected)
        {
            var meta = new AiMessagesMeta { CurrentPage = page, PerPage = perPage, Total = total };

            Assert.AreEqual(expected, meta.HasMore);
        }

        [Test]
        public async Task GetConversation_NotFound_IsReported()
        {
            _api.Transport.Enqueue(404, "{\"message\":\"No query results for model.\"}");

            var response = await _service.GetConversationAsync("a/b");

            Assert.AreEqual("ai/conversations/a%2Fb?page=1", _api.Url(0));
            Assert.AreEqual(ApiErrorCodes.NotFound, response.Error.Code);
        }

        [Test]
        public void ConversationCalls_BlankId_Throw()
        {
            Assert.Throws<ArgumentException>(() => _service.GetConversationAsync(" "));
            Assert.Throws<ArgumentException>(() => _service.UpdateConversationAsync("", new UpdateAiConversationRequest()));
            Assert.Throws<ArgumentException>(() => _service.DeleteConversationAsync(null));
        }

        [Test]
        public async Task UpdateConversation_SendsOnlyTheChangedFields()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"id\":\"01C1\",\"title\":\"Renamed\",\"status\":\"archived\",\"language\":\"sk\",\"messages_count\":4,"
                + "\"created_at\":\"2026-03-17T09:00:00+00:00\",\"updated_at\":\"2026-03-17T10:00:00+00:00\"}}");

            var response = await _service.UpdateConversationAsync("01C1", new UpdateAiConversationRequest { Title = "Renamed" });

            Assert.AreEqual("PATCH", _api.Transport.Requests[0].Method);
            Assert.AreEqual("ai/conversations/01C1", _api.Url(0));
            Assert.AreEqual("{\"title\":\"Renamed\"}", _api.Body(0));
            Assert.AreEqual("Renamed", response.Data.Title);
        }

        [Test]
        public async Task UpdateConversation_InvalidStatus_ExposesTheFieldError()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"x\",\"errors\":{\"status\":[\"The selected status is invalid.\"]}}");

            var response = await _service.UpdateConversationAsync("01C1", new UpdateAiConversationRequest { Status = "deleted" });

            Assert.AreEqual("The selected status is invalid.", response.Error.GetFieldErrors("status")[0]);
        }

        [Test]
        public void UpdateConversation_NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateConversationAsync("01C1", null));
        }

        [Test]
        public async Task DeleteConversation_NoContent_IsSuccess()
        {
            _api.Transport.Enqueue(204, "");

            var response = await _service.DeleteConversationAsync("01C1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("DELETE", _api.Transport.Requests[0].Method);
            Assert.AreEqual("ai/conversations/01C1", _api.Url(0));
        }
    }
}
