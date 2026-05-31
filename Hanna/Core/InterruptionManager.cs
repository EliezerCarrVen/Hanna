using System.Collections.Concurrent;

namespace Hanna.Core;

internal sealed class InterruptionManager
{
    private readonly ConcurrentDictionary<long, CancellationTokenSource> activeTasks = new();

    public CancellationToken Begin(long chatId, CancellationToken outerToken)
    {
        Stop(chatId);
        var linked = CancellationTokenSource.CreateLinkedTokenSource(outerToken);
        activeTasks[chatId] = linked;
        return linked.Token;
    }

    public void Stop(long chatId)
    {
        if (activeTasks.TryRemove(chatId, out var cts))
        {
            try { cts.Cancel(); } catch { }
            try { cts.Dispose(); } catch { }
        }
    }
}
