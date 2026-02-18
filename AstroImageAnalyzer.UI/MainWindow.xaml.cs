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
    public static readonly RoutedCommand ToggleFullScreenCommand = new();

    private bool _isFullScreen;
    private double _restoreLeft, _restoreTop, _restoreWidth, _restoreHeight;

    public MainWindow()
    {
        InitializeComponent();

        CommandBindings.Add(new CommandBinding(ToggleFullScreenCommand, (_, _) => ToggleFullScreen()));
        
        // Restore last position and size (or keep XAML defaults)
        WindowPlacement.Restore(this, 1400, 900);
        
        // Initialize services and ViewModel
        var fitsReader = new FitsReader();
        var statisticsCalculator = new StatisticsCalculator();
        DataContext = new MainViewModel(fitsReader, statisticsCalculator);
        
        // Update maximize button icon when window state changes
        StateChanged += MainWindow_StateChanged;
        UpdateMaximizeButton();
        UpdateFullScreenButton();
        
        Closing += MainWindow_Closing;
        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isFullScreen)
        {
            ExitFullScreen();
            e.Handled = true;
        }
    }
    
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isFullScreen)
            WindowPlacement.SaveRestoreBounds(_restoreLeft, _restoreTop, _restoreWidth, _restoreHeight);
        else
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

    private void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void ToggleFullScreen()
    {
        if (_isFullScreen)
        {
            ExitFullScreen();
        }
        else
        {
            EnterFullScreen();
        }
    }

    private void EnterFullScreen()
    {
        // Save current position and size (use RestoreBounds if already maximized)
        if (WindowState == WindowState.Maximized)
        {
            _restoreLeft = RestoreBounds.Left;
            _restoreTop = RestoreBounds.Top;
            _restoreWidth = RestoreBounds.Width;
            _restoreHeight = RestoreBounds.Height;
        }
        else
        {
            _restoreLeft = Left;
            _restoreTop = Top;
            _restoreWidth = Width;
            _restoreHeight = Height;
        }

        WindowPlacement.SaveRestoreBounds(_restoreLeft, _restoreTop, _restoreWidth, _restoreHeight);

        // Size window to the work area (screen minus taskbar) so the bottom border and status bar stay visible
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left;
        Top = workArea.Top;
        Width = workArea.Width;
        Height = workArea.Height;
        WindowState = WindowState.Normal;
        WindowStartupLocation = WindowStartupLocation.Manual;

        _isFullScreen = true;
        UpdateFullScreenButton();
    }

    private void ExitFullScreen()
    {
        _isFullScreen = false;
        WindowState = WindowState.Normal;

        Left = _restoreLeft;
        Top = _restoreTop;
        Width = _restoreWidth;
        Height = _restoreHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;

        WindowPlacement.Save(this);
        UpdateFullScreenButton();
    }

    private void UpdateFullScreenButton()
    {
        if (FullScreenButton != null)
        {
            // E740 = full screen (expand), E73E = exit full screen (back to window)
            FullScreenButton.Content = _isFullScreen ? "\uE73E" : "\uE740";
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
