using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace SVGViewer.Services;

/// <summary>
/// A file clipboard for copy/paste. Backed by the Windows clipboard so files can
/// be pasted to/from Explorer too. Abstracted for testability.
/// </summary>
public interface IFileClipboard
{
    void SetFile(string path);

    IReadOnlyList<string> GetFiles();
}

/// <summary>Windows-clipboard implementation (file drop list).</summary>
public sealed class WpfFileClipboard : IFileClipboard
{
    public void SetFile(string path)
    {
        try
        {
            Clipboard.SetFileDropList(new StringCollection { path });
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not copy the file to the clipboard.", ex);
        }
    }

    public IReadOnlyList<string> GetFiles()
    {
        try
        {
            if (!Clipboard.ContainsFileDropList())
            {
                return Array.Empty<string>();
            }

            return Clipboard.GetFileDropList()
                .Cast<string?>()
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not read files from the clipboard.", ex);
            return Array.Empty<string>();
        }
    }
}
