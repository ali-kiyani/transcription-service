using Transcription.Application.Ports;
using Transcription.Application.Services;
using Transcription.Infrastructure.Audio;
using Transcription.Infrastructure.Speech;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<AzureSpeechOptions>(builder.Configuration.GetSection("AzureSpeech"));
builder.Services.AddHttpClient<ISpeechToTextService, AzureSpeechToTextAdapter>();
builder.Services.AddSingleton<IAudioProcessor, FfmpegAudioProcessor>();
builder.Services.AddScoped<ITransriptionService, TranscriptionService>();

var app = builder.Build();

app.MapControllers();

app.Run();
