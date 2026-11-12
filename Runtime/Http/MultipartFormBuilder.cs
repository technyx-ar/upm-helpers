using System;
using System.Collections.Generic;
using System.Text;

namespace Technyx.Sdk.Http
{
    public sealed class MultipartFormBuilder
    {
        private readonly string _boundary = "----TechnyxSdk" + Guid.NewGuid().ToString("N");
        private readonly List<byte[]> _parts = new List<byte[]>();

        public string ContentType => "multipart/form-data; boundary=" + _boundary;

        public MultipartFormBuilder AddField(string name, string value)
        {
            var header = $"--{_boundary}\r\nContent-Disposition: form-data; name=\"{Escape(name)}\"\r\n\r\n";
            _parts.Add(Encoding.UTF8.GetBytes(header));
            _parts.Add(Encoding.UTF8.GetBytes(value ?? ""));
            _parts.Add(Encoding.ASCII.GetBytes("\r\n"));
            return this;
        }

        public MultipartFormBuilder AddFile(string name, string fileName, string contentType, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var header = $"--{_boundary}\r\nContent-Disposition: form-data; name=\"{Escape(name)}\"; filename=\"{Escape(fileName)}\"\r\n"
                + $"Content-Type: {contentType}\r\n\r\n";
            _parts.Add(Encoding.UTF8.GetBytes(header));
            _parts.Add(data);
            _parts.Add(Encoding.ASCII.GetBytes("\r\n"));
            return this;
        }

        public byte[] Build()
        {
            var closing = Encoding.ASCII.GetBytes($"--{_boundary}--\r\n");

            var length = closing.Length;
            foreach (var part in _parts)
                length += part.Length;

            var result = new byte[length];
            var offset = 0;
            foreach (var part in _parts)
            {
                Buffer.BlockCopy(part, 0, result, offset, part.Length);
                offset += part.Length;
            }

            Buffer.BlockCopy(closing, 0, result, offset, closing.Length);
            return result;
        }

        private static string Escape(string value)
        {
            return (value ?? "").Replace("\"", "%22").Replace("\r", "%0D").Replace("\n", "%0A");
        }
    }
}
