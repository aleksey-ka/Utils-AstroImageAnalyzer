using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AstroImageAnalyzer.Core.Services;

/// <summary>
/// Bilinear debayering for CFA (Bayer pattern) FITS images.
/// Produces BGR24 byte data suitable for WPF BitmapSource.
/// </summary>
public static class DebayerService
{
    /// <summary>
    /// Debayers raw CFA pixel data to BGR24 (3 bytes per pixel: Blue, Green, Red).
    /// Uses STF range (min/max) for normalization, then gray-world channel balance to avoid green cast from 2:1 green in Bayer.
    /// </summary>
    /// <param name="pixelData">Raw 2D pixel data (height, width)</param>
    /// <param name="pattern">Bayer pattern: RGGB, BGGR, GRBG, or GBRG</param>
    /// <param name="min">STF minimum (black point)</param>
    /// <param name="max">STF maximum (white point)</param>
    /// <returns>BGR24 byte array, length width*height*3, stride = width*3</returns>
    public static byte[] DebayerBilinear(double[,] pixelData, string pattern, double min, double max)
    {
        int height = pixelData.GetLength(0);
        int width = pixelData.GetLength(1);
        double range = max > min ? max - min : 1.0;

        // Precompute pattern index and channel lookup (0=R, 1=G, 2=B) for (r,c) in 2x2
        int pi = PatternIndex(pattern);
        byte[] ch = new byte[4];
        ch[0] = (byte)ChannelAt(pi, 0, 0);
        ch[1] = (byte)ChannelAt(pi, 0, 1);
        ch[2] = (byte)ChannelAt(pi, 1, 0);
        ch[3] = (byte)ChannelAt(pi, 1, 1);

        var R = new double[height, width];
        var G = new double[height, width];
        var B = new double[height, width];

        // Phase 1: copy known channel values from raw (parallel over rows)
        Parallel.For(0, height, y =>
        {
            int ry = y & 1;
            for (int x = 0; x < width; x++)
            {
                int rx = x & 1;
                int c = ch[(ry << 1) + rx];
                double v = pixelData[y, x];
                if (double.IsNaN(v) || double.IsInfinity(v))
                    v = min;
                if (c == 0) R[y, x] = v;
                else if (c == 1) G[y, x] = v;
                else B[y, x] = v;
            }
        });

        // Phase 2: interpolate missing channels (3x3 neighborhood, parallel over rows)
        Parallel.For(0, height, y =>
        {
            int ry = y & 1;
            for (int x = 0; x < width; x++)
            {
                int rx = x & 1;
                int cur = ch[(ry << 1) + rx];
                if (cur != 0) R[y, x] = Interp3x3(R, width, height, y, x, pi, 0);
                if (cur != 1) G[y, x] = Interp3x3(G, width, height, y, x, pi, 1);
                if (cur != 2) B[y, x] = Interp3x3(B, width, height, y, x, pi, 2);
            }
        });

        // Phase 3: compute channel means after STF normalization (for gray-world balance)
        long sumR = 0, sumG = 0, sumB = 0;
        int n = width * height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double r = Math.Clamp((R[y, x] - min) / range, 0, 1);
                double g = Math.Clamp((G[y, x] - min) / range, 0, 1);
                double b = Math.Clamp((B[y, x] - min) / range, 0, 1);
                sumR += (long)(r * 1_000_000);
                sumG += (long)(g * 1_000_000);
                sumB += (long)(b * 1_000_000);
            }
        }
        double meanR = sumR / 1_000_000.0 / n;
        double meanG = sumG / 1_000_000.0 / n;
        double meanB = sumB / 1_000_000.0 / n;
        double targetMean = (meanR + meanG + meanB) / 3.0;
        double scaleR = targetMean > 1e-6 && meanR > 1e-6 ? targetMean / meanR : 1.0;
        double scaleG = targetMean > 1e-6 && meanG > 1e-6 ? targetMean / meanG : 1.0;
        double scaleB = targetMean > 1e-6 && meanB > 1e-6 ? targetMean / meanB : 1.0;

        // Phase 4: normalize with STF, apply gray-world balance, write BGR (parallel over rows)
        var bgr = new byte[width * height * 3];
        Parallel.For(0, height, y =>
        {
            int rowStart = y * width * 3;
            for (int x = 0; x < width; x++)
            {
                int idx = rowStart + x * 3;
                double r = Math.Clamp((R[y, x] - min) / range, 0, 1) * scaleR;
                double g = Math.Clamp((G[y, x] - min) / range, 0, 1) * scaleG;
                double b = Math.Clamp((B[y, x] - min) / range, 0, 1) * scaleB;
                bgr[idx] = (byte)Math.Clamp(b * 255, 0, 255);
                bgr[idx + 1] = (byte)Math.Clamp(g * 255, 0, 255);
                bgr[idx + 2] = (byte)Math.Clamp(r * 255, 0, 255);
            }
        });
        return bgr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Interp3x3(double[,] channel, int width, int height, int y, int x, int patternIndex, int targetChannel)
    {
        double sum = 0;
        int count = 0;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dy == 0 && dx == 0) continue;
                int ny = y + dy;
                int nx = x + dx;
                if ((uint)nx < (uint)width && (uint)ny < (uint)height && ChannelAt(patternIndex, ny & 1, nx & 1) == targetChannel)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int PatternIndex(string pat)
    {
        return pat switch
        {
            "RGGB" => 0,
            "BGGR" => 1,
            "GRBG" => 2,
            "GBRG" => 3,
            _ => 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ChannelAt(int patternIndex, int r, int c)
    {
        return (patternIndex, r, c) switch
        {
            (0, 0, 0) => 0, (0, 0, 1) => 1, (0, 1, 0) => 1, (0, 1, 1) => 2, // RGGB
            (1, 0, 0) => 2, (1, 0, 1) => 1, (1, 1, 0) => 1, (1, 1, 1) => 0, // BGGR
            (2, 0, 0) => 1, (2, 0, 1) => 0, (2, 1, 0) => 2, (2, 1, 1) => 1, // GRBG
            (3, 0, 0) => 1, (3, 0, 1) => 2, (3, 1, 0) => 0, (3, 1, 1) => 1, // GBRG
            _ => 1
        };
    }
}
