using Hanna.Core;
using NAudio.Wave;

namespace Hanna.Services;

internal sealed class LocalAudioPlaybackService : IDisposable
{
    private readonly TtsService tts;
    private readonly SemaphoreSlim gate = new(1, 1);
    private WaveOutEvent? currentOutput;
    private AudioFileReader? currentReader;

    public LocalAudioPlaybackService(TtsService tts)
    {
        this.tts = tts;
    }

    public async Task Speak(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        await gate.WaitAsync(cancellationToken);

        string? audioPath = null;

        try
        {
            StopCurrentPlayback();

            audioPath = await tts.Generate(text, cancellationToken);

            if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
                return;

            await PlayAudioInBackground(audioPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audio local Error]: {ex.Message}");
        }
        finally
        {
            StopCurrentPlayback();

            if (!string.IsNullOrWhiteSpace(audioPath))
                TryDelete(audioPath);

            gate.Release();
        }
    }

    public void Stop()
    {
        try
        {
            StopCurrentPlayback();
        }
        catch
        {
        }
    }

    private async Task PlayAudioInBackground(string audioPath, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        currentReader = new AudioFileReader(audioPath);
        currentOutput = new WaveOutEvent
        {
            DesiredLatency = 120
        };

        currentOutput.Init(currentReader);
        currentOutput.PlaybackStopped += (_, e) =>
        {
            if (e.Exception != null)
                tcs.TrySetException(e.Exception);
            else
                tcs.TrySetResult();
        };

        currentOutput.Play();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                currentOutput?.Stop();
            }
            catch
            {
            }

            tcs.TrySetCanceled(cancellationToken);
        });

        await tcs.Task;
    }

    private void StopCurrentPlayback()
    {
        try
        {
            currentOutput?.Stop();
        }
        catch
        {
        }

        try
        {
            currentOutput?.Dispose();
        }
        catch
        {
        }

        try
        {
            currentReader?.Dispose();
        }
        catch
        {
        }

        currentOutput = null;
        currentReader = null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        StopCurrentPlayback();
        gate.Dispose();
    }
}
