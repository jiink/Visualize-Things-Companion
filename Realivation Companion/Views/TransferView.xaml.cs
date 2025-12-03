using Realivation_Companion.ViewModels;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Realivation_Companion.Views;

/// <summary>
/// Interaction logic for TransferView.xaml
/// </summary>
public partial class TransferView : UserControl
{
    Brush fileSubmissionAreaOgBackground;
    Brush fileSubmissionAreaOgBorder;
    public TransferView()
    {
        InitializeComponent();
        fileSubmissionAreaOgBackground = FileSubmissionArea.Background;
        fileSubmissionAreaOgBorder = FileSubmissionArea.BorderBrush;
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            FileSubmissionArea.Background = new SolidColorBrush(Colors.LightGreen);
            FileSubmissionArea.BorderBrush = new SolidColorBrush(Colors.Green);
            DropText.Text = "Release to Drop";
            DropText.Foreground = new SolidColorBrush(Colors.White);
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        ResetDragDropArea();
    }

    private void ResetDragDropArea()
    {
        FileSubmissionArea.Background = fileSubmissionAreaOgBackground;
        FileSubmissionArea.BorderBrush = fileSubmissionAreaOgBorder;
        DropText.Text = "Drag & Drop";
        DropText.Foreground = new SolidColorBrush(Colors.White);
    }

    private void OnDragNDrop(object sender, DragEventArgs e)
    {
        string filePath = "";
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                filePath = files[0]; // one file only
                Log.Information("Dropped file: " + filePath);
                ResetDragDropArea();
            }
            if (DataContext is TransferViewModel vm)
            {
                vm.OnDragNDrop(filePath);
            }
        }
    }

    private void OnFilePrompt(object sender, EventArgs e)
    {
        if (DataContext is TransferViewModel vm)
        {
            vm.OnFilePrompt();
        }
    }
}
