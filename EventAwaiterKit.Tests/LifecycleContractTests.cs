using System.Collections.Concurrent;

namespace EventAwaiterKit.Tests;

[TestClass]
public sealed class LifecycleContractTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);

    // Each adapter exposes the same controllable callback to the lifecycle tests.
    private static Task<bool> Wait(int shape, Action<Action> add, Action<Action> remove,
        TimeSpan? timeout = null, CancellationToken token = default)
    {
        if (shape == 0)
            return EventAwaiter.WaitForEventAsync(add, remove, timeout ?? Timeout.InfiniteTimeSpan, token);
        Action? invoke = null;
        return EventAwaiter.WaitForEventAsync((EventHandler h) =>
        {
            invoke = () => h(null, EventArgs.Empty);
            add(invoke);
        }, _ => remove(invoke!), timeout ?? Timeout.InfiniteTimeSpan, token);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task ValidationAndPreCancellationNeverSubscribe(int shape)
    {
        var calls = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        foreach (var timeout in new[] { TimeSpan.FromTicks(-1), TimeSpan.FromMilliseconds(-0.5),
            TimeSpan.FromMilliseconds(-2), TimeSpan.FromMilliseconds(int.MaxValue).Add(TimeSpan.FromTicks(1)), TimeSpan.MaxValue })
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                Wait(shape, _ => calls++, _ => calls++, timeout, cts.Token));
        }
        var canceled = Wait(shape, _ => calls++, _ => calls++, TimeSpan.Zero, cts.Token);
        var error = await Assert.ThrowsAsync<OperationCanceledException>(() => canceled);
        Assert.AreEqual(cts.Token, error.CancellationToken);
        Assert.IsTrue(canceled.IsCanceled);
        Assert.IsFalse(await Wait(shape, _ => calls++, _ => calls++, TimeSpan.Zero));
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task SubscriptionAndCleanupUseSameHandlerExactlyOnce(int shape)
    {
        Action? attached = null;
        var removes = 0;
        var wait = Wait(shape, h => attached = h, h =>
        {
            Assert.AreSame(attached, h);
            attached = null;
            removes++;
        }, TimeSpan.FromMilliseconds(int.MaxValue));
        Assert.IsNotNull(attached);
        var lateCallback = attached;
        lateCallback();
        Assert.IsTrue(await wait.WaitAsync(Bound));
        lateCallback();
        Assert.IsNull(attached);
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task CancellationDuringSubscriptionWaitsForAccessorExit(int shape)
    {
        using var cts = new CancellationTokenSource();
        var insideAdd = false;
        var removes = 0;
        var wait = Wait(shape, _ =>
        {
            insideAdd = true;
            cts.Cancel();
            Assert.AreEqual(0, removes);
            insideAdd = false;
        }, _ =>
        {
            Assert.IsFalse(insideAdd);
            removes++;
        }, token: cts.Token);
        await Assert.ThrowsAsync<OperationCanceledException>(() => wait.WaitAsync(Bound));
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    public async Task SubscriptionFailureAttemptsCleanupAndOverridesEvent(int shape, bool attachFirst)
    {
        Action? attached = null;
        var failure = new InvalidOperationException("subscription");
        var removes = 0;
        var wait = Wait(shape, h =>
        {
            if (attachFirst) { attached = h; h(); }
            throw failure;
        }, _ => { attached = null; removes++; });
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => wait.WaitAsync(Bound));
        Assert.AreSame(failure, actual);
        Assert.IsTrue(wait.IsFaulted);
        Assert.IsNull(attached);
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task CombinedAccessorFailuresPreserveBothExceptions(int shape)
    {
        var addFailure = new InvalidOperationException("add");
        var removeFailure = new ArgumentException("remove");
        var wait = Wait(shape, _ => throw addFailure, _ => throw removeFailure);
        var actual = await Assert.ThrowsAsync<AggregateException>(() => wait.WaitAsync(Bound));
        CollectionAssert.AreEqual(new Exception[] { addFailure, removeFailure }, actual.InnerExceptions.ToArray());
    }

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 1)]
    [DataRow(0, 2)]
    [DataRow(1, 0)]
    [DataRow(1, 1)]
    [DataRow(1, 2)]
    public async Task CleanupFailureOverridesEventTimeoutAndCancellation(int shape, int outcome)
    {
        using var cts = new CancellationTokenSource();
        Action? callback = null;
        var removes = 0;
        var failure = new OperationCanceledException("accessor failure is a fault");
        var wait = Wait(shape, h => callback = h, _ => { removes++; throw failure; },
            outcome == 1 ? TimeSpan.FromMilliseconds(10) : Timeout.InfiniteTimeSpan, cts.Token);
        if (outcome == 0) callback!();
        if (outcome == 2) cts.Cancel();
        var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => wait.WaitAsync(Bound));
        Assert.AreSame(failure, actual);
        Assert.IsTrue(wait.IsFaulted);
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task CancellationExceptionFromSubscriptionIsFaulted(int shape)
    {
        var failure = new OperationCanceledException("add");
        var wait = Wait(shape, _ => throw failure, _ => { });
        Assert.AreSame(failure, await Assert.ThrowsAsync<OperationCanceledException>(() => wait));
        Assert.IsTrue(wait.IsFaulted);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task ReentrantCleanupAndIndependentWaitAreSafe(int shape)
    {
        using var cts = new CancellationTokenSource();
        Action? callback = null;
        Task<bool>? nested = null;
        var removes = 0;
        var wait = Wait(shape, h => callback = h, _ =>
        {
            removes++;
            callback!();
            cts.Cancel();
            nested = Wait(shape, h => h(), _ => { });
        }, token: cts.Token);
        callback!();
        Assert.IsTrue(await wait.WaitAsync(Bound));
        Assert.IsNotNull(nested);
        Assert.IsTrue(await nested.WaitAsync(Bound));
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task ConcurrentSignalsAndLateCallbacksHaveOneCleanup(int shape)
    {
        for (var iteration = 0; iteration < 100; iteration++)
        {
            using var cts = new CancellationTokenSource();
            Action? callback = null;
            var removes = 0;
            var wait = Wait(shape, h => callback = h, _ => Interlocked.Increment(ref removes),
                TimeSpan.FromMilliseconds(1), cts.Token);
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var event1 = Task.Run(async () => { await start.Task; callback!(); });
            var event2 = Task.Run(async () => { await start.Task; callback!(); });
            var cancellation = Task.Run(async () => { await start.Task; cts.Cancel(); });
            start.SetResult();
            await Task.WhenAll(event1, event2, cancellation).WaitAsync(Bound);
            try { await wait.WaitAsync(Bound); }
            catch (OperationCanceledException ex) { Assert.AreEqual(cts.Token, ex.CancellationToken); }
            callback!();
            Assert.AreEqual(1, removes);
        }
    }

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 1)]
    [DataRow(0, 2)]
    [DataRow(1, 0)]
    [DataRow(1, 1)]
    [DataRow(1, 2)]
    public async Task CapturedContextOwnsCleanupAndTaskWaitsForIt(int shape, int outcome)
    {
        using var cts = new CancellationTokenSource();
        var context = new QueuedContext();
        var previous = SynchronizationContext.Current;
        var removes = 0;
        Action? callback = null;
        Task<bool> wait;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            wait = Wait(shape, h => callback = h, _ =>
            {
                Assert.AreSame(context, SynchronizationContext.Current);
                removes++;
            }, outcome == 1 ? TimeSpan.FromMilliseconds(10) : Timeout.InfiniteTimeSpan, cts.Token);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        if (outcome == 0) callback!();
        if (outcome == 2) cts.Cancel();
        await context.Posted.Task.WaitAsync(Bound);
        Assert.IsFalse(wait.IsCompleted);
        Assert.AreEqual(0, removes);
        context.Drain();
        if (outcome == 2) await Assert.ThrowsAsync<OperationCanceledException>(() => wait.WaitAsync(Bound));
        else Assert.AreEqual(outcome == 0, await wait.WaitAsync(Bound));
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task SynchronousSignalBeforeAttachmentStillDetachesAfterAdd(int shape)
    {
        Action? attached = null;
        var insideAdd = false;
        var removes = 0;
        var wait = Wait(shape, h =>
        {
            insideAdd = true;
            h();
            attached = h;
            insideAdd = false;
        }, h =>
        {
            Assert.IsFalse(insideAdd);
            Assert.AreSame(attached, h);
            attached = null;
            removes++;
        });
        Assert.IsTrue(await wait.WaitAsync(Bound));
        Assert.IsNull(attached);
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task ConsumerContinuationDoesNotRunInsideTheEventCallback(int shape)
    {
        using var insideEvent = new ThreadLocal<bool>();
        Action? callback = null;
        var wait = Wait(shape, h => callback = h, _ => { });
        var continuation = wait.ContinueWith(_ => Assert.IsFalse(insideEvent.Value),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        insideEvent.Value = true;
        try { callback!(); }
        finally { insideEvent.Value = false; }
        await continuation.WaitAsync(Bound);
        Assert.IsTrue(await wait);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailingDispatchFaultsTaskWithoutWrongContextCleanup(bool enqueueFirst)
    {
        var context = new QueuedContext { ThrowAfterEnqueue = enqueueFirst, ThrowBeforeEnqueue = !enqueueFirst };
        var previous = SynchronizationContext.Current;
        var removes = 0;
        Action? callback = null;
        Task<bool> wait;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            wait = Wait(0, h => callback = h, _ => removes++);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        callback!();
        await Assert.ThrowsAsync<InvalidOperationException>(() => wait.WaitAsync(Bound));
        context.Drain();
        Assert.AreEqual(0, removes);
    }

    private sealed class QueuedContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<Action> callbacks = new();
        internal TaskCompletionSource Posted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool ThrowBeforeEnqueue { get; init; }
        internal bool ThrowAfterEnqueue { get; init; }
        public override void Post(SendOrPostCallback d, object? state)
        {
            if (ThrowBeforeEnqueue) throw new InvalidOperationException("dispatch");
            callbacks.Enqueue(() => d(state));
            Posted.TrySetResult();
            if (ThrowAfterEnqueue) throw new InvalidOperationException("dispatch");
        }
        internal void Drain()
        {
            var previous = Current;
            try
            {
                SetSynchronizationContext(this);
                while (callbacks.TryDequeue(out var callback)) callback();
            }
            finally { SetSynchronizationContext(previous); }
        }
    }
}
