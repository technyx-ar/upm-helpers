# Voice

`TechnyxSdk.Voice` (`VoiceService`): voice sessions, spoken or typed commands and the conversation history. Every call returns `ApiResponse<T>` (see [HTTP Client](05-http-client.md)) and takes an optional `CancellationToken`.

The SDK is the REST client only. It does not capture microphone audio, does not create a WebRTC peer connection and does not play audio; the app brings its own WebRTC or LiveKit client and uses the values returned here.

> **Availability:** the voice module is deployed on `api.prod.technyx.ar` with the OpenAI key configured, and the `oai_rt` session creation was verified against OpenAI from the production backend. The SDK was only tested against scripted responses, never against the live endpoint or with a real headset.

## Transports

| Constant | Value | How the audio flows |
|----------|-------|---------------------|
| `VoiceTransports.OpenAiRealtime` | `oai_rt` | The app negotiates WebRTC with OpenAI (`gpt-realtime`). |
| `VoiceTransports.LiveKit` | `webrtc` | The app joins a LiveKit room with the participant token. |
| `VoiceTransports.WebSocket` | `ws` | Accepted by the backend, no connection data is returned. |

Languages: `sk`, `en`, `cs` (`LanguageCodes`).

## Create and end a session

```csharp
var created = await TechnyxSdk.Voice.CreateSessionAsync(new CreateVoiceSessionRequest
{
    Language = LanguageCodes.Slovak,
    Transport = VoiceTransports.OpenAiRealtime,
});

if (!created.IsSuccess)
    return; // created.Error.Message, created.StatusCode (502 when OpenAI or LiveKit fails, 503 when it is not configured)

var session = created.Data;
```

`EphemeralKey` and `SessionToken` (`oai_rt`), or `RoomName`, `Token` and `LiveKitUrl` (`webrtc`), are returned only by this call. `GetSessionAsync(id)` returns the session without them. The session token is valid for 30 minutes.

```csharp
await TechnyxSdk.Voice.EndSessionAsync(session.Id);   // also closes the LiveKit room
```

## OpenAI Realtime (`oai_rt`)

Two ways to connect, both with the data from `CreateSessionAsync`:

1. **Through the backend.** Create the WebRTC offer in your client, send it and apply the returned answer:

```csharp
var answer = await TechnyxSdk.Voice.SendOfferAsync(new VoiceSdpOfferRequest
{
    SessionToken = session.SessionToken,
    Sdp = offerSdp,
});

// answer.Data.Sdp is the remote description, answer.Data.Type is "answer"
```

   The backend creates a fresh ephemeral key for every offer, so the offer does not depend on the key returned at session creation.

2. **Directly to OpenAI.** Post the offer as `application/sdp` to `https://api.openai.com/v1/realtime/calls` with `Authorization: Bearer <EphemeralKey>`. The key is short-lived; the SDK does not do this call.

`SendIceCandidateAsync` validates the session token and returns success. OpenAI handles ICE itself, so the candidate is not forwarded anywhere.

Calling `SendOfferAsync` with the token of a LiveKit session fails with `422`.

### Tools

The session offers two functions to the model: `search_manuals` (equipment manuals in the knowledge base) and `web_search`. Pass every message from the `oai-events` data channel to `RealtimeEvents.Parse`:

```csharp
var e = RealtimeEvents.Parse(message);

switch (e.Kind)
{
    case RealtimeEventKind.ToolCallStarted:
        cueSource.Play();   // e.ToolName is known, the arguments are not yet
        break;

    case RealtimeEventKind.FunctionCall:
        var result = await TechnyxSdk.Voice.ExecuteToolAsync(e.ToToolCallRequest(session.SessionToken));
        // send result.Data.Output to OpenAI as function_call_output for result.Data.CallId, then request a response
        // result.Data.Sources lists manual files and pages or web URLs
        break;
}
```

`ToolCallStarted` is `response.output_item.added` with an item of type `function_call`. It arrives about 0.9 s after the user stops talking (1.0 s at worst in our measurements), while the spoken answer to a manual question starts only after 2–3 s. Play a short local sound or show an indicator at that moment so the user hears a reaction within a second. The model does not speak during the search.

`FunctionCall` is `response.function_call_arguments.done` and carries the complete arguments. Every other event has `Kind == Other` and its `Type` set. A message without `type`, or a finished call without `name` or `call_id`, throws a `JsonException`.

`search_manuals` takes `{"queries": [...]}`, one to three phrasings of the same question; the backend searches all of them and merges the results.

A failed search returns an error response (`502`), unknown tools and bad arguments `422`, someone else's session `403`. Do not send an empty output in those cases. The SDK does not run the data channel itself.

## LiveKit (`webrtc`)

Connect your LiveKit client to `session.LiveKitUrl` with `session.Token`. The room allows two participants and closes five minutes after it empties.

## Commands

Text:

```csharp
var result = await TechnyxSdk.Voice.SendCommandAsync(new VoiceCommandRequest
{
    Text = "Show my open tasks",                         // required, at most 2000 characters
    Language = LanguageCodes.English,                    // required
    Context = new AiChatContext { TaskId = taskUlid, Location = "Hangar 2" },   // optional
    Speak = false,                                        // optional, default true
});

Debug.Log(result.Data.Text);   // answer; AudioUrl is set when speech was generated
```

The backend answers from the manuals (`search_manuals`) and the web (`web_search`) when the question needs it. `result.Data.Sources` lists what was used: `{type:"manual", source_file, pages, ...}` or `{type:"web", url, title}`. A failing search fails the whole command with `502`.

Audio (mp3, wav, ogg or webm, at most 25 MB; checked before the request):

```csharp
var result = await TechnyxSdk.Voice.SendAudioAsync(bytes, "command.wav", "audio/wav", LanguageCodes.Slovak);
var detected = await TechnyxSdk.Voice.SendAudioAsync(bytes, "command.wav", "audio/wav", speak: false);
```

Without a language the backend detects it from the speech and answers in it. Speech it cannot place as Slovak, English or Czech fails with `422` on `language`.

`speak: false` (or `Speak = false` on a text command) skips speech generation: `AudioUrl` is `null` and the answer arrives sooner by the speech generation time (about 3 s for a short answer with the backend's default `tts-1`). Use it when the app only shows the text or speaks it on the device.

The recording is transcribed first. When it holds no recognizable speech the call succeeds with an empty `Text` and `ConversationId`. The backend allows 30 text and 12 audio commands per minute (`429`, handled by the retry policy).

## History

```csharp
var page = await TechnyxSdk.Voice.ListHistoryAsync(page: 1, sessionId: session.Id, perPage: 20);   // perPage 1 to 50
foreach (var item in page.Data.Data)
    Debug.Log($"{item.Role}: {item.Content}");
```

Without `sessionId` the history of the signed-in user is returned, newest first.
