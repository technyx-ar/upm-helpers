using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Technyx.Sdk.Ai;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class AiAdminServiceTests
    {
        private TestApi _api;
        private AiAdminService _service;

        private const string UserUlid = "01JQ6Z8M3K4N5P7R9S0T1V2W3X";

        private const string UsageJson =
            "{\"success\":true,\"data\":{\"period\":{\"from\":\"2026-03-01\",\"to\":\"2026-03-17\"},"
            + "\"totals\":{\"requests\":12,\"tokens_input\":4000,\"tokens_output\":1500,\"cost_cents\":37,"
            + "\"avg_grounded_ratio\":0.81,\"avg_latency_ms\":1840,\"kb_context_usage_rate\":0.75},"
            + "\"by_day\":[{\"date\":\"2026-03-16\",\"requests\":\"5\",\"tokens_total\":\"2300\",\"cost_cents\":\"15\",\"avg_grounded_ratio\":\"0.80\"},"
            + "{\"date\":\"2026-03-17\",\"requests\":7,\"tokens_total\":3200,\"cost_cents\":22,\"avg_grounded_ratio\":null}],"
            + "\"by_user\":[{\"user_ulid\":\"" + UserUlid + "\",\"user_name\":\"Jana Nováková\",\"requests\":\"12\",\"cost_cents\":\"37\",\"avg_grounded_ratio\":\"0.81\"}]}}";

        private const string AuditJson =
            "{\"id\":\"01AUDIT1\",\"action\":\"chat_stream\",\"provider\":\"openai\",\"model\":\"gpt-x\","
            + "\"tokens\":{\"input\":120,\"output\":45,\"total\":165},\"cost_cents\":2,\"latency_ms\":1830,\"grounded_ratio\":0.5,"
            + "\"sources_count\":1,\"kb_context_provided\":true,\"language\":\"sk\","
            + "\"user\":{\"id\":\"" + UserUlid + "\",\"name\":\"Jana Nováková\"},\"conversation_id\":\"01CONV1\","
            + "\"created_at\":\"2026-03-17T10:00:00+00:00\"}";

        private const string PageMeta = "\"links\":{\"first\":null,\"last\":null,\"prev\":null,\"next\":null},"
            + "\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":50,\"total\":1}";

        [SetUp]
        public void SetUp()
        {
            _api = new TestApi("_tnxtest_aiadmin");
            _service = new AiAdminService(_api.Client);
        }

        [TearDown]
        public void TearDown() => _api.Dispose();

        [Test]
        public async Task GetUsage_WithoutFilter_ReadsTotalsAndBreakdowns()
        {
            _api.Transport.Enqueue(200, UsageJson);

            var response = await _service.GetUsageAsync();

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("admin/ai/usage", _api.Url(0));
            var stats = response.Data;
            Assert.AreEqual("2026-03-01", stats.Period.From);
            Assert.AreEqual("2026-03-17", stats.Period.To);
            Assert.AreEqual(12, stats.Totals.Requests);
            Assert.AreEqual(4000, stats.Totals.TokensInput);
            Assert.AreEqual(1500, stats.Totals.TokensOutput);
            Assert.AreEqual(37, stats.Totals.CostCents);
            Assert.AreEqual(0.81, stats.Totals.AvgGroundedRatio);
            Assert.AreEqual(1840, stats.Totals.AvgLatencyMs);
            Assert.AreEqual(0.75, stats.Totals.KbContextUsageRate);
            Assert.AreEqual(2, stats.ByDay.Count);
            Assert.AreEqual(1, stats.ByUser.Count);
            Assert.AreEqual(UserUlid, stats.ByUser[0].UserId);
            Assert.AreEqual("Jana Nováková", stats.ByUser[0].UserName);
        }

        [Test]
        public async Task GetUsage_SumsSentAsDecimalStrings_AreReadAsNumbers()
        {
            _api.Transport.Enqueue(200, UsageJson);

            var stats = (await _service.GetUsageAsync()).Data;

            Assert.AreEqual("2026-03-16", stats.ByDay[0].Date);
            Assert.AreEqual(5, stats.ByDay[0].Requests);
            Assert.AreEqual(2300, stats.ByDay[0].TokensTotal);
            Assert.AreEqual(15, stats.ByDay[0].CostCents);
            Assert.AreEqual(0.80, stats.ByDay[0].AvgGroundedRatio);
            Assert.AreEqual(12, stats.ByUser[0].Requests);
            Assert.AreEqual(37, stats.ByUser[0].CostCents);
            Assert.AreEqual(0.81, stats.ByUser[0].AvgGroundedRatio);
        }

        [Test]
        public async Task GetUsage_DayWithoutAGroundingAnalysis_HasANullRatio()
        {
            _api.Transport.Enqueue(200, UsageJson);

            var stats = (await _service.GetUsageAsync()).Data;

            Assert.IsNull(stats.ByDay[1].AvgGroundedRatio);
            Assert.AreEqual(3200, stats.ByDay[1].TokensTotal);
        }

        [Test]
        public async Task GetUsage_OrganizationWithoutRequests_ReadsEmptyBreakdowns()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"period\":{\"from\":\"2026-03-01\",\"to\":\"2026-03-17\"},"
                + "\"totals\":{\"requests\":0,\"tokens_input\":0,\"tokens_output\":0,\"cost_cents\":0,\"avg_grounded_ratio\":0,\"avg_latency_ms\":0,\"kb_context_usage_rate\":0},"
                + "\"by_day\":[],\"by_user\":[]}}");

            var stats = (await _service.GetUsageAsync()).Data;

            Assert.AreEqual(0, stats.Totals.Requests);
            Assert.IsEmpty(stats.ByDay);
            Assert.IsEmpty(stats.ByUser);
        }

        [Test]
        public async Task GetUsage_Filter_IsSentWithUtcCalendarDates()
        {
            _api.Transport.Enqueue(200, UsageJson);

            await _service.GetUsageAsync(new AiUsageFilter
            {
                From = new DateTimeOffset(2026, 3, 1, 0, 30, 0, TimeSpan.FromHours(2)),
                To = new DateTimeOffset(2026, 3, 17, 23, 0, 0, TimeSpan.Zero),
                Provider = "openai",
                Action = AiAuditAction.ChatStream,
                UserId = UserUlid,
            });

            Assert.AreEqual(
                "admin/ai/usage?from=2026-02-28&to=2026-03-17&provider=openai&action=chat_stream&user_ulid=" + UserUlid,
                _api.Url(0));
        }

        [Test]
        public async Task GetUsage_UnsetFilterFields_AreNotSent()
        {
            _api.Transport.Enqueue(200, UsageJson);

            await _service.GetUsageAsync(new AiUsageFilter { Provider = "openai" });

            Assert.AreEqual("admin/ai/usage?provider=openai", _api.Url(0));
        }

        [Test]
        public async Task GetUsage_WithoutThePermission_IsForbidden()
        {
            _api.Transport.Enqueue(403, "{\"message\":\"This action is unauthorized.\"}");

            var response = await _service.GetUsageAsync();

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.Forbidden, response.Error.Code);
        }

        [Test]
        public async Task GetUsage_UnknownAction_ExposesTheFieldError()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"The selected action is invalid.\",\"errors\":{\"action\":[\"The selected action is invalid.\"]}}");

            var response = await _service.GetUsageAsync(new AiUsageFilter { Action = "dictate" });

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("action").Count);
        }

        [Test]
        public async Task ListAudit_ReadsThePaginatedEntries()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[" + AuditJson + "]," + PageMeta + "}}");

            var response = await _service.ListAuditAsync();

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("admin/ai/audit?page=1", _api.Url(0));
            Assert.AreEqual(1, response.Data.Total);
            var entry = response.Data.Data[0];
            Assert.AreEqual("01AUDIT1", entry.Id);
            Assert.AreEqual(AiAuditAction.ChatStream, entry.Action);
            Assert.AreEqual("openai", entry.Provider);
            Assert.AreEqual("gpt-x", entry.Model);
            Assert.AreEqual(120, entry.Tokens.Input);
            Assert.AreEqual(45, entry.Tokens.Output);
            Assert.AreEqual(165, entry.Tokens.Total);
            Assert.AreEqual(2, entry.CostCents);
            Assert.AreEqual(1830, entry.LatencyMs);
            Assert.AreEqual(0.5, entry.GroundedRatio);
            Assert.AreEqual(1, entry.SourcesCount);
            Assert.IsTrue(entry.KbContextProvided);
            Assert.AreEqual("sk", entry.Language);
            Assert.AreEqual(UserUlid, entry.User.Id);
            Assert.AreEqual("Jana Nováková", entry.User.Name);
            Assert.AreEqual("01CONV1", entry.ConversationId);
            Assert.AreEqual("2026-03-17T10:00:00+00:00", entry.CreatedAt);
        }

        [Test]
        public async Task ListAudit_EntryInAList_HasNoSourcesDetailAndNoError()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[" + AuditJson + "]," + PageMeta + "}}");

            var entry = (await _service.ListAuditAsync()).Data.Data[0];

            Assert.IsNull(entry.SourcesDetail);
            Assert.IsNull(entry.Error);
        }

        [Test]
        public async Task ListAudit_EntryWithoutGrounding_HasANullRatio()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"data\":[{\"id\":\"01AUDIT2\",\"action\":\"voice_audio\",\"provider\":\"openai\",\"model\":\"whisper\","
                + "\"tokens\":{\"input\":0,\"output\":0,\"total\":0},\"cost_cents\":0,\"latency_ms\":900,\"grounded_ratio\":null,"
                + "\"sources_count\":0,\"kb_context_provided\":false,\"language\":\"en\",\"error\":\"Provider timeout\","
                + "\"user\":{\"id\":\"" + UserUlid + "\",\"name\":\"x\"},\"conversation_id\":null,\"created_at\":\"2026-03-17T11:00:00+00:00\"}],"
                + PageMeta + "}}");

            var entry = (await _service.ListAuditAsync()).Data.Data[0];

            Assert.IsNull(entry.GroundedRatio);
            Assert.AreEqual("Provider timeout", entry.Error);
            Assert.IsNull(entry.ConversationId);
            Assert.IsFalse(entry.KbContextProvided);
        }

        [Test]
        public async Task ListAudit_Filter_IsSentInDocumentedOrder()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[]," + PageMeta + "}}");

            await _service.ListAuditAsync(
                new AiAuditFilter
                {
                    From = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
                    To = new DateTimeOffset(2026, 3, 17, 8, 0, 0, TimeSpan.Zero),
                    Provider = "openai",
                    Action = AiAuditAction.VoiceCommand,
                    UserId = UserUlid,
                    GroundedRatioMin = 0.25,
                    GroundedRatioMax = 0.9,
                },
                page: 2,
                perPage: 20);

            Assert.AreEqual(
                "admin/ai/audit?page=2&per_page=20&from=2026-03-01&to=2026-03-17&provider=openai&action=voice_command&user_ulid="
                + UserUlid + "&grounded_ratio_min=0.25&grounded_ratio_max=0.9",
                _api.Url(0));
        }

        [Test]
        public async Task ListAudit_UserWithoutAnOrganization_ReturnsMetaWithoutLastPageAsAParseError()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[],\"meta\":{\"current_page\":1,\"per_page\":50,\"total\":0}}}");

            var response = await _service.ListAuditAsync();

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.ParseError, response.Error.Code);
        }

        [Test]
        public async Task ListAudit_WithoutThePermission_IsForbidden()
        {
            _api.Transport.Enqueue(403, "{\"message\":\"This action is unauthorized.\"}");

            var response = await _service.ListAuditAsync();

            Assert.AreEqual(ApiErrorCodes.Forbidden, response.Error.Code);
        }

        [Test]
        public async Task ListAudit_RatioOutOfRange_ExposesTheFieldError()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"The grounded ratio min field must be between 0 and 1.\",\"errors\":{\"grounded_ratio_min\":[\"The grounded ratio min field must be between 0 and 1.\"]}}");

            var response = await _service.ListAuditAsync(new AiAuditFilter { GroundedRatioMin = 2 });

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("grounded_ratio_min").Count);
        }

        [Test]
        public async Task GetAudit_ReadsTheSourcesDetail()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":" + AuditJson.TrimEnd('}')
                + ",\"sources_detail\":[{\"type\":\"manual\",\"id\":\"s1\",\"title\":\"AMM\",\"relevance_score\":0.9,\"referenced\":true,"
                + "\"document_meta\":{\"filename\":\"manual.pdf\",\"pages\":[6,7]}}]}}");

            var response = await _service.GetAuditAsync("01AUDIT1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("admin/ai/audit/01AUDIT1", _api.Url(0));
            Assert.AreEqual(1, response.Data.SourcesDetail.Count);
            var source = response.Data.SourcesDetail[0];
            Assert.AreEqual("manual", source.Type);
            Assert.AreEqual("s1", source.Id);
            Assert.AreEqual("AMM", source.Title);
            Assert.AreEqual(0.9, source.RelevanceScore);
            Assert.IsTrue(source.Referenced);
        }

        [Test]
        public async Task GetAudit_EntryWithoutSources_HasANullSourcesDetail()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":" + AuditJson.TrimEnd('}') + ",\"sources_detail\":null}}");

            var response = await _service.GetAuditAsync("01AUDIT1");

            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Data.SourcesDetail);
        }

        [Test]
        public async Task GetAudit_EntryOfAnotherOrganization_IsNotFound()
        {
            _api.Transport.Enqueue(404, "{\"message\":\"No query results for model [Modules\\\\AI\\\\Models\\\\AiAuditLog].\"}");

            var response = await _service.GetAuditAsync("01OTHER");

            Assert.AreEqual(ApiErrorCodes.NotFound, response.Error.Code);
        }

        [Test]
        public void GetAudit_EmptyId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _service.GetAuditAsync(" "));
        }

        [Test]
        public async Task GetAudit_IdIsEscapedIntoThePath()
        {
            _api.Transport.Enqueue(404, "{\"message\":\"Not found.\"}");

            await _service.GetAuditAsync("a/b");

            Assert.AreEqual("admin/ai/audit/a%2Fb", _api.Url(0));
        }
    }
}
