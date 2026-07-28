using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class FileOpenServiceTests
{
    /// <summary>Records what would be launched, or throws a configured exception.</summary>
    private sealed class FakeLauncher : IShellLauncher
    {
        private readonly Exception? _throw;

        public FakeLauncher(Exception? toThrow = null) => _throw = toThrow;

        public ProcessStartInfo? LastStart { get; private set; }
        public int CallCount { get; private set; }

        public void Start(ProcessStartInfo startInfo)
        {
            CallCount++;
            LastStart = startInfo;
            if (_throw is not null)
            {
                throw _throw;
            }
        }
    }

    private static string ExistingSvg(TestTree tree) => Path.Combine(tree.Icons, "one.svg");

    [Fact]
    public void OpenInAssociatedApp_launches_the_file_via_the_shell()
    {
        using var tree = new TestTree();
        var launcher = new FakeLauncher();
        var service = new FileOpenService(launcher);

        var outcome = service.OpenInAssociatedApp(ExistingSvg(tree));

        Assert.Equal(FileActionOutcome.Opened, outcome);
        Assert.Equal(ExistingSvg(tree), launcher.LastStart!.FileName);
        Assert.True(launcher.LastStart.UseShellExecute);
    }

    [Fact]
    public void OpenInAssociatedApp_reports_missing_file_without_launching()
    {
        var launcher = new FakeLauncher();
        var service = new FileOpenService(launcher);

        var outcome = service.OpenInAssociatedApp(@"Z:\gone\missing.svg");

        Assert.Equal(FileActionOutcome.FileNotFound, outcome);
        Assert.Equal(0, launcher.CallCount);
    }

    [Fact]
    public void OpenInAssociatedApp_maps_error_1155_to_NoAssociation()
    {
        using var tree = new TestTree();
        // 1155 == ERROR_NO_ASSOCIATION
        var launcher = new FakeLauncher(new Win32Exception(1155));
        var service = new FileOpenService(launcher);

        var outcome = service.OpenInAssociatedApp(ExistingSvg(tree));

        Assert.Equal(FileActionOutcome.NoAssociation, outcome);
    }

    [Fact]
    public void OpenInAssociatedApp_maps_other_failures_to_Failed()
    {
        using var tree = new TestTree();
        var launcher = new FakeLauncher(new InvalidOperationException("boom"));
        var service = new FileOpenService(launcher);

        var outcome = service.OpenInAssociatedApp(ExistingSvg(tree));

        Assert.Equal(FileActionOutcome.Failed, outcome);
    }

    [Fact]
    public void OpenWithDialog_uses_the_openas_verb()
    {
        using var tree = new TestTree();
        var launcher = new FakeLauncher();
        var service = new FileOpenService(launcher);

        var outcome = service.OpenWithDialog(ExistingSvg(tree));

        Assert.Equal(FileActionOutcome.Opened, outcome);
        Assert.Equal("openas", launcher.LastStart!.Verb);
        Assert.True(launcher.LastStart.UseShellExecute);
    }

    [Fact]
    public void ShowInExplorer_selects_the_file()
    {
        using var tree = new TestTree();
        var launcher = new FakeLauncher();
        var service = new FileOpenService(launcher);

        var outcome = service.ShowInExplorer(ExistingSvg(tree));

        Assert.Equal(FileActionOutcome.Opened, outcome);
        Assert.Equal("explorer.exe", launcher.LastStart!.FileName);
        Assert.Contains("/select", launcher.LastStart.Arguments);
        Assert.Contains(ExistingSvg(tree), launcher.LastStart.Arguments);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_path_is_reported_as_missing(string path)
    {
        var launcher = new FakeLauncher();
        var service = new FileOpenService(launcher);

        Assert.Equal(FileActionOutcome.FileNotFound, service.OpenInAssociatedApp(path));
        Assert.Equal(FileActionOutcome.FileNotFound, service.OpenWithDialog(path));
        Assert.Equal(FileActionOutcome.FileNotFound, service.ShowInExplorer(path));
        Assert.Equal(0, launcher.CallCount);
    }
}
