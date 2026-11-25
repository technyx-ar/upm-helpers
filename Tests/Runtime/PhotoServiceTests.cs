using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using Technyx.Sdk.Photos;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class PhotoServiceTests
    {
        private TestApi _api;
        private PhotoService _service;

        [SetUp]
        public void SetUp()
        {
            _api = new TestApi("_tnxtest_photos");
            _service = new PhotoService(_api.Client);
        }

        [TearDown]
        public void TearDown() => _api.Dispose();

        private const string PhotoJson =
            "{\"id\":\"01P1\",\"name\":\"site_visit\",\"tags\":[{\"id\":\"01G1\",\"name\":\"roof\"}],"
            + "\"url\":\"https://s3.test/a.jpg\",\"preview\":\"https://s3.test/a-preview.jpg\",\"description\":\"north side\","
            + "\"gps_lat\":48.1486,\"gps_lng\":17.1077,\"timestamp\":\"2026-03-17T10:00:00.000000Z\","
            + "\"user\":\"01JQ6Z8M3K4N5P7R9S0T1V2W3X\",\"task\":\"01JQ6Z8M3K4N5P7R9S0T1V2W4Y\","
            + "\"meta\":{\"shift\":\"night\",\"floor\":3},\"created_at\":\"2026-03-17T10:00:01+00:00\",\"updated_at\":\"2026-03-17T10:00:02+00:00\"}";

        private const string TaskUlid = "01JQ6Z8M3K4N5P7R9S0T1V2W4Y";
        private const string UserUlid = "01JQ6Z8M3K4N5P7R9S0T1V2W3X";

        private static string Latin1(byte[] bytes) => Encoding.GetEncoding("ISO-8859-1").GetString(bytes);

        [Test]
        public async Task List_ReadsThePaginatedPhotosWithUserAndTaskUlids()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"data\":[" + PhotoJson + "],\"links\":{},"
                + "\"meta\":{\"current_page\":1,\"last_page\":3,\"per_page\":30,\"total\":75}}}");

            var response = await _service.ListAsync();

            Assert.IsTrue(response.IsSuccess);
            Assert.IsTrue(response.Data.HasMorePages);
            Assert.AreEqual(75, response.Data.Total);

            var photo = response.Data.Data[0];
            Assert.AreEqual("01P1", photo.Id);
            Assert.AreEqual("site_visit", photo.Name);
            Assert.AreEqual("roof", photo.Tags[0].Name);
            Assert.AreEqual("01G1", photo.Tags[0].Id);
            Assert.AreEqual("https://s3.test/a-preview.jpg", photo.Preview);
            Assert.AreEqual(48.1486, photo.GpsLat);
            Assert.AreEqual(17.1077, photo.GpsLng);
            Assert.AreEqual("2026-03-17T10:00:00.000000Z", photo.Timestamp);
            Assert.AreEqual(UserUlid, photo.User);
            Assert.AreEqual(TaskUlid, photo.Task);
            Assert.AreEqual("night", (string)photo.Meta["shift"]);
            Assert.AreEqual(3, (int)photo.Meta["floor"]);
        }

        [Test]
        public async Task Get_PhotoWithoutATask_HasANullTask()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"id\":\"01P3\",\"name\":\"n\",\"user\":\"" + UserUlid + "\",\"task\":null,"
                + "\"tags\":[],\"meta\":[]}}");

            var response = await _service.GetAsync("01P3");

            Assert.AreEqual(UserUlid, response.Data.User);
            Assert.IsNull(response.Data.Task);
        }

        [Test]
        public async Task Get_EmptyMetaEncodedAsArray_IsAnEmptyDictionary()
        {
            _api.Transport.Enqueue(200,
                "{\"success\":true,\"data\":{\"id\":\"01P2\",\"name\":null,\"tags\":[],\"url\":null,\"preview\":null,"
                + "\"description\":null,\"gps_lat\":null,\"gps_lng\":null,\"meta\":[]}}");

            var response = await _service.GetAsync("01P2");

            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Data.Meta);
            Assert.AreEqual(0, response.Data.Meta.Count);
            Assert.IsNull(response.Data.GpsLat);
            Assert.AreEqual(0, response.Data.Tags.Count);
        }

        [Test]
        public async Task List_SendsPageAndEveryFilter()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[],\"links\":{},\"meta\":{\"current_page\":2,\"last_page\":2,\"per_page\":30,\"total\":31}}}");

            await _service.ListAsync(2, new PhotoFilter
            {
                Name = "site",
                Task = TaskUlid,
                User = UserUlid,
                Description = "north side",
                GpsLat = 48.1486,
                GpsLng = 17.1077,
                Radius = 250.5,
                Timestamp = new DateTimeOffset(2026, 3, 17, 10, 0, 0, TimeSpan.Zero),
                TimestampFrom = new DateTimeOffset(2026, 3, 1, 1, 0, 0, TimeSpan.FromHours(1)),
                TimestampTo = new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero),
                Meta = new Dictionary<string, string> { { "shift", "night" }, { "floor", "3" } },
            });

            Assert.AreEqual(
                "photos?page=2&name=site&task=" + TaskUlid + "&user=" + UserUlid
                + "&description=north%20side&gps_lat=48.1486&gps_lng=17.1077&radius=250.5"
                + "&timestamp=2026-03-17T10%3A00%3A00Z&timestamp_from=2026-03-01T00%3A00%3A00Z&timestamp_to=2026-03-31T00%3A00%3A00Z"
                + "&meta%5Bfloor%5D=3&meta%5Bshift%5D=night",
                _api.Url(0));
        }

        [Test]
        public async Task List_WithoutFilter_SendsOnlyThePage()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[],\"links\":{},\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":30,\"total\":0}}}");

            await _service.ListAsync();

            Assert.AreEqual("photos?page=1", _api.Url(0));
        }

        [Test]
        public void List_EmptyMetaKey_Throws()
        {
            var filter = new PhotoFilter { Meta = new Dictionary<string, string> { { " ", "x" } } };

            Assert.Throws<ArgumentException>(() => _service.ListAsync(1, filter));
        }

        [Test]
        public async Task Get_EscapesTheIdentifierAndReportsNotFound()
        {
            _api.Transport.Enqueue(404, "{\"message\":\"No query results for model [Modules\\\\Media\\\\Models\\\\WorkMedia].\"}");

            var response = await _service.GetAsync("a/b");

            Assert.AreEqual("photos/a%2Fb", _api.Url(0));
            Assert.AreEqual(ApiErrorCodes.NotFound, response.Error.Code);
        }

        [Test]
        public void Get_BlankId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _service.GetAsync(""));
        }

        [Test]
        public async Task Upload_SendsTheFileAndTheMetadataAsAJsonField()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");
            var image = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

            var response = await _service.UploadAsync(image, "frame.jpg", "image/jpeg", new PhotoMetadata
            {
                Name = "site_visit",
                Description = "north side",
                Task = TaskUlid,
                Timestamp = new DateTimeOffset(2026, 3, 17, 11, 0, 0, TimeSpan.FromHours(1)),
                GpsLat = 48.1486,
                GpsLng = 17.1077,
                Tags = new List<string> { "roof", "north" },
                Extra = new Dictionary<string, object> { { "shift", "night" }, { "floor", 3 } },
            });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("01P1", response.Data.Id);

            var request = _api.Transport.Requests[0];
            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("photos", _api.Url(0));
            StringAssert.StartsWith("multipart/form-data; boundary=", request.ContentType);

            var text = Latin1(request.Body);
            StringAssert.Contains("name=\"file\"; filename=\"frame.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n" + Latin1(image) + "\r\n", text);

            const string marker = "name=\"metadata\"\r\n\r\n";
            var start = text.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            var json = (JObject)JsonParser.Parse(text.Substring(start, text.IndexOf("\r\n", start, StringComparison.Ordinal) - start));

            Assert.AreEqual("site_visit", (string)json["name"]);
            Assert.AreEqual("north side", (string)json["description"]);
            Assert.AreEqual("2026-03-17T10:00:00Z", (string)json["timestamp"]);
            Assert.AreEqual(48.1486, (double)json["gps_lat"]);
            Assert.AreEqual(17.1077, (double)json["gps_lng"]);
            Assert.AreEqual("roof", (string)json["tags"][0]);
            Assert.AreEqual("night", (string)json["shift"]);
            Assert.AreEqual(3, (int)json["floor"]);
            Assert.AreEqual(TaskUlid, (string)json["task_ulid"]);
        }

        [Test]
        public async Task Upload_TaskOfAnotherOrganization_ExposesTheFieldError()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"The selected metadata.task ulid is invalid.\",\"errors\":{\"metadata.task_ulid\":[\"The selected metadata.task ulid is invalid.\"]}}");

            var response = await _service.UploadAsync(new byte[] { 1 }, "a.png", "image/png", new PhotoMetadata { Task = TaskUlid });

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("metadata.task_ulid").Count);
        }

        [Test]
        public async Task Upload_WithoutMetadata_SendsOnlyTheFile()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UploadAsync(new byte[] { 1, 2, 3 }, "frame.png", "image/png");

            var text = Latin1(_api.Transport.Requests[0].Body);
            StringAssert.DoesNotContain("name=\"metadata\"", text);
        }

        [Test]
        public async Task Upload_UnsetMetadataFields_AreOmitted()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UploadAsync(new byte[] { 1 }, "a.png", "image/png", new PhotoMetadata { Name = "only" });

            var text = Latin1(_api.Transport.Requests[0].Body);
            StringAssert.Contains("name=\"metadata\"\r\n\r\n{\"name\":\"only\"}\r\n", text);
        }

        [Test]
        public async Task Upload_ValidationFailureFromTheBackend_ExposesFieldErrors()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"The file must be an image.\",\"errors\":{\"file\":[\"The file must be an image.\"]}}");

            var response = await _service.UploadAsync(new byte[] { 1 }, "a.png", "image/png");

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual("The file must be an image.", response.Error.GetFieldErrors("file")[0]);
        }

        [Test]
        public async Task Upload_InvalidMetadataJson_IsReportedAsTheBackendSendsIt()
        {
            _api.Transport.Enqueue(422, "{\"success\":false,\"message\":\"Invalid metadata JSON.\"}");

            var response = await _service.UploadAsync(new byte[] { 1 }, "a.png", "image/png");

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual("Invalid metadata JSON.", response.Error.Message);
        }

        [Test]
        public async Task Upload_EmptyImage_FailsWithoutARequest()
        {
            var response = await _service.UploadAsync(new byte[0], "a.png", "image/png");

            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("file").Count);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task Upload_NotAnImage_FailsWithoutARequest()
        {
            var response = await _service.UploadAsync(new byte[] { 1 }, "a.pdf", "application/pdf");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual("Only images can be uploaded.", response.Error.GetFieldErrors("file")[0]);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task Upload_LargerThanFiftyMegabytes_FailsWithoutARequest()
        {
            var response = await _service.UploadAsync(new byte[PhotoService.MaxPhotoBytes + 1], "a.jpg", "image/jpeg");

            Assert.IsFalse(response.IsSuccess);
            StringAssert.Contains("51200 KB", response.Error.GetFieldErrors("file")[0]);
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public void Upload_ExtraKeyThatShadowsANamedField_Throws()
        {
            var metadata = new PhotoMetadata { Extra = new Dictionary<string, object> { { "task_ulid", "01X" } } };

            Assert.Throws<ArgumentException>(() => _service.UploadAsync(new byte[] { 1 }, "a.png", "image/png", metadata));
            Assert.AreEqual(0, _api.Transport.Requests.Count);
        }

        [Test]
        public async Task Update_SendsOnlyTheChangedFieldsInsideMetadata()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            var response = await _service.UpdateAsync("01P1", new UpdatePhotoRequest { Name = "renamed", Tags = new List<string> { "a" } });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("PATCH", _api.Transport.Requests[0].Method);
            Assert.AreEqual("photos/01P1", _api.Url(0));
            Assert.AreEqual("{\"metadata\":{\"name\":\"renamed\",\"tags\":[\"a\"]}}", _api.Body(0));
        }

        [Test]
        public async Task Update_EmptyTagList_IsSentToRemoveAllTags()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UpdateAsync("01P1", new UpdatePhotoRequest { Tags = new List<string>() });

            Assert.AreEqual("{\"metadata\":{\"tags\":[]}}", _api.Body(0));
        }

        [Test]
        public async Task Update_AssignedNulls_ClearDescriptionAndCoordinates()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UpdateAsync("01P1", new UpdatePhotoRequest { Description = null, GpsLat = null, GpsLng = null });

            Assert.AreEqual("{\"metadata\":{\"description\":null,\"gps_lat\":null,\"gps_lng\":null}}", _api.Body(0));
        }

        [Test]
        public async Task Update_AssignedValues_SendDescriptionAndCoordinates()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UpdateAsync("01P1", new UpdatePhotoRequest { Description = "south side", GpsLat = 48.2, GpsLng = 17.2 });

            Assert.AreEqual("{\"metadata\":{\"description\":\"south side\",\"gps_lat\":48.2,\"gps_lng\":17.2}}", _api.Body(0));
        }

        [Test]
        public async Task Update_UnassignedFields_AreNotSent()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UpdateAsync("01P1", new UpdatePhotoRequest { Name = "only" });

            Assert.AreEqual("{\"metadata\":{\"name\":\"only\"}}", _api.Body(0));
        }

        [Test]
        public async Task Update_ExtraKeys_AreMergedAndANullValueRemovesTheKey()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UpdateAsync("01P1", new UpdatePhotoRequest
            {
                Extra = new Dictionary<string, object> { { "floor", 4 }, { "room", null } },
            });

            Assert.AreEqual("{\"metadata\":{\"floor\":4,\"room\":null}}", _api.Body(0));
        }

        [Test]
        public async Task Update_ByAColleague_IsForbidden()
        {
            _api.Transport.Enqueue(403, "{\"message\":\"This action is unauthorized.\"}");

            var response = await _service.UpdateAsync("01P1", new UpdatePhotoRequest { Name = "x" });

            Assert.AreEqual(403, response.StatusCode);
            Assert.AreEqual(ApiErrorCodes.Forbidden, response.Error.Code);
        }

        [Test]
        public async Task Delete_OfAPhotoTheCallerCannotSee_IsNotFound()
        {
            _api.Transport.Enqueue(404, "{\"message\":\"No query results for model [Modules\\\\Media\\\\Models\\\\WorkMedia].\"}");

            var response = await _service.DeleteAsync("01P1");

            Assert.AreEqual(ApiErrorCodes.NotFound, response.Error.Code);
        }

        [Test]
        public async Task List_NumericTaskFilter_IsRejectedByTheBackendWithFieldErrors()
        {
            _api.Transport.Enqueue(422, "{\"message\":\"The task field must be 26 characters.\",\"errors\":{\"task\":[\"The task field must be 26 characters.\"]}}");

            var response = await _service.ListAsync(1, new PhotoFilter { Task = "1" });

            Assert.AreEqual("photos?page=1&task=1", _api.Url(0));
            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual(1, response.Error.GetFieldErrors("task").Count);
        }

        [Test]
        public async Task Update_WithoutExtra_SendsNoMetaKeys()
        {
            _api.Transport.Enqueue(200, "{\"success\":true,\"data\":" + PhotoJson + "}");

            await _service.UpdateAsync("01P1", new UpdatePhotoRequest());

            Assert.AreEqual("{\"metadata\":{}}", _api.Body(0));
        }

        [Test]
        public void Update_NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateAsync("01P1", null));
        }

        [Test]
        public void Update_ExtraKeyThatShadowsANamedField_Throws()
        {
            var request = new UpdatePhotoRequest { Extra = new Dictionary<string, object> { { "gps_lat", 1.0 } } };

            Assert.Throws<ArgumentException>(() => _service.UpdateAsync("01P1", request));
        }

        [Test]
        public async Task Delete_SuccessEnvelopeWithoutData_IsSuccess()
        {
            _api.Transport.Enqueue(200, "{\"success\":true}");

            var response = await _service.DeleteAsync("01P1");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("DELETE", _api.Transport.Requests[0].Method);
            Assert.AreEqual("photos/01P1", _api.Url(0));
        }
    }
}
