using AstroImageAnalyzer.Core.Models;

namespace AstroImageAnalyzer.Core.Services;

/// <summary>
/// Service for calculating statistical measurements on astronomical images
/// </summary>
public class StatisticsCalculator : IStatisticsCalculator
{
    public ImageStatistics CalculateStatistics(FitsImageData imageData, int histogramBins = 256)
    {
        if (imageData == null)
            throw new ArgumentNullException(nameof(imageData));
        
        var pixels = FlattenPixelData(imageData.PixelData);
        
        if (pixels.Length == 0)
            throw new InvalidOperationException("Image has no pixel data");
        
        var stats = new ImageStatistics
        {
            Minimum = pixels.Min(),
            Maximum = pixels.Max(),
            Mean = pixels.Average(),
            Sum = pixels.Sum(),
            PixelCount = pixels.Length,
            FilePath = imageData.FilePath
        };
        
        // Calculate variance and standard deviation
        var variance = pixels.Select(p => Math.Pow(p - stats.Mean, 2)).Average();
        stats.Variance = variance;
        stats.StandardDeviation = Math.Sqrt(variance);
        
        // Calculate median
        var sortedPixels = pixels.OrderBy(p => p).ToArray();
        int mid = sortedPixels.Length / 2;
        stats.Median = sortedPixels.Length % 2 == 0
            ? (sortedPixels[mid - 1] + sortedPixels[mid]) / 2.0
            : sortedPixels[mid];
        
        // Calculate histogram
        stats.Histogram = CalculateHistogram(pixels, histogramBins, stats.Minimum, stats.Maximum);
        
        return stats;
    }
    
    public IEnumerable<ImageStatistics> CalculateStatistics(IEnumerable<FitsImageData> images, int histogramBins = 256)
    {
        foreach (var image in images)
        {
            yield return CalculateStatistics(image, histogramBins);
        }
    }
    
    private static double[] FlattenPixelData(double[,] pixelData)
    {
        int height = pixelData.GetLength(0);
        int width = pixelData.GetLength(1);
        var result = new double[height * width];
        
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[index++] = pixelData[y, x];
            }
        }
        
        return result;
    }
    
    private static Dictionary<double, int> CalculateHistogram(double[] pixels, int bins, double min, double max)
    {
        var histogram = new Dictionary<double, int>();
        
        if (max == min)
        {
            histogram[min] = pixels.Length;
            return histogram;
        }
        
        double binWidth = (max - min) / bins;
        var binCounts = new int[bins];
        
        foreach (var pixel in pixels)
        {
            int binIndex = Math.Min((int)((pixel - min) / binWidth), bins - 1);
            binCounts[binIndex]++;
        }
        
        for (int i = 0; i < bins; i++)
        {
            double binCenter = min + (i + 0.5) * binWidth;
            histogram[binCenter] = binCounts[i];
        }
        
        return histogram;
    }
}
