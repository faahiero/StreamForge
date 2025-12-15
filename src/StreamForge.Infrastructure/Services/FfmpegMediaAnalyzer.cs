using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Services;

public class FfmpegMediaAnalyzer : IMediaAnalyzer
{
    private readonly ILogger<FfmpegMediaAnalyzer> _logger;

    public FfmpegMediaAnalyzer(ILogger<FfmpegMediaAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<MediaMetadata> AnalyzeAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("❌ Arquivo de mídia não encontrado para análise: {FilePath}", filePath);
            throw new FileNotFoundException("Media file not found", filePath);
        }

        var fileInfo = new FileInfo(filePath);
        _logger.LogInformation("🔍 Analisando arquivo: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);

        // ffprobe -v quiet -print_format json -show_format -show_streams input.mp4
        var startInfo = new ProcessStartInfo
        {
            FileName = "/usr/bin/ffprobe", // Caminho absoluto para Linux/Docker
            Arguments = $"-v quiet -print_format json -show_format \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        
        process.OutputDataReceived += (sender, args) => { if (args.Data != null) outputBuilder.AppendLine(args.Data); };
        process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errorBuilder.AppendLine(args.Data); };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = errorBuilder.ToString();
            _logger.LogError("FFprobe falhou: {Error}", error);
            throw new InvalidOperationException($"FFprobe failed with code {process.ExitCode}");
        }

        var json = outputBuilder.ToString();
        
        // Parse JSON (Simplificado)
        using var doc = JsonDocument.Parse(json);
        var format = doc.RootElement.GetProperty("format");
        
        var durationSec = double.Parse(format.GetProperty("duration").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var formatName = format.GetProperty("format_name").GetString()!;
        var bitRate = long.Parse(format.GetProperty("bit_rate").GetString()!);

        return new MediaMetadata(TimeSpan.FromSeconds(durationSec), formatName, bitRate);
    }
}
