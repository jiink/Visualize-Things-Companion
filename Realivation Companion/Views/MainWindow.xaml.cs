using Realivation_Companion.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Realivation_Companion.Views
{
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
}
