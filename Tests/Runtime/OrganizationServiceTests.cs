using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using Technyx.Sdk.Teams;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class OrganizationServiceTests
    {
        private TestApi _api;
        private OrganizationService _service;

        [SetUp]
        public void SetUp()
        {
            _api = new TestApi("_tnxtest_orgs");
            _service = new OrganizationService(_api.Client);
        }

        [TearDown]
        public void TearDown() => _api.Dispose();

        private const string OrganizationJson =
            "{\"id\":\"01O1\",\"name\":\"Acme\",\"slug\":\"acme\",\"status\":\"active\","
            + "\"settings\":{\"invoice_prefix\":\"AC\",\"default_language\":\"sk\",\"inactivity_timeout\":30,\"theme\":\"dark\"},"
            + "\"logo\":\"https://cdn.test/logo.png\",\"users_count\":4,\"teams_count\":2,"
            + "\"created_at\":\"2026-03-17T10:00:00+00:00\",\"updated_at\":\"2026-03-18T10:00:00+00:00\"}";

        [Test]
        public async Task List_ReadsTheUnwrappedPaginatedCollection()
        {
            _api.Transport.Enqueue(200,
                "{\"data\":[" + OrganizationJson + "],\"links\":{},\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":15,\"total\":1}}");

            var response = await _service.ListAsync(OrganizationStatus.Active, 1, 15);

            Assert.AreEqual("organizations?page=1&per_page=15&status=active", _api.Url(0));
            var organization = response.Data.Data[0];
            Assert.AreEqual("01O1", organization.Id);
            Assert.AreEqual("acme", organization.Slug);
            Assert.AreEqual("AC", organization.Settings.InvoicePrefix);
            Assert.AreEqual(LanguageCodes.Slovak, organization.Settings.DefaultLanguage);
            Assert.AreEqual(30, organization.Settings.InactivityTimeout);
            Assert.AreEqual("dark", (string)organization.Settings.Extra["theme"]);
            Assert.AreEqual("https://cdn.test/logo.png", organization.Logo);
            Assert.AreEqual(4, organization.UsersCount);
            Assert.AreEqual(2, organization.TeamsCount);
            Assert.AreEqual("2026-03-18T10:00:00+00:00", organization.UpdatedAt);
        }

        [Test]
        public async Task Get_EmptySettingsEncodedAsArray_AreAnEmptyObject()
        {
            _api.Transport.Enqueue(200,
                "{\"data\":{\"id\":\"01O2\",\"name\":\"Bare\",\"slug\":\"bare\",\"status\":\"inactive\",\"settings\":[],\"logo\":null,\"created_at\":\"2026-03-17T10:00:00+00:00\"}}");

            var response = await _service.GetAsync("01O2");

            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Data.Settings);
            Assert.IsNull(response.Data.Settings.InvoicePrefix);
            Assert.IsNull(response.Data.Settings.InactivityTimeout);
            Assert.IsNull(response.Data.Logo);
            Assert.IsNull(response.Data.UsersCount);
        }

        [Test]
        public async Task Get_NullSettings_StayNull()
        {
            _api.Transport.Enqueue(200, "{\"data\":{\"id\":\"01O2\",\"name\":\"Bare\",\"settings\":null}}");

            var response = await _service.GetAsync("01O2");

            Assert.IsNull(response.Data.Settings);
        }

        [Test]
        public async Task Get_NonEmptyArrayAsSettings_IsAParseError()
        {
            _api.Transport.Enqueue(200, "{\"data\":{\"id\":\"01O2\",\"name\":\"Bad\",\"settings\":[1,2]}}");

            var response = await _service.GetAsync("01O2");

            Assert.AreEqual(ApiErrorCodes.ParseError, response.Error.Code);
        }

        [Test]
        public async Task List_WithoutStatus_OmitsTheFilter()
        {
            _api.Transport.Enqueue(200, "{\"data\":[],\"links\":{},\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":15,\"total\":0}}");

            await _service.ListAsync();

            Assert.AreEqual("organizations?page=1", _api.Url(0));
        }

        [Test]
        public async Task Create_SendsNameAndOnlyTheSettingsThatAreSet()
        {
            _api.Transport.Enqueue(201, "{\"data\":" + OrganizationJson + "}");

            var response = await _service.CreateAsync(new CreateOrganizationRequest
            {
                Name = "Acme",
                Settings = new OrganizationSettings { InvoicePrefix = "AC", InactivityTimeout = 30 },
            });

            Assert.AreEqual(201, response.StatusCode);
            Assert.AreEqual("{\"name\":\"Acme\",\"settings\":{\"invoice_prefix\":\"AC\",\"inactivity_timeout\":30}}", _api.Body(0));
        }

        [Test]
        public async Task Create_WithoutSettings_OmitsThem()
        {
            _api.Transport.Enqueue(201, "{\"data\":" + OrganizationJson + "}");

            await _service.CreateAsync(new CreateOrganizationRequest { Name = "Acme" });

            Assert.AreEqual("{\"name\":\"Acme\"}", _api.Body(0));
        }

        [Test]
        public async Task Update_SendsPatchWithOnlyTheChangedSettings()
        {
            _api.Transport.Enqueue(200, "{\"data\":" + OrganizationJson + "}");

            await _service.UpdateAsync("01O1", new UpdateOrganizationRequest
            {
                Settings = new OrganizationSettings { DefaultLanguage = LanguageCodes.Czech },
            });

            Assert.AreEqual("PATCH", _api.Transport.Requests[0].Method);
            Assert.AreEqual("organizations/01O1", _api.Url(0));
            Assert.AreEqual("{\"settings\":{\"default_language\":\"cs\"}}", _api.Body(0));
        }

        [Test]
        public async Task Update_ValidationFailure_ExposesNestedFieldErrors()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"x\",\"errors\":{\"settings.inactivity_timeout\":[\"Must be at least 5.\"]}}");

            var response = await _service.UpdateAsync("01O1", new UpdateOrganizationRequest
            {
                Settings = new OrganizationSettings { InactivityTimeout = 1 },
            });

            Assert.AreEqual("Must be at least 5.", response.Error.GetFieldErrors("settings.inactivity_timeout")[0]);
        }

        [Test]
        public async Task UploadLogo_IsAPostWithMethodOverrideAndTheFile()
        {
            _api.Transport.Enqueue(200, "{\"data\":" + OrganizationJson + "}");
            var image = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x00, 0xFF };

            var response = await _service.UploadLogoAsync("01O1", image, "logo.png", "image/png");

            Assert.IsTrue(response.IsSuccess);
            var request = _api.Transport.Requests[0];
            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("organizations/01O1", _api.Url(0));
            StringAssert.StartsWith("multipart/form-data; boundary=", request.ContentType);

            var text = Encoding.GetEncoding("ISO-8859-1").GetString(request.Body);
            StringAssert.Contains("name=\"_method\"\r\n\r\nPATCH\r\n", text);
            StringAssert.Contains("name=\"logo\"; filename=\"logo.png\"\r\nContent-Type: image/png\r\n\r\n", text);
            StringAssert.Contains(Encoding.GetEncoding("ISO-8859-1").GetString(image) + "\r\n", text);
        }

        [Test]
        public async Task UploadLogo_EmptyImage_FailsWithoutARequest()
        {
            var response = await _service.UploadLogoAsync("01O1", new byte[0], "logo.png", "image/png");

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("logo").Count);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task UploadLogo_TooLarge_FailsWithoutARequest()
        {
            var response = await _service.UploadLogoAsync("01O1", new byte[OrganizationService.MaxLogoBytes + 1], "logo.png", "image/png");

            Assert.IsFalse(response.IsSuccess);
            StringAssert.Contains("2048 KB", response.Error.GetFieldErrors("logo")[0]);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task UploadLogo_ExactlyTheLimit_IsSent()
        {
            _api.Transport.Enqueue(200, "{\"data\":" + OrganizationJson + "}");

            var response = await _service.UploadLogoAsync("01O1", new byte[OrganizationService.MaxLogoBytes], "logo.png", "image/png");

            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        public void UploadLogo_BlankOrganizationId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _service.UploadLogoAsync(" ", new byte[] { 1 }, "a.png", "image/png"));
        }

        [Test]
        public async Task Delete_NoContent_IsSuccess()
        {
            _api.Transport.Enqueue(204, "");

            var response = await _service.DeleteAsync("01O1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("organizations/01O1", _api.Url(0));
        }

        [Test]
        public void OrganizationSettings_RoundTripsUnknownKeys()
        {
            var settings = JsonConvert.DeserializeObject<OrganizationSettings>("{\"invoice_prefix\":\"X\",\"future\":{\"a\":1}}");

            Assert.AreEqual("X", settings.InvoicePrefix);
            Assert.AreEqual(1, (int)settings.Extra["future"]["a"]);
        }
    }
}
