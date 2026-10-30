using Transcription.Domain;

namespace Transcription.Application.Ports;

public interface ISpeechToTextService
{
    Task<List<Segment>> TranscribeAsync(string wavPath);
}
