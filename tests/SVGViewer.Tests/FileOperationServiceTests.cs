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

    [Fact]
    public void Copy_places_the_file_in_another_folder()
    {
        var source = CreateFile("logo.svg", "<svg>x</svg>");
        var target = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(target);

        var outcome = _service.Copy(source, target, overwrite: false);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.True(File.Exists(source), "the source should remain");
        Assert.Equal("<svg>x</svg>", File.ReadAllText(Path.Combine(target, "logo.svg")));
    }

    [Fact]
    public void Copy_reports_a_conflict_without_overwrite()
    {
        var source = CreateFile("logo.svg");
        var target = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "logo.svg"), "<svg>old</svg>");

        var outcome = _service.Copy(source, target, overwrite: false);

        Assert.Equal(FileOperationOutcome.TargetExists, outcome);
    }

    [Fact]
    public void Copy_overwrites_the_target_when_allowed()
    {
        var source = CreateFile("logo.svg", "<svg>new</svg>");
        var target = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "logo.svg"), "<svg>old</svg>");

        var outcome = _service.Copy(source, target, overwrite: true);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.Equal("<svg>new</svg>", File.ReadAllText(Path.Combine(target, "logo.svg")));
    }

    [Fact]
    public void Copy_into_the_same_folder_makes_a_uniquely_named_duplicate()
    {
        var source = CreateFile("logo.svg");

        var outcome = _service.Copy(source, _dir, overwrite: false);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.True(File.Exists(source), "the original should remain");
        Assert.True(File.Exists(Path.Combine(_dir, "logo (2).svg")), "a duplicate should be created");
    }

    [Fact]
    public void Copy_reports_a_missing_source()
    {
        var outcome = _service.Copy(Path.Combine(_dir, "ghost.svg"), _dir, overwrite: false);

        Assert.Equal(FileOperationOutcome.FileNotFound, outcome);
    }

    [Fact]
    public void Move_places_the_file_in_another_folder_and_removes_the_source()
    {
        var source = CreateFile("logo.svg", "<svg>x</svg>");
        var target = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(target);

        var outcome = _service.Move(source, target, overwrite: false);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.False(File.Exists(source), "the source should be gone after a move");
        Assert.Equal("<svg>x</svg>", File.ReadAllText(Path.Combine(target, "logo.svg")));
    }

    [Fact]
    public void Move_reports_a_conflict_without_overwrite_and_keeps_the_source()
    {
        var source = CreateFile("logo.svg");
        var target = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "logo.svg"), "<svg>old</svg>");

        var outcome = _service.Move(source, target, overwrite: false);

        Assert.Equal(FileOperationOutcome.TargetExists, outcome);
        Assert.True(File.Exists(source), "the source should remain on a conflict");
    }

    [Fact]
    public void Move_overwrites_the_target_when_allowed()
    {
        var source = CreateFile("logo.svg", "<svg>new</svg>");
        var target = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "logo.svg"), "<svg>old</svg>");

        var outcome = _service.Move(source, target, overwrite: true);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.False(File.Exists(source));
        Assert.Equal("<svg>new</svg>", File.ReadAllText(Path.Combine(target, "logo.svg")));
    }

    [Fact]
    public void Move_into_the_same_folder_is_a_no_op()
    {
        var source = CreateFile("logo.svg");

        var outcome = _service.Move(source, _dir, overwrite: false);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.True(File.Exists(source), "the file should stay where it is");
    }

    [Fact]
    public void Move_reports_a_missing_source()
    {
        var target = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(target);

        var outcome = _service.Move(Path.Combine(_dir, "ghost.svg"), target, overwrite: false);

        Assert.Equal(FileOperationOutcome.FileNotFound, outcome);
    }
}
