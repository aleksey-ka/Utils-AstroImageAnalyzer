using System.Reflection;
using AstroImageAnalyzer.Core.Services;

namespace AstroImageAnalyzer.Tests.Services;

public class FitsReaderTests
{
    private readonly IFitsReader _reader;
    
    public FitsReaderTests()
    {
        _reader = new FitsReader();
    }
    
    [Fact]
    public void ReadFitsFile_WithRealInt16Fits_DoesNotThrow_WhenTestDataPresent()
    {
        // Arrange - locate TestData directory relative to the test assembly
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        
        // Navigate from bin/Debug/net8.0 back to project root, then to TestData
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyDirectory!, "..", "..", "..", ".."));
        var testDataPath = Path.Combine(projectRoot, "AstroImageAnalyzer.Tests", "TestData", "01.fits");

        if (!File.Exists(testDataPath))
        {
            // If the file is not present, fail the test with a helpful message
            Assert.Fail($"Test data file not found at: {testDataPath}. Please ensure 01.fits exists in AstroImageAnalyzer.Tests\\TestData");
        }

        // Act
        var image = _reader.ReadFitsFile(testDataPath);

        // Assert
        Assert.NotNull(image);
        Assert.True(image.Width > 0, $"Expected width > 0, got {image.Width}");
        Assert.True(image.Height > 0, $"Expected height > 0, got {image.Height}");
        Assert.NotNull(image.PixelData);
        Assert.Equal(image.Width * image.Height, image.PixelData.Length);
    }
    [Fact]
    public void ReadFitsFile_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _reader.ReadFitsFile(null!));
        Assert.Throws<ArgumentException>(() => _reader.ReadFitsFile(string.Empty));
        Assert.Throws<ArgumentException>(() => _reader.ReadFitsFile("   "));
    }
    
    [Fact]
    public void ReadFitsFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _reader.ReadFitsFile("nonexistent.fits"));
    }
    
    [Fact]
    public void ReadFitsFiles_WithMultipleFiles_ReturnsAllImages()
    {
        // Note: This test requires actual FITS files to run properly
        // In a real scenario, you would use test fixtures or mock data
        
        // Arrange
        var filePaths = new[] { "test1.fits", "test2.fits" };
        
        // Act & Assert
        // This will throw FileNotFoundException if files don't exist, which is expected
        // In a production test suite, you would use test FITS files or mocks
        Assert.Throws<FileNotFoundException>(() => _reader.ReadFitsFiles(filePaths).ToList());
    }

    [Fact]
    public void ReadFitsFile_WithCfaFile02Fit_ReadsBayerPatternGrbg()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyDirectory!, "..", "..", "..", ".."));
        var testDataPath = Path.Combine(projectRoot, "AstroImageAnalyzer.Tests", "TestData", "02.fit");

        if (!File.Exists(testDataPath))
        {
            Assert.Fail($"Test data file not found at: {testDataPath}. Please ensure 02.fit exists in AstroImageAnalyzer.Tests\\TestData");
        }

        var image = _reader.ReadFitsFile(testDataPath);

        Assert.NotNull(image);
        Assert.True(image.Width > 0, $"Expected width > 0, got {image.Width}");
        Assert.True(image.Height > 0, $"Expected height > 0, got {image.Height}");
        Assert.NotNull(image.PixelData);
        Assert.Equal("GRBG", image.BayerPattern);
    }
}
