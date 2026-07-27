using System.IO;
using System.Reflection;

namespace SVGViewer.Services;

/// <summary>
/// Reads the Syncfusion license key from a local <c>syncfusionlicense.txt</c> file
/// and registers it, if the Syncfusion licensing assembly is present.
///
/// Design notes:
/// <list type="bullet">
///   <item>The key file is git-ignored and must never be committed.</item>
///   <item>Registration happens through reflection, so the application also
///         builds and runs when no Syncfusion package is referenced.</item>
///   <item>The key is never written to a log or shown in the UI.</item>
/// </list>
/// </summary>
public static class LicenseManager
{
    private const string LicenseFileName = "syncfusionlicense.txt";

    /// <summary>True when a key was found and successfully registered.</summary>
    public static bool IsLicenseRegistered { get; private set; }

    /// <summary>
    /// Locates and registers the license key. Safe to call unconditionally:
    /// a missing file or missing Syncfusion assembly is not an error.
    /// </summary>
    public static void RegisterIfAvailable()
    {
        var key = TryReadLicenseKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        IsLicenseRegistered = TryRegister(key!);
    }

    /// <summary>
    /// Searches for the license file next to the executable and in the project
    /// folders above it, so it works both when running from Visual Studio and
    /// from a published build.
    /// </summary>
    private static string? TryReadLicenseKey()
    {
        foreach (var candidate in GetCandidatePaths())
        {
            try
            {
                if (File.Exists(candidate))
                {
                    var content = File.ReadAllText(candidate).Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return content;
                    }
                }
            }
            catch (Exception)
            {
                // An unreadable candidate should not stop the search.
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        var directory = AppContext.BaseDirectory;

        // Walk up from the output folder: bin\Debug\net8.0-windows -> project -> src -> repo root.
        for (var i = 0; i < 6 && directory is not null; i++)
        {
            yield return Path.Combine(directory, LicenseFileName);
            directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
        }
    }

    /// <summary>
    /// Calls Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(string)
    /// via reflection when that assembly is loaded in the current app.
    /// </summary>
    private static bool TryRegister(string key)
    {
        try
        {
            var providerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Syncfusion.Licensing.SyncfusionLicenseProvider", throwOnError: false))
                .FirstOrDefault(t => t is not null);

            var method = providerType?.GetMethod(
                "RegisterLicense",
                BindingFlags.Public | BindingFlags.Static,
                new[] { typeof(string) });

            if (method is null)
            {
                // Syncfusion is not referenced in this build; nothing to do.
                return false;
            }

            method.Invoke(null, new object[] { key });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
