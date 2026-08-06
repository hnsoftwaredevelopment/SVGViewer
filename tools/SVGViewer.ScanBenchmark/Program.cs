using System.Diagnostics;
using System.IO;
using SVGViewer.Services;

if (args.Length != 2 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: SVGViewer.ScanBenchmark <existing-folder> <iterations 1-10>");
    return 2;
}

if (!int.TryParse(args[1], out var iterations) || iterations is < 1 or > 10)
{
    Console.Error.WriteLine("The iteration count must be between 1 and 10.");
    return 2;
}

var rootPath = Path.GetFullPath(args[0]);
var scanner = new SvgIndexService();
var measurements = new List<Measurement>();

Console.WriteLine($"Scanning {rootPath} ({iterations} iteration(s))");
for (var iteration = 1; iteration <= iterations; iteration++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var stopwatch = Stopwatch.StartNew();
    var index = await scanner.BuildIndexAsync(rootPath);
    stopwatch.Stop();

    var measurement = new Measurement(
        iteration,
        stopwatch.Elapsed,
        index.TotalFoldersScanned,
        index.FoldersWithSvg.Count,
        index.RelevantFolders.Count);
    measurements.Add(measurement);

    Console.WriteLine(
        $"Run {iteration}: {measurement.Elapsed.TotalSeconds:N2} s | " +
        $"{measurement.FoldersScanned:N0} folders | {measurement.FoldersPerSecond:N0} folders/s | " +
        $"{measurement.FoldersWithSvg:N0} SVG folders");
}

Console.WriteLine("\nSummary");
var summary = new Summary(
    measurements.Count,
    TimeSpan.FromTicks((long)measurements.Average(m => m.Elapsed.Ticks)),
    measurements.Average(m => m.FoldersPerSecond));
Console.WriteLine(
    $"{summary.Runs} run(s), {summary.AverageElapsed.TotalSeconds:N2} s average, " +
    $"{summary.AverageFoldersPerSecond:N0} folders/s average");
Console.WriteLine("Tip: use 3 iterations for a steadier result.");

return 0;

internal sealed record Measurement(
    int Iteration,
    TimeSpan Elapsed,
    int FoldersScanned,
    int FoldersWithSvg,
    int RelevantFolders)
{
    public double FoldersPerSecond =>
        Elapsed.Ticks == 0 ? 0 : FoldersScanned / Elapsed.TotalSeconds;
}

internal sealed record Summary(
    int Runs,
    TimeSpan AverageElapsed,
    double AverageFoldersPerSecond);
