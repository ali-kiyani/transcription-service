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
  Transcription.Application     TranscriptionService and the interfaces it depends on
  Transcription.Domain          Segment model and InvalidAudioException
  Transcription.Infrastructure  The Azure Speech and ffmpeg implementations
```

The core logic only talks to two interfaces, `ISpeechToTextService` and `IAudioProcessor`. Azure and ffmpeg plug in behind them, so swapping Azure for something like Whisper means writing one new class and changing one line in `Program.cs`.

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
