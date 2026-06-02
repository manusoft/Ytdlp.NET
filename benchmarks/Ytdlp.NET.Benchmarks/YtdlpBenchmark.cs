using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ManuHub.Ytdlp.NET.Core;

namespace ManuHub.Ytdlp.NET.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class YtdlpBenchmark
{
    private readonly ProcessFactory _factory;
    private readonly ProcessRunner _newRunner;
    private readonly ProbeRunner _oldProbeRunner;   // old class

    private string toolsPath = Path.Combine(AppContext.BaseDirectory, "tools");

    private const string TestUrl = "https://www.youtube.com/watch?v=A_MjCqQoLLA&list=PLyZup5wpI7M7MXxXDS4riiZ_E3rh6t_pi"; // big playlist with 1000 videos

    public YtdlpBenchmark()
    {
        _factory = new ProcessFactory(Path.Combine(toolsPath, "yt-dlp.exe"));
        _newRunner = new ProcessRunner(_factory, new ConsoleLogger());
        _oldProbeRunner = new ProbeRunner(_factory, new ConsoleLogger());
    }

    [Benchmark(Baseline = true)]
    public async Task OldProbe_ReadToEndAsync()
    {
        await _oldProbeRunner.RunAsync($"-j --flat-playlist \"{TestUrl}\"");
    }

    [Benchmark]
    public async Task NewRunner_CaptureFullOutput()
    {
        await _newRunner.ExecuteAsync(
            $"-j --flat-playlist \"{TestUrl}\"",
            captureFullOutput: true);
    }

    [Benchmark]
    public async Task NewRunner_LineByLine()
    {
        var list = new List<string>(500);
        await _newRunner.ExecuteAsync(
            $"-j --flat-playlist \"{TestUrl}\"",
            onLineReceived: list.Add);
    }
}
