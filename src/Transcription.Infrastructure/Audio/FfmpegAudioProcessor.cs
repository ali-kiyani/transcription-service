using System.Diagnostics;
using Transcription.Application.Ports;
using Transcription.Domain;

namespace Transcription.Infrastructure.Audio;

public class FfmpegAudioProcessor : IAudioProcessor
{
	public async Task<string> ConvertToWavAsync(string inputPath)
	{
		var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

		await RunFfmpegAsync("-y", "-i", inputPath, "-ac", "1", "-ar", "16000", outputPath); //16 KHz WAV
		return outputPath;
	}

	public async Task<List<string>> SplitAsync(string wavPath, int chunkSeconds)
	{
		var prefix = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		await RunFfmpegAsync("-i", wavPath, "-f", "segment", "-segment_time", chunkSeconds.ToString(), "-c", "copy", $"{prefix}_%03d.wav");

		return Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileName(prefix)}_*.wav").Order().ToList();
	}

	private static async Task RunFfmpegAsync(params string[] args)
	{
		var startInfo = new ProcessStartInfo("ffmpeg") { RedirectStandardError = true };
		foreach (var arg in args) startInfo.ArgumentList.Add(arg);

		using var process = Process.Start(startInfo)!;
		await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
			throw new InvalidAudioException("Unsupported or corrupt audio file.");
	}
}
