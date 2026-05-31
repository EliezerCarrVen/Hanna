using OpenCvSharp;

namespace Hanna.Services;

internal sealed class WebcamLedService : IDisposable
{
    private readonly object sync = new();
    private readonly int cameraIndex;
    private VideoCapture? capture;
    private CancellationTokenSource? loopCts;
    private Task? loopTask;
    private bool disposed;

    public bool AutoIndicatorEnabled { get; private set; }
    public bool IsCameraOpen => capture != null;

    public WebcamLedService(int cameraIndex, bool autoIndicatorEnabled = false)
    {
        this.cameraIndex = cameraIndex < 0 ? 0 : cameraIndex;
        AutoIndicatorEnabled = autoIndicatorEnabled;
    }

    public string TurnOn()
    {
        lock (sync)
        {
            if (disposed)
                return "El servicio de cámara ya está cerrado.";

            if (capture != null)
                return "La cámara ya está activa.";

            try
            {
                capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);

                if (!capture.IsOpened())
                {
                    capture.Release();
                    capture.Dispose();
                    capture = null;
                    return $"No pude abrir la cámara índice {cameraIndex}.";
                }

                loopCts = new CancellationTokenSource();
                CancellationToken token = loopCts.Token;
                loopTask = Task.Run(() => CaptureLoop(token), token);

                return "Cámara activada como indicador.";
            }
            catch (Exception ex)
            {
                SafeRelease();
                return $"No pude activar la cámara: {ex.Message}";
            }
        }
    }

    public string TurnOff()
    {
        lock (sync)
        {
            if (capture == null)
                return "La cámara ya está apagada.";

            SafeRelease();
            return "Cámara desactivada.";
        }
    }

    public string SetAutoIndicator(bool enabled)
    {
        AutoIndicatorEnabled = enabled;

        if (!enabled)
            TurnOff();

        return enabled
            ? "Indicador de cámara activado para comandos de voz. Se encenderá mientras Hanna escucha."
            : "Indicador de cámara desactivado para comandos de voz.";
    }

    public string Toggle()
    {
        return IsCameraOpen ? TurnOff() : TurnOn();
    }

    public void TurnOnForListeningIfEnabled()
    {
        if (AutoIndicatorEnabled)
            Console.WriteLine("[Cámara] " + TurnOn());
    }

    public void TurnOffAfterListeningIfEnabled()
    {
        if (AutoIndicatorEnabled)
            Console.WriteLine("[Cámara] " + TurnOff());
    }

    private void CaptureLoop(CancellationToken token)
    {
        try
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested)
            {
                lock (sync)
                {
                    if (capture == null)
                        return;

                    try
                    {
                        capture.Read(frame);
                    }
                    catch
                    {
                        return;
                    }
                }

                Thread.Sleep(120);
            }
        }
        catch
        {
        }
    }

    private void SafeRelease()
    {
        try
        {
            loopCts?.Cancel();
        }
        catch
        {
        }

        try
        {
            if (loopTask != null && !loopTask.IsCompleted)
                loopTask.Wait(500);
        }
        catch
        {
        }

        try
        {
            capture?.Release();
            capture?.Dispose();
        }
        catch
        {
        }

        try
        {
            loopCts?.Dispose();
        }
        catch
        {
        }

        capture = null;
        loopCts = null;
        loopTask = null;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        TurnOff();
    }
}
