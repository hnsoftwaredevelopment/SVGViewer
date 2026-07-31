using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class FileOperationServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly FileOperationService _service = new();

    public FileOperationServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "svgv-fileop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    private string CreateFile(string name, string content = "<svg/>")
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void DeleteToRecycleBin_removes_an_existing_file()
    {
        var path = CreateFile("delete-me.svg");

        var outcome = _service.DeleteToRecycleBin(path);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.False(File.Exists(path), "the file should no longer be at its original path");
    }

    [Fact]
    public void DeleteToRecycleBin_reports_a_missing_file()
    {
        var outcome = _service.DeleteToRecycleBin(Path.Combine(_dir, "nope.svg"));

        Assert.Equal(FileOperationOutcome.FileNotFound, outcome);
    }

    [Fact]
    public void Rename_moves_the_file_to_the_new_name()
    {
        var path = CreateFile("old.svg");

        var outcome = _service.Rename(path, "new.svg", overwrite: false);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(Path.Combine(_dir, "new.svg")));
    }

    [Fact]
    public void Rename_reports_a_conflict_without_overwrite()
    {
        var path = CreateFile("a.svg");
        CreateFile("b.svg");

        var outcome = _service.Rename(path, "b.svg", overwrite: false);

        Assert.Equal(FileOperationOutcome.TargetExists, outcome);
        Assert.True(File.Exists(path), "the source should be left untouched on a conflict");
    }

    [Fact]
    public void Rename_replaces_the_target_when_overwrite_is_allowed()
    {
        var path = CreateFile("a.svg", "<svg>a</svg>");
        CreateFile("b.svg", "<svg>b</svg>");

        var outcome = _service.Rename(path, "b.svg", overwrite: true);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.False(File.Exists(path));
        Assert.Equal("<svg>a</svg>", File.ReadAllText(Path.Combine(_dir, "b.svg")));
    }

    [Fact]
    public void Rename_reports_a_missing_source()
    {
        var outcome = _service.Rename(Path.Combine(_dir, "ghost.svg"), "x.svg", overwrite: false);

        Assert.Equal(FileOperationOutcome.FileNotFound, outcome);
    }

    [Fact]
    public void Rename_rejects_an_invalid_name()
    {
        var path = CreateFile("valid.svg");

        var outcome = _service.Rename(path, "bad<name>.svg", overwrite: false);

        Assert.Equal(FileOperationOutcome.InvalidName, outcome);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void CreateFolder_creates_a_sub_folder()
    {
        var outcome = _service.CreateFolder(_dir, "new-folder");

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.True(Directory.Exists(Path.Combine(_dir, "new-folder")));
    }

    [Fact]
    public void CreateFolder_reports_an_existing_folder()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "existing"));

        var outcome = _service.CreateFolder(_dir, "existing");

        Assert.Equal(FileOperationOutcome.TargetExists, outcome);
    }

    [Fact]
    public void CreateFolder_rejects_an_invalid_name()
    {
        var outcome = _service.CreateFolder(_dir, "bad:name");

        Assert.Equal(FileOperationOutcome.InvalidName, outcome);
    }
}
