using AstroImageAnalyzer.Core.Models;

namespace AstroImageAnalyzer.Core.Services;

/// <summary>
/// Interface for calculating image statistics
/// </summary>
public interface IStatisticsCalculator
{
    /// <summary>
    /// Calculates statistics for a FITS image
    /// </summary>
    /// <param name="imageData">The image data to analyze</param>
    /// <param name="histogramBins">Number of bins for histogram calculation (default: 256)</param>
    /// <returns>ImageStatistics with calculated values</returns>
    ImageStatistics CalculateStatistics(FitsImageData imageData, int histogramBins = 256);
    
    /// <summary>
    /// Calculates statistics for multiple images
    /// </summary>
    /// <param name="images">Collection of image data</param>
    /// <param name="histogramBins">Number of bins for histogram calculation</param>
    /// <returns>Collection of statistics for each image</returns>
    IEnumerable<ImageStatistics> CalculateStatistics(IEnumerable<FitsImageData> images, int histogramBins = 256);
}
