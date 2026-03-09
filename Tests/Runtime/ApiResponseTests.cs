using NUnit.Framework;
using Technyx.One.Http;

namespace Technyx.One.Tests
{
    [TestFixture]
    public class ApiResponseTests
    {
        [Test]
        public void Success_SetsProperties()
        {
            var response = ApiResponse<string>.Success("hello", 200, "{\"data\":\"hello\"}");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("hello", response.Data);
            Assert.IsNull(response.Error);
        }

        [Test]
        public void Fail_SetsProperties()
        {
            var error = new ApiError { Code = "NOT_FOUND", Message = "Not found." };
            var response = ApiResponse<string>.Fail(error, 404, "{\"error\":{}}");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(404, response.StatusCode);
            Assert.IsNull(response.Data);
            Assert.AreEqual("NOT_FOUND", response.Error.Code);
        }

        [Test]
        public void Fail_NullError_GetsDefault()
        {
            var response = ApiResponse<string>.Fail(null, 500, "");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual("UNKNOWN", response.Error.Code);
        }
    }
}
