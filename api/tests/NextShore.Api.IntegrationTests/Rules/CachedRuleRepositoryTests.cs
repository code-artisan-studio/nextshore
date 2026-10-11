using Microsoft.Extensions.Logging.Abstractions;
using NextShore.Domain.Rules;
using NextShore.Infrastructure.Rules;

namespace NextShore.Api.IntegrationTests.Rules;

public class CachedRuleRepositoryTests
{
    private sealed class FakeSource : IRuleSnapshotSource
    {
        private int _loads;

        public int Loads => _loads;

        /// <summary>When set, each load waits for it, so tests can interleave calls.</summary>
        public TaskCompletionSource? Gate { get; set; }

        public async Task<RuleSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            var version = Interlocked.Increment(ref _loads);
            if (Gate is { } gate)
            {
                await gate.Task.WaitAsync(cancellationToken);
            }

            return new RuleSnapshot(version, [], [], []);
        }
    }

    private static CachedRuleRepository Create(FakeSource source) =>
        new(source, NullLogger<CachedRuleRepository>.Instance);

    [Fact]
    public async Task Loads_once_and_serves_from_cache()
    {
        var source = new FakeSource();
        var repository = Create(source);

        var first = await repository.GetSnapshotAsync();
        var second = await repository.GetSnapshotAsync();

        Assert.Same(first, second);
        Assert.Equal(1, source.Loads);
    }

    [Fact]
    public async Task Invalidate_forces_a_reload()
    {
        var source = new FakeSource();
        var repository = Create(source);
        await repository.GetSnapshotAsync();

        repository.Invalidate();
        var reloaded = await repository.GetSnapshotAsync();

        Assert.Equal(2, reloaded.Version);
        Assert.Equal(2, source.Loads);
    }

    [Fact]
    public async Task Concurrent_first_reads_share_one_load()
    {
        var source = new FakeSource { Gate = new TaskCompletionSource() };
        var repository = Create(source);

        var reads = Enumerable.Range(0, 10).Select(_ => repository.GetSnapshotAsync().AsTask()).ToList();
        source.Gate.SetResult();
        var snapshots = await Task.WhenAll(reads);

        Assert.Equal(1, source.Loads);
        Assert.All(snapshots, s => Assert.Same(snapshots[0], s));
    }

    [Fact]
    public async Task A_load_started_before_invalidate_is_not_cached()
    {
        var source = new FakeSource { Gate = new TaskCompletionSource() };
        var repository = Create(source);

        var inFlight = repository.GetSnapshotAsync().AsTask();
        repository.Invalidate();
        source.Gate.SetResult();
        var stale = await inFlight;

        source.Gate = null;
        var fresh = await repository.GetSnapshotAsync();

        Assert.Equal(1, stale.Version);
        Assert.Equal(2, fresh.Version);
    }
}
