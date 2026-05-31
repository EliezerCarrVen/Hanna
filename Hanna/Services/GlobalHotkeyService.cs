using System.Runtime.InteropServices;

namespace Hanna.Services;

internal sealed record HotkeyBinding(int Id, uint Modifiers, uint Key, string Label, Func<Task> Action);

internal sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModNoRepeat = 0x4000;
    public const uint VkF8 = 0x77;
    public const uint VkF9 = 0x78;
    public const uint VkEnter = 0x0D;
    public const uint VkH = 0x48;

    private readonly List<HotkeyBinding> bindings;
    private readonly CancellationTokenSource cts = new();
    private readonly Dictionary<int, HotkeyBinding> registered = new();
    private Thread? thread;
    private bool disposed;
    private uint threadId;

    public GlobalHotkeyService(IEnumerable<HotkeyBinding> bindings)
    {
        this.bindings = bindings.ToList();
    }

    public void Start()
    {
        if (thread != null)
            return;

        thread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "HannaGlobalHotkeyThread"
        };

        thread.Start();
    }

    private void MessageLoop()
    {
        threadId = GetCurrentThreadId();
        RegisterHotkeys();

        while (!cts.IsCancellationRequested && GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            if (msg.message == WmHotkey)
            {
                int id = msg.wParam.ToInt32();
                if (registered.TryGetValue(id, out var binding))
                    _ = Task.Run(binding.Action);
            }

            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        UnregisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        registered.Clear();

        foreach (var binding in bindings)
        {
            if (RegisterHotKey(IntPtr.Zero, binding.Id, binding.Modifiers | ModNoRepeat, binding.Key))
            {
                registered[binding.Id] = binding;
                Console.WriteLine($"[Hotkey] Registrado: {binding.Label}");
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"[Hotkey] No se registró {binding.Label}. Win32Error={error}");
            }
        }

        if (registered.Count == 0)
            Console.WriteLine("[Hotkey] No se pudo registrar ningún atajo. Prueba ejecutar Hanna como administrador.");
    }

    private void UnregisterHotkeys()
    {
        foreach (int id in registered.Keys)
        {
            try
            {
                UnregisterHotKey(IntPtr.Zero, id);
            }
            catch
            {
            }
        }

        registered.Clear();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        cts.Cancel();

        if (threadId != 0)
            PostThreadMessage(threadId, 0x0012, IntPtr.Zero, IntPtr.Zero);

        try
        {
            thread?.Join(1000);
        }
        catch
        {
        }

        cts.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);
}
