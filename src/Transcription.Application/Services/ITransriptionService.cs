using Transcription.Domain;

namespace Transcription.Application.Services
{
	public interface ITransriptionService
	{
		Task<List<Segment>> TranscribeAsync(string inputPath);
	}
}
