using System.Collections.Concurrent;

namespace Stockfish.McpServer;

public sealed class StockfishPool : IDisposable
{
    private readonly ConcurrentBag<StockfishClient> _engines = new();
    private readonly SemaphoreSlim _poolSemaphore;
    private readonly int _size;

    public StockfishPool(string enginePath, int size)
    {
        _size = size;
        _poolSemaphore = new SemaphoreSlim(size, size);

        for (int i = 0; i < size; i++)
        {
            _engines.Add(new StockfishClient(enginePath));
        }
    }

    public async Task<T> UseAsync<T>(Func<StockfishClient, Task<T>> func)
    {
        await _poolSemaphore.WaitAsync();

        if (!_engines.TryTake(out var engine))
        {
            // This should never happen due to semaphore
            _poolSemaphore.Release();
            throw new InvalidOperationException("No Stockfish engine available");
        }

        try
        {
            return await func(engine);
        }
        finally
        {
            _engines.Add(engine);
            _poolSemaphore.Release();
        }
    }

    public void Dispose()
    {
        while (_engines.TryTake(out var engine))
        {
            engine.Dispose();
        }
        _poolSemaphore.Dispose();
    }
}