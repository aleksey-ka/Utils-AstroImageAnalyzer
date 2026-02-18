namespace AstroImageAnalyzer.Core.Services;

/// <summary>
/// Bilinear debayering for CFA (Bayer pattern) FITS images.
/// Produces BGR24 byte data suitable for WPF BitmapSource.
/// </summary>
public static class DebayerService
{
    /// <summary>
    /// Debayers raw CFA pixel data to BGR24 (3 bytes per pixel: Blue, Green, Red).
    /// </summary>
    /// <param name="pixelData">Raw 2D pixel data (height, width)</param>
    /// <param name="pattern">Bayer pattern: RGGB, BGGR, GRBG, or GBRG</param>
    /// <param name="min">Minimum value for normalization (e.g. image min)</param>
    /// <param name="max">Maximum value for normalization (e.g. image max)</param>
    /// <returns>BGR24 byte array, length width*height*3, stride = width*3</returns>
    public static byte[] DebayerBilinear(double[,] pixelData, string pattern, double min, double max)
    {
        int height = pixelData.GetLength(0);
        int width = pixelData.GetLength(1);
        double range = max > min ? max - min : 1.0;

        var R = new double[height, width];
        var G = new double[height, width];
        var B = new double[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double v = pixelData[y, x];
                if (double.IsNaN(v) || double.IsInfinity(v))
                    v = min;
                int ch = ChannelAt(pattern, y & 1, x & 1);
                if (ch == 0) R[y, x] = v;
                else if (ch == 1) G[y, x] = v;
                else B[y, x] = v;
            }
        }

        // Bilinear interpolate missing channels (only from same-channel Bayer positions)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (ChannelAt(pattern, y & 1, x & 1) != 0) R[y, x] = InterpBilinearSameChannel(R, width, height, pattern, x, y, 0);
                if (ChannelAt(pattern, y & 1, x & 1) != 1) G[y, x] = InterpBilinearSameChannel(G, width, height, pattern, x, y, 1);
                if (ChannelAt(pattern, y & 1, x & 1) != 2) B[y, x] = InterpBilinearSameChannel(B, width, height, pattern, x, y, 2);
            }
        }

        var bgr = new byte[width * height * 3];
        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double r = (R[y, x] - min) / range;
                double g = (G[y, x] - min) / range;
                double b = (B[y, x] - min) / range;
                bgr[idx++] = (byte)Math.Clamp(b * 255, 0, 255);
                bgr[idx++] = (byte)Math.Clamp(g * 255, 0, 255);
                bgr[idx++] = (byte)Math.Clamp(r * 255, 0, 255);
            }
        }
        return bgr;
    }

    private static double InterpBilinearSameChannel(double[,] channel, int width, int height, string pattern, int x, int y, int targetChannel)
    {
        double sum = 0;
        int count = 0;
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && ChannelAt(pattern, ny & 1, nx & 1) == targetChannel)
                {
                    double v = channel[ny, nx];
                    if (!double.IsNaN(v) && !double.IsInfinity(v))
                    {
                        sum += v;
                        count++;
                    }
                }
            }
        }
        return count > 0 ? sum / count : 0;
    }

    // Channel at (y % 2, x % 2) for each pattern (row, col) -> 0=R, 1=G, 2=B
    private static int ChannelAt(string pat, int r, int c)
    {
        return (pat, r, c) switch
        {
            ("RGGB", 0, 0) => 0, ("RGGB", 0, 1) => 1, ("RGGB", 1, 0) => 1, ("RGGB", 1, 1) => 2,
            ("BGGR", 0, 0) => 2, ("BGGR", 0, 1) => 1, ("BGGR", 1, 0) => 1, ("BGGR", 1, 1) => 0,
            ("GRBG", 0, 0) => 1, ("GRBG", 0, 1) => 0, ("GRBG", 1, 0) => 2, ("GRBG", 1, 1) => 1,
            ("GBRG", 0, 0) => 1, ("GBRG", 0, 1) => 2, ("GBRG", 1, 0) => 0, ("GBRG", 1, 1) => 1,
            _ => 1
        };
    }
}
