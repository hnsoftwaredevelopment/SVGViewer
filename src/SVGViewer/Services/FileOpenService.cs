using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace SVGViewer.Services;

/// <summary>Outcome of a file/shell action, so the UI can respond appropriately.</summary>
public enum FileActionOutcome
{
    /// <summary>The action was launched successfully.</summary>
    Opened,

    /// <summary>The file no longer exists on disk.</summary>
    FileNotFound,

    /// <summary>No application is associated with this file type.</summary>
    NoAssociation,

    /// <summary>The action failed for another reason.</summary>
    Failed
}

/// <summary>
/// Abstraction over launching a shell process, so the decision logic in
/// <see cref="FileOpenService"/> can be tested without starting real processes.
/// </summary>
public interface IShellLauncher
{
    void Start(ProcessStartInfo startInfo);
}

/// <summary>Default launcher that actually starts the process via the shell.</summary>
public sealed class ShellLauncher : IShellLauncher
{
    public void Start(ProcessStartInfo startInfo) => Process.Start(startInfo);
}

/// <summary>
/// Opens SVG files in their associated application, shows them in Explorer, or
/// offers the Windows "Open with" dialog. All actions fail safely and report an
/// <see cref="FileActionOutcome"/> instead of throwing.
/// </summary>
public sealed class FileOpenService
{
    /// <summary>Win32 error returned when a file type has no associated app.</summary>
    private const int ErrorNoAssociation = 1155;

    private readonly IShellLauncher _launcher;

    public FileOpenService(IShellLauncher? launcher = null)
    {
        _launcher = launcher ?? new ShellLauncher();
    }

    /// <summary>Opens the file with its associated application (e.g. Inkscape).</summary>
    public FileActionOutcome OpenInAssociatedApp(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return FileActionOutcome.FileNotFound;
        }

        try
        {
            _launcher.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return FileActionOutcome.Opened;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorNoAssociation)
        {
            return FileActionOutcome.NoAssociation;
        }
        catch (Exception)
        {
            return FileActionOutcome.Failed;
        }
    }

    /// <summary>Shows the Windows "Open with..." dialog for the file.</summary>
    public FileActionOutcome OpenWithDialog(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return FileActionOutcome.FileNotFound;
        }

        try
        {
            // The "openas" shell verb is unreliable across Windows versions, so we
            // invoke the picker directly. OpenAs_RunDLL treats the remainder of the
            // command line as the path, so spaces work without quoting.
            _launcher.Start(new ProcessStartInfo("rundll32.exe")
            {
                Arguments = $"shell32.dll,OpenAs_RunDLL {path}",
                UseShellExecute = true
            });
            return FileActionOutcome.Opened;
        }
        catch (Exception)
        {
            return FileActionOutcome.Failed;
        }
    }

    /// <summary>Opens Explorer with the file selected.</summary>
    public FileActionOutcome ShowInExplorer(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return FileActionOutcome.FileNotFound;
        }

        try
        {
            // /select, expects the quoted path immediately after the comma.
            _launcher.Start(new ProcessStartInfo("explorer.exe")
            {
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
            return FileActionOutcome.Opened;
        }
        catch (Exception)
        {
            return FileActionOutcome.Failed;
        }
    }
}
