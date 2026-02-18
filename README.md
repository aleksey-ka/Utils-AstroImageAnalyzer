# FITS Image Analyzer

A .NET 8 WPF application for analyzing astronomical FITS (Flexible Image Transport System) files with statistical measurements and visualizations.

## Features

- **FITS File Reading**: Load and parse FITS files using the FITSReader library
- **Image Statistics**: Calculate comprehensive statistics including:
  - Minimum, Maximum, Mean, Median
  - Standard Deviation and Variance
  - Pixel count and sum
  - Histogram distribution
- **Visualization**: 
  - Dark theme UI optimized for astronomical image viewing
  - Histogram charts showing pixel value distribution
  - Image statistics panel with detailed measurements
- **Multi-file Support**: Load and analyze multiple FITS files simultaneously

## Project Structure

```
FitsImageAnalyzer/
├── FitsImageAnalyzer.Core/          # Core library with business logic
│   ├── Models/                      # Data models (FitsImageData, ImageStatistics)
│   └── Services/                    # Services (FitsReader, StatisticsCalculator)
├── FitsImageAnalyzer.UI/            # WPF user interface
│   ├── ViewModels/                  # MVVM view models
│   ├── Converters/                  # Value converters
│   └── Themes/                      # Dark theme resources
└── FitsImageAnalyzer.Tests/         # Unit tests
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
   dotnet build FitsImageAnalyzer.sln
   ```

2. Run the application:
   ```bash
   dotnet run --project FitsImageAnalyzer.UI/FitsImageAnalyzer.UI.csproj
   ```

3. Click "Load FITS Files" to select one or more FITS files
4. Select a file from the list to view its statistics and histogram
5. Use "Clear All" to remove all loaded files

## Running Tests

```bash
dotnet test FitsImageAnalyzer.sln
```

## Architecture

The application follows a clean architecture pattern:

- **Core Library**: Contains all business logic, independent of UI
- **UI Project**: WPF application using MVVM pattern
- **Test Project**: Unit tests for core functionality

## Supported FITS Data Types

Currently supports:
- Single-precision floating point (32-bit float)
- Byte (8-bit unsigned integer)

Additional data types can be added by extending the `FitsReader` service.

## License

This project is provided as-is for educational and research purposes.
