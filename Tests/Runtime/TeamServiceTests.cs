using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using Technyx.Sdk.Teams;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class TeamServiceTests
    {
        private TestApi _api;
        private TeamService _service;

        [SetUp]
        public void SetUp()
        {
            _api = new TestApi("_tnxtest_teams");
            _service = new TeamService(_api.Client);
        }

        [TearDown]
        public void TearDown() => _api.Dispose();

        [Test]
        public async Task List_ReadsTheUnwrappedPaginatedCollection()
        {
            _api.Transport.Enqueue(200,
                "{\"data\":[{\"id\":\"01T1\",\"name\":\"Line A\",\"description\":null,\"manager\":{\"id\":\"01U1\",\"name\":\"Eva\"},"
                + "\"members_count\":3,\"created_at\":\"2026-03-17T10:00:00+00:00\"},"
                + "{\"id\":\"01T2\",\"name\":\"Line B\",\"description\":\"Night\",\"members_count\":0,\"created_at\":\"2026-03-18T10:00:00+00:00\"}],"
                + "\"links\":{\"first\":\"u1\",\"last\":\"u2\",\"prev\":null,\"next\":\"u3\"},"
                + "\"meta\":{\"current_page\":1,\"last_page\":2,\"per_page\":15,\"total\":20,\"from\":1,\"to\":15}}");

            var response = await _service.ListAsync();

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(2, response.Data.Data.Count);
            Assert.IsTrue(response.Data.HasMorePages);
            Assert.AreEqual(20, response.Data.Total);
            Assert.AreEqual("Eva", response.Data.Data[0].Manager.Name);
            Assert.IsNull(response.Data.Data[1].Manager);
            Assert.IsNull(response.Data.Data[0].Members);
            Assert.AreEqual(3, response.Data.Data[0].MembersCount);
            Assert.AreEqual("2026-03-17T10:00:00+00:00", response.Data.Data[0].CreatedAt);
        }

        [Test]
        public async Task List_SendsPageAndPageSize()
        {
            _api.Transport.Enqueue(200, "{\"data\":[],\"links\":{},\"meta\":{\"current_page\":2,\"last_page\":2,\"per_page\":50,\"total\":60}}");

            await _service.ListAsync(2, 50);

            Assert.AreEqual("teams?page=2&per_page=50", _api.Url(0));
            Assert.AreEqual("GET", _api.Transport.Requests[0].Method);
        }

        [Test]
        public async Task List_WithoutPageSize_OmitsIt()
        {
            _api.Transport.Enqueue(200, "{\"data\":[],\"links\":{},\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":15,\"total\":0}}");

            await _service.ListAsync();

            Assert.AreEqual("teams?page=1", _api.Url(0));
        }

        [Test]
        public async Task Get_ReadsMembersAndManager()
        {
            _api.Transport.Enqueue(200,
                "{\"data\":{\"id\":\"01T1\",\"name\":\"Line A\",\"description\":\"d\",\"manager\":{\"id\":\"01U1\",\"name\":\"Eva\"},"
                + "\"members\":[{\"id\":\"01U1\",\"name\":\"Eva\",\"role\":\"manager\",\"joined_at\":\"2026-03-17T10:00:00+00:00\"},"
                + "{\"id\":\"01U2\",\"name\":\"Adam\",\"role\":null,\"joined_at\":null}],\"members_count\":2,\"created_at\":\"2026-03-17T09:00:00+00:00\"}}");

            var response = await _service.GetAsync("01T1");

            Assert.AreEqual("teams/01T1", _api.Url(0));
            Assert.AreEqual(2, response.Data.Members.Count);
            Assert.AreEqual("manager", response.Data.Members[0].Role);
            Assert.AreEqual("2026-03-17T10:00:00+00:00", response.Data.Members[0].JoinedAt);
            Assert.IsNull(response.Data.Members[1].Role);
            Assert.IsNull(response.Data.Members[1].JoinedAt);
        }

        [Test]
        public async Task Get_EscapesTheIdentifier()
        {
            _api.Transport.Enqueue(404, "{\"message\":\"No query results.\"}");

            var response = await _service.GetAsync("a/b?c");

            Assert.AreEqual("teams/a%2Fb%3Fc", _api.Url(0));
            Assert.AreEqual(ApiErrorCodes.NotFound, response.Error.Code);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void Get_BlankId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => _service.GetAsync(id));
        }

        [Test]
        public async Task Create_SendsOnlyTheFieldsThatAreSet()
        {
            _api.Transport.Enqueue(201, "{\"data\":{\"id\":\"01T9\",\"name\":\"New\",\"description\":null,\"members_count\":0,\"created_at\":\"2026-03-17T10:00:00+00:00\"}}");

            var response = await _service.CreateAsync(new CreateTeamRequest { Name = "New" });

            Assert.AreEqual(201, response.StatusCode);
            Assert.AreEqual("01T9", response.Data.Id);
            Assert.AreEqual("POST", _api.Transport.Requests[0].Method);
            Assert.AreEqual("{\"name\":\"New\"}", _api.Body(0));
        }

        [Test]
        public async Task Create_WithDescriptionAndManager_SendsBoth()
        {
            _api.Transport.Enqueue(201, "{\"data\":{\"id\":\"01T9\",\"name\":\"New\"}}");

            await _service.CreateAsync(new CreateTeamRequest { Name = "New", Description = "d", ManagerId = "01U1" });

            Assert.AreEqual("{\"name\":\"New\",\"description\":\"d\",\"manager_id\":\"01U1\"}", _api.Body(0));
        }

        [Test]
        public async Task Create_ValidationFailure_ExposesFieldErrors()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"The selected manager id is invalid.\",\"errors\":{\"manager_id\":[\"The selected manager id is invalid.\"]}}");

            var response = await _service.CreateAsync(new CreateTeamRequest { Name = "X", ManagerId = "nope" });

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual("The selected manager id is invalid.", response.Error.GetFieldErrors("manager_id")[0]);
        }

        [Test]
        public void Create_NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateAsync(null));
        }

        [Test]
        public async Task Update_NothingSet_SendsAnEmptyObject()
        {
            _api.Transport.Enqueue(200, "{\"data\":{\"id\":\"01T1\",\"name\":\"A\"}}");

            await _service.UpdateAsync("01T1", new UpdateTeamRequest());

            Assert.AreEqual("PATCH", _api.Transport.Requests[0].Method);
            Assert.AreEqual("{}", _api.Body(0));
        }

        [Test]
        public async Task Update_OnlyName_OmitsTheOtherFields()
        {
            _api.Transport.Enqueue(200, "{\"data\":{\"id\":\"01T1\",\"name\":\"Renamed\"}}");

            await _service.UpdateAsync("01T1", new UpdateTeamRequest { Name = "Renamed" });

            Assert.AreEqual("{\"name\":\"Renamed\"}", _api.Body(0));
        }

        [Test]
        public async Task Update_ClearingDescriptionAndManager_SendsExplicitNulls()
        {
            _api.Transport.Enqueue(200, "{\"data\":{\"id\":\"01T1\",\"name\":\"A\"}}");

            await _service.UpdateAsync("01T1", new UpdateTeamRequest().ClearDescription().ClearManager());

            Assert.AreEqual("{\"description\":null,\"manager_id\":null}", _api.Body(0));
        }

        [Test]
        public async Task Update_SettingManager_SendsTheUlid()
        {
            _api.Transport.Enqueue(200, "{\"data\":{\"id\":\"01T1\",\"name\":\"A\"}}");

            await _service.UpdateAsync("01T1", new UpdateTeamRequest().SetManager("01U7").SetDescription("text"));

            Assert.AreEqual("{\"description\":\"text\",\"manager_id\":\"01U7\"}", _api.Body(0));
        }

        [Test]
        public void Update_NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateAsync("01T1", null));
        }

        [Test]
        public async Task Delete_NoContent_IsSuccess()
        {
            _api.Transport.Enqueue(204, "");

            var response = await _service.DeleteAsync("01T1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("DELETE", _api.Transport.Requests[0].Method);
            Assert.AreEqual("teams/01T1", _api.Url(0));
        }

        [Test]
        public async Task Forbidden_IsReportedWithItsMessage()
        {
            _api.Transport.Enqueue(403, "{\"message\":\"This action is unauthorized.\"}");

            var response = await _service.ListAsync();

            Assert.AreEqual(ApiErrorCodes.Forbidden, response.Error.Code);
            Assert.AreEqual("This action is unauthorized.", response.Error.Message);
        }
    }
}
