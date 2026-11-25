using System;
using System.IO;
using System.Text;

namespace Technyx.Sdk.Http
{
    public readonly struct SseEvent
    {
        public SseEvent(string name, string data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; }
        public string Data { get; }
    }

    // Splits on the LF byte and decodes only whole lines, so a multi-byte UTF-8 character that is
    // split across two network reads is never decoded half. Lines end with LF or CRLF.
    public sealed class SseParser
    {
        private const int MaxLineBytes = 1024 * 1024;
        private const char ByteOrderMark = (char)0xFEFF;

        private readonly Action<SseEvent> _onEvent;
        private readonly MemoryStream _line = new MemoryStream();
        private readonly StringBuilder _data = new StringBuilder();

        private string _eventName;
        private bool _hasData;
        private bool _atStart = true;

        public SseParser(Action<SseEvent> onEvent)
        {
            _onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));
        }

        public void Feed(byte[] buffer, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var value = buffer[i];
                if (value == (byte)'\n')
                {
                    ProcessLine();
                    continue;
                }

                if (_line.Length >= MaxLineBytes)
                    throw new InvalidDataException("An event stream line exceeded " + MaxLineBytes + " bytes.");

                _line.WriteByte(value);
            }
        }

        // Dispatches a last event whose closing blank line never arrived. A line that is still
        // incomplete is dropped.
        public void Complete()
        {
            Dispatch();
        }

        private void ProcessLine()
        {
            var bytes = _line.ToArray();
            _line.SetLength(0);

            var length = bytes.Length;
            if (length > 0 && bytes[length - 1] == (byte)'\r')
                length--;

            var line = Encoding.UTF8.GetString(bytes, 0, length);

            if (_atStart)
            {
                _atStart = false;
                if (line.Length > 0 && line[0] == ByteOrderMark)
                    line = line.Substring(1);
            }

            if (line.Length == 0)
            {
                Dispatch();
                return;
            }

            if (line[0] == ':')
                return;

            var colon = line.IndexOf(':');
            var field = colon < 0 ? line : line.Substring(0, colon);
            var fieldValue = colon < 0 ? "" : line.Substring(colon + 1);
            if (fieldValue.Length > 0 && fieldValue[0] == ' ')
                fieldValue = fieldValue.Substring(1);

            if (field == "event")
            {
                _eventName = fieldValue;
            }
            else if (field == "data")
            {
                if (_hasData)
                    _data.Append('\n');

                _data.Append(fieldValue);
                _hasData = true;
            }
        }

        private void Dispatch()
        {
            if (!_hasData)
            {
                _eventName = null;
                return;
            }

            var evt = new SseEvent(string.IsNullOrEmpty(_eventName) ? "message" : _eventName, _data.ToString());
            _data.Clear();
            _hasData = false;
            _eventName = null;
            _onEvent(evt);
        }
    }
}
