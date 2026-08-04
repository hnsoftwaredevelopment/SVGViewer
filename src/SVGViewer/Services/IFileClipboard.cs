using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;

namespace SVGViewer.Services;

/// <summary>Whether clipboard files were placed there to copy or to move (cut).</summary>
public enum ClipboardOperation
{
    None,
    Copy,
    Move
}

/// <summary>The files on the clipboard and how they were placed there.</summary>
public readonly record struct ClipboardContents(IReadOnlyList<string> Files, ClipboardOperation Operation);

/// <summary>
/// A file clipboard for copy/cut/paste. Backed by the Windows clipboard (with the
/// standard "Preferred DropEffect") so it interoperates with Explorer. Abstracted
/// for testability. Cutting only marks the file; it is not moved until pasted.
/// </summary>
public interface IFileClipboard
{
    void SetCopy(string path);

    void SetMove(string path);

    ClipboardContents GetContents();

    void Clear();
}

/// <summary>Windows-clipboard implementation (file drop list + drop effect).</summary>
public sealed class WpfFileClipboard : IFileClipboard
{
    private const string DropEffectFormat = "Preferred DropEffect";
    private const int DropEffectCopy = 1; // DROPEFFECT_COPY
    private const int DropEffectMove = 2; // DROPEFFECT_MOVE

    public void SetCopy(string path) => SetWithEffect(path, DropEffectCopy);

    public void SetMove(string path) => SetWithEffect(path, DropEffectMove);

    private static void SetWithEffect(string path, int effect)
    {
        try
        {
            var data = new DataObject();
            data.SetFileDropList(new StringCollection { path });
            data.SetData(DropEffectFormat, new MemoryStream(BitConverter.GetBytes(effect)));
            Clipboard.SetDataObject(data, true);
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not place the file on the clipboard.", ex);
        }
    }

    public ClipboardContents GetContents()
    {
        try
        {
            if (!Clipboard.ContainsFileDropList())
            {
                return new ClipboardContents(Array.Empty<string>(), ClipboardOperation.None);
            }

            var files = Clipboard.GetFileDropList()
                .Cast<string?>()
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToList();

            var operation = files.Count == 0 ? ClipboardOperation.None : ReadOperation();
            return new ClipboardContents(files, operation);
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not read files from the clipboard.", ex);
            return new ClipboardContents(Array.Empty<string>(), ClipboardOperation.None);
        }
    }

    private static ClipboardOperation ReadOperation()
    {
        // No "Preferred DropEffect" present (e.g. a plain SetFileDropList) means copy.
        var data = Clipboard.GetDataObject();
        if (data is not null && data.GetDataPresent(DropEffectFormat) &&
            data.GetData(DropEffectFormat) is MemoryStream stream)
        {
            var bytes = new byte[4];
            stream.Position = 0;
            if (stream.Read(bytes, 0, 4) == 4 && (BitConverter.ToInt32(bytes, 0) & DropEffectMove) == DropEffectMove)
            {
                return ClipboardOperation.Move;
            }
        }

        return ClipboardOperation.Copy;
    }

    public void Clear()
    {
        try
        {
            Clipboard.Clear();
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not clear the clipboard.", ex);
        }
    }
}
