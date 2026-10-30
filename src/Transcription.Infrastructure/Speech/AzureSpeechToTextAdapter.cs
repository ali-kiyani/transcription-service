using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Transcription.Application.Ports;
using Transcription.Domain;

namespace Transcription.Infrastructure.Speech;

public class AzureSpeechToTextAdapter(HttpClient http, IOptions<AzureSpeechOptions> options) : ISpeechToTextService
{
    public async Task<List<Segment>> TranscribeAsync(string wavPath)
    {
        var url = $"{options.Value.Endpoint.TrimEnd('/')}/speechtotext/transcriptions:transcribe?api-version=2024-11-15";

        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(File.OpenRead(wavPath)), "audio", Path.GetFileName(wavPath));
        form.Add(new StringContent("{}"), "definition");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        request.Headers.Add("Ocp-Apim-Subscription-Key", options.Value.Key);

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureResponse>();

        return (result?.Phrases ?? [])
            .Select(p => new Segment(
                p.OffsetMilliseconds / 1000.0,
                (p.OffsetMilliseconds + p.DurationMilliseconds) / 1000.0,
                p.Text.Trim()))
            .ToList();
    }

    private record AzureResponse(List<AzurePhrase>? Phrases);
    private record AzurePhrase(long OffsetMilliseconds, long DurationMilliseconds, string Text);
}
