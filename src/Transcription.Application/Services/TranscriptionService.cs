using Transcription.Application.Ports;
using Transcription.Domain;

namespace Transcription.Application.Services;

public class TranscriptionService(IAudioProcessor audioProcessor, ISpeechToTextService speechToText) : ITransriptionService
{
	private const int ChunkSeconds = 600;

	public async Task<List<Segment>> TranscribeAsync(string inputPath)
	{
		var wavPath = await audioProcessor.ConvertToWavAsync(inputPath);
		var chunks = await audioProcessor.SplitAsync(wavPath, ChunkSeconds);

		try
		{
			var segments = new List<Segment>();
			for (int i = 0; i < chunks.Count; i++)
			{
				var chunkSegments = await speechToText.TranscribeAsync(chunks[i]);

				// Chunk timestamps start at 0, so move them to the chunk's position in the full file
				segments.AddRange(chunkSegments.Select(s => s.ShiftBy(i * ChunkSeconds)));
			}
			return segments;
		}
		finally
		{
			File.Delete(wavPath);
			chunks.ForEach(File.Delete);
		}
	}
}
