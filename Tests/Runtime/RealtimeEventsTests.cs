using System;
using Newtonsoft.Json;
using NUnit.Framework;
using Technyx.Sdk.Voice;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class RealtimeEventsTests
    {
        [Test]
        public void FunctionCallItemAdded_IsToolCallStarted()
        {
            var e = RealtimeEvents.Parse("{\"type\":\"response.output_item.added\",\"response_id\":\"resp_1\",\"output_index\":0,"
                + "\"item\":{\"id\":\"item_1\",\"type\":\"function_call\",\"status\":\"in_progress\",\"name\":\"search_manuals\",\"call_id\":\"call_1\",\"arguments\":\"\"}}");

            Assert.AreEqual(RealtimeEventKind.ToolCallStarted, e.Kind);
            Assert.AreEqual("search_manuals", e.ToolName);
            Assert.AreEqual("call_1", e.CallId);
        }

        [Test]
        public void MessageItemAdded_IsOther()
        {
            var e = RealtimeEvents.Parse("{\"type\":\"response.output_item.added\",\"item\":{\"id\":\"item_2\",\"type\":\"message\",\"role\":\"assistant\"}}");

            Assert.AreEqual(RealtimeEventKind.Other, e.Kind);
            Assert.AreEqual("response.output_item.added", e.Type);
            Assert.IsNull(e.ToolName);
        }

        [Test]
        public void ArgumentsDone_IsFunctionCallReadyForTheBackend()
        {
            var e = RealtimeEvents.Parse("{\"type\":\"response.function_call_arguments.done\",\"item_id\":\"item_1\",\"output_index\":0,"
                + "\"call_id\":\"call_1\",\"name\":\"search_manuals\",\"arguments\":\"{\\\"queries\\\":[\\\"propeller torque\\\"]}\"}");

            Assert.AreEqual(RealtimeEventKind.FunctionCall, e.Kind);

            var request = e.ToToolCallRequest("st_1");

            Assert.AreEqual("st_1", request.SessionToken);
            Assert.AreEqual("search_manuals", request.Name);
            Assert.AreEqual("call_1", request.CallId);
            Assert.AreEqual("{\"queries\":[\"propeller torque\"]}", request.Arguments);
        }

        [Test]
        public void ArgumentsDone_WithoutCallId_Throws()
        {
            Assert.Throws<JsonException>(() =>
                RealtimeEvents.Parse("{\"type\":\"response.function_call_arguments.done\",\"name\":\"search_manuals\",\"arguments\":\"{}\"}"));
        }

        [Test]
        public void OtherEvents_KeepTheirType()
        {
            var e = RealtimeEvents.Parse("{\"type\":\"input_audio_buffer.speech_stopped\",\"audio_end_ms\":1200}");

            Assert.AreEqual(RealtimeEventKind.Other, e.Kind);
            Assert.AreEqual("input_audio_buffer.speech_stopped", e.Type);
        }

        [Test]
        public void MalformedMessages_Throw()
        {
            Assert.Throws<ArgumentException>(() => RealtimeEvents.Parse(""));
            Assert.Throws<JsonException>(() => RealtimeEvents.Parse("{\"event_id\":\"e1\"}"));
            Assert.Throws<JsonReaderException>(() => RealtimeEvents.Parse("{not json"));
        }

        [Test]
        public void ToolCallStarted_CannotBeExecuted()
        {
            var e = RealtimeEvents.Parse("{\"type\":\"response.output_item.added\",\"item\":{\"type\":\"function_call\",\"name\":\"web_search\",\"call_id\":\"call_2\"}}");

            Assert.Throws<InvalidOperationException>(() => e.ToToolCallRequest("st_1"));
        }
    }
}
