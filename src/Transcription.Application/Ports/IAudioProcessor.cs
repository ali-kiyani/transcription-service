namespace Transcription.Application.Ports;

public interface IAudioProcessor
{
    Task<string> ConvertToWavAsync(string inputPath);
    Task<List<string>> SplitAsync(string wavPath, int chunkSeconds);
}
