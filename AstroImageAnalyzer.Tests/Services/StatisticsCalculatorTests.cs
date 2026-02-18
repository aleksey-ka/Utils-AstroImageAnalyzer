using AstroImageAnalyzer.Core.Models;
using AstroImageAnalyzer.Core.Services;

namespace AstroImageAnalyzer.Tests.Services;

public class StatisticsCalculatorTests
{
    private readonly IStatisticsCalculator _calculator;
    
    public StatisticsCalculatorTests()
    {
        _calculator = new StatisticsCalculator();
    }
    
    [Fact]
    public void CalculateStatistics_WithValidImageData_ReturnsCorrectStatistics()
    {
        // Arrange
        var imageData = new FitsImageData
        {
            PixelData = new double[,]
            {
                { 10.0, 20.0, 30.0 },
                { 40.0, 50.0, 60.0 },
                { 70.0, 80.0, 90.0 }
            },
            Width = 3,
            Height = 3,
            FilePath = "test.fits"
        };
        
        // Act
        var stats = _calculator.CalculateStatistics(imageData);
        
        // Assert
        Assert.NotNull(stats);
        Assert.Equal(10.0, stats.Minimum);
        Assert.Equal(90.0, stats.Maximum);
        Assert.Equal(50.0, stats.Mean);
        Assert.Equal(450.0, stats.Sum);
        Assert.Equal(9, stats.PixelCount);
        Assert.Equal("test.fits", stats.FilePath);
    }
    
    [Fact]
    public void CalculateStatistics_CalculatesCorrectStandardDeviation()
    {
        // Arrange
        var imageData = new FitsImageData
        {
            PixelData = new double[,]
            {
                { 10.0, 10.0 },
                { 10.0, 10.0 }
            },
            Width = 2,
            Height = 2,
            FilePath = "test.fits"
        };
        
        // Act
        var stats = _calculator.CalculateStatistics(imageData);
        
        // Assert
        Assert.Equal(0.0, stats.StandardDeviation, 5);
        Assert.Equal(0.0, stats.Variance, 5);
    }
    
    [Fact]
    public void CalculateStatistics_CalculatesCorrectMedian()
    {
        // Arrange
        var imageData = new FitsImageData
        {
            PixelData = new double[,]
            {
                { 10.0, 20.0, 30.0 }
            },
            Width = 3,
            Height = 1,
            FilePath = "test.fits"
        };
        
        // Act
        var stats = _calculator.CalculateStatistics(imageData);
        
        // Assert
        Assert.Equal(20.0, stats.Median);
    }
    
    [Fact]
    public void CalculateStatistics_GeneratesHistogram()
    {
        // Arrange
        var imageData = new FitsImageData
        {
            PixelData = new double[,]
            {
                { 0.0, 50.0, 100.0 }
            },
            Width = 3,
            Height = 1,
            FilePath = "test.fits"
        };
        
        // Act
        var stats = _calculator.CalculateStatistics(imageData, histogramBins: 10);
        
        // Assert
        Assert.NotNull(stats.Histogram);
        Assert.True(stats.Histogram.Count > 0);
    }
    
    [Fact]
    public void CalculateStatistics_WithNullImageData_ThrowsArgumentNullException()
    {
        // Act & Assert
        FitsImageData? nullData = null;
        Assert.Throws<ArgumentNullException>(() => _calculator.CalculateStatistics(nullData!));
    }
    
    [Fact]
    public void CalculateStatistics_WithEmptyImageData_ThrowsInvalidOperationException()
    {
        // Arrange
        var imageData = new FitsImageData
        {
            PixelData = new double[0, 0],
            Width = 0,
            Height = 0,
            FilePath = "test.fits"
        };
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _calculator.CalculateStatistics(imageData));
    }
    
    [Fact]
    public void CalculateStatistics_WithMultipleImages_ReturnsStatisticsForEach()
    {
        // Arrange
        var images = new[]
        {
            new FitsImageData
            {
                PixelData = new double[,] { { 10.0 } },
                Width = 1,
                Height = 1,
                FilePath = "test1.fits"
            },
            new FitsImageData
            {
                PixelData = new double[,] { { 20.0 } },
                Width = 1,
                Height = 1,
                FilePath = "test2.fits"
            }
        };
        
        // Act
        var statsList = new List<ImageStatistics>();
        foreach (var image in images)
        {
            statsList.Add(_calculator.CalculateStatistics(image));
        }
        var stats = statsList;
        
        // Assert
        Assert.Equal(2, stats.Count);
        Assert.Equal(10.0, stats[0].Mean);
        Assert.Equal(20.0, stats[1].Mean);
    }
}
