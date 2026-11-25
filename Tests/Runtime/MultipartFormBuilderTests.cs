using System;
using System.Text;
using NUnit.Framework;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class MultipartFormBuilderTests
    {
        private static int IndexOf(byte[] haystack, byte[] needle, int from = 0)
        {
            for (var i = from; i <= haystack.Length - needle.Length; i++)
            {
                var match = true;
                for (var j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return i;
            }

            return -1;
        }

        [Test]
        public void ContentType_CarriesTheBoundaryUsedInTheBody()
        {
            var form = new MultipartFormBuilder().AddField("a", "b");

            var boundary = form.ContentType.Substring(form.ContentType.IndexOf("boundary=", StringComparison.Ordinal) + 9);
            var body = Encoding.UTF8.GetString(form.Build());

            StringAssert.StartsWith("multipart/form-data; boundary=", form.ContentType);
            StringAssert.StartsWith("--" + boundary + "\r\n", body);
            StringAssert.EndsWith("--" + boundary + "--\r\n", body);
        }

        [Test]
        public void Field_IsWrittenAsFormData()
        {
            var body = Encoding.UTF8.GetString(new MultipartFormBuilder().AddField("metadata", "{\"name\":\"Č\"}").Build());

            StringAssert.Contains("Content-Disposition: form-data; name=\"metadata\"\r\n\r\n{\"name\":\"Č\"}\r\n", body);
        }

        [Test]
        public void File_BinaryContentSurvivesUnchanged()
        {
            var data = new byte[256];
            for (var i = 0; i < data.Length; i++)
                data[i] = (byte)i;

            var bytes = new MultipartFormBuilder().AddFile("file", "shot.jpg", "image/jpeg", data).Build();

            var header = Encoding.UTF8.GetBytes("filename=\"shot.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n");
            var start = IndexOf(bytes, header) + header.Length;
            Assert.Greater(start, header.Length);

            for (var i = 0; i < data.Length; i++)
                Assert.AreEqual(data[i], bytes[start + i], "byte " + i);

            Assert.AreEqual((byte)'\r', bytes[start + data.Length]);
            Assert.AreEqual((byte)'\n', bytes[start + data.Length + 1]);
        }

        [Test]
        public void FileName_QuotesAndLineBreaksAreEscaped()
        {
            var body = Encoding.UTF8.GetString(
                new MultipartFormBuilder().AddFile("file", "a\"b\r\nc.png", "image/png", new byte[] { 1 }).Build());

            StringAssert.Contains("filename=\"a%22b%0D%0Ac.png\"", body);
        }

        [Test]
        public void EachBuilder_UsesItsOwnBoundary()
        {
            Assert.AreNotEqual(new MultipartFormBuilder().ContentType, new MultipartFormBuilder().ContentType);
        }

        [Test]
        public void Build_CanBeCalledRepeatedly_WithIdenticalBytes()
        {
            var form = new MultipartFormBuilder().AddField("a", "1").AddFile("f", "x.bin", "application/octet-stream", new byte[] { 9, 8 });

            CollectionAssert.AreEqual(form.Build(), form.Build());
        }

        [Test]
        public void AddFile_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MultipartFormBuilder().AddFile("f", "x", "image/png", null));
        }
    }
}
