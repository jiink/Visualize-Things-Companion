using System.Windows;

namespace Realivation_Companion.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
public partial class AboutView : Window
{
    public AboutView()
    {
        Owner = Application.Current.MainWindow;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        InitializeComponent();
    }
}
