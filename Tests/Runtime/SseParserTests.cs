using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class SseParserTests
    {
        private List<SseEvent> _events;
        private SseParser _parser;

        [SetUp]
        public void SetUp()
        {
            _events = new List<SseEvent>();
            _parser = new SseParser(e => _events.Add(e));
        }

        private void Feed(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            _parser.Feed(bytes, bytes.Length);
        }

        [Test]
        public void NamedEvent_IsDispatchedOnBlankLine()
        {
            Feed("event: chunk\ndata: {\"text\":\"Ahoj\",\"index\":0}\n\n");

            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("chunk", _events[0].Name);
            Assert.AreEqual("{\"text\":\"Ahoj\",\"index\":0}", _events[0].Data);
        }

        [Test]
        public void EventWithoutName_IsAMessage()
        {
            Feed("data: hi\n\n");

            Assert.AreEqual("message", _events[0].Name);
        }

        [Test]
        public void NothingIsDispatchedBeforeTheBlankLine()
        {
            Feed("event: chunk\ndata: x\n");

            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void SeveralEventsInOneRead_AreDispatchedInOrder()
        {
            Feed("event: chunk\ndata: 1\n\nevent: chunk\ndata: 2\n\nevent: done\ndata: 3\n\n");

            Assert.AreEqual(3, _events.Count);
            Assert.AreEqual("1", _events[0].Data);
            Assert.AreEqual("2", _events[1].Data);
            Assert.AreEqual("done", _events[2].Name);
        }

        [Test]
        public void CrLf_LineEndingsAreAccepted()
        {
            Feed("event: chunk\r\ndata: x\r\n\r\n");

            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("chunk", _events[0].Name);
            Assert.AreEqual("x", _events[0].Data);
        }

        [Test]
        public void MultipleDataLines_AreJoinedWithNewline()
        {
            Feed("data: one\ndata: two\n\n");

            Assert.AreEqual("one\ntwo", _events[0].Data);
        }

        [Test]
        public void Comments_AreIgnored()
        {
            Feed(": keep-alive\nevent: chunk\ndata: x\n\n");

            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("x", _events[0].Data);
        }

        [Test]
        public void LeadingByteOrderMark_IsIgnored()
        {
            var bytes = new List<byte> { 0xEF, 0xBB, 0xBF };
            bytes.AddRange(Encoding.UTF8.GetBytes("event: chunk\ndata: x\n\n"));

            _parser.Feed(bytes.ToArray(), bytes.Count);

            Assert.AreEqual("chunk", _events[0].Name);
        }

        [Test]
        public void OnlyOneSpaceAfterTheColon_IsStripped()
        {
            Feed("data:  two spaces\n\n");

            Assert.AreEqual(" two spaces", _events[0].Data);
        }

        [Test]
        public void FieldWithoutColon_HasEmptyValue()
        {
            Feed("data\n\n");

            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("", _events[0].Data);
        }

        [Test]
        public void BlankLinesWithoutData_DispatchNothing()
        {
            Feed("\n\n\nevent: chunk\n\n");

            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void EventNameDoesNotLeakIntoTheNextEvent()
        {
            Feed("event: source\ndata: a\n\ndata: b\n\n");

            Assert.AreEqual("source", _events[0].Name);
            Assert.AreEqual("message", _events[1].Name);
        }

        [Test]
        public void Complete_DispatchesFinalEventWithoutTrailingBlankLine()
        {
            Feed("event: done\ndata: {\"ok\":true}\n");
            Assert.AreEqual(0, _events.Count);

            _parser.Complete();

            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("done", _events[0].Name);
        }

        [Test]
        public void Complete_DropsAnUnterminatedLine()
        {
            Feed("event: done\ndata: {\"ok\"");

            _parser.Complete();

            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void EveryByteSplit_YieldsTheSameEvents_IncludingMultiByteCharacters()
        {
            const string stream = "event: chunk\ndata: {\"text\":\"Dobrý deň, čo robíš? 🙂\",\"index\":0}\n\n"
                + "event: done\ndata: {\"message_ulid\":\"01H\"}\n\n";
            var bytes = Encoding.UTF8.GetBytes(stream);

            for (var split = 1; split < bytes.Length; split++)
            {
                var events = new List<SseEvent>();
                var parser = new SseParser(events.Add);
                var first = new byte[split];
                var second = new byte[bytes.Length - split];
                System.Array.Copy(bytes, 0, first, 0, split);
                System.Array.Copy(bytes, split, second, 0, second.Length);

                parser.Feed(first, first.Length);
                parser.Feed(second, second.Length);

                Assert.AreEqual(2, events.Count, "split at " + split);
                Assert.AreEqual("{\"text\":\"Dobrý deň, čo robíš? 🙂\",\"index\":0}", events[0].Data, "split at " + split);
            }
        }

        [Test]
        public void SingleByteFeeding_YieldsTheSameEvents()
        {
            var bytes = Encoding.UTF8.GetBytes("event: chunk\ndata: ščř\n\n");

            foreach (var value in bytes)
                _parser.Feed(new[] { value }, 1);

            Assert.AreEqual("ščř", _events[0].Data);
        }

        [Test]
        public void Feed_HonoursTheCountNotTheBufferLength()
        {
            var bytes = Encoding.UTF8.GetBytes("data: a\n\ndata: garbage\n\n");

            _parser.Feed(bytes, "data: a\n\n".Length);

            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("a", _events[0].Data);
        }

        [Test]
        public void ReusedReadBuffer_DoesNotCorruptEarlierEvents()
        {
            var buffer = new byte[64];

            var first = Encoding.UTF8.GetBytes("data: first\n\n");
            System.Array.Copy(first, buffer, first.Length);
            _parser.Feed(buffer, first.Length);

            var second = Encoding.UTF8.GetBytes("data: other\n\n");
            System.Array.Copy(second, buffer, second.Length);
            _parser.Feed(buffer, second.Length);

            Assert.AreEqual("first", _events[0].Data);
            Assert.AreEqual("other", _events[1].Data);
        }

        [Test]
        public void OversizedLine_Throws()
        {
            var huge = new byte[1024 * 1024 + 1];
            for (var i = 0; i < huge.Length; i++)
                huge[i] = (byte)'a';

            Assert.Throws<InvalidDataException>(() => _parser.Feed(huge, huge.Length));
        }
    }
}
