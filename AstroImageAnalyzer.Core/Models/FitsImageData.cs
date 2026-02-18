namespace AstroImageAnalyzer.Core.Models;

/// <summary>
/// Represents image data loaded from a FITS file
/// </summary>
public class FitsImageData
{
    /// <summary>
    /// Raw pixel data as a 2D array
    /// </summary>
    public double[,] PixelData { get; set; } = new double[0, 0];
    
    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// FITS header metadata
    /// </summary>
    public Dictionary<string, string> Header { get; set; } = new();
    
    /// <summary>
    /// File path of the source FITS file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// File name without path
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Bayer/CFA pattern from FITS header (e.g. RGGB, BGGR, GRBG, GBRG).
    /// Null when the image is not a color filter array.
    /// </summary>
    public string? BayerPattern { get; set; }
}
