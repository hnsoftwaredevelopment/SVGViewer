using System.IO;
using System.Text;

namespace SVGViewer.Services;

/// <summary>
/// Minimal, dependency-free file logger. Best-effort and thread-safe: logging must
/// never throw or interfere with the app, so every failure is swallowed. Writes to
/// <c>%AppData%\SVGViewer\logs\app.log</c> and rotates to <c>app.prev.log</c> once
/// the file passes <see cref="MaxBytes"/>.
/// </summary>
public static class Logger
{
    private static readonly object Gate = new();

    private static string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SVGViewer", "logs");

    /// <summary>Size at which the log rotates. Adjustable for tests.</summary>
    internal static long MaxBytes { get; set; } = 1_000_000;

    /// <summary>The folder the log file lives in (shown to the user in error dialogs).</summary>
    public static string LogDirectory => _directory;

    /// <summary>Full path of the current log file.</summary>
    public static string LogFilePath => Path.Combine(_directory, "app.log");

    /// <summary>Overrides the log folder. Call once at start-up (or from tests).</summary>
    public static void Configure(string directory)
    {
        lock (Gate)
        {
            _directory = directory;
        }
    }

    public static void Info(string message) => Write("INFO", message, null);

    public static void Warn(string message, Exception? exception = null) => Write("WARN", message, exception);

    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (Gate)
            {
                System.IO.Directory.CreateDirectory(_directory);

                var path = LogFilePath;
                RotateIfNeeded(path);

                var line = new StringBuilder()
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .Append(" [").Append(level).Append("] ")
                    .Append(message);

                if (exception is not null)
                {
                    line.Append(" | ").Append(exception.GetType().Name)
                        .Append(": ").Append(exception.Message)
                        .Append(Environment.NewLine).Append(exception.StackTrace);
                }

                line.Append(Environment.NewLine);
                File.AppendAllText(path, line.ToString());
            }
        }
        catch
        {
            // Logging must never crash or block the app.
        }
    }

    private static void RotateIfNeeded(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Exists && info.Length >= MaxBytes)
            {
                var backup = Path.Combine(_directory, "app.prev.log");
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }

                File.Move(path, backup);
            }
        }
        catch
        {
            // If rotation fails, just keep appending to the current file.
        }
    }
}
