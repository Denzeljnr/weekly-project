namespace SemanticSearch.Services;

// Tracks a rolling 60-second window of embedding requests and makes callers
// wait if sending more right now would exceed the free-tier quota (100/minute).
public class EmbeddingRateLimiter
{
    private readonly int _maxRequestsPerWindow;
    private readonly TimeSpan _window;
    private readonly Queue<DateTime> _timestamps = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EmbeddingRateLimiter(int maxRequestsPerWindow = 90, int windowSeconds = 60)
    {
        // 90, not 100 — small safety margin below Google's actual cap
        _maxRequestsPerWindow = maxRequestsPerWindow;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task WaitForSlotAsync(int requestCount)
    {
        await _lock.WaitAsync();
        try
        {
            while (true)
            {
                var now = DateTime.UtcNow;
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _window)
                    _timestamps.Dequeue();

                if (_timestamps.Count + requestCount <= _maxRequestsPerWindow)
                {
                    for (int i = 0; i < requestCount; i++)
                        _timestamps.Enqueue(now);
                    return;
                }

                var oldest = _timestamps.Peek();
                var waitTime = _window - (now - oldest) + TimeSpan.FromMilliseconds(200);
                await Task.Delay(waitTime > TimeSpan.Zero ? waitTime : TimeSpan.FromMilliseconds(200));
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}