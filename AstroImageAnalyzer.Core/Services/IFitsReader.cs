using AstroImageAnalyzer.Core.Models;

namespace AstroImageAnalyzer.Core.Services;

/// <summary>
/// Interface for reading FITS files
/// </summary>
public interface IFitsReader
{
    /// <summary>
    /// Reads a FITS file and returns the image data
    /// </summary>
    /// <param name="filePath">Path to the FITS file</param>
    /// <returns>FitsImageData containing pixel data and metadata</returns>
    FitsImageData ReadFitsFile(string filePath);
    
    /// <summary>
    /// Reads multiple FITS files
    /// </summary>
    /// <param name="filePaths">Paths to FITS files</param>
    /// <returns>Collection of FitsImageData</returns>
    IEnumerable<FitsImageData> ReadFitsFiles(IEnumerable<string> filePaths);
}
