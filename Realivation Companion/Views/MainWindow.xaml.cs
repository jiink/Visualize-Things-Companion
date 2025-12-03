using Realivation_Companion.ViewModels;
using Serilog;
using System.Windows;

namespace Realivation_Companion.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Log.Logger = new LoggerConfiguration()
        .WriteTo.RichTextBox(MyRichTextBox)
        .CreateLogger();
    }

    public void Window_Loaded(object sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.RunStartupStuff();
        }
        else
        {
            Log.Fatal("Wrong datacontext?");
        }
    }
}
