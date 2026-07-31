using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class FileOperationServiceTests
{
    [Fact]
    public void DeleteToRecycleBin_removes_an_existing_file()
    {
        var path = Path.Combine(Path.GetTempPath(), "svgv-del-" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(path, "<svg/>");

        var outcome = new FileOperationService().DeleteToRecycleBin(path);

        Assert.Equal(FileOperationOutcome.Success, outcome);
        Assert.False(File.Exists(path), "the file should no longer be at its original path");
    }

    [Fact]
    public void DeleteToRecycleBin_reports_a_missing_file()
    {
        var path = Path.Combine(Path.GetTempPath(), "svgv-missing-" + Guid.NewGuid().ToString("N") + ".svg");

        var outcome = new FileOperationService().DeleteToRecycleBin(path);

        Assert.Equal(FileOperationOutcome.FileNotFound, outcome);
    }
}
