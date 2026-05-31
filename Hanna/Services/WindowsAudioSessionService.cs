using NAudio.CoreAudioApi;

namespace Hanna.Services;

internal sealed class WindowsAudioSessionService
{
    public string SetApplicationVolume(string appName, float percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        string target = NormalizeProcessName(appName);

        using var deviceEnumerator = new MMDeviceEnumerator();
        using MMDevice device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var sessions = device.AudioSessionManager.Sessions;

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];

            try
            {
                string processName = session.GetProcessID != 0
                    ? Process.GetProcessById((int)session.GetProcessID).ProcessName
                    : "";

                if (NormalizeProcessName(processName).Contains(target) || target.Contains(NormalizeProcessName(processName)))
                {
                    session.SimpleAudioVolume.Volume = percent / 100f;
                    session.SimpleAudioVolume.Mute = percent <= 0;
                    return $"Volumen de {processName} ajustado a {percent:0}%.";
                }
            }
            catch
            {
            }
        }

        return $"No encontré una sesión de audio activa para {appName}. Abre la app o reproduce algo primero.";
    }

    public string SetMasterVolume(float percent)
    {
        percent = Math.Clamp(percent, 0, 100);

        using var deviceEnumerator = new MMDeviceEnumerator();
        using MMDevice device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        device.AudioEndpointVolume.MasterVolumeLevelScalar = percent / 100f;
        device.AudioEndpointVolume.Mute = percent <= 0;

        return $"Volumen general ajustado a {percent:0}%.";
    }

    public object ListSessions()
    {
        var list = new List<object>();

        using var deviceEnumerator = new MMDeviceEnumerator();
        using MMDevice device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var sessions = device.AudioSessionManager.Sessions;

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            try
            {
                string processName = session.GetProcessID != 0
                    ? Process.GetProcessById((int)session.GetProcessID).ProcessName
                    : "Sistema";

                list.Add(new
                {
                    processName,
                    volume = Math.Round(session.SimpleAudioVolume.Volume * 100, 0),
                    muted = session.SimpleAudioVolume.Mute
                });
            }
            catch
            {
            }
        }

        return list;
    }

    private static string NormalizeProcessName(string value)
    {
        value = (value ?? "").Trim().ToLowerInvariant();

        return value switch
        {
            "spotify.exe" => "spotify",
            "google chrome" => "chrome",
            "chrome.exe" => "chrome",
            "discord.exe" => "discord",
            _ => value.Replace(".exe", "")
        };
    }
}
