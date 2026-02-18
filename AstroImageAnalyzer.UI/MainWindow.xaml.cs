using System.Windows;
using System.Windows.Input;
using AstroImageAnalyzer.Core.Services;
using AstroImageAnalyzer.UI.ViewModels;

namespace AstroImageAnalyzer.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Restore last position and size (or keep XAML defaults)
        WindowPlacement.Restore(this, 1400, 900);
        
        // Initialize services and ViewModel
        var fitsReader = new FitsReader();
        var statisticsCalculator = new StatisticsCalculator();
        DataContext = new MainViewModel(fitsReader, statisticsCalculator);
        
        // Update maximize button icon when window state changes
        StateChanged += MainWindow_StateChanged;
        UpdateMaximizeButton();
        
        Closing += MainWindow_Closing;
    }
    
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        WindowPlacement.Save(this);
    }
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }
    }
    
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }
    
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeButton();
    }
    
    private void UpdateMaximizeButton()
    {
        if (MaximizeButton != null)
        {
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }
    }

    private void ImagePanel_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void ImagePanel_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (paths == null || paths.Length == 0)
            return;

        var firstPath = paths[0];
        if (string.IsNullOrWhiteSpace(firstPath))
            return;

        if (DataContext is MainViewModel vm)
            vm.LoadFromFilePath(firstPath.Trim());

        e.Handled = true;
    }
}
