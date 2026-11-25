using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Voice
{
    public enum RealtimeEventKind
    {
        Other,
        ToolCallStarted,
        FunctionCall,
    }

    // One OpenAI Realtime data-channel event, reduced to what the app needs for tool calls.
    public sealed class RealtimeEvent
    {
        public RealtimeEventKind Kind;
        public string Type;
        public string ToolName;
        public string CallId;
        public string Arguments;

        public VoiceToolCallRequest ToToolCallRequest(string sessionToken)
        {
            if (Kind != RealtimeEventKind.FunctionCall)
                throw new InvalidOperationException($"A {Kind} event has no complete function call.");
            if (string.IsNullOrEmpty(sessionToken))
                throw new ArgumentException("A session token is required.", nameof(sessionToken));

            return new VoiceToolCallRequest
            {
                SessionToken = sessionToken,
                Name = ToolName,
                Arguments = Arguments,
                CallId = CallId,
            };
        }
    }

    public static class RealtimeEvents
    {
        // ToolCallStarted arrives about 0.9 s after the user stops talking, well before the search result:
        // the moment to play a local "looking it up" cue.
        public static RealtimeEvent Parse(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("The data-channel message is empty.", nameof(message));

            JObject json;
            using (var reader = new JsonTextReader(new System.IO.StringReader(message)) { DateParseHandling = DateParseHandling.None })
                json = JObject.Load(reader);

            var type = (string)json["type"];
            if (string.IsNullOrEmpty(type))
                throw new JsonException("The data-channel message has no type.");

            var result = new RealtimeEvent { Kind = RealtimeEventKind.Other, Type = type };

            if (type == "response.output_item.added" && json["item"] is JObject item && (string)item["type"] == "function_call")
            {
                result.Kind = RealtimeEventKind.ToolCallStarted;
                result.ToolName = (string)item["name"];
                result.CallId = (string)item["call_id"];
            }
            else if (type == "response.function_call_arguments.done")
            {
                result.Kind = RealtimeEventKind.FunctionCall;
                result.ToolName = Required(json, "name", type);
                result.CallId = Required(json, "call_id", type);
                result.Arguments = (string)json["arguments"] ?? "{}";
            }

            return result;
        }

        private static string Required(JObject json, string field, string type)
        {
            var value = (string)json[field];
            if (string.IsNullOrEmpty(value))
                throw new JsonException($"{type} has no {field}.");
            return value;
        }
    }
}
