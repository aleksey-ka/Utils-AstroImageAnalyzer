# Astro Image Analyzer

A .NET 8 WPF application for analyzing astronomical images with statistical measurements and visualizations. FITS (Flexible Image Transport System) is supported now; other image formats will be added.

## Features

- **Image Loading**: Load and parse FITS files (FITSReader library); additional formats planned
- **Image Statistics**: Calculate comprehensive statistics including:
  - Minimum, Maximum, Mean, Median
  - Standard Deviation and Variance
  - Pixel count and sum
  - Histogram distribution
- **Visualization**: 
  - Dark theme UI optimized for astronomical image viewing
  - Histogram charts showing pixel value distribution
  - Image statistics panel with detailed measurements
- **Multi-file Support**: Load and analyze multiple image files simultaneously

## Project Structure

```
AstroImageAnalyzer/
├── AstroImageAnalyzer.Core/        # Core library with business logic
│   ├── Models/                      # Data models (FitsImageData, ImageStatistics)
│   └── Services/                    # Services (FitsReader, StatisticsCalculator)
├── AstroImageAnalyzer.UI/          # WPF user interface
│   ├── ViewModels/                  # MVVM view models
│   ├── Converters/                  # Value converters
│   └── Themes/                      # Dark theme resources
└── AstroImageAnalyzer.Tests/        # Unit tests
    └── Services/                    # Test classes
```

## Requirements

- .NET 8.0 SDK
- Windows (for WPF)

## Dependencies

- **FITSReader** (v1.4.0): For reading FITS files
- **OxyPlot.Wpf** (v2.2.0): For charting and visualization

## Usage

1. Build the solution:
   ```bash
   dotnet build AstroImageAnalyzer.sln
   ```

2. Run the application:
   ```bash
   dotnet run --project AstroImageAnalyzer.UI/AstroImageAnalyzer.UI.csproj
   ```

3. Use **File → Open** (or Ctrl+O) to select one or more image files
4. Select a file from the list to view its statistics and histogram
5. Use "Clear All" to remove all loaded files

## Running Tests

```bash
dotnet test AstroImageAnalyzer.sln
```

## Architecture

The application follows a clean architecture pattern:

- **Core Library**: Contains all business logic, independent of UI
- **UI Project**: WPF application using MVVM pattern
- **Test Project**: Unit tests for core functionality

## Supported Formats and Data Types

**Currently supported:** FITS (Flexible Image Transport System).  
FITS data types: single-precision float (32-bit), byte (8-bit unsigned). Additional formats and data types will be added; the core design supports extending via reader services.

## License

This project is provided as-is for educational and research purposes.
