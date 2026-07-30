using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class LoggerTests
{
    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "SVGViewerLoggerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Error_writes_level_message_and_exception_details()
    {
        var dir = NewTempDir();
        try
        {
            Logger.Configure(dir);
            Logger.Error("something broke", new InvalidOperationException("bad state"));

            var content = File.ReadAllText(Logger.LogFilePath);

            Assert.Contains("[ERROR]", content);
            Assert.Contains("something broke", content);
            Assert.Contains("InvalidOperationException", content);
            Assert.Contains("bad state", content);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public void Log_rotates_to_prev_once_it_exceeds_the_limit()
    {
        var dir = NewTempDir();
        try
        {
            Logger.Configure(dir);

            // One oversized entry pushes the file past the rotation threshold...
            Logger.Info(new string('x', 1_100_000));
            // ...so the next write rotates the current file to app.prev.log.
            Logger.Info("after rotation");

            var backup = Path.Combine(dir, "app.prev.log");
            Assert.True(File.Exists(backup), "expected the previous log to be rotated out");

            var current = File.ReadAllText(Logger.LogFilePath);
            Assert.Contains("after rotation", current);
            Assert.True(new FileInfo(Logger.LogFilePath).Length < 100_000, "fresh log should be small");
        }
        finally
        {
            TryDelete(dir);
        }
    }

    private static void TryDelete(string dir)
    {
        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup; the OS clears the temp folder eventually.
        }
    }
}
