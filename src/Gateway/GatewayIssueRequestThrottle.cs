using System.Collections.Concurrent;

namespace EnterprisePKI.Gateway;

public interface IGatewayIssueRequestThrottle
{
    bool TryAcquire(string partitionKey);
}

public sealed class GatewayIssueRequestThrottle : IGatewayIssueRequestThrottle
{
    private sealed class BucketState
    {
        public DateTime WindowStartUtc { get; set; }
        public int Count { get; set; }
    }

    private readonly ConcurrentDictionary<string, BucketState> _buckets = new();
    private readonly int _permitLimit;
    private readonly TimeSpan _window;

    public GatewayIssueRequestThrottle(int permitLimit, TimeSpan window)
    {
        _permitLimit = permitLimit;
        _window = window;
    }

    public bool TryAcquire(string partitionKey)
    {
        var now = DateTime.UtcNow;
        var bucket = _buckets.GetOrAdd(partitionKey, _ => new BucketState
        {
            WindowStartUtc = now,
            Count = 0
        });

        lock (bucket)
        {
            if (now - bucket.WindowStartUtc >= _window)
            {
                bucket.WindowStartUtc = now;
                bucket.Count = 0;
            }

            if (bucket.Count >= _permitLimit)
            {
                return false;
            }

            bucket.Count++;
            return true;
        }
    }
}