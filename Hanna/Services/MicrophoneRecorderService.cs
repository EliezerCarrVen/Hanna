using NAudio.Wave;

namespace Hanna.Services;

internal sealed class MicrophoneRecorderService : IDisposable
{
    private readonly object sync = new();
    private WaveInEvent? waveIn;
    private WaveFileWriter? writer;
    private string? currentFile;
    private bool disposed;

    public bool IsRecording { get; private set; }

    public async Task<string?> RecordUntilSilenceAsync(
        int silenceMs,
        int noVoiceTimeoutMs,
        int maxSeconds,
        int minSeconds,
        int voiceStartRms,
        int voiceStopRms,
        CancellationToken cancellationToken,
        bool recordImmediately = true,
        int initialGraceMs = 900)
    {
        if (IsRecording)
            throw new InvalidOperationException("Ya hay una grabación activa.");

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        DateTime startedAt = DateTime.UtcNow;
        DateTime lastVoiceAt = DateTime.UtcNow;
        bool voiceDetected = recordImmediately;

        string path = Start();
        if (recordImmediately)
            Console.WriteLine("[Voz local] Grabando inmediatamente.");

        waveIn!.DataAvailable += (_, e) =>
        {
            double rms = CalculateRms(e.Buffer, e.BytesRecorded);
            DateTime now = DateTime.UtcNow;
            double elapsed = (now - startedAt).TotalSeconds;

            if (!voiceDetected)
            {
                if (rms >= voiceStartRms)
                {
                    voiceDetected = true;
                    lastVoiceAt = now;
                    Console.WriteLine("[Voz local] Voz detectada.");
                }
                else if ((now - startedAt).TotalMilliseconds >= noVoiceTimeoutMs)
                {
                    tcs.TrySetResult(null);
                }
            }
            else
            {
                if (rms >= voiceStopRms)
                    lastVoiceAt = now;

                bool minReached = elapsed >= minSeconds;
                bool graceReached = (now - startedAt).TotalMilliseconds >= Math.Max(0, initialGraceMs);
                bool silenceReached = (now - lastVoiceAt).TotalMilliseconds >= silenceMs;

                if (minReached && graceReached && silenceReached)
                    tcs.TrySetResult(path);
            }

            if (elapsed >= maxSeconds)
                tcs.TrySetResult(path);
        };

        try
        {
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            string? result = await tcs.Task;
            return result;
        }
        finally
        {
            StopInternal();
        }
    }

    private string Start()
    {
        lock (sync)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(MicrophoneRecorderService));

            if (IsRecording)
                throw new InvalidOperationException("Ya se está grabando.");

            string folder = Path.Combine(Path.GetTempPath(), "HannaVoice");
            Directory.CreateDirectory(folder);

            currentFile = Path.Combine(folder, $"hanna_mic_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.wav");

            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 100
            };

            writer = new WaveFileWriter(currentFile, waveIn.WaveFormat);

            waveIn.DataAvailable += (_, e) =>
            {
                writer?.Write(e.Buffer, 0, e.BytesRecorded);
                writer?.Flush();
            };

            waveIn.StartRecording();
            IsRecording = true;

            return currentFile;
        }
    }

    private void StopInternal()
    {
        lock (sync)
        {
            if (!IsRecording)
                return;

            try
            {
                waveIn?.StopRecording();
            }
            catch
            {
            }

            try
            {
                waveIn?.Dispose();
                writer?.Dispose();
            }
            catch
            {
            }

            waveIn = null;
            writer = null;
            IsRecording = false;
        }
    }

    private static double CalculateRms(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded <= 1)
            return 0;

        long sumSquares = 0;
        int samples = bytesRecorded / 2;

        for (int i = 0; i < bytesRecorded - 1; i += 2)
        {
            short sample = BitConverter.ToInt16(buffer, i);
            sumSquares += (long)sample * sample;
        }

        if (samples == 0)
            return 0;

        return Math.Sqrt(sumSquares / (double)samples);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        StopInternal();
    }
}
