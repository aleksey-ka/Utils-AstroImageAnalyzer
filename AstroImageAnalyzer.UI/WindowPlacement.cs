using System.IO;
using System.Text.Json;
using System.Windows;

namespace AstroImageAnalyzer.UI;

/// <summary>
/// Saves and restores window position and size to the user's local app data.
/// </summary>
public static class WindowPlacement
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AstroImageAnalyzer");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "window.json");

    public record SavedState(double Left, double Top, double Width, double Height, int WindowState);

    public static void Save(Window window)
    {
        try
        {
            var state = window.WindowState == System.Windows.WindowState.Maximized
                ? new SavedState(
                    window.RestoreBounds.Left,
                    window.RestoreBounds.Top,
                    window.RestoreBounds.Width,
                    window.RestoreBounds.Height,
                    (int)System.Windows.WindowState.Maximized)
                : new SavedState(
                    window.Left,
                    window.Top,
                    window.Width,
                    window.Height,
                    (int)window.WindowState);

            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore persistence errors
        }
    }

    /// <summary>
    /// Saves explicit bounds as the normal (restored) window state.
    /// Used when closing in full screen so the app reopens at the same position/size.
    /// </summary>
    public static void SaveRestoreBounds(double left, double top, double width, double height)
    {
        try
        {
            var state = new SavedState(left, top, width, height, (int)System.Windows.WindowState.Normal);
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore persistence errors
        }
    }

    public static void Restore(Window window, double defaultWidth, double defaultHeight)
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;

            var json = File.ReadAllText(SettingsPath);
            var state = JsonSerializer.Deserialize<SavedState>(json);
            if (state == null)
                return;

            // Sanity check: ensure window fits on screen (e.g. after monitor change)
            var left = state.Left;
            var top = state.Top;
            var width = Math.Max(200, state.Width);
            var height = Math.Max(200, state.Height);

            window.Left = left;
            window.Top = top;
            window.Width = width;
            window.Height = height;
            window.WindowStartupLocation = WindowStartupLocation.Manual;

            if (state.WindowState == (int)System.Windows.WindowState.Maximized)
                window.WindowState = System.Windows.WindowState.Maximized;
        }
        catch
        {
            // Use default size and position
        }
    }
}
