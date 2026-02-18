using AstroImageAnalyzer.Core.Models;
using System.Globalization;

namespace AstroImageAnalyzer.Core.Services;

/// <summary>
/// Service for reading FITS files using a minimal built-in parser
/// </summary>
public class FitsReader : IFitsReader
{
    public FitsImageData ReadFitsFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"FITS file not found: {filePath}", filePath);
        
        // Use our own minimal FITS parser for primary 2D images
        return ReadPrimaryImage(filePath);
    }
    
    public IEnumerable<FitsImageData> ReadFitsFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            yield return ReadFitsFile(filePath);
        }
    }
    
    private static FitsImageData ReadPrimaryImage(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        // --- Read header cards (80-byte records) until we hit END ---
        var headerDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var headerBytes = new List<byte>();
        var cardBuffer = new byte[80];

        while (true)
        {
            int read = fs.Read(cardBuffer, 0, cardBuffer.Length);
            if (read < cardBuffer.Length)
                throw new InvalidOperationException("Unexpected end of file while reading FITS header.");

            headerBytes.AddRange(cardBuffer);
            var card = System.Text.Encoding.ASCII.GetString(cardBuffer);
            var keyword = card.Substring(0, 8).Trim();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string value = string.Empty;
                if (card.Length >= 30 && card[8] == '=' && card[9] == ' ')
                {
                    // value field starts at column 11 (index 10)
                    var valueComment = card.Substring(10).Trim();
                    var slashIndex = valueComment.IndexOf('/');
                    if (slashIndex >= 0)
                        value = valueComment.Substring(0, slashIndex).Trim();
                    else
                        value = valueComment.Trim();
                }

                headerDict[keyword] = value;
            }

            if (keyword == "END")
                break;
        }

        // Header is padded to 2880-byte boundary
        long headerSize = headerBytes.Count;
        long padding = (2880 - (headerSize % 2880)) % 2880;
        if (padding > 0)
            fs.Seek(padding, SeekOrigin.Current);

        // Parse important header values
        int bitpix = ParseIntHeader(headerDict, "BITPIX");
        int naxis = ParseIntHeader(headerDict, "NAXIS");
        int width = ParseIntHeader(headerDict, "NAXIS1");
        int height = naxis >= 2 ? ParseIntHeader(headerDict, "NAXIS2") : 1;

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException($"Invalid FITS dimensions: {width}x{height}");

        // Optional scaling
        double bscale = TryParseDoubleHeader(headerDict, "BSCALE") ?? 1.0;
        double bzero = TryParseDoubleHeader(headerDict, "BZERO") ?? 0.0;

        var pixelData = new double[height, width];

        switch (bitpix)
        {
            case 16: // 16-bit signed integer
                ReadInt16Image(fs, pixelData, width, height, bscale, bzero);
                break;
            case 8: // 8-bit unsigned integer
                ReadByteImage(fs, pixelData, width, height, bscale, bzero);
                break;
            case 32: // 32-bit signed integer
                ReadInt32Image(fs, pixelData, width, height, bscale, bzero);
                break;
            case -32: // 32-bit floating point
                ReadFloatImage(fs, pixelData, width, height, bscale, bzero);
                break;
            case -64: // 64-bit floating point
                ReadDoubleImage(fs, pixelData, width, height, bscale, bzero);
                break;
            default:
                throw new NotSupportedException($"Unsupported BITPIX value: {bitpix}");
        }

        // Normalize header keys we know
        headerDict["NAXIS1"] = width.ToString(CultureInfo.InvariantCulture);
        headerDict["NAXIS2"] = height.ToString(CultureInfo.InvariantCulture);
        headerDict["BITPIX"] = bitpix.ToString(CultureInfo.InvariantCulture);

        return new FitsImageData
        {
            PixelData = pixelData,
            Width = width,
            Height = height,
            Header = headerDict,
            FilePath = filePath
        };
    }

    private static int ParseIntHeader(Dictionary<string, string> header, string key)
    {
        if (!header.TryGetValue(key, out var valueStr))
            return 0;

        valueStr = valueStr.Trim().Trim('\'');
        return int.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static double? TryParseDoubleHeader(Dictionary<string, string> header, string key)
    {
        if (!header.TryGetValue(key, out var valueStr))
            return null;

        valueStr = valueStr.Trim().Trim('\'');
        return double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static void ReadInt16Image(Stream fs, double[,] pixelData, int width, int height, double bscale, double bzero)
    {
        var buffer = new byte[2];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int read = fs.Read(buffer, 0, 2);
                if (read < 2)
                    throw new InvalidOperationException("Unexpected end of file while reading Int16 image data.");

                short raw = (short)((buffer[0] << 8) | buffer[1]);
                double scaled = bscale * raw + bzero;
                pixelData[y, x] = scaled;
            }
        }
    }

    private static void ReadByteImage(Stream fs, double[,] pixelData, int width, int height, double bscale, double bzero)
    {
        var buffer = new byte[1];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int read = fs.Read(buffer, 0, 1);
                if (read < 1)
                    throw new InvalidOperationException("Unexpected end of file while reading byte image data.");

                byte raw = buffer[0];
                double scaled = bscale * raw + bzero;
                pixelData[y, x] = scaled;
            }
        }
    }

    private static void ReadInt32Image(Stream fs, double[,] pixelData, int width, int height, double bscale, double bzero)
    {
        var buffer = new byte[4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int read = fs.Read(buffer, 0, 4);
                if (read < 4)
                    throw new InvalidOperationException("Unexpected end of file while reading Int32 image data.");

                int raw = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
                double scaled = bscale * raw + bzero;
                pixelData[y, x] = scaled;
            }
        }
    }

    private static void ReadFloatImage(Stream fs, double[,] pixelData, int width, int height, double bscale, double bzero)
    {
        var buffer = new byte[4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int read = fs.Read(buffer, 0, 4);
                if (read < 4)
                    throw new InvalidOperationException("Unexpected end of file while reading float image data.");

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                float raw = BitConverter.ToSingle(buffer, 0);
                double scaled = bscale * raw + bzero;
                pixelData[y, x] = scaled;
            }
        }
    }

    private static void ReadDoubleImage(Stream fs, double[,] pixelData, int width, int height, double bscale, double bzero)
    {
        var buffer = new byte[8];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int read = fs.Read(buffer, 0, 8);
                if (read < 8)
                    throw new InvalidOperationException("Unexpected end of file while reading double image data.");

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                double raw = BitConverter.ToDouble(buffer, 0);
                double scaled = bscale * raw + bzero;
                pixelData[y, x] = scaled;
            }
        }
    }
}
