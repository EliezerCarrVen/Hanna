using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Hanna.Core;

namespace Hanna.Services;

internal sealed class ScreenCaptureService
{
    private readonly AppConfig config;

    public ScreenCaptureService(AppConfig config)
    {
        this.config = config;
    }

    public string CapturePrimaryScreenToBase64()
    {
        string path = CapturePrimaryScreenToFile();
        byte[] bytes = File.ReadAllBytes(path);
        return Convert.ToBase64String(bytes);
    }

    public string CapturePrimaryScreenToFile()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("La captura de pantalla local está implementada para Windows.");

        int left = GetSystemMetrics(76);
        int top = GetSystemMetrics(77);
        int width = GetSystemMetrics(78);
        int height = GetSystemMetrics(79);

        if (width <= 0 || height <= 0)
        {
            left = 0;
            top = 0;
            width = GetSystemMetrics(0);
            height = GetSystemMetrics(1);
        }

        IntPtr desktop = GetDC(IntPtr.Zero);
        IntPtr memory = CreateCompatibleDC(desktop);
        IntPtr bitmap = CreateCompatibleBitmap(desktop, width, height);
        IntPtr oldBitmap = SelectObject(memory, bitmap);

        try
        {
            const int SRCCOPY = 0x00CC0020;
            if (!BitBlt(memory, 0, 0, width, height, desktop, left, top, SRCCOPY))
                throw new InvalidOperationException("BitBlt falló capturando pantalla.");

            using Bitmap bmp = Bitmap.FromHbitmap(bitmap);
            string folder = Path.Combine(config.AgentOutputDirectory, "pantallas");
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, $"screen_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            bmp.Save(path, ImageFormat.Jpeg);
            return path;
        }
        finally
        {
            SelectObject(memory, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(memory);
            ReleaseDC(IntPtr.Zero, desktop);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);
}
