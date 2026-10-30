using Microsoft.AspNetCore.Mvc;
using Transcription.Application.Services;
using Transcription.Domain;

namespace Transcription.Api.Controllers;

[ApiController]
[Route("api/transcriptions")]
public class TranscriptionsController(ITransriptionService transcriptionService) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> Transcribe(IFormFile file)
	{
		if (file.Length == 0)
			return BadRequest("Please upload an audio file.");

		var inputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));
		using (var stream = System.IO.File.Create(inputPath))
			await file.CopyToAsync(stream);

		try
		{
			var segments = await transcriptionService.TranscribeAsync(inputPath);
			return Ok(segments);
		}
		catch (InvalidAudioException ex)
		{
			return BadRequest(ex.Message);
		}
		finally
		{
			System.IO.File.Delete(inputPath);
		}
	}
}
