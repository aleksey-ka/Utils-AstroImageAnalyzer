using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AstroImageAnalyzer.Core.Models;
using AstroImageAnalyzer.Core.Services;
using AstroImageAnalyzer.UI;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace AstroImageAnalyzer.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFitsReader _fitsReader;
    private readonly IStatisticsCalculator _statisticsCalculator;
    
    private FitsImageData? _selectedImage;
    private ImageStatistics? _selectedStatistics;
    private string _statusMessage = "Ready";
    private string? _loadedFolderName;
    private PlotModel? _histogramModel;
    private ImageSource? _previewImage;
    private bool _isDebayerEnabled;
    private double? _stfMin;
    private double? _stfMax;

    public MainViewModel(IFitsReader fitsReader, IStatisticsCalculator statisticsCalculator)
    {
        _fitsReader = fitsReader;
        _statisticsCalculator = statisticsCalculator;
        
        LoadFilesCommand = new RelayCommand(_ => LoadFiles());
        LoadLastFileCommand = new RelayCommand(_ => LoadLastFile());
        ToggleDebayerCommand = new RelayCommand(_ => IsDebayerEnabled = !IsDebayerEnabled);

        HistogramModel = CreateEmptyHistogramModel();
    }
    
    public ObservableCollection<FitsImageData> LoadedImages { get; } = new();
    public ObservableCollection<ImageStatistics> Statistics { get; } = new();
    
    public FitsImageData? SelectedImage
    {
        get => _selectedImage;
        set
        {
            _selectedImage = value;
            OnPropertyChanged();
            UpdateSelectedStatistics();
            if (value != null)
                WindowPlacement.SaveLastFilePath(value.FilePath);
        }
    }
    
    public ImageStatistics? SelectedStatistics
    {
        get => _selectedStatistics;
        set
        {
            _selectedStatistics = value;
            OnPropertyChanged();
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>Name of the open folder, or "Loaded Files" when none.</summary>
    public string LoadedFolderName => string.IsNullOrEmpty(_loadedFolderName) ? "Loaded Files" : _loadedFolderName;

    /// <summary>True when a folder has been loaded (header should be visible).</summary>
    public bool HasLoadedFolder => !string.IsNullOrEmpty(_loadedFolderName);
    
    public ICommand LoadFilesCommand { get; }
    public ICommand LoadLastFileCommand { get; }
    public ICommand ToggleDebayerCommand { get; }

    /// <summary>Set STF limit from histogram click. Called from view when user clicks the graph.</summary>
    public void SetStfLimitFromHistogram(double value, bool isLowerLimit)
    {
        if (isLowerLimit)
            _stfMin = value;
        else
            _stfMax = value;
        UpdatePreviewImage();
        UpdateHistogram();
        StatusMessage = isLowerLimit ? $"STF min set to {value:F1}" : $"STF max set to {value:F1}";
    }

    public PlotModel? HistogramModel
    {
        get => _histogramModel;
        set
        {
            _histogramModel = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        set
        {
            _previewImage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>When true and the image has a Bayer pattern, preview is debayered to color.</summary>
    public bool IsDebayerEnabled
    {
        get => _isDebayerEnabled;
        set
        {
            if (_isDebayerEnabled == value) return;
            _isDebayerEnabled = value;
            OnPropertyChanged();
            UpdatePreviewImage();
        }
    }

    private void LoadFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an image file (all files with the same extension in the folder will be loaded)",
            Filter = "FITS files (*.fits;*.fit;*.fts)|*.fits;*.fit;*.fts|All files (*.*)|*.*",
            Multiselect = false
        };
        
        if (dialog.ShowDialog() == true)
        {
            LoadFromFilePath(dialog.FileName);
        }
    }

    private void LoadLastFile()
    {
        var path = WindowPlacement.GetLastFilePath();
        if (string.IsNullOrEmpty(path))
        {
            StatusMessage = "No previous file to load";
            return;
        }
        if (!File.Exists(path))
        {
            StatusMessage = "Previous file no longer exists";
            return;
        }
        LoadFromFilePath(path);
    }
    
    /// <summary>
    /// Loads all files in the same folder as the given file that share the same extension.
    /// Same logic as File → Open; used for menu and for drag-and-drop.
    /// </summary>
    public void LoadFromFilePath(string selectedFilePath)
    {
        try
        {
            StatusMessage = "Scanning folder...";

            if (!File.Exists(selectedFilePath))
            {
                StatusMessage = "Selected file does not exist";
                return;
            }

            var folderPath = Path.GetDirectoryName(selectedFilePath);
            if (string.IsNullOrEmpty(folderPath))
            {
                StatusMessage = "Could not determine folder path";
                return;
            }

            _loadedFolderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            OnPropertyChanged(nameof(LoadedFolderName));
            OnPropertyChanged(nameof(HasLoadedFolder));

            var extension = Path.GetExtension(selectedFilePath);
            if (string.IsNullOrEmpty(extension))
            {
                StatusMessage = "Selected file has no extension";
                return;
            }

            var searchPattern = "*" + extension;
            var matchingFiles = Directory.GetFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly);

            if (matchingFiles.Length == 0)
            {
                StatusMessage = $"No files with extension {extension} found in folder";
                return;
            }

            LoadedImages.Clear();
            Statistics.Clear();
            SelectedImage = null;
            SelectedStatistics = null;

            StatusMessage = $"Loading {matchingFiles.Length} file(s)...";

            int loadedCount = 0;
            int errorCount = 0;

            foreach (var filePath in matchingFiles.OrderBy(f => f))
            {
                try
                {
                    var imageData = _fitsReader.ReadFitsFile(filePath);
                    LoadedImages.Add(imageData);

                    var stats = _statisticsCalculator.CalculateStatistics(imageData);
                    Statistics.Add(stats);

                    loadedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    StatusMessage = $"Error loading {Path.GetFileName(filePath)}: {ex.Message}";
                }
            }

            if (LoadedImages.Count > 0)
            {
                var normalizedSelected = Path.GetFullPath(selectedFilePath);
                var picked = LoadedImages.FirstOrDefault(img => string.Equals(Path.GetFullPath(img.FilePath), normalizedSelected, StringComparison.OrdinalIgnoreCase));
                SelectedImage = picked ?? LoadedImages[0];
                if (errorCount > 0)
                    StatusMessage = $"Loaded {loadedCount} file(s), {errorCount} error(s)";
                else
                    StatusMessage = $"Loaded {loadedCount} file(s)";
            }
            else
            {
                StatusMessage = $"Failed to load any files ({errorCount} error(s))";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
    
    private void UpdateSelectedStatistics()
    {
        if (SelectedImage != null)
        {
            SelectedStatistics = Statistics.FirstOrDefault(s => s.FilePath == SelectedImage.FilePath);
            UpdateHistogram();
            UpdatePreviewImage();
        }
        else
        {
            SelectedStatistics = null;
            HistogramModel = CreateEmptyHistogramModel();
            PreviewImage = null;
        }
    }

    private void UpdatePreviewImage()
    {
        if (SelectedImage == null || SelectedImage.PixelData == null)
        {
            PreviewImage = null;
            return;
        }

        int height = SelectedImage.PixelData.GetLength(0);
        int width = SelectedImage.PixelData.GetLength(1);

        if (width <= 0 || height <= 0)
        {
            PreviewImage = null;
            return;
        }

        double min = double.MaxValue;
        double max = double.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double v = SelectedImage.PixelData[y, x];
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        if (min == double.MaxValue || max == double.MinValue || max <= min)
        {
            PreviewImage = null;
            return;
        }

        double effectiveMin = _stfMin ?? min;
        double effectiveMax = _stfMax ?? max;
        if (effectiveMin >= effectiveMax)
        {
            effectiveMin = min;
            effectiveMax = max;
        }

        int dpi = 96;
        BitmapSource bitmap;

        bool useDebayer = IsDebayerEnabled && !string.IsNullOrEmpty(SelectedImage.BayerPattern);

        if (useDebayer)
        {
            byte[] bgr = DebayerService.DebayerBilinear(SelectedImage.PixelData, SelectedImage.BayerPattern!, effectiveMin, effectiveMax);
            int stride = width * 3;
            bitmap = BitmapSource.Create(
                width,
                height,
                dpi,
                dpi,
                PixelFormats.Bgr24,
                null,
                bgr,
                stride);
        }
        else
        {
            var pixels = new byte[width * height];
            double range = effectiveMax - effectiveMin;
            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double v = SelectedImage.PixelData[y, x];
                    if (double.IsNaN(v) || double.IsInfinity(v))
                    {
                        pixels[index++] = 0;
                        continue;
                    }
                    double norm = (v - effectiveMin) / range;
                    pixels[index++] = (byte)Math.Clamp(norm * 255.0, 0.0, 255.0);
                }
            }
            bitmap = BitmapSource.Create(
                width,
                height,
                dpi,
                dpi,
                PixelFormats.Gray8,
                null,
                pixels,
                width);
        }

        bitmap.Freeze();
        PreviewImage = bitmap;
    }
    
    private void UpdateHistogram()
    {
        if (SelectedStatistics == null || SelectedStatistics.Histogram.Count == 0)
        {
            HistogramModel = CreateEmptyHistogramModel();
            return;
        }

        double dataMin = SelectedStatistics.Minimum;
        double dataMax = SelectedStatistics.Maximum;
        double effectiveMin = _stfMin ?? dataMin;
        double effectiveMax = _stfMax ?? dataMax;
        if (effectiveMin >= effectiveMax)
        {
            effectiveMin = dataMin;
            effectiveMax = dataMax;
        }
        
        var model = new PlotModel
        {
            PlotAreaBorderColor = OxyColors.Gray,
            PlotAreaBorderThickness = new OxyThickness(1),
            TextColor = OxyColors.White,
            Background = OxyColor.FromRgb(45, 45, 48)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            TitleColor = OxyColors.White,
            TextColor = OxyColors.White,
            TicklineColor = OxyColors.Gray,
            MajorGridlineColor = OxyColor.FromRgb(70, 70, 70),
            MinorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            TitleColor = OxyColors.White,
            TextColor = OxyColors.White,
            TicklineColor = OxyColors.Gray,
            MajorGridlineColor = OxyColor.FromRgb(70, 70, 70),
            MinorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });
        
        var series = new LineSeries
        {
            Color = OxyColor.FromRgb(0, 120, 212),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3
        };
        
        var sortedHistogram = SelectedStatistics.Histogram.OrderBy(h => h.Key).ToList();
        foreach (var bin in sortedHistogram)
        {
            series.Points.Add(new DataPoint(bin.Key, bin.Value));
        }
        
        model.Series.Add(series);

        if (_stfMin.HasValue || _stfMax.HasValue)
        {
            if (_stfMin.HasValue)
            {
                model.Annotations.Add(new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = effectiveMin,
                    Color = OxyColors.Orange,
                    StrokeThickness = 1.5,
                    LineStyle = LineStyle.Dash
                });
            }
            if (_stfMax.HasValue)
            {
                model.Annotations.Add(new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = effectiveMax,
                    Color = OxyColors.Orange,
                    StrokeThickness = 1.5,
                    LineStyle = LineStyle.Dash
                });
            }
        }
        
        HistogramModel = model;
    }
    
    private static PlotModel CreateEmptyHistogramModel()
    {
        var model = new PlotModel
        {
            PlotAreaBorderColor = OxyColors.Gray,
            PlotAreaBorderThickness = new OxyThickness(1),
            TextColor = OxyColors.White,
            Background = OxyColor.FromRgb(45, 45, 48)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            TitleColor = OxyColors.White,
            TextColor = OxyColors.White,
            TicklineColor = OxyColors.Gray
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            TitleColor = OxyColors.White,
            TextColor = OxyColors.White,
            TicklineColor = OxyColors.Gray
        });
        
        return model;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    
    public void Execute(object? parameter) => _execute(parameter);
}
