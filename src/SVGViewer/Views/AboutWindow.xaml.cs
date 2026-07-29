using System.Reflection;
using System.Windows;
using SVGViewer.Services;

namespace SVGViewer.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        AppLogo.Source = SvgResourceImage.Load("/Assets/appicon.svg");

        var companyLogo = SvgResourceImage.Load("/Assets/HN-Software_Logo_White.svg");
        if (companyLogo is not null)
        {
            CompanyLogo.Source = companyLogo;
        }
        else
        {
            // The publisher artwork has not been added yet; hide the strip.
            CompanySection.Visibility = Visibility.Collapsed;
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null
            ? string.Empty
            : $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
