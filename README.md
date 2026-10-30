# Transcription Service

A small ASP.NET Core API that takes an audio file, sends it to Azure AI Speech, and gives you back the transcript broken into segments with start and end times.

You can throw pretty much any common format at it (MP3, WAV, M4A, OGG...), and long recordings work too.

## How it works

When a file comes in, the service:

1. Converts it to 16 kHz mono WAV with ffmpeg, so every format is handled the same way.
2. Splits it into 10-minute chunks, so long recordings stay within Azure's limits.
3. Sends each chunk to Azure's fast transcription API. The language is detected automatically.
4. Shifts each chunk's timestamps to where that chunk sits in the original file, then returns everything as one list.

Temporary files are cleaned up once the request is done.

## Project structure

The solution follows a layered setup, with ports and adapters for the external services:

```
src/
  Transcription.Api             Controller and Program.cs (where everything is registered)
  Transcription.Application     TranscriptionService and the interfaces it depends on (Ports)
  Transcription.Domain          Segment model and InvalidAudioException
  Transcription.Infrastructure  The Azure Speech and ffmpeg implementations (Adapters)
```

The core logic only talks to two interfaces, `ISpeechToTextService` and `IAudioProcessor`. Azure and ffmpeg plug in behind them, so swapping Azure for something like Whisper means writing one new class and changing one line in `Program.cs`.

## Design decisions

**ffmpeg for audio.** It reads virtually every audio and video format and works the same on Windows, Linux and Docker. Converting everything to 16 kHz mono WAV first means the rest of the code only ever deals with one format, and that's what speech models work best with.

**10-minute chunks.** Splitting keeps every request well within Azure's size and length limits, and keeps a single request from taking too long. Each chunk's timestamps start at 0, so they're shifted by the chunk's position (chunk 2 starts at 1200 s) to match the original recording.

**Ports and adapters.** The core logic depends only on `ISpeechToTextService` and `IAudioProcessor`. Azure and ffmpeg are adapters registered in `Program.cs`. Switching to another provider, such as Whisper or AWS Transcribe, means adding one class and changing one line. It also makes `TranscriptionService` easy to test with fakes.

**Synchronous for now.** The request waits until the transcript is ready. That keeps things simple, but it isn't ideal for very long files. See below for how I'd change that.

## Going to production

For real traffic, I'd move to a job-based design:

- **Uploads:** The API returns a short-lived, write-only SAS URL, and the client uploads straight to Azure Blob Storage. Event Grid signals when an upload is finished.
- **Processing:** Each upload becomes a job on an Azure Service Bus queue. Background workers pick jobs up, so many uploads can be handled at once without overloading the API or hitting Azure Speech rate limits.
- **Storage:** Audio and transcripts (as JSON) go in Blob Storage. Job details (status, attempts, file paths) go in a small database table.
- **Failures:** Temporary errors are retried with increasing delays. If a worker crashes, Service Bus delivers the job again. Jobs that keep failing go to a dead-letter queue and are marked Failed.
- **API:** `POST` creates a job and returns the upload URL. `GET` returns the status and, once done, the transcript. Clients can also pass a callback URL to be notified when it's ready.

## Getting started

You'll need:

- .NET 8 SDK
- ffmpeg installed and on your PATH (run `ffmpeg -version` to check)
- An Azure Speech resource in a region that supports fast transcription, such as East US

Add your endpoint and key to `src/Transcription.Api/appsettings.json`:

```json
"AzureSpeech": {
  "Endpoint": "https://eastus.api.cognitive.microsoft.com",
  "Key": "your-key-here"
}
```

Then run it:

```bash
cd src/Transcription.Api
dotnet run
```

## Using the API

Send a file as multipart form data:

```bash
curl -F file=@meeting.mp3 http://localhost:50289/api/transcriptions
```

You'll get back something like:

```json
[
  {
    "start": 0.0,
    "end": 3.5,
    "text": "Good morning everyone, thanks for joining."
  },
  {
    "start": 4.0,
    "end": 7.5,
    "text": "Let's start with a quick update on the release."
  }
]
```

Times are in seconds from the start of the recording. If the file isn't valid audio, you'll get a `400` with a short message explaining why.

## Notes

- Keep your real Azure key out of source control. You can also set it with an environment variable (`AzureSpeech__Key`), which overrides `appsettings.json`.
- Chunks are transcribed one after another. Sending them in parallel would speed up long files.
