namespace AstroImageAnalyzer.Core.Models;

/// <summary>
/// Statistical measurements for an astronomical image
/// </summary>
public class ImageStatistics
{
    /// <summary>
    /// Minimum pixel value
    /// </summary>
    public double Minimum { get; set; }
    
    /// <summary>
    /// Maximum pixel value
    /// </summary>
    public double Maximum { get; set; }
    
    /// <summary>
    /// Mean (average) pixel value
    /// </summary>
    public double Mean { get; set; }
    
    /// <summary>
    /// Median pixel value
    /// </summary>
    public double Median { get; set; }
    
    /// <summary>
    /// Standard deviation of pixel values
    /// </summary>
    public double StandardDeviation { get; set; }
    
    /// <summary>
    /// Variance of pixel values
    /// </summary>
    public double Variance { get; set; }
    
    /// <summary>
    /// Total sum of all pixel values
    /// </summary>
    public double Sum { get; set; }
    
    /// <summary>
    /// Total number of pixels
    /// </summary>
    public int PixelCount { get; set; }
    
    /// <summary>
    /// Histogram data (value -> count)
    /// </summary>
    public Dictionary<double, int> Histogram { get; set; } = new();
    
    /// <summary>
    /// Source file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
