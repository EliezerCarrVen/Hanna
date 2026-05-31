using Hanna.Core;

namespace Hanna.Services;

internal sealed class OverlayNotificationService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService? runtime;

    public OverlayNotificationService(AppConfig config, RuntimeSettingsService? runtime = null)
    {
        this.config = config;
        this.runtime = runtime;
    }

    public Task ShowAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        if (!(runtime?.Snapshot().OverlayEnabled ?? config.OverlayEnabled))
            return Task.CompletedTask;

        return Task.Run(() => Show(title, message), cancellationToken);
    }

    private void Show(string title, string message)
    {
        try
        {
            string ps1 = Path.Combine(Path.GetTempPath(), $"hanna_overlay_{Guid.NewGuid():N}.ps1");
            string safeTitle = EscapePowerShell(title);
            string safeMessage = EscapePowerShell(TrimForOverlay(message));
            int seconds = Math.Clamp(runtime?.Snapshot().OverlaySeconds ?? config.OverlaySeconds, 3, 60);

            string script = $$"""
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$window = New-Object System.Windows.Window
$window.Title = 'Hanna'
$window.Width = 420
$window.Height = 190
$window.WindowStyle = 'None'
$window.ResizeMode = 'NoResize'
$window.Topmost = $true
$window.ShowInTaskbar = $false
$window.Background = [System.Windows.Media.Brushes]::Transparent
$window.AllowsTransparency = $true
$window.Opacity = 0.94

$border = New-Object System.Windows.Controls.Border
$border.CornerRadius = 14
$border.Padding = 14
$border.Background = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromArgb(235, 20, 20, 24))
$border.BorderBrush = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromArgb(180, 80, 130, 255))
$border.BorderThickness = 1

$stack = New-Object System.Windows.Controls.StackPanel

$title = New-Object System.Windows.Controls.TextBlock
$title.Text = '{{safeTitle}}'
$title.Foreground = [System.Windows.Media.Brushes]::White
$title.FontWeight = 'Bold'
$title.FontSize = 15
$title.Margin = '0,0,0,8'

$msg = New-Object System.Windows.Controls.TextBlock
$msg.Text = '{{safeMessage}}'
$msg.Foreground = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(232,232,236))
$msg.FontSize = 13
$msg.TextWrapping = 'Wrap'

$stack.Children.Add($title) | Out-Null
$stack.Children.Add($msg) | Out-Null
$border.Child = $stack
$window.Content = $border

$area = [System.Windows.SystemParameters]::WorkArea
$window.Left = $area.Right - $window.Width - 18
$window.Top = $area.Bottom - $window.Height - 18

$timer = New-Object System.Windows.Threading.DispatcherTimer
$timer.Interval = [TimeSpan]::FromSeconds({{seconds}})
$timer.Add_Tick({ $timer.Stop(); $window.Close() })
$timer.Start()

$window.ShowDialog() | Out-Null
""";

            File.WriteAllText(ps1, script, Encoding.UTF8);

            var psi = new ProcessStartInfo("powershell")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-ExecutionPolicy");
            psi.ArgumentList.Add("Bypass");
            psi.ArgumentList.Add("-File");
            psi.ArgumentList.Add(ps1);

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Overlay Error]: " + ex.Message);
        }
    }

    private static string EscapePowerShell(string text)
    {
        return (text ?? "").Replace("'", "''").Replace("`", "");
    }

    private static string TrimForOverlay(string text)
    {
        text = Regex.Replace(text ?? "", @"```[\s\S]*?```", "[Código generado: revisa el archivo de salida]");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= 900 ? text : text[..900] + "...";
    }
}
